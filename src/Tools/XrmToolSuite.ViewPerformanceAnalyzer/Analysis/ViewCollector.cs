using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.ViewPerformanceAnalyzer.Analysis
{
    /// <summary>
    /// Retrieves an entity's system views (<c>savedquery</c>) and, optionally, personal views
    /// (<c>userquery</c>) from a live connection and scores each via <see cref="ViewScorer"/>. This is the
    /// only Dataverse-touching piece, so it is deliberately kept out of the SDK-free unit-test set. Per-row
    /// failures degrade to an informational finding rather than throwing, so one bad view can't fail a batch.
    /// </summary>
    public sealed class ViewCollector
    {
        private readonly ViewScoreOptions _options;

        public ViewCollector(ViewScoreOptions options = null)
        {
            _options = options ?? ViewScoreOptions.Default;
        }

        public List<ViewAnalysis> Collect(
            IOrganizationService svc,
            string entityLogicalName,
            bool includePersonal,
            BackgroundWorker worker,
            Action<string> progress)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));

            var results = new List<ViewAnalysis>();

            progress?.Invoke("Retrieving system views...");
            var system = svc.RetrieveAll(BuildQuery("savedquery", entityLogicalName), worker: worker);
            foreach (var v in system)
                results.Add(ScoreRow(v, "System", entityLogicalName));

            if (includePersonal)
            {
                if (worker?.CancellationPending == true) return results;
                progress?.Invoke("Retrieving personal views...");
                var personal = svc.RetrieveAll(BuildQuery("userquery", entityLogicalName), worker: worker);
                foreach (var v in personal)
                    results.Add(ScoreRow(v, "Personal", entityLogicalName));
            }

            progress?.Invoke($"Scored {results.Count} view(s).");
            return ViewScorer.Rank(results);
        }

        private static QueryExpression BuildQuery(string logicalName, string entityLogicalName)
        {
            var query = new QueryExpression(logicalName)
            {
                ColumnSet = new ColumnSet("name", "fetchxml", "layoutxml", "returnedtypecode", "querytype")
            };
            if (!string.IsNullOrWhiteSpace(entityLogicalName))
                query.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, entityLogicalName);
            return query;
        }

        private ViewAnalysis ScoreRow(Entity row, string viewType, string fallbackEntity)
        {
            var name = row.GetAttributeValue<string>("name");
            var entity = row.GetAttributeValue<string>("returnedtypecode") ?? fallbackEntity;
            var fetchXml = row.GetAttributeValue<string>("fetchxml");
            var layoutXml = row.GetAttributeValue<string>("layoutxml");

            try
            {
                return ViewScorer.Analyze(name, viewType, entity, fetchXml, layoutXml, _options);
            }
            catch (Exception ex)
            {
                // Never let one malformed view break the batch — surface it as an informational row.
                var view = new ViewAnalysis
                {
                    Name = name,
                    ViewType = viewType,
                    Entity = entity,
                    FetchXml = fetchXml,
                    LayoutXml = layoutXml,
                    Score = 0,
                    Band = ScoreBand.Low
                };
                view.Findings.Add(new Finding("View", Severity.Info,
                    "View could not be analyzed",
                    "Scoring this view raised an unexpected error, so it was skipped: " + ex.Message,
                    component: name));
                return view;
            }
        }

        /// <summary>
        /// Opt-in, read-only timing probe: runs the view's FetchXML with a small top cap and returns the
        /// elapsed milliseconds and row count. Bounds an otherwise-unbounded query so the probe stays cheap.
        /// </summary>
        public static (long ms, int rows) TimeView(IOrganizationService svc, string fetchXml, BackgroundWorker worker, int topCap = 50)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            if (worker?.CancellationPending == true) return (0, 0);

            var capped = AddTopLimit(fetchXml, topCap);
            var sw = Stopwatch.StartNew();
            var result = svc.RetrieveMultiple(new FetchExpression(capped));
            sw.Stop();
            return (sw.ElapsedMilliseconds, result.Entities.Count);
        }

        /// <summary>Adds a <c>top</c> to the fetch element when the query has no limit, keeping timing bounded.</summary>
        private static string AddTopLimit(string fetchXml, int top)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(fetchXml);
                if (doc.Root != null
                    && doc.Root.Attribute("top") == null
                    && doc.Root.Attribute("count") == null
                    && !string.Equals((string)doc.Root.Attribute("aggregate"), "true", StringComparison.OrdinalIgnoreCase))
                {
                    doc.Root.SetAttributeValue("top", top);
                }
                return doc.ToString();
            }
            catch
            {
                return fetchXml;
            }
        }
    }
}
