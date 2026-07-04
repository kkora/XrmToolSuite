using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.AiSolutionReviewer.Reporting
{
    /// <summary>
    /// Projects an AI-review run (collector findings) onto the suite-shared <see cref="ReportModel"/> and
    /// owns the architecture-reviewer AI prompt. The findings are the structured facts; the AI narrative
    /// (executive summary, architecture recommendations, modernization, refactoring, backlog, sprints)
    /// lands in <see cref="ReportModel.AiSummary"/>. The offline generator provides a deterministic
    /// fallback. SDK-free, so the projection is unit-testable.
    /// </summary>
    public static class ReviewReport
    {
        /// <summary>Concern weighting for the headline gauge (reuses the suite default severity weights).</summary>
        public static readonly ScoreCalculator Scorer = ScoreCalculator.RiskDefault;

        public const string AiSystemPrompt =
            "You are a principal Dynamics 365 / Dataverse solution architect performing a solution review. " +
            "You are given JSON of observations gathered across plugins, JavaScript, automation, security, " +
            "ALM, and governance. Produce a professional review for an architecture board.\n" +
            "Write these sections, each as a short titled block in PLAIN TEXT (no Markdown, no '#', '*', " +
            "'**', or backticks):\n" +
            "EXECUTIVE SUMMARY, ARCHITECTURE RECOMMENDATIONS, MODERNIZATION GUIDANCE, REFACTORING " +
            "SUGGESTIONS, PRIORITIZED BACKLOG (numbered, highest value first), SPRINT PLAN (group the " +
            "backlog into 2-3 sprints). Base everything strictly on the observations — do not invent " +
            "findings. End with a final line beginning exactly 'RECOMMENDATION: ' and a one-sentence verdict.";

        public static ReportModel Build(AnalysisRun run, string subjectName, string subjectKey,
            string version, bool managed, string environmentName)
        {
            var findings = run.Findings;
            int score = Scorer.Score(findings);
            var band = Scorer.Band(findings, score);

            var model = new ReportModel
            {
                ToolName = "AI Solution Reviewer",
                ToolVersion = "1.0.0",
                ReportTitle = "AI Solution Review",
                Subtitle = "XrmToolSuite · AI Solution Reviewer",
                ScoreWord = "concern",
                SubjectName = subjectName,
                SubjectKey = subjectKey,
                SubjectVersion = version,
                IsManaged = managed,
                SourceEnvironment = environmentName,
                Score = score,
                Band = band,
                VerdictHigh = "Material concerns — address the architecture recommendations before scaling this solution.",
                VerdictMedium = "Some concerns — fold the recommendations into the roadmap.",
                VerdictLow = "Healthy — no material architecture concerns detected.",
                LeadIn = $"This review found {findings.Count(f => f.Severity >= Severity.Medium)} notable observation(s) " +
                         $"across {findings.Select(f => f.Category).Distinct().Count()} areas. " +
                         "Generate the executive summary for AI-authored recommendations and a backlog.",
            };

            model.Findings.AddRange(findings);
            model.AnalyzersRun.AddRange(run.AnalyzersRun);
            model.AnalyzersSkipped.AddRange(run.AnalyzersSkipped);

            model.Metrics.Add(new MetricRow("Observations", findings.Count.ToString()));
            foreach (var g in findings.GroupBy(f => f.Category).OrderByDescending(g => g.Count()))
                model.Metrics.Add(new MetricRow(g.Key, g.Count().ToString()));

            model.NextSteps.Add(new NextStep("Generate the AI review",
                "Use 'Executive summary' to produce recommendations, a backlog, and a sprint plan."));
            model.NextSteps.Add(new NextStep("Review with the architecture board",
                "Export to Word and circulate for sign-off."));
            model.NextSteps.Add(new NextStep("Turn the backlog into work items",
                "Load the prioritized backlog into your tracker."));

            return model;
        }
    }
}
