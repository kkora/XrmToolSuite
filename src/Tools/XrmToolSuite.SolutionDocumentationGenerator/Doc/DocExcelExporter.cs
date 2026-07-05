using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;

namespace XrmToolSuite.SolutionDocumentationGenerator.Doc
{
    /// <summary>
    /// Excel (.xlsx) exporter for a <see cref="SolutionDoc"/> via ClosedXML: an "Overview" sheet (solution
    /// metadata + component inventory) plus one worksheet per section, each carrying that section's tables.
    /// Every ClosedXML type stays a method-body local; the public signature is <see cref="SolutionDoc"/> +
    /// string only, so this file is excluded from the SDK-free unit-test compile set.
    /// </summary>
    public static class DocExcelExporter
    {
        public static void Export(SolutionDoc doc, string path)
        {
            doc = doc ?? new SolutionDoc();

            using (var wb = new XLWorkbook())
            {
                BuildOverview(wb, doc);

                var usedNames = new HashSet<string> { "Overview" };
                foreach (var section in doc.Sections ?? new List<DocSection>())
                {
                    var ws = wb.Worksheets.Add(SheetName(section.Title ?? section.Kind, usedNames));
                    int row = 1;

                    ws.Cell(row, 1).Value = section.Title ?? section.Kind;
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Font.FontSize = 14;
                    row += 2;

                    foreach (var note in section.Notes ?? new List<string>())
                    {
                        ws.Cell(row, 1).Value = note;
                        ws.Cell(row, 1).Style.Font.Italic = true;
                        row++;
                    }
                    if ((section.Notes?.Count ?? 0) > 0) row++;

                    if (!string.IsNullOrWhiteSpace(section.Body) &&
                        !string.Equals(section.Kind, SectionKinds.Diagrams, System.StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var line in section.Body.Replace("\r\n", "\n").Split('\n'))
                        {
                            ws.Cell(row, 1).Value = line;
                            row++;
                        }
                        row++;
                    }

                    foreach (var table in section.Tables ?? new List<DocTable>())
                    {
                        if (table.RowCount == 0) continue;
                        if (!string.IsNullOrWhiteSpace(table.Caption))
                        {
                            ws.Cell(row, 1).Value = table.Caption;
                            ws.Cell(row, 1).Style.Font.Bold = true;
                            row++;
                        }

                        for (int c = 0; c < table.Headers.Count; c++)
                            ws.Cell(row, c + 1).Value = table.Headers[c];
                        ws.Range(row, 1, row, System.Math.Max(1, table.Headers.Count)).Style.Font.Bold = true;
                        row++;

                        foreach (var r in table.Rows)
                        {
                            for (int c = 0; c < table.Headers.Count; c++)
                                ws.Cell(row, c + 1).Value = c < r.Count ? (r[c] ?? "") : "";
                            row++;
                        }
                        row++;
                    }

                    ws.Columns().AdjustToContents();
                }

                wb.SaveAs(path);
            }
        }

        private static void BuildOverview(XLWorkbook wb, SolutionDoc doc)
        {
            var ws = wb.Worksheets.Add("Overview");

            ws.Cell(1, 1).Value = doc.SolutionName ?? "Solution documentation";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 15;

            var meta = new (string, string)[]
            {
                ("Unique name", doc.UniqueName),
                ("Version", doc.Version),
                ("Publisher", doc.Publisher),
                ("Managed", doc.IsManaged ? "Yes" : "No"),
                ("Documentation mode", doc.ModeLabel),
                ("Generated (UTC)", doc.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)),
            };
            int row = 3;
            foreach (var (k, v) in meta)
            {
                ws.Cell(row, 1).Value = k;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Value = v ?? "";
                row++;
            }

            // Component inventory (from the Inventory section's first table, if present).
            var inv = doc.Section(SectionKinds.Inventory)?.Tables?.FirstOrDefault(t => t.RowCount > 0);
            if (inv != null)
            {
                row++;
                ws.Cell(row, 1).Value = "Component inventory";
                ws.Cell(row, 1).Style.Font.Bold = true;
                row++;
                for (int c = 0; c < inv.Headers.Count; c++)
                    ws.Cell(row, c + 1).Value = inv.Headers[c];
                ws.Range(row, 1, row, System.Math.Max(1, inv.Headers.Count)).Style.Font.Bold = true;
                row++;
                foreach (var r in inv.Rows)
                {
                    for (int c = 0; c < inv.Headers.Count; c++)
                        ws.Cell(row, c + 1).Value = c < r.Count ? (r[c] ?? "") : "";
                    row++;
                }
            }

            ws.Columns().AdjustToContents();
        }

        /// <summary>Excel worksheet names must be ≤31 chars, unique, and free of []:*?/\ characters.</summary>
        private static string SheetName(string desired, HashSet<string> used)
        {
            var cleaned = new string((desired ?? "Sheet")
                .Select(ch => "[]:*?/\\".IndexOf(ch) >= 0 ? ' ' : ch).ToArray()).Trim();
            if (cleaned.Length == 0) cleaned = "Sheet";
            if (cleaned.Length > 31) cleaned = cleaned.Substring(0, 31);

            var name = cleaned;
            int n = 2;
            while (used.Contains(name))
            {
                var suffix = " " + n++;
                name = cleaned.Length + suffix.Length > 31
                    ? cleaned.Substring(0, 31 - suffix.Length) + suffix
                    : cleaned + suffix;
            }
            used.Add(name);
            return name;
        }
    }
}
