using System;
using System.Collections.Generic;
using System.Linq;

namespace XrmToolSuite.ApiDocumentationBuilder.Api
{
    // =====================================================================================
    // SDK-free API-documentation model. NOTHING in this file (or Redactor / ApiDocEmitters /
    // OpenApiEmitter) references Microsoft.Xrm.Sdk, so the whole model + redaction + emit pipeline is
    // unit-testable with the plain .NET SDK — no Dataverse, no WinForms. The SDK collector (ApiCollector)
    // fills an ApiCatalog from live metadata; the emitters turn it into Markdown / HTML / JSON / OpenAPI.
    // =====================================================================================

    /// <summary>Custom API parameter/response data types (mirrors the platform <c>customapifieldtype</c>).</summary>
    public enum ApiFieldType
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
    public enum ApiBindingType
    {
        Global = 0,
        Entity = 1,
        EntityCollection = 2,
    }

    /// <summary>One request parameter of a Custom API.</summary>
    public sealed class ApiParameter
    {
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string LogicalName { get; set; }
        public ApiFieldType Type { get; set; }
        public bool IsOptional { get; set; }
        public string LogicalEntityName { get; set; }
        public string Description { get; set; }
    }

    /// <summary>One response property of a Custom API.</summary>
    public sealed class ApiResponseProperty
    {
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string LogicalName { get; set; }
        public ApiFieldType Type { get; set; }
        public string LogicalEntityName { get; set; }
        public string Description { get; set; }
    }

    /// <summary>A Custom API and everything the builder documents about it.</summary>
    public sealed class ApiDoc
    {
        public Guid Id { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public ApiBindingType BindingType { get; set; }
        public string BoundEntityLogicalName { get; set; }
        public bool IsFunction { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsManaged { get; set; }

        public string PluginTypeName { get; set; }
        public string SdkMessageName { get; set; }
        public string ExecutePrivilegeName { get; set; }

        public List<ApiParameter> Parameters { get; } = new List<ApiParameter>();
        public List<ApiResponseProperty> ResponseProperties { get; } = new List<ApiResponseProperty>();

        public IEnumerable<ApiParameter> RequiredParameters => Parameters.Where(p => !p.IsOptional);

        /// <summary>The bound-operation kind for the OpenAPI/Web-API view (Action for functions=false, Function otherwise).</summary>
        public string OperationKind => IsFunction ? "Function" : "Action";

        public string BindingSummary()
        {
            switch (BindingType)
            {
                case ApiBindingType.Entity: return $"Bound to {BoundEntityLogicalName}";
                case ApiBindingType.EntityCollection: return $"Bound to {BoundEntityLogicalName} (collection)";
                default: return "Global (unbound)";
            }
        }
    }

    /// <summary>The full documentation result: the APIs plus any degraded-scan notes.</summary>
    public sealed class ApiCatalog
    {
        public string EnvironmentName { get; set; }
        public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;
        public List<ApiDoc> Apis { get; } = new List<ApiDoc>();
        public List<string> Notes { get; } = new List<string>();

        public int Count => Apis.Count;

        /// <summary>APIs in stable unique-name order.</summary>
        public IEnumerable<ApiDoc> OrderedApis =>
            Apis.OrderBy(a => a.UniqueName ?? a.DisplayName ?? "", StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Emit options: whether to include example payloads and the operator's user-controlled redaction terms
    /// (added to the built-in secret-name heuristics). Plain serializable POCO — never carries credentials.
    /// </summary>
    public sealed class ApiDocOptions
    {
        public bool IncludeExamples { get; set; } = true;

        /// <summary>Extra parameter/property name fragments the operator wants masked before export.</summary>
        public List<string> AdditionalRedactTerms { get; set; } = new List<string>();

        public static ApiDocOptions Default() => new ApiDocOptions();
    }

    /// <summary>Maps <see cref="ApiFieldType"/> to friendly labels, OpenAPI schema fragments, and sample values.</summary>
    public static class FieldTypes
    {
        public static string Label(ApiFieldType t) => t.ToString();

        /// <summary>An OpenAPI 3.0 schema object (JSON) for the type — best-effort.</summary>
        public static string OpenApiSchema(ApiFieldType t)
        {
            switch (t)
            {
                case ApiFieldType.Boolean: return "{\"type\":\"boolean\"}";
                case ApiFieldType.DateTime: return "{\"type\":\"string\",\"format\":\"date-time\"}";
                case ApiFieldType.Decimal:
                case ApiFieldType.Float:
                case ApiFieldType.Money: return "{\"type\":\"number\"}";
                case ApiFieldType.Integer:
                case ApiFieldType.Picklist: return "{\"type\":\"integer\"}";
                case ApiFieldType.String: return "{\"type\":\"string\"}";
                case ApiFieldType.StringArray: return "{\"type\":\"array\",\"items\":{\"type\":\"string\"}}";
                case ApiFieldType.Guid: return "{\"type\":\"string\",\"format\":\"uuid\"}";
                case ApiFieldType.EntityCollection: return "{\"type\":\"array\",\"items\":{\"type\":\"object\"}}";
                default: return "{\"type\":\"object\"}"; // Entity / EntityReference
            }
        }

        /// <summary>A raw JSON sample value for the type (for template example payloads).</summary>
        public static string SampleJson(ApiFieldType t)
        {
            switch (t)
            {
                case ApiFieldType.Boolean: return "true";
                case ApiFieldType.DateTime: return "\"2020-01-01T00:00:00Z\"";
                case ApiFieldType.Decimal:
                case ApiFieldType.Float:
                case ApiFieldType.Money: return "0.0";
                case ApiFieldType.Integer:
                case ApiFieldType.Picklist: return "0";
                case ApiFieldType.String: return "\"string\"";
                case ApiFieldType.StringArray: return "[\"string\"]";
                case ApiFieldType.Guid: return "\"00000000-0000-0000-0000-000000000000\"";
                case ApiFieldType.EntityCollection: return "[]";
                case ApiFieldType.Entity:
                case ApiFieldType.EntityReference:
                    return "{\"@odata.type\":\"Microsoft.Dynamics.CRM.entityname\",\"id\":\"00000000-0000-0000-0000-000000000000\"}";
                default: return "null";
            }
        }
    }
}
