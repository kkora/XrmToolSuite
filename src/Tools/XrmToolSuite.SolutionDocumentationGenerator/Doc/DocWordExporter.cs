using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace XrmToolSuite.SolutionDocumentationGenerator.Doc
{
    /// <summary>
    /// Word (.docx) exporter built directly on the OpenXML SDK (<c>DocumentFormat.OpenXml</c>, already shipped
    /// by the ClosedXML chain — no extra dependency). Renders a real multi-section document: title + metadata,
    /// then one Word heading per <see cref="DocSection"/> with its prose body, notes and native Word tables.
    /// Every OpenXML type is confined to this method body — the public signature is <see cref="SolutionDoc"/>
    /// + string only — so this file stays OUT of the SDK-free unit-test compile set.
    /// </summary>
    public static class DocWordExporter
    {
        public static void Export(SolutionDoc doc, string path)
        {
            doc = doc ?? new SolutionDoc();

            using (var word = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                var main = word.AddMainDocumentPart();
                var body = new Body();

                var title = string.IsNullOrWhiteSpace(doc.SolutionName) ? "Solution documentation" : doc.SolutionName;
                body.Append(Para(title, 40, true));
                if (!string.IsNullOrWhiteSpace(doc.BrandingHeader))
                    body.Append(Para(doc.BrandingHeader, 20, false, "808080"));
                body.Append(Para(
                    $"{doc.UniqueName}  v{doc.Version}  ·  {(doc.IsManaged ? "Managed" : "Unmanaged")}  ·  Publisher: {doc.Publisher}",
                    18, false, "808080"));
                body.Append(Para(
                    $"Mode: {doc.ModeLabel}  ·  Generated {doc.GeneratedUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)}",
                    16, false, "A0A0A0"));

                foreach (var section in doc.Sections ?? new List<DocSection>())
                {
                    body.Append(Para(section.Title ?? section.Kind, 28, true, "2B2B40"));

                    if (!string.IsNullOrWhiteSpace(section.Body))
                        foreach (var line in section.Body.Replace("\r\n", "\n").Split('\n'))
                            body.Append(Para(line, 18, false));

                    foreach (var note in section.Notes ?? new List<string>())
                        body.Append(Para(note, 16, false, "8A5B00"));

                    foreach (var table in section.Tables ?? new List<DocTable>())
                    {
                        if (table.RowCount == 0) continue;
                        if (!string.IsNullOrWhiteSpace(table.Caption))
                            body.Append(Para(table.Caption, 20, true));
                        body.Append(BuildTable(table));
                        body.Append(new Paragraph());
                    }
                }

                main.Document = new Document(body);
                main.Document.Save();
            }
        }

        private static Table BuildTable(DocTable src)
        {
            var table = new Table();

            var props = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "D0D0D0" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "D0D0D0" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "D0D0D0" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "D0D0D0" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "E1E1E1" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "E1E1E1" }),
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });
            table.Append(props);

            var header = new TableRow();
            foreach (var h in src.Headers)
                header.Append(Cell(h, bold: true, shade: "2B2B40", color: "FFFFFF"));
            table.Append(header);

            int width = src.Headers.Count;
            foreach (var row in src.Rows)
            {
                var tr = new TableRow();
                for (int i = 0; i < width; i++)
                    tr.Append(Cell(i < row.Count ? row[i] : "", bold: false));
                table.Append(tr);
            }
            return table;
        }

        private static TableCell Cell(string text, bool bold, string shade = null, string color = null)
        {
            var cellProps = new TableCellProperties();
            if (shade != null) cellProps.Append(new Shading { Val = ShadingPatternValues.Clear, Fill = shade });

            var runProps = new RunProperties(new FontSize { Val = "16" });
            if (bold) runProps.Append(new Bold());
            if (color != null) runProps.Append(new Color { Val = color });

            var run = new Run(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }) { RunProperties = runProps };
            return new TableCell(cellProps, new Paragraph(run));
        }

        private static Paragraph Para(string text, int halfPointSize, bool bold, string colorHex = null)
        {
            var runProps = new RunProperties(new FontSize { Val = halfPointSize.ToString(CultureInfo.InvariantCulture) });
            if (bold) runProps.Append(new Bold());
            if (colorHex != null) runProps.Append(new Color { Val = colorHex });
            var run = new Run(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }) { RunProperties = runProps };
            return new Paragraph(run);
        }
    }
}
