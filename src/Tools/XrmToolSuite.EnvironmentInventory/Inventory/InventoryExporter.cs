using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace XrmToolSuite.EnvironmentInventory.Inventory
{
    /// <summary>
    /// SDK-free / UI-free exporters for an <see cref="InventorySnapshot"/>: CSV, JSON, Markdown and HTML.
    /// Uses only the BCL (no ClosedXML / MigraDoc — this tool must not ship them). Every format carries a
    /// summary counts section plus the normalized rows; NONE emits a secret/value column for environment
    /// variables. Fully unit-tested.
    /// </summary>
    public static class InventoryExporter
    {
        private static readonly string[] Header =
            { "Category", "ComponentType", "Name", "SchemaName", "Owner", "Managed", "ModifiedOn", "Details" };

        // ---- CSV (RFC-4180) ----

        public static string ToCsv(InventorySnapshot snapshot)
        {
            snapshot = snapshot ?? new InventorySnapshot();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", Header));
            foreach (var i in snapshot.Items ?? new List<InventoryItem>())
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(i.Category),
                    Csv(i.ComponentType),
                    Csv(i.Name),
                    Csv(i.SchemaName),
                    Csv(i.Owner),
                    Csv(Managed(i.IsManaged)),
                    Csv(i.ModifiedOn?.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)),
                    Csv(DetailString(i.Details)),
                }));
            }
            return sb.ToString();
        }

        /// <summary>RFC-4180: quote fields containing comma, quote, CR or LF; double embedded quotes.</summary>
        private static string Csv(string value)
        {
            value = value ?? "";
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        // ---- JSON ----
        // Hand-rolled (BCL-only) so the SDK-free model files stay dependency-free for the unit-test compile
        // set; the tool control may still use Newtonsoft at runtime elsewhere.

        public static string ToJson(InventorySnapshot snapshot)
        {
            snapshot = snapshot ?? new InventorySnapshot();
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"environmentName\":").Append(JStr(snapshot.EnvironmentName)).Append(',');
            sb.Append("\"collectedOnUtc\":")
              .Append(JStr(snapshot.CollectedOnUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)))
              .Append(',');
            sb.Append("\"total\":").Append(snapshot.Total).Append(',');

            sb.Append("\"countByCategory\":{");
            var counts = snapshot.CountByCategory();
            sb.Append(string.Join(",", counts.Select(kv => JStr(kv.Key) + ":" + kv.Value)));
            sb.Append("},");

            sb.Append("\"unavailableSources\":[");
            sb.Append(string.Join(",", (snapshot.UnavailableSources ?? new List<string>()).Select(JStr)));
            sb.Append("],");

            sb.Append("\"items\":[");
            var items = snapshot.Items ?? new List<InventoryItem>();
            for (int n = 0; n < items.Count; n++)
            {
                if (n > 0) sb.Append(',');
                var i = items[n];
                sb.Append('{');
                sb.Append("\"category\":").Append(JStr(i.Category)).Append(',');
                sb.Append("\"componentType\":").Append(JStr(i.ComponentType)).Append(',');
                sb.Append("\"name\":").Append(JStr(i.Name)).Append(',');
                sb.Append("\"schemaName\":").Append(JStr(i.SchemaName)).Append(',');
                sb.Append("\"owner\":").Append(JStr(i.Owner)).Append(',');
                sb.Append("\"isManaged\":").Append(i.IsManaged.HasValue ? (i.IsManaged.Value ? "true" : "false") : "null").Append(',');
                sb.Append("\"modifiedOn\":").Append(i.ModifiedOn.HasValue
                    ? JStr(i.ModifiedOn.Value.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture))
                    : "null").Append(',');
                sb.Append("\"details\":{");
                var det = (i.Details ?? new Dictionary<string, string>()).ToList();
                for (int d = 0; d < det.Count; d++)
                {
                    if (d > 0) sb.Append(',');
                    sb.Append(JStr(det[d].Key)).Append(':').Append(JStr(det[d].Value));
                }
                sb.Append('}');
                sb.Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string JStr(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // ---- Markdown ----

        public static string ToMarkdown(InventorySnapshot snapshot)
        {
            snapshot = snapshot ?? new InventorySnapshot();
            var sb = new StringBuilder();
            sb.AppendLine("# Environment Inventory");
            sb.AppendLine();
            sb.AppendLine($"- **Environment:** {snapshot.EnvironmentName ?? "(unknown)"}");
            sb.AppendLine($"- **Collected (UTC):** {snapshot.CollectedOnUtc.ToUniversalTime():u}");
            sb.AppendLine($"- **Total components:** {snapshot.Total}");
            sb.AppendLine();

            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine("| Category | Count |");
            sb.AppendLine("|---|---|");
            foreach (var kv in snapshot.CountByCategory())
                sb.AppendLine($"| {Md(kv.Key)} | {kv.Value} |");
            sb.AppendLine();

            if (snapshot.UnavailableSources != null && snapshot.UnavailableSources.Count > 0)
            {
                sb.AppendLine("> **Unavailable sources:** " +
                    string.Join(", ", snapshot.UnavailableSources.Select(Md)));
                sb.AppendLine();
            }

            foreach (var category in snapshot.Categories())
            {
                sb.AppendLine($"## {Md(category)}");
                sb.AppendLine();
                sb.AppendLine("| Type | Name | Schema | Owner | Managed | Modified (UTC) |");
                sb.AppendLine("|---|---|---|---|---|---|");
                foreach (var i in (snapshot.Items ?? new List<InventoryItem>())
                             .Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine($"| {Md(i.ComponentType)} | {Md(i.Name)} | {Md(i.SchemaName)} | {Md(i.Owner)} | " +
                                  $"{Md(Managed(i.IsManaged))} | {Md(i.ModifiedOn?.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture))} |");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string Md(string value) =>
            (value ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

        // ---- HTML (self-contained, theme-neutral) ----

        public static string ToHtml(InventorySnapshot snapshot)
        {
            snapshot = snapshot ?? new InventorySnapshot();
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.AppendLine("<title>Environment Inventory</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1b1b1b;background:#fff;}");
            sb.AppendLine("h1{font-size:22px;} h2{font-size:17px;margin-top:28px;border-bottom:1px solid #ddd;padding-bottom:4px;}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;margin:8px 0 20px;font-size:13px;}");
            sb.AppendLine("th,td{border:1px solid #d0d0d0;padding:5px 8px;text-align:left;vertical-align:top;}");
            sb.AppendLine("th{background:#f2f2f2;} tr:nth-child(even) td{background:#fafafa;}");
            sb.AppendLine(".meta{color:#555;font-size:13px;} .warn{color:#8a5b00;}");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<h1>Environment Inventory</h1>");
            sb.AppendLine("<p class=\"meta\">Environment: <strong>" + H(snapshot.EnvironmentName ?? "(unknown)") + "</strong><br>");
            sb.AppendLine("Collected (UTC): " + H(snapshot.CollectedOnUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)) + "<br>");
            sb.AppendLine("Total components: <strong>" + snapshot.Total + "</strong></p>");

            sb.AppendLine("<h2>Summary</h2>");
            sb.AppendLine("<table><thead><tr><th>Category</th><th>Count</th></tr></thead><tbody>");
            foreach (var kv in snapshot.CountByCategory())
                sb.AppendLine("<tr><td>" + H(kv.Key) + "</td><td>" + kv.Value + "</td></tr>");
            sb.AppendLine("</tbody></table>");

            if (snapshot.UnavailableSources != null && snapshot.UnavailableSources.Count > 0)
            {
                sb.AppendLine("<p class=\"warn\">Unavailable sources: " +
                    H(string.Join(", ", snapshot.UnavailableSources)) + "</p>");
            }

            foreach (var category in snapshot.Categories())
            {
                sb.AppendLine("<h2>" + H(category) + "</h2>");
                sb.AppendLine("<table><thead><tr><th>Type</th><th>Name</th><th>Schema</th><th>Owner</th>" +
                              "<th>Managed</th><th>Modified (UTC)</th></tr></thead><tbody>");
                foreach (var i in (snapshot.Items ?? new List<InventoryItem>())
                             .Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)))
                {
                    sb.AppendLine("<tr><td>" + H(i.ComponentType) + "</td><td>" + H(i.Name) + "</td><td>" +
                                  H(i.SchemaName) + "</td><td>" + H(i.Owner) + "</td><td>" +
                                  H(Managed(i.IsManaged)) + "</td><td>" +
                                  H(i.ModifiedOn?.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)) + "</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string H(string value)
        {
            value = value ?? "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                        .Replace("\"", "&quot;");
        }

        // ---- shared ----

        private static string Managed(bool? m) => m.HasValue ? (m.Value ? "Managed" : "Unmanaged") : "";

        private static string DetailString(Dictionary<string, string> details)
        {
            if (details == null || details.Count == 0) return "";
            return string.Join("; ", details.Select(kv => kv.Key + "=" + kv.Value));
        }
    }
}
