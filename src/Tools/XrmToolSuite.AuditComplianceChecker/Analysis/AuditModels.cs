using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.AuditComplianceChecker.Analysis
{
    /// <summary>
    /// Org/table/column audit configuration for one environment. SDK-free (no Microsoft.Xrm.Sdk) so
    /// the compliance rules that consume it stay unit-testable without a live org. The
    /// <see cref="AuditCollector"/> populates it from Dataverse metadata.
    /// </summary>
    public class AuditCoverage
    {
        /// <summary>organization.isauditenabled — the master switch. When off, no table/column
        /// audit setting takes effect.</summary>
        public bool OrgAuditEnabled { get; set; }

        public List<TableAudit> Tables { get; set; } = new List<TableAudit>();

        /// <summary>Non-fatal notes from the collector (missing metadata, degraded reads).</summary>
        public List<string> Notes { get; set; } = new List<string>();
    }

    /// <summary>One table's audit configuration and (optionally) its column-level detail.</summary>
    public class TableAudit
    {
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }

        /// <summary>Part of a managed solution (vs. an unmanaged/custom table).</summary>
        public bool IsManaged { get; set; }

        /// <summary>EntityMetadata.IsAuditEnabled.</summary>
        public bool IsAuditEnabled { get; set; }

        /// <summary>Flagged sensitive by <see cref="SensitivityHeuristics.IsSensitiveTable"/>.</summary>
        public bool IsSensitive { get; set; }

        public List<ColumnAudit> Columns { get; set; } = new List<ColumnAudit>();
    }

    /// <summary>One column's audit configuration.</summary>
    public class ColumnAudit
    {
        public string LogicalName { get; set; }

        /// <summary>Attribute type name (e.g. "String", "Money", "Lookup").</summary>
        public string Type { get; set; }

        /// <summary>AttributeMetadata.IsAuditEnabled.</summary>
        public bool IsAuditEnabled { get; set; }

        /// <summary>Flagged sensitive by <see cref="SensitivityHeuristics.IsSensitiveColumn"/>.</summary>
        public bool IsSensitive { get; set; }
    }

    /// <summary>
    /// Aggregated audit activity over a date window. Tallies come from paged <c>audit</c> queries.
    /// SDK-free. Every value is derived deterministically from the underlying rows.
    /// </summary>
    public class AuditActivitySummary
    {
        public int TotalRecords { get; set; }

        /// <summary>objecttypecode -&gt; record count.</summary>
        public Dictionary<string, int> ByTable { get; set; } = new Dictionary<string, int>();

        /// <summary>user (name or id) -&gt; record count.</summary>
        public Dictionary<string, int> ByUser { get; set; } = new Dictionary<string, int>();

        /// <summary>yyyy-MM-dd (local) -&gt; record count. Powers the by-date view and the storage estimate.</summary>
        public Dictionary<string, int> ByDate { get; set; } = new Dictionary<string, int>();

        public int DeleteCount { get; set; }

        /// <summary>Role/privilege/team-membership related changes (heuristic — see collector).</summary>
        public int SecurityChangeCount { get; set; }

        /// <summary>Changes recorded outside the configured business hours (or on weekends).</summary>
        public int AfterHoursCount { get; set; }

        /// <summary>Start (UTC) of the analyzed window (informational).</summary>
        public System.DateTime FromUtc { get; set; }

        /// <summary>End (UTC) of the analyzed window (informational).</summary>
        public System.DateTime ToUtc { get; set; }

        /// <summary>Non-fatal notes from the collector.</summary>
        public List<string> Notes { get; set; } = new List<string>();

        /// <summary>
        /// ESTIMATED audit-log storage from record volume (records × ~2&#160;KB/record). This is an
        /// estimate derived from row counts — NOT billed/official Dataverse storage. Clearly labelled
        /// as such everywhere it is shown.
        /// </summary>
        public double EstimatedStorageMb =>
            System.Math.Round((TotalRecords * AuditComplianceRules.EstimatedKbPerAuditRecord) / 1024.0, 2);
    }

    /// <summary>Tunable inputs for <see cref="AuditComplianceRules.Evaluate"/>. Defaults are conservative.</summary>
    public class AuditComplianceOptions
    {
        /// <summary>A day's delete volume at/above this trips the "high delete volume" activity rule.</summary>
        public int HighDeleteVolumeThreshold { get; set; } = 100;
    }

    /// <summary>
    /// The compliance verdict: a 0–100 readiness score (HIGHER = MORE compliant), its band
    /// (High = good), the findings that explain it, and the headline metrics/category breakdown.
    /// </summary>
    public class AuditComplianceReport
    {
        public int Score { get; set; }
        public ScoreBand Band { get; set; }
        public List<Finding> Findings { get; set; } = new List<Finding>();
        public List<MetricRow> Metrics { get; set; } = new List<MetricRow>();
    }
}
