using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace XrmToolSuite.ApiDocumentationBuilder.Api
{
    /// <summary>
    /// Best-effort OpenAPI 3.0-style JSON generator for Dataverse Custom APIs (SDK-free, unit-testable). Each
    /// Custom API becomes a POST path (<c>/{uniquename}</c>) whose requestBody schema is built from its request
    /// parameters and whose 200 response schema is built from its response properties. This is a *bootstrap*
    /// contract for client-code generation, NOT a guaranteed-valid live endpoint spec (Dataverse bound
    /// functions/actions have their own OData conventions) — the description says so. Secret-named parameters
    /// are annotated so generators don't emit real values.
    /// </summary>
    public static class OpenApiEmitter
    {
        public static string Generate(ApiCatalog catalog, ApiDocOptions options = null)
        {
            catalog = catalog ?? new ApiCatalog();
            var redactor = new Redactor((options ?? ApiDocOptions.Default()).AdditionalRedactTerms);
            var apis = catalog.OrderedApis.ToList();

            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"openapi\":\"3.0.3\",");
            sb.Append("\"info\":{")
              .Append("\"title\":").Append(JsonText.Quote((catalog.EnvironmentName ?? "Dataverse") + " — Custom APIs")).Append(',')
              .Append("\"version\":").Append(JsonText.Quote(catalog.GeneratedUtc.ToUniversalTime().ToString("yyyyMMdd", CultureInfo.InvariantCulture))).Append(',')
              .Append("\"description\":").Append(JsonText.Quote(
                  "Best-effort OpenAPI-style contract generated from Custom API metadata. Bootstrap for client code, not a guaranteed live-endpoint spec. Example values for secret-named parameters are omitted."))
              .Append("},");

            sb.Append("\"paths\":{");
            for (int i = 0; i < apis.Count; i++)
            {
                if (i > 0) sb.Append(',');
                EmitPath(sb, apis[i], redactor);
            }
            sb.Append("}}");
            return sb.ToString();
        }

        private static void EmitPath(StringBuilder sb, ApiDoc api, Redactor redactor)
        {
            var opId = api.UniqueName ?? api.DisplayName ?? "operation";
            sb.Append(JsonText.Quote("/" + opId)).Append(":{");
            sb.Append("\"post\":{");
            sb.Append("\"operationId\":").Append(JsonText.Quote(opId)).Append(',');
            sb.Append("\"summary\":").Append(JsonText.Quote(api.DisplayName ?? api.UniqueName)).Append(',');
            sb.Append("\"description\":").Append(JsonText.Quote(redactor.RedactText(api.Description) ?? "")).Append(',');
            sb.Append("\"x-binding\":").Append(JsonText.Quote(api.BindingSummary())).Append(',');
            sb.Append("\"x-operationKind\":").Append(JsonText.Quote(api.OperationKind)).Append(',');

            // requestBody
            sb.Append("\"requestBody\":{\"required\":").Append(api.RequiredParameters.Any() ? "true" : "false")
              .Append(",\"content\":{\"application/json\":{\"schema\":");
            EmitObjectSchema(sb, api.Parameters, redactor);
            sb.Append("}}},");

            // responses
            sb.Append("\"responses\":{\"200\":{\"description\":\"Success\",\"content\":{\"application/json\":{\"schema\":");
            EmitResponseSchema(sb, api.ResponseProperties);
            sb.Append("}}}}");

            sb.Append("}}"); // close post, path
        }

        private static void EmitObjectSchema(StringBuilder sb, List<ApiParameter> parameters, Redactor redactor)
        {
            parameters = parameters ?? new List<ApiParameter>();
            sb.Append("{\"type\":\"object\",\"properties\":{");
            for (int i = 0; i < parameters.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var p = parameters[i];
                sb.Append(JsonText.Quote(p.UniqueName ?? p.LogicalName ?? ("param" + i))).Append(':');
                // Splice a description / secret annotation into the type schema.
                var schema = FieldTypes.OpenApiSchema(p.Type);
                var extra = new List<string>();
                if (!string.IsNullOrWhiteSpace(p.Description))
                    extra.Add("\"description\":" + JsonText.Quote(redactor.RedactText(p.Description)));
                if (redactor.IsSensitiveName(p.UniqueName) || redactor.IsSensitiveName(p.DisplayName))
                {
                    extra.Add("\"format\":\"password\"");
                    extra.Add("\"x-redacted\":true");
                }
                sb.Append(Splice(schema, extra));
            }
            sb.Append('}');
            var required = parameters.Where(p => !p.IsOptional).Select(p => p.UniqueName ?? p.LogicalName).ToList();
            if (required.Count > 0)
            {
                sb.Append(",\"required\":[");
                sb.Append(string.Join(",", required.Select(JsonText.Quote)));
                sb.Append(']');
            }
            sb.Append('}');
        }

        private static void EmitResponseSchema(StringBuilder sb, List<ApiResponseProperty> props)
        {
            props = props ?? new List<ApiResponseProperty>();
            sb.Append("{\"type\":\"object\",\"properties\":{");
            for (int i = 0; i < props.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(JsonText.Quote(props[i].UniqueName ?? props[i].LogicalName ?? ("prop" + i))).Append(':')
                  .Append(FieldTypes.OpenApiSchema(props[i].Type));
            }
            sb.Append("}}");
        }

        // Merge extra JSON members into a "{...}" schema object literal.
        private static string Splice(string schemaObject, List<string> extraMembers)
        {
            if (extraMembers == null || extraMembers.Count == 0) return schemaObject;
            // schemaObject is like {"type":"string"} — insert before the closing brace.
            var inner = schemaObject.TrimEnd('}');
            var sep = inner.TrimEnd().EndsWith("{") ? "" : ",";
            return inner + sep + string.Join(",", extraMembers) + "}";
        }
    }
}
