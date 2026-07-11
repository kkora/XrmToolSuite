using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.JavaScriptPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Retrieves JavaScript web resources and form event handlers from a live connection and runs the
    /// SDK-free rule engine over them. This is the only Dataverse-touching piece, so it is deliberately
    /// kept out of the unit-test set. Per-row failures degrade to an informational finding rather than
    /// throwing, so one bad web resource or form can't fail a whole batch.
    /// </summary>
    public sealed class JsCollector
    {
        // JScript web resource type per the webresourcetype option set.
        private const int WebResourceTypeJScript = 3;

        private readonly JsAnalysisOptions _options;

        /// <summary>Form-level findings (e.g. too many OnLoad handlers) produced by the last CollectFormUsage call.</summary>
        public List<Finding> LastFormFindings { get; } = new List<Finding>();

        public JsCollector(JsAnalysisOptions options = null)
        {
            _options = options ?? JsAnalysisOptions.Default;
        }

        // How many script bodies to download per request. Content is base64 (bundled libraries can be
        // megabytes each); pulling it for thousands of rows in one paged query produces responses large
        // enough for the Dataverse gateway to kill with "502 Bad Gateway" — so list light, then download
        // bodies in small bounded chunks with progress and cancellation.
        private const int ContentChunkSize = 10;

        public List<JsScriptAnalysis> Collect(IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
            => Collect(svc, worker, progress, customOnly: false, excludeNamePrefixes: null);

        /// <summary>
        /// Collects and analyzes JScript web resources. <paramref name="customOnly"/> limits the scan to
        /// unmanaged (custom) web resources server-side — skipping the thousands of Microsoft system
        /// libraries (AppCommon/…, Activities/…) most orgs carry. <paramref name="excludeNamePrefixes"/>
        /// drops any web resource whose name starts with one of the prefixes (case-insensitive) BEFORE its
        /// content is downloaded, so excluded scripts cost nothing.
        /// </summary>
        public List<JsScriptAnalysis> Collect(IOrganizationService svc, BackgroundWorker worker, Action<string> progress,
            bool customOnly, IEnumerable<string> excludeNamePrefixes)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));

            // Phase 1: lightweight listing — names only, never the content payload.
            progress?.Invoke("Listing JavaScript web resources...");
            var listQuery = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("name", "displayname")
            };
            listQuery.Criteria.AddCondition("webresourcetype", ConditionOperator.Equal, WebResourceTypeJScript);
            if (customOnly)
                listQuery.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
            var rows = svc.RetrieveAll(listQuery, worker: worker);

            var prefixes = (excludeNamePrefixes ?? Enumerable.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
            if (prefixes.Count > 0)
            {
                int before = rows.Count;
                rows = rows.Where(r =>
                {
                    var name = r.GetAttributeValue<string>("name") ?? "";
                    return !prefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));
                }).ToList();
                progress?.Invoke($"Excluded {before - rows.Count} script(s) by name prefix; {rows.Count} remain.");
            }

            // Phase 2: download + analyze content in small chunks.
            var results = new List<JsScriptAnalysis>();
            int done = 0;
            for (int i = 0; i < rows.Count; i += ContentChunkSize)
            {
                if (worker?.CancellationPending == true) break;
                var chunk = rows.Skip(i).Take(ContentChunkSize).ToList();
                var content = FetchContent(svc, chunk);
                foreach (var row in chunk)
                {
                    if (worker?.CancellationPending == true) break;
                    content.TryGetValue(row.Id, out var base64);
                    results.Add(ScoreRow(row, base64));
                    done++;
                }
                progress?.Invoke($"Analyzed {done} of {rows.Count} script(s)...");
            }

            progress?.Invoke($"Analyzed {results.Count} script(s).");
            return JsRules.Rank(results);
        }

        /// <summary>
        /// Downloads the <c>content</c> column for one chunk of web resources. Tries a single In-filtered
        /// query first; if that fails (e.g. the chunk's combined payload still trips the gateway), falls
        /// back to per-record retrieves so one oversized script can't sink the batch. A record whose
        /// content can't be fetched is simply absent from the map (analyzed as empty, surfaced as Info).
        /// </summary>
        private static Dictionary<Guid, string> FetchContent(IOrganizationService svc, List<Entity> chunk)
        {
            var map = new Dictionary<Guid, string>();
            try
            {
                var q = new QueryExpression("webresource") { ColumnSet = new ColumnSet("content") };
                q.Criteria.AddCondition("webresourceid", ConditionOperator.In,
                    chunk.Select(r => (object)r.Id).ToArray());
                foreach (var e in svc.RetrieveMultiple(q).Entities)
                    map[e.Id] = e.GetAttributeValue<string>("content");
                return map;
            }
            catch
            {
                foreach (var r in chunk)
                {
                    try
                    {
                        var e = svc.Retrieve("webresource", r.Id, new ColumnSet("content"));
                        map[r.Id] = e.GetAttributeValue<string>("content");
                    }
                    catch { /* skip this record; ScoreRow degrades it to an Info finding */ }
                }
                return map;
            }
        }

        private JsScriptAnalysis ScoreRow(Entity row, string base64Content)
        {
            var name = row.GetAttributeValue<string>("name")
                       ?? row.GetAttributeValue<string>("displayname")
                       ?? "(unnamed)";
            try
            {
                var code = DecodeContent(base64Content);
                return JsRules.Analyze(name, code, _options);
            }
            catch (Exception ex)
            {
                // Never let one bad web resource break the batch — surface it as an informational row.
                var analysis = new JsScriptAnalysis { ScriptName = name, Code = string.Empty, Score = 0, Band = ScoreBand.Low };
                analysis.Findings.Add(new Finding("JavaScript", Severity.Info, "Web resource could not be analyzed",
                    "Decoding or scanning this web resource raised an error, so it was skipped: " + ex.Message,
                    component: name));
                return analysis;
            }
        }

        private static string DecodeContent(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return string.Empty;
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Retrieves system forms and maps their FormXML event handlers to script libraries, and flags
        /// forms whose OnLoad handler count exceeds the configured threshold (added to <see cref="LastFormFindings"/>).
        /// </summary>
        public List<FormScriptUsage> CollectFormUsage(IOrganizationService svc, BackgroundWorker worker)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));

            LastFormFindings.Clear();

            var query = new QueryExpression("systemform")
            {
                ColumnSet = new ColumnSet("name", "objecttypecode", "formxml")
            };
            var rows = svc.RetrieveAll(query, worker: worker);

            var forms = new List<(string formName, string entity, string formXml)>();
            foreach (var row in rows)
            {
                if (worker?.CancellationPending == true) break;
                var name = row.GetAttributeValue<string>("name") ?? "(unnamed form)";
                var entity = row.GetAttributeValue<string>("objecttypecode") ?? "";
                var formXml = row.GetAttributeValue<string>("formxml") ?? "";
                forms.Add((name, entity, formXml));

                try
                {
                    int onLoad = FormEventMap.OnLoadHandlerCount(formXml);
                    if (onLoad > _options.OnLoadHandlerWarn)
                        LastFormFindings.Add(new Finding("Form", Severity.Medium, "Too many OnLoad handlers",
                            $"Form '{name}' ({entity}) has {onLoad} OnLoad handlers (threshold {_options.OnLoadHandlerWarn}). " +
                            "Each handler runs on every form load, adding startup latency.",
                            component: name,
                            recommendation: "Consolidate handlers, defer non-critical work, or move logic off the OnLoad event."));
                }
                catch
                {
                    // A malformed FormXML simply contributes no form finding — never throw.
                }
            }

            return FormEventMap.Map(forms);
        }
    }
}
