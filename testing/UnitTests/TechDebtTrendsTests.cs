using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.TechnicalDebtAnalyzer.Trends;

namespace XrmToolSuite.UnitTests
{
    /// <summary>
    /// SDK-free tests for the RPT4 Technical Debt Trends logic: the snapshot store (append / per-env cap /
    /// same-run dedupe / per-env selection) and the analytics (run-over-run delta, direction, series,
    /// best/worst). The JSON+file persistence layer (Newtonsoft/disk) is manual-tested. Traces to US-TD-8.
    /// </summary>
    public class TechDebtTrendsTests
    {
        private static DebtSnapshot Snap(string env, int daysAgoDescending, int score,
            ScoreBand band = ScoreBand.Medium, Dictionary<string, int> cats = null) =>
            new DebtSnapshot
            {
                EnvironmentName = env,
                // Fixed base date (Date.Now is unavailable/undesirable in tests); vary by index.
                TimestampUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(daysAgoDescending),
                Score = score,
                Band = band,
                TotalFindings = score,
                CategoryCounts = cats ?? new Dictionary<string, int>(),
            };

        // ---- store (US-TD-8) ----

        [Fact]
        public void Append_AddsAndSelectsPerEnvironment_Ordered()
        {
            var h = new List<DebtSnapshot>();
            TrendStore.Append(h, Snap("DEV", 2, 40));
            TrendStore.Append(h, Snap("PROD", 1, 10));
            TrendStore.Append(h, Snap("DEV", 1, 30));

            var dev = TrendStore.ForEnvironment(h, "DEV");
            Assert.Equal(2, dev.Count);
            Assert.True(dev[0].TimestampUtc < dev[1].TimestampUtc); // oldest first
            Assert.Single(TrendStore.ForEnvironment(h, "PROD"));
        }

        [Fact]
        public void Append_SameEnvAndTimestamp_IsIgnored()
        {
            var h = new List<DebtSnapshot>();
            Assert.True(TrendStore.Append(h, Snap("DEV", 1, 40)));
            Assert.False(TrendStore.Append(h, Snap("DEV", 1, 99))); // same env+timestamp -> ignored
            Assert.Single(h);
            Assert.Equal(40, h[0].Score);
        }

        [Fact]
        public void Append_CapsPerEnvironment_KeepingMostRecent()
        {
            var h = new List<DebtSnapshot>();
            for (int i = 0; i < 105; i++) TrendStore.Append(h, Snap("DEV", i, i), capPerEnvironment: 100);
            var dev = TrendStore.ForEnvironment(h, "DEV");
            Assert.Equal(100, dev.Count);
            Assert.Equal(5, dev.First().Score);   // oldest kept is day 5 (0..4 trimmed)
            Assert.Equal(104, dev.Last().Score);  // newest kept
        }

        [Fact]
        public void Cap_IsPerEnvironment_NotGlobal()
        {
            var h = new List<DebtSnapshot>();
            for (int i = 0; i < 3; i++) TrendStore.Append(h, Snap("DEV", i, i), capPerEnvironment: 2);
            for (int i = 0; i < 3; i++) TrendStore.Append(h, Snap("PROD", i, i), capPerEnvironment: 2);
            Assert.Equal(2, TrendStore.ForEnvironment(h, "DEV").Count);
            Assert.Equal(2, TrendStore.ForEnvironment(h, "PROD").Count);
        }

        // ---- analytics (US-TD-8) ----

        [Fact]
        public void SincePrevious_FewerThanTwo_IsNull()
        {
            Assert.Null(TrendAnalytics.SincePrevious(new List<DebtSnapshot>()));
            Assert.Null(TrendAnalytics.SincePrevious(new[] { Snap("DEV", 1, 40) }));
        }

        [Fact]
        public void SincePrevious_FallingScore_IsImproving_WithCategoryDeltas()
        {
            var ordered = new[]
            {
                Snap("DEV", 1, 40, cats: new Dictionary<string, int> { ["Unused"] = 8, ["Naming"] = 4 }),
                Snap("DEV", 2, 30, cats: new Dictionary<string, int> { ["Unused"] = 5, ["Naming"] = 4 }),
            };
            var d = TrendAnalytics.SincePrevious(ordered);
            Assert.Equal(-10, d.ScoreChange);
            Assert.Equal(TrendDirection.Improving, d.Direction);
            Assert.Equal(-3, d.CategoryChanges["Unused"]);
            Assert.False(d.CategoryChanges.ContainsKey("Naming")); // unchanged categories omitted
        }

        [Fact]
        public void SincePrevious_RisingScore_IsWorsening()
        {
            var ordered = new[] { Snap("DEV", 1, 20), Snap("DEV", 2, 35) };
            Assert.Equal(TrendDirection.Worsening, TrendAnalytics.SincePrevious(ordered).Direction);
        }

        [Fact]
        public void Series_And_BestWorst()
        {
            var ordered = new[] { Snap("DEV", 1, 40), Snap("DEV", 2, 22), Snap("DEV", 3, 55) };
            var series = TrendAnalytics.ScoreSeries(ordered);
            Assert.Equal(new[] { 40, 22, 55 }, series.Select(kv => kv.Value));
            Assert.Equal(22, TrendAnalytics.Best(ordered).Score);   // lowest debt
            Assert.Equal(55, TrendAnalytics.Worst(ordered).Score);  // highest debt
        }
    }
}
