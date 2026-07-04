using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.TechnicalDebtAnalyzer.Reporting;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the Technical Debt Analyzer's SDK-free scoring/report projection
    /// (<see cref="TechDebtReport"/>). Debt weights mirror the suite default (Critical=25/High=12/
    /// Medium=5/Low=2) but — unlike deployment risk — a single Critical does NOT force a High band;
    /// debt accumulates. Traces to US-TD-3 (debt score) and US-TD-4 (dashboard metrics).
    /// </summary>
    public class TechDebtScoreTests
    {
        private static AnalysisRun RunWith(params Severity[] severities)
        {
            var run = new AnalysisRun();
            foreach (var s in severities)
                run.Findings.Add(new Finding("Unused Metadata", s, "t", "d", "c", "fix"));
            run.AnalyzersRun.Add("Unused Metadata");
            return run;
        }

        // TC-TD-SCORE-01: no findings -> score 0, Low band, and the score word is "technical debt".
        [Fact]
        public void NoFindings_ScoreZero_Low()
        {
            var m = TechDebtReport.Build(new AnalysisRun(), "DEV");
            Assert.Equal(0, m.Score);
            Assert.Equal(ScoreBand.Low, m.Band);
            Assert.Equal("technical debt", m.ScoreWord);
        }

        // TC-TD-SCORE-02: weights sum across findings (12 + 5 + 2 = 19 -> Medium).
        [Fact]
        public void Weights_SumAcrossFindings()
        {
            var m = TechDebtReport.Build(RunWith(Severity.High, Severity.Medium, Severity.Low), "DEV");
            Assert.Equal(19, m.Score);
            Assert.Equal(ScoreBand.Medium, m.Band);
        }

        // TC-TD-SCORE-03: a single Critical does NOT force High (debt accrues; 25 -> Medium band).
        [Fact]
        public void SingleCritical_DoesNotForceHigh()
        {
            var m = TechDebtReport.Build(RunWith(Severity.Critical), "DEV");
            Assert.Equal(25, m.Score);
            Assert.Equal(ScoreBand.Medium, m.Band); // 15 <= 25 < 40
        }

        // TC-TD-SCORE-04: accumulated debt reaches the High band (>= 40) and caps at 100.
        [Fact]
        public void AccumulatedDebt_BandsHigh_AndCaps()
        {
            var high = TechDebtReport.Build(RunWith(Enumerable.Repeat(Severity.High, 4).ToArray()), "DEV"); // 48
            Assert.Equal(ScoreBand.High, high.Band);

            var capped = TechDebtReport.Build(RunWith(Enumerable.Repeat(Severity.Critical, 10).ToArray()), "DEV"); // raw 250
            Assert.Equal(100, capped.Score);
        }

        // TC-TD-DASH-05: the dashboard metrics carry the total and a per-category breakdown.
        [Fact]
        public void Build_PopulatesMetrics_TotalAndPerCategory()
        {
            var run = new AnalysisRun();
            run.Findings.Add(new Finding("Unused Metadata", Severity.Medium, "a", "d", "c", "fix"));
            run.Findings.Add(new Finding("Dead Plugins", Severity.Low, "b", "d", "c", "fix"));
            run.Findings.Add(new Finding("Dead Plugins", Severity.Low, "c", "d", "c", "fix"));

            var m = TechDebtReport.Build(run, "DEV");
            Assert.Equal("Technical Debt Analyzer", m.ToolName);
            Assert.Contains(m.Metrics, x => x.Label == "Total findings" && x.Value == "3");
            Assert.Contains(m.Metrics, x => x.Label == "Dead Plugins" && x.Value == "2");
        }
    }
}
