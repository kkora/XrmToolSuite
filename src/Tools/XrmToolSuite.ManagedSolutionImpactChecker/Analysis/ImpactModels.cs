using System;
using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.ManagedSolutionImpactChecker.Analysis
{
    /// <summary>
    /// The deployment operation being modelled. The delete/overwrite semantics differ per path and are
    /// surfaced explicitly by <see cref="LayerImpactRules"/>:
    /// <list type="bullet">
    /// <item><see cref="Upgrade"/> — replaces the managed base and DELETES components missing from the incoming solution (data loss).</item>
    /// <item><see cref="Update"/> — updates the managed base in place; does NOT delete missing components.</item>
    /// <item><see cref="Patch"/> — an additive delta on top of a base; does NOT delete missing components.</item>
    /// <item><see cref="Holding"/> — a staged upgrade (holding solution); deletions still apply once the upgrade is applied.</item>
    /// </list>
    /// </summary>
    public enum DeploymentPath
    {
        Upgrade,
        Update,
        Patch,
        Holding
    }

    /// <summary>
    /// One component's layering position: what it is, who owns its active layer, and the layering hazards
    /// the collector detected (an unmanaged layer sitting above the managed one, or restrictive managed
    /// properties). SDK-free (the id is a <see cref="Guid"/> from the BCL, not an SDK type) so the rules
    /// stay unit-testable off a live connection.
    /// </summary>
    public class ComponentLayer
    {
        /// <summary>Friendly component-type label (e.g. "Entity", "Attribute", "Web Resource").</summary>
        public string ComponentType { get; set; }

        /// <summary>Best-effort display/logical name of the component (falls back to the object id).</summary>
        public string Name { get; set; }

        /// <summary>Component object id.</summary>
        public Guid ObjectId { get; set; }

        /// <summary>The solution whose layer currently owns the component's active definition.</summary>
        public string OwningSolution { get; set; }

        /// <summary>True when the owning/active layer is a managed layer.</summary>
        public bool IsManaged { get; set; }

        /// <summary>True when an active <em>unmanaged</em> layer sits above the managed layer (an admin override).</summary>
        public bool HasUnmanagedLayerAbove { get; set; }

        /// <summary>True when managed properties restrict customization of this component post-import.</summary>
        public bool RestrictiveManagedProperties { get; set; }
    }

    /// <summary>
    /// The full, SDK-free input to <see cref="LayerImpactRules.Evaluate"/>: the component layers, the set
    /// of components that would be removed by the change (target-minus-source), missing dependencies, and
    /// the source/target publisher prefixes. Populated by the SDK collector; consumed by the pure rules.
    /// </summary>
    public class LayerAnalysisInput
    {
        /// <summary>Every component's layering position.</summary>
        public List<ComponentLayer> Layers { get; set; } = new List<ComponentLayer>();

        /// <summary>
        /// Components present in the target but not the incoming source (i.e. would be deleted on an
        /// upgrade). Each entry is "&lt;Type&gt;: &lt;Name&gt;" (e.g. "Entity: new_widget") so the rules
        /// can classify a removed table (Critical / data loss) vs a column (High) vs anything else (Medium)
        /// without an SDK type. A bare name with no "&lt;Type&gt;:" prefix is treated as an unclassified
        /// component (Medium).
        /// </summary>
        public List<string> RemovedComponents { get; set; } = new List<string>();

        /// <summary>
        /// True only when removed-component analysis actually ran (the caller compared the target against the
        /// incoming solution package or a source environment). When false, an empty <see cref="RemovedComponents"/>
        /// means "not assessed" — NOT "nothing is deleted" — so the rules must not imply a delete-capable
        /// upgrade is data-loss-free. A single-connection collector inspecting only the installed solution
        /// leaves this false.
        /// </summary>
        public bool RemovedComponentsAssessed { get; set; }

        /// <summary>Required components the solution depends on but does not contain (type, name).</summary>
        public List<(string type, string name)> MissingDependencies { get; set; } = new List<(string type, string name)>();

        /// <summary>Publisher customization prefix of the incoming (source) solution.</summary>
        public string SourcePublisherPrefix { get; set; }

        /// <summary>Publisher customization prefix in the target environment (null when single-connection).</summary>
        public string TargetPublisherPrefix { get; set; }
    }

    /// <summary>Tunable knobs for <see cref="LayerImpactRules.Evaluate"/>. Conservative defaults.</summary>
    public class ImpactOptions
    {
        /// <summary>
        /// When true, a <see cref="DeploymentPath.Holding"/> path is treated as eventually deleting
        /// (the staged upgrade will delete once applied), so deletion risk is surfaced for it too.
        /// </summary>
        public bool TreatHoldingAsDeleting { get; set; } = true;

        public static ImpactOptions Default => new ImpactOptions();
    }

    /// <summary>
    /// The result of a layer-impact analysis: the banded impact score, the findings, a generated
    /// pre-upgrade checklist and rollback guidance, and headline metrics. SDK-free.
    /// </summary>
    public class ImpactReport
    {
        public int Score { get; set; }
        public ScoreBand Band { get; set; }
        public List<Finding> Findings { get; set; } = new List<Finding>();

        /// <summary>Ordered pre-upgrade checklist lines generated from the findings + path.</summary>
        public List<string> Checklist { get; set; } = new List<string>();

        /// <summary>Rollback-guidance lines generated from the findings + path.</summary>
        public List<string> RollbackGuidance { get; set; } = new List<string>();

        public List<MetricRow> Metrics { get; set; } = new List<MetricRow>();
    }
}
