using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.ManagedSolutionImpactChecker.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.ManagedSolutionImpactChecker
{
    public partial class ManagedSolutionImpactCheckerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ImpactSettings _settings;
        private List<Entity> _solutions = new List<Entity>();
        private ImpactReport _report;
        private ImpactCollector.ImpactCollectionResult _lastCollection;

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public ManagedSolutionImpactCheckerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Managed Solution Impact Checker"));

            foreach (var p in Enum.GetNames(typeof(DeploymentPath)))
                cboPath.Items.Add(p);
            cboPath.SelectedIndex = 0; // Upgrade
        }

        private void ManagedSolutionImpactCheckerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ImpactSettings>();
            if (!string.IsNullOrEmpty(_settings.LastPath) && cboPath.Items.Contains(_settings.LastPath))
                cboPath.SelectedItem = _settings.LastPath;
            LogInfo("Managed Solution Impact Checker loaded");
            SetStatusMessage("Click 'Refresh solutions', pick a managed solution and a path, then 'Analyze impact'.");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
            {
                _settings.LastSolution = SelectedSolutionUniqueName() ?? _settings.LastSolution;
                _settings.LastPath = cboPath.SelectedItem?.ToString() ?? _settings.LastPath;
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
            cboSolution.Items.Clear();
            _solutions = new List<Entity>();
            ClearResults();
            SetStatusMessage($"Connected to {detail?.ConnectionName}. Click 'Refresh solutions'.");
        }


        // ----------------------------------------------------------------- Solution picker (needs connection)

        private void tsbRefreshSolutions_Click(object sender, EventArgs e) => ExecuteMethod(LoadSolutions);

        private void LoadSolutions()
        {
            RunAsync(
                "Loading managed solutions…",
                worker =>
                {
                    var qe = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("friendlyname", "uniquename", "version", "ismanaged", "publisherid"),
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                                new ConditionExpression("ismanaged", ConditionOperator.Equal, true)
                            }
                        },
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };
                    return Service.RetrieveAll(qe, worker: worker)
                        .Where(s =>
                        {
                            var u = s.GetAttributeValue<string>("uniquename");
                            return u != "Default" && u != "Active";
                        })
                        .ToList();
                },
                solutions =>
                {
                    _solutions = solutions;
                    cboSolution.Items.Clear();
                    foreach (var s in _solutions)
                        cboSolution.Items.Add(SolutionLabel(s));

                    int dropWidth = cboSolution.Width;
                    foreach (var item in cboSolution.Items)
                        dropWidth = Math.Max(dropWidth, TextRenderer.MeasureText(item.ToString(), cboSolution.Font).Width);
                    cboSolution.ComboBox.DropDownWidth = dropWidth + SystemInformation.VerticalScrollBarWidth + 8;

                    if (!string.IsNullOrEmpty(_settings?.LastSolution))
                    {
                        var idx = _solutions.FindIndex(s =>
                            string.Equals(s.GetAttributeValue<string>("uniquename"), _settings.LastSolution, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0) cboSolution.SelectedIndex = idx;
                    }
                    if (cboSolution.SelectedIndex < 0 && cboSolution.Items.Count > 0)
                        cboSolution.SelectedIndex = 0;

                    SetStatusMessage($"{_solutions.Count} managed solution(s) loaded. Pick one and click 'Analyze impact'.");
                });
        }

        private static string SolutionLabel(Entity s) =>
            $"{s.GetAttributeValue<string>("friendlyname")} ({s.GetAttributeValue<string>("uniquename")}) v{s.GetAttributeValue<string>("version")}";

        private Entity SelectedSolution() =>
            cboSolution.SelectedIndex >= 0 && cboSolution.SelectedIndex < _solutions.Count
                ? _solutions[cboSolution.SelectedIndex]
                : null;

        private string SelectedSolutionUniqueName() =>
            SelectedSolution()?.GetAttributeValue<string>("uniquename");

        private DeploymentPath SelectedPath() =>
            (DeploymentPath)Enum.Parse(typeof(DeploymentPath), cboPath.SelectedItem?.ToString() ?? "Upgrade");

        // ----------------------------------------------------------------- Analyze (needs connection)

        private void tsbAnalyze_Click(object sender, EventArgs e) => ExecuteMethod(AnalyzeImpact);

        private void AnalyzeImpact()
        {
            var solution = SelectedSolution();
            if (solution == null)
            {
                MessageBox.Show("Pick a managed solution first (click 'Refresh solutions' if the list is empty).",
                    "No solution selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var path = SelectedPath();

            RunAsync(
                $"Analyzing {path} impact for {solution.GetAttributeValue<string>("friendlyname")}…",
                worker =>
                {
                    var collection = new ImpactCollector().Collect(Service, solution, worker,
                        msg => worker.ReportProgress(0, msg));
                    var report = LayerImpactRules.Evaluate(collection.Input, path);
                    // Merge the collector's Info notes (degraded queries) into the findings.
                    if (collection.Notes.Count > 0)
                        report.Findings.InsertRange(0, collection.Notes);
                    return (collection, report);
                },
                r =>
                {
                    _lastCollection = r.collection;
                    _report = r.report;
                    PopulateFindings(_report);
                    PopulateList(lstChecklist, _report.Checklist);
                    PopulateList(lstRollback, _report.RollbackGuidance);
                    txtSummary.Text = BuildSummary(r.collection, _report, path);
                    tsbExport.Enabled = true;
                    SetStatusMessage(
                        $"{path} impact for '{r.collection.SolutionFriendlyName}': score {_report.Score}/100 ({_report.Band}). " +
                        $"{_report.Findings.Count(f => f.Severity >= Severity.Medium)} actionable finding(s).");
                });
        }

        private void PopulateFindings(ImpactReport report)
        {
            grdFindings.Rows.Clear();
            foreach (var f in report.Findings.OrderByDescending(x => x.Severity))
            {
                int rowIndex = grdFindings.Rows.Add(
                    f.Severity.ToString(),
                    f.Category ?? "",
                    f.Component ?? "",
                    f.Recommendation ?? "");
                grdFindings.Rows[rowIndex].Tag = f;
                grdFindings.Rows[rowIndex].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
        }

        private static void PopulateList(ListBox list, List<string> lines)
        {
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var line in lines ?? new List<string>())
                list.Items.Add(line);
            list.EndUpdate();
        }

        private string BuildSummary(ImpactCollector.ImpactCollectionResult c, ImpactReport report, DeploymentPath path)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Solution: {c.SolutionFriendlyName} ({c.SolutionUniqueName}) v{c.SolutionVersion}" +
                (c.IsManaged ? " [managed]" : " [UNMANAGED]"));
            sb.AppendLine($"Deployment path: {path}   Impact score: {report.Score}/100 ({report.Band})   " +
                (LayerImpactRules.PathDeletes(path) ? "This path DELETES components missing from the incoming solution."
                                                    : "This path does NOT delete components."));
            sb.AppendLine(ScoreCalculator.Explain(report.Findings, report.Score, report.Band, "impact"));
            var metricLine = string.Join("   ", report.Metrics
                .Where(m => m.Label != "Deployment path")
                .Select(m => $"{m.Label}: {m.Value}"));
            sb.AppendLine(metricLine);
            return sb.ToString();
        }

        // ----------------------------------------------------------------- Export

        private void tsmExportExcel_Click(object sender, EventArgs e) =>
            ExportWith("Excel (*.xlsx)|*.xlsx", "managed-solution-impact.xlsx",
                path => ExcelReportExporter.Export(BuildReportModel(), path));

        private void tsmExportPdf_Click(object sender, EventArgs e) =>
            ExportWith("PDF (*.pdf)|*.pdf", "managed-solution-impact.pdf",
                path => PdfReportExporter.Export(BuildReportModel(), path));

        private void tsmExportJson_Click(object sender, EventArgs e) =>
            ExportWith("JSON (*.json)|*.json", "managed-solution-impact.json",
                path => JsonReportExporter.Export(BuildReportModel(), path));

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportWith("HTML (*.html)|*.html", "managed-solution-impact.html",
                path => System.IO.File.WriteAllText(path, BuildHtml(), Encoding.UTF8));

        private void ExportWith(string filter, string fileName, Action<string> writer)
        {
            if (_report == null)
            {
                MessageBox.Show("Analyze a solution first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = fileName })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var target = dlg.FileName;
                RunAsync(
                    "Exporting report…",
                    worker => { writer(target); return target; },
                    saved => { SetStatusMessage("Exported impact report to " + saved); PromptOpenExportedFile(saved); });
            }
        }

        private ReportModel BuildReportModel()
        {
            var c = _lastCollection;
            var path = SelectedPath();

            var r = new ReportModel
            {
                ToolName = "Managed Solution Impact Checker",
                ReportTitle = "Managed Solution Impact Report",
                ScoreWord = "impact",
                SubjectName = c?.SolutionFriendlyName,
                SubjectKey = c?.SolutionUniqueName,
                SubjectVersion = c?.SolutionVersion,
                IsManaged = c?.IsManaged,
                Score = _report.Score,
                Band = _report.Band,
                LeadIn = $"Read-only {path} impact analysis of the managed solution's layering, deletion/overwrite " +
                         "risk, dependencies, and managed-property/publisher restrictions. " +
                         (LayerImpactRules.PathDeletes(path)
                             ? "This path deletes components missing from the incoming solution (and their data)."
                             : "This path does not delete components."),
                VerdictHigh = "High impact — resolve the critical/high findings and complete the pre-upgrade checklist before proceeding.",
                VerdictMedium = "Medium impact — review the findings and complete the checklist before proceeding.",
                VerdictLow = "Low impact — standard change control applies."
            };

            foreach (var m in _report.Metrics)
                r.Metrics.Add(m);

            foreach (var f in _report.Findings)
                r.Findings.Add(f);

            // Rollback guidance rides along on the fix checklist (per CLAUDE.md export pattern).
            foreach (var line in _report.RollbackGuidance)
                r.ChecklistGuidance.Add("Rollback: " + line);

            return r;
        }

        // ----------------------------------------------------------------- HTML (BCL only)

        private string BuildHtml()
        {
            var c = _lastCollection;
            var path = SelectedPath();
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Managed Solution Impact Report</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                "h1{margin-bottom:4px}table{border-collapse:collapse;width:100%;margin-top:12px}" +
                "th,td{border:1px solid #ccc;padding:6px 8px;text-align:left;vertical-align:top}th{background:#f4f4f4}" +
                ".sev-Critical{background:#ffcdd2}.sev-High{background:#ffe0b2}.sev-Medium{background:#fff5c8}" +
                ".sev-Low{background:#e2f0d9}.sev-Info{background:#eef}ul{margin-top:6px}</style></head><body>");
            sb.AppendLine($"<h1>Managed Solution Impact Report</h1>");
            sb.AppendLine($"<p><b>Solution:</b> {Html(c?.SolutionFriendlyName)} ({Html(c?.SolutionUniqueName)}) v{Html(c?.SolutionVersion)}<br>");
            sb.AppendLine($"<b>Deployment path:</b> {path} — " +
                (LayerImpactRules.PathDeletes(path) ? "deletes components missing from the incoming solution."
                                                    : "does not delete components.") + "<br>");
            sb.AppendLine($"<b>Impact score:</b> {_report.Score}/100 ({_report.Band})</p>");

            sb.AppendLine("<h2>Findings</h2>");
            sb.AppendLine("<table><tr><th>Severity</th><th>Category</th><th>Component</th><th>Recommendation</th></tr>");
            foreach (var f in _report.Findings.OrderByDescending(x => x.Severity))
                sb.AppendLine($"<tr class=\"sev-{f.Severity}\"><td>{f.Severity}</td><td>{Html(f.Category)}</td>" +
                    $"<td>{Html(f.Component)}</td><td>{Html(f.Recommendation)}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Pre-upgrade checklist</h2><ul>");
            foreach (var line in _report.Checklist) sb.AppendLine($"<li>{Html(line)}</li>");
            sb.AppendLine("</ul>");

            sb.AppendLine("<h2>Rollback guidance</h2><ul>");
            foreach (var line in _report.RollbackGuidance) sb.AppendLine($"<li>{Html(line)}</li>");
            sb.AppendLine("</ul></body></html>");
            return sb.ToString();
        }

        private static string Html(string s) => (s ?? "")
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        // ----------------------------------------------------------------- Helpers

        private void ClearResults()
        {
            _report = null;
            _lastCollection = null;
            grdFindings.Rows.Clear();
            lstChecklist.Items.Clear();
            lstRollback.Items.Clear();
            txtSummary.Text = "";
            tsbExport.Enabled = false;
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
    }

    /// <summary>Serializable settings POCO round-tripped via BaseToolControl LoadSettings/SaveSettings.</summary>
    public class ImpactSettings
    {
        public string LastSolution { get; set; }
        public string LastPath { get; set; } = "Upgrade";
    }
}
