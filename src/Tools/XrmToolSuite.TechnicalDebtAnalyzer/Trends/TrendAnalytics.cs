using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Trends
{
    /// <summary>The change between the two most recent runs for one environment.</summary>
    public sealed class TrendDelta
    {
        /// <summary>Score change (current − previous). Negative = debt fell = improving.</summary>
        public int ScoreChange { get; set; }
        public TrendDirection Direction { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        /// <summary>Per-category change (current − previous); categories present in either run.</summary>
        public Dictionary<string, int> CategoryChanges { get; } = new Dictionary<string, int>();
    }

    /// <summary>Lower debt score is better, so a falling score is "Improving".</summary>
    public enum TrendDirection { Flat, Improving, Worsening }

    /// <summary>
    /// SDK-free analytics over an environment's ordered snapshot history (oldest→newest, as produced by
    /// <see cref="TrendStore.ForEnvironment"/>): run-over-run delta, direction, the score series, and
    /// best/worst runs. Pure functions — fully unit-testable.
    /// </summary>
    public static class TrendAnalytics
    {
        /// <summary>
        /// Delta between the last two snapshots, or null if there are fewer than two. Assumes the input is a
        /// single environment ordered oldest→newest.
        /// </summary>
        public static TrendDelta SincePrevious(IReadOnlyList<DebtSnapshot> ordered)
        {
            if (ordered == null || ordered.Count < 2) return null;
            var prev = ordered[ordered.Count - 2];
            var curr = ordered[ordered.Count - 1];

            var delta = new TrendDelta
            {
                ScoreChange = curr.Score - prev.Score,
                FromUtc = prev.TimestampUtc,
                ToUtc = curr.TimestampUtc,
                Direction = curr.Score < prev.Score ? TrendDirection.Improving
                          : curr.Score > prev.Score ? TrendDirection.Worsening
                          : TrendDirection.Flat,
            };

            var categories = (prev.CategoryCounts?.Keys ?? Enumerable.Empty<string>())
                .Concat(curr.CategoryCounts?.Keys ?? Enumerable.Empty<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase);
            foreach (var cat in categories)
            {
                int p = Get(prev.CategoryCounts, cat);
                int c = Get(curr.CategoryCounts, cat);
                if (c - p != 0) delta.CategoryChanges[cat] = c - p;
            }
            return delta;
        }

        /// <summary>The (timestamp, score) series for charting, oldest→newest.</summary>
        public static IReadOnlyList<KeyValuePair<DateTime, int>> ScoreSeries(IReadOnlyList<DebtSnapshot> ordered) =>
            (ordered ?? new List<DebtSnapshot>())
                .Select(s => new KeyValuePair<DateTime, int>(s.TimestampUtc, s.Score))
                .ToList();

        public static DebtSnapshot Best(IReadOnlyList<DebtSnapshot> ordered) =>   // lowest debt
            ordered == null || ordered.Count == 0 ? null
            : ordered.Aggregate((a, b) => b.Score < a.Score ? b : a);

        public static DebtSnapshot Worst(IReadOnlyList<DebtSnapshot> ordered) =>  // highest debt
            ordered == null || ordered.Count == 0 ? null
            : ordered.Aggregate((a, b) => b.Score > a.Score ? b : a);

        private static int Get(Dictionary<string, int> d, string key)
        {
            if (d == null) return 0;
            return d.TryGetValue(key, out var v) ? v : 0;
        }
    }
}
