using System;
using System.IO;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace XrmToolSuite.ErdGenerator.Erd
{
    /// <summary>
    /// Renders the ERD to a readable PDF with MigraDoc/PdfSharp (GDI build): a title, an embedded diagram
    /// image (rasterised via <see cref="ErdPngExporter"/>), a per-table section listing columns and keys,
    /// and a relationships table. MigraDoc/PdfSharp types are confined to this method body (never in a
    /// public signature). Requires the PdfSharp/MigraDoc-GDI chain shipped in the tool's Plugins folder.
    /// </summary>
    public static class ErdPdfExporter
    {
        public static void Export(ErdModel model, ColumnDisplay display, string path)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var doc = new Document();
            doc.Info.Title = "Dataverse ERD";
            doc.Info.Author = "XrmToolSuite ERD Generator";

            var normal = doc.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 9;

            var section = doc.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.4);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.4);

            var title = section.AddParagraph("Dataverse Entity-Relationship Diagram");
            title.Format.Font.Size = 18;
            title.Format.Font.Bold = true;
            title.Format.Font.Color = new Color(27, 27, 47);

            var sub = section.AddParagraph(
                $"{model.Tables.Count} table(s) · {model.Relationships.Count} relationship(s) · generated {DateTime.Now:yyyy-MM-dd HH:mm}");
            sub.Format.Font.Size = 9;
            sub.Format.Font.Color = new Color(120, 120, 120);
            sub.Format.SpaceAfter = Unit.FromPoint(8);

            // Embed the rasterised diagram (best-effort; the PDF is still valid without it).
            string tempPng = null;
            try
            {
                tempPng = Path.Combine(Path.GetTempPath(), "erd_" + Guid.NewGuid().ToString("N") + ".png");
                ErdPngExporter.Export(model, ColumnDisplay.KeysAndLookupsOnly, tempPng);
                var img = section.AddImage(tempPng);
                img.LockAspectRatio = true;
                img.Width = Unit.FromCentimeter(25);
            }
            catch
            {
                var note = section.AddParagraph("(Diagram image could not be rendered; see the table detail below.)");
                note.Format.Font.Italic = true;
                note.Format.Font.Color = new Color(150, 150, 150);
            }

            // Per-table detail.
            foreach (var t in model.Tables.OrderBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                var heading = section.AddParagraph($"{t.DisplayName ?? t.LogicalName}  ({(t.IsCustom ? "custom" : "standard")}{(t.IsManaged ? ", managed" : "")})");
                heading.Format.Font.Size = 12;
                heading.Format.Font.Bold = true;
                heading.Format.SpaceBefore = Unit.FromPoint(12);
                heading.Format.SpaceAfter = Unit.FromPoint(2);
                heading.Format.KeepWithNext = true;

                var meta = section.AddParagraph(
                    $"logical: {t.LogicalName}    schema: {t.SchemaName}    PK: {t.PrimaryIdColumn}    name: {t.PrimaryNameColumn}");
                meta.Format.Font.Size = 8;
                meta.Format.Font.Color = new Color(120, 120, 120);
                meta.Format.SpaceAfter = Unit.FromPoint(3);

                var table = section.AddTable();
                table.Borders.Color = new Color(225, 225, 225);
                table.Borders.Width = 0.5;
                table.AddColumn(Unit.FromCentimeter(7));
                table.AddColumn(Unit.FromCentimeter(4));
                table.AddColumn(Unit.FromCentimeter(4));
                table.AddColumn(Unit.FromCentimeter(3));
                table.AddColumn(Unit.FromCentimeter(7));
                var hr = table.AddRow();
                hr.Shading.Color = new Color(43, 43, 64);
                hr.HeadingFormat = true;
                string[] headers = { "Column", "Type", "Required", "Flags", "Targets" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var hp = hr.Cells[i].AddParagraph(headers[i]);
                    hp.Format.Font.Color = Colors.White;
                    hp.Format.Font.Bold = true;
                    hp.Format.Font.Size = 8;
                }

                foreach (var c in ErdModel.SelectColumns(t, display))
                {
                    var row = table.AddRow();
                    row.Cells[0].AddParagraph(c.LogicalName ?? "");
                    row.Cells[1].AddParagraph(c.Type ?? "");
                    row.Cells[2].AddParagraph(c.RequiredLevel ?? "");
                    var flags = string.Join(" ", new[]
                    {
                        c.IsPrimaryId ? "PK" : null, c.IsPrimaryName ? "name" : null, c.IsLookup ? "FK" : null
                    }.Where(f => f != null));
                    row.Cells[3].AddParagraph(flags);
                    row.Cells[4].AddParagraph(string.Join(", ", c.Targets ?? Enumerable.Empty<string>()));
                }

                if (t.AlternateKeys.Count > 0)
                {
                    var keys = section.AddParagraph("Alternate keys: " +
                        string.Join("; ", t.AlternateKeys.Select(k => $"{k.Name} ({string.Join(", ", k.Columns)})")));
                    keys.Format.Font.Size = 8;
                    keys.Format.Font.Color = new Color(90, 90, 90);
                    keys.Format.SpaceBefore = Unit.FromPoint(2);
                }
            }

            // Relationships.
            var relHead = section.AddParagraph("Relationships");
            relHead.Format.Font.Size = 12;
            relHead.Format.Font.Bold = true;
            relHead.Format.SpaceBefore = Unit.FromPoint(14);
            relHead.Format.SpaceAfter = Unit.FromPoint(3);
            relHead.Format.KeepWithNext = true;

            if (model.Relationships.Count == 0)
            {
                section.AddParagraph("No relationships between the selected tables.").Format.Font.Italic = true;
            }
            else
            {
                var rt = section.AddTable();
                rt.Borders.Color = new Color(225, 225, 225);
                rt.Borders.Width = 0.5;
                rt.AddColumn(Unit.FromCentimeter(6));
                rt.AddColumn(Unit.FromCentimeter(3));
                rt.AddColumn(Unit.FromCentimeter(4.5));
                rt.AddColumn(Unit.FromCentimeter(4.5));
                rt.AddColumn(Unit.FromCentimeter(3.5));
                rt.AddColumn(Unit.FromCentimeter(3.5));
                var rhr = rt.AddRow();
                rhr.Shading.Color = new Color(43, 43, 64);
                rhr.HeadingFormat = true;
                string[] rcols = { "Schema name", "Type", "From → To", "Lookup", "Required", "Cascade" };
                for (int i = 0; i < rcols.Length; i++)
                {
                    var hp = rhr.Cells[i].AddParagraph(rcols[i]);
                    hp.Format.Font.Color = Colors.White;
                    hp.Format.Font.Bold = true;
                    hp.Format.Font.Size = 8;
                }
                foreach (var r in model.Relationships)
                {
                    var row = rt.AddRow();
                    row.Cells[0].AddParagraph(r.SchemaName ?? "");
                    row.Cells[1].AddParagraph(r.RelationType ?? "");
                    row.Cells[2].AddParagraph($"{r.FromTable} → {r.ToTable}");
                    row.Cells[3].AddParagraph(r.LookupColumn ?? "");
                    row.Cells[4].AddParagraph(r.RequiredLevel ?? "");
                    row.Cells[5].AddParagraph(r.CascadeSummary ?? "");
                }
            }

            if (model.Notes != null && model.Notes.Count > 0)
            {
                var nh = section.AddParagraph("Notes");
                nh.Format.Font.Size = 11;
                nh.Format.Font.Bold = true;
                nh.Format.SpaceBefore = Unit.FromPoint(12);
                foreach (var n in model.Notes)
                {
                    var np = section.AddParagraph("• " + n);
                    np.Format.Font.Size = 8;
                    np.Format.Font.Color = new Color(110, 110, 110);
                }
            }

            var renderer = new PdfDocumentRenderer(true) { Document = doc };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(path);

            if (tempPng != null)
            {
                try { File.Delete(tempPng); } catch { /* temp file cleanup is best-effort */ }
            }
        }
    }
}
