using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace XrmToolSuite.EnvironmentInventory.Inventory
{
    /// <summary>
    /// Word (.docx) catalog for a filtered <see cref="InventorySnapshot"/> — the inventory's own report, NOT
    /// the suite's score/severity analyzer template. Title block, key-metric counts, and the records grouped
    /// by category in a table per category (Type / Name / Schema / Managed / Modified). NEVER emits a
    /// secret/value column.
    ///
    /// Built directly on the OpenXML SDK (already shipped with the ClosedXML chain — no extra dependency).
    /// Every OpenXML type stays a method-body local; the public signature is InventorySnapshot + string only.
    /// (Keep it out of the SDK-free unit-test project.)
    /// </summary>
    public static class InventoryWordExporter
    {
        private static readonly string[] Columns = { "Type", "Name", "Schema", "Managed", "Modified" };

        public static void Export(InventorySnapshot snapshot, string path)
        {
            snapshot = snapshot ?? new InventorySnapshot();

            using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                var main = doc.AddMainDocumentPart();
                var body = new Body();

                body.Append(Para("Environment Inventory", 36, true));
                body.Append(Para(snapshot.EnvironmentName ?? "(unknown environment)", 20, false, "808080"));
                body.Append(Para(
                    $"Collected (UTC): {snapshot.CollectedOnUtc.ToUniversalTime():u}    Components: {snapshot.Total}",
                    16, false, "A0A0A0"));

                body.Append(Para("Key metrics", 26, true));
                body.Append(Para($"• Total components: {snapshot.Total}", 20, false));
                foreach (var kv in snapshot.CountByCategory())
                    body.Append(Para($"• {kv.Key}: {kv.Value}", 20, false));

                var groups = (snapshot.Items ?? new List<InventoryItem>())
                    .GroupBy(i => string.IsNullOrWhiteSpace(i.Category) ? "(uncategorized)" : i.Category)
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var group in groups)
                {
                    body.Append(Para($"{group.Key} ({group.Count()})", 24, true));

                    var rows = group
                        .OrderBy(i => i.ComponentType, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(i => new[]
                        {
                            i.ComponentType ?? "",
                            i.Name ?? "",
                            i.SchemaName ?? "",
                            ManagedText(i),
                            i.ModifiedOn.HasValue
                                ? i.ModifiedOn.Value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                                : ""
                        });

                    body.Append(BuildTable(Columns, rows));
                }

                if (snapshot.UnavailableSources != null && snapshot.UnavailableSources.Count > 0)
                    body.Append(Para(
                        $"Unavailable sources (not collected): {string.Join(", ", snapshot.UnavailableSources)}",
                        16, false, "A0A0A0"));

                main.Document = new Document(body);
                main.Document.Save();
            }
        }

        private static Table BuildTable(string[] header, IEnumerable<string[]> rows)
        {
            var table = new Table();
            var props = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "E1E1E1" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "E1E1E1" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "E1E1E1" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "E1E1E1" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "E1E1E1" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "E1E1E1" }),
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });
            table.Append(props);

            table.Append(HeaderRow(header));
            foreach (var cells in rows)
                table.Append(DataRow(cells));
            return table;
        }

        private static TableRow HeaderRow(string[] cells)
        {
            var row = new TableRow();
            foreach (var text in cells)
                row.Append(Cell(text, bold: true, shadeHex: "2B2B40", textHex: "FFFFFF"));
            return row;
        }

        private static TableRow DataRow(string[] cells)
        {
            var row = new TableRow();
            foreach (var text in cells)
                row.Append(Cell(text, bold: false));
            return row;
        }

        private static TableCell Cell(string text, bool bold, string shadeHex = null, string textHex = null)
        {
            var cell = new TableCell();
            if (shadeHex != null)
                cell.Append(new TableCellProperties(
                    new Shading { Val = ShadingPatternValues.Clear, Fill = shadeHex }));
            cell.Append(Para(text, 16, bold, textHex));
            return cell;
        }

        private static Paragraph Para(string text, int halfPointSize, bool bold, string colorHex = null)
        {
            var runProps = new RunProperties(new FontSize { Val = halfPointSize.ToString() });
            if (bold) runProps.Append(new Bold());
            if (colorHex != null) runProps.Append(new Color { Val = colorHex });
            var run = new Run(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            run.RunProperties = runProps;
            return new Paragraph(run);
        }

        private static string ManagedText(InventoryItem i) =>
            i.IsManaged.HasValue ? (i.IsManaged.Value ? "Managed" : "Unmanaged") : "";
    }
}
