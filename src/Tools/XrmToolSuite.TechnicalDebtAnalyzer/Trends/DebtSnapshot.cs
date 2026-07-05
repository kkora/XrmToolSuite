using System;
using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.TechnicalDebtAnalyzer.Trends
{
    /// <summary>
    /// One technical-debt scan reduced to the numbers a trend needs. Plain, SDK-free POCO so the trend
    /// list/analytics logic is unit-testable without a connection. Persisted as JSON history per machine
    /// (Dataverse doesn't retain past scores), keyed by environment.
    /// </summary>
    public sealed class DebtSnapshot
    {
        public DateTime TimestampUtc { get; set; }
        public string EnvironmentName { get; set; }
        public int Score { get; set; }
        public ScoreBand Band { get; set; }
        public int TotalFindings { get; set; }

        /// <summary>Finding count per category at this run (drives per-category trend deltas).</summary>
        public Dictionary<string, int> CategoryCounts { get; set; } = new Dictionary<string, int>();
    }
}
