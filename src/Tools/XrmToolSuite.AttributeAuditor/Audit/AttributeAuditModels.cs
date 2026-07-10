using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.AttributeAuditor.Audit
{
    /// <summary>
    /// The usage signals an audit looks for. A column with no signals is a retirement candidate.
    /// SDK-free so the whole classification model is unit-testable without a connection.
    /// </summary>
    public enum UsageSignal
    {
        Form,          // present on a system form (formxml datafieldname)
        View,          // used by a saved query (fetchxml attribute/condition/order, or layout cell)
        Process,       // referenced by a workflow / business rule / cloud flow
        FieldSecurity, // protected by a field security profile (IsSecured)
    }

    /// <summary>One column (attribute) and the evidence gathered for it.</summary>
    public sealed class ColumnAudit
    {
        public string Table { get; set; }            // entity logical name
        public string TableDisplay { get; set; }
        public string LogicalName { get; set; }
        public string DisplayName { get; set; }
        public string AttributeType { get; set; }
        public bool IsCustom { get; set; }
        public bool IsManaged { get; set; }

        /// <summary>Distinct usage signals that fired, each with human-readable evidence.</summary>
        public List<Evidence> Evidence { get; } = new List<Evidence>();

        public bool IsUsed => Evidence.Count > 0;

        /// <summary>
        /// A retirement candidate is a custom, unmanaged column with no usage evidence. Managed and
        /// system columns are never candidates (they cannot be deleted / are not the customer's to retire).
        /// </summary>
        public bool IsRetirementCandidate => IsCustom && !IsManaged && !IsUsed;

        public IEnumerable<UsageSignal> Signals => Evidence.Select(e => e.Signal).Distinct();

        public string UsageSummary() =>
            IsUsed ? string.Join("; ", Evidence.Select(e => e.Detail)) : "No usage signals";

        public void Add(UsageSignal signal, string detail)
        {
            // De-dupe identical evidence (e.g. the same form counted twice).
            if (!Evidence.Any(e => e.Signal == signal && e.Detail == detail))
                Evidence.Add(new Evidence(signal, detail));
        }
    }

    /// <summary>A single piece of usage evidence: which signal, and a human-readable detail.</summary>
    public sealed class Evidence
    {
        public UsageSignal Signal { get; }
        public string Detail { get; }
        public Evidence(UsageSignal signal, string detail) { Signal = signal; Detail = detail; }
    }

    /// <summary>The full audit: every audited column plus roll-up counts.</summary>
    public sealed class AuditResult
    {
        public string EnvironmentName { get; set; }
        public DateTime AuditedOnUtc { get; set; } = DateTime.UtcNow;
        public List<ColumnAudit> Columns { get; } = new List<ColumnAudit>();

        /// <summary>Total tables in the environment (excluding N:N intersect tables).</summary>
        public int TotalTables { get; set; }

        /// <summary>Tables that are not custom (system tables) — a subset of <see cref="TotalTables"/>.</summary>
        public int NonCustomTables { get; set; }

        /// <summary>Distinct tables represented in the audited columns.</summary>
        public int AuditedTables =>
            Columns.Select(c => c.Table).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        public int TotalColumns => Columns.Count;
        public int UsedColumns => Columns.Count(c => c.IsUsed);
        public int CandidateColumns => Columns.Count(c => c.IsRetirementCandidate);

        public IEnumerable<ColumnAudit> Candidates => Columns.Where(c => c.IsRetirementCandidate);

        /// <summary>Retirement candidates grouped by table, most-candidates-first.</summary>
        public IEnumerable<IGrouping<string, ColumnAudit>> CandidatesByTable() =>
            Candidates.GroupBy(c => c.Table).OrderByDescending(g => g.Count());
    }
}
