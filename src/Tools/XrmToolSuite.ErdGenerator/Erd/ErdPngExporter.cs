using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// Rasterises the ERD to a PNG with GDI+ (System.Drawing, a net48 GAC assembly — referenced, never
    /// shipped in the nupkg), using the same deterministic grid layout as <see cref="ErdSvg"/>. Headless
    /// PNG without a browser. System.Drawing types are confined to this method body (never in a signature),
    /// so the surrounding model stays SDK/platform-free and unit-testable.
    /// </summary>
    public static class ErdPngExporter
    {
        private const int BoxWidth = 220;
        private const int HeaderHeight = 26;
        private const int RowHeight = 16;
        private const int HGap = 70;
        private const int VGap = 60;
        private const int Margin = 30;

        public static void Export(ErdModel model, ColumnDisplay display, string path)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var tables = model.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase).ToList();
            int count = Math.Max(1, tables.Count);
            int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));

            var boxes = new List<Box>();
            var byName = new Dictionary<string, Box>(StringComparer.OrdinalIgnoreCase);
            int rowMaxHeight = 0;
            var rowHeights = new List<int>();
            for (int i = 0; i < tables.Count; i++)
            {
                var shown = ErdModel.SelectColumns(tables[i], display);
                int h = HeaderHeight + Math.Max(1, shown.Count) * RowHeight + 8;
                var box = new Box { Table = tables[i], Columns = shown, Height = h, Col = i % cols, Row = i / cols };
                boxes.Add(box);
                byName[tables[i].LogicalName] = box;
                rowMaxHeight = Math.Max(rowMaxHeight, h);
                if (box.Col == cols - 1 || i == tables.Count - 1) { rowHeights.Add(rowMaxHeight); rowMaxHeight = 0; }
            }

            var rowY = new int[rowHeights.Count];
            int acc = Margin;
            for (int r = 0; r < rowHeights.Count; r++) { rowY[r] = acc; acc += rowHeights[r] + VGap; }
            int totalHeight = (rowHeights.Count == 0 ? Margin : acc - VGap) + Margin;
            int totalWidth = Margin * 2 + cols * BoxWidth + (cols - 1) * HGap;

            foreach (var b in boxes)
            {
                b.X = Margin + b.Col * (BoxWidth + HGap);
                b.Y = rowY[b.Row];
            }

            using (var bmp = new Bitmap(Math.Max(1, totalWidth), Math.Max(1, totalHeight)))
            using (var gfx = Graphics.FromImage(bmp))
            using (var edgePen = new Pen(Color.FromArgb(154, 165, 184), 1.2f))
            using (var dashPen = new Pen(Color.FromArgb(154, 165, 184), 1.2f) { DashStyle = DashStyle.Dash })
            using (var boxPen = new Pen(Color.FromArgb(148, 163, 184), 1f))
            using (var titleFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var colFont = new Font("Segoe UI", 8f))
            using (var typeFont = new Font("Segoe UI", 7.5f))
            using (var labelFont = new Font("Segoe UI", 7f))
            using (var whiteBrush = new SolidBrush(Color.White))
            using (var textBrush = new SolidBrush(Color.FromArgb(31, 41, 55)))
            using (var typeBrush = new SolidBrush(Color.FromArgb(154, 165, 184)))
            using (var labelBrush = new SolidBrush(Color.FromArgb(107, 114, 128)))
            {
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                gfx.Clear(Color.White);

                foreach (var rel in model.Relationships)
                {
                    if (!byName.TryGetValue(rel.FromTable, out var a) || !byName.TryGetValue(rel.ToTable, out var b))
                        continue;
                    var pa = new PointF(a.X + BoxWidth / 2f, a.Y + a.Height / 2f);
                    var pb = new PointF(b.X + BoxWidth / 2f, b.Y + b.Height / 2f);
                    gfx.DrawLine(rel.RelationType == "ManyToMany" ? dashPen : edgePen, pa, pb);
                    var label = rel.LookupColumn ?? rel.SchemaName ?? "";
                    var mid = new PointF((pa.X + pb.X) / 2f, (pa.Y + pb.Y) / 2f);
                    gfx.DrawString(Trim(label, 24), labelFont, labelBrush, mid.X - 30, mid.Y - 6);
                }

                foreach (var box in boxes)
                {
                    var t = box.Table;
                    var rect = new Rectangle(box.X, box.Y, BoxWidth, box.Height);
                    gfx.FillRectangle(whiteBrush, rect);
                    gfx.DrawRectangle(boxPen, rect);
                    using (var header = new SolidBrush(t.IsCustom ? Color.FromArgb(37, 99, 235) : Color.FromArgb(71, 85, 105)))
                        gfx.FillRectangle(header, box.X, box.Y, BoxWidth, HeaderHeight);
                    gfx.DrawString(Trim(t.DisplayName ?? t.LogicalName, 28), titleFont, whiteBrush, box.X + 6, box.Y + 6);

                    int y = box.Y + HeaderHeight + 4;
                    foreach (var c in box.Columns)
                    {
                        string marker = c.IsPrimaryId ? "PK " : c.IsLookup ? "FK " : "";
                        gfx.DrawString(marker + Trim(c.LogicalName, 22), colFont, textBrush, box.X + 6, y);
                        var typeText = Trim(c.Type, 12);
                        var size = gfx.MeasureString(typeText, typeFont);
                        gfx.DrawString(typeText, typeFont, typeBrush, box.X + BoxWidth - 6 - size.Width, y);
                        y += RowHeight;
                    }
                }

                bmp.Save(path, ImageFormat.Png);
            }
        }

        private static string Trim(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 1) + "…";

        private sealed class Box
        {
            public ErdTable Table;
            public IReadOnlyList<ErdColumn> Columns;
            public int Height;
            public int Col, Row;
            public int X, Y;
        }
    }
}
