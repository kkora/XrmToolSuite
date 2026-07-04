using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using CoreScore = XrmToolSuite.Core.Analysis.ScoreCalculator;
using CoreFinding = XrmToolSuite.Core.Analysis.Finding;
using CoreSeverity = XrmToolSuite.Core.Analysis.Severity;
using CoreBand = XrmToolSuite.Core.Analysis.ScoreBand;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Scoring
{
    /// <summary>
    /// Deployment-risk scoring facade over the suite-shared <see cref="CoreScore"/>. Kept as a thin
    /// wrapper on <see cref="AnalysisResult"/> so the control and tests keep their existing API while
    /// the weighting/banding logic lives in exactly one place (the shared core, using its default
    /// Critical=25 / High=12 / Medium=5 / Low=2 weights, bands at 15/40, any Critical ⇒ High).
    /// </summary>
    public static class RiskScoreCalculator
    {
        public const int MediumThreshold = 15;
        public const int HighThreshold = 40;

        public static void Apply(AnalysisResult result)
        {
            var mapped = Map(result);
            result.Score = CoreScore.RiskDefault.Score(mapped);
            result.Risk = (OverallRisk)(int)CoreScore.RiskDefault.Band(mapped, result.Score);
        }

        public static string Explain(AnalysisResult r) =>
            CoreScore.Explain(Map(r), r.Score, (CoreBand)(int)r.Risk, "risk");

        // Findings only need their severity for scoring; project onto the shared Finding type.
        private static System.Collections.Generic.IEnumerable<CoreFinding> Map(AnalysisResult r) =>
            r.Findings.Select(f => new CoreFinding { Severity = (CoreSeverity)(int)f.Severity }).ToList();
    }
}
