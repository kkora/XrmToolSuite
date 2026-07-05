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

        public List<JsScriptAnalysis> Collect(IOrganizationService svc, BackgroundWorker worker, Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));

            progress?.Invoke("Retrieving JavaScript web resources...");
            var query = new QueryExpression("webresource")
            {
                ColumnSet = new ColumnSet("name", "displayname", "content")
            };
            query.Criteria.AddCondition("webresourcetype", ConditionOperator.Equal, WebResourceTypeJScript);

            var rows = svc.RetrieveAll(query, worker: worker);

            var results = new List<JsScriptAnalysis>();
            int done = 0;
            foreach (var row in rows)
            {
                if (worker?.CancellationPending == true) break;
                results.Add(ScoreRow(row));
                if ((++done % 25) == 0)
                    progress?.Invoke($"Analyzed {done} of {rows.Count} script(s)...");
            }

            progress?.Invoke($"Analyzed {results.Count} script(s).");
            return JsRules.Rank(results);
        }

        private JsScriptAnalysis ScoreRow(Entity row)
        {
            var name = row.GetAttributeValue<string>("name")
                       ?? row.GetAttributeValue<string>("displayname")
                       ?? "(unnamed)";
            try
            {
                var code = DecodeContent(row.GetAttributeValue<string>("content"));
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
