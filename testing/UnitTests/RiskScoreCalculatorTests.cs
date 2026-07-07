using System.Linq;
using Xunit;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using XrmToolSuite.DeploymentRiskAnalyzer.Scoring;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for DeploymentRiskAnalyzer risk scoring (US-ALM07-8.1).
    /// Weights: Critical=25, High=12, Medium=5, Low=2, Info=0; capped at 100.
    /// Banding: any Critical OR score>=40 -> High; score>=15 -> Medium; else Low.
    /// </summary>
    public class RiskScoreCalculatorTests
    {
        private static AnalysisResult ResultWith(params Severity[] severities)
        {
            var r = new AnalysisResult();
            foreach (var s in severities)
                r.Findings.Add(new RiskFinding(AnalyzerCategory.General, s, "t", "d", "c", "fix"));
            RiskScoreCalculator.Apply(r);
            return r;
        }

        // TC-ALM07-SCORE-01: no findings -> score 0, Low risk.
        [Fact]
        public void NoFindings_ScoreZero_Low()
        {
            var r = ResultWith();
            Assert.Equal(0, r.Score);
            Assert.Equal(OverallRisk.Low, r.Risk);
        }

        // TC-ALM07-SCORE-02: per-severity weights are applied.
        [Theory]
        [InlineData(Severity.Low, 2)]
        [InlineData(Severity.Medium, 5)]
        [InlineData(Severity.High, 12)]
        [InlineData(Severity.Critical, 25)]
        [InlineData(Severity.Info, 0)]
        public void SingleFinding_UsesConfiguredWeight(Severity severity, int expected)
        {
            Assert.Equal(expected, ResultWith(severity).Score);
        }

        // TC-ALM07-SCORE-03: weights sum across findings.
        [Fact]
        public void MultipleFindings_WeightsSum()
        {
            // 12 + 5 + 2 = 19
            Assert.Equal(19, ResultWith(Severity.High, Severity.Medium, Severity.Low).Score);
        }

        // TC-ALM07-SCORE-04: score is capped at 100.
        [Fact]
        public void ManyCriticals_ScoreCappedAt100()
        {
            var r = ResultWith(Enumerable.Repeat(Severity.Critical, 10).ToArray()); // raw 250
            Assert.Equal(100, r.Score);
        }

        // TC-ALM07-SCORE-05: Info findings never move the score.
        [Fact]
        public void OnlyInfo_ScoreZero_Low()
        {
            var r = ResultWith(Severity.Info, Severity.Info, Severity.Info);
            Assert.Equal(0, r.Score);
            Assert.Equal(OverallRisk.Low, r.Risk);
        }

        // TC-ALM07-BAND-06: score below 15 bands Low.
        [Fact]
        public void ScoreBelowMediumThreshold_Low()
        {
            // 5 + 5 + 2 = 12 (< 15)
            Assert.Equal(OverallRisk.Low, ResultWith(Severity.Medium, Severity.Medium, Severity.Low).Risk);
        }

        // TC-ALM07-BAND-07: exactly 15 bands Medium (boundary).
        [Fact]
        public void ScoreAtMediumThreshold_Medium()
        {
            // 5 * 3 = 15
            var r = ResultWith(Severity.Medium, Severity.Medium, Severity.Medium);
            Assert.Equal(15, r.Score);
            Assert.Equal(OverallRisk.Medium, r.Risk);
        }

        // TC-ALM07-BAND-08: just below 40 bands Medium.
        [Fact]
        public void ScoreJustBelowHighThreshold_Medium()
        {
            // 12*3 + 2 = 38 (< 40)
            var r = ResultWith(Severity.High, Severity.High, Severity.High, Severity.Low);
            Assert.Equal(38, r.Score);
            Assert.Equal(OverallRisk.Medium, r.Risk);
        }

        // TC-ALM07-BAND-09: exactly 40 bands High (boundary).
        [Fact]
        public void ScoreAtHighThreshold_High()
        {
            // 5 * 8 = 40
            var r = ResultWith(Enumerable.Repeat(Severity.Medium, 8).ToArray());
            Assert.Equal(40, r.Score);
            Assert.Equal(OverallRisk.High, r.Risk);
        }

        // TC-ALM07-BAND-10: a single Critical forces High even when the score would band Medium.
        [Fact]
        public void SingleCritical_ForcesHigh_EvenWhenScoreWouldBeMedium()
        {
            var r = ResultWith(Severity.Critical); // score 25 -> would be Medium (15..39) without the override
            Assert.Equal(25, r.Score);
            Assert.Equal(OverallRisk.High, r.Risk);
        }

        // TC-ALM07-BAND-11: Critical forces High even at a very low score.
        [Fact]
        public void CriticalPlusNothing_LowScoreButHigh_IsImpossibleToDowngrade()
        {
            // Critical alone = 25; add nothing else. Risk must be High regardless of thresholds.
            Assert.Equal(OverallRisk.High, ResultWith(Severity.Critical).Risk);
        }

        // TC-ALM07-EXPLAIN-12: Explain reports counts and the score/risk.
        [Fact]
        public void Explain_ReportsCountsScoreAndRisk()
        {
            var r = ResultWith(Severity.Critical, Severity.High, Severity.Medium, Severity.Low, Severity.Info);
            var text = RiskScoreCalculator.Explain(r);
            Assert.Contains("1 critical", text);
            Assert.Contains("1 high", text);
            Assert.Contains("score 44/100", text); // 25+12+5+2 = 44
            Assert.Contains("High risk", text);     // any Critical -> High
        }

        // TC-ALM07-SUMMARY-13: SeveritySummary / CountBySeverity tally correctly.
        [Fact]
        public void SeveritySummary_CountsBySeverity()
        {
            var r = ResultWith(Severity.High, Severity.High, Severity.Low);
            Assert.Equal(2, r.CountBySeverity(Severity.High));
            Assert.Equal(1, r.CountBySeverity(Severity.Low));
            Assert.Equal(0, r.CountBySeverity(Severity.Critical));
            var summary = r.SeveritySummary();
            Assert.Equal(2, summary["High"]);
            Assert.Equal(1, summary["Low"]);
        }
    }
}
