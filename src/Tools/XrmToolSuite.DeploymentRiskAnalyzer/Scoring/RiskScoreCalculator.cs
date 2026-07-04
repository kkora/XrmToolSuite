using System;
using System.Linq;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Scoring
{
    /// <summary>
    /// Converts a set of findings into a 0–100 score and a Low/Medium/High banding.
    /// Weights are intentionally simple and tunable.
    /// </summary>
    public static class RiskScoreCalculator
    {
        public const int WeightCritical = 25;
        public const int WeightHigh = 12;
        public const int WeightMedium = 5;
        public const int WeightLow = 2;

        public const int MediumThreshold = 15;
        public const int HighThreshold = 40;

        public static void Apply(AnalysisResult result)
        {
            int raw = result.Findings.Sum(f => Weight(f.Severity));
            result.Score = Math.Min(100, raw);

            // Any Critical finding forces High risk regardless of score.
            if (result.Findings.Any(f => f.Severity == Severity.Critical) || result.Score >= HighThreshold)
                result.Risk = OverallRisk.High;
            else if (result.Score >= MediumThreshold)
                result.Risk = OverallRisk.Medium;
            else
                result.Risk = OverallRisk.Low;
        }

        private static int Weight(Severity s)
        {
            switch (s)
            {
                case Severity.Critical: return WeightCritical;
                case Severity.High: return WeightHigh;
                case Severity.Medium: return WeightMedium;
                case Severity.Low: return WeightLow;
                default: return 0;
            }
        }

        public static string Explain(AnalysisResult r) =>
            $"{r.CountBySeverity(Severity.Critical)} critical, {r.CountBySeverity(Severity.High)} high, " +
            $"{r.CountBySeverity(Severity.Medium)} medium, {r.CountBySeverity(Severity.Low)} low, " +
            $"{r.CountBySeverity(Severity.Info)} informational → score {r.Score}/100 ({r.Risk} risk).";
    }
}
