using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
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
using XrmToolSuite.AuditComplianceChecker.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.AuditComplianceChecker
{
    public partial class AuditComplianceCheckerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;
        private string _connectionName;

        // Latest results (null until the relevant action runs).
        private AuditCoverage _coverage;
        private AuditActivitySummary _activity;
        private AuditComplianceReport _report;

        // Powers "Report a bug" / help links in XrmToolBox
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        private static readonly string[] ActivityViews = { "By table", "By user", "By date" };

        public AuditComplianceCheckerControl()
        {
            InitializeComponent();
            InitGrids();
            cboActivityView.Items.AddRange(ActivityViews);
            cboActivityView.SelectedIndex = 0;
            dtpFrom.Value = DateTime.Today.AddDays(-30);
            dtpTo.Value = DateTime.Today;
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Audit Compliance Checker"));
        }

        #region Grid setup

        private void InitGrids()
        {
            AddCols(grdCoverage, "Table", "Display name", "Managed", "Sensitive", "Auditing", "Sensitive cols (audited/total)");
            AddCols(grdActivity, "Table", "Records");
            AddCols(grdStorage, "Date", "Records", "Cumulative", "Est. MB (cumulative)");
            AddCols(grdCategories, "Category", "Score (0-100)", "Weight");
            AddCols(grdFindings, "Severity", "Finding", "Component", "Evidence", "Recommendation");
            grdCoverage.Columns[0].FillWeight = 130;
            grdCoverage.Columns[1].FillWeight = 130;
            grdFindings.Columns[3].FillWeight = 200;
            grdFindings.Columns[4].FillWeight = 180;
        }

        private static void AddCols(DataGridView g, params string[] headers)
        {
            foreach (var h in headers)
                g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = h, Name = h });
        }

        #endregion

        #region Lifecycle

        private void AuditComplianceCheckerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();
            if (_settings.LastRangeDays > 0)
                dtpFrom.Value = DateTime.Today.AddDays(-_settings.LastRangeDays);
            if (!string.IsNullOrEmpty(_settings.TableScope))
                tstScope.Text = _settings.TableScope;
            LogInfo("Audit Compliance Checker loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.LastRangeDays = Math.Max(1, (int)Math.Round((dtpTo.Value.Date - dtpFrom.Value.Date).TotalDays));
            _settings.TableScope = tstScope.Text;
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata & audit settings differ between environments
            _connectionName = detail?.ConnectionName;
            _coverage = null;
            _activity = null;
            _report = null;
            grdCoverage.Rows.Clear();
            grdActivity.Rows.Clear();
            grdStorage.Rows.Clear();
            grdCategories.Rows.Clear();
            grdFindings.Rows.Clear();
            lblOrgAudit.Text = "Click \"Check audit settings\" to read org/table/column audit configuration.";
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        #endregion

        #region Check audit settings

        private void tsbCheckSettings_Click(object sender, EventArgs e) => ExecuteMethod(CheckSettings);

        private void CheckSettings()
        {
            RunAsync(
                "Reading audit settings...",
                worker =>
                {
                    Action<string> progress = s => worker.ReportProgress(0, s);
                    var coverage = new AuditCollector().CollectCoverage(Service, worker, progress);
                    return coverage;
                },
                coverage =>
                {
                    _coverage = coverage;
                    BindCoverage();
                    Reevaluate();
                    tabControl.SelectedTab = tabCoverage;
                    SetStatusMessage($"Read audit settings for {coverage.Tables.Count} table(s); " +
                                     $"org auditing {(coverage.OrgAuditEnabled ? "ON" : "OFF")}.");
                });
        }

        private void BindCoverage()
        {
            lblOrgAudit.Text = _coverage.OrgAuditEnabled
                ? "Organization auditing: ON. Table/column settings below take effect."
                : "Organization auditing: OFF — no table/column audit setting has any effect until this is enabled.";
            lblOrgAudit.ForeColor = _coverage.OrgAuditEnabled ? Color.FromArgb(60, 118, 61) : Color.FromArgb(169, 68, 66);

            grdCoverage.Rows.Clear();
            // Show the rows that matter: sensitive tables and any audited table. Sensitive first.
            var view = _coverage.Tables
                .Where(t => t.IsSensitive || t.IsAuditEnabled)
                .OrderByDescending(t => t.IsSensitive)
                .ThenBy(t => t.LogicalName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var t in view)
            {
                int sensCols = t.Columns.Count(c => c.IsSensitive);
                int auditedSensCols = t.Columns.Count(c => c.IsSensitive && c.IsAuditEnabled);
                var i = grdCoverage.Rows.Add(
                    t.LogicalName,
                    t.DisplayName,
                    t.IsManaged ? "Managed" : "Custom",
                    t.IsSensitive ? "Yes" : "",
                    t.IsAuditEnabled ? "On" : "Off",
                    sensCols > 0 ? $"{auditedSensCols}/{sensCols}" : "");
                if (t.IsSensitive && !t.IsAuditEnabled)
                    grdCoverage.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(242, 222, 222);
                else if (t.IsSensitive && sensCols > auditedSensCols)
                    grdCoverage.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(252, 248, 227);
            }
            if (grdCoverage.Rows.Count == 0)
                grdCoverage.Rows.Add("(no sensitive or audited tables found)", "", "", "", "", "");
        }

        #endregion

        #region Analyze activity

        private void tsbAnalyzeActivity_Click(object sender, EventArgs e) => ExecuteMethod(AnalyzeActivity);

        private void AnalyzeActivity()
        {
            var fromUtc = dtpFrom.Value.Date.ToUniversalTime();
            var toUtc = dtpTo.Value.Date.AddDays(1).AddSeconds(-1).ToUniversalTime();
            if (toUtc < fromUtc)
            {
                Warn("The activity 'to' date is before the 'from' date. Adjust the range and try again.");
                return;
            }

            var scopeTables = (tstScope.Text ?? string.Empty)
                .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLowerInvariant())
                .Where(s => s.Length > 0)
                .Distinct()
                .ToList();

            RunAsync(
                "Analyzing audit activity...",
                worker =>
                {
                    Action<string> progress = s => worker.ReportProgress(0, s);
                    var collector = new AuditCollector
                    {
                        BusinessHourStart = _settings.BusinessHourStart,
                        BusinessHourEnd = _settings.BusinessHourEnd
                    };
                    return collector.CollectActivity(Service, fromUtc, toUtc, scopeTables, worker, progress);
                },
                activity =>
                {
                    _activity = activity;
                    BindActivity();
                    BindStorage();
                    Reevaluate();
                    tabControl.SelectedTab = tabActivity;
                    SetStatusMessage($"Analyzed {activity.TotalRecords} audit record(s) " +
                                     $"({activity.DeleteCount} delete(s), {activity.SecurityChangeCount} security change(s), " +
                                     $"{activity.AfterHoursCount} after-hours).");
                });
        }

        private void BindActivity()
        {
            lblActivityHighlights.Text =
                $"{_activity.TotalRecords} record(s) from {_activity.FromUtc.ToLocalTime():d} to {_activity.ToUtc.ToLocalTime():d}  ·  " +
                $"Deletes: {_activity.DeleteCount}  ·  Security changes: {_activity.SecurityChangeCount}  ·  " +
                $"After-hours: {_activity.AfterHoursCount}";
            RenderActivityView();
        }

        private void cboActivityView_SelectedIndexChanged(object sender, EventArgs e) => RenderActivityView();

        private void RenderActivityView()
        {
            grdActivity.Rows.Clear();
            if (_activity == null) return;

            Dictionary<string, int> data;
            string keyHeader;
            switch (cboActivityView.SelectedIndex)
            {
                case 1: data = _activity.ByUser; keyHeader = "User"; break;
                case 2: data = _activity.ByDate; keyHeader = "Date"; break;
                default: data = _activity.ByTable; keyHeader = "Table"; break;
            }
            grdActivity.Columns[0].HeaderText = keyHeader;

            IEnumerable<KeyValuePair<string, int>> ordered = cboActivityView.SelectedIndex == 2
                ? data.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                : data.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in ordered)
                grdActivity.Rows.Add(kv.Key, kv.Value.ToString());
            if (grdActivity.Rows.Count == 0)
                grdActivity.Rows.Add("(no records in range)", "0");
        }

        private void BindStorage()
        {
            grdStorage.Rows.Clear();
            int cumulative = 0;
            foreach (var kv in _activity.ByDate.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                cumulative += kv.Value;
                double mb = Math.Round(cumulative * AuditComplianceRules.EstimatedKbPerAuditRecord / 1024.0, 3);
                grdStorage.Rows.Add(kv.Key, kv.Value.ToString(), cumulative.ToString(), mb.ToString("0.###"));
            }
            if (grdStorage.Rows.Count == 0)
                grdStorage.Rows.Add("(no records in range)", "0", "0", "0");
        }

        #endregion

        #region Score / findings

        private void Reevaluate()
        {
            if (_coverage == null) return; // score needs the coverage read
            var opts = new AuditComplianceOptions { HighDeleteVolumeThreshold = _settings.HighDeleteVolumeThreshold };
            _report = AuditComplianceRules.Evaluate(_coverage, _activity, opts);
            BindDashboard();
            BindFindings();
        }

        private void BindDashboard()
        {
            lblScoreValue.Text = $"{_report.Score}";
            lblBand.Text = _report.Band == ScoreBand.High ? "HIGH — most compliant"
                : _report.Band == ScoreBand.Medium ? "MEDIUM — partial coverage"
                : "LOW — significant gaps";
            lblBand.ForeColor = BandColor(_report.Band);
            lblScoreValue.ForeColor = BandColor(_report.Band);
            lblScoreLead.Text = "Compliance readiness score (0–100). HIGHER = MORE compliant. " +
                "Weighted blend of org config (25%), sensitive-table coverage (30%), sensitive-column coverage (25%), " +
                "and activity health (20%). See the categories below and Recommendations for the drivers.";

            grdCategories.Rows.Clear();
            foreach (var m in _report.Metrics)
                grdCategories.Rows.Add(m.Label, m.Value, m.Hint);
        }

        private void BindFindings()
        {
            grdFindings.Rows.Clear();
            foreach (var f in _report.Findings.OrderByDescending(x => x.Severity))
            {
                var i = grdFindings.Rows.Add(f.Severity.ToString(), f.Title, f.Component, f.Description, f.Recommendation);
                grdFindings.Rows[i].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
        }

        #endregion

        #region Export

        private void miExportExcel_Click(object sender, EventArgs e) => Export("xlsx");
        private void miExportPdf_Click(object sender, EventArgs e) => Export("pdf");
        private void miExportJson_Click(object sender, EventArgs e) => Export("json");
        private void miExportHtml_Click(object sender, EventArgs e) => Export("html");
        private void miExportCsv_Click(object sender, EventArgs e) => Export("csv");

        private void Export(string kind)
        {
            if (_report == null)
            {
                Warn("Run \"Check audit settings\" (and optionally \"Analyze activity\") before exporting.");
                return;
            }

            string filter = kind == "xlsx" ? "Excel workbook (*.xlsx)|*.xlsx"
                : kind == "pdf" ? "PDF document (*.pdf)|*.pdf"
                : kind == "json" ? "JSON (*.json)|*.json"
                : kind == "csv" ? "CSV (*.csv)|*.csv"
                : "HTML report (*.html)|*.html";
            string defaultName = $"AuditCompliance_{DateTime.Now:yyyyMMdd_HHmm}.{kind}";

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var path = dlg.FileName;
                var model = BuildReportModel();

                RunAsync(
                    "Exporting audit compliance report...",
                    worker =>
                    {
                        switch (kind)
                        {
                            case "xlsx": ExcelReportExporter.Export(model, path); break;
                            case "pdf": PdfReportExporter.Export(model, path); break;
                            case "json": JsonReportExporter.Export(model, path); break;
                            case "html": WriteHtml(path); break;
                            default: WriteCsv(path); break;
                        }
                        return path;
                    },
                    written =>
                    {
                        if (MessageBox.Show(this, "Report exported. Open it now?", "Audit Compliance Checker",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                            System.Diagnostics.Process.Start(written);
                        SetStatusMessage($"Exported to {Path.GetFileName(written)}");
                    });
            }
        }

        /// <summary>Projects the compliance report into the shared ReportModel (compliance score words).</summary>
        private ReportModel BuildReportModel()
        {
            var model = new ReportModel
            {
                ToolName = "Audit Compliance Checker",
                ToolVersion = GetType().Assembly.GetName().Version?.ToString(),
                ReportTitle = "Audit Compliance Report",
                Subtitle = "Org/table/column audit coverage, activity, and readiness score",
                ScoreWord = "compliance",
                SubjectName = _connectionName ?? "Dataverse environment",
                SourceEnvironment = _connectionName,
                Score = _report.Score,
                Band = _report.Band,
                LeadIn = "Audit configuration coverage and activity. Score is compliance readiness — higher is more compliant. " +
                         "Read-only; no changed/sample field values are collected or shown.",
                VerdictHigh = "Audit coverage is strong; maintain it and review the informational notes.",
                VerdictMedium = "Close the flagged sensitive-table/column gaps and review the activity findings.",
                VerdictLow = "Significant audit gaps — enable organization auditing and audit the sensitive tables/columns below."
            };

            foreach (var m in _report.Metrics) model.Metrics.Add(m);
            foreach (var f in _report.Findings) model.Findings.Add(f);
            return model;
        }

        private void WriteCsv(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Audit Compliance Report — generated {DateTime.Now:u} — score {_report.Score}/100 ({_report.Band})");
            sb.AppendLine("Section,Key,Value,Value2,Value3");
            foreach (var m in _report.Metrics)
                sb.AppendLine(string.Join(",", "Metric", Csv(m.Label), Csv(m.Value), Csv(m.Hint), ""));
            foreach (var t in _coverage.Tables.Where(x => x.IsSensitive || x.IsAuditEnabled)
                         .OrderByDescending(x => x.IsSensitive).ThenBy(x => x.LogicalName))
                sb.AppendLine(string.Join(",", "Coverage", Csv(t.LogicalName),
                    t.IsSensitive ? "Sensitive" : "", t.IsAuditEnabled ? "Audited" : "Not audited",
                    t.IsManaged ? "Managed" : "Custom"));
            if (_activity != null)
                foreach (var kv in _activity.ByTable.OrderByDescending(x => x.Value))
                    sb.AppendLine(string.Join(",", "Activity(byTable)", Csv(kv.Key), kv.Value, "", ""));
            foreach (var f in _report.Findings.OrderByDescending(x => x.Severity))
                sb.AppendLine(string.Join(",", "Finding", Csv(f.Severity.ToString()), Csv(f.Title),
                    Csv(f.Component), Csv(f.Recommendation)));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private void WriteHtml(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Audit Compliance Report</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                          "h1{font-size:20px}h2{font-size:15px;margin-top:22px}" +
                          "table{border-collapse:collapse;margin-top:8px;width:100%}" +
                          "th,td{border:1px solid #ccc;padding:5px 8px;text-align:left;font-size:12px;vertical-align:top}" +
                          "th{background:#f4f4f4}.big{font-size:34px;font-weight:bold}" +
                          ".sev-High,.sev-Critical{color:#a94442;font-weight:bold}" +
                          ".sev-Medium{color:#8a6d3b}.sev-Low{color:#31708f}.sev-Info{color:#3c763d}</style></head><body>");
            sb.AppendLine("<h1>Audit Compliance Report</h1>");
            sb.AppendLine($"<p class=\"big\">{_report.Score}/100 — {H(_report.Band.ToString())} compliance</p>");
            sb.AppendLine($"<p style=\"color:#666\">Generated {DateTime.Now:u}. Read-only; no changed/sample field values are collected. " +
                          "Storage figures are estimates from record volume, not billed storage.</p>");

            sb.AppendLine("<h2>Metrics</h2><table><tr><th>Category / metric</th><th>Value</th><th>Note</th></tr>");
            foreach (var m in _report.Metrics)
                sb.AppendLine($"<tr><td>{H(m.Label)}</td><td>{H(m.Value)}</td><td>{H(m.Hint)}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Findings & recommendations</h2>");
            sb.AppendLine("<table><tr><th>Severity</th><th>Finding</th><th>Component</th><th>Evidence</th><th>Recommendation</th></tr>");
            foreach (var f in _report.Findings.OrderByDescending(x => x.Severity))
                sb.AppendLine($"<tr><td class=\"sev-{f.Severity}\">{f.Severity}</td><td>{H(f.Title)}</td>" +
                              $"<td>{H(f.Component)}</td><td>{H(f.Description)}</td><td>{H(f.Recommendation)}</td></tr>");
            sb.AppendLine("</table>");

            if (_activity != null)
            {
                sb.AppendLine("<h2>Activity by table</h2><table><tr><th>Table</th><th>Records</th></tr>");
                foreach (var kv in _activity.ByTable.OrderByDescending(x => x.Value))
                    sb.AppendLine($"<tr><td>{H(kv.Key)}</td><td>{kv.Value}</td></tr>");
                sb.AppendLine("</table>");
            }

            sb.AppendLine("</body></html>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Helpers

        private void Warn(string message) =>
            MessageBox.Show(this, message, "Audit Compliance Checker", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static Color BandColor(ScoreBand band) =>
            band == ScoreBand.High ? Color.FromArgb(60, 118, 61)
            : band == ScoreBand.Medium ? Color.FromArgb(138, 109, 59)
            : Color.FromArgb(169, 68, 66);

        private static Color SeverityColor(Severity s)
        {
            switch (s)
            {
                case Severity.Critical:
                case Severity.High: return Color.FromArgb(242, 222, 222);
                case Severity.Medium: return Color.FromArgb(252, 248, 227);
                case Severity.Low: return Color.FromArgb(217, 237, 247);
                default: return Color.FromArgb(223, 240, 216);
            }
        }

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            bool needsQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            var v = s.Replace("\"", "\"\"");
            return needsQuote ? $"\"{v}\"" : v;
        }

        private static string H(string s) => string.IsNullOrEmpty(s)
            ? string.Empty
            : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        #endregion
    }

    /// <summary>Persisted UI state (POCO — no controls/services/credentials).</summary>
    public class ToolSettings
    {
        /// <summary>Length of the activity window in days (re-applied on load relative to today).</summary>
        public int LastRangeDays { get; set; } = 30;

        /// <summary>Optional comma-separated table logical names scoping activity queries.</summary>
        public string TableScope { get; set; }

        /// <summary>Business-day start hour (local); earlier = after-hours.</summary>
        public int BusinessHourStart { get; set; } = 7;

        /// <summary>Business-day end hour (local); at/after = after-hours.</summary>
        public int BusinessHourEnd { get; set; } = 19;

        /// <summary>Delete volume threshold for the "high delete volume" activity rule.</summary>
        public int HighDeleteVolumeThreshold { get; set; } = 100;
    }
}
