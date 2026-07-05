using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace XrmToolSuite.SolutionDocumentationGenerator.Doc
{
    /// <summary>
    /// Native PDF exporter for a <see cref="SolutionDoc"/> using MigraDoc/PdfSharp (GDI build). Renders a
    /// real multi-section document: a title block, then a MigraDoc heading per section with its prose body,
    /// notes and native tables. MigraDoc/PdfSharp types are confined to this method body (never in a public
    /// signature); requires the PdfSharp/MigraDoc-GDI chain shipped in the tool's Plugins folder.
    /// </summary>
    public static class DocPdfExporter
    {
        public static void Export(SolutionDoc doc, string path)
        {
            doc = doc ?? new SolutionDoc();

            var pdf = new Document();
            pdf.Info.Title = (doc.SolutionName ?? "Solution") + " — documentation";
            pdf.Info.Author = "XrmToolSuite Solution Documentation Generator";

            var normal = pdf.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 9;

            var section = pdf.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.6);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.6);
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.5);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);

            // Footer page numbers.
            var footer = section.Footers.Primary.AddParagraph();
            footer.Format.Alignment = ParagraphAlignment.Center;
            footer.Format.Font.Size = 7;
            footer.Format.Font.Color = new Color(150, 150, 150);
            footer.AddText("Page ");
            footer.AddPageField();

            var title = section.AddParagraph(doc.SolutionName ?? "Solution documentation");
            title.Format.Font.Size = 20;
            title.Format.Font.Bold = true;
            title.Format.Font.Color = new Color(27, 27, 47);

            if (!string.IsNullOrWhiteSpace(doc.BrandingHeader))
            {
                var brand = section.AddParagraph(doc.BrandingHeader);
                brand.Format.Font.Size = 10;
                brand.Format.Font.Italic = true;
                brand.Format.Font.Color = new Color(120, 120, 120);
            }

            var meta = section.AddParagraph(
                $"{doc.UniqueName}   v{doc.Version}   ·   {(doc.IsManaged ? "Managed" : "Unmanaged")}   ·   Publisher: {doc.Publisher}");
            meta.Format.Font.Size = 9;
            meta.Format.Font.Color = new Color(110, 110, 110);

            var meta2 = section.AddParagraph(
                $"Mode: {doc.ModeLabel}   ·   Generated {doc.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)}");
            meta2.Format.Font.Size = 8;
            meta2.Format.Font.Color = new Color(140, 140, 140);
            meta2.Format.SpaceAfter = Unit.FromPoint(8);

            foreach (var docSection in doc.Sections ?? new List<DocSection>())
            {
                var head = section.AddParagraph(docSection.Title ?? docSection.Kind);
                head.Format.Font.Size = 14;
                head.Format.Font.Bold = true;
                head.Format.Font.Color = new Color(43, 43, 64);
                head.Format.SpaceBefore = Unit.FromPoint(14);
                head.Format.SpaceAfter = Unit.FromPoint(4);
                head.Format.KeepWithNext = true;

                if (!string.IsNullOrWhiteSpace(docSection.Body))
                {
                    foreach (var line in docSection.Body.Replace("\r\n", "\n").Split('\n'))
                    {
                        var p = section.AddParagraph(line);
                        p.Format.Font.Size = 9;
                        if (string.Equals(docSection.Kind, SectionKinds.Diagrams, StringComparison.OrdinalIgnoreCase))
                        {
                            p.Format.Font.Name = "Courier New";
                            p.Format.Font.Size = 7;
                            p.Format.Font.Color = new Color(80, 80, 80);
                        }
                    }
                }

                foreach (var note in docSection.Notes ?? new List<string>())
                {
                    var np = section.AddParagraph(note);
                    np.Format.Font.Size = 8;
                    np.Format.Font.Italic = true;
                    np.Format.Font.Color = new Color(138, 91, 0);
                    np.Format.SpaceBefore = Unit.FromPoint(2);
                }

                foreach (var table in docSection.Tables ?? new List<DocTable>())
                {
                    if (table.RowCount == 0) continue;
                    if (!string.IsNullOrWhiteSpace(table.Caption))
                    {
                        var cap = section.AddParagraph(table.Caption);
                        cap.Format.Font.Size = 9;
                        cap.Format.Font.Bold = true;
                        cap.Format.SpaceBefore = Unit.FromPoint(6);
                        cap.Format.SpaceAfter = Unit.FromPoint(2);
                        cap.Format.KeepWithNext = true;
                    }
                    AddTable(section, table);
                }
            }

            var renderer = new PdfDocumentRenderer(true) { Document = pdf };
            renderer.RenderDocument();
            renderer.PdfDocument.Save(path);
        }

        private static void AddTable(Section section, DocTable src)
        {
            int cols = Math.Max(1, src.Headers.Count);
            double usable = 17.8; // cm across A4 within margins
            double colWidth = usable / cols;

            var table = section.AddTable();
            table.Borders.Color = new Color(225, 225, 225);
            table.Borders.Width = 0.5;
            for (int i = 0; i < cols; i++) table.AddColumn(Unit.FromCentimeter(colWidth));

            var hr = table.AddRow();
            hr.Shading.Color = new Color(43, 43, 64);
            hr.HeadingFormat = true;
            for (int i = 0; i < cols; i++)
            {
                var hp = hr.Cells[i].AddParagraph(i < src.Headers.Count ? src.Headers[i] ?? "" : "");
                hp.Format.Font.Color = Colors.White;
                hp.Format.Font.Bold = true;
                hp.Format.Font.Size = 7.5;
            }

            foreach (var row in src.Rows)
            {
                var r = table.AddRow();
                for (int i = 0; i < cols; i++)
                {
                    var cp = r.Cells[i].AddParagraph(i < row.Count ? row[i] ?? "" : "");
                    cp.Format.Font.Size = 7.5;
                }
            }
        }
    }
}
