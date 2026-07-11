using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.ComponentUsageExplorer.Analysis;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;
// PluginControlBase pulls in a Label; disambiguate to the WinForms one (CS0104 guard for future edits).
using Label = System.Windows.Forms.Label;

namespace XrmToolSuite.ComponentUsageExplorer
{
    public partial class ComponentUsageExplorerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;
        private List<ComponentRef> _results = new List<ComponentRef>();
        private ComponentRef _selected;
        private UsageFootprint _footprint;
        private UsageReport _report;

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        // (display label, component-type code or null for "All types")
        private static readonly (string Label, int? Code)[] TypeFilters =
        {
            ("All types", null),
            ("Table (Entity)", 1),
            ("Column (Attribute)", 2),
            ("Form", 60),
            ("View (Saved Query)", 26),
            ("Chart", 59),
            ("Workflow / Flow", 29),
            ("Web Resource", 61),
            ("Security Role", 20),
            ("SDK Message Step (plugin)", 92),
            ("Model-driven App", 80),
            ("Canvas App", 300),
            ("Environment Variable", 380),
        };

        public ComponentUsageExplorerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Component Usage Explorer"));
        }

        #region Lifecycle

        private void ComponentUsageExplorerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();

            tscbType.Items.Clear();
            foreach (var f in TypeFilters) tscbType.Items.Add(f.Label);
            var idx = Array.FindIndex(TypeFilters, f => f.Code == _settings.LastTypeFilter);
            tscbType.SelectedIndex = idx >= 0 ? idx : 0;
            tstSearch.Text = _settings.LastSearch ?? string.Empty;

            grdResults.SelectionChanged += grdResults_SelectionChanged;
            ResetVerdictBanner();
            LogInfo("Component Usage Explorer loaded");
            SetStatusMessage("Type a component name or GUID and click 'Find', then 'Analyze usage'.");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
            {
                _settings.LastSearch = tstSearch.Text;
                _settings.LastTypeFilter = SelectedTypeFilter();
            }
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
            SetStatusMessage($"Connected to {detail?.ConnectionName}. Search for a component and click 'Find'.");
        }

        #endregion

        #region Find components (needs connection)

        private int? SelectedTypeFilter()
        {
            var i = tscbType.SelectedIndex;
            return i >= 0 && i < TypeFilters.Length ? TypeFilters[i].Code : null;
        }

        private void tsbFind_Click(object sender, EventArgs e) => ExecuteMethod(FindComponents);

        private void FindComponents()
        {
            var query = (tstSearch.Text ?? string.Empty).Trim();
            var typeFilter = SelectedTypeFilter();

            if (string.IsNullOrEmpty(query) && typeFilter == null)
            {
                MessageBox.Show(this,
                    "Enter a name/GUID to search, or pick a component type to browse.",
                    "Nothing to search", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RunAsync(
                "Searching components...",
                worker =>
                {
                    var collector = new UsageCollector();
                    return collector.Search(Service, query, typeFilter, worker);
                },
                results =>
                {
                    _results = results ?? new List<ComponentRef>();
                    PopulateResultsGrid(_results);
                    ClearAnalysis();
                    lblResultsHeader.Text = $"Search results — {_results.Count} match(es). Select one and click 'Analyze usage'.";
                    SetStatusMessage(_results.Count == 0
                        ? "No components matched. Broaden the search or pick a type."
                        : $"Found {_results.Count} component(s). Select one and click 'Analyze usage'.");
                });
        }

        private void PopulateResultsGrid(List<ComponentRef> results)
        {
            grdResults.Rows.Clear();
            foreach (var c in results)
            {
                int i = grdResults.Rows.Add(
                    c.ComponentTypeName,
                    c.Name ?? "(unnamed)",
                    c.SchemaName ?? "",
                    string.Join(", ", c.OwningSolutions ?? new List<string>()),
                    c.IsManaged ? "Managed" : "Unmanaged");
                grdResults.Rows[i].Tag = c;
            }
            grdResults.ClearSelection();
        }

        private void grdResults_SelectionChanged(object sender, EventArgs e)
        {
            _selected = grdResults.SelectedRows.Count > 0
                ? grdResults.SelectedRows[0].Tag as ComponentRef
                : null;
        }

        #endregion

        #region Analyze usage (needs connection)

        private void tsbAnalyze_Click(object sender, EventArgs e) => ExecuteMethod(AnalyzeUsage);

        private void AnalyzeUsage()
        {
            var component = _selected ?? (grdResults.SelectedRows.Count > 0
                ? grdResults.SelectedRows[0].Tag as ComponentRef
                : null);

            if (component == null)
            {
                MessageBox.Show(this, "Select a component in the results grid first (click 'Find' if empty).",
                    "No component selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RunAsync(
                $"Analyzing usage of '{component.Label}'...",
                worker =>
                {
                    var collector = new UsageCollector();
                    var footprint = collector.BuildFootprint(Service, component, worker,
                        msg => worker.ReportProgress(0, msg));
                    var report = UsageVerdictRules.Evaluate(footprint);
                    return (footprint, report);
                },
                result =>
                {
                    _footprint = result.footprint;
                    _report = result.report;
                    BindAnalysis(_footprint, _report);
                    tsbExport.Enabled = true;
                    SetStatusMessage(
                        $"'{component.Label}': {_report.VerdictLabel} — {_footprint.DependentComponents.Count} dependent(s), " +
                        $"{_footprint.RequiredComponents.Count} required. Impact score {_report.Score}/100.");
                });
        }

        private void BindAnalysis(UsageFootprint fp, UsageReport report)
        {
            SetVerdictBanner(report);

            grdRequired.Rows.Clear();
            foreach (var r in fp.RequiredComponents)
                grdRequired.Rows.Add(r.ComponentTypeName, r.Label, string.Join(", ", r.OwningSolutions ?? new List<string>()));
            lblRequiredHeader.Text = $"Required components ({fp.RequiredComponents.Count}) — this component depends on";

            grdDependents.Rows.Clear();
            foreach (var d in fp.DependentComponents)
            {
                int i = grdDependents.Rows.Add(d.ComponentTypeName, d.Label,
                    string.Join(", ", d.OwningSolutions ?? new List<string>()),
                    d.IsManaged ? "Managed" : "Unmanaged");
                if (d.IsManaged) grdDependents.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 200);
            }
            lblDependentsHeader.Text = $"Dependent components ({fp.DependentComponents.Count}) — depend on this component";

            grdUsage.Rows.Clear();
            foreach (var kv in fp.UsageByType.OrderByDescending(x => x.Value))
                grdUsage.Rows.Add(kv.Key, kv.Value.ToString());
            if (fp.UsageByType.Count == 0)
                grdUsage.Rows.Add("(no dependents)", "0");

            grdFindings.Rows.Clear();
            foreach (var f in report.Findings.OrderByDescending(x => x.Severity))
            {
                int i = grdFindings.Rows.Add(f.Severity.ToString(), f.Title, f.Description);
                grdFindings.Rows[i].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }

            txtExplanation.Text = report.Explanation ?? "";
        }

        private void SetVerdictBanner(UsageReport report)
        {
            var color = VerdictColor(report.Verdict);
            lblVerdict.BackColor = color;
            lblVerdict.ForeColor = ContrastText(color);
            lblVerdict.Text = $"  {report.VerdictLabel}   —   impact score {report.Score}/100 ({report.Band})" +
                              $"   ·   {_selected?.Label ?? _footprint?.Component?.Label}";
        }

        private void ResetVerdictBanner()
        {
            lblVerdict.BackColor = SystemColors.Control;
            lblVerdict.ForeColor = SystemColors.ControlText;
            lblVerdict.Text = "Select a component and click 'Analyze usage' for a change-safety verdict.";
        }

        #endregion

        #region Export

        private void tsmExportExcel_Click(object sender, EventArgs e) =>
            ExportWith("Excel (*.xlsx)|*.xlsx", "component-usage.xlsx",
                path => ExcelReportExporter.Export(BuildReportModel(), path));

        private void tsmExportPdf_Click(object sender, EventArgs e) =>
            ExportWith("PDF (*.pdf)|*.pdf", "component-usage.pdf",
                path => PdfReportExporter.Export(BuildReportModel(), path));

        private void tsmExportJson_Click(object sender, EventArgs e) =>
            ExportWith("JSON (*.json)|*.json", "component-usage.json",
                path => JsonReportExporter.Export(BuildReportModel(), path));

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportWith("HTML (*.html)|*.html", "component-usage.html",
                path => File.WriteAllText(path, BuildHtml(), Encoding.UTF8));

        private void ExportWith(string filter, string fileName, Action<string> writer)
        {
            if (_report == null || _footprint == null)
            {
                MessageBox.Show(this, "Analyze a component's usage first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = fileName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var path = dlg.FileName;
                RunAsync(
                    "Exporting usage report...",
                    worker => { writer(path); return path; },
                    written => { SetStatusMessage("Exported component usage report to " + written); PromptOpenExportedFile(written); });
            }
        }

        private ReportModel BuildReportModel()
        {
            var c = _footprint.Component;
            var r = new ReportModel
            {
                ToolName = "Component Usage Explorer",
                ToolVersion = GetType().Assembly.GetName().Version?.ToString(),
                ReportTitle = "Component Usage & Change-Safety Report",
                Subtitle = "Where-used footprint and change-safety verdict",
                ScoreWord = "impact",
                SubjectName = c?.Label,
                SubjectKey = c?.SchemaName,
                IsManaged = c?.IsManaged,
                Score = _report.Score,
                Band = _report.Band,
                LeadIn = $"Change-safety verdict: {_report.VerdictLabel}. {_report.Explanation}"
            };

            r.Metrics.Add(new MetricRow("Component", c?.Label));
            r.Metrics.Add(new MetricRow("Component type", c?.ComponentTypeName));
            r.Metrics.Add(new MetricRow("Verdict", _report.VerdictLabel));
            foreach (var m in _report.Metrics)
                r.Metrics.Add(m);
            if (_footprint.DependencyDataIncomplete)
                r.Metrics.Add(new MetricRow("Dependency data", "Incomplete (verify manually)"));

            foreach (var f in _report.Findings)
                r.Findings.Add(new Finding(f.Category, f.Severity, f.Title, f.Description,
                    component: c?.Label, recommendation: f.Recommendation, helpUrl: f.HelpUrl));

            r.NextSteps.Add(new NextStep(_report.VerdictLabel, _report.Explanation));
            return r;
        }

        private string BuildHtml()
        {
            var c = _footprint.Component;
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Component Usage Report</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                "h1{font-size:20px}h2{font-size:15px;margin-top:22px}" +
                "table{border-collapse:collapse;width:100%;margin-top:8px}" +
                "th,td{border:1px solid #ccc;padding:5px 8px;text-align:left;font-size:12px;vertical-align:top}" +
                "th{background:#f4f4f4}.verdict{padding:10px 14px;border-radius:6px;font-weight:bold;font-size:16px}" +
                ".sev-High,.sev-Critical{color:#a94442;font-weight:bold}.sev-Medium{color:#8a6d3b}" +
                ".sev-Low{color:#31708f}.sev-Info{color:#3c763d}</style></head><body>");
            sb.AppendLine("<h1>Component Usage &amp; Change-Safety Report</h1>");
            var vc = VerdictColor(_report.Verdict);
            sb.AppendLine($"<p class=\"verdict\" style=\"background:{ToHex(vc)};color:{ToHex(ContrastText(vc))}\">" +
                $"{H(_report.VerdictLabel)} — impact {_report.Score}/100 ({_report.Band})</p>");
            sb.AppendLine($"<p><b>Component:</b> {H(c?.Label)} " +
                $"<span style=\"color:#666\">({H(c?.ComponentTypeName)}{(c != null && c.IsManaged ? ", managed" : "")})</span></p>");
            sb.AppendLine($"<p>{H(_report.Explanation)}</p>");
            if (_footprint.DependencyDataIncomplete)
                sb.AppendLine("<p style=\"color:#8a6d3b\"><b>Note:</b> platform dependency data was incomplete — verify usage manually.</p>");

            sb.AppendLine($"<h2>Dependent components ({_footprint.DependentComponents.Count})</h2>");
            sb.AppendLine("<table><tr><th>Type</th><th>Name</th><th>Owning solution(s)</th><th>Managed</th></tr>");
            foreach (var d in _footprint.DependentComponents)
                sb.AppendLine($"<tr><td>{H(d.ComponentTypeName)}</td><td>{H(d.Label)}</td>" +
                    $"<td>{H(string.Join(", ", d.OwningSolutions ?? new List<string>()))}</td>" +
                    $"<td>{(d.IsManaged ? "Yes" : "No")}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine($"<h2>Required components ({_footprint.RequiredComponents.Count})</h2>");
            sb.AppendLine("<table><tr><th>Type</th><th>Name</th><th>Owning solution(s)</th></tr>");
            foreach (var rq in _footprint.RequiredComponents)
                sb.AppendLine($"<tr><td>{H(rq.ComponentTypeName)}</td><td>{H(rq.Label)}</td>" +
                    $"<td>{H(string.Join(", ", rq.OwningSolutions ?? new List<string>()))}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Usage by type</h2>");
            sb.AppendLine("<table><tr><th>Component type</th><th>Count</th></tr>");
            foreach (var kv in _footprint.UsageByType.OrderByDescending(x => x.Value))
                sb.AppendLine($"<tr><td>{H(kv.Key)}</td><td>{kv.Value}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Findings</h2>");
            sb.AppendLine("<table><tr><th>Severity</th><th>Finding</th><th>Detail</th><th>Recommendation</th></tr>");
            foreach (var f in _report.Findings.OrderByDescending(x => x.Severity))
                sb.AppendLine($"<tr><td class=\"sev-{f.Severity}\">{f.Severity}</td><td>{H(f.Title)}</td>" +
                    $"<td>{H(f.Description)}</td><td>{H(f.Recommendation)}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine($"<p style=\"color:#666;margin-top:18px\">Generated {DateTime.Now:u}. Read-only analysis.</p>");
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        #endregion

        #region Helpers

        private void ClearResults()
        {
            _results = new List<ComponentRef>();
            _selected = null;
            grdResults.Rows.Clear();
            ClearAnalysis();
            lblResultsHeader.Text = "Search results — type a name/GUID and click Find, then select a component";
        }

        private void ClearAnalysis()
        {
            _footprint = null;
            _report = null;
            grdRequired.Rows.Clear();
            grdDependents.Rows.Clear();
            grdUsage.Rows.Clear();
            grdFindings.Rows.Clear();
            txtExplanation.Text = "";
            tsbExport.Enabled = false;
            ResetVerdictBanner();
        }

        private static Color VerdictColor(ChangeSafety v)
        {
            switch (v)
            {
                case ChangeSafety.SafeToChange: return Color.FromArgb(198, 239, 206);   // green
                case ChangeSafety.ChangeWithCaution: return Color.FromArgb(255, 235, 156); // amber
                case ChangeSafety.HighImpact: return Color.FromArgb(255, 199, 206);      // red
                case ChangeSafety.DoNotDelete: return Color.FromArgb(192, 0, 0);         // deep red
                case ChangeSafety.RequiresDependencyReview: return Color.FromArgb(197, 217, 241); // blue
                case ChangeSafety.RequiresAlmReview: return Color.FromArgb(228, 178, 240);  // purple
                default: return SystemColors.Control;
            }
        }

        private static Color ContrastText(Color bg)
        {
            // Perceived luminance — dark text on light backgrounds, white on dark.
            var luminance = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255.0;
            return luminance > 0.55 ? Color.FromArgb(30, 30, 30) : Color.White;
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

        private static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static string H(string s) => string.IsNullOrEmpty(s)
            ? string.Empty
            : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        #endregion
    }

    /// <summary>Persisted UI state (POCO — no controls/services/credentials).</summary>
    public class ToolSettings
    {
        public string LastSearch { get; set; }
        public int? LastTypeFilter { get; set; }
    }
}
