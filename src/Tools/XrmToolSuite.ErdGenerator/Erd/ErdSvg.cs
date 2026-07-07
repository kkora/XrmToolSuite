using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// Emits a self-contained SVG of the ERD using a deterministic grid layout (a box per table, its
    /// selected columns listed, relationship lines between boxes). Pure string generation — no external
    /// deps, no System.Drawing — so it is headless-safe and unit-testable.
    /// </summary>
    public static class ErdSvg
    {
        private const int BoxWidth = 220;
        private const int HeaderHeight = 26;
        private const int RowHeight = 16;
        private const int HGap = 70;
        private const int VGap = 60;
        private const int Margin = 30;

        public static string Emit(ErdModel model, ColumnDisplay display)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var tables = model.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase).ToList();
            int count = Math.Max(1, tables.Count);
            int cols = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(count)));

            // Pre-compute each box's rendered columns + height, and its grid cell.
            var boxes = new List<Box>();
            var byName = new Dictionary<string, Box>(StringComparer.OrdinalIgnoreCase);
            int rowMaxHeight = 0;
            var rowHeights = new List<int>();
            for (int i = 0; i < tables.Count; i++)
            {
                var t = tables[i];
                var shown = ErdModel.SelectColumns(t, display);
                int h = HeaderHeight + Math.Max(1, shown.Count) * RowHeight + 8;
                var box = new Box { Table = t, Columns = shown, Height = h, Col = i % cols, Row = i / cols };
                boxes.Add(box);
                byName[t.LogicalName] = box;
                rowMaxHeight = Math.Max(rowMaxHeight, h);
                if (box.Col == cols - 1 || i == tables.Count - 1)
                {
                    rowHeights.Add(rowMaxHeight);
                    rowMaxHeight = 0;
                }
            }

            // Absolute positions from grid cells.
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

            // Strip XML-1.0-illegal chars (C0 controls other than tab/CR/LF, plus 0xFFFE/0xFFFF) before
            // encoding, so a stray control char in a display name can't make the SVG non-well-formed.
            string X(string s) => WebUtility.HtmlEncode(s == null ? "" : new string(s.Where(c =>
            {
                int u = c;
                return u == 0x09 || u == 0x0A || u == 0x0D || (u >= 0x20 && u <= 0xFFFD);
            }).ToArray()));
            string N(double d) => d.ToString("0.#", CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{totalWidth}\" height=\"{totalHeight}\" viewBox=\"0 0 {totalWidth} {totalHeight}\" font-family=\"Segoe UI, Arial, sans-serif\">");
            sb.AppendLine("  <rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>");

            // Relationship lines first (behind boxes) — connect box centers.
            foreach (var rel in model.Relationships)
            {
                if (!byName.TryGetValue(rel.FromTable, out var a) || !byName.TryGetValue(rel.ToTable, out var b))
                    continue;
                double ax = a.X + BoxWidth / 2.0, ay = a.Y + a.Height / 2.0;
                double bx = b.X + BoxWidth / 2.0, by = b.Y + b.Height / 2.0;
                var dash = rel.RelationType == "ManyToMany" ? " stroke-dasharray=\"5 4\"" : "";
                sb.AppendLine($"  <line x1=\"{N(ax)}\" y1=\"{N(ay)}\" x2=\"{N(bx)}\" y2=\"{N(by)}\" stroke=\"#9aa5b8\" stroke-width=\"1.2\"{dash}/>");
                double mx = (ax + bx) / 2, my = (ay + by) / 2;
                sb.AppendLine($"  <text x=\"{N(mx)}\" y=\"{N(my)}\" font-size=\"9\" fill=\"#6b7280\" text-anchor=\"middle\">{X(rel.LookupColumn ?? rel.SchemaName)}</text>");
            }

            // Boxes.
            foreach (var box in boxes)
            {
                var t = box.Table;
                string headerFill = t.IsCustom ? "#2563eb" : "#475569";
                sb.AppendLine($"  <g>");
                sb.AppendLine($"    <rect x=\"{box.X}\" y=\"{box.Y}\" width=\"{BoxWidth}\" height=\"{box.Height}\" rx=\"4\" fill=\"#ffffff\" stroke=\"#94a3b8\" stroke-width=\"1\"/>");
                sb.AppendLine($"    <rect x=\"{box.X}\" y=\"{box.Y}\" width=\"{BoxWidth}\" height=\"{HeaderHeight}\" rx=\"4\" fill=\"{headerFill}\"/>");
                sb.AppendLine($"    <text x=\"{box.X + 8}\" y=\"{box.Y + 17}\" font-size=\"11\" font-weight=\"bold\" fill=\"#ffffff\">{X(Trim(t.DisplayName ?? t.LogicalName, 28))}</text>");
                int y = box.Y + HeaderHeight + 12;
                foreach (var c in box.Columns)
                {
                    string marker = c.IsPrimaryId ? "PK " : c.IsLookup ? "FK " : "";
                    string weight = (c.IsPrimaryId || c.IsPrimaryName) ? " font-weight=\"bold\"" : "";
                    sb.AppendLine($"    <text x=\"{box.X + 8}\" y=\"{y}\" font-size=\"10\" fill=\"#1f2937\"{weight}>{X(marker + Trim(c.LogicalName, 22))}</text>");
                    sb.AppendLine($"    <text x=\"{box.X + BoxWidth - 8}\" y=\"{y}\" font-size=\"9\" fill=\"#9aa5b8\" text-anchor=\"end\">{X(Trim(c.Type, 14))}</text>");
                    y += RowHeight;
                }
                sb.AppendLine($"  </g>");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        public static void Export(ErdModel model, ColumnDisplay display, string path)
            => File.WriteAllText(path, Emit(model, display), Encoding.UTF8);

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
