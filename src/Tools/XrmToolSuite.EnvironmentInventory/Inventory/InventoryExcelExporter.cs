using System;
using System.Collections.Generic;
using System.Globalization;
using ClosedXML.Excel;

namespace XrmToolSuite.EnvironmentInventory.Inventory
{
    /// <summary>
    /// Excel (.xlsx) exporter for a full <see cref="InventorySnapshot"/> grid — richer than the
    /// summary-level Word/PDF report. Produces a "Summary" worksheet (environment name, collected-on,
    /// total, and a category→count table) plus an "Items" worksheet with one row per
    /// <see cref="InventoryItem"/>. NEVER emits a secret/value column.
    ///
    /// This exporter references ClosedXML, so — unlike the BCL-only <see cref="InventoryExporter"/> —
    /// it must NOT be added to the SDK-free unit-test project. Every ClosedXML type stays a
    /// method-body local; the public signature is InventorySnapshot + string only.
    /// </summary>
    public static class InventoryExcelExporter
    {
        public static void Export(InventorySnapshot snapshot, string path)
        {
            snapshot = snapshot ?? new InventorySnapshot();

            using (var workbook = new XLWorkbook())
            {
                BuildSummarySheet(workbook, snapshot);
                BuildItemsSheet(workbook, snapshot);
                workbook.SaveAs(path);
            }
        }

        private static void BuildSummarySheet(XLWorkbook workbook, InventorySnapshot snapshot)
        {
            var ws = workbook.Worksheets.Add("Summary");

            ws.Cell(1, 1).Value = "Environment Inventory";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 15;

            ws.Cell(3, 1).Value = "Environment";
            ws.Cell(3, 2).Value = snapshot.EnvironmentName ?? "(unknown)";
            ws.Cell(4, 1).Value = "Collected (UTC)";
            ws.Cell(4, 2).Value = snapshot.CollectedOnUtc.ToUniversalTime()
                .ToString("u", CultureInfo.InvariantCulture);
            ws.Cell(5, 1).Value = "Total components";
            ws.Cell(5, 2).Value = snapshot.Total;

            ws.Range(3, 1, 5, 1).Style.Font.Bold = true;

            var row = 7;
            ws.Cell(row, 1).Value = "Category";
            ws.Cell(row, 2).Value = "Count";
            ws.Range(row, 1, row, 2).Style.Font.Bold = true;
            row++;

            foreach (var kv in snapshot.CountByCategory())
            {
                ws.Cell(row, 1).Value = kv.Key;
                ws.Cell(row, 2).Value = kv.Value;
                row++;
            }

            if (snapshot.UnavailableSources != null && snapshot.UnavailableSources.Count > 0)
            {
                row++;
                ws.Cell(row, 1).Value = "Unavailable sources";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Value = string.Join(", ", snapshot.UnavailableSources);
            }

            ws.Columns().AdjustToContents();
        }

        private static void BuildItemsSheet(XLWorkbook workbook, InventorySnapshot snapshot)
        {
            var ws = workbook.Worksheets.Add("Items");

            string[] header = { "Category", "Type", "Name", "Schema", "Managed", "Modified" };
            for (var c = 0; c < header.Length; c++)
                ws.Cell(1, c + 1).Value = header[c];
            ws.Range(1, 1, 1, header.Length).Style.Font.Bold = true;
            ws.SheetView.FreezeRows(1);

            var row = 2;
            foreach (var i in snapshot.Items ?? new List<InventoryItem>())
            {
                ws.Cell(row, 1).Value = i.Category ?? "";
                ws.Cell(row, 2).Value = i.ComponentType ?? "";
                ws.Cell(row, 3).Value = i.Name ?? "";
                ws.Cell(row, 4).Value = i.SchemaName ?? "";
                ws.Cell(row, 5).Value = i.IsManaged.HasValue
                    ? (i.IsManaged.Value ? "Managed" : "Unmanaged")
                    : "";
                ws.Cell(row, 6).Value = i.ModifiedOn.HasValue
                    ? i.ModifiedOn.Value.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)
                    : "";
                row++;
            }

            ws.Columns().AdjustToContents();
        }
    }
}
