using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.Core.Analysis
{
    /// <summary>
    /// Converts a set of <see cref="Finding"/>s into a 0–100 score and a Low/Medium/High band.
    /// Weights and thresholds are configurable per tool (deployment risk and technical debt weight
    /// severities differently), with a shared <see cref="RiskDefault"/> matching the Deployment Risk
    /// Analyzer's original constants. Pure and SDK-free, so it is fully unit-testable.
    /// </summary>
    public sealed class ScoreCalculator
    {
        private readonly IReadOnlyDictionary<Severity, int> _weights;
        private readonly int _mediumThreshold;
        private readonly int _highThreshold;
        private readonly bool _criticalForcesHigh;
        private readonly int _cap;

        public ScoreCalculator(
            IReadOnlyDictionary<Severity, int> weights,
            int mediumThreshold,
            int highThreshold,
            bool criticalForcesHigh = true,
            int cap = 100)
        {
            _weights = weights ?? throw new ArgumentNullException(nameof(weights));
            _mediumThreshold = mediumThreshold;
            _highThreshold = highThreshold;
            _criticalForcesHigh = criticalForcesHigh;
            _cap = cap;
        }

        /// <summary>Default weighting shared by the risk/debt family: Critical=25, High=12, Medium=5, Low=2,
        /// bands at 15 (Medium) / 40 (High), and any Critical finding forces a High band.</summary>
        public static readonly ScoreCalculator RiskDefault = new ScoreCalculator(
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
            criticalForcesHigh: true);

        public int WeightOf(Severity s) => _weights.TryGetValue(s, out var w) ? w : 0;

        /// <summary>Weighted, capped score for the findings.</summary>
        public int Score(IEnumerable<Finding> findings) =>
            Math.Min(_cap, (findings ?? Enumerable.Empty<Finding>()).Sum(f => WeightOf(f.Severity)));

        /// <summary>Bands a score, honouring the "any Critical ⇒ High" rule when configured.</summary>
        public ScoreBand Band(IEnumerable<Finding> findings, int score)
        {
            if (_criticalForcesHigh && (findings ?? Enumerable.Empty<Finding>()).Any(f => f.Severity == Severity.Critical))
                return ScoreBand.High;
            return BandFor(score, _mediumThreshold, _highThreshold);
        }

        /// <summary>Bands a raw score against explicit thresholds (no findings context).</summary>
        public static ScoreBand BandFor(int score, int mediumThreshold, int highThreshold)
        {
            if (score >= highThreshold) return ScoreBand.High;
            if (score >= mediumThreshold) return ScoreBand.Medium;
            return ScoreBand.Low;
        }

        /// <summary>One-line explanation of the severity mix and resulting score/band.</summary>
        public static string Explain(IEnumerable<Finding> findings, int score, ScoreBand band, string scoreWord = "risk")
        {
            var f = (findings ?? Enumerable.Empty<Finding>()).ToList();
            int C(Severity s) => f.Count(x => x.Severity == s);
            return $"{C(Severity.Critical)} critical, {C(Severity.High)} high, {C(Severity.Medium)} medium, " +
                   $"{C(Severity.Low)} low, {C(Severity.Info)} informational → score {score}/100 ({band} {scoreWord}).";
        }
    }
}
