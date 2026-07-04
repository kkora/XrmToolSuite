using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.Reporting
{
    /// <summary>
    /// Excel workbook: Summary, Findings, and Fix Checklist sheets. Generic over <see cref="ReportModel"/>.
    /// Requires ClosedXML (shipped in the tool's Plugins folder — see the tool nuspec/csproj wiring).
    /// </summary>
    public static class ExcelReportExporter
    {
        public static void Export(ReportModel r, string path)
        {
            using (var wb = new XLWorkbook())
            {
                // ---- Summary ----
                var sum = wb.Worksheets.Add("Summary");
                sum.Cell(1, 1).Value = r.ToolName;
                sum.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16);

                var rows = new List<(string, string)>
                {
                    ("Subject", string.IsNullOrEmpty(r.SubjectKey) ? r.SubjectName : $"{r.SubjectName} ({r.SubjectKey})"),
                };
                if (!string.IsNullOrEmpty(r.SubjectVersion)) rows.Add(("Version", r.SubjectVersion));
                if (r.IsManaged.HasValue) rows.Add(("Managed", r.IsManaged.Value ? "Yes" : "No"));
                if (!string.IsNullOrEmpty(r.SourceEnvironment)) rows.Add(("Source", r.SourceEnvironment));
                if (!string.IsNullOrEmpty(r.TargetEnvironment)) rows.Add(("Target", r.TargetEnvironment));
                rows.Add(("Analyzed (UTC)", r.AnalyzedOnUtc.ToString("u")));
                rows.Add((char.ToUpperInvariant(r.ScoreWord[0]) + r.ScoreWord.Substring(1), r.Band.ToString()));
                rows.Add(("Score", $"{r.Score}/100"));
                foreach (var m in r.Metrics) rows.Add((m.Label, m.Value));
                rows.Add(("Critical", r.CountBySeverity(Severity.Critical).ToString()));
                rows.Add(("High", r.CountBySeverity(Severity.High).ToString()));
                rows.Add(("Medium", r.CountBySeverity(Severity.Medium).ToString()));
                rows.Add(("Low", r.CountBySeverity(Severity.Low).ToString()));
                rows.Add(("Info", r.CountBySeverity(Severity.Info).ToString()));

                for (int i = 0; i < rows.Count; i++)
                {
                    sum.Cell(i + 3, 1).Value = rows[i].Item1;
                    sum.Cell(i + 3, 1).Style.Font.SetBold();
                    sum.Cell(i + 3, 2).Value = rows[i].Item2;
                }
                sum.Columns().AdjustToContents();

                // ---- Findings ----
                var ws = wb.Worksheets.Add("Findings");
                string[] headers = { "Severity", "Category", "Finding", "Component", "Description", "Recommendation", "Help" };
                for (int c = 0; c < headers.Length; c++)
                {
                    ws.Cell(1, c + 1).Value = headers[c];
                    ws.Cell(1, c + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#2B2B40"));
                    ws.Cell(1, c + 1).Style.Font.SetFontColor(XLColor.White);
                }

                int row = 2;
                foreach (var f in r.Findings.OrderByDescending(x => x.Severity))
                {
                    ws.Cell(row, 1).Value = f.Severity.ToString();
                    ws.Cell(row, 2).Value = f.Category;
                    ws.Cell(row, 3).Value = f.Title;
                    ws.Cell(row, 4).Value = f.Component;
                    ws.Cell(row, 5).Value = f.Description;
                    ws.Cell(row, 6).Value = f.Recommendation;
                    ws.Cell(row, 7).Value = f.HelpUrl ?? "";

                    var color = f.Severity == Severity.Critical ? "#A4262C"
                              : f.Severity == Severity.High ? "#D13438"
                              : f.Severity == Severity.Medium ? "#F7A924"
                              : f.Severity == Severity.Low ? "#8A8886" : "#0078D4";
                    ws.Cell(row, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml(color));
                    ws.Cell(row, 1).Style.Font.SetFontColor(f.Severity == Severity.Medium ? XLColor.Black : XLColor.White);
                    row++;
                }
                if (r.Findings.Count > 0)
                {
                    ws.RangeUsed().SetAutoFilter();
                    ws.Columns().AdjustToContents(1, 60);
                    ws.SheetView.FreezeRows(1);
                }

                // ---- Fix Checklist ----
                var cl = wb.Worksheets.Add("Fix Checklist");
                cl.Cell(1, 1).Value = FixChecklistGenerator.Generate(r);
                cl.Cell(1, 1).Style.Alignment.SetWrapText();
                cl.Column(1).Width = 140;

                wb.SaveAs(path);
            }
        }
    }
}
