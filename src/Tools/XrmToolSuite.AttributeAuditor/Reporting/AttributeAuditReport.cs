using System.Linq;
using XrmToolSuite.AttributeAuditor.Audit;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.AttributeAuditor.Reporting
{
    /// <summary>
    /// Projects an <see cref="AuditResult"/> onto the suite-shared <see cref="ReportModel"/> so the audit
    /// reuses every shared exporter (HTML dashboard, JSON, Excel, Markdown, PDF). Each retirement
    /// candidate becomes a Low finding; the gauge/metrics summarise the cleanup opportunity. SDK-free so
    /// it is unit-tested without a connection, exactly like the other tools' report projections.
    /// </summary>
    public static class AttributeAuditReport
    {
        public static ReportModel ToReportModel(AuditResult r)
        {
            int candidates = r.CandidateColumns;
            var model = new ReportModel
            {
                ToolName = "Attribute Auditor",
                ToolVersion = "1.0.0",
                ReportTitle = "Attribute Audit",
                Subtitle = "XrmToolSuite · Attribute Auditor",
                ScoreWord = "cleanup",
                SubjectName = r.EnvironmentName,
                AnalyzedOnUtc = r.AuditedOnUtc,
                Score = candidates > 100 ? 100 : candidates,
                Band = candidates == 0 ? ScoreBand.Low : candidates < 10 ? ScoreBand.Medium : ScoreBand.High,
                LeadIn = $"{candidates} custom column(s) show no usage signals out of {r.TotalColumns} audited — " +
                         "review the evidence and retire the confirmed-unused ones through a cleanup solution.",
                VerdictLow = "No unused custom columns detected.",
                VerdictMedium = "A few unused custom columns — review and retire the confirmed ones.",
                VerdictHigh = "Many unused custom columns — a cleanup pass will meaningfully reduce schema clutter.",
            };

            model.Metrics.Add(new MetricRow("Columns audited", r.TotalColumns.ToString()));
            model.Metrics.Add(new MetricRow("Used", r.UsedColumns.ToString()));
            model.Metrics.Add(new MetricRow("Retirement candidates", candidates.ToString()));
            model.Metrics.Add(new MetricRow("Used share",
                r.TotalColumns == 0 ? "—" : $"{100 * r.UsedColumns / r.TotalColumns}%"));

            foreach (var g in r.CandidatesByTable())
                foreach (var c in g.OrderBy(x => x.LogicalName))
                    model.Findings.Add(new Finding("Unused columns", Severity.Low,
                        $"Unused column '{c.LogicalName}'",
                        $"'{c.DisplayName ?? c.LogicalName}' ({c.AttributeType}) on {c.Table} has no usage signals (forms, views, processes, or field security).",
                        $"{c.Table}.{c.LogicalName}",
                        "Confirm it is genuinely unused, then retire it via a reviewed cleanup solution (dependency check first)."));

            model.NextSteps.Add(new NextStep("Review the candidates", $"{candidates} column(s) flagged — confirm each against business need"));
            model.NextSteps.Add(new NextStep("Check dependencies", "Run a dependency check before removing any column"));
            model.NextSteps.Add(new NextStep("Retire via a solution", "Delete confirmed-unused columns through source-controlled ALM, not ad hoc"));

            model.ChecklistGuidance.Add("Never delete a column without a passing dependency check and an explicit confirmation of scope.");
            model.ChecklistGuidance.Add("Prefer retiring columns through a solution export so the change is reviewable and reversible.");

            return model;
        }
    }
}
