using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Reporting
{
    /// <summary>
    /// Turns a technical-debt analysis run into the suite-shared <see cref="ReportModel"/> that every
    /// exporter and the summary generators consume. Owns the tool's scoring weights, branding, cleanup
    /// next-steps, and the debt-focused AI prompt so the shared reporters stay tool-agnostic.
    /// </summary>
    public static class TechDebtReport
    {
        /// <summary>Debt weighting: higher score = more debt. Mirrors the suite default severity weights.</summary>
        public static readonly ScoreCalculator Scorer = new ScoreCalculator(
            new Dictionary<Severity, int>
            {
                { Severity.Critical, 25 },
                { Severity.High, 12 },
                { Severity.Medium, 5 },
                { Severity.Low, 2 },
                { Severity.Info, 0 },
            },
            mediumThreshold: 15,
            highThreshold: 40,
            criticalForcesHigh: false); // debt has no single "critical" gate; it accumulates

        public const string AiSystemPrompt =
            "You are a Dynamics 365 / Dataverse platform architect. From the technical-debt analysis JSON, " +
            "write a concise executive summary of the environment's debt and the highest-value cleanup work. " +
            "Group related items and call out the biggest themes by name.\n" +
            "FORMAT (strict): plain text only — no Markdown. Do not use '#', '*', '**', backticks, or a title " +
            "line. Write 2-3 short paragraphs separated by a single blank line. End with a final line that " +
            "begins exactly with 'RECOMMENDATION: ' and a one-sentence next action. Do not invent findings.";

        public static ReportModel Build(AnalysisRun run, string environmentName)
        {
            var findings = run.Findings;
            int score = Scorer.Score(findings);
            var band = Scorer.Band(findings, score);

            var model = new ReportModel
            {
                ToolName = "Technical Debt Analyzer",
                ToolVersion = "1.0.0",
                ReportTitle = "Technical Debt Report",
                Subtitle = "XrmToolSuite · Technical Debt Analyzer",
                ScoreWord = "technical debt",
                SubjectName = environmentName ?? "Dataverse environment",
                SourceEnvironment = environmentName,
                Score = score,
                Band = band,
                VerdictHigh = "High debt — schedule a dedicated cleanup sprint before adding new customisations.",
                VerdictMedium = "Moderate debt — fold the top items into upcoming sprints.",
                VerdictLow = "Low debt — maintain current hygiene and re-scan periodically.",
                LeadIn = $"This environment carries {band.ToString().ToLowerInvariant()} technical debt " +
                         $"(score {score}/100). Work the highest-severity items first.",
            };

            model.Findings.AddRange(findings);
            model.AnalyzersRun.AddRange(run.AnalyzersRun);
            model.AnalyzersSkipped.AddRange(run.AnalyzersSkipped);

            // Headline metrics: total items + per-category counts (drives the dashboard metric strip).
            model.Metrics.Add(new MetricRow("Total findings", findings.Count.ToString()));
            foreach (var g in findings.GroupBy(f => f.Category).OrderByDescending(g => g.Count()))
                model.Metrics.Add(new MetricRow(g.Key, g.Count().ToString()));

            model.NextSteps.Add(new NextStep("Triage the highest-severity items",
                "Start with High/Medium findings — they carry the most maintenance cost."));
            model.NextSteps.Add(new NextStep("Delete confirmed-unused components",
                "Remove empty tables, dead plugin steps, and draft processes."));
            model.NextSteps.Add(new NextStep("Modernise deprecated code",
                "Migrate Xrm.Page / 2011-endpoint scripts to the supported client API."));
            model.NextSteps.Add(new NextStep("Re-scan after cleanup",
                "Track the debt score down over successive sprints."));

            model.ChecklistGuidance.Add("Take a solution/environment backup before deleting any component.");
            model.ChecklistGuidance.Add("Delete unused components in a sandbox first and validate before promoting.");
            model.ChecklistGuidance.Add("Record each removal in your ALM change log for traceability.");

            return model;
        }
    }
}
