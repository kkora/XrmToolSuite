using System;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using XrmToolSuite.Core.Analysis;

namespace XrmToolSuite.Core.Reporting
{
    /// <summary>
    /// Native executive PDF (no HTML round-trip) rendered with MigraDoc/PdfSharp (GDI build): a titled
    /// cover block with a radial score gauge, a colour-coded band banner, category + severity summaries,
    /// top recommendations, next steps, an optional executive summary, and findings grouped by category.
    /// Generic over <see cref="ReportModel"/>. MigraDoc types stay confined to method bodies (never in a
    /// public signature). Requires the PdfSharp/MigraDoc-GDI chain (shipped in the tool's Plugins folder).
    /// </summary>
    public static class PdfReportExporter
    {
        public static void Export(ReportModel r, string path)
        {
            var doc = new Document();
            doc.Info.Title = $"{r.ReportTitle} — {r.SubjectName}";
            doc.Info.Author = r.ToolName;

            var normal = doc.Styles["Normal"];
            normal.Font.Name = "Arial";
            normal.Font.Size = 9;

            var section = doc.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.8);
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.6);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);

            // Title block — right indent keeps flowed text clear of the gauge drawn into the top-right.
            var title = section.AddParagraph(r.ReportTitle);
            title.Format.Font.Size = 20;
            title.Format.Font.Bold = true;
            title.Format.Font.Color = new Color(27, 27, 47);
            title.Format.RightIndent = Unit.FromPoint(170);
            title.Format.SpaceAfter = Unit.FromPoint(2);

            string managed = r.IsManaged.HasValue ? (r.IsManaged.Value ? "  •  Managed" : "  •  Unmanaged") : "";
            string ver = string.IsNullOrEmpty(r.SubjectVersion) ? "" : $"  v{r.SubjectVersion}";
            string key = string.IsNullOrEmpty(r.SubjectKey) ? "" : $" ({r.SubjectKey})";
            var sub = section.AddParagraph($"{r.SubjectName}{key}{ver}{managed}");
            sub.Format.Font.Size = 10;
            sub.Format.Font.Color = new Color(90, 90, 90);
            sub.Format.RightIndent = Unit.FromPoint(170);

            var meta = section.AddParagraph(
                $"Source: {r.SourceEnvironment}    Target: {r.TargetEnvironment ?? "not connected"}    Analyzed: {r.AnalyzedOnUtc:u}");
            meta.Format.Font.Size = 8;
            meta.Format.Font.Color = new Color(130, 130, 130);
            meta.Format.RightIndent = Unit.FromPoint(170);
            meta.Format.SpaceAfter = Unit.FromPoint(58); // reserve room for the gauge overlay

            // Band banner
            var banner = section.AddTable();
            banner.Borders.Width = 0;
            banner.AddColumn(Unit.FromCentimeter(17.4));
            var brow = banner.AddRow();
            brow.Shading.Color = BandColor(r.Band);
            var bcell = brow.Cells[0].AddParagraph(
                $"{r.BandText()}    •    Score {r.Score}/100");
            bcell.Format.Font.Size = 14;
            bcell.Format.Font.Bold = true;
            bcell.Format.Font.Color = Colors.White;
            brow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
            brow.Height = Unit.FromPoint(30);
            brow.Cells[0].Format.LeftIndent = Unit.FromPoint(8);

            // Headline metrics (when present)
            if (r.Metrics.Count > 0)
            {
                var mHead = section.AddParagraph("Key metrics");
                mHead.Format.Font.Size = 12;
                mHead.Format.Font.Bold = true;
                mHead.Format.SpaceBefore = Unit.FromPoint(14);
                mHead.Format.SpaceAfter = Unit.FromPoint(4);
                mHead.Format.KeepWithNext = true;

                var mTable = section.AddTable();
                mTable.Borders.Color = new Color(225, 225, 225);
                mTable.Borders.Width = 0.5;
                mTable.AddColumn(Unit.FromCentimeter(11.4));
                mTable.AddColumn(Unit.FromCentimeter(6.0));
                foreach (var m in r.Metrics)
                {
                    var mr = mTable.AddRow();
                    mr.Cells[0].AddParagraph(m.Label ?? "");
                    var vp = mr.Cells[1].AddParagraph(m.Value ?? "");
                    vp.Format.Font.Bold = true;
                }
            }

            // Categories (score = worst finding in the area)
            var cats = r.Findings
                .GroupBy(f => f.Category)
                .Select(g => new { Cat = g.Key, Count = g.Count(), Score = ReportModel.CategoryScore(g) })
                .OrderByDescending(x => x.Score).ThenByDescending(x => x.Count)
                .ToList();
            if (cats.Count > 0)
            {
                var catHead = section.AddParagraph("Categories");
                catHead.Format.Font.Size = 12;
                catHead.Format.Font.Bold = true;
                catHead.Format.SpaceBefore = Unit.FromPoint(14);
                catHead.Format.SpaceAfter = Unit.FromPoint(4);
                catHead.Format.KeepWithNext = true;

                var catTable = section.AddTable();
                catTable.Borders.Color = new Color(225, 225, 225);
                catTable.Borders.Width = 0.5;
                catTable.AddColumn(Unit.FromCentimeter(11.4));
                catTable.AddColumn(Unit.FromCentimeter(3.0));
                catTable.AddColumn(Unit.FromCentimeter(3.0));
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
                    cr.Cells[0].AddParagraph(c.Cat ?? "");
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

            // Recommendations (top actionable items)
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
                recTable.AddColumn(Unit.FromCentimeter(2.0));
                recTable.AddColumn(Unit.FromCentimeter(10.4));
                recTable.AddColumn(Unit.FromCentimeter(5.0));
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
                    rr.Cells[2].AddParagraph(f.Component ?? "");
                }
            }

            // Next steps
            if (r.NextSteps.Count > 0)
            {
                var nsHead = section.AddParagraph("Next steps");
                nsHead.Format.Font.Size = 12;
                nsHead.Format.Font.Bold = true;
                nsHead.Format.SpaceBefore = Unit.FromPoint(14);
                nsHead.Format.SpaceAfter = Unit.FromPoint(4);
                nsHead.Format.KeepWithNext = true;
                foreach (var s in r.NextSteps)
                    AddStep(section, s.Title, s.Detail);
            }

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
                var heading = section.AddParagraph($"{group.Key} ({group.Count()})");
                heading.Format.Font.Size = 12;
                heading.Format.Font.Bold = true;
                heading.Format.SpaceBefore = Unit.FromPoint(14);
                heading.Format.SpaceAfter = Unit.FromPoint(4);
                heading.Format.KeepWithNext = true;

                var table = section.AddTable();
                table.Borders.Color = new Color(225, 225, 225);
                table.Borders.Width = 0.5;
                table.AddColumn(Unit.FromCentimeter(2.0));
                table.AddColumn(Unit.FromCentimeter(5.0));
                table.AddColumn(Unit.FromCentimeter(10.4));

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
                    row.KeepWith = 1;
                    var sp = row.Cells[0].AddParagraph(f.Severity.ToString());
                    sp.Format.Font.Color = SeverityTextColor(f.Severity);
                    sp.Format.Font.Bold = true;
                    row.Cells[0].Shading.Color = SeverityColor(f.Severity);
                    row.Cells[1].AddParagraph(f.Title ?? "").Format.Font.Bold = true;
                    row.Cells[2].AddParagraph(f.Component ?? "");

                    var detailRow = table.AddRow();
                    detailRow.Shading.Color = new Color(248, 248, 248);
                    detailRow.Cells[0].MergeRight = 2;
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
            DrawGauge(renderer.PdfDocument.Pages[0], r.Score, r.Band, r.ScoreWord);
            renderer.PdfDocument.Save(path);
        }

        private static void AddStep(Section section, string title, string detail)
        {
            var p = section.AddParagraph();
            p.Format.SpaceAfter = Unit.FromPoint(4);
            p.Format.LeftIndent = Unit.FromPoint(4);
            p.AddFormattedText("• " + title, TextFormat.Bold);
            p.AddText("   ");
            var d = p.AddFormattedText(detail ?? "");
            d.Font.Color = new Color(110, 110, 110);
            d.Font.Size = 8.5;
        }

        private static void DrawGauge(PdfPage page, int score, ScoreBand band, string scoreWord)
        {
            try
            {
                int pct = Math.Max(0, Math.Min(100, score));
                const double x = 399, y = 52, w = 140, h = 140;
                double cy = y + h / 2.0;
                XColor bandColor = BandXColor(band);

                using (var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
                {
                    var track = new XPen(XColor.FromArgb(227, 232, 240), 12) { LineCap = XLineCap.Round };
                    gfx.DrawArc(track, x, y, w, h, 180, 180);
                    if (pct > 0)
                    {
                        var val = new XPen(bandColor, 12) { LineCap = XLineCap.Round };
                        gfx.DrawArc(val, x, y, w, h, 180, 180.0 * pct / 100.0);
                    }

                    var numFont = new XFont("Arial", 30, XFontStyle.Bold);
                    var ofFont = new XFont("Arial", 9, XFontStyle.Regular);
                    var bandFont = new XFont("Arial", 9, XFontStyle.Bold);
                    gfx.DrawString(pct.ToString(), numFont, new XSolidBrush(XColor.FromArgb(26, 29, 41)),
                        new XRect(x, cy - 50, w, 34), XStringFormats.Center);
                    gfx.DrawString("/ 100", ofFont, new XSolidBrush(XColor.FromArgb(139, 149, 173)),
                        new XRect(x, cy - 12, w, 12), XStringFormats.Center);
                    string bandLabel = $"{band.ToString().ToUpperInvariant()} {scoreWord.ToUpperInvariant()}";
                    gfx.DrawString(bandLabel, bandFont, new XSolidBrush(bandColor),
                        new XRect(x, cy + 4, w, 12), XStringFormats.Center);
                }
            }
            catch { /* gauge is decorative; never fail the export over it */ }
        }

        private static XColor BandXColor(ScoreBand band) =>
            band == ScoreBand.High ? XColor.FromArgb(209, 52, 56)
            : band == ScoreBand.Medium ? XColor.FromArgb(247, 135, 31)
            : XColor.FromArgb(18, 161, 80);

        private static Color ScoreColor(int score) =>
            score >= 70 ? new Color(209, 52, 56)
            : score >= 45 ? new Color(247, 135, 31)
            : new Color(18, 161, 80);

        private static Color BandColor(ScoreBand band) =>
            band == ScoreBand.High ? new Color(209, 52, 56)
            : band == ScoreBand.Medium ? new Color(247, 169, 36)
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

        private static Color SeverityTextColor(Severity s) =>
            s == Severity.Medium ? Colors.Black : Colors.White;
    }
}
