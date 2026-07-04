using System.Linq;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using CoreSeverity = XrmToolSuite.Core.Analysis.Severity;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Reporting
{
    /// <summary>
    /// Projects the Deployment Risk Analyzer's <see cref="AnalysisResult"/> onto the suite-shared
    /// <see cref="ReportModel"/> that every exporter and the summary generators consume. All the
    /// deployment-specific vocabulary (report title, category labels, next steps, rollback guidance,
    /// GO/NO-GO verdicts, and the release-manager AI prompt) lives here so the shared reporters stay
    /// tool-agnostic.
    /// </summary>
    public static class DeploymentReportModel
    {
        /// <summary>System prompt used when this tool calls the AI summary generator.</summary>
        public const string AiSystemPrompt =
            "You are a Dynamics 365 / Dataverse release manager. From the deployment risk analysis JSON, " +
            "write a concise executive summary for a release decision. Call out the most important risks " +
            "by name.\n" +
            "FORMAT (strict): plain text only — no Markdown. Do not use '#', '*', '**', backticks, or a " +
            "title line. Write 2-3 short paragraphs separated by a single blank line. End with a final " +
            "line that begins exactly with 'RECOMMENDATION: ' followed by GO, GO WITH CAUTION, or NO-GO " +
            "and a one-sentence justification. Do not invent findings that are not present in the JSON.";

        public static ReportModel ToReportModel(AnalysisResult r)
        {
            var model = new ReportModel
            {
                ToolName = "Deployment Risk Analyzer",
                ToolVersion = "1.0.0",
                ReportTitle = "Deployment Risk Report",
                Subtitle = "XrmToolSuite · Deployment Risk Analyzer",
                ScoreWord = "risk",
                SubjectName = r.SolutionFriendlyName,
                SubjectKey = r.SolutionUniqueName,
                SubjectVersion = r.SolutionVersion,
                IsManaged = r.SolutionIsManaged,
                SourceEnvironment = r.SourceEnvironment,
                TargetEnvironment = r.TargetEnvironment,
                AnalyzedOnUtc = r.AnalyzedOnUtc,
                Score = r.Score,
                Band = (ScoreBand)(int)r.Risk,
                AiSummary = r.AiSummary,
                VerdictHigh = "NO-GO — remediate the critical/high findings before deploying.",
                VerdictMedium = "GO WITH CAUTION — review the findings below and mitigate where feasible.",
                VerdictLow = "GO — no significant deployment risk detected.",
            };

            foreach (var f in r.Findings)
                model.Findings.Add(new Finding(
                    CategoryName(f.Category),
                    (CoreSeverity)(int)f.Severity,
                    f.Title,
                    f.Description,
                    f.AffectedComponent,
                    f.Recommendation,
                    f.HelpUrl));

            model.AnalyzersRun.AddRange(r.AnalyzersRun);
            model.AnalyzersSkipped.AddRange(r.AnalyzersSkipped);

            string target = string.IsNullOrWhiteSpace(r.TargetEnvironment) ? "" : $" to {r.TargetEnvironment}";
            model.LeadIn =
                $"This solution has a {r.Risk.ToString().ToLowerInvariant()} deployment risk. " +
                $"Resolve the critical issues and review the recommendations before promoting{target}.";

            int crit = r.Findings.Count(f => f.Severity == Models.Severity.Critical);
            model.NextSteps.Add(new NextStep("Resolve all critical issues",
                crit > 0 ? $"{crit} must clear before deployment" : "None outstanding"));
            model.NextSteps.Add(new NextStep("Validate in a sandbox first",
                "Test the import before the target environment"));
            model.NextSteps.Add(new NextStep("Take a backup / stage for upgrade",
                r.SolutionIsManaged ? "Managed: stage before Apply upgrade" : "Unmanaged: no clean rollback — back up first"));
            model.NextSteps.Add(new NextStep("Share this report with stakeholders",
                "Print → Save as PDF for distribution"));

            model.ChecklistGuidance.Add(r.SolutionIsManaged
                ? "Managed solution: capture the current target version; rollback = re-import the previous managed .zip (or uninstall if newly introduced). Note: uninstalling a managed solution DELETES its tables and data — export data first."
                : "Unmanaged solution: THERE IS NO CLEAN ROLLBACK. Take an environment backup (admin center) before import and document every component for manual reversal.");
            model.ChecklistGuidance.Add("Take an on-demand environment backup immediately before deployment.");
            model.ChecklistGuidance.Add("Use 'Stage for upgrade' for managed upgrades so deletions can be reviewed before 'Apply upgrade'.");
            model.ChecklistGuidance.Add("Keep the previous solution export (.zip) in your release artifacts for fast restore.");

            return model;
        }

        /// <summary>Friendly category label for the deployment analyzer's categories.</summary>
        public static string CategoryName(AnalyzerCategory c)
        {
            switch (c)
            {
                case AnalyzerCategory.Dependencies: return "Missing Dependencies";
                case AnalyzerCategory.EnvironmentVariables: return "Environment Variables";
                case AnalyzerCategory.FlowsAndPlugins: return "Flows & Plugins";
                case AnalyzerCategory.Security: return "Security Changes";
                case AnalyzerCategory.SchemaConflicts: return "Schema Conflicts";
                case AnalyzerCategory.DeletedComponents: return "Deleted Components";
                case AnalyzerCategory.Forms: return "Form Changes";
                case AnalyzerCategory.Ribbon: return "Ribbon Changes";
                case AnalyzerCategory.PowerPages: return "Power Pages";
                default: return "General";
            }
        }
    }
}
