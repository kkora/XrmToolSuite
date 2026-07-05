using System.Globalization;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.SolutionComplexityScore.Analysis;

namespace XrmToolSuite.SolutionComplexityScore.Reporting
{
    /// <summary>
    /// Projects a solution's <see cref="ComponentCounts"/> and computed <see cref="ComplexityResult"/>
    /// onto the suite-shared <see cref="ReportModel"/>: the complexity score drives the gauge, the
    /// effort/cost estimates and dimension tallies become dashboard metrics, and notable outliers become
    /// findings. SDK-free, so the whole projection is unit-testable.
    /// </summary>
    public static class ComplexityReport
    {
        // Complexity bands split the 0–100 range into thirds.
        public const int MediumBand = 34;
        public const int HighBand = 67;

        public const string AiSystemPrompt =
            "You are a Dynamics 365 / Dataverse solution architect. From the solution complexity JSON, " +
            "write a concise executive summary of how complex the solution is, what drives that complexity, " +
            "and the maintenance/upgrade implications. Reference the biggest dimensions and effort estimates.\n" +
            "FORMAT (strict): plain text only — no Markdown. Do not use '#', '*', '**', backticks, or a title " +
            "line. Write 2-3 short paragraphs separated by a single blank line. End with a final line that " +
            "begins exactly with 'RECOMMENDATION: ' and a one-sentence action. Do not invent numbers.";

        // Quality-finding category + severity mapping (deduction weight -> severity).
        private const string QualityCategory = "Solution Quality";
        private static Severity QualitySeverity(int points) => points >= 10 ? Severity.Medium : Severity.Low;

        public static ReportModel Build(ComponentCounts counts, string subjectName, string subjectKey,
            string version, bool managed, string environmentName)
        {
            var r = ComplexityMetrics.Compute(counts);
            var q = QualityScore.Compute(counts, r);
            var band = ScoreCalculator.BandFor(r.ComplexityScore, MediumBand, HighBand);

            var model = new ReportModel
            {
                ToolName = "Solution Complexity Score",
                ToolVersion = "1.0.0",
                ReportTitle = "Solution Complexity Report",
                Subtitle = "XrmToolSuite · Solution Complexity Score",
                ScoreWord = "complexity",
                SubjectName = subjectName,
                SubjectKey = subjectKey,
                SubjectVersion = version,
                IsManaged = managed,
                SourceEnvironment = environmentName,
                Score = r.ComplexityScore,
                Band = band,
                VerdictHigh = "High complexity — plan refactoring and budget extra upgrade/testing time.",
                VerdictMedium = "Moderate complexity — manageable, but watch the largest dimensions.",
                VerdictLow = "Low complexity — straightforward to maintain and upgrade.",
                LeadIn = $"This solution scores {r.ComplexityScore}/100 for complexity " +
                         $"({band.ToString().ToLowerInvariant()}). Maintainability is {r.MaintainabilityScore}/100. " +
                         $"Build quality is {q.QualityScore}/100 ({q.BandLabel.ToLowerInvariant()}).",
            };

            // Headline dashboard metrics: the derived estimates first, then the raw tallies.
            model.Metrics.Add(new MetricRow("Quality score", $"{q.QualityScore}/100 ({q.BandLabel})",
                "how well-built vs. best practice (higher is better)"));
            model.Metrics.Add(new MetricRow("Maintainability", $"{r.MaintainabilityScore}/100"));
            model.Metrics.Add(new MetricRow("Upgrade effort", $"{r.UpgradeEffortDays} d", "person-days"));
            model.Metrics.Add(new MetricRow("Migration effort", $"{r.MigrationEffortDays} d", "person-days"));
            model.Metrics.Add(new MetricRow("Testing effort", $"{r.TestingEffortDays} d", "person-days"));
            model.Metrics.Add(new MetricRow("Est. support cost", "$" + r.SupportCostPerYear.ToString("N0", CultureInfo.InvariantCulture), "per year"));
            model.Metrics.Add(new MetricRow("Tables", counts.Tables.ToString()));
            model.Metrics.Add(new MetricRow("Columns", counts.Columns.ToString()));
            model.Metrics.Add(new MetricRow("Forms", counts.Forms.ToString()));
            model.Metrics.Add(new MetricRow("Views", counts.Views.ToString()));
            model.Metrics.Add(new MetricRow("Plugin steps", counts.PluginSteps.ToString()));
            model.Metrics.Add(new MetricRow("Flows", counts.Flows.ToString()));
            model.Metrics.Add(new MetricRow("Business rules", counts.BusinessRules.ToString()));

            AddHotspots(model, counts);
            AddQualityFindings(model, q);

            model.NextSteps.Add(new NextStep("Tackle the largest dimension first",
                "Complexity concentrates where counts are highest — reduce there for the biggest win."));
            model.NextSteps.Add(new NextStep("Split oversized forms",
                "Very wide forms hurt load time and usability — move rarely-used fields to tabs or related tables."));
            model.NextSteps.Add(new NextStep("Consolidate processes",
                "Merge overlapping flows/workflows/business rules to cut the automation surface."));
            model.NextSteps.Add(new NextStep("Budget the estimated effort",
                "Use the upgrade/migration/testing estimates when planning the next release."));

            model.AnalyzersRun.Add("Component inventory");
            return model;
        }

        private const string Hotspots = "Complexity Hotspots";

        private static void AddHotspots(ReportModel model, ComponentCounts c)
        {
            if (c.WidestForm >= 100)
                model.Findings.Add(new Finding(Hotspots, Severity.Medium, "Very wide form",
                    $"Form '{c.WidestFormName}' has ~{c.WidestForm} controls, which slows load and hurts usability.",
                    c.WidestFormName, "Split it into tabs or move rarely-used fields to a related table."));

            if (c.PluginSteps >= 30)
                model.Findings.Add(new Finding(Hotspots, Severity.Medium, "High plugin-step count",
                    $"{c.PluginSteps} plugin steps add significant hidden logic and upgrade risk.",
                    "Plugin steps", "Review for redundant steps and consolidate where possible."));

            int automation = c.Flows + c.Workflows + c.BusinessRules;
            if (automation >= 40)
                model.Findings.Add(new Finding(Hotspots, Severity.Low, "Large automation surface",
                    $"{automation} processes (flows/workflows/business rules) are a lot to reason about and test.",
                    "Automation", "Consolidate overlapping automation and retire unused processes."));

            if (c.JavaScriptWebResources >= 25)
                model.Findings.Add(new Finding(Hotspots, Severity.Low, "Heavy client-side scripting",
                    $"{c.JavaScriptWebResources} JavaScript web resources increase maintenance and upgrade cost.",
                    "JavaScript", "Audit scripts for dead code and prefer supported low-code alternatives."));

            if (c.Tables >= 50)
                model.Findings.Add(new Finding(Hotspots, Severity.Low, "Large data model",
                    $"{c.Tables} tables make the schema harder to learn and migrate.",
                    "Tables", "Confirm every table is used; retire obsolete ones."));

            if (model.Findings.Count == 0)
                model.Findings.Add(new Finding(Hotspots, Severity.Info, "No structural hotspots",
                    "No single dimension stands out as an outlier — complexity is evenly distributed.",
                    "(overview)", "Maintain current hygiene; re-score after major changes."));
        }

        /// <summary>
        /// Adds one finding per quality deduction (called AFTER <see cref="AddHotspots"/> so the hotspot
        /// "no structural hotspots" fallback still keys off an empty findings list). A clean solution gets a
        /// single positive note.
        /// </summary>
        private static void AddQualityFindings(ReportModel model, QualityResult q)
        {
            if (q.Deductions.Count == 0)
            {
                model.Findings.Add(new Finding(QualityCategory, Severity.Info, "Well-structured solution",
                    $"Build quality is {q.QualityScore}/100 ({q.BandLabel}) — no best-practice violations detected.",
                    "(overview)", "Maintain current standards; re-score after major changes."));
                return;
            }

            foreach (var d in q.Deductions)
                model.Findings.Add(new Finding(QualityCategory, QualitySeverity(d.Points),
                    d.Signal, d.Why, "(quality)",
                    "Address to raise the build-quality score toward best practice."));
        }
    }
}
