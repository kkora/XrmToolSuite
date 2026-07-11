using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.JavaScriptPerformanceAnalyzer.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;
// PluginControlBase pulls in a Label; disambiguate to the WinForms one (CS0104 guard for future edits).
using Label = System.Windows.Forms.Label;

namespace XrmToolSuite.JavaScriptPerformanceAnalyzer
{
    public partial class JavaScriptPerformanceAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;
        private List<JsScriptAnalysis> _scripts = new List<JsScriptAnalysis>();
        private List<FormScriptUsage> _usages = new List<FormScriptUsage>();
        private List<Finding> _formFindings = new List<Finding>();

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public JavaScriptPerformanceAnalyzerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("JavaScript Performance Analyzer"));
        }

        private void JavaScriptPerformanceAnalyzerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();
            txtSearch.Text = _settings.LastSearch ?? "";
            LogInfo("JavaScript Performance Analyzer loaded");
            SetStatusMessage("Click 'Analyze web resources' to statically scan every JScript web resource.");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
                _settings.LastSearch = txtSearch.Text;
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService,
            ConnectionDetail detail,
            string actionName,
            object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            ClearResults();
            SetStatusMessage($"Connected to {detail?.ConnectionName}. Click 'Analyze web resources'.");
        }


        // ----------------------------------------------------------------- Analyze (needs connection)

        private void tsbAnalyze_Click(object sender, EventArgs e) => ExecuteMethod(AnalyzeWebResources);

        private void AnalyzeWebResources()
        {
            var options = OptionsFromSettings();

            RunAsync(
                "Analyzing JavaScript web resources...",
                worker =>
                {
                    var collector = new JsCollector(options);
                    var scripts = collector.Collect(Service, worker, msg => worker.ReportProgress(0, msg));
                    worker.ReportProgress(0, "Mapping form event handlers...");
                    var usages = collector.CollectFormUsage(Service, worker);
                    return new AnalyzeResult
                    {
                        Scripts = scripts,
                        Usages = usages,
                        FormFindings = new List<Finding>(collector.LastFormFindings)
                    };
                },
                result =>
                {
                    _scripts = result.Scripts ?? new List<JsScriptAnalysis>();
                    _usages = result.Usages ?? new List<FormScriptUsage>();
                    _formFindings = result.FormFindings ?? new List<Finding>();

                    PopulateScriptsGrid();
                    txtSummary.Text = BuildDashboardText();
                    tsbExport.Enabled = _scripts.Count > 0;

                    if (_scripts.Count == 0)
                    {
                        SetStatusMessage("No JScript web resources found in this environment.");
                        return;
                    }

                    grdScripts.ClearSelection();
                    if (grdScripts.Rows.Count > 0)
                        grdScripts.Rows[0].Selected = true;

                    SetStatusMessage(
                        $"Analyzed {_scripts.Count} script(s). Worst score {_scripts.Max(s => s.Score)}/100. " +
                        $"{_formFindings.Count} form(s) flagged for heavy OnLoad.");
                });
        }

        private JsAnalysisOptions OptionsFromSettings()
        {
            return new JsAnalysisOptions
            {
                ConsoleWarn = _settings?.ConsoleWarn > 0 ? _settings.ConsoleWarn : 10,
                SizeWarnBytes = _settings?.SizeWarnBytes > 0 ? _settings.SizeWarnBytes : 51200,
                SizeHighBytes = _settings?.SizeHighBytes > 0 ? _settings.SizeHighBytes : 204800,
                RepeatedRetrieveWarn = _settings?.RepeatedRetrieveWarn > 0 ? _settings.RepeatedRetrieveWarn : 3,
                OnLoadHandlerWarn = _settings?.OnLoadHandlerWarn > 0 ? _settings.OnLoadHandlerWarn : 5
            };
        }

        // ----------------------------------------------------------------- Scripts grid + search filter

        private IEnumerable<JsScriptAnalysis> FilteredScripts()
        {
            var term = (txtSearch.Text ?? "").Trim();
            if (term.Length == 0) return _scripts;
            return _scripts.Where(s => (s.Code ?? "").IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void PopulateScriptsGrid()
        {
            grdScripts.Rows.Clear();
            foreach (var s in FilteredScripts())
            {
                int rowIndex = grdScripts.Rows.Add(
                    s.Score,
                    s.Band.ToString(),
                    s.ScriptName ?? "(unnamed)",
                    FormatSize(s.SizeBytes),
                    s.Findings.Count(f => f.Severity >= Severity.Low));
                grdScripts.Rows[rowIndex].Tag = s;
                grdScripts.Rows[rowIndex].DefaultCellStyle.BackColor = BandColor(s.Band);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_scripts.Count == 0) return;
            PopulateScriptsGrid();
            SetStatusMessage($"{grdScripts.Rows.Count} of {_scripts.Count} script(s) match the search filter.");
        }

        private void grdScripts_SelectionChanged(object sender, EventArgs e)
        {
            var script = grdScripts.SelectedRows.Count > 0
                ? grdScripts.SelectedRows[0].Tag as JsScriptAnalysis
                : null;

            if (script == null)
            {
                grdFindings.Rows.Clear();
                txtCode.Text = "";
                lstUsage.Items.Clear();
                lblFindingsHeader.Text = "Findings for the selected script";
                lblUsageHeader.Text = "Form / event usage";
                return;
            }

            lblFindingsHeader.Text = $"Findings for: {script.ScriptName}";
            PopulateFindings(script);

            txtCode.Text = script.Code ?? "";

            var usages = _usages
                .Where(u => string.Equals(u.ScriptLibrary, script.ScriptName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            lstUsage.BeginUpdate();
            lstUsage.Items.Clear();
            foreach (var u in usages)
                lstUsage.Items.Add($"{u.Entity} — {u.FormName} · {u.Event}" +
                    (string.IsNullOrWhiteSpace(u.FunctionName) ? "" : $" → {u.FunctionName}"));
            if (usages.Count == 0)
                lstUsage.Items.Add("(no form/event references found for this library)");
            lstUsage.EndUpdate();
            lblUsageHeader.Text = $"Form / event usage ({usages.Count})";
        }

        private void PopulateFindings(JsScriptAnalysis script)
        {
            grdFindings.Rows.Clear();
            foreach (var f in script.Findings.OrderByDescending(x => x.Severity))
            {
                var jf = f as JsFinding;
                int rowIndex = grdFindings.Rows.Add(
                    f.Severity.ToString(),
                    f.Title,
                    jf != null && jf.Line > 0 ? jf.Line.ToString() : "",
                    jf?.CodeLine ?? "",
                    jf?.Confidence ?? "",
                    f.Recommendation ?? "");
                grdFindings.Rows[rowIndex].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
        }

        // ----------------------------------------------------------------- Export

        private void tsmExportExcel_Click(object sender, EventArgs e) =>
            ExportWith("Excel (*.xlsx)|*.xlsx", "javascript-performance.xlsx",
                path => ExcelReportExporter.Export(BuildReportModel(), path));

        private void tsmExportPdf_Click(object sender, EventArgs e) =>
            ExportWith("PDF (*.pdf)|*.pdf", "javascript-performance.pdf",
                path => PdfReportExporter.Export(BuildReportModel(), path));

        private void tsmExportJson_Click(object sender, EventArgs e) =>
            ExportWith("JSON (*.json)|*.json", "javascript-performance.json",
                path => JsonReportExporter.Export(BuildReportModel(), path));

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportWith("HTML (*.html)|*.html", "javascript-performance.html",
                path => System.IO.File.WriteAllText(path, BuildHtml(), Encoding.UTF8));

        private void tsmExportMarkdown_Click(object sender, EventArgs e) =>
            ExportWith("Markdown (*.md)|*.md", "javascript-performance.md",
                path => System.IO.File.WriteAllText(path, BuildMarkdown(), Encoding.UTF8));

        private void tsmExportCsv_Click(object sender, EventArgs e) =>
            ExportWith("CSV (*.csv)|*.csv", "javascript-performance.csv",
                path => System.IO.File.WriteAllText(path, BuildCsv(), Encoding.UTF8));

        private void ExportWith(string filter, string fileName, Action<string> writer)
        {
            if (_scripts == null || _scripts.Count == 0)
            {
                MessageBox.Show("Analyze web resources first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = fileName })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    writer(dlg.FileName);
                    SetStatusMessage("Exported JavaScript analysis to " + dlg.FileName);
                    PromptOpenExportedFile(dlg.FileName);
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }
        }

        private ReportModel BuildReportModel()
        {
            int worst = _scripts.Count > 0 ? _scripts.Max(s => s.Score) : 0;

            var r = new ReportModel
            {
                ToolName = "JavaScript Performance Analyzer",
                ReportTitle = "JavaScript Performance Analysis",
                ScoreWord = "risk",
                SubjectName = "JavaScript web resources",
                Score = worst,
                Band = ScoreCalculator.BandFor(worst, 15, 40),
                LeadIn = "Static analysis of JScript web resources plus their form/event usage. Findings are " +
                         "labeled heuristics (regex/line scans may match comments or strings) — no runtime was executed."
            };

            r.Metrics.Add(new MetricRow("Scripts analyzed", _scripts.Count.ToString()));
            r.Metrics.Add(new MetricRow("High-band scripts", _scripts.Count(s => s.Band == ScoreBand.High).ToString()));
            r.Metrics.Add(new MetricRow("Medium-band scripts", _scripts.Count(s => s.Band == ScoreBand.Medium).ToString()));
            r.Metrics.Add(new MetricRow("Low-band scripts", _scripts.Count(s => s.Band == ScoreBand.Low).ToString()));
            r.Metrics.Add(new MetricRow("Total findings",
                _scripts.Sum(s => s.Findings.Count(f => f.Severity >= Severity.Low)).ToString()));
            r.Metrics.Add(new MetricRow("Forms with heavy OnLoad", _formFindings.Count.ToString()));
            var worstScript = _scripts.OrderByDescending(s => s.Score).FirstOrDefault();
            if (worstScript != null)
                r.Metrics.Add(new MetricRow("Worst script", worstScript.ScriptName, $"score {worstScript.Score}/100"));

            // Aggregate every actionable finding across scripts, tagged with its script name as Component.
            foreach (var s in _scripts)
                foreach (var f in s.Findings.Where(x => x.Severity >= Severity.Low))
                    r.Findings.Add(new Finding(f.Category, f.Severity, f.Title, f.Description,
                        component: s.ScriptName, recommendation: f.Recommendation, helpUrl: f.HelpUrl));

            // Form-level findings (heavy OnLoad handlers) round out the report.
            foreach (var f in _formFindings)
                r.Findings.Add(f);

            return r;
        }

        // ----------------------------------------------------------------- Dashboard + BCL exports

        private string BuildDashboardText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Scripts: {_scripts.Count}   " +
                $"Bands: High {_scripts.Count(s => s.Band == ScoreBand.High)}   " +
                $"Medium {_scripts.Count(s => s.Band == ScoreBand.Medium)}   " +
                $"Low {_scripts.Count(s => s.Band == ScoreBand.Low)}   (heuristic — no runtime executed)");
            sb.AppendLine($"Findings: {_scripts.Sum(s => s.Findings.Count(f => f.Severity >= Severity.Low))}   " +
                $"Forms with heavy OnLoad: {_formFindings.Count}");
            var top = _scripts.OrderByDescending(s => s.Score).Take(3).ToList();
            if (top.Count > 0)
                sb.AppendLine("Worst: " + string.Join("   ", top.Select(s => $"{s.ScriptName} ({s.Score})")));
            return sb.ToString();
        }

        private string BuildCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Score,Band,Script,SizeBytes,Findings,HighestSeverity");
            foreach (var s in _scripts)
            {
                var worst = s.Findings.Count > 0 ? s.Findings.Max(f => f.Severity).ToString() : "Info";
                sb.AppendLine(string.Join(",", new[]
                {
                    s.Score.ToString(), Csv(s.Band.ToString()), Csv(s.ScriptName),
                    s.SizeBytes.ToString(),
                    s.Findings.Count(f => f.Severity >= Severity.Low).ToString(), Csv(worst)
                }));
            }
            return sb.ToString();
        }

        private static string Csv(string s)
        {
            s = s ?? "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private string BuildMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# JavaScript Performance Analysis");
            sb.AppendLine();
            sb.AppendLine($"- **Scripts analyzed:** {_scripts.Count}");
            sb.AppendLine($"- **Bands:** High {_scripts.Count(s => s.Band == ScoreBand.High)}, " +
                $"Medium {_scripts.Count(s => s.Band == ScoreBand.Medium)}, Low {_scripts.Count(s => s.Band == ScoreBand.Low)}");
            sb.AppendLine($"- **Forms with heavy OnLoad:** {_formFindings.Count}");
            sb.AppendLine("- _Findings are labeled heuristics (no runtime executed)._");
            sb.AppendLine();
            sb.AppendLine("## Scripts (ranked by risk)");
            sb.AppendLine();
            sb.AppendLine("| Score | Band | Script | Size (bytes) | #Findings |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var s in _scripts)
                sb.AppendLine($"| {s.Score} | {s.Band} | {Md(s.ScriptName)} | {s.SizeBytes} | " +
                    $"{s.Findings.Count(f => f.Severity >= Severity.Low)} |");
            return sb.ToString();
        }

        private static string Md(string s) => (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

        private string BuildHtml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>JavaScript Performance Analysis</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                "table{border-collapse:collapse;width:100%;margin-top:12px}th,td{border:1px solid #ccc;padding:6px 8px;text-align:left;vertical-align:top}" +
                "th{background:#f4f4f4}.band-High{background:#ffcdd2}.band-Medium{background:#fff5c8}.band-Low{background:#e2f0d9}</style></head><body>");
            sb.AppendLine("<h1>JavaScript Performance Analysis</h1>");
            sb.AppendLine($"<p><b>Scripts analyzed:</b> {_scripts.Count}. " +
                $"<b>Bands:</b> High {_scripts.Count(s => s.Band == ScoreBand.High)}, " +
                $"Medium {_scripts.Count(s => s.Band == ScoreBand.Medium)}, Low {_scripts.Count(s => s.Band == ScoreBand.Low)}. " +
                $"<b>Forms with heavy OnLoad:</b> {_formFindings.Count}. " +
                "Findings are labeled heuristics (no runtime executed).</p>");
            sb.AppendLine("<table><tr><th>Score</th><th>Band</th><th>Script</th><th>Size (bytes)</th><th>#Findings</th></tr>");
            foreach (var s in _scripts)
            {
                sb.AppendLine($"<tr class=\"band-{s.Band}\"><td>{s.Score}</td><td>{s.Band}</td><td>{Html(s.ScriptName)}</td>" +
                    $"<td>{s.SizeBytes}</td><td>{s.Findings.Count(f => f.Severity >= Severity.Low)}</td></tr>");
            }
            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }

        private static string Html(string s) => (s ?? "")
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        // ----------------------------------------------------------------- Helpers

        private void ClearResults()
        {
            _scripts = new List<JsScriptAnalysis>();
            _usages = new List<FormScriptUsage>();
            _formFindings = new List<Finding>();
            grdScripts.Rows.Clear();
            grdFindings.Rows.Clear();
            txtCode.Text = "";
            lstUsage.Items.Clear();
            txtSummary.Text = "";
            tsbExport.Enabled = false;
        }

        private static string FormatSize(int bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
            return $"{bytes / (1024.0 * 1024.0):0.#} MB";
        }

        private static Color BandColor(ScoreBand band)
        {
            switch (band)
            {
                case ScoreBand.High: return Color.FromArgb(255, 205, 210);
                case ScoreBand.Medium: return Color.FromArgb(255, 245, 200);
                default: return Color.FromArgb(226, 240, 217);
            }
        }

        private static Color SeverityColor(Severity s)
        {
            switch (s)
            {
                case Severity.Critical: return Color.FromArgb(255, 205, 210);
                case Severity.High: return Color.FromArgb(255, 224, 178);
                case Severity.Medium: return Color.FromArgb(255, 245, 200);
                case Severity.Low: return Color.FromArgb(226, 240, 217);
                default: return Color.White;
            }
        }

        private sealed class AnalyzeResult
        {
            public List<JsScriptAnalysis> Scripts { get; set; }
            public List<FormScriptUsage> Usages { get; set; }
            public List<Finding> FormFindings { get; set; }
        }
    }

    /// <summary>Serializable settings POCO round-tripped via BaseToolControl LoadSettings/SaveSettings.</summary>
    public class ToolSettings
    {
        public string LastSearch { get; set; }
        public int ConsoleWarn { get; set; } = 10;
        public int SizeWarnBytes { get; set; } = 51200;
        public int SizeHighBytes { get; set; } = 204800;
        public int RepeatedRetrieveWarn { get; set; } = 3;
        public int OnLoadHandlerWarn { get; set; } = 5;
    }
}
