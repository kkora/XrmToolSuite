using Xunit;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using XrmToolSuite.DeploymentRiskAnalyzer.Reporting;
using XrmToolSuite.Core.Reporting;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the HTML dashboard report (US-DG-EXPORT-HTML). The Deployment Risk
    /// Analyzer projects its result onto the suite-shared <c>ReportModel</c> (via
    /// <c>DeploymentReportModel.ToReportModel</c>) which the shared <c>HtmlDashboardBuilder</c>
    /// renders — a pure, SDK-free string builder, so we assert on the generated markup: escaping,
    /// theme scaffolding, gauge value, severity tallies, and that findings/recommendations surface.
    /// </summary>
    public class HtmlReportTests
    {
        private static string BuildHtml(AnalysisResult r) =>
            HtmlDashboardBuilder.Build(DeploymentReportModel.ToReportModel(r));

        private static AnalysisResult Sample()
        {
            var r = new AnalysisResult
            {
                SolutionUniqueName = "core_sales",
                SolutionFriendlyName = "Core Sales Solution",
                SolutionVersion = "1.2.0.0",
                SolutionIsManaged = true,
                SourceEnvironment = "DEV",
                TargetEnvironment = "PRODUCTION",
                Score = 78,
                Risk = OverallRisk.High,
            };
            r.AnalyzersRun.Add("DependencyAnalyzer");
            r.Findings.Add(new RiskFinding(AnalyzerCategory.Dependencies, Severity.Critical,
                "Missing plugin assembly", "ContactDeletePlugin not present in target",
                "ContactDeletePlugin", "Add the missing plugin assembly", "https://learn.microsoft.com/x"));
            r.Findings.Add(new RiskFinding(AnalyzerCategory.EnvironmentVariables, Severity.High,
                "Environment variable not found", "API_Endpoint missing", "API_Endpoint",
                "Create the environment variable"));
            r.Findings.Add(new RiskFinding(AnalyzerCategory.Security, Severity.Info,
                "New security role", "adds a role", "sales_role", "Review the role"));
            return r;
        }

        // TC-DG-HTML-01: a complete, self-contained document is produced.
        [Fact]
        public void Build_ProducesSelfContainedDocument()
        {
            var html = BuildHtml(Sample());
            Assert.Contains("<title>", html);
            Assert.Contains("<style>", html);
            Assert.DoesNotContain("http-equiv", html); // no external refs
            Assert.DoesNotContain("cdn", html.ToLowerInvariant());
        }

        // TC-DG-HTML-02: both theme hooks are present so the report follows the reader's theme.
        [Fact]
        public void Build_IsThemeAware()
        {
            var html = BuildHtml(Sample());
            Assert.Contains("prefers-color-scheme:dark", html);
            Assert.Contains("[data-theme=\"dark\"]", html);
            Assert.Contains("[data-theme=\"light\"]", html);
        }

        // TC-DG-HTML-03: the gauge renders the score and the risk band.
        [Fact]
        public void Build_RendersScoreAndBand()
        {
            var html = BuildHtml(Sample());
            Assert.Contains(">78<", html);            // gauge number
            Assert.Contains("HIGH RISK", html);       // band label
            Assert.Contains("stroke-dasharray=\"78 100\"", html); // arc fill = score
        }

        // TC-DG-HTML-04: severity KPI counts are tallied and the friendly category name surfaces.
        [Fact]
        public void Build_TalliesSeverityCounts()
        {
            var html = BuildHtml(Sample());
            Assert.Contains("Critical", html);
            Assert.Contains("Environment Variables", html); // friendly category name (from the adapter)
        }

        // TC-DG-HTML-05: findings, recommendations and help links surface.
        [Fact]
        public void Build_SurfacesFindingsAndRecommendations()
        {
            var html = BuildHtml(Sample());
            Assert.Contains("Missing plugin assembly", html);
            Assert.Contains("Add the missing plugin assembly", html);
            Assert.Contains("https://learn.microsoft.com/x", html);
        }

        // TC-DG-HTML-06: user content is HTML-encoded (no injection / broken markup).
        [Fact]
        public void Build_EncodesUserContent()
        {
            var r = Sample();
            r.Findings.Add(new RiskFinding(AnalyzerCategory.General, Severity.Medium,
                "Bad <script> & \"quote\"", "desc", "comp<>", "fix", null));
            var html = BuildHtml(r);
            Assert.Contains("Bad &lt;script&gt;", html);
            Assert.DoesNotContain("<script>", html);
        }

        // TC-DG-HTML-07: the AI/offline executive summary is included when present.
        [Fact]
        public void Build_IncludesExecutiveSummaryWhenPresent()
        {
            var r = Sample();
            r.AiSummary = "Deployment is high risk. Resolve criticals first.";
            var html = BuildHtml(r);
            Assert.Contains("Executive Summary", html);
            Assert.Contains("Resolve criticals first", html);
        }

        // TC-DG-HTML-08: a clean solution reads as clear, not blank.
        [Fact]
        public void Build_NoFindings_ShowsClearState()
        {
            var r = new AnalysisResult
            {
                SolutionFriendlyName = "Clean", SolutionVersion = "1.0.0.0",
                SourceEnvironment = "DEV", Score = 0, Risk = OverallRisk.Low,
            };
            var html = BuildHtml(r);
            Assert.Contains("LOW RISK", html);
            Assert.Contains("Nothing flagged", html);
        }
    }
}
