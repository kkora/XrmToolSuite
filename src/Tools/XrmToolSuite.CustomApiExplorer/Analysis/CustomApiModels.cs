using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.CustomApiExplorer.Analysis
{
    /// <summary>
    /// Custom API parameter/response data types (mirrors the platform <c>customapifieldtype</c> option set).
    /// Kept as a plain enum so request-building and value parsing are SDK-free and unit-testable.
    /// </summary>
    public enum CustomApiFieldType
    {
        Boolean = 0,
        DateTime = 1,
        Decimal = 2,
        Entity = 3,
        EntityCollection = 4,
        EntityReference = 5,
        Float = 6,
        Integer = 7,
        Money = 8,
        Picklist = 9,
        String = 10,
        StringArray = 11,
        Guid = 12,
    }

    /// <summary>How a Custom API is bound (mirrors <c>customapi.bindingtype</c>).</summary>
    public enum CustomApiBindingType
    {
        Global = 0,
        Entity = 1,
        EntityCollection = 2,
    }

    /// <summary>One request parameter of a Custom API.</summary>
    public sealed class CustomApiParameter
    {
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string LogicalName { get; set; }     // the key used when building the request
        public CustomApiFieldType Type { get; set; }
        public bool IsOptional { get; set; }
        public string LogicalEntityName { get; set; } // for Entity/EntityReference/collection params

        /// <summary>Scalar types can be typed straight into a textbox; complex types need structured input.</summary>
        public bool IsScalar => ValueParsing.IsScalar(Type);
    }

    /// <summary>One response property of a Custom API.</summary>
    public sealed class CustomApiResponseProperty
    {
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string LogicalName { get; set; }
        public CustomApiFieldType Type { get; set; }
        public string LogicalEntityName { get; set; }
    }

    /// <summary>A component that references a Custom API (a step, flow, or other dependency).</summary>
    public sealed class CustomApiCaller
    {
        public string ComponentType { get; set; }
        public string Name { get; set; }
    }

    /// <summary>A Custom API and everything the explorer catalogs about it.</summary>
    public sealed class CustomApiInfo
    {
        public Guid Id { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public CustomApiBindingType BindingType { get; set; }
        public string BoundEntityLogicalName { get; set; }
        public bool IsFunction { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsManaged { get; set; }

        /// <summary>Backing plugin type name, or null when the API has no logic registered.</summary>
        public string PluginTypeName { get; set; }
        public string SdkMessageName { get; set; }

        public List<CustomApiParameter> Parameters { get; } = new List<CustomApiParameter>();
        public List<CustomApiResponseProperty> ResponseProperties { get; } = new List<CustomApiResponseProperty>();
        public List<CustomApiCaller> Callers { get; } = new List<CustomApiCaller>();

        public IEnumerable<CustomApiParameter> RequiredParameters =>
            Parameters.Where(p => !p.IsOptional);

        public bool RequiresTarget => BindingType != CustomApiBindingType.Global;

        public string BindingSummary()
        {
            switch (BindingType)
            {
                case CustomApiBindingType.Entity: return $"Bound to {BoundEntityLogicalName}";
                case CustomApiBindingType.EntityCollection: return $"Bound to {BoundEntityLogicalName} (collection)";
                default: return "Global (unbound)";
            }
        }
    }

    /// <summary>The full inventory result: the APIs plus any degraded-scan notes.</summary>
    public sealed class CustomApiCatalog
    {
        public string EnvironmentName { get; set; }
        public DateTime CollectedOnUtc { get; set; } = DateTime.UtcNow;
        public List<CustomApiInfo> Apis { get; } = new List<CustomApiInfo>();
        public List<string> Notes { get; } = new List<string>();

        public int Count => Apis.Count;
    }
}
