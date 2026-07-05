using System;
using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.EnvironmentComparisonSuite.Analysis
{
    /// <summary>
    /// How a single component differs between the source and target environment. Integer order is not
    /// meaningful (unlike <see cref="Severity"/>); these are mutually-exclusive classifications.
    /// </summary>
    public enum DiffClass
    {
        /// <summary>Present in source but absent from target (a release would need to add it).</summary>
        Missing,

        /// <summary>Present in target but absent from source (drift the source doesn't account for).</summary>
        Extra,

        /// <summary>Present in both but one or more compared properties differ.</summary>
        Changed,

        /// <summary>Present in both but the managed/unmanaged layering differs (import-blocking risk).</summary>
        ManagedVsUnmanaged,

        /// <summary>Present in both and every compared property matches.</summary>
        Identical
    }

    /// <summary>
    /// A normalized, comparable snapshot of ANY Dataverse component class (solution, table, column,
    /// role, plugin step, env var, web resource, …). The collector projects each raw record onto this
    /// shape from BOTH environments; <see cref="SnapshotComparer"/> then diffs source vs target purely
    /// on these fields. SDK-free (BCL only) so the diff engine stays unit-testable and reusable (a
    /// future ADMIN8 Configuration Drift Monitor consumes the same engine).
    /// </summary>
    public sealed class ComponentSnapshot
    {
        /// <summary>Component class this snapshot belongs to (e.g. "Tables"); groups the diff output.</summary>
        public string Category { get; set; }

        /// <summary>
        /// Stable identity used to MATCH a source snapshot to a target snapshot — a schema/unique name
        /// (e.g. table logical name, solution unique name, "entity.attribute", plugin message+entity+stage).
        /// Matching is case-insensitive.
        /// </summary>
        public string Key { get; set; }

        /// <summary>Human-friendly display name shown in the grid (may equal <see cref="Key"/>).</summary>
        public string Name { get; set; }

        /// <summary>Managed/unmanaged layering flag; a mismatch is classified <see cref="DiffClass.ManagedVsUnmanaged"/>.</summary>
        public bool IsManaged { get; set; }

        /// <summary>Version string where the component carries one (solutions/publishers); null otherwise.</summary>
        public string Version { get; set; }

        /// <summary>The compared properties (datatype, required level, cascade, content hash, …). Ordinal compare.</summary>
        public Dictionary<string, string> Properties { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Property keys whose VALUES are secret-typed and must never be shown. The underlying values are
        /// still compared to detect a change, but they are masked in every diff output and export.
        /// </summary>
        public HashSet<string> SecretKeys { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ComponentSnapshot() { }

        public ComponentSnapshot(string category, string key, string name, bool isManaged = false, string version = null)
        {
            Category = category;
            Key = key;
            Name = name;
            IsManaged = isManaged;
            Version = version;
        }

        /// <summary>Fluent helper: add a compared property (optionally secret-typed).</summary>
        public ComponentSnapshot With(string prop, string value, bool secret = false)
        {
            Properties[prop] = value;
            if (secret) SecretKeys.Add(prop);
            return this;
        }
    }

    /// <summary>A single per-property difference: property name plus the source and target values.</summary>
    public struct ChangedProperty
    {
        public string Prop { get; }
        public string Source { get; }
        public string Target { get; }

        public ChangedProperty(string prop, string source, string target)
        {
            Prop = prop; Source = source; Target = target;
        }
    }

    /// <summary>One component's classified difference between source and target.</summary>
    public sealed class ComponentDiff
    {
        public string Category { get; set; }
        public string Name { get; set; }

        /// <summary>Stable identity (mirrors the matched snapshots' <see cref="ComponentSnapshot.Key"/>).</summary>
        public string Key { get; set; }

        public DiffClass Class { get; set; }

        /// <summary>Per-property before/after (source vs target) for a <see cref="DiffClass.Changed"/> diff. Secrets masked.</summary>
        public List<ChangedProperty> ChangedProperties { get; } = new List<ChangedProperty>();

        public Severity Severity { get; set; }

        /// <summary>Managed state on each side (used by the detail viewer and managed/unmanaged diffs).</summary>
        public bool SourceManaged { get; set; }
        public bool TargetManaged { get; set; }
    }

    /// <summary>
    /// The rolled-up result of comparing one or more categories: every classified diff, a weighted
    /// difference score + band, exporter-ready findings/metrics, and a per-category × class count matrix
    /// for the summary cards. SDK-free.
    /// </summary>
    public sealed class ComparisonReport
    {
        public List<ComponentDiff> Diffs { get; } = new List<ComponentDiff>();
        public int Score { get; set; }
        public ScoreBand Band { get; set; }
        public List<Finding> Findings { get; } = new List<Finding>();
        public List<MetricRow> Metrics { get; } = new List<MetricRow>();

        /// <summary>category → (DiffClass → count). Every enabled category appears even when all-identical.</summary>
        public Dictionary<string, Dictionary<DiffClass, int>> CountsByCategoryAndClass { get; } =
            new Dictionary<string, Dictionary<DiffClass, int>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Options controlling a comparison run. Deterministic and pure — no I/O. A caller may supply extra
    /// secret-property keys (applied on top of any a snapshot already marks) and/or override the default
    /// severity table.
    /// </summary>
    public sealed class CompareOptions
    {
        /// <summary>The masked placeholder shown in place of any secret-typed value.</summary>
        public const string Mask = "••••••"; // ••••••

        /// <summary>Additional property keys to treat as secret across every snapshot (case-insensitive).</summary>
        public HashSet<string> SecretProperties { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Optional severity override. Returns null to fall back to the built-in default table.</summary>
        public Func<string, DiffClass, Severity?> SeverityResolver { get; set; }

        public static CompareOptions Default => new CompareOptions();
    }
}
