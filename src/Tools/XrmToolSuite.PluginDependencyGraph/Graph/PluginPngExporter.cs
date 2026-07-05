using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace XrmToolSuite.PluginDependencyGraph.Graph
{
    /// <summary>
    /// Rasterises the plugin dependency graph to a PNG with GDI+ (System.Drawing, a net48 GAC assembly —
    /// referenced, never shipped in the nupkg), reusing the deterministic layered layout from
    /// <see cref="PluginGraphEmitters.Layout"/>. Headless PNG without a browser. System.Drawing types are
    /// confined to this method body (never in a signature), so the model stays SDK/platform-free.
    /// </summary>
    public static class PluginPngExporter
    {
        private const int ColWidth = 220, RowHeight = 34, MarginX = 24, MarginY = 24, BoxW = 180, BoxH = 22;

        public static void Export(PluginGraph g, string path)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            var placed = PluginGraphEmitters.Layout(g, ColWidth, RowHeight, MarginX, MarginY, out int width, out int height);
            var pos = placed.ToDictionary(p => p.Node.Id, p => p, StringComparer.OrdinalIgnoreCase);

            using (var bmp = new Bitmap(Math.Max(1, width), Math.Max(1, height)))
            using (var gfx = Graphics.FromImage(bmp))
            using (var edgePen = new Pen(Color.FromArgb(195, 204, 218), 1f))
            using (var labelFont = new Font("Segoe UI", 8f))
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                gfx.Clear(Color.White);

                foreach (var e in g.Edges)
                {
                    if (!pos.TryGetValue(e.FromId, out var a) || !pos.TryGetValue(e.ToId, out var b)) continue;
                    gfx.DrawLine(edgePen,
                        (float)(a.X + BoxW), (float)(a.Y + BoxH / 2.0),
                        (float)b.X, (float)(b.Y + BoxH / 2.0));
                }

                foreach (var p in placed)
                {
                    using (var brush = new SolidBrush(ColorTranslator.FromHtml(PluginGraphEmitters.ColorFor(p.Node.Type))))
                        FillRounded(gfx, brush, (float)p.X, (float)p.Y, BoxW, BoxH, 4);
                    var label = Trim(p.Node.Label, 26);
                    gfx.DrawString(label, labelFont, whiteBrush, (float)p.X + 5, (float)p.Y + 4);
                }

                bmp.Save(path, ImageFormat.Png);
            }
        }

        private static void FillRounded(Graphics gfx, Brush brush, float x, float y, float w, float h, float r)
        {
            using (var gp = new GraphicsPath())
            {
                gp.AddArc(x, y, r * 2, r * 2, 180, 90);
                gp.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
                gp.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
                gp.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
                gp.CloseFigure();
                gfx.FillPath(brush, gp);
            }
        }

        private static string Trim(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}
