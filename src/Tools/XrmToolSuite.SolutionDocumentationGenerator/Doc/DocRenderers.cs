using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace XrmToolSuite.SolutionDocumentationGenerator.Doc
{
    /// <summary>
    /// SDK-free text renderers for a <see cref="SolutionDoc"/>: Markdown, self-contained theme-aware HTML,
    /// and structured JSON. BCL-only (no ClosedXML / MigraDoc / Newtonsoft), deterministic, and fully
    /// unit-tested. The Word/PDF/Excel exporters live in their own files because they carry the
    /// ClosedXML/MigraDoc dependencies and must stay out of the SDK-free unit-test compile set.
    /// </summary>
    public static class DocRenderers
    {
        // =====================================================================================
        // Markdown
        // =====================================================================================

        public static string Markdown(SolutionDoc doc)
        {
            doc = doc ?? new SolutionDoc();
            var sb = new StringBuilder();

            var title = string.IsNullOrWhiteSpace(doc.SolutionName) ? "Solution documentation" : doc.SolutionName;
            sb.Append("# ").AppendLine(Md(title));
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(doc.BrandingHeader))
                sb.Append("_").Append(Md(doc.BrandingHeader)).AppendLine("_").AppendLine();

            sb.Append("- **Unique name:** ").AppendLine(Md(doc.UniqueName ?? ""));
            sb.Append("- **Version:** ").AppendLine(Md(doc.Version ?? ""));
            sb.Append("- **Publisher:** ").AppendLine(Md(doc.Publisher ?? ""));
            sb.Append("- **Managed:** ").AppendLine(doc.IsManaged ? "Yes" : "No");
            sb.Append("- **Documentation mode:** ").AppendLine(Md(doc.ModeLabel ?? ""));
            sb.Append("- **Generated (UTC):** ")
              .AppendLine(doc.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            sb.AppendLine();

            foreach (var section in doc.Sections ?? new List<DocSection>())
            {
                sb.Append("## ").AppendLine(Md(section.Title ?? section.Kind));
                sb.AppendLine();

                if (!string.IsNullOrWhiteSpace(section.Body))
                {
                    // Preserve fenced diagram bodies (mermaid) as code blocks; render prose verbatim.
                    if (string.Equals(section.Kind, SectionKinds.Diagrams, StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine("```mermaid").AppendLine(section.Body.TrimEnd()).AppendLine("```").AppendLine();
                    else
                        sb.AppendLine(section.Body.Trim()).AppendLine();
                }

                foreach (var note in section.Notes ?? new List<string>())
                    sb.Append("> ").AppendLine(Md(note)).AppendLine();

                foreach (var table in section.Tables ?? new List<DocTable>())
                {
                    if (table.RowCount == 0) continue;
                    if (!string.IsNullOrWhiteSpace(table.Caption))
                        sb.Append("**").Append(Md(table.Caption)).AppendLine("**").AppendLine();

                    sb.Append("| ").Append(string.Join(" | ", table.Headers.Select(Md))).AppendLine(" |");
                    sb.Append("|").Append(string.Join("|", table.Headers.Select(_ => "---"))).AppendLine("|");
                    foreach (var row in table.Rows)
                        sb.Append("| ").Append(string.Join(" | ", Pad(row, table.Headers.Count).Select(Md))).AppendLine(" |");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static string Md(string value) =>
            (value ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

        // =====================================================================================
        // HTML (self-contained, theme-aware light/dark)
        // =====================================================================================

        public static string Html(SolutionDoc doc)
        {
            doc = doc ?? new SolutionDoc();
            var title = string.IsNullOrWhiteSpace(doc.SolutionName) ? "Solution documentation" : doc.SolutionName;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.Append("<title>").Append(H(title)).AppendLine(" — Solution documentation</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(":root{--bg:#ffffff;--fg:#1b1b1b;--muted:#5a5a5a;--line:#d7d7d7;--head:#f3f3f5;--accent:#2b2b40;--zebra:#fafafa;}");
            sb.AppendLine("@media (prefers-color-scheme:dark){:root{--bg:#16161d;--fg:#e8e8ea;--muted:#a2a2ad;--line:#33333f;--head:#22222c;--accent:#c9c9e6;--zebra:#1c1c25;}}");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:var(--bg);color:var(--fg);}");
            sb.AppendLine(".wrap{max-width:1100px;margin:0 auto;padding:28px 22px;}");
            sb.AppendLine("h1{font-size:24px;margin:0 0 4px;} h2{font-size:18px;margin:32px 0 8px;color:var(--accent);border-bottom:1px solid var(--line);padding-bottom:5px;}");
            sb.AppendLine(".brand{color:var(--muted);font-style:italic;margin:0 0 12px;} .logo{max-height:56px;margin-bottom:10px;}");
            sb.AppendLine(".meta{color:var(--muted);font-size:13px;margin:0 0 8px;}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;margin:10px 0 18px;font-size:13px;} .tblwrap{overflow-x:auto;}");
            sb.AppendLine("th,td{border:1px solid var(--line);padding:5px 8px;text-align:left;vertical-align:top;}");
            sb.AppendLine("th{background:var(--head);} tr:nth-child(even) td{background:var(--zebra);}");
            sb.AppendLine(".cap{font-weight:600;margin:6px 0 2px;} .note{color:var(--muted);border-left:3px solid var(--line);padding:4px 10px;margin:8px 0;}");
            sb.AppendLine("pre{background:var(--head);border:1px solid var(--line);border-radius:6px;padding:10px;overflow-x:auto;font-size:12px;}");
            sb.AppendLine("p{white-space:pre-wrap;}");
            sb.AppendLine("</style></head><body><div class=\"wrap\">");

            if (!string.IsNullOrWhiteSpace(doc.LogoUrl))
                sb.Append("<img class=\"logo\" src=\"").Append(H(doc.LogoUrl)).AppendLine("\" alt=\"logo\">");
            sb.Append("<h1>").Append(H(title)).AppendLine("</h1>");
            if (!string.IsNullOrWhiteSpace(doc.BrandingHeader))
                sb.Append("<p class=\"brand\">").Append(H(doc.BrandingHeader)).AppendLine("</p>");

            sb.Append("<p class=\"meta\">Unique name: <strong>").Append(H(doc.UniqueName ?? "")).Append("</strong> · ")
              .Append("Version: <strong>").Append(H(doc.Version ?? "")).Append("</strong> · ")
              .Append("Publisher: <strong>").Append(H(doc.Publisher ?? "")).Append("</strong> · ")
              .Append(doc.IsManaged ? "Managed" : "Unmanaged").AppendLine("</p>");
            sb.Append("<p class=\"meta\">Mode: ").Append(H(doc.ModeLabel ?? "")).Append(" · Generated (UTC): ")
              .Append(H(doc.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)))
              .AppendLine("</p>");

            foreach (var section in doc.Sections ?? new List<DocSection>())
            {
                sb.Append("<h2>").Append(H(section.Title ?? section.Kind)).AppendLine("</h2>");

                if (!string.IsNullOrWhiteSpace(section.Body))
                {
                    if (string.Equals(section.Kind, SectionKinds.Diagrams, StringComparison.OrdinalIgnoreCase))
                        sb.Append("<pre>").Append(H(section.Body.TrimEnd())).AppendLine("</pre>");
                    else
                        sb.Append("<p>").Append(H(section.Body.Trim())).AppendLine("</p>");
                }

                foreach (var note in section.Notes ?? new List<string>())
                    sb.Append("<div class=\"note\">").Append(H(note)).AppendLine("</div>");

                foreach (var table in section.Tables ?? new List<DocTable>())
                {
                    if (table.RowCount == 0) continue;
                    if (!string.IsNullOrWhiteSpace(table.Caption))
                        sb.Append("<div class=\"cap\">").Append(H(table.Caption)).AppendLine("</div>");
                    sb.AppendLine("<div class=\"tblwrap\"><table><thead><tr>");
                    foreach (var h in table.Headers) sb.Append("<th>").Append(H(h)).Append("</th>");
                    sb.AppendLine("</tr></thead><tbody>");
                    foreach (var row in table.Rows)
                    {
                        sb.Append("<tr>");
                        foreach (var cell in Pad(row, table.Headers.Count)) sb.Append("<td>").Append(H(cell)).Append("</td>");
                        sb.AppendLine("</tr>");
                    }
                    sb.AppendLine("</tbody></table></div>");
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

        // =====================================================================================
        // JSON (structured — hand-rolled BCL only so the unit-test compile set stays dependency-free)
        // =====================================================================================

        public static string Json(SolutionDoc doc)
        {
            doc = doc ?? new SolutionDoc();
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"solutionName\":").Append(J(doc.SolutionName)).Append(',');
            sb.Append("\"uniqueName\":").Append(J(doc.UniqueName)).Append(',');
            sb.Append("\"version\":").Append(J(doc.Version)).Append(',');
            sb.Append("\"publisher\":").Append(J(doc.Publisher)).Append(',');
            sb.Append("\"isManaged\":").Append(doc.IsManaged ? "true" : "false").Append(',');
            sb.Append("\"mode\":").Append(J(doc.ModeLabel)).Append(',');
            sb.Append("\"generatedUtc\":")
              .Append(J(doc.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture))).Append(',');

            sb.Append("\"sections\":[");
            var sections = doc.Sections ?? new List<DocSection>();
            for (int i = 0; i < sections.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var s = sections[i];
                sb.Append('{');
                sb.Append("\"kind\":").Append(J(s.Kind)).Append(',');
                sb.Append("\"title\":").Append(J(s.Title)).Append(',');
                sb.Append("\"body\":").Append(J(s.Body)).Append(',');
                sb.Append("\"notes\":[")
                  .Append(string.Join(",", (s.Notes ?? new List<string>()).Select(J)))
                  .Append("],");
                sb.Append("\"tables\":[");
                var tables = s.Tables ?? new List<DocTable>();
                for (int t = 0; t < tables.Count; t++)
                {
                    if (t > 0) sb.Append(',');
                    var tbl = tables[t];
                    sb.Append('{');
                    sb.Append("\"caption\":").Append(J(tbl.Caption)).Append(',');
                    sb.Append("\"headers\":[")
                      .Append(string.Join(",", (tbl.Headers ?? new List<string>()).Select(J)))
                      .Append("],");
                    sb.Append("\"rows\":[");
                    var rows = tbl.Rows ?? new List<List<string>>();
                    for (int r = 0; r < rows.Count; r++)
                    {
                        if (r > 0) sb.Append(',');
                        sb.Append('[').Append(string.Join(",", rows[r].Select(J))).Append(']');
                    }
                    sb.Append("]}");
                }
                sb.Append("]}");
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static string J(string s)
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

        // ---- shared ----

        private static IEnumerable<string> Pad(List<string> row, int width)
        {
            row = row ?? new List<string>();
            for (int i = 0; i < width; i++)
                yield return i < row.Count ? (row[i] ?? "") : "";
        }
    }
}
