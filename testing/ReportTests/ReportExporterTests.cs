using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using XrmToolSuite.DeploymentRiskAnalyzer.Reporting;

namespace XrmToolSuite.ReportTests
{
    /// <summary>
    /// Executable tests for the report exporters. The PDF path renders through MigraDoc/PdfSharp
    /// (GDI) and is asserted to be a real PDF; the others are checked for a well-formed payload.
    /// Traces to US-DG-9 (exportable reports).
    /// </summary>
    public class ReportExporterTests : IDisposable
    {
        private readonly string _dir;

        public ReportExporterTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dg_report_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { /* best-effort cleanup */ }
        }

        private static AnalysisResult Sample()
        {
            var r = new AnalysisResult
            {
                SolutionUniqueName = "contoso_core",
                SolutionFriendlyName = "Contoso Core",
                SolutionVersion = "2.3.0.0",
                SolutionIsManaged = true,
                SourceEnvironment = "DEV",
                TargetEnvironment = "PROD",
                Score = 61,
                Risk = OverallRisk.High
            };
            r.AnalyzersRun.Add("Deleted Components");
            r.AnalyzersRun.Add("Flows & Plugins");
            r.Findings.Add(new RiskFinding(AnalyzerCategory.DeletedComponents, Severity.Critical,
                "Entity deleted on managed upgrade",
                "A table exists in the target's managed solution but not in the source; a managed upgrade deletes it.",
                "Entity:0001", "Back up affected data, then confirm the removal is intentional."));
            r.Findings.Add(new RiskFinding(AnalyzerCategory.FlowsAndPlugins, Severity.High,
                "Duplicate SDK step registration",
                "Two steps register the same plugin type on one event with overlapping filtering attributes.",
                "MyPlugin.PostCreate", "Remove or disable one of the duplicate steps."));
            r.Findings.Add(new RiskFinding(AnalyzerCategory.FlowsAndPlugins, Severity.Medium,
                "Plugin steps share an execution rank",
                "Two steps on account (Post-operation) share rank 1.",
                "'A', 'B'", "Assign distinct rank values."));
            return r;
        }

        private string Path_(string name) => Path.Combine(_dir, name);

        // TC-DG-RPT-01: the PDF exporter renders a valid, non-trivial PDF (MigraDoc/PdfSharp GDI).
        [Fact]
        public void Pdf_RendersValidPdf()
        {
            var path = Path_("report.pdf");
            PdfReportExporter.Export(Sample(), path);

            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 1000, $"PDF too small: {bytes.Length} bytes");
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        }

        // TC-DG-RPT-02: the HTML exporter writes a standalone HTML document containing the findings.
        [Fact]
        public void Html_WritesDocumentWithFindings()
        {
            var path = Path_("report.html");
            HtmlReportExporter.Export(Sample(), path);

            var html = File.ReadAllText(path);
            Assert.Contains("<html", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Duplicate SDK step registration", html);
        }

        // TC-DG-RPT-03: the JSON exporter emits parseable CI output with the score and pass flag.
        [Fact]
        public void Json_EmitsParseableCiPayload()
        {
            var path = Path_("report.json");
            JsonReportExporter.Export(Sample(), path, OverallRisk.High);

            var json = JObject.Parse(File.ReadAllText(path));
            Assert.Equal(61, (int)json["score"]);
            Assert.False((bool)json["ci"]["pass"]);           // High risk fails the High gate
            Assert.Equal(1, (int)json["ci"]["suggestedExitCode"]);
        }

        // TC-DG-RPT-04: the Markdown checklist ends with rollback guidance.
        [Fact]
        public void Markdown_IncludesRollbackGuidance()
        {
            var path = Path_("report.md");
            FixChecklistGenerator.Export(Sample(), path);

            var md = File.ReadAllText(path);
            Assert.Contains("Fix Checklist", md);
            Assert.Contains("Rollback guidance", md);
        }

        // TC-DG-RPT-06: an executive summary, when present, is embedded in PDF/HTML/JSON exports.
        [Fact]
        public void Summary_EmbeddedInExports()
        {
            var r = Sample();
            r.AiSummary = "GO WITH CAUTION — one critical deletion to review.\nMitigate before deploying.";

            var pdf = Path_("s.pdf");
            PdfReportExporter.Export(r, pdf);
            Assert.Equal("%PDF-", Encoding.ASCII.GetString(File.ReadAllBytes(pdf), 0, 5)); // renders with the summary block

            var html = Path_("s.html");
            HtmlReportExporter.Export(r, html);
            var htmlText = File.ReadAllText(html);
            Assert.Contains("Executive summary", htmlText);
            Assert.Contains("GO WITH CAUTION", htmlText);

            var jsonPath = Path_("s.json");
            JsonReportExporter.Export(r, jsonPath, OverallRisk.High);
            Assert.Equal(r.AiSummary, (string)JObject.Parse(File.ReadAllText(jsonPath))["aiSummary"]);
        }

        // TC-DG-RPT-05: the Excel exporter writes a valid .xlsx (ZIP/OOXML) file.
        [Fact]
        public void Excel_WritesXlsxPackage()
        {
            var path = Path_("report.xlsx");
            ExcelReportExporter.Export(Sample(), path);

            var bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length > 1000, $"XLSX too small: {bytes.Length} bytes");
            Assert.Equal("PK", Encoding.ASCII.GetString(bytes, 0, 2)); // OOXML is a ZIP container
        }
    }
}
