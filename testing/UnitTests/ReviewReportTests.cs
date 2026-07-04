using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.AiSolutionReviewer.Reporting;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the AI Solution Reviewer's SDK-free report projection
    /// (<see cref="ReviewReport"/>). The collector findings feed a concern score and per-area metrics;
    /// the AI narrative is manual-tested (live HTTP). Traces to US-AR-2 (concern score) and US-AR-3 (metrics).
    /// </summary>
    public class ReviewReportTests
    {
        private static AnalysisRun RunWith(params (string area, Severity sev)[] items)
        {
            var run = new AnalysisRun();
            foreach (var it in items)
                run.Findings.Add(new Finding(it.area, it.sev, "obs", "desc", "comp", "fix"));
            return run;
        }

        // TC-AR-REPORT-01: an empty review is zero-concern, Low band, and labelled "concern".
        [Fact]
        public void Empty_ScoreZero_Low()
        {
            var m = ReviewReport.Build(new AnalysisRun(), "Sales", "sales", "1.0", false, "DEV");
            Assert.Equal(0, m.Score);
            Assert.Equal(ScoreBand.Low, m.Band);
            Assert.Equal("concern", m.ScoreWord);
            Assert.Equal("AI Solution Reviewer", m.ToolName);
        }

        // TC-AR-REPORT-02: concern score weights severities (High=12, Medium=5 -> 17 -> Medium band).
        [Fact]
        public void ConcernScore_WeightsSeverities()
        {
            var m = ReviewReport.Build(RunWith(("Plugins", Severity.High), ("JavaScript", Severity.Medium)),
                "Sales", "sales", "1.0", false, "DEV");
            Assert.Equal(17, m.Score);
            Assert.Equal(ScoreBand.Medium, m.Band);
        }

        // TC-AR-REPORT-03: metrics carry the observation total and a per-area breakdown.
        [Fact]
        public void Metrics_TotalAndPerArea()
        {
            var m = ReviewReport.Build(RunWith(("Plugins", Severity.Medium), ("Plugins", Severity.Low), ("ALM & Governance", Severity.Info)),
                "Sales", "sales", "1.0", false, "DEV");
            Assert.Contains(m.Metrics, x => x.Label == "Observations" && x.Value == "3");
            Assert.Contains(m.Metrics, x => x.Label == "Plugins" && x.Value == "2");
        }

        // TC-AR-REPORT-04: the reviewer supplies an architecture-review AI prompt covering the required sections.
        [Fact]
        public void AiPrompt_CoversReviewSections()
        {
            Assert.Contains("PRIORITIZED BACKLOG", ReviewReport.AiSystemPrompt);
            Assert.Contains("SPRINT PLAN", ReviewReport.AiSystemPrompt);
            Assert.Contains("ARCHITECTURE RECOMMENDATIONS", ReviewReport.AiSystemPrompt);
        }
    }
}
