using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// Emits a self-contained, theme-neutral HTML page: a summary, the embedded ERD SVG, a table list
    /// (with column/key detail) and a relationships table. No external references (fonts/CSS/JS inline),
    /// so it opens anywhere. Pure string generation — SDK-free and deterministic.
    /// </summary>
    public static class ErdHtml
    {
        public static string Emit(ErdModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            string X(string s) => WebUtility.HtmlEncode(s ?? "");

            var sb = new StringBuilder();
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1f2937;background:#ffffff}");
            sb.AppendLine("h1{font-size:20px;margin:0 0 4px}h2{font-size:15px;margin:22px 0 8px;border-bottom:1px solid #e5e7eb;padding-bottom:4px}");
            sb.AppendLine(".muted{color:#6b7280;font-size:12px}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;font-size:12px;margin:6px 0 14px}");
            sb.AppendLine("th,td{border:1px solid #e5e7eb;padding:4px 8px;text-align:left;vertical-align:top}");
            sb.AppendLine("th{background:#f1f5f9}");
            sb.AppendLine(".pill{display:inline-block;padding:1px 6px;border-radius:8px;font-size:10px;color:#fff}");
            sb.AppendLine(".custom{background:#2563eb}.standard{background:#475569}.managed{background:#7c3aed}");
            sb.AppendLine(".diagram{overflow:auto;border:1px solid #e5e7eb;border-radius:6px;padding:8px;margin:8px 0}");
            sb.AppendLine("code{background:#f1f5f9;padding:1px 4px;border-radius:3px}");
            sb.AppendLine("</style>");

            sb.AppendLine("<h1>Dataverse ERD</h1>");
            sb.AppendLine($"<div class=\"muted\">{model.Tables.Count} table(s) &middot; {model.Relationships.Count} relationship(s) &middot; generated {DateTime.Now:yyyy-MM-dd HH:mm}</div>");

            sb.AppendLine("<h2>Diagram</h2>");
            sb.AppendLine("<div class=\"diagram\">");
            sb.AppendLine(ErdSvg.Emit(model, ColumnDisplay.KeysAndLookupsOnly));
            sb.AppendLine("</div>");

            sb.AppendLine("<h2>Tables</h2>");
            foreach (var t in model.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                var cls = t.IsCustom ? "custom" : "standard";
                var clsLabel = t.IsCustom ? "custom" : "standard";
                var managed = t.IsManaged ? " <span class=\"pill managed\">managed</span>" : "";
                sb.AppendLine($"<h3 style=\"font-size:13px;margin:14px 0 4px\">{X(t.DisplayName ?? t.LogicalName)} " +
                              $"<span class=\"pill {cls}\">{clsLabel}</span>{managed}</h3>");
                sb.AppendLine($"<div class=\"muted\">logical <code>{X(t.LogicalName)}</code> &middot; schema <code>{X(t.SchemaName)}</code> &middot; " +
                              $"PK <code>{X(t.PrimaryIdColumn)}</code> &middot; name <code>{X(t.PrimaryNameColumn)}</code></div>");

                sb.AppendLine("<table><tr><th>Column</th><th>Type</th><th>Required</th><th>Flags</th><th>Targets</th></tr>");
                foreach (var c in t.Columns)
                {
                    var flags = string.Join(" ", new[]
                    {
                        c.IsPrimaryId ? "PK" : null,
                        c.IsPrimaryName ? "name" : null,
                        c.IsLookup ? "FK" : null
                    }.Where(f => f != null));
                    sb.AppendLine($"<tr><td>{X(c.LogicalName)}</td><td>{X(c.Type)}</td><td>{X(c.RequiredLevel)}</td>" +
                                  $"<td>{X(flags)}</td><td>{X(string.Join(", ", c.Targets ?? Enumerable.Empty<string>()))}</td></tr>");
                }
                sb.AppendLine("</table>");

                if (t.AlternateKeys.Count > 0)
                {
                    sb.AppendLine("<div class=\"muted\">Alternate keys: " +
                        string.Join("; ", t.AlternateKeys.Select(k => $"{X(k.Name)} ({X(string.Join(", ", k.Columns))})")) + "</div>");
                }
            }

            sb.AppendLine("<h2>Relationships</h2>");
            if (model.Relationships.Count == 0)
            {
                sb.AppendLine("<div class=\"muted\">No relationships between the selected tables.</div>");
            }
            else
            {
                sb.AppendLine("<table><tr><th>Schema name</th><th>Type</th><th>From</th><th>To</th><th>Lookup</th><th>Required</th><th>Cascade</th></tr>");
                foreach (var r in model.Relationships)
                {
                    sb.AppendLine($"<tr><td>{X(r.SchemaName)}</td><td>{X(r.RelationType)}</td><td>{X(r.FromTable)}</td>" +
                                  $"<td>{X(r.ToTable)}</td><td>{X(r.LookupColumn)}</td><td>{X(r.RequiredLevel)}</td><td>{X(r.CascadeSummary)}</td></tr>");
                }
                sb.AppendLine("</table>");
            }

            if (model.Notes != null && model.Notes.Count > 0)
            {
                sb.AppendLine("<h2>Notes</h2><ul>");
                foreach (var n in model.Notes) sb.AppendLine($"<li class=\"muted\">{X(n)}</li>");
                sb.AppendLine("</ul>");
            }

            return sb.ToString();
        }

        public static void Export(ErdModel model, string path)
            => File.WriteAllText(path, Emit(model), Encoding.UTF8);
    }
}
