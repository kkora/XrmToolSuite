using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace XrmToolSuite.ArchitectureDiagramGenerator.Diagram
{
    /// <summary>
    /// SDK-free, BCL-only emitters for an <see cref="ArchDiagram"/>: Mermaid, PlantUML, DOT/Graphviz,
    /// Markdown (fenced Mermaid + legend), a self-contained theme-aware HTML page (hand-laid-out inline
    /// SVG so it renders offline with no external engine, plus the Mermaid source), and structured JSON.
    /// Deterministic and fully unit-testable — no ClosedXML / MigraDoc / Newtonsoft, no Dataverse.
    /// </summary>
    public static class DiagramEmitters
    {
        // Sequential, syntax-safe node ids (n0, n1, …) keyed off the diagram's stable component keys,
        // so Mermaid/PlantUML/DOT never choke on GUIDs or names with punctuation.
        private static Dictionary<string, string> NodeIds(IReadOnlyList<ArchNode> nodes)
        {
            var map = new Dictionary<string, string>();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Key != null && !map.ContainsKey(nodes[i].Key))
                    map[nodes[i].Key] = "n" + i.ToString(CultureInfo.InvariantCulture);
            return map;
        }

        // =====================================================================================
        // Mermaid
        // =====================================================================================

        public static string Mermaid(ArchDiagram d, DiagramOptions options = null)
        {
            d = d ?? new ArchDiagram();
            options = options ?? DiagramOptions.Default();
            var nodes = d.VisibleNodes(options);
            var edges = d.VisibleEdges(options);
            var ids = NodeIds(nodes);

            var sb = new StringBuilder();
            sb.Append("graph ").AppendLine(options.Direction == DiagramDirection.TopToBottom ? "TD" : "LR");

            if (options.Layout == DiagramLayout.Layered)
            {
                int s = 0;
                foreach (var layer in d.NodesByLayer(options))
                {
                    sb.Append("  subgraph L").Append(s++).Append("[\"").Append(MerLbl(layer.Key)).AppendLine("\"]");
                    foreach (var n in layer.Value)
                        sb.Append("    ").Append(ids[n.Key]).Append("[\"").Append(MerLbl(NodeText(n))).AppendLine("\"]");
                    sb.AppendLine("  end");
                }
            }
            else
            {
                foreach (var n in nodes)
                    sb.Append("  ").Append(ids[n.Key]).Append("[\"").Append(MerLbl(NodeText(n))).AppendLine("\"]");
            }

            foreach (var e in edges)
                sb.Append("  ").Append(ids[e.FromKey]).Append(" --> ").AppendLine(ids[e.ToKey]);

            return sb.ToString();
        }

        // Mermaid label escaping: quotes break the node string; keep it single-line.
        private static string MerLbl(string s) =>
            (s ?? "").Replace("\"", "&quot;").Replace("\r", " ").Replace("\n", " ");

        private static string NodeText(ArchNode n) =>
            string.IsNullOrWhiteSpace(n.Name) ? n.TypeLabel : $"{n.Name} ({n.TypeLabel})";

        // =====================================================================================
        // PlantUML
        // =====================================================================================

        public static string PlantUml(ArchDiagram d, DiagramOptions options = null)
        {
            d = d ?? new ArchDiagram();
            options = options ?? DiagramOptions.Default();
            var nodes = d.VisibleNodes(options);
            var edges = d.VisibleEdges(options);
            var ids = NodeIds(nodes);

            var sb = new StringBuilder();
            sb.AppendLine("@startuml");
            sb.Append("left to right direction").AppendLine();
            if (!string.IsNullOrWhiteSpace(d.DisplayTitle))
                sb.Append("title ").AppendLine(PumlLine(d.DisplayTitle));

            if (options.Layout == DiagramLayout.Layered)
            {
                foreach (var layer in d.NodesByLayer(options))
                {
                    sb.Append("package \"").Append(PumlLine(layer.Key)).AppendLine("\" {");
                    foreach (var n in layer.Value)
                        sb.Append("  rectangle \"").Append(PumlLine(NodeText(n))).Append("\" as ").AppendLine(ids[n.Key]);
                    sb.AppendLine("}");
                }
            }
            else
            {
                foreach (var n in nodes)
                    sb.Append("rectangle \"").Append(PumlLine(NodeText(n))).Append("\" as ").AppendLine(ids[n.Key]);
            }

            foreach (var e in edges)
                sb.Append(ids[e.FromKey]).Append(" --> ").AppendLine(ids[e.ToKey]);

            sb.AppendLine("@enduml");
            return sb.ToString();
        }

        private static string PumlLine(string s) =>
            (s ?? "").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ");

        // =====================================================================================
        // DOT / Graphviz
        // =====================================================================================

        public static string Dot(ArchDiagram d, DiagramOptions options = null)
        {
            d = d ?? new ArchDiagram();
            options = options ?? DiagramOptions.Default();
            var nodes = d.VisibleNodes(options);
            var edges = d.VisibleEdges(options);
            var ids = NodeIds(nodes);

            var sb = new StringBuilder();
            sb.AppendLine("digraph architecture {");
            sb.Append("  rankdir=").Append(options.Direction == DiagramDirection.TopToBottom ? "TB" : "LR").AppendLine(";");
            sb.AppendLine("  node [shape=box, style=rounded, fontname=\"Segoe UI\"];");

            if (options.Layout == DiagramLayout.Layered)
            {
                int c = 0;
                foreach (var layer in d.NodesByLayer(options))
                {
                    sb.Append("  subgraph cluster_").Append(c++).AppendLine(" {");
                    sb.Append("    label=\"").Append(DotStr(layer.Key)).AppendLine("\";");
                    foreach (var n in layer.Value)
                        sb.Append("    ").Append(ids[n.Key]).Append(" [label=\"").Append(DotStr(NodeText(n))).AppendLine("\"];");
                    sb.AppendLine("  }");
                }
            }
            else
            {
                foreach (var n in nodes)
                    sb.Append("  ").Append(ids[n.Key]).Append(" [label=\"").Append(DotStr(NodeText(n))).AppendLine("\"];");
            }

            foreach (var e in edges)
                sb.Append("  ").Append(ids[e.FromKey]).Append(" -> ").Append(ids[e.ToKey]).AppendLine(";");

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string DotStr(string s) =>
            (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");

        // =====================================================================================
        // Markdown (fenced Mermaid + a per-layer legend table)
        // =====================================================================================

        public static string Markdown(ArchDiagram d, DiagramOptions options = null)
        {
            d = d ?? new ArchDiagram();
            var sb = new StringBuilder();

            sb.Append("# ").AppendLine(Md(d.DisplayTitle));
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(d.BrandingHeader))
                sb.Append("_").Append(Md(d.BrandingHeader)).AppendLine("_").AppendLine();

            sb.Append("- **Solution:** ").AppendLine(Md(d.SolutionName ?? ""));
            sb.Append("- **Unique name:** ").AppendLine(Md(d.UniqueName ?? ""));
            sb.Append("- **Version:** ").AppendLine(Md(d.Version ?? ""));
            sb.Append("- **Publisher:** ").AppendLine(Md(d.Publisher ?? ""));
            sb.Append("- **Managed:** ").AppendLine(d.IsManaged ? "Yes" : "No");
            sb.Append("- **Generated (UTC):** ")
              .AppendLine(d.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
            sb.AppendLine();

            foreach (var note in d.Notes ?? new List<string>())
                sb.Append("> ").AppendLine(Md(note)).AppendLine();

            sb.AppendLine("## Diagram").AppendLine();
            sb.AppendLine("```mermaid");
            sb.Append(Mermaid(d, options).TrimEnd()).AppendLine();
            sb.AppendLine("```").AppendLine();

            var counts = d.LayerCounts().ToList();
            if (counts.Count > 0)
            {
                sb.AppendLine("## Legend").AppendLine();
                sb.AppendLine("| Layer | Components |");
                sb.AppendLine("|---|---|");
                foreach (var kv in counts)
                    sb.Append("| ").Append(Md(kv.Key)).Append(" | ").Append(kv.Value).AppendLine(" |");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string Md(string s) =>
            (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

        // =====================================================================================
        // JSON (hand-rolled BCL-only so the unit-test compile set stays dependency-free)
        // =====================================================================================

        public static string Json(ArchDiagram d, DiagramOptions options = null)
        {
            d = d ?? new ArchDiagram();
            options = options ?? DiagramOptions.Default();
            var nodes = d.VisibleNodes(options);
            var edges = d.VisibleEdges(options);

            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"title\":").Append(J(d.DisplayTitle)).Append(',');
            sb.Append("\"solutionName\":").Append(J(d.SolutionName)).Append(',');
            sb.Append("\"uniqueName\":").Append(J(d.UniqueName)).Append(',');
            sb.Append("\"version\":").Append(J(d.Version)).Append(',');
            sb.Append("\"isManaged\":").Append(d.IsManaged ? "true" : "false").Append(',');
            sb.Append("\"generatedUtc\":")
              .Append(J(d.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture))).Append(',');

            sb.Append("\"nodes\":[");
            for (int i = 0; i < nodes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var n = nodes[i];
                sb.Append('{')
                  .Append("\"key\":").Append(J(n.Key)).Append(',')
                  .Append("\"name\":").Append(J(n.Name)).Append(',')
                  .Append("\"type\":").Append(J(n.TypeLabel)).Append(',')
                  .Append("\"layer\":").Append(J(n.Layer))
                  .Append('}');
            }
            sb.Append("],\"edges\":[");
            for (int i = 0; i < edges.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var e = edges[i];
                sb.Append('{')
                  .Append("\"from\":").Append(J(e.FromKey)).Append(',')
                  .Append("\"to\":").Append(J(e.ToKey)).Append(',')
                  .Append("\"label\":").Append(J(e.Label))
                  .Append('}');
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

        // =====================================================================================
        // HTML (self-contained, theme-aware) — hand-laid-out inline SVG (offline, no external engine)
        // plus the Mermaid source in a <details> for users who want to re-render elsewhere.
        // =====================================================================================

        private const int MaxNodesPerLayer = 40; // keep the static SVG readable; note truncation

        public static string Html(ArchDiagram d, DiagramOptions options = null)
        {
            d = d ?? new ArchDiagram();
            options = options ?? DiagramOptions.Default();
            var title = d.DisplayTitle;

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            sb.Append("<title>").Append(H(title)).AppendLine("</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(":root{--bg:#ffffff;--fg:#1b1b1b;--muted:#5a5a5a;--line:#c7c7d2;--accent:#2b2b40;--node:#f3f3f7;--edge:#8a8a9a;}");
            sb.AppendLine("@media (prefers-color-scheme:dark){:root{--bg:#16161d;--fg:#e8e8ea;--muted:#a2a2ad;--line:#44445a;--accent:#c9c9e6;--node:#22222c;--edge:#6a6a80;}}");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:var(--bg);color:var(--fg);}");
            sb.AppendLine(".wrap{max-width:1200px;margin:0 auto;padding:24px 20px;}");
            sb.AppendLine("h1{font-size:22px;margin:0 0 4px;} h2{font-size:16px;color:var(--accent);margin:22px 0 8px;}");
            sb.AppendLine(".brand{color:var(--muted);font-style:italic;margin:0 0 10px;}");
            sb.AppendLine(".meta{color:var(--muted);font-size:13px;margin:0 0 6px;}");
            sb.AppendLine(".note{color:var(--muted);border-left:3px solid var(--line);padding:4px 10px;margin:8px 0;}");
            sb.AppendLine(".diagram{overflow:auto;border:1px solid var(--line);border-radius:8px;padding:8px;background:var(--bg);}");
            sb.AppendLine("svg .layer-box{fill:none;stroke:var(--line);stroke-dasharray:4 3;}");
            sb.AppendLine("svg .layer-label{fill:var(--muted);font:600 12px Segoe UI,Arial,sans-serif;}");
            sb.AppendLine("svg .node-box{fill:var(--node);stroke:var(--line);}");
            sb.AppendLine("svg .node-label{fill:var(--fg);font:12px Segoe UI,Arial,sans-serif;}");
            sb.AppendLine("svg .edge{stroke:var(--edge);stroke-width:1.2;fill:none;}");
            sb.AppendLine("table{border-collapse:collapse;margin:8px 0;font-size:13px;} th,td{border:1px solid var(--line);padding:4px 9px;text-align:left;} th{background:var(--node);}");
            sb.AppendLine("details{margin-top:14px;} pre{background:var(--node);border:1px solid var(--line);border-radius:6px;padding:10px;overflow:auto;font-size:12px;}");
            sb.AppendLine("</style></head><body><div class=\"wrap\">");

            sb.Append("<h1>").Append(H(title)).AppendLine("</h1>");
            if (!string.IsNullOrWhiteSpace(d.BrandingHeader))
                sb.Append("<p class=\"brand\">").Append(H(d.BrandingHeader)).AppendLine("</p>");
            sb.Append("<p class=\"meta\">Solution: <strong>").Append(H(d.SolutionName ?? "")).Append("</strong> · Version: <strong>")
              .Append(H(d.Version ?? "")).Append("</strong> · ").Append(d.IsManaged ? "Managed" : "Unmanaged")
              .Append(" · Generated (UTC): ").Append(H(d.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)))
              .AppendLine("</p>");

            foreach (var note in d.Notes ?? new List<string>())
                sb.Append("<div class=\"note\">").Append(H(note)).AppendLine("</div>");

            sb.AppendLine("<h2>Diagram</h2>");
            sb.Append("<div class=\"diagram\">").Append(Svg(d, options)).AppendLine("</div>");

            var counts = d.LayerCounts().ToList();
            if (counts.Count > 0)
            {
                sb.AppendLine("<h2>Legend</h2><table><thead><tr><th>Layer</th><th>Components</th></tr></thead><tbody>");
                foreach (var kv in counts)
                    sb.Append("<tr><td>").Append(H(kv.Key)).Append("</td><td>").Append(kv.Value).AppendLine("</td></tr>");
                sb.AppendLine("</tbody></table>");
            }

            sb.AppendLine("<details><summary>Mermaid source</summary><pre>")
              .Append(H(Mermaid(d, options).TrimEnd())).AppendLine("</pre></details>");

            sb.AppendLine("</div></body></html>");
            return sb.ToString();
        }

        private static string H(string value)
        {
            value = value ?? "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        // =====================================================================================
        // Inline SVG: deterministic layered layout — each layer is a column, nodes stack vertically.
        // Edges are straight lines between node box centers with an arrowhead marker.
        // =====================================================================================

        public static string Svg(ArchDiagram d, DiagramOptions options = null)
        {
            d = d ?? new ArchDiagram();
            options = options ?? DiagramOptions.Default();

            const int nodeW = 170, nodeH = 30, vGap = 10, colGap = 70, padX = 16, padY = 30, headerH = 22;

            // Column layout: layered → one column per layer; dependency graph → a single column split into rows.
            List<KeyValuePair<string, List<ArchNode>>> columns;
            if (options.Layout == DiagramLayout.Layered)
                columns = d.NodesByLayer(options).ToList();
            else
                columns = new List<KeyValuePair<string, List<ArchNode>>>
                {
                    new KeyValuePair<string, List<ArchNode>>("", d.VisibleNodes(options).ToList())
                };

            // Assign positions.
            var pos = new Dictionary<string, (double cx, double cy, double x, double y)>();
            double x = padX;
            double maxColHeight = 0;
            var truncated = false;
            var colRects = new List<(double x, double y, double w, double h, string label)>();

            foreach (var col in columns)
            {
                var shown = col.Value.Take(MaxNodesPerLayer).ToList();
                if (col.Value.Count > shown.Count) truncated = true;
                double y = padY + headerH;
                foreach (var n in shown)
                {
                    if (!pos.ContainsKey(n.Key))
                        pos[n.Key] = (x + nodeW / 2.0, y + nodeH / 2.0, x, y);
                    y += nodeH + vGap;
                }
                double colH = headerH + shown.Count * (nodeH + vGap);
                if (colH > maxColHeight) maxColHeight = colH;
                colRects.Add((x - 6, padY - 4, nodeW + 12, colH + 8, col.Key));
                x += nodeW + colGap;
            }

            double width = Math.Max(x - colGap + padX, nodeW + 2 * padX);
            double height = padY + headerH + maxColHeight + padY;

            var sb = new StringBuilder();
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 ")
              .Append(Num(width)).Append(' ').Append(Num(height)).Append("\" width=\"").Append(Num(width))
              .Append("\" height=\"").Append(Num(height)).Append("\" role=\"img\">");
            sb.Append("<defs><marker id=\"arrow\" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerWidth=\"7\" markerHeight=\"7\" orient=\"auto-start-reverse\">")
              .Append("<path d=\"M0,0 L10,5 L0,10 z\" fill=\"var(--edge)\"/></marker></defs>");

            // layer boxes + labels (only in layered layout, where labels exist)
            foreach (var r in colRects)
            {
                if (!string.IsNullOrEmpty(r.label))
                {
                    sb.Append("<rect class=\"layer-box\" x=\"").Append(Num(r.x)).Append("\" y=\"").Append(Num(r.y))
                      .Append("\" width=\"").Append(Num(r.w)).Append("\" height=\"").Append(Num(r.h))
                      .Append("\" rx=\"6\"/>");
                    sb.Append("<text class=\"layer-label\" x=\"").Append(Num(r.x + 6)).Append("\" y=\"").Append(Num(r.y + 14))
                      .Append("\">").Append(H(r.label)).Append("</text>");
                }
            }

            // edges first (so nodes paint on top)
            foreach (var e in d.VisibleEdges(options))
            {
                if (!pos.TryGetValue(e.FromKey, out var a) || !pos.TryGetValue(e.ToKey, out var b)) continue;
                sb.Append("<path class=\"edge\" marker-end=\"url(#arrow)\" d=\"M")
                  .Append(Num(a.cx)).Append(' ').Append(Num(a.cy)).Append(" L").Append(Num(b.cx)).Append(' ').Append(Num(b.cy)).Append("\"/>");
            }

            // nodes
            foreach (var col in columns)
            {
                foreach (var n in col.Value.Take(MaxNodesPerLayer))
                {
                    if (!pos.TryGetValue(n.Key, out var p)) continue;
                    sb.Append("<rect class=\"node-box\" x=\"").Append(Num(p.x)).Append("\" y=\"").Append(Num(p.y))
                      .Append("\" width=\"").Append(nodeW).Append("\" height=\"").Append(nodeH).Append("\" rx=\"5\"/>");
                    sb.Append("<text class=\"node-label\" x=\"").Append(Num(p.x + 8)).Append("\" y=\"").Append(Num(p.y + nodeH / 2.0 + 4))
                      .Append("\">").Append(H(Truncate(NodeText(n), 26))).Append("</text>");
                }
            }

            if (truncated)
                sb.Append("<text class=\"layer-label\" x=\"").Append(Num(padX)).Append("\" y=\"").Append(Num(height - 8))
                  .Append("\">Some layers truncated to ").Append(MaxNodesPerLayer).Append(" nodes for readability.</text>");

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static string Truncate(string s, int max)
        {
            s = s ?? "";
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }
    }
}
