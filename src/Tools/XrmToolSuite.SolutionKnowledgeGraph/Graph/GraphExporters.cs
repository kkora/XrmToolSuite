using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace XrmToolSuite.SolutionKnowledgeGraph.Graph
{
    /// <summary>
    /// Encodes text for an XML/SVG body: strips characters illegal in XML 1.0 (C0 controls other than
    /// tab/CR/LF, and the two non-characters) before HTML-encoding, so a stray control char in a label
    /// can't make the emitted GraphML/SVG non-well-formed.
    /// </summary>
    internal static class XmlText
    {
        public static string Enc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // Valid XML 1.0 chars: 0x09, 0x0A, 0x0D, and 0x20..0xFFFD (excludes C0 controls and the
            // non-characters 0xFFFE/0xFFFF). Surrogates (<= 0xFFFD) pass through for well-formed pairs.
            var clean = new string(s.Where(c =>
            {
                int u = c;
                return u == 0x09 || u == 0x0A || u == 0x0D || (u >= 0x20 && u <= 0xFFFD);
            }).ToArray());
            return WebUtility.HtmlEncode(clean);
        }
    }

    /// <summary>Exports the graph to GraphML (a standard XML graph format read by yEd, Gephi, Cytoscape).</summary>
    public static class GraphMlExporter
    {
        public static string Build(GraphModel g)
        {
            var idMap = new Dictionary<string, string>();
            int i = 0;
            foreach (var n in g.Nodes) idMap[n.Id] = "n" + (i++);

            string X(string s) => XmlText.Enc(s);
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\">");
            sb.AppendLine("  <key id=\"d_label\" for=\"node\" attr.name=\"label\" attr.type=\"string\"/>");
            sb.AppendLine("  <key id=\"d_type\" for=\"node\" attr.name=\"type\" attr.type=\"string\"/>");
            sb.AppendLine("  <key id=\"d_kind\" for=\"edge\" attr.name=\"kind\" attr.type=\"string\"/>");
            sb.AppendLine("  <graph edgedefault=\"directed\">");
            foreach (var n in g.Nodes)
                sb.AppendLine($"    <node id=\"{idMap[n.Id]}\"><data key=\"d_label\">{X(n.Label)}</data><data key=\"d_type\">{X(n.Type)}</data></node>");
            foreach (var e in g.Edges)
                if (idMap.ContainsKey(e.From) && idMap.ContainsKey(e.To))
                    sb.AppendLine($"    <edge source=\"{idMap[e.From]}\" target=\"{idMap[e.To]}\"><data key=\"d_kind\">{X(e.Kind)}</data></edge>");
            sb.AppendLine("  </graph>");
            sb.AppendLine("</graphml>");
            return sb.ToString();
        }

        public static void Export(GraphModel g, string path) => File.WriteAllText(path, Build(g), Encoding.UTF8);
    }

    /// <summary>
    /// Exports the graph to SVG using a deterministic circular layout (nodes on a ring, edges as lines).
    /// Pure string generation (no System.Drawing), so it is unit-testable and headless-safe.
    /// </summary>
    public static class SvgExporter
    {
        public static string Build(GraphModel g)
        {
            var nodes = g.Nodes.ToList();
            int n = Math.Max(1, nodes.Count);
            double size = Math.Max(600, Math.Min(2400, 300 + n * 14));
            double cx = size / 2, cy = size / 2, r = size / 2 - 90;

            var pos = new Dictionary<string, (double x, double y)>();
            for (int i = 0; i < nodes.Count; i++)
            {
                double a = 2 * Math.PI * i / n;
                pos[nodes[i].Id] = (cx + r * Math.Cos(a), cy + r * Math.Sin(a));
            }

            string X(string s) => XmlText.Enc(s);
            // Invariant culture: coordinate attributes must use '.' as the decimal separator or the SVG is
            // invalid on comma-decimal locales (de-DE, fr-FR, …).
            string N(double d) => d.ToString("0.#", CultureInfo.InvariantCulture);
            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{N(size)}\" height=\"{N(size)}\" viewBox=\"0 0 {N(size)} {N(size)}\" font-family=\"Segoe UI, sans-serif\">");
            sb.AppendLine("  <rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");
            foreach (var e in g.Edges)
                if (pos.TryGetValue(e.From, out var a) && pos.TryGetValue(e.To, out var b))
                    sb.AppendLine($"  <line x1=\"{N(a.x)}\" y1=\"{N(a.y)}\" x2=\"{N(b.x)}\" y2=\"{N(b.y)}\" stroke=\"#c3ccda\" stroke-width=\"1\"/>");
            foreach (var node in nodes)
            {
                var p = pos[node.Id];
                sb.AppendLine($"  <circle cx=\"{N(p.x)}\" cy=\"{N(p.y)}\" r=\"6\" fill=\"{ColorFor(node.Type)}\"/>");
                sb.AppendLine($"  <text x=\"{N(p.x + 9)}\" y=\"{N(p.y + 4)}\" font-size=\"10\" fill=\"#333\">{X(Trim(node.Label))}</text>");
            }
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        public static void Export(GraphModel g, string path) => File.WriteAllText(path, Build(g), Encoding.UTF8);

        private static string Trim(string s) => string.IsNullOrEmpty(s) || s.Length <= 32 ? s : s.Substring(0, 31) + "…";

        internal static string ColorFor(string type)
        {
            switch (type)
            {
                case "Table": return "#2563eb";
                case "Form": return "#12a150";
                case "View": return "#0891b2";
                case "Plugin Step": return "#d13438";
                case "Web Resource": return "#f7871f";
                case "Workflow / Flow": return "#7c3aed";
                case "Security Role": return "#be185d";
                case "Model-driven App": return "#0f766e";
                default: return "#8b95ad";
            }
        }
    }
}
