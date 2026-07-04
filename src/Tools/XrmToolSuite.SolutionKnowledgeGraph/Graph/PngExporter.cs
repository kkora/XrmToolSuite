using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace XrmToolSuite.SolutionKnowledgeGraph.Graph
{
    /// <summary>
    /// Rasterises the graph to a PNG using the same deterministic circular layout as the SVG export,
    /// drawn with GDI+ (System.Drawing, available on net48/WinForms). Headless PNG without WebView2.
    /// </summary>
    public static class PngExporter
    {
        public static void Export(GraphModel g, string path)
        {
            var nodes = g.Nodes.ToList();
            int n = Math.Max(1, nodes.Count);
            int size = (int)Math.Max(700, Math.Min(3000, 320 + n * 16));
            double cx = size / 2.0, cy = size / 2.0, r = size / 2.0 - 100;

            var pos = new Dictionary<string, PointF>();
            for (int i = 0; i < nodes.Count; i++)
            {
                double a = 2 * Math.PI * i / n;
                pos[nodes[i].Id] = new PointF((float)(cx + r * Math.Cos(a)), (float)(cy + r * Math.Sin(a)));
            }

            using (var bmp = new Bitmap(size, size))
            using (var gfx = Graphics.FromImage(bmp))
            using (var edgePen = new Pen(Color.FromArgb(195, 204, 218), 1f))
            using (var labelFont = new Font("Segoe UI", 8f))
            using (var labelBrush = new SolidBrush(Color.FromArgb(51, 51, 51)))
            {
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.Clear(Color.White);

                foreach (var e in g.Edges)
                    if (pos.TryGetValue(e.From, out var a) && pos.TryGetValue(e.To, out var b))
                        gfx.DrawLine(edgePen, a, b);

                foreach (var node in nodes)
                {
                    var p = pos[node.Id];
                    using (var brush = new SolidBrush(ColorTranslator.FromHtml(SvgExporter.ColorFor(node.Type))))
                        gfx.FillEllipse(brush, p.X - 6, p.Y - 6, 12, 12);
                    var label = node.Label ?? "";
                    if (label.Length > 28) label = label.Substring(0, 27) + "…";
                    gfx.DrawString(label, labelFont, labelBrush, p.X + 8, p.Y - 6);
                }

                bmp.Save(path, ImageFormat.Png);
            }
        }
    }
}
