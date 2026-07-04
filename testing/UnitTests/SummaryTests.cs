using System.Linq;
using Xunit;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using XrmToolSuite.DeploymentRiskAnalyzer.Summarization;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// Executable tests for the SDK-free summary logic: the anonymized payload builder and the
    /// offline templated generator. The live ClaudeSummaryGenerator (HTTP) is manual-tested.
    /// Traces to US-DG-8 (deployment summary).
    /// </summary>
    public class SummaryTests
    {
        private static AnalysisResult Result(OverallRisk risk, int score, string target,
            params RiskFinding[] findings)
        {
            var r = new AnalysisResult
            {
                SolutionFriendlyName = "Demo",
                SolutionVersion = "1.0.0.0",
                SolutionIsManaged = false,
                Risk = risk,
                Score = score,
                TargetEnvironment = target
            };
            r.Findings.AddRange(findings);
            return r;
        }

        private static RiskFinding F(Severity sev, string title = "t", string comp = "c") =>
            new RiskFinding(AnalyzerCategory.Dependencies, sev, title, "desc", comp, "fix");

        // TC-DG-SUM-01: payload maps score/risk and reflects the target connection as a boolean flag.
        [Fact]
        public void Payload_MapsScoreRisk_AndHasTargetFlag()
        {
            var withTarget = SummaryPayloadBuilder.Build(Result(OverallRisk.Medium, 17, "PROD", F(Severity.High)), true);
            Assert.Equal(17, withTarget.Score);
            Assert.Equal("Medium", withTarget.Risk);
            Assert.True(withTarget.HasTarget);

            var noTarget = SummaryPayloadBuilder.Build(Result(OverallRisk.Low, 3, null, F(Severity.Low)), true);
            Assert.False(noTarget.HasTarget);
        }

        // TC-DG-SUM-02: component redaction (Mode C) nulls out component names; enabled keeps them.
        [Fact]
        public void Payload_RedactsComponents_WhenIncludeComponentsFalse()
        {
            var r = Result(OverallRisk.High, 40, "PROD", F(Severity.High, "Publisher prefix collision", "SolutionDemo"));

            Assert.Null(SummaryPayloadBuilder.Build(r, includeComponents: false).TopFindings[0].Component);
            Assert.Equal("SolutionDemo", SummaryPayloadBuilder.Build(r, includeComponents: true).TopFindings[0].Component);
        }

        // TC-DG-SUM-03: top-N caps the finding list, sets Truncated, and keeps highest severities first.
        [Fact]
        public void Payload_TopN_TruncatesAndOrdersBySeverity()
        {
            var findings = new[] { F(Severity.Low), F(Severity.Critical), F(Severity.Medium), F(Severity.High) };
            var p = SummaryPayloadBuilder.Build(Result(OverallRisk.High, 50, "PROD", findings), true, topN: 2);

            Assert.Equal(2, p.TopFindings.Count);
            Assert.True(p.Truncated);
            Assert.Equal(4, p.FindingsTotal);
            Assert.Equal("Critical", p.TopFindings[0].Severity);
            Assert.Equal("High", p.TopFindings[1].Severity);
        }

        // TC-DG-SUM-04: offline generator emits the right go/no-go verdict per risk band and is not AI.
        [Theory]
        [InlineData(OverallRisk.High, "NO-GO")]
        [InlineData(OverallRisk.Medium, "GO WITH CAUTION")]
        [InlineData(OverallRisk.Low, "GO — no significant")]
        public void Offline_Verdict_MatchesRiskBand(OverallRisk risk, string expected)
        {
            var s = new TemplatedSummaryGenerator().Generate(Result(risk, 20, "PROD", F(Severity.Medium)), null, null);

            Assert.False(s.FromAi);
            Assert.Contains(expected, s.Text);
            Assert.Contains("score 20/100", s.Text);
        }

        // TC-DG-SUM-06: the plain-text normalizer strips Markdown and isolates the recommendation line.
        [Fact]
        public void Formatting_StripsMarkdown_AndIsolatesRecommendation()
        {
            var raw = "# Release Decision\n**Overall risk** is high with `jplist.min.js` empty. " +
                      "RECOMMENDATION: GO WITH CAUTION — fix web files first.";
            var clean = SummaryFormatting.ToPlainText(raw);

            Assert.DoesNotContain("#", clean);
            Assert.DoesNotContain("**", clean);
            Assert.DoesNotContain("`", clean);
            Assert.Contains("\n\nRECOMMENDATION: GO WITH CAUTION", clean); // own line, blank line before
        }

        // TC-DG-SUM-05: offline generator lists Medium+ findings as top risks.
        [Fact]
        public void Offline_ListsTopRisks()
        {
            var s = new TemplatedSummaryGenerator().Generate(
                Result(OverallRisk.High, 45, "PROD", F(Severity.Critical, "Attribute type mismatch"), F(Severity.Low, "minor")),
                null, null);

            Assert.Contains("Top risks:", s.Text);
            Assert.Contains("Attribute type mismatch", s.Text);
            Assert.DoesNotContain("minor", s.Text); // Low is below the Medium+ cutoff
        }
    }
}
