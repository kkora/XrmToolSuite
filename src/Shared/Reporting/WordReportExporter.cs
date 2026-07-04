using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.Reporting
{
    /// <summary>
    /// Word (.docx) exporter built directly on the OpenXML SDK (<c>DocumentFormat.OpenXml</c>, which the
    /// ClosedXML chain already ships — no extra dependency). Generic over <see cref="ReportModel"/>:
    /// title, score, executive summary, key metrics, and findings grouped by category. Used primarily by
    /// the AI Solution Reviewer, whose narrative output reads best as a Word document.
    /// </summary>
    public static class WordReportExporter
    {
        public static void Export(ReportModel r, string path)
        {
            using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                var main = doc.AddMainDocumentPart();
                var body = new Body();

                body.Append(Heading(r.ReportTitle, 36, true));
                body.Append(Para($"{r.SubjectName}" +
                    (string.IsNullOrEmpty(r.SubjectVersion) ? "" : $"  v{r.SubjectVersion}") +
                    $"    {r.BandText()} · Score {r.Score}/100", 20, false, "808080"));
                body.Append(Para($"Generated {r.AnalyzedOnUtc:u} by {r.ToolName}", 16, false, "A0A0A0"));

                if (r.Metrics.Count > 0)
                {
                    body.Append(Heading("Key metrics", 26, true));
                    foreach (var m in r.Metrics)
                        body.Append(Para($"• {m.Label}: {m.Value}" + (string.IsNullOrEmpty(m.Hint) ? "" : $" {m.Hint}"), 20, false));
                }

                if (!string.IsNullOrWhiteSpace(r.AiSummary))
                {
                    body.Append(Heading("Executive summary", 26, true));
                    foreach (var line in r.AiSummary.Replace("\r\n", "\n").Split('\n'))
                        body.Append(Para(line, 20, false));
                }

                foreach (var group in r.Findings.GroupBy(f => f.Category))
                {
                    body.Append(Heading($"{group.Key} ({group.Count()})", 24, true));
                    foreach (var f in group.OrderByDescending(x => x.Severity))
                    {
                        body.Append(Para($"[{f.Severity}] {f.Title}", 20, true));
                        if (!string.IsNullOrWhiteSpace(f.Component)) body.Append(Para($"Component: {f.Component}", 18, false, "808080"));
                        if (!string.IsNullOrWhiteSpace(f.Description)) body.Append(Para(f.Description, 18, false));
                        if (!string.IsNullOrWhiteSpace(f.Recommendation)) body.Append(Para($"→ {f.Recommendation}", 18, false, "555555"));
                    }
                }

                main.Document = new Document(body);
                main.Document.Save();
            }
        }

        private static Paragraph Heading(string text, int halfPointSize, bool bold) =>
            Para(text, halfPointSize, bold);

        private static Paragraph Para(string text, int halfPointSize, bool bold, string colorHex = null)
        {
            var runProps = new RunProperties(new FontSize { Val = halfPointSize.ToString() });
            if (bold) runProps.Append(new Bold());
            if (colorHex != null) runProps.Append(new Color { Val = colorHex });
            var run = new Run(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            run.RunProperties = runProps;
            return new Paragraph(run);
        }
    }
}
