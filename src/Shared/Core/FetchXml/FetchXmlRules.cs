using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.FetchXml
{
    /// <summary>Tunable thresholds for the FetchXML performance rules.</summary>
    public sealed class FetchXmlAnalysisOptions
    {
        /// <summary>Selected-column count above which payload is flagged (Medium).</summary>
        public int MaxAttributes { get; set; } = 30;

        /// <summary>Link-entity (join) count above which joins are flagged High.</summary>
        public int MaxLinkEntities { get; set; } = 4;

        /// <summary>Link-entity count above which joins are flagged Medium (and Distinct-with-links noted).</summary>
        public int WarnLinkEntities { get; set; } = 2;

        public static FetchXmlAnalysisOptions Default => new FetchXmlAnalysisOptions();
    }

    /// <summary>Output of <see cref="FetchXmlRules.Analyze"/>: findings, an estimated cost, its band, and plain suggestions.</summary>
    public sealed class FetchXmlAnalysis
    {
        public List<Finding> Findings { get; } = new List<Finding>();

        /// <summary>Heuristic cost estimate (0–100) derived from the finding severities. Not a server statistic.</summary>
        public int CostEstimate { get; set; }

        public ScoreBand Band { get; set; }

        /// <summary>Plain-text, actionable optimization suggestions.</summary>
        public List<string> Suggestions { get; } = new List<string>();
    }

    /// <summary>
    /// The FetchXML performance rule engine. Pure, deterministic, and SDK-free — it operates only on a
    /// <see cref="ParsedFetchXml"/>, so it is unit-testable and reusable by View / Dashboard analyzers.
    /// Every cost/index judgement is a labeled <em>heuristic estimate</em>: without server statistics the
    /// tool cannot measure true cost, only structural risk.
    /// </summary>
    public static class FetchXmlRules
    {
        private const string Category = "FetchXML";

        public static FetchXmlAnalysis Analyze(ParsedFetchXml q, FetchXmlAnalysisOptions opts = null)
        {
            opts = opts ?? FetchXmlAnalysisOptions.Default;
            var a = new FetchXmlAnalysis();

            int linkCount = q.LinkCount;
            bool hasPaging = q.PageSize.HasValue && q.PageSize.Value > 0;
            bool hasTop = q.Top.HasValue && q.Top.Value > 0;

            // --- Payload: all-attributes ---
            if (q.AllAttributes)
            {
                a.Findings.Add(new Finding(Category, Severity.High,
                    "Query selects all attributes",
                    "The query uses <all-attributes/>, which retrieves every column of the entity and inflates payload, memory, and network cost.",
                    component: q.RootEntity,
                    recommendation: "Replace <all-attributes/> with explicit <attribute/> elements for only the columns you need."));
                a.Suggestions.Add("Replace <all-attributes/> with explicit <attribute/> elements.");
            }

            // --- Payload: too many columns ---
            if (q.TotalAttributeCount > opts.MaxAttributes)
            {
                a.Findings.Add(new Finding(Category, Severity.Medium,
                    "Large number of selected columns",
                    $"The query selects {q.TotalAttributeCount} attributes (heuristic threshold {opts.MaxAttributes}). Wide projections increase payload and slow serialization.",
                    component: q.RootEntity,
                    recommendation: "Trim the attribute list to only the columns the view/report actually displays."));
                a.Suggestions.Add($"Reduce the selected columns (currently {q.TotalAttributeCount}, over the {opts.MaxAttributes} threshold).");
            }

            // --- Missing root filter (unbounded scan) ---
            if (!q.HasRootFilter && !q.HasAggregate)
            {
                a.Findings.Add(new Finding(Category, Severity.High,
                    "No filter on the root entity",
                    "The root entity has no filter, so the query can scan the entire table — the single biggest driver of slow FetchXML.",
                    component: q.RootEntity,
                    recommendation: "Add a <filter> on the root entity (e.g. an owner, status, or date-range condition) to bound the scan."));
                a.Suggestions.Add("Add a filter on the root entity to avoid an unbounded table scan.");
            }

            // --- Joins ---
            if (linkCount > opts.MaxLinkEntities)
            {
                a.Findings.Add(new Finding(Category, Severity.High,
                    "Excessive link-entity joins",
                    $"The query has {linkCount} link-entities (heuristic threshold {opts.MaxLinkEntities}). Many joins multiply the work the server must do per row.",
                    component: q.RootEntity,
                    recommendation: "Reduce the number of joins, or split the query and stitch results in the client."));
                a.Suggestions.Add($"Reduce link-entity joins (currently {linkCount}, over the {opts.MaxLinkEntities} threshold).");
            }
            else if (linkCount > opts.WarnLinkEntities)
            {
                a.Findings.Add(new Finding(Category, Severity.Medium,
                    "Several link-entity joins",
                    $"The query has {linkCount} link-entities (heuristic warning threshold {opts.WarnLinkEntities}). Each join adds cost; verify all are necessary.",
                    component: q.RootEntity,
                    recommendation: "Confirm each join is required and that join keys are indexed lookup/id columns."));
                a.Suggestions.Add("Review whether every link-entity join is necessary.");
            }

            // --- Outer joins (heuristic) ---
            var outerLinks = q.AllLinks().Where(l => l.IsOuter).ToList();
            if (outerLinks.Any())
            {
                a.Findings.Add(new Finding(Category, Severity.Low,
                    "Outer join(s) present",
                    $"The query contains {outerLinks.Count} outer join(s). Heuristic estimate: outer joins prevent some query-optimizer plans and can be costlier than inner joins.",
                    component: string.Join(", ", outerLinks.Select(l => l.Entity).Where(e => !string.IsNullOrEmpty(e))),
                    recommendation: "Use an inner join where the related record is always expected; keep outer joins only where nulls are meaningful."));
                a.Suggestions.Add("Prefer inner joins over outer joins where the related record always exists.");
            }

            // --- Sort on link-entity / likely non-indexed (heuristic) ---
            var linkOrders = q.Orders.Where(o => o.OnLinkEntity).ToList();
            if (linkOrders.Any())
            {
                a.Findings.Add(new Finding(Category, Severity.Medium,
                    "Sort on a link-entity column",
                    $"The query orders by {linkOrders.Count} link-entity column(s). Heuristic estimate: sorting on joined (often non-indexed) columns forces expensive server-side sorts.",
                    component: string.Join(", ", linkOrders.Select(o => o.Attribute).Where(x => !string.IsNullOrEmpty(x))),
                    recommendation: "Sort on an indexed column of the root entity where possible, or remove the sort if the UI can order client-side."));
                a.Suggestions.Add("Avoid sorting on link-entity columns; prefer an indexed root-entity column.");
            }

            // --- Aggregate without filter ---
            if (q.HasAggregate && !q.HasRootFilter)
            {
                a.Findings.Add(new Finding(Category, Severity.Medium,
                    "Aggregate without a filter",
                    "The query aggregates without any root filter, so it may aggregate across the entire table and can hit the aggregate record cap.",
                    component: q.RootEntity,
                    recommendation: "Add a filter to bound the aggregate to a relevant subset of records."));
                a.Suggestions.Add("Add a filter before aggregating to bound the record set.");
            }

            // --- No paging and no top and no aggregate (unbounded rows) ---
            if (!hasPaging && !hasTop && !q.HasAggregate)
            {
                a.Findings.Add(new Finding(Category, Severity.Low,
                    "No paging or top limit",
                    "The query sets neither a page size (count) nor a top limit. Heuristic estimate: it may return an unbounded number of rows.",
                    component: q.RootEntity,
                    recommendation: "Add a page size (count) or a top limit, and page through results with the paging cookie."));
                a.Suggestions.Add("Add a page size (count) or top limit and page through results.");
            }

            // --- Distinct with several links (informational) ---
            if (q.Distinct && linkCount > opts.WarnLinkEntities)
            {
                a.Findings.Add(new Finding(Category, Severity.Info,
                    "Distinct over multiple joins",
                    $"The query is distinct across {linkCount} joins. De-duplicating a multi-join result set adds server work; confirm distinct is required.",
                    component: q.RootEntity,
                    recommendation: "Verify distinct is needed; if joins don't fan out rows, distinct may be removable."));
                a.Suggestions.Add("Confirm distinct is necessary given the number of joins.");
            }

            if (a.Findings.Count == 0)
            {
                a.Findings.Add(new Finding(Category, Severity.Info,
                    "No performance risks detected",
                    "The heuristic rules found no structural performance risks in this query. Live timing can still ground the estimate.",
                    component: q.RootEntity));
            }

            // Cost estimate (heuristic) = capped weighted sum of finding severities; band at 15/40.
            a.CostEstimate = ScoreCalculator.RiskDefault.Score(a.Findings);
            a.Band = ScoreCalculator.BandFor(a.CostEstimate, 15, 40);

            return a;
        }
    }
}
