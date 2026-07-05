using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.SharingAnalyzer.Analysis
{
    /// <summary>
    /// One <c>principalobjectaccess</c> row: a single record shared with a single principal at a decoded
    /// access level. SDK-free (Guid/string only) so the aggregations and risk rules stay unit-testable
    /// without a live org. The collector maps Dataverse rows into these.
    /// </summary>
    public class SharedRecordAccess
    {
        /// <summary>Logical name of the shared record's table (e.g. "account").</summary>
        public string Table { get; set; }

        /// <summary>Id of the shared record.</summary>
        public Guid ObjectId { get; set; }

        /// <summary>Id of the principal the record is shared with.</summary>
        public Guid PrincipalId { get; set; }

        public string PrincipalName { get; set; }

        /// <summary>"User" or "Team".</summary>
        public string PrincipalType { get; set; }

        /// <summary>False when the user is disabled or the team is empty/disabled.</summary>
        public bool PrincipalActive { get; set; } = true;

        /// <summary>Decoded <c>accessrightsmask</c>.</summary>
        public int AccessMask { get; set; }
    }

    /// <summary>Aggregate view of one record's inbound shares.</summary>
    public sealed class RecordShareStat
    {
        public string Table { get; set; }
        public Guid ObjectId { get; set; }
        /// <summary>Distinct principals the record is shared with.</summary>
        public int DistinctPrincipals { get; set; }
        /// <summary>Total POA rows for the record (a principal may appear once).</summary>
        public int ShareCount { get; set; }
        /// <summary>OR of every access mask granted on the record.</summary>
        public int CombinedMask { get; set; }
    }

    /// <summary>Aggregate view of one principal's inbound shared access.</summary>
    public sealed class PrincipalShareStat
    {
        public Guid PrincipalId { get; set; }
        public string PrincipalName { get; set; }
        public string PrincipalType { get; set; }
        public bool PrincipalActive { get; set; } = true;
        /// <summary>Distinct records shared to the principal.</summary>
        public int InboundRecords { get; set; }
        /// <summary>Total POA rows referencing the principal.</summary>
        public int InboundShares { get; set; }
    }

    /// <summary>One cell of the table x principal intensity summary.</summary>
    public sealed class IntensityCell
    {
        public string Table { get; set; }
        public string PrincipalName { get; set; }
        public string PrincipalType { get; set; }
        public int Shares { get; set; }
    }

    /// <summary>
    /// A collected set of shares plus pure aggregations over them: shares-per-record,
    /// shares-per-principal, distinct principals per record, and inbound access per principal.
    /// Deterministic and SDK-free.
    /// </summary>
    public class SharingSummary
    {
        public List<SharedRecordAccess> Shares { get; set; } = new List<SharedRecordAccess>();

        /// <summary>Tables that were scanned (so an empty result is distinguishable from "not scanned").</summary>
        public List<string> ScannedTables { get; set; } = new List<string>();

        /// <summary>
        /// Informational findings the collector raises when a per-table read degrades (never throws).
        /// Merged with the risk findings by the UI; kept off the pure aggregations.
        /// </summary>
        public List<Finding> CollectionNotes { get; set; } = new List<Finding>();

        // ---- record aggregations ----

        /// <summary>Per-record share stats, ordered by distinct-principal count (highest first).</summary>
        public IReadOnlyList<RecordShareStat> RecordStats()
        {
            return (Shares ?? new List<SharedRecordAccess>())
                .Where(s => s != null)
                .GroupBy(s => new { s.Table, s.ObjectId })
                .Select(g => new RecordShareStat
                {
                    Table = g.Key.Table,
                    ObjectId = g.Key.ObjectId,
                    DistinctPrincipals = g.Select(x => x.PrincipalId).Distinct().Count(),
                    ShareCount = g.Count(),
                    CombinedMask = g.Aggregate(0, (m, x) => m | x.AccessMask)
                })
                .OrderByDescending(r => r.DistinctPrincipals)
                .ThenByDescending(r => r.ShareCount)
                .ToList();
        }

        /// <summary>Table logical name -&gt; distinct principals shared with across the table's records.</summary>
        public Dictionary<string, int> DistinctPrincipalsPerRecordByTable()
        {
            return (Shares ?? new List<SharedRecordAccess>())
                .Where(s => s != null)
                .GroupBy(s => s.Table)
                .ToDictionary(g => g.Key ?? "(unknown)",
                    g => g.Select(x => x.PrincipalId).Distinct().Count());
        }

        // ---- principal aggregations ----

        /// <summary>Per-principal inbound share stats, ordered by inbound-record count (highest first).</summary>
        public IReadOnlyList<PrincipalShareStat> PrincipalStats()
        {
            return (Shares ?? new List<SharedRecordAccess>())
                .Where(s => s != null)
                .GroupBy(s => s.PrincipalId)
                .Select(g =>
                {
                    var first = g.First();
                    return new PrincipalShareStat
                    {
                        PrincipalId = g.Key,
                        PrincipalName = first.PrincipalName,
                        PrincipalType = first.PrincipalType,
                        // A principal is inactive if any row marks it so (collector sets it per-principal).
                        PrincipalActive = g.All(x => x.PrincipalActive),
                        InboundRecords = g.Select(x => x.ObjectId).Distinct().Count(),
                        InboundShares = g.Count()
                    };
                })
                .OrderByDescending(p => p.InboundRecords)
                .ThenByDescending(p => p.InboundShares)
                .ToList();
        }

        // ---- heat / intensity ----

        /// <summary>
        /// table x principal share counts, ordered by intensity (highest first). Intended for a compact
        /// heat summary; callers cap the rows they render.
        /// </summary>
        public IReadOnlyList<IntensityCell> Intensity()
        {
            return (Shares ?? new List<SharedRecordAccess>())
                .Where(s => s != null)
                .GroupBy(s => new { s.Table, s.PrincipalId, s.PrincipalName, s.PrincipalType })
                .Select(g => new IntensityCell
                {
                    Table = g.Key.Table,
                    PrincipalName = g.Key.PrincipalName,
                    PrincipalType = g.Key.PrincipalType,
                    Shares = g.Count()
                })
                .OrderByDescending(c => c.Shares)
                .ThenBy(c => c.Table, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ---- scalar rollups ----

        public int TotalShares => Shares?.Count ?? 0;
        public int DistinctRecords =>
            (Shares ?? new List<SharedRecordAccess>()).Where(s => s != null).Select(s => s.ObjectId).Distinct().Count();
        public int DistinctPrincipals =>
            (Shares ?? new List<SharedRecordAccess>()).Where(s => s != null).Select(s => s.PrincipalId).Distinct().Count();
    }

    /// <summary>Tunable thresholds for <see cref="SharingRiskRules"/>. Defaults are conservative.</summary>
    public class SharingRiskOptions
    {
        /// <summary>Excessive sharing (High) trips when a record is shared with more principals than this.</summary>
        public int MaxPrincipalsPerRecord { get; set; } = 25;

        /// <summary>Inbound-sprawl (Medium) trips when a principal has more inbound shared records than this.</summary>
        public int MaxInboundPerPrincipal { get; set; } = 500;

        /// <summary>A record must reach at least this many principals to be considered for the statistical-outlier rule.</summary>
        public int OutlierFloor { get; set; } = 5;

        /// <summary>Standard deviations above the mean at which a record's principal count is an outlier.</summary>
        public double OutlierSigma { get; set; } = 2.0;
    }
}
