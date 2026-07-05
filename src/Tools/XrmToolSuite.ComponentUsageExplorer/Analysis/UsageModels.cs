using System;
using System.Collections.Generic;
using System.Linq;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.ComponentUsageExplorer.Analysis
{
    /// <summary>
    /// A single Dataverse solution component in identity-only form: its component type (numeric code +
    /// friendly label), object id, display/schema name, managed state, and the solution(s) that ship it.
    /// SDK-free (no <c>Microsoft.Xrm.Sdk</c>) so the "where-used" model and verdict rules stay unit-testable
    /// without a live org. The SDK collector maps platform rows into these; the rules never touch Dataverse.
    /// </summary>
    public class ComponentRef
    {
        /// <summary>Numeric solutioncomponent type code (1 = Entity/table, 29 = Workflow/Flow, 90 = Plugin Type, …).</summary>
        public int ComponentType { get; set; }

        /// <summary>Friendly component-type label ("Entity", "Form", "Plugin Type", …).</summary>
        public string ComponentTypeName { get; set; }

        /// <summary>The component's object id (the primary key of its own table row).</summary>
        public Guid ObjectId { get; set; }

        /// <summary>Display name (or best-effort friendly name) of the component.</summary>
        public string Name { get; set; }

        /// <summary>Schema/logical name where one exists (tables, columns, web resources).</summary>
        public string SchemaName { get; set; }

        /// <summary>True if the component lives in a managed solution layer.</summary>
        public bool IsManaged { get; set; }

        /// <summary>Unique names of the solution(s) that contain this component.</summary>
        public List<string> OwningSolutions { get; set; } = new List<string>();

        /// <summary>Best label for display: Name, else SchemaName, else the object id.</summary>
        public string Label =>
            !string.IsNullOrWhiteSpace(Name) ? Name
            : !string.IsNullOrWhiteSpace(SchemaName) ? SchemaName
            : ObjectId.ToString();
    }

    /// <summary>
    /// The full "where used" footprint of one selected component: the components it <em>requires</em>, the
    /// components that <em>depend on</em> it, and a per-type usage tally. Populated by the SDK collector from
    /// the platform dependency APIs; consumed by <see cref="UsageVerdictRules"/>. SDK-free.
    /// </summary>
    public class UsageFootprint
    {
        /// <summary>The component the footprint was built for.</summary>
        public ComponentRef Component { get; set; }

        /// <summary>Components the selected component needs to exist (upstream of it).</summary>
        public List<ComponentRef> RequiredComponents { get; set; } = new List<ComponentRef>();

        /// <summary>Components that reference / would break if the selected component changes (downstream).</summary>
        public List<ComponentRef> DependentComponents { get; set; } = new List<ComponentRef>();

        /// <summary>Dependent-component count grouped by friendly component-type name.</summary>
        public Dictionary<string, int> UsageByType { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// True when the platform dependency APIs could not fully answer for this component (unsupported
        /// type, Power Pages, or a query error). An incomplete answer must never read as "safe".
        /// </summary>
        public bool DependencyDataIncomplete { get; set; }

        /// <summary>Groups <see cref="DependentComponents"/> by friendly type name into a count map.</summary>
        public static Dictionary<string, int> BuildUsageByType(IEnumerable<ComponentRef> components)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in components ?? Enumerable.Empty<ComponentRef>())
            {
                var key = string.IsNullOrWhiteSpace(c?.ComponentTypeName) ? "(unknown)" : c.ComponentTypeName;
                map[key] = map.TryGetValue(key, out var n) ? n + 1 : 1;
            }
            return map;
        }
    }

    /// <summary>The single change-safety verdict a component's footprint resolves to.</summary>
    public enum ChangeSafety
    {
        /// <summary>No dependents found — the component can be changed with low risk.</summary>
        SafeToChange,

        /// <summary>A handful of dependents — review them before changing.</summary>
        ChangeWithCaution,

        /// <summary>Many or high-value dependents (forms, plugins, flows, apps) — a large blast radius.</summary>
        HighImpact,

        /// <summary>A table with many dependents (and typically data) — do not delete; changes are destructive.</summary>
        DoNotDelete,

        /// <summary>The platform could not fully enumerate dependencies — verify manually before acting.</summary>
        RequiresDependencyReview,

        /// <summary>Managed dependents or cross-solution usage — requires ALM sign-off before changing.</summary>
        RequiresAlmReview
    }

    /// <summary>Tunable thresholds for <see cref="UsageVerdictRules"/>. Defaults are conservative.</summary>
    public class UsageVerdictOptions
    {
        /// <summary>At or above this dependent count (with no higher-severity trigger) the verdict is HighImpact.</summary>
        public int HighImpactDependentThreshold { get; set; } = 8;

        /// <summary>A table (Entity component) with at least this many dependents is DoNotDelete.</summary>
        public int DoNotDeleteDependentThreshold { get; set; } = 3;

        public static UsageVerdictOptions Default => new UsageVerdictOptions();
    }

    /// <summary>
    /// The result of evaluating a <see cref="UsageFootprint"/>: the change-safety verdict, a banded 0–100
    /// impact score, the findings behind it, headline metric rows, and a plain-language explanation.
    /// SDK-free and fully unit-testable.
    /// </summary>
    public class UsageReport
    {
        public ChangeSafety Verdict { get; set; }

        /// <summary>Labeled-heuristic 0–100 impact score derived from the dependent findings.</summary>
        public int Score { get; set; }

        public ScoreBand Band { get; set; }

        public List<Finding> Findings { get; set; } = new List<Finding>();

        public List<MetricRow> Metrics { get; set; } = new List<MetricRow>();

        /// <summary>Plain-language "what breaks if I touch this" summary and required review steps.</summary>
        public string Explanation { get; set; }

        /// <summary>Short human label for the verdict (used in banners and exports).</summary>
        public string VerdictLabel => VerdictText(Verdict);

        public static string VerdictText(ChangeSafety v)
        {
            switch (v)
            {
                case ChangeSafety.SafeToChange: return "Safe to change";
                case ChangeSafety.ChangeWithCaution: return "Change with caution";
                case ChangeSafety.HighImpact: return "High impact";
                case ChangeSafety.DoNotDelete: return "Do not delete";
                case ChangeSafety.RequiresDependencyReview: return "Requires dependency review";
                case ChangeSafety.RequiresAlmReview: return "Requires ALM review";
                default: return v.ToString();
            }
        }
    }
}
