using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace XrmToolSuite.ApiDocumentationBuilder.Api
{
    /// <summary>
    /// SDK-free, BCL-only documentation emitters for an <see cref="ApiCatalog"/>: a Markdown API reference, a
    /// self-contained theme-aware HTML reference, a raw structured JSON model, and template example payloads.
    /// All secret-name values are masked through <see cref="Redactor"/>. Deterministic and fully unit-testable.
    /// The OpenAPI-style spec lives in <see cref="OpenApiEmitter"/>.
    /// </summary>
    public static class ApiDocEmitters
    {
        // =====================================================================================
        // Example request payload (a template — clearly labelled, secrets masked)
        // =====================================================================================

        public static string ExampleRequestJson(ApiDoc api, Redactor redactor)
        {
            api = api ?? new ApiDoc();
            redactor = redactor ?? new Redactor();
            var sb = new StringBuilder();
            sb.Append('{');
            var pars = api.Parameters ?? new List<ApiParameter>();
            for (int i = 0; i < pars.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(JsonText.Quote(pars[i].UniqueName ?? pars[i].LogicalName ?? ("param" + i)))
                  .Append(':').Append(redactor.SampleFor(pars[i]));
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string ExampleResponseJson(ApiDoc api)
        {
            api = api ?? new ApiDoc();
            var sb = new StringBuilder();
            sb.Append('{');
            var props = api.ResponseProperties ?? new List<ApiResponseProperty>();
            for (int i = 0; i < props.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(JsonText.Quote(props[i].UniqueName ?? props[i].LogicalName ?? ("prop" + i)))
                  .Append(':').Append(FieldTypes.SampleJson(props[i].Type));
            }
            sb.Append('}');
            return sb.ToString();
        }

        // =====================================================================================
        // Markdown reference
        // =====================================================================================

        public static string Markdown(ApiCatalog catalog, ApiDocOptions options = null)
        {
            catalog = catalog ?? new ApiCatalog();
            options = options ?? ApiDocOptions.Default();
            var redactor = new Redactor(options.AdditionalRedactTerms);
            var sb = new StringBuilder();

            sb.AppendLine("# Custom API reference").AppendLine();
            sb.Append("- **Environment:** ").AppendLine(Md(catalog.EnvironmentName ?? ""));
            sb.Append("- **APIs documented:** ").AppendLine(catalog.Count.ToString(CultureInfo.InvariantCulture));
            sb.Append("- **Generated (UTC):** ")
              .AppendLine(catalog.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            sb.AppendLine();

            foreach (var note in catalog.Notes ?? new List<string>())
                sb.Append("> ").AppendLine(Md(note)).AppendLine();

            foreach (var api in catalog.OrderedApis)
            {
                sb.Append("## ").AppendLine(Md(api.DisplayName ?? api.UniqueName));
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(api.Description))
                    sb.AppendLine(Md(redactor.RedactText(api.Description))).AppendLine();

                sb.Append("- **Unique name:** `").Append(Md(api.UniqueName ?? "")).AppendLine("`");
                sb.Append("- **Type:** ").AppendLine(api.OperationKind + (api.IsPrivate ? " (private)" : ""));
                sb.Append("- **Binding:** ").AppendLine(Md(api.BindingSummary()));
                if (!string.IsNullOrWhiteSpace(api.PluginTypeName))
                    sb.Append("- **Plugin type:** `").Append(Md(api.PluginTypeName)).AppendLine("`");
                if (!string.IsNullOrWhiteSpace(api.SdkMessageName))
                    sb.Append("- **Message:** `").Append(Md(api.SdkMessageName)).AppendLine("`");
                sb.AppendLine();

                sb.AppendLine("**Request parameters**").AppendLine();
                sb.AppendLine("| Name | Type | Required | Description |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var p in api.Parameters ?? new List<ApiParameter>())
                    sb.Append("| `").Append(Md(p.UniqueName ?? p.LogicalName)).Append("` | ")
                      .Append(FieldTypes.Label(p.Type)).Append(" | ")
                      .Append(p.IsOptional ? "No" : "Yes").Append(" | ")
                      .Append(Md(redactor.RedactText(p.Description ?? ""))).AppendLine(" |");
                if ((api.Parameters?.Count ?? 0) == 0) sb.AppendLine("| _(none)_ | | | |");
                sb.AppendLine();

                sb.AppendLine("**Response properties**").AppendLine();
                sb.AppendLine("| Name | Type | Description |");
                sb.AppendLine("|---|---|---|");
                foreach (var r in api.ResponseProperties ?? new List<ApiResponseProperty>())
                    sb.Append("| `").Append(Md(r.UniqueName ?? r.LogicalName)).Append("` | ")
                      .Append(FieldTypes.Label(r.Type)).Append(" | ")
                      .Append(Md(redactor.RedactText(r.Description ?? ""))).AppendLine(" |");
                if ((api.ResponseProperties?.Count ?? 0) == 0) sb.AppendLine("| _(none)_ | | |");
                sb.AppendLine();

                if (options.IncludeExamples)
                {
                    sb.AppendLine("**Example request** _(template — replace values; secrets are masked)_").AppendLine();
                    sb.AppendLine("```json").AppendLine(ExampleRequestJson(api, redactor)).AppendLine("```").AppendLine();
                    sb.AppendLine("**Example response** _(template)_").AppendLine();
                    sb.AppendLine("```json").AppendLine(ExampleResponseJson(api)).AppendLine("```").AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string Md(string s) =>
            (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

        // =====================================================================================
        // Raw JSON model
        // =====================================================================================

        public static string Json(ApiCatalog catalog, ApiDocOptions options = null)
        {
            catalog = catalog ?? new ApiCatalog();
            var redactor = new Redactor((options ?? ApiDocOptions.Default()).AdditionalRedactTerms);
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"environment\":").Append(JsonText.Quote(catalog.EnvironmentName)).Append(',');
            sb.Append("\"generatedUtc\":")
              .Append(JsonText.Quote(catalog.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture))).Append(',');
            sb.Append("\"apis\":[");
            var apis = catalog.OrderedApis.ToList();
            for (int i = 0; i < apis.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var a = apis[i];
                sb.Append('{');
                sb.Append("\"uniqueName\":").Append(JsonText.Quote(a.UniqueName)).Append(',');
                sb.Append("\"displayName\":").Append(JsonText.Quote(a.DisplayName)).Append(',');
                sb.Append("\"description\":").Append(JsonText.Quote(redactor.RedactText(a.Description))).Append(',');
                sb.Append("\"operationKind\":").Append(JsonText.Quote(a.OperationKind)).Append(',');
                sb.Append("\"isPrivate\":").Append(a.IsPrivate ? "true" : "false").Append(',');
                sb.Append("\"binding\":").Append(JsonText.Quote(a.BindingSummary())).Append(',');
                sb.Append("\"boundEntity\":").Append(JsonText.Quote(a.BoundEntityLogicalName)).Append(',');
                sb.Append("\"pluginType\":").Append(JsonText.Quote(a.PluginTypeName)).Append(',');
                sb.Append("\"parameters\":[");
                var pars = a.Parameters ?? new List<ApiParameter>();
                for (int p = 0; p < pars.Count; p++)
                {
                    if (p > 0) sb.Append(',');
                    sb.Append('{')
                      .Append("\"name\":").Append(JsonText.Quote(pars[p].UniqueName)).Append(',')
                      .Append("\"type\":").Append(JsonText.Quote(FieldTypes.Label(pars[p].Type))).Append(',')
                      .Append("\"required\":").Append(pars[p].IsOptional ? "false" : "true")
                      .Append('}');
                }
                sb.Append("],\"responseProperties\":[");
                var props = a.ResponseProperties ?? new List<ApiResponseProperty>();
                for (int r = 0; r < props.Count; r++)
                {
                    if (r > 0) sb.Append(',');
                    sb.Append('{')
                      .Append("\"name\":").Append(JsonText.Quote(props[r].UniqueName)).Append(',')
                      .Append("\"type\":").Append(JsonText.Quote(FieldTypes.Label(props[r].Type)))
                      .Append('}');
                }
                sb.Append("]}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        // =====================================================================================
        // Self-contained, theme-aware HTML reference
        // =====================================================================================

        public static string Html(ApiCatalog catalog, ApiDocOptions options = null)
        {
            catalog = catalog ?? new ApiCatalog();
            options = options ?? ApiDocOptions.Default();
            var redactor = new Redactor(options.AdditionalRedactTerms);
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.AppendLine("<title>Custom API reference</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(":root{--bg:#ffffff;--fg:#1b1b1b;--muted:#5a5a5a;--line:#d7d7d7;--head:#f3f3f5;--accent:#2b2b40;--zebra:#fafafa;}");
            sb.AppendLine("@media (prefers-color-scheme:dark){:root{--bg:#16161d;--fg:#e8e8ea;--muted:#a2a2ad;--line:#33333f;--head:#22222c;--accent:#c9c9e6;--zebra:#1c1c25;}}");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:var(--bg);color:var(--fg);}");
            sb.AppendLine(".wrap{max-width:1000px;margin:0 auto;padding:26px 20px;}");
            sb.AppendLine("h1{font-size:23px;margin:0 0 4px;} h2{font-size:18px;color:var(--accent);border-bottom:1px solid var(--line);padding-bottom:5px;margin:28px 0 8px;}");
            sb.AppendLine(".meta{color:var(--muted);font-size:13px;margin:0 0 6px;} .note{color:var(--muted);border-left:3px solid var(--line);padding:4px 10px;margin:8px 0;}");
            sb.AppendLine(".tag{display:inline-block;font-size:11px;padding:1px 7px;border:1px solid var(--line);border-radius:10px;color:var(--muted);margin-left:6px;}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;margin:8px 0 14px;font-size:13px;} th,td{border:1px solid var(--line);padding:5px 8px;text-align:left;vertical-align:top;} th{background:var(--head);} tr:nth-child(even) td{background:var(--zebra);}");
            sb.AppendLine("code,pre{font-family:Consolas,monospace;} pre{background:var(--head);border:1px solid var(--line);border-radius:6px;padding:10px;overflow-x:auto;font-size:12px;}");
            sb.AppendLine(".tmpl{color:var(--muted);font-size:12px;font-style:italic;margin:6px 0 2px;}");
            sb.AppendLine("</style></head><body><div class=\"wrap\">");

            sb.AppendLine("<h1>Custom API reference</h1>");
            sb.Append("<p class=\"meta\">Environment: <strong>").Append(H(catalog.EnvironmentName ?? "")).Append("</strong> · APIs: <strong>")
              .Append(catalog.Count).Append("</strong> · Generated (UTC): ")
              .Append(H(catalog.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture))).AppendLine("</p>");

            foreach (var note in catalog.Notes ?? new List<string>())
                sb.Append("<div class=\"note\">").Append(H(note)).AppendLine("</div>");

            foreach (var api in catalog.OrderedApis)
            {
                sb.Append("<h2>").Append(H(api.DisplayName ?? api.UniqueName))
                  .Append("<span class=\"tag\">").Append(H(api.OperationKind)).Append("</span>");
                if (api.IsPrivate) sb.Append("<span class=\"tag\">private</span>");
                sb.AppendLine("</h2>");

                if (!string.IsNullOrWhiteSpace(api.Description))
                    sb.Append("<p>").Append(H(redactor.RedactText(api.Description))).AppendLine("</p>");

                sb.Append("<p class=\"meta\">Unique name: <code>").Append(H(api.UniqueName ?? "")).Append("</code> · ")
                  .Append(H(api.BindingSummary()));
                if (!string.IsNullOrWhiteSpace(api.PluginTypeName))
                    sb.Append(" · Plugin: <code>").Append(H(api.PluginTypeName)).Append("</code>");
                sb.AppendLine("</p>");

                sb.AppendLine("<table><thead><tr><th>Request parameter</th><th>Type</th><th>Required</th><th>Description</th></tr></thead><tbody>");
                foreach (var p in api.Parameters ?? new List<ApiParameter>())
                    sb.Append("<tr><td><code>").Append(H(p.UniqueName ?? p.LogicalName)).Append("</code></td><td>")
                      .Append(H(FieldTypes.Label(p.Type))).Append("</td><td>").Append(p.IsOptional ? "No" : "Yes")
                      .Append("</td><td>").Append(H(redactor.RedactText(p.Description ?? ""))).AppendLine("</td></tr>");
                if ((api.Parameters?.Count ?? 0) == 0) sb.AppendLine("<tr><td colspan=\"4\"><em>(none)</em></td></tr>");
                sb.AppendLine("</tbody></table>");

                sb.AppendLine("<table><thead><tr><th>Response property</th><th>Type</th><th>Description</th></tr></thead><tbody>");
                foreach (var r in api.ResponseProperties ?? new List<ApiResponseProperty>())
                    sb.Append("<tr><td><code>").Append(H(r.UniqueName ?? r.LogicalName)).Append("</code></td><td>")
                      .Append(H(FieldTypes.Label(r.Type))).Append("</td><td>").Append(H(redactor.RedactText(r.Description ?? ""))).AppendLine("</td></tr>");
                if ((api.ResponseProperties?.Count ?? 0) == 0) sb.AppendLine("<tr><td colspan=\"3\"><em>(none)</em></td></tr>");
                sb.AppendLine("</tbody></table>");

                if (options.IncludeExamples)
                {
                    sb.AppendLine("<div class=\"tmpl\">Example request (template — replace values; secrets masked)</div>");
                    sb.Append("<pre>").Append(H(ExampleRequestJson(api, redactor))).AppendLine("</pre>");
                    sb.AppendLine("<div class=\"tmpl\">Example response (template)</div>");
                    sb.Append("<pre>").Append(H(ExampleResponseJson(api))).AppendLine("</pre>");
                }
            }

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        private static string H(string value)
        {
            value = value ?? "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
