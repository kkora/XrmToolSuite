using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace XrmToolSuite.DeploymentRiskAnalyzer.Reporting
{
    /// <summary>CI/CD-friendly JSON: summary + findings + pass/fail against a threshold.</summary>
    public static class JsonReportExporter
    {
        public static void Export(AnalysisResult result, string path, OverallRisk failAt = OverallRisk.High)
        {
            var payload = new
            {
                tool = "Deployment Risk Analyzer",
                version = "1.0.0",
                solution = new
                {
                    uniqueName = result.SolutionUniqueName,
                    friendlyName = result.SolutionFriendlyName,
                    version = result.SolutionVersion,
                    managed = result.SolutionIsManaged
                },
                environments = new { source = result.SourceEnvironment, target = result.TargetEnvironment },
                analyzedOnUtc = result.AnalyzedOnUtc,
                analyzersRun = result.AnalyzersRun,
                analyzersSkipped = result.AnalyzersSkipped,
                score = result.Score,
                risk = result.Risk.ToString(),
                aiSummary = result.AiSummary,
                summary = result.SeveritySummary(),
                ci = new
                {
                    failAtRisk = failAt.ToString(),
                    pass = result.Risk < failAt,
                    suggestedExitCode = result.Risk < failAt ? 0 : 1
                },
                findings = result.Findings
                    .OrderByDescending(f => f.Severity)
                    .Select(f => new
                    {
                        category = f.Category.ToString(),
                        severity = f.Severity.ToString(),
                        title = f.Title,
                        component = f.AffectedComponent,
                        description = f.Description,
                        recommendation = f.Recommendation,
                        helpUrl = f.HelpUrl
                    })
            };

            var json = JsonConvert.SerializeObject(payload, Formatting.Indented,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }

    /// <summary>Ordered, actionable fix checklist (Markdown). </summary>
    public static class FixChecklistGenerator
    {
        public static string Generate(AnalysisResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Fix Checklist — {result.SolutionFriendlyName} v{result.SolutionVersion}");
            sb.AppendLine();
            sb.AppendLine($"Risk: **{result.Risk}** (score {result.Score}/100) — generated {result.AnalyzedOnUtc:u}");
            sb.AppendLine();

            var actionable = result.Findings
                .Where(f => f.Severity >= Severity.Low && !string.IsNullOrWhiteSpace(f.Recommendation))
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.Category)
                .ToList();

            if (actionable.Count == 0)
            {
                sb.AppendLine("No actionable items — clear for deployment. ✔");
                return sb.ToString();
            }

            foreach (var group in actionable.GroupBy(f => f.Severity).OrderByDescending(g => g.Key))
            {
                sb.AppendLine($"## {group.Key} ({group.Count()})");
                foreach (var f in group)
                    sb.AppendLine($"- [ ] **[{f.Category}] {f.Title}** — `{f.AffectedComponent}`: {f.Recommendation}");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine("## Rollback guidance");
            sb.AppendLine(result.SolutionIsManaged
                ? "- [ ] Managed solution: capture the current target version; rollback = re-import the previous managed .zip (or uninstall if newly introduced). Note: uninstalling a managed solution DELETES its tables and data — export data first."
                : "- [ ] Unmanaged solution: THERE IS NO CLEAN ROLLBACK. Take an environment backup (admin center) before import and document every component for manual reversal.");
            sb.AppendLine("- [ ] Take an on-demand environment backup immediately before deployment.");
            sb.AppendLine("- [ ] Use 'Stage for upgrade' for managed upgrades so deletions can be reviewed before 'Apply upgrade'.");
            sb.AppendLine("- [ ] Keep the previous solution export (.zip) in your release artifacts for fast restore.");

            return sb.ToString();
        }

        public static void Export(AnalysisResult result, string path) =>
            File.WriteAllText(path, Generate(result), Encoding.UTF8);
    }

    /// <summary>
    /// Native executive PDF (no HTML round-trip) rendered with MigraDoc/PdfSharp: a titled cover
    /// block, a colour-coded risk banner, a severity summary, and findings grouped by category.
    /// MigraDoc types stay confined to this method's body (never in a public signature).
    /// </summary>
    public static class PdfReportExporter
    {
        public static void Export(AnalysisResult r, string path)
        {
            var doc = new Document();
            doc.Info.Title = $"Deployment Risk Analyzer — {r.SolutionFriendlyName}";
            doc.Info.Author = "Deployment Risk Analyzer";

            var normal = doc.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 9;

            var section = doc.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.6);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);

            // Title block. A right indent keeps the flowed text clear of the radial gauge, which is
            // drawn absolutely into the top-right corner with PdfSharp after the document renders
            // (MigraDoc has no charting, so the dashboard's signature gauge is an XGraphics overlay).
            var title = section.AddParagraph("Deployment Risk Report");
            title.Format.Font.Size = 20;
            title.Format.Font.Bold = true;
            title.Format.Font.Color = new Color(27, 27, 47);
            title.Format.RightIndent = Unit.FromPoint(170);
            title.Format.SpaceAfter = Unit.FromPoint(2);

            var sub = section.AddParagraph(
                $"{r.SolutionFriendlyName} ({r.SolutionUniqueName})  v{r.SolutionVersion}  •  {(r.SolutionIsManaged ? "Managed" : "Unmanaged")}");
            sub.Format.Font.Size = 10;
            sub.Format.Font.Color = new Color(90, 90, 90);
            sub.Format.RightIndent = Unit.FromPoint(170);

            var meta = section.AddParagraph(
                $"Source: {r.SourceEnvironment}    Target: {r.TargetEnvironment ?? "not connected"}    Analyzed: {r.AnalyzedOnUtc:u}");
            meta.Format.Font.Size = 8;
            meta.Format.Font.Color = new Color(130, 130, 130);
            meta.Format.RightIndent = Unit.FromPoint(170);
            meta.Format.SpaceAfter = Unit.FromPoint(58); // reserve vertical room for the gauge overlay

            // Risk banner
            var banner = section.AddTable();
            banner.Borders.Width = 0;
            banner.AddColumn(Unit.FromCentimeter(17.4));
            var brow = banner.AddRow();
            brow.Shading.Color = RiskColor(r.Risk);
            var bcell = brow.Cells[0].AddParagraph(
                $"Overall risk: {r.Risk.ToString().ToUpperInvariant()}    •    Score {r.Score}/100");
            bcell.Format.Font.Size = 14;
            bcell.Format.Font.Bold = true;
            bcell.Format.Font.Color = Colors.White;
            brow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            brow.Height = Unit.FromPoint(30);
            brow.Cells[0].Format.LeftIndent = Unit.FromPoint(8);

            // Risk categories (derived — score = worst finding in the area, mirroring the dashboard)
            var cats = r.Findings
                .GroupBy(f => f.Category)
                .Select(g => new { Cat = g.Key, Count = g.Count(), Score = HtmlReportExporter.CategoryScore(g) })
                .OrderByDescending(x => x.Score).ThenByDescending(x => x.Count)
                .ToList();
            if (cats.Count > 0)
            {
                var catHead = section.AddParagraph("Risk categories");
                catHead.Format.Font.Size = 12;
                catHead.Format.Font.Bold = true;
                catHead.Format.SpaceBefore = Unit.FromPoint(14);
                catHead.Format.SpaceAfter = Unit.FromPoint(4);
                catHead.Format.KeepWithNext = true;

                var catTable = section.AddTable();
                catTable.Borders.Color = new Color(225, 225, 225);
                catTable.Borders.Width = 0.5;
                catTable.AddColumn(Unit.FromCentimeter(11.4)); // category
                catTable.AddColumn(Unit.FromCentimeter(3.0));  // score
                catTable.AddColumn(Unit.FromCentimeter(3.0));  // issues
                var ch = catTable.AddRow();
                ch.Shading.Color = new Color(43, 43, 64);
                ch.HeadingFormat = true;
                foreach (var (label, idx, align) in new[] { ("Category", 0, ParagraphAlignment.Left), ("Score", 1, ParagraphAlignment.Right), ("Issues", 2, ParagraphAlignment.Center) })
                {
                    var hp = ch.Cells[idx].AddParagraph(label);
                    hp.Format.Font.Color = Colors.White;
                    hp.Format.Font.Bold = true;
                    hp.Format.Font.Size = 8;
                    hp.Format.Alignment = align;
                }
                foreach (var c in cats)
                {
                    var cr = catTable.AddRow();
                    cr.Cells[0].AddParagraph(HtmlReportExporter.CategoryName(c.Cat));
                    var scP = cr.Cells[1].AddParagraph(c.Score.ToString());
                    scP.Format.Alignment = ParagraphAlignment.Right;
                    scP.Format.Font.Bold = true;
                    scP.Format.Font.Color = ScoreColor(c.Score);
                    var isP = cr.Cells[2].AddParagraph(c.Count.ToString());
                    isP.Format.Alignment = ParagraphAlignment.Center;
                }
            }

            // Severity summary
            var summary = section.AddParagraph("Severity summary");
            summary.Format.Font.Size = 12;
            summary.Format.Font.Bold = true;
            summary.Format.SpaceBefore = Unit.FromPoint(14);
            summary.Format.SpaceAfter = Unit.FromPoint(4);

            var sevTable = section.AddTable();
            sevTable.Borders.Color = new Color(220, 220, 220);
            sevTable.Borders.Width = 0.5;
            for (int i = 0; i < 5; i++) sevTable.AddColumn(Unit.FromCentimeter(3.48));
            var labelRow = sevTable.AddRow();
            var countRow = sevTable.AddRow();
            var severities = new[] { Severity.Critical, Severity.High, Severity.Medium, Severity.Low, Severity.Info };
            for (int i = 0; i < severities.Length; i++)
            {
                var lc = labelRow.Cells[i].AddParagraph(severities[i].ToString());
                lc.Format.Alignment = ParagraphAlignment.Center;
                lc.Format.Font.Color = SeverityTextColor(severities[i]);
                lc.Format.Font.Bold = true;
                labelRow.Cells[i].Shading.Color = SeverityColor(severities[i]);
                var cc = countRow.Cells[i].AddParagraph(r.CountBySeverity(severities[i]).ToString());
                cc.Format.Alignment = ParagraphAlignment.Center;
                cc.Format.Font.Size = 16;
                cc.Format.Font.Bold = true;
            }

            // Recommendations (top actionable items, mirroring the dashboard)
            var recs = r.Findings
                .Where(f => f.Severity >= Severity.Low && !string.IsNullOrWhiteSpace(f.Recommendation))
                .OrderByDescending(f => f.Severity).ThenBy(f => f.Category)
                .ToList();
            if (recs.Count > 0)
            {
                var shown = recs.Take(8).ToList();
                var recHead = section.AddParagraph(recs.Count > shown.Count
                    ? $"Recommendations (top {shown.Count} of {recs.Count})" : "Recommendations");
                recHead.Format.Font.Size = 12;
                recHead.Format.Font.Bold = true;
                recHead.Format.SpaceBefore = Unit.FromPoint(14);
                recHead.Format.SpaceAfter = Unit.FromPoint(4);
                recHead.Format.KeepWithNext = true;

                var recTable = section.AddTable();
                recTable.Borders.Color = new Color(225, 225, 225);
                recTable.Borders.Width = 0.5;
                recTable.AddColumn(Unit.FromCentimeter(2.0));   // priority
                recTable.AddColumn(Unit.FromCentimeter(10.4));  // recommendation
                recTable.AddColumn(Unit.FromCentimeter(5.0));   // component
                var rh = recTable.AddRow();
                rh.Shading.Color = new Color(43, 43, 64);
                rh.HeadingFormat = true;
                string[] rcols = { "Priority", "Recommendation", "Component" };
                for (int i = 0; i < rcols.Length; i++)
                {
                    var hp = rh.Cells[i].AddParagraph(rcols[i]);
                    hp.Format.Font.Color = Colors.White;
                    hp.Format.Font.Bold = true;
                    hp.Format.Font.Size = 8;
                }
                foreach (var f in shown)
                {
                    var rr = recTable.AddRow();
                    var pr = rr.Cells[0].AddParagraph(f.Severity.ToString());
                    pr.Format.Font.Color = SeverityTextColor(f.Severity);
                    pr.Format.Font.Bold = true;
                    pr.Format.Font.Size = 8;
                    rr.Cells[0].Shading.Color = SeverityColor(f.Severity);
                    rr.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                    rr.Cells[1].AddParagraph(f.Recommendation ?? "");
                    rr.Cells[2].AddParagraph(f.AffectedComponent ?? "");
                }
            }

            // Next steps (generic deployment guidance, tuned by the run)
            int crit = r.CountBySeverity(Severity.Critical);
            var nsHead = section.AddParagraph("Next steps");
            nsHead.Format.Font.Size = 12;
            nsHead.Format.Font.Bold = true;
            nsHead.Format.SpaceBefore = Unit.FromPoint(14);
            nsHead.Format.SpaceAfter = Unit.FromPoint(4);
            nsHead.Format.KeepWithNext = true;
            AddStep(section, "Resolve all critical issues",
                crit > 0 ? $"{crit} must clear before deployment" : "None outstanding");
            AddStep(section, "Validate in a sandbox first", "Test the import before the target environment");
            AddStep(section, "Take a backup / stage for upgrade", r.SolutionIsManaged
                ? "Managed: stage before Apply upgrade so deletions are reviewable"
                : "Unmanaged: no clean rollback — back up the environment first");
            AddStep(section, "Share this report with stakeholders", "Circulate before promoting");

            // Executive summary (AI-generated or offline template), if present
            if (!string.IsNullOrWhiteSpace(r.AiSummary))
            {
                var esHead = section.AddParagraph("Executive summary");
                esHead.Format.Font.Size = 12;
                esHead.Format.Font.Bold = true;
                esHead.Format.SpaceBefore = Unit.FromPoint(14);
                esHead.Format.SpaceAfter = Unit.FromPoint(4);
                esHead.Format.KeepWithNext = true;
                foreach (var line in r.AiSummary.Replace("\r\n", "\n").Split('\n'))
                {
                    var p = section.AddParagraph(line);
                    p.Format.SpaceAfter = Unit.FromPoint(3);
                }
            }

            // Findings by category
            foreach (var group in r.Findings.GroupBy(f => f.Category))
            {
                var heading = section.AddParagraph($"{HtmlReportExporter.CategoryName(group.Key)} ({group.Count()})");
                heading.Format.Font.Size = 12;
                heading.Format.Font.Bold = true;
                heading.Format.SpaceBefore = Unit.FromPoint(14);
                heading.Format.SpaceAfter = Unit.FromPoint(4);
                heading.Format.KeepWithNext = true;

                // Three columns; Detail & recommendation drops to its own full-width row beneath each
                // finding, so long unbreakable component names (e.g. "Account/Signin/…") can no longer
                // overflow a narrow column and overlap the detail text.
                var table = section.AddTable();
                table.Borders.Color = new Color(225, 225, 225);
                table.Borders.Width = 0.5;
                table.AddColumn(Unit.FromCentimeter(2.0));   // severity
                table.AddColumn(Unit.FromCentimeter(5.0));   // finding
                table.AddColumn(Unit.FromCentimeter(10.4));  // component

                var header = table.AddRow();
                header.Shading.Color = new Color(43, 43, 64);
                header.HeadingFormat = true;
                string[] cols = { "Severity", "Finding", "Component" };
                for (int i = 0; i < cols.Length; i++)
                {
                    var hp = header.Cells[i].AddParagraph(cols[i]);
                    hp.Format.Font.Color = Colors.White;
                    hp.Format.Font.Bold = true;
                    hp.Format.Font.Size = 8;
                }

                foreach (var f in group.OrderByDescending(x => x.Severity))
                {
                    var row = table.AddRow();
                    row.KeepWith = 1; // keep the finding row with its detail row across page breaks
                    var sp = row.Cells[0].AddParagraph(f.Severity.ToString());
                    sp.Format.Font.Color = SeverityTextColor(f.Severity);
                    sp.Format.Font.Bold = true;
                    row.Cells[0].Shading.Color = SeverityColor(f.Severity);
                    row.Cells[1].AddParagraph(f.Title ?? "").Format.Font.Bold = true;
                    row.Cells[2].AddParagraph(f.AffectedComponent ?? "");

                    var detailRow = table.AddRow();
                    detailRow.Shading.Color = new Color(248, 248, 248);
                    detailRow.Cells[0].MergeRight = 2; // full width under the finding
                    var detail = detailRow.Cells[0].AddParagraph(f.Description ?? "");
                    detail.Format.LeftIndent = Unit.FromPoint(4);
                    var rec = detailRow.Cells[0].AddParagraph($"→ {f.Recommendation}");
                    rec.Format.Font.Color = new Color(70, 70, 70);
                    rec.Format.LeftIndent = Unit.FromPoint(4);
                    rec.Format.SpaceBefore = Unit.FromPoint(1);
                    rec.Format.SpaceAfter = Unit.FromPoint(2);
                }
            }

            var footer = section.AddParagraph(
                $"Analyzers run: {string.Join(", ", r.AnalyzersRun)}" +
                (r.AnalyzersSkipped.Any() ? $"  —  skipped: {string.Join(", ", r.AnalyzersSkipped)}" : ""));
            footer.Format.Font.Size = 7;
            footer.Format.Font.Color = new Color(140, 140, 140);
            footer.Format.SpaceBefore = Unit.FromPoint(14);

            var renderer = new PdfDocumentRenderer(true) { Document = doc };
            renderer.RenderDocument();
            DrawGauge(renderer.PdfDocument.Pages[0], r.Score, r.Risk);
            renderer.PdfDocument.Save(path);
        }

        // Bold-title + detail line used for the "Next steps" block.
        private static void AddStep(Section section, string title, string detail)
        {
            var p = section.AddParagraph();
            p.Format.SpaceAfter = Unit.FromPoint(4);
            p.Format.LeftIndent = Unit.FromPoint(4);
            var t = p.AddFormattedText("• " + title, TextFormat.Bold);
            p.AddText("   ");
            var d = p.AddFormattedText(detail);
            d.Font.Color = new Color(110, 110, 110);
            d.Font.Size = 8.5;
        }

        // Radial score gauge drawn absolutely into page 1's top-right corner (MigraDoc can't chart).
        // Purely additive — any drawing failure degrades to a gauge-less PDF rather than throwing.
        private static void DrawGauge(PdfPage page, int score, OverallRisk risk)
        {
            try
            {
                int pct = Math.Max(0, Math.Min(100, score));
                // Bounding box for the full circle; only the top semicircle (180°→360°) is drawn.
                const double x = 399, y = 52, w = 140, h = 140;
                double cy = y + h / 2.0;
                XColor band = RiskXColor(risk);

                using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
                {
                    var track = new XPen(XColor.FromArgb(227, 232, 240), 12) { LineCap = XLineCap.Round };
                    gfx.DrawArc(track, x, y, w, h, 180, 180);
                    if (pct > 0)
                    {
                        var val = new XPen(band, 12) { LineCap = XLineCap.Round };
                        gfx.DrawArc(val, x, y, w, h, 180, 180.0 * pct / 100.0);
                    }

                    var numFont = new XFont("Arial", 30, XFontStyle.Bold);
                    var ofFont = new XFont("Arial", 9, XFontStyle.Regular);
                    var bandFont = new XFont("Arial", 9, XFontStyle.Bold);
                    // Readout centred in the arc opening: number sits above the diameter, /100 and band below it.
                    gfx.DrawString(pct.ToString(), numFont, new XSolidBrush(XColor.FromArgb(26, 29, 41)),
                        new XRect(x, cy - 50, w, 34), XStringFormats.Center);
                    gfx.DrawString("/ 100", ofFont, new XSolidBrush(XColor.FromArgb(139, 149, 173)),
                        new XRect(x, cy - 12, w, 12), XStringFormats.Center);
                    gfx.DrawString(BandText(risk), bandFont, new XSolidBrush(band),
                        new XRect(x, cy + 4, w, 12), XStringFormats.Center);
                }
            }
            catch { /* gauge is decorative; never fail the export over it */ }
        }

        private static string BandText(OverallRisk risk) =>
            risk == OverallRisk.High ? "HIGH RISK" : risk == OverallRisk.Medium ? "MEDIUM RISK" : "LOW RISK";

        private static XColor RiskXColor(OverallRisk risk) =>
            risk == OverallRisk.High ? XColor.FromArgb(209, 52, 56)
            : risk == OverallRisk.Medium ? XColor.FromArgb(247, 135, 31)
            : XColor.FromArgb(18, 161, 80);

        // Category-score colour, matching the dashboard bands (>=70 crit, >=45 high, else good).
        private static Color ScoreColor(int score) =>
            score >= 70 ? new Color(209, 52, 56)
            : score >= 45 ? new Color(247, 135, 31)
            : new Color(18, 161, 80);

        private static Color RiskColor(OverallRisk risk) =>
            risk == OverallRisk.High ? new Color(209, 52, 56)
            : risk == OverallRisk.Medium ? new Color(247, 169, 36)
            : new Color(16, 124, 16);

        private static Color SeverityColor(Severity s)
        {
            switch (s)
            {
                case Severity.Critical: return new Color(164, 38, 44);
                case Severity.High: return new Color(209, 52, 56);
                case Severity.Medium: return new Color(247, 169, 36);
                case Severity.Low: return new Color(138, 132, 134);
                default: return new Color(0, 120, 212);
            }
        }

        // Medium sits on amber — black reads better there; every other band is on a dark fill.
        private static Color SeverityTextColor(Severity s) =>
            s == Severity.Medium ? Colors.Black : Colors.White;
    }

    /// <summary>Excel workbook: Summary, Findings, Fix Checklist sheets.</summary>
    public static class ExcelReportExporter
    {
        public static void Export(AnalysisResult r, string path)
        {
            using (var wb = new XLWorkbook())
            {
                // Summary
                var sum = wb.Worksheets.Add("Summary");
                sum.Cell(1, 1).Value = "Deployment Risk Analyzer";
                sum.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16);
                var rows = new (string, string)[]
                {
                    ("Solution", $"{r.SolutionFriendlyName} ({r.SolutionUniqueName})"),
                    ("Version", r.SolutionVersion),
                    ("Managed", r.SolutionIsManaged ? "Yes" : "No"),
                    ("Source", r.SourceEnvironment),
                    ("Target", r.TargetEnvironment ?? "not connected"),
                    ("Analyzed (UTC)", r.AnalyzedOnUtc.ToString("u")),
                    ("Risk", r.Risk.ToString()),
                    ("Score", $"{r.Score}/100"),
                    ("Critical", r.CountBySeverity(Severity.Critical).ToString()),
                    ("High", r.CountBySeverity(Severity.High).ToString()),
                    ("Medium", r.CountBySeverity(Severity.Medium).ToString()),
                    ("Low", r.CountBySeverity(Severity.Low).ToString()),
                    ("Info", r.CountBySeverity(Severity.Info).ToString()),
                };
                for (int i = 0; i < rows.Length; i++)
                {
                    sum.Cell(i + 3, 1).Value = rows[i].Item1;
                    sum.Cell(i + 3, 1).Style.Font.SetBold();
                    sum.Cell(i + 3, 2).Value = rows[i].Item2;
                }
                sum.Columns().AdjustToContents();

                // Findings
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
                    ws.Cell(row, 2).Value = f.Category.ToString();
                    ws.Cell(row, 3).Value = f.Title;
                    ws.Cell(row, 4).Value = f.AffectedComponent;
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
                ws.RangeUsed().SetAutoFilter();
                ws.Columns().AdjustToContents(1, 60);
                ws.SheetView.FreezeRows(1);

                // Checklist
                var cl = wb.Worksheets.Add("Fix Checklist");
                cl.Cell(1, 1).Value = FixChecklistGenerator.Generate(r);
                cl.Cell(1, 1).Style.Alignment.SetWrapText();
                cl.Column(1).Width = 140;

                wb.SaveAs(path);
            }
        }
    }
}
