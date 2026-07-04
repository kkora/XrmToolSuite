using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.SolutionComplexityScore.Analysis;
using XrmToolSuite.SolutionComplexityScore.Reporting;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the Solution Complexity Score's SDK-free metric/effort model
    /// (<see cref="ComplexityMetrics"/>) and report projection (<see cref="ComplexityReport"/>).
    /// The formulas are pure functions of the component tallies, so exact values are asserted.
    /// Traces to US-SC-3 (complexity/maintainability) and US-SC-4 (effort/cost estimates).
    /// </summary>
    public class ComplexityScoreTests
    {
        // TC-SC-METRIC-01: an empty solution is zero-complexity and fully maintainable.
        [Fact]
        public void Empty_ScoreZero_MaintainabilityFull()
        {
            var r = ComplexityMetrics.Compute(new ComponentCounts());
            Assert.Equal(0, r.ComplexityScore);
            Assert.Equal(100, r.MaintainabilityScore);
            Assert.Equal(0, r.UpgradeEffortDays);
            Assert.Equal(0, r.TestingEffortDays);
        }

        // TC-SC-METRIC-02: weighted points, score, maintainability, and effort are exact functions of the tallies.
        [Fact]
        public void KnownCounts_ProduceExactModel()
        {
            var c = new ComponentCounts { Tables = 10, Columns = 50, PluginSteps = 8, Forms = 4 };
            var r = ComplexityMetrics.Compute(c);

            Assert.Equal(66.0, r.ComplexityPoints);       // 30 + 10 + 20 + 6
            Assert.Equal(11, r.ComplexityScore);          // round(66/600*100)
            Assert.Equal(89, r.MaintainabilityScore);
            Assert.Equal(8, r.TestingEffortDays);         // 4*0.5 + 8*0.75
            Assert.Equal(3, r.UpgradeEffortDays);         // round(66*0.05)
            Assert.Equal(10, r.MigrationEffortDays);      // round(66*0.08 + 10*0.5)
            Assert.Equal(17600, r.SupportCostPerYear);    // (8+3)*800*2
        }

        // TC-SC-METRIC-03: the score saturates at 100 for a very large solution.
        [Fact]
        public void LargeSolution_ScoreCapsAt100()
        {
            var r = ComplexityMetrics.Compute(new ComponentCounts { Tables = 1000 }); // 3000 points
            Assert.Equal(100, r.ComplexityScore);
            Assert.Equal(0, r.MaintainabilityScore);
        }

        // TC-SC-METRIC-04: each dimension contributes its weighted points to the breakdown.
        [Fact]
        public void Dimensions_CarryWeightedContribution()
        {
            var r = ComplexityMetrics.Compute(new ComponentCounts { Tables = 5, CustomApis = 2 });
            Assert.Equal(15.0, r.Dimensions.Single(d => d.Name == "Tables").Points);       // 5*3
            Assert.Equal(5.0, r.Dimensions.Single(d => d.Name == "Custom APIs").Points);   // 2*2.5
        }

        // TC-SC-REPORT-05: the report projection sets the complexity gauge, effort metrics, and band.
        [Fact]
        public void Report_ProjectsScore_MetricsAndBand()
        {
            var c = new ComponentCounts { Tables = 200 }; // 600 points -> score 100 -> High
            var m = ComplexityReport.Build(c, "Sales", "sales", "1.0.0.0", false, "DEV");

            Assert.Equal("complexity", m.ScoreWord);
            Assert.Equal(100, m.Score);
            Assert.Equal(ScoreBand.High, m.Band);
            Assert.Contains(m.Metrics, x => x.Label == "Maintainability");
            Assert.Contains(m.Metrics, x => x.Label == "Upgrade effort");
        }

        // TC-SC-REPORT-06: a wide form is flagged as a hotspot; an unremarkable solution reads as "no hotspots".
        [Fact]
        public void Report_FlagsWideForm_ElseNoHotspots()
        {
            var wide = ComplexityReport.Build(
                new ComponentCounts { Forms = 1, WidestForm = 140, WidestFormName = "account_main" },
                "Sales", "sales", "1.0", false, "DEV");
            Assert.Contains(wide.Findings, f => f.Title == "Very wide form" && f.Severity == Severity.Medium);

            var plain = ComplexityReport.Build(new ComponentCounts { Tables = 2 }, "Sales", "sales", "1.0", false, "DEV");
            Assert.Contains(plain.Findings, f => f.Title == "No structural hotspots");
        }
    }
}
