using System.Linq;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.EnvironmentComparisonSuite.Analysis;

namespace XrmToolSuite.EnvironmentComparisonSuite.Reporting
{
    /// <summary>
    /// Projects a <see cref="ComparisonReport"/> onto the suite-shared <see cref="ReportModel"/> that every
    /// exporter (JSON / Excel / PDF / HTML / Markdown) consumes. All Environment-Comparison vocabulary
    /// (title, score word "difference", verdicts, next steps) lives here so the shared reporters stay
    /// tool-agnostic. Masked (secret) values are already masked in the diffs, so they stay masked here.
    /// </summary>
    public static class ComparisonReportModel
    {
        public static ReportModel ToReportModel(
            ComparisonReport r,
            string sourceEnvironment,
            string targetEnvironment,
            string toolVersion = "1.2026.7.2")
        {
            var model = new ReportModel
            {
                ToolName = "Environment Comparison Suite",
                ToolVersion = toolVersion,
                ReportTitle = "Environment Comparison Report",
                Subtitle = "XrmToolSuite · Environment Comparison Suite",
                ScoreWord = "difference",
                SubjectName = string.IsNullOrWhiteSpace(targetEnvironment)
                    ? sourceEnvironment
                    : $"{sourceEnvironment} → {targetEnvironment}",
                SourceEnvironment = sourceEnvironment,
                TargetEnvironment = targetEnvironment,
                Score = r.Score,
                Band = r.Band,
                VerdictHigh = "Significant drift — reconcile the missing/changed components before promoting.",
                VerdictMedium = "Some drift — review the differences below and reconcile where required.",
                VerdictLow = "Environments are closely aligned — no significant drift detected.",
            };

            foreach (var f in r.Findings)
                model.Findings.Add(f);

            foreach (var m in r.Metrics)
                model.Metrics.Add(m);

            model.LeadIn =
                $"Comparing source '{sourceEnvironment}' against target '{targetEnvironment}'. " +
                $"{r.Diffs.Count(d => d.Class != DiffClass.Identical)} component(s) differ across " +
                $"{r.CountsByCategoryAndClass.Count} category(ies). Read-only — no changes were made to either environment.";

            int missing = r.Diffs.Count(d => d.Class == DiffClass.Missing);
            int extra = r.Diffs.Count(d => d.Class == DiffClass.Extra);
            int layering = r.Diffs.Count(d => d.Class == DiffClass.ManagedVsUnmanaged);

            model.NextSteps.Add(new NextStep("Add missing components",
                missing > 0 ? $"{missing} present in source only" : "None outstanding"));
            model.NextSteps.Add(new NextStep("Review extra components in target",
                extra > 0 ? $"{extra} present in target only" : "None"));
            model.NextSteps.Add(new NextStep("Reconcile managed/unmanaged layering",
                layering > 0 ? $"{layering} layering mismatch(es)" : "None"));
            model.NextSteps.Add(new NextStep("Share this report with stakeholders",
                "Export to PDF/Excel for the release record"));

            model.ChecklistGuidance.Add("This comparison is read-only; nothing was written to either environment.");
            model.ChecklistGuidance.Add("Secret-typed environment variable values are masked in every export.");
            model.ChecklistGuidance.Add("Re-run after remediation to confirm the environments converge.");

            return model;
        }
    }
}
