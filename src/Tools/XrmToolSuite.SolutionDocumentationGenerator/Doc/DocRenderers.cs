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
        // HTML Portal (self-contained, searchable, offline — folds in the DOC05 "HTML Documentation
        // Portal" concept as a single-file static site: sticky sidebar table-of-contents, client-side
        // search that filters sections and table rows, collapsible sections, and a light/dark theme
        // toggle. Everything is inlined (no external CDN / assets) so it browses straight from file://.
        // Still SDK-free / BCL-only, so it stays in the unit-test compile set.
        // =====================================================================================

        public static string HtmlPortal(SolutionDoc doc)
        {
            doc = doc ?? new SolutionDoc();
            var title = string.IsNullOrWhiteSpace(doc.SolutionName) ? "Solution documentation" : doc.SolutionName;
            var sections = (doc.Sections ?? new List<DocSection>());

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.Append("<title>").Append(H(title)).AppendLine(" — Documentation portal</title>");
            sb.AppendLine("<style>");
            // Theme: prefers-color-scheme default, overridable by the [data-theme] toggle (both directions win).
            sb.AppendLine(":root{--bg:#ffffff;--fg:#1b1b1b;--muted:#5a5a5a;--line:#d7d7d7;--head:#f3f3f5;--accent:#2b2b40;--zebra:#fafafa;--side:#f7f7f9;--hit:#fff6cc;}");
            sb.AppendLine("@media (prefers-color-scheme:dark){:root{--bg:#16161d;--fg:#e8e8ea;--muted:#a2a2ad;--line:#33333f;--head:#22222c;--accent:#c9c9e6;--zebra:#1c1c25;--side:#1b1b24;--hit:#4d451f;}}");
            sb.AppendLine(":root[data-theme=\"light\"]{--bg:#ffffff;--fg:#1b1b1b;--muted:#5a5a5a;--line:#d7d7d7;--head:#f3f3f5;--accent:#2b2b40;--zebra:#fafafa;--side:#f7f7f9;--hit:#fff6cc;}");
            sb.AppendLine(":root[data-theme=\"dark\"]{--bg:#16161d;--fg:#e8e8ea;--muted:#a2a2ad;--line:#33333f;--head:#22222c;--accent:#c9c9e6;--zebra:#1c1c25;--side:#1b1b24;--hit:#4d451f;}");
            sb.AppendLine("*{box-sizing:border-box;}");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:var(--bg);color:var(--fg);}");
            sb.AppendLine(".layout{display:flex;align-items:flex-start;}");
            sb.AppendLine("nav.side{position:sticky;top:0;height:100vh;overflow-y:auto;width:250px;flex:0 0 250px;background:var(--side);border-right:1px solid var(--line);padding:16px 12px;}");
            sb.AppendLine("nav.side h3{font-size:13px;text-transform:uppercase;letter-spacing:.04em;color:var(--muted);margin:14px 0 6px;}");
            sb.AppendLine("nav.side a{display:block;padding:5px 8px;border-radius:5px;color:var(--fg);text-decoration:none;font-size:13px;}");
            sb.AppendLine("nav.side a:hover{background:var(--head);} nav.side ul{list-style:none;margin:0;padding:0;}");
            sb.AppendLine("#search{width:100%;padding:7px 9px;border:1px solid var(--line);border-radius:6px;background:var(--bg);color:var(--fg);font-size:13px;}");
            sb.AppendLine(".toolbar{display:flex;gap:8px;align-items:center;margin-bottom:10px;}");
            sb.AppendLine("#themeBtn{flex:0 0 auto;padding:6px 9px;border:1px solid var(--line);border-radius:6px;background:var(--bg);color:var(--fg);cursor:pointer;font-size:13px;}");
            sb.AppendLine("main{flex:1 1 auto;min-width:0;max-width:1100px;padding:26px 26px 60px;}");
            sb.AppendLine("h1{font-size:24px;margin:0 0 4px;}");
            sb.AppendLine("section.doc-section h2{font-size:18px;margin:30px 0 8px;color:var(--accent);border-bottom:1px solid var(--line);padding-bottom:5px;cursor:pointer;user-select:none;}");
            sb.AppendLine("section.doc-section h2::before{content:'▾ ';font-size:12px;color:var(--muted);}");
            sb.AppendLine("section.doc-section.collapsed h2::before{content:'▸ ';}");
            sb.AppendLine("section.doc-section.collapsed .sec-body{display:none;}");
            sb.AppendLine(".brand{color:var(--muted);font-style:italic;margin:0 0 12px;} .logo{max-height:56px;margin-bottom:10px;}");
            sb.AppendLine(".meta{color:var(--muted);font-size:13px;margin:0 0 8px;}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;margin:10px 0 18px;font-size:13px;} .tblwrap{overflow-x:auto;}");
            sb.AppendLine("th,td{border:1px solid var(--line);padding:5px 8px;text-align:left;vertical-align:top;}");
            sb.AppendLine("th{background:var(--head);} tr:nth-child(even) td{background:var(--zebra);}");
            sb.AppendLine(".cap{font-weight:600;margin:6px 0 2px;} .note{color:var(--muted);border-left:3px solid var(--line);padding:4px 10px;margin:8px 0;}");
            sb.AppendLine("pre{background:var(--head);border:1px solid var(--line);border-radius:6px;padding:10px;overflow-x:auto;font-size:12px;}");
            sb.AppendLine("p.prose{white-space:pre-wrap;}");
            sb.AppendLine("#noresults{color:var(--muted);font-style:italic;display:none;margin-top:14px;}");
            sb.AppendLine("</style></head><body>");
            sb.AppendLine("<div class=\"layout\">");

            // ---- sidebar: search, theme toggle, table of contents ----
            sb.AppendLine("<nav class=\"side\">");
            sb.AppendLine("<div class=\"toolbar\">");
            sb.AppendLine("<input id=\"search\" type=\"search\" placeholder=\"Search…\" aria-label=\"Search documentation\" autocomplete=\"off\">");
            sb.AppendLine("<button id=\"themeBtn\" type=\"button\" title=\"Toggle light/dark\">◐</button>");
            sb.AppendLine("</div>");
            sb.AppendLine("<h3>Contents</h3><ul id=\"toc\">");
            foreach (var section in sections)
            {
                var id = SectionId(section);
                sb.Append("<li><a href=\"#").Append(id).Append("\">")
                  .Append(H(section.Title ?? section.Kind)).AppendLine("</a></li>");
            }
            sb.AppendLine("</ul></nav>");

            // ---- main content ----
            sb.AppendLine("<main>");
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
            sb.AppendLine("<div id=\"noresults\">No sections match your search.</div>");

            foreach (var section in sections)
            {
                var id = SectionId(section);
                sb.Append("<section class=\"doc-section\" id=\"").Append(id).Append("\">");
                sb.Append("<h2>").Append(H(section.Title ?? section.Kind)).AppendLine("</h2>");
                sb.AppendLine("<div class=\"sec-body\">");

                if (!string.IsNullOrWhiteSpace(section.Body))
                {
                    if (string.Equals(section.Kind, SectionKinds.Diagrams, StringComparison.OrdinalIgnoreCase))
                        sb.Append("<pre>").Append(H(section.Body.TrimEnd())).AppendLine("</pre>");
                    else
                        sb.Append("<p class=\"prose\">").Append(H(section.Body.Trim())).AppendLine("</p>");
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

                sb.AppendLine("</div></section>");
            }
            sb.AppendLine("</main></div>");

            // ---- inline client-side behaviour (search filter, collapse, theme) ----
            sb.AppendLine("<script>");
            sb.AppendLine("(function(){");
            sb.AppendLine("var input=document.getElementById('search');");
            sb.AppendLine("var sections=Array.prototype.slice.call(document.querySelectorAll('section.doc-section'));");
            sb.AppendLine("var noResults=document.getElementById('noresults');");
            sb.AppendLine("function tocLink(id){return document.querySelector('#toc a[href=\"#'+id+'\"]');}");
            sb.AppendLine("function filter(){");
            sb.AppendLine("  var q=(input.value||'').trim().toLowerCase();var visible=0;");
            sb.AppendLine("  sections.forEach(function(sec){");
            sb.AppendLine("    if(q) sec.classList.remove('collapsed');");
            sb.AppendLine("    var titleMatch=!q||sec.querySelector('h2').textContent.toLowerCase().indexOf(q)>=0;");
            sb.AppendLine("    var rows=sec.querySelectorAll('tbody tr');var anyRow=false;");
            sb.AppendLine("    rows.forEach(function(tr){");
            sb.AppendLine("      var m=!q||titleMatch||tr.textContent.toLowerCase().indexOf(q)>=0;");
            sb.AppendLine("      tr.style.display=m?'':'none';if(m)anyRow=true;");
            sb.AppendLine("    });");
            sb.AppendLine("    var bodyMatch=!q||sec.textContent.toLowerCase().indexOf(q)>=0;");
            sb.AppendLine("    var show=!q||titleMatch||anyRow||(rows.length===0&&bodyMatch);");
            sb.AppendLine("    sec.style.display=show?'':'none';");
            sb.AppendLine("    var link=tocLink(sec.id);if(link)link.parentElement.style.display=show?'':'none';");
            sb.AppendLine("    if(show)visible++;");
            sb.AppendLine("  });");
            sb.AppendLine("  noResults.style.display=visible?'none':'block';");
            sb.AppendLine("}");
            sb.AppendLine("input.addEventListener('input',filter);");
            sb.AppendLine("sections.forEach(function(sec){");
            sb.AppendLine("  sec.querySelector('h2').addEventListener('click',function(){sec.classList.toggle('collapsed');});");
            sb.AppendLine("});");
            sb.AppendLine("var root=document.documentElement;var tb=document.getElementById('themeBtn');");
            sb.AppendLine("tb.addEventListener('click',function(){");
            sb.AppendLine("  var cur=root.getAttribute('data-theme');");
            sb.AppendLine("  if(!cur)cur=window.matchMedia&&window.matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light';");
            sb.AppendLine("  root.setAttribute('data-theme',cur==='dark'?'light':'dark');");
            sb.AppendLine("});");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        /// <summary>Stable, anchor-safe element id for a section (its <see cref="SectionKinds"/> kind).</summary>
        private static string SectionId(DocSection section)
        {
            var key = section?.Kind ?? section?.Title ?? "section";
            var chars = key.Where(c => char.IsLetterOrDigit(c)).ToArray();
            var slug = chars.Length > 0 ? new string(chars).ToLowerInvariant() : "section";
            return "sec-" + slug;
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
