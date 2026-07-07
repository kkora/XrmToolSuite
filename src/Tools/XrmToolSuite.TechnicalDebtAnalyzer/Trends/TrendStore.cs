using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Trends
{
    /// <summary>
    /// Pure, SDK-free operations over the in-memory snapshot history: append with a per-environment cap and
    /// same-run de-duplication, and per-environment selection ordered oldest→newest. JSON serialization and
    /// file I/O live in <c>TrendHistoryFile</c> (Newtonsoft + disk) so this logic stays fully unit-testable.
    /// </summary>
    public static class TrendStore
    {
        public const int DefaultCapPerEnvironment = 100;

        /// <summary>
        /// Append a snapshot to <paramref name="history"/> in place. A snapshot with the same environment and
        /// the exact same <see cref="DebtSnapshot.TimestampUtc"/> as an existing one is ignored (idempotent
        /// re-append guard). After adding, the environment's snapshots are trimmed to the most recent
        /// <paramref name="capPerEnvironment"/> by timestamp. Returns true if the snapshot was added.
        /// </summary>
        public static bool Append(List<DebtSnapshot> history, DebtSnapshot snapshot,
            int capPerEnvironment = DefaultCapPerEnvironment)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            bool duplicate = history.Any(s =>
                SameEnvironment(s.EnvironmentName, snapshot.EnvironmentName) &&
                s.TimestampUtc == snapshot.TimestampUtc);
            if (duplicate) return false;

            history.Add(snapshot);

            if (capPerEnvironment > 0)
            {
                var forEnv = history
                    .Where(s => SameEnvironment(s.EnvironmentName, snapshot.EnvironmentName))
                    .OrderByDescending(s => s.TimestampUtc)
                    .ToList();
                foreach (var stale in forEnv.Skip(capPerEnvironment))
                    history.Remove(stale);
            }
            return true;
        }

        /// <summary>This environment's snapshots, oldest first (the order a trend chart plots).</summary>
        public static IReadOnlyList<DebtSnapshot> ForEnvironment(IEnumerable<DebtSnapshot> history, string environment)
        {
            if (history == null) return new List<DebtSnapshot>();
            return history
                .Where(s => SameEnvironment(s.EnvironmentName, environment))
                .OrderBy(s => s.TimestampUtc)
                .ToList();
        }

        /// <summary>Distinct environment names present in the history (for a picker).</summary>
        public static IReadOnlyList<string> Environments(IEnumerable<DebtSnapshot> history) =>
            (history ?? Enumerable.Empty<DebtSnapshot>())
                .Select(s => s.EnvironmentName ?? string.Empty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static bool SameEnvironment(string a, string b) =>
            string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
