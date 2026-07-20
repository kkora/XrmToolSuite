using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace XrmToolSuite.EnvironmentInventory.Inventory
{
    /// <summary>
    /// Native PDF catalog for a filtered <see cref="InventorySnapshot"/> — the inventory's own report, NOT
    /// the suite's score/severity analyzer template. Renders a title block, key-metric counts, and the
    /// records themselves grouped by category (Type / Name / Schema / Managed / Modified). No risk gauge,
    /// no severity grid — an inventory is a catalog, not an assessment. NEVER emits a secret/value column.
    ///
    /// Like <see cref="InventoryExcelExporter"/>, this references the PdfSharp/MigraDoc-GDI chain, so every
    /// MigraDoc type stays a method-body local; the public signature is InventorySnapshot + string only.
    /// (Keep it out of the SDK-free unit-test project.)
    /// </summary>
    public static class InventoryPdfExporter
    {
        private static readonly Color HeaderFill = new Color(43, 43, 64);
        private static readonly Color GridLine = new Color(225, 225, 225);
        private static readonly Color Muted = new Color(130, 130, 130);

        public static void Export(InventorySnapshot snapshot, string path)
        {
            snapshot = snapshot ?? new InventorySnapshot();

            var doc = new Document();
            doc.Info.Title = $"Environment Inventory — {snapshot.EnvironmentName}";
            doc.Info.Author = "Environment Inventory";

            var normal = doc.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 8;

            var section = doc.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.6);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);

            var title = section.AddParagraph("Environment Inventory");
            title.Format.Font.Size = 20;
            title.Format.Font.Bold = true;
            title.Format.Font.Color = new Color(27, 27, 47);
            title.Format.SpaceAfter = Unit.FromPoint(2);

            var sub = section.AddParagraph(snapshot.EnvironmentName ?? "(unknown environment)");
            sub.Format.Font.Size = 10;
            sub.Format.Font.Color = new Color(90, 90, 90);

            var meta = section.AddParagraph(
                $"Collected (UTC): {snapshot.CollectedOnUtc.ToUniversalTime():u}    " +
                $"Components: {snapshot.Total}");
            meta.Format.Font.Size = 8;
            meta.Format.Font.Color = Muted;
            meta.Format.SpaceAfter = Unit.FromPoint(12);

            AddKeyMetrics(section, snapshot);
            AddCategoryTables(section, snapshot);

            if (snapshot.UnavailableSources != null && snapshot.UnavailableSources.Count > 0)
            {
                var note = section.AddParagraph(
                    $"Unavailable sources (not collected): {string.Join(", ", snapshot.UnavailableSources)}");
                note.Format.Font.Size = 7;
                note.Format.Font.Color = Muted;
                note.Format.SpaceBefore = Unit.FromPoint(14);
            }

            var renderer = new PdfDocumentRenderer(true) { Document = doc };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(path);
        }

        private static void AddKeyMetrics(Section section, InventorySnapshot snapshot)
        {
            var head = section.AddParagraph("Key metrics");
            head.Format.Font.Size = 12;
            head.Format.Font.Bold = true;
            head.Format.SpaceAfter = Unit.FromPoint(4);
            head.Format.KeepWithNext = true;

            var table = section.AddTable();
            table.Borders.Color = GridLine;
            table.Borders.Width = 0.5;
            table.AddColumn(Unit.FromCentimeter(11.4));
            table.AddColumn(Unit.FromCentimeter(6.0));

            AddMetricRow(table, "Total components", snapshot.Total.ToString(CultureInfo.InvariantCulture));
            foreach (var kv in snapshot.CountByCategory())
                AddMetricRow(table, kv.Key, kv.Value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AddMetricRow(Table table, string label, string value)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(label ?? "");
            var vp = row.Cells[1].AddParagraph(value ?? "");
            vp.Format.Font.Bold = true;
        }

        private static void AddCategoryTables(Section section, InventorySnapshot snapshot)
        {
            var groups = (snapshot.Items ?? new List<InventoryItem>())
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Category) ? "(uncategorized)" : i.Category)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var heading = section.AddParagraph($"{group.Key} ({group.Count()})");
                heading.Format.Font.Size = 12;
                heading.Format.Font.Bold = true;
                heading.Format.SpaceBefore = Unit.FromPoint(14);
                heading.Format.SpaceAfter = Unit.FromPoint(4);
                heading.Format.KeepWithNext = true;

                var table = section.AddTable();
                table.Borders.Color = GridLine;
                table.Borders.Width = 0.5;
                table.AddColumn(Unit.FromCentimeter(2.8)); // Type
                table.AddColumn(Unit.FromCentimeter(6.0)); // Name
                table.AddColumn(Unit.FromCentimeter(5.1)); // Schema
                table.AddColumn(Unit.FromCentimeter(1.6)); // Managed
                table.AddColumn(Unit.FromCentimeter(1.9)); // Modified

                var header = table.AddRow();
                header.Shading.Color = HeaderFill;
                header.HeadingFormat = true; // repeats on each page
                string[] cols = { "Type", "Name", "Schema", "Managed", "Modified" };
                for (int i = 0; i < cols.Length; i++)
                {
                    var hp = header.Cells[i].AddParagraph(cols[i]);
                    hp.Format.Font.Color = Colors.White;
                    hp.Format.Font.Bold = true;
                    hp.Format.Font.Size = 8;
                }

                foreach (var item in group.OrderBy(i => i.ComponentType, StringComparer.OrdinalIgnoreCase)
                                           .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var row = table.AddRow();
                    row.Cells[0].AddParagraph(item.ComponentType ?? "");
                    row.Cells[1].AddParagraph(item.Name ?? "");
                    row.Cells[2].AddParagraph(item.SchemaName ?? "");
                    row.Cells[3].AddParagraph(ManagedText(item));
                    row.Cells[4].AddParagraph(item.ModifiedOn.HasValue
                        ? item.ModifiedOn.Value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : "");
                }
            }
        }

        private static string ManagedText(InventoryItem i) =>
            i.IsManaged.HasValue ? (i.IsManaged.Value ? "Managed" : "Unmanaged") : "";
    }
}
