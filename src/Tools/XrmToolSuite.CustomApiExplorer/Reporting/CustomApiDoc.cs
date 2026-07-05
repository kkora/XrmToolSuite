using System;
using System.Globalization;
using System.Linq;
using System.Text;
using XrmToolSuite.CustomApiExplorer.Analysis;

namespace XrmToolSuite.CustomApiExplorer.Reporting
{
    /// <summary>
    /// SDK-free catalog exporters for the Custom API inventory: Markdown, self-contained HTML, and CSV.
    /// BCL-only (no Dataverse, no Newtonsoft), so the rendering is unit-testable. Read-only documentation —
    /// contains no secrets and no live invocation.
    /// </summary>
    public static class CustomApiDoc
    {
        // ---- Markdown ----

        public static string ToMarkdown(CustomApiCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var sb = new StringBuilder();
            sb.AppendLine("# Custom API Catalog");
            sb.AppendLine();
            sb.AppendLine($"- Environment: {catalog.EnvironmentName}");
            sb.AppendLine($"- APIs: {catalog.Count}");
            sb.AppendLine($"- Generated: {catalog.CollectedOnUtc:u}");
            sb.AppendLine();

            foreach (var api in catalog.Apis.OrderBy(a => a.UniqueName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"## {api.UniqueName}");
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(api.DisplayName)) sb.AppendLine($"**{api.DisplayName}**  ");
                if (!string.IsNullOrWhiteSpace(api.Description)) sb.AppendLine(api.Description + "  ");
                sb.AppendLine($"- Kind: {(api.IsFunction ? "Function" : "Action")}{(api.IsPrivate ? " (private)" : "")}");
                sb.AppendLine($"- Binding: {api.BindingSummary()}");
                sb.AppendLine($"- Backing plugin: {api.PluginTypeName ?? "_none_"}");
                if (!string.IsNullOrWhiteSpace(api.SdkMessageName)) sb.AppendLine($"- SDK message: {api.SdkMessageName}");
                sb.AppendLine();

                sb.AppendLine("**Request parameters**");
                sb.AppendLine();
                if (api.Parameters.Count == 0) sb.AppendLine("_None_");
                else
                {
                    sb.AppendLine("| Name | Type | Optional |");
                    sb.AppendLine("|---|---|---|");
                    foreach (var p in api.Parameters)
                        sb.AppendLine($"| {p.LogicalName} | {p.Type} | {(p.IsOptional ? "yes" : "no")} |");
                }
                sb.AppendLine();

                sb.AppendLine("**Response properties**");
                sb.AppendLine();
                if (api.ResponseProperties.Count == 0) sb.AppendLine("_None_");
                else
                {
                    sb.AppendLine("| Name | Type |");
                    sb.AppendLine("|---|---|");
                    foreach (var r in api.ResponseProperties)
                        sb.AppendLine($"| {r.LogicalName} | {r.Type} |");
                }
                sb.AppendLine();

                if (api.Callers.Count > 0)
                {
                    sb.AppendLine("**Referenced by**");
                    sb.AppendLine();
                    foreach (var c in api.Callers)
                        sb.AppendLine($"- {c.ComponentType}: {c.Name}");
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        // ---- CSV (flat params + responses across all APIs) ----

        public static string ToCsv(CustomApiCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var sb = new StringBuilder();
            sb.AppendLine("Api,Member,Kind,Type,Optional");
            foreach (var api in catalog.Apis.OrderBy(a => a.UniqueName, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var p in api.Parameters)
                    sb.AppendLine(Csv(api.UniqueName, p.LogicalName, "Request", p.Type.ToString(), p.IsOptional ? "yes" : "no"));
                foreach (var r in api.ResponseProperties)
                    sb.AppendLine(Csv(api.UniqueName, r.LogicalName, "Response", r.Type.ToString(), ""));
                if (api.Parameters.Count == 0 && api.ResponseProperties.Count == 0)
                    sb.AppendLine(Csv(api.UniqueName, "", "", "", ""));
            }
            return sb.ToString();
        }

        // ---- self-contained HTML ----

        public static string ToHtml(CustomApiCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\">");
            sb.AppendLine("<title>Custom API Catalog</title><style>");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:2rem;color:#1b1b1b}" +
                          "h1{margin-bottom:.2rem}h2{margin-top:2rem;border-bottom:1px solid #ddd}" +
                          "table{border-collapse:collapse;margin:.5rem 0}td,th{border:1px solid #ccc;padding:.25rem .6rem;text-align:left}" +
                          ".meta{color:#555}.badge{background:#eef;border-radius:4px;padding:0 .4rem;font-size:.85em}" +
                          "@media(prefers-color-scheme:dark){body{background:#1b1b1b;color:#eee}td,th{border-color:#444}h2{border-color:#444}}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine($"<h1>Custom API Catalog</h1>");
            sb.AppendLine($"<p class=\"meta\">Environment: {Html(catalog.EnvironmentName)} &middot; {catalog.Count} API(s) &middot; {catalog.CollectedOnUtc:u}</p>");

            foreach (var api in catalog.Apis.OrderBy(a => a.UniqueName, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"<h2>{Html(api.UniqueName)} <span class=\"badge\">{(api.IsFunction ? "Function" : "Action")}</span>" +
                              (api.IsPrivate ? " <span class=\"badge\">private</span>" : "") + "</h2>");
                if (!string.IsNullOrWhiteSpace(api.Description))
                    sb.AppendLine($"<p>{Html(api.Description)}</p>");
                sb.AppendLine($"<p class=\"meta\">{Html(api.BindingSummary())} &middot; backing plugin: {Html(api.PluginTypeName ?? "none")}</p>");

                sb.AppendLine("<h3>Request parameters</h3>");
                if (api.Parameters.Count == 0) sb.AppendLine("<p><em>None</em></p>");
                else
                {
                    sb.AppendLine("<table><tr><th>Name</th><th>Type</th><th>Optional</th></tr>");
                    foreach (var p in api.Parameters)
                        sb.AppendLine($"<tr><td>{Html(p.LogicalName)}</td><td>{p.Type}</td><td>{(p.IsOptional ? "yes" : "no")}</td></tr>");
                    sb.AppendLine("</table>");
                }

                sb.AppendLine("<h3>Response properties</h3>");
                if (api.ResponseProperties.Count == 0) sb.AppendLine("<p><em>None</em></p>");
                else
                {
                    sb.AppendLine("<table><tr><th>Name</th><th>Type</th></tr>");
                    foreach (var r in api.ResponseProperties)
                        sb.AppendLine($"<tr><td>{Html(r.LogicalName)}</td><td>{r.Type}</td></tr>");
                    sb.AppendLine("</table>");
                }
            }
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string Csv(params string[] cells) =>
            string.Join(",", cells.Select(CsvCell));

        private static string CsvCell(string s)
        {
            s = s ?? string.Empty;
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static string Html(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
