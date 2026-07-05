using System;
using System.Collections.Generic;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.SolutionMergeAssistant.Analysis
{
    /// <summary>
    /// A solution to compare, with its components. SDK-free (no Microsoft.Xrm.Sdk) so the comparison
    /// engine stays pure and unit-testable — the collector translates Dataverse rows into this shape.
    /// </summary>
    public class SolutionInfo
    {
        public string UniqueName { get; set; }
        public string FriendlyName { get; set; }
        public string Version { get; set; }
        public bool IsManaged { get; set; }
        /// <summary>The owning publisher's customization prefix (e.g. "new", "contoso").</summary>
        public string PublisherPrefix { get; set; }
        public List<SolutionComponentRef> Components { get; set; } = new List<SolutionComponentRef>();

        /// <summary>Display label used in reports and findings.</summary>
        public string Label =>
            string.IsNullOrEmpty(FriendlyName) ? UniqueName : $"{FriendlyName} ({UniqueName})";
    }

    /// <summary>A single component packaged in a solution, keyed for overlap by (ComponentType, ObjectId).</summary>
    public class SolutionComponentRef
    {
        public int ComponentType { get; set; }
        /// <summary>Human display type (e.g. "Web Resource", "Business Rule"); the collector fills this.</summary>
        public string ComponentTypeName { get; set; }
        public Guid ObjectId { get; set; }
        /// <summary>Resolved friendly/schema name where cheap; falls back to the object id.</summary>
        public string Name { get; set; }
        /// <summary>Managed state of this component (degrades to the owning solution's IsManaged).</summary>
        public bool IsManaged { get; set; }

        public string DisplayName => string.IsNullOrEmpty(Name) ? ObjectId.ToString() : Name;
    }

    /// <summary>
    /// An environment variable or connection reference packaged by a solution — the unit of the
    /// config-conflict class (same schema name, different definition/value across solutions).
    /// </summary>
    public class ConfigItem
    {
        /// <summary>"EnvVar" or "ConnRef".</summary>
        public string Kind { get; set; }
        public string SchemaName { get; set; }
        /// <summary>The env-var definition/current value, or the connection reference's connector id.</summary>
        public string DefinitionOrValue { get; set; }
        public string OwningSolution { get; set; }
    }

    /// <summary>Pre-merge go/no-go signal, ordered from safest to riskiest.</summary>
    public enum MergeVerdict
    {
        SafeToMerge,
        MergeWithWarnings,
        ManualReview,
        HighRisk,
        DoNotMerge
    }

    /// <summary>Tunable thresholds for <see cref="MergeRules"/>. Defaults are conservative.</summary>
    public class MergeOptions
    {
        /// <summary>A merge is "Do not merge" once this many High findings pile up.</summary>
        public int DoNotMergeHighCount { get; set; } = 3;
    }

    /// <summary>
    /// The full result of comparing a set of solutions: the verdict, a shared 0–100 score/band, every
    /// conflict finding, a recommended merge strategy, and a merged-component checklist. SDK-free so the
    /// control renders it and the exporters serialize it without a live org.
    /// </summary>
    public class MergeReport
    {
        public MergeVerdict Verdict { get; set; }
        public int Score { get; set; }
        public ScoreBand Band { get; set; }
        public List<Finding> Findings { get; set; } = new List<Finding>();
        public List<string> Checklist { get; set; } = new List<string>();
        public List<MetricRow> Metrics { get; set; } = new List<MetricRow>();
        public string RecommendedStrategy { get; set; }

        /// <summary>Short human phrase for the verdict banner.</summary>
        public string VerdictText
        {
            get
            {
                switch (Verdict)
                {
                    case MergeVerdict.SafeToMerge: return "Safe to merge";
                    case MergeVerdict.MergeWithWarnings: return "Merge with warnings";
                    case MergeVerdict.ManualReview: return "Manual review required";
                    case MergeVerdict.HighRisk: return "High-risk merge";
                    case MergeVerdict.DoNotMerge: return "Do not merge";
                    default: return Verdict.ToString();
                }
            }
        }
    }

    /// <summary>
    /// Well-known <c>solutioncomponent.componenttype</c> values plus display/category mapping. Kept here
    /// (SDK-free) so both the collector and the pure rules agree on how a component type is named and
    /// grouped. The "called-out" high-churn overlap classes (web resources, plugin assemblies, plugin
    /// steps, forms, views, business rules) each map to their own category.
    /// </summary>
    public static class ComponentTypes
    {
        public const int Entity = 1;
        public const int Attribute = 2;
        public const int Relationship = 10;
        public const int OptionSet = 9;
        public const int Role = 20;
        public const int SavedQuery = 26;        // view
        public const int Workflow = 29;          // process / business rule
        public const int SavedQueryVisualization = 59; // chart
        public const int SystemForm = 60;
        public const int WebResource = 61;
        public const int SiteMap = 62;
        public const int PluginType = 90;
        public const int PluginAssembly = 91;
        public const int SdkMessageProcessingStep = 92;
        public const int SdkMessageProcessingStepImage = 93;
        public const int EnvironmentVariableDefinition = 380;
        public const int EnvironmentVariableValue = 381;
        public const int ConnectionReference = 10112;

        /// <summary>Human-readable singular name for a component type number.</summary>
        public static string Name(int type)
        {
            switch (type)
            {
                case Entity: return "Table";
                case Attribute: return "Column";
                case Relationship: return "Relationship";
                case OptionSet: return "Choice";
                case Role: return "Security Role";
                case SavedQuery: return "View";
                case Workflow: return "Process";
                case SavedQueryVisualization: return "Chart";
                case SystemForm: return "Form";
                case WebResource: return "Web Resource";
                case SiteMap: return "Site Map";
                case PluginType: return "Plugin Type";
                case PluginAssembly: return "Plugin Assembly";
                case SdkMessageProcessingStep: return "Plugin Step";
                case SdkMessageProcessingStepImage: return "Plugin Step Image";
                case EnvironmentVariableDefinition: return "Environment Variable";
                case EnvironmentVariableValue: return "Environment Variable Value";
                case ConnectionReference: return "Connection Reference";
                default: return "Component (type " + type + ")";
            }
        }

        /// <summary>Plural category label a finding for this component is grouped under in reports.</summary>
        public static string Category(SolutionComponentRef c)
        {
            if (c != null && !string.IsNullOrEmpty(c.ComponentTypeName) &&
                c.ComponentTypeName.IndexOf("Business Rule", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Business Rules";

            switch (c?.ComponentType ?? -1)
            {
                case Entity: return "Tables";
                case Attribute: return "Columns";
                case Relationship: return "Relationships";
                case OptionSet: return "Choices";
                case Role: return "Security Roles";
                case SavedQuery: return "Views";
                case Workflow: return "Processes";
                case SavedQueryVisualization: return "Charts";
                case SystemForm: return "Forms";
                case WebResource: return "Web Resources";
                case SiteMap: return "Site Maps";
                case PluginType: return "Plugin Types";
                case PluginAssembly: return "Plugin Assemblies";
                case SdkMessageProcessingStep: return "Plugin Steps";
                case SdkMessageProcessingStepImage: return "Plugin Step Images";
                case EnvironmentVariableDefinition:
                case EnvironmentVariableValue: return "Environment Variables";
                case ConnectionReference: return "Connection References";
                default: return "Components";
            }
        }

        /// <summary>
        /// Severity for a duplicate/overlapping component, graded by overwrite/registration risk:
        /// plugin assemblies/types/steps double-register (High); web resources, forms, views, business
        /// rules and roles are last-writer-wins overwrites (Medium); shared base tables/columns are
        /// normal and low-risk (Low).
        /// </summary>
        public static Severity DuplicateSeverity(int type)
        {
            switch (type)
            {
                case PluginAssembly:
                case PluginType:
                case SdkMessageProcessingStep:
                case SdkMessageProcessingStepImage:
                    return Severity.High;
                case Entity:
                case Attribute:
                case Relationship:
                case OptionSet:
                    return Severity.Low;
                default:
                    return Severity.Medium;
            }
        }
    }
}
