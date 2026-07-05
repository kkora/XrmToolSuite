using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace XrmToolSuite.PluginDependencyGraph.Graph
{
    /// <summary>
    /// SDK-free, deterministic emitters for the plugin dependency graph: Mermaid, GraphML (round-trips
    /// node/edge types), SVG (simple layered layout), JSON and self-contained HTML. BCL-only, so they are
    /// unit-testable and headless-safe. A secure-config value is never present in the graph, so it can
    /// never appear in any output here.
    /// </summary>
    public static class PluginGraphEmitters
    {
        // ---- shared helpers -------------------------------------------------------------------

        internal static string ColorFor(PluginNodeType t)
        {
            switch (t)
            {
                case PluginNodeType.Solution: return "#0f766e";
                case PluginNodeType.Assembly: return "#2563eb";
                case PluginNodeType.PluginType: return "#7c3aed";
                case PluginNodeType.Step: return "#d13438";
                case PluginNodeType.Image: return "#f7871f";
                case PluginNodeType.Table: return "#12a150";
                case PluginNodeType.Message: return "#0891b2";
                case PluginNodeType.CustomApi: return "#be185d";
                case PluginNodeType.Config: return "#8a6d3b";
                default: return "#8b95ad";
            }
        }

        // Fixed left-to-right column per node type so the layout is deterministic.
        private static int Column(PluginNodeType t)
        {
            switch (t)
            {
                case PluginNodeType.Solution: return 0;
                case PluginNodeType.Assembly: return 1;
                case PluginNodeType.CustomApi: return 1;
                case PluginNodeType.PluginType: return 2;
                case PluginNodeType.Step: return 3;
                default: return 4; // Image, Table, Message, Config
            }
        }

        internal sealed class Placed
        {
            public PluginNode Node;
            public int Col;
            public double X, Y;
        }

        /// <summary>Deterministic layered layout shared by the SVG and PNG renderers.</summary>
        internal static List<Placed> Layout(PluginGraph g, int colWidth, int rowHeight, int marginX, int marginY,
            out int width, out int height)
        {
            var placed = g.Nodes
                .Select(n => new Placed { Node = n, Col = Column(n.Type) })
                .OrderBy(p => p.Col)
                .ThenBy(p => (int)p.Node.Type)
                .ThenBy(p => p.Node.Label ?? p.Node.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.Node.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rowInCol = new Dictionary<int, int>();
            int maxCol = 0;
            foreach (var p in placed)
            {
                int row = rowInCol.TryGetValue(p.Col, out var r) ? r : 0;
                rowInCol[p.Col] = row + 1;
                p.X = marginX + p.Col * colWidth;
                p.Y = marginY + row * rowHeight;
                maxCol = Math.Max(maxCol, p.Col);
            }

            int maxRows = rowInCol.Count == 0 ? 1 : rowInCol.Values.Max();
            width = marginX * 2 + maxCol * colWidth + 200;
            height = marginY * 2 + Math.Max(1, maxRows) * rowHeight;
            return placed;
        }

        // ---- Mermaid --------------------------------------------------------------------------

        public static string Mermaid(PluginGraph g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            var sb = new StringBuilder();
            sb.AppendLine("flowchart LR");

            var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int i = 0;
            foreach (var n in g.Nodes) idMap[n.Id] = "n" + (i++).ToString(CultureInfo.InvariantCulture);

            foreach (var n in g.Nodes)
            {
                var shape = ShapeOpen(n.Type);
                sb.AppendLine($"    {idMap[n.Id]}{shape.open}\"{MmEscape(n.Label)}\"{shape.close}");
                sb.AppendLine($"    class {idMap[n.Id]} t{(int)n.Type};");
            }

            foreach (var e in g.Edges)
            {
                if (!idMap.ContainsKey(e.FromId) || !idMap.ContainsKey(e.ToId)) continue;
                var label = string.IsNullOrEmpty(e.Kind) ? "-->" : $"-- {MmEscape(e.Kind)} -->";
                sb.AppendLine($"    {idMap[e.FromId]} {label} {idMap[e.ToId]}");
            }

            foreach (PluginNodeType t in Enum.GetValues(typeof(PluginNodeType)))
                sb.AppendLine($"    classDef t{(int)t} fill:{ColorFor(t)},stroke:#333,color:#fff;");

            return sb.ToString();
        }

        private static (string open, string close) ShapeOpen(PluginNodeType t)
        {
            switch (t)
            {
                case PluginNodeType.Solution: return ("[[", "]]");
                case PluginNodeType.Assembly: return ("[(", ")]");
                case PluginNodeType.Step: return ("{{", "}}");
                case PluginNodeType.Table: return ("[", "]");
                case PluginNodeType.Message: return ("([", "])");
                case PluginNodeType.CustomApi: return (">", "]");
                default: return ("(", ")");
            }
        }

        private static string MmEscape(string s) =>
            (s ?? "").Replace("\"", "'").Replace("\r", " ").Replace("\n", " ").Replace("[", "(").Replace("]", ")");

        // ---- GraphML (round-trips node type + edge kind) --------------------------------------

        public static string GraphML(PluginGraph g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int i = 0;
            foreach (var n in g.Nodes) idMap[n.Id] = "n" + (i++).ToString(CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\">");
            sb.AppendLine("  <key id=\"d_label\" for=\"node\" attr.name=\"label\" attr.type=\"string\"/>");
            sb.AppendLine("  <key id=\"d_type\" for=\"node\" attr.name=\"type\" attr.type=\"string\"/>");
            sb.AppendLine("  <key id=\"d_managed\" for=\"node\" attr.name=\"managed\" attr.type=\"boolean\"/>");
            sb.AppendLine("  <key id=\"d_kind\" for=\"edge\" attr.name=\"kind\" attr.type=\"string\"/>");
            sb.AppendLine("  <graph edgedefault=\"directed\">");
            foreach (var n in g.Nodes)
            {
                sb.AppendLine($"    <node id=\"{idMap[n.Id]}\">" +
                              $"<data key=\"d_label\">{X(n.Label)}</data>" +
                              $"<data key=\"d_type\">{n.Type}</data>" +
                              $"<data key=\"d_managed\">{(n.IsManaged ? "true" : "false")}</data></node>");
            }
            foreach (var e in g.Edges)
                if (idMap.ContainsKey(e.FromId) && idMap.ContainsKey(e.ToId))
                    sb.AppendLine($"    <edge source=\"{idMap[e.FromId]}\" target=\"{idMap[e.ToId]}\">" +
                                  $"<data key=\"d_kind\">{X(e.Kind)}</data></edge>");
            sb.AppendLine("  </graph>");
            sb.AppendLine("</graphml>");
            return sb.ToString();
        }

        // ---- SVG (deterministic layered layout) -----------------------------------------------

        public static string Svg(PluginGraph g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            const int colWidth = 220, rowHeight = 34, marginX = 24, marginY = 24, boxW = 180, boxH = 22;
            var placed = Layout(g, colWidth, rowHeight, marginX, marginY, out int width, out int height);
            var pos = placed.ToDictionary(p => p.Node.Id, p => p, StringComparer.OrdinalIgnoreCase);

            string N(double d) => d.ToString("0.#", CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" font-family=\"Segoe UI, Arial, sans-serif\">");
            sb.AppendLine("  <rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

            foreach (var e in g.Edges)
            {
                if (!pos.TryGetValue(e.FromId, out var a) || !pos.TryGetValue(e.ToId, out var b)) continue;
                sb.AppendLine($"  <line x1=\"{N(a.X + boxW)}\" y1=\"{N(a.Y + boxH / 2.0)}\" x2=\"{N(b.X)}\" y2=\"{N(b.Y + boxH / 2.0)}\" stroke=\"#c3ccda\" stroke-width=\"1\"/>");
            }
            foreach (var p in placed)
            {
                var fill = ColorFor(p.Node.Type);
                sb.AppendLine($"  <rect x=\"{N(p.X)}\" y=\"{N(p.Y)}\" width=\"{boxW}\" height=\"{boxH}\" rx=\"4\" fill=\"{fill}\"/>");
                sb.AppendLine($"  <text x=\"{N(p.X + 6)}\" y=\"{N(p.Y + 15)}\" font-size=\"10\" fill=\"#ffffff\">{X(Trim(p.Node.Label, 26))}</text>");
            }
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        // ---- JSON (hand-rolled, BCL-only) -----------------------------------------------------

        public static string Json(PluginGraph g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append("\"nodeCount\":").Append(g.Nodes.Count).Append(',');
            sb.Append("\"edgeCount\":").Append(g.Edges.Count).Append(',');
            sb.Append("\"nodes\":[");
            for (int i = 0; i < g.Nodes.Count; i++)
            {
                var n = g.Nodes[i];
                if (i > 0) sb.Append(',');
                sb.Append('{');
                sb.Append("\"id\":").Append(Q(n.Id)).Append(',');
                sb.Append("\"type\":").Append(Q(n.Type.ToString())).Append(',');
                sb.Append("\"label\":").Append(Q(n.Label)).Append(',');
                sb.Append("\"managed\":").Append(n.IsManaged ? "true" : "false").Append(',');
                sb.Append("\"props\":{");
                bool first = true;
                foreach (var kv in n.Props.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!first) sb.Append(',');
                    sb.Append(Q(kv.Key)).Append(':').Append(Q(kv.Value));
                    first = false;
                }
                sb.Append('}');
                sb.Append('}');
            }
            sb.Append("],\"edges\":[");
            for (int i = 0; i < g.Edges.Count; i++)
            {
                var e = g.Edges[i];
                if (i > 0) sb.Append(',');
                sb.Append('{');
                sb.Append("\"from\":").Append(Q(e.FromId)).Append(',');
                sb.Append("\"to\":").Append(Q(e.ToId)).Append(',');
                sb.Append("\"kind\":").Append(Q(e.Kind));
                sb.Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        // ---- HTML (self-contained) ------------------------------------------------------------

        public static string Html(PluginGraph g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            var sb = new StringBuilder();
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1f2937;background:#fff}");
            sb.AppendLine("h1{font-size:20px;margin:0 0 4px}h2{font-size:15px;margin:22px 0 8px;border-bottom:1px solid #e5e7eb;padding-bottom:4px}");
            sb.AppendLine(".muted{color:#6b7280;font-size:12px}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;font-size:12px;margin:6px 0 14px}");
            sb.AppendLine("th,td{border:1px solid #e5e7eb;padding:4px 8px;text-align:left;vertical-align:top}");
            sb.AppendLine("th{background:#f1f5f9}");
            sb.AppendLine(".diagram{overflow:auto;border:1px solid #e5e7eb;border-radius:6px;padding:8px;margin:8px 0}");
            sb.AppendLine(".pill{display:inline-block;padding:1px 6px;border-radius:8px;font-size:10px;color:#fff}");
            sb.AppendLine("</style>");

            sb.AppendLine("<h1>Plugin Dependency Graph</h1>");
            sb.AppendLine($"<div class=\"muted\">{g.Nodes.Count} node(s) &middot; {g.Edges.Count} edge(s) &middot; generated {DateTime.Now:yyyy-MM-dd HH:mm}</div>");

            sb.AppendLine("<h2>Diagram</h2>");
            sb.AppendLine("<div class=\"diagram\">");
            sb.AppendLine(Svg(g));
            sb.AppendLine("</div>");

            sb.AppendLine("<h2>Nodes</h2>");
            sb.AppendLine("<table><tr><th>Type</th><th>Label</th><th>Managed</th><th>Details</th></tr>");
            foreach (var n in g.Nodes)
            {
                var details = string.Join("; ", n.Props
                    .Where(p => !string.IsNullOrEmpty(p.Value))
                    .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(p => $"{X(p.Key)}={X(p.Value)}"));
                sb.AppendLine($"<tr><td><span class=\"pill\" style=\"background:{ColorFor(n.Type)}\">{n.Type}</span></td>" +
                              $"<td>{X(n.Label)}</td><td>{(n.IsManaged ? "Yes" : "No")}</td><td>{details}</td></tr>");
            }
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Edges</h2>");
            sb.AppendLine("<table><tr><th>From</th><th>Kind</th><th>To</th></tr>");
            foreach (var e in g.Edges)
            {
                var from = g.Node(e.FromId);
                var to = g.Node(e.ToId);
                sb.AppendLine($"<tr><td>{X(from?.Label ?? e.FromId)}</td><td>{X(e.Kind)}</td><td>{X(to?.Label ?? e.ToId)}</td></tr>");
            }
            sb.AppendLine("</table>");
            return sb.ToString();
        }

        // ---- export helpers -------------------------------------------------------------------

        public static void ExportMermaid(PluginGraph g, string path) => File.WriteAllText(path, Mermaid(g), Encoding.UTF8);
        public static void ExportGraphML(PluginGraph g, string path) => File.WriteAllText(path, GraphML(g), Encoding.UTF8);
        public static void ExportSvg(PluginGraph g, string path) => File.WriteAllText(path, Svg(g), Encoding.UTF8);
        public static void ExportJson(PluginGraph g, string path) => File.WriteAllText(path, Json(g), Encoding.UTF8);
        public static void ExportHtml(PluginGraph g, string path) => File.WriteAllText(path, Html(g), Encoding.UTF8);

        private static string X(string s) => WebUtility.HtmlEncode(s ?? "");
        private static string Trim(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 1) + "…";

        private static string Q(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder();
            sb.Append('"');
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < ' ') sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
