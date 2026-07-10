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
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.SharingAnalyzer.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;
// Microsoft.Xrm.Sdk also defines a Label; disambiguate to the WinForms control (CS0104).
using Label = System.Windows.Forms.Label;

namespace XrmToolSuite.SharingAnalyzer
{
    public partial class SharingAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;

        // Cap on rows we render / export from potentially huge POA result sets (keeps the UI responsive
        // and limits how much sensitive detail leaves the tool).
        private const int MaxGridRows = 2000;
        private const int MaxIntensityRows = 500;

        private SharingSummary _summary = new SharingSummary();
        private List<Finding> _findings = new List<Finding>();
        private readonly List<string> _selectedTables = new List<string>();
        private List<(string LogicalName, string Display)> _tableOptions;

        // Suppresses the full-scan warning while we restore the persisted toggle state on load.
        private bool _loading;

        // Powers "Report a bug" / help links in XrmToolBox
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public SharingAnalyzerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Sharing Analyzer"));
        }

        #region Lifecycle

        private void SharingAnalyzerControl_Load(object sender, EventArgs e)
        {
            _loading = true;
            _settings = LoadSettings<ToolSettings>();
            if (_settings.LastTables != null) _selectedTables.AddRange(_settings.LastTables);
            tsbFullScan.Checked = _settings.FullScan;
            UpdateTablesButton();
            _loading = false;
            LogInfo("Sharing Analyzer loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.LastTables = _selectedTables.ToList();
            _settings.FullScan = tsbFullScan.Checked;
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
            MetadataCache.Clear(); // object type codes & metadata differ between environments
            _tableOptions = null;
            _summary = new SharingSummary();
            _findings = new List<Finding>();
            ClearResults();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        #endregion

        #region Table picker

        private void tsbTables_Click(object sender, EventArgs e) => ExecuteMethod(PickTables);

        private void PickTables()
        {
            if (_tableOptions != null)
            {
                ShowTablePicker();
                return;
            }
            RunAsync(
                "Loading shareable tables...",
                worker => SharingCollector.ShareableTables(Service),
                options =>
                {
                    _tableOptions = options;
                    ShowTablePicker();
                });
        }

        private void ShowTablePicker()
        {
            using (var dlg = new Form
            {
                Text = "Select tables to scan",
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(420, 480),
                MinimizeBox = false,
                MaximizeBox = false
            })
            {
                var search = new TextBox { Dock = DockStyle.Top };
                var list = new CheckedListBox
                {
                    Dock = DockStyle.Fill,
                    CheckOnClick = true,
                    IntegralHeight = false
                };

                void Populate(string filter)
                {
                    list.BeginUpdate();
                    list.Items.Clear();
                    foreach (var t in _tableOptions)
                    {
                        var label = string.IsNullOrWhiteSpace(t.Display) ? t.LogicalName : $"{t.Display} ({t.LogicalName})";
                        if (!string.IsNullOrEmpty(filter) &&
                            label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                        var idx = list.Items.Add(new TableItem { LogicalName = t.LogicalName, Label = label });
                        if (_selectedTables.Contains(t.LogicalName, StringComparer.OrdinalIgnoreCase))
                            list.SetItemChecked(idx, true);
                    }
                    list.EndUpdate();
                }
                Populate(null);
                search.TextChanged += (s, e) => Populate(search.Text);

                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 80 };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 80 };
                var buttons = new Panel { Dock = DockStyle.Bottom, Height = 34 };
                buttons.Controls.Add(ok);
                buttons.Controls.Add(cancel);

                dlg.Controls.Add(list);
                dlg.Controls.Add(search);
                dlg.Controls.Add(buttons);
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                // Merge: keep previously-selected tables that are filtered out of view, replace the visible set.
                var visible = list.Items.Cast<TableItem>().Select(i => i.LogicalName).ToList();
                var stillChecked = list.CheckedItems.Cast<TableItem>().Select(i => i.LogicalName).ToList();
                _selectedTables.RemoveAll(t => visible.Contains(t, StringComparer.OrdinalIgnoreCase));
                foreach (var t in stillChecked)
                    if (!_selectedTables.Contains(t, StringComparer.OrdinalIgnoreCase))
                        _selectedTables.Add(t);

                UpdateTablesButton();
            }
        }

        private void UpdateTablesButton()
        {
            tsbTables.Text = _selectedTables.Count == 0 ? "Tables…" : $"Tables… ({_selectedTables.Count})";
        }

        private void tsbFullScan_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading || !tsbFullScan.Checked) return;
            var proceed = MessageBox.Show(this,
                "A full-environment scan reads PrincipalObjectAccess for every table. On a large org this can " +
                "return millions of rows and take a long time.\r\n\r\nEnable the full-environment scan?",
                "Sharing Analyzer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (proceed != DialogResult.Yes)
                tsbFullScan.Checked = false;
        }

        #endregion

        #region Scan

        private void tsbScan_Click(object sender, EventArgs e) => ExecuteMethod(ScanSharing);

        private void ScanSharing()
        {
            if (_selectedTables.Count == 0 && !tsbFullScan.Checked)
            {
                Warn("Pick one or more tables (Tables…) or enable the full-environment scan first.");
                return;
            }

            var tables = _selectedTables.ToList();
            var fullScan = tsbFullScan.Checked;

            RunAsync(
                "Scanning record-level sharing...",
                worker =>
                {
                    Action<string> progress = s => worker.ReportProgress(0, s);
                    var summary = new SharingCollector().Collect(Service, tables, fullScan, worker, progress);
                    var findings = new List<Finding>();
                    findings.AddRange(SharingRiskRules.Evaluate(summary, CurrentOptions()));
                    findings.AddRange(summary.CollectionNotes);
                    return (summary, findings);
                },
                result =>
                {
                    _summary = result.summary;
                    _findings = result.findings;
                    BindResults();
                    SetStatusMessage(
                        $"{_summary.TotalShares} share(s) on {_summary.DistinctRecords} record(s) across " +
                        $"{_summary.DistinctPrincipals} principal(s).");
                });
        }

        private SharingRiskOptions CurrentOptions() => new SharingRiskOptions
        {
            MaxPrincipalsPerRecord = _settings.MaxPrincipalsPerRecord > 0 ? _settings.MaxPrincipalsPerRecord : 25,
            MaxInboundPerPrincipal = _settings.MaxInboundPerPrincipal > 0 ? _settings.MaxInboundPerPrincipal : 500
        };

        #endregion

        #region Rendering

        private void ClearResults()
        {
            grdShares.Rows.Clear();
            grdFindings.Rows.Clear();
            grdIntensity.Rows.Clear();
            grdRecommendations.Rows.Clear();
            flpCards.Controls.Clear();
            lblHeader.Text = "Pick tables and click Scan sharing to analyze record-level sharing.";
        }

        private void BindResults()
        {
            BindSharesGrid();
            BindCards();
            BindFindings();
            BindIntensity();
            BindRecommendations();

            var scope = _summary.ScannedTables.Count > 0 ? string.Join(", ", _summary.ScannedTables) : "(none)";
            lblHeader.Text = $"Scope: {scope}  ·  {_summary.TotalShares} share(s)  ·  {_summary.DistinctRecords} record(s)  ·  " +
                             $"{_summary.DistinctPrincipals} principal(s)";
        }

        private void BindSharesGrid()
        {
            var filter = (tstPrincipal.Text ?? string.Empty).Trim();
            var rows = _summary.Shares
                .Where(s => string.IsNullOrEmpty(filter) ||
                            (s.PrincipalName ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(MaxGridRows)
                .ToList();

            grdShares.SuspendLayout();
            grdShares.Rows.Clear();
            foreach (var s in rows)
            {
                var i = grdShares.Rows.Add(
                    s.Table,
                    s.ObjectId.ToString(),
                    s.PrincipalName,
                    s.PrincipalType,
                    s.PrincipalActive ? "Yes" : "No",
                    AccessRights.Summary(s.AccessMask));
                if (!s.PrincipalActive)
                    grdShares.Rows[i].DefaultCellStyle.BackColor = Color.FromArgb(252, 248, 227);
            }
            grdShares.ResumeLayout();
        }

        private void BindCards()
        {
            flpCards.Controls.Clear();

            var risk = _findings.Where(f => f.Severity > Severity.Info).ToList();
            int score = SharingRiskRules.Score(risk);
            var band = SharingRiskRules.Band(risk);

            flpCards.Controls.Add(MakeCard($"{band}", $"sharing risk · {score}/100", BandColor(band)));
            flpCards.Controls.Add(MakeCard(_summary.TotalShares.ToString(), "total shares"));
            flpCards.Controls.Add(MakeCard(_summary.DistinctRecords.ToString(), "shared records"));
            flpCards.Controls.Add(MakeCard(_summary.DistinctPrincipals.ToString(), "principals"));

            // Access-rights mix across all shares.
            foreach (var name in new[] { "Read", "Write", "Delete", "Assign", "Share" })
            {
                int n = _summary.Shares.Count(s => AccessRights.Decode(s.AccessMask).Contains(name));
                flpCards.Controls.Add(MakeCard(n.ToString(), name.ToLowerInvariant() + " grants"));
            }
        }

        private void BindFindings()
        {
            grdFindings.Rows.Clear();
            foreach (var f in _findings.OrderByDescending(x => x.Severity))
            {
                var i = grdFindings.Rows.Add(f.Severity.ToString(), f.Title, f.Description);
                grdFindings.Rows[i].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
            if (grdFindings.Rows.Count == 0)
                grdFindings.Rows.Add("Info", "No findings", "Run a scan to evaluate sharing.");
        }

        private void BindIntensity()
        {
            grdIntensity.Rows.Clear();
            foreach (var c in _summary.Intensity().Take(MaxIntensityRows))
            {
                var i = grdIntensity.Rows.Add(c.Table, c.PrincipalName, c.PrincipalType, c.Shares.ToString());
                grdIntensity.Rows[i].DefaultCellStyle.BackColor = HeatColor(c.Shares);
            }
        }

        private void BindRecommendations()
        {
            grdRecommendations.Rows.Clear();
            foreach (var f in _findings
                         .Where(f => f.Severity >= Severity.Low && !string.IsNullOrWhiteSpace(f.Recommendation))
                         .OrderByDescending(f => f.Severity))
            {
                var i = grdRecommendations.Rows.Add(f.Severity.ToString(), f.Component, f.Recommendation);
                grdRecommendations.Rows[i].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
            if (grdRecommendations.Rows.Count == 0)
                grdRecommendations.Rows.Add("Info", "", "No cleanup recommended — sharing looks healthy.");
        }

        private void tstPrincipal_TextChanged(object sender, EventArgs e)
        {
            if (_summary.TotalShares > 0) BindSharesGrid();
        }

        private static Control MakeCard(string value, string caption, Color? accent = null)
        {
            var panel = new Panel
            {
                Width = 128,
                Height = 74,
                Margin = new Padding(4),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = accent ?? Color.FromArgb(245, 245, 245)
            };
            panel.Controls.Add(new Label
            {
                Text = value,
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            });
            panel.Controls.Add(new Label
            {
                Text = caption,
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter
            });
            return panel;
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
            if (_summary.TotalShares == 0 && _findings.Count == 0)
            {
                Warn("Run a scan before exporting.");
                return;
            }

            string filter = kind == "xlsx" ? "Excel workbook (*.xlsx)|*.xlsx"
                : kind == "pdf" ? "PDF document (*.pdf)|*.pdf"
                : kind == "json" ? "JSON (*.json)|*.json"
                : kind == "csv" ? "CSV (*.csv)|*.csv"
                : "HTML report (*.html)|*.html";
            string defaultName = $"SharingAnalysis_{DateTime.Now:yyyyMMdd_HHmm}.{kind}";

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var path = dlg.FileName;

                RunAsync(
                    "Exporting sharing report...",
                    worker =>
                    {
                        switch (kind)
                        {
                            case "xlsx": ExcelReportExporter.Export(BuildReportModel(), path); break;
                            case "pdf": PdfReportExporter.Export(BuildReportModel(), path); break;
                            case "json": JsonReportExporter.Export(BuildReportModel(), path); break;
                            case "html": WriteHtml(path); break;
                            default: WriteCsv(path); break;
                        }
                        return path;
                    },
                    written =>
                    {
                        if (MessageBox.Show(this, "Report exported. Open it now?", "Sharing Analyzer",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                            System.Diagnostics.Process.Start(written);
                        SetStatusMessage($"Exported to {Path.GetFileName(written)}");
                    });
            }
        }

        /// <summary>
        /// Projects the scan into the shared ReportModel. Deliberately carries findings + aggregate metrics
        /// only (no raw dump of every share), so exports stay auditable without leaking full principal lists.
        /// </summary>
        private ReportModel BuildReportModel()
        {
            var risk = _findings.Where(f => f.Severity > Severity.Info).ToList();
            var model = new ReportModel
            {
                ToolName = "Sharing Analyzer",
                ToolVersion = GetType().Assembly.GetName().Version?.ToString(),
                ReportTitle = "Record-Level Sharing Report",
                Subtitle = "PrincipalObjectAccess analysis",
                ScoreWord = "sharing risk",
                SubjectName = _summary.ScannedTables.Count > 0 ? string.Join(", ", _summary.ScannedTables) : "Sharing",
                LeadIn = "Shares by table and principal, excessive/stale sharing findings, and cleanup recommendations.",
                Score = SharingRiskRules.Score(risk),
                Band = SharingRiskRules.Band(risk)
            };

            foreach (var f in _findings)
                model.Findings.Add(f);

            model.Metrics.Add(new MetricRow("Tables scanned", _summary.ScannedTables.Count.ToString()));
            model.Metrics.Add(new MetricRow("Total shares", _summary.TotalShares.ToString()));
            model.Metrics.Add(new MetricRow("Shared records", _summary.DistinctRecords.ToString()));
            model.Metrics.Add(new MetricRow("Distinct principals", _summary.DistinctPrincipals.ToString()));
            model.Metrics.Add(new MetricRow("Shares with inactive principals",
                _summary.PrincipalStats().Where(p => !p.PrincipalActive).Sum(p => p.InboundShares).ToString()));
            model.Metrics.Add(new MetricRow("High findings", risk.Count(f => f.Severity >= Severity.High).ToString()));
            model.Metrics.Add(new MetricRow("Medium findings", risk.Count(f => f.Severity == Severity.Medium).ToString()));

            model.VerdictHigh = "Revoke or consolidate the flagged excessive/stale shares before they become audit debt.";
            model.VerdictMedium = "Review the flagged shares and plan cleanup for stale or outlier sharing.";
            model.VerdictLow = "No significant record-level sharing risk detected.";
            return model;
        }

        private void WriteCsv(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Severity,Category,Finding,Component,Evidence,Recommendation");
            foreach (var f in _findings.OrderByDescending(x => x.Severity))
                sb.AppendLine(string.Join(",",
                    Csv(f.Severity.ToString()), Csv(f.Category), Csv(f.Title),
                    Csv(f.Component), Csv(f.Description), Csv(f.Recommendation)));

            sb.AppendLine();
            sb.AppendLine("Table,DistinctPrincipals,TotalShares");
            foreach (var kv in _summary.DistinctPrincipalsPerRecordByTable().OrderByDescending(x => x.Value))
            {
                var shares = _summary.Shares.Count(s => string.Equals(s.Table, kv.Key, StringComparison.OrdinalIgnoreCase));
                sb.AppendLine(string.Join(",", Csv(kv.Key), kv.Value, shares));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private void WriteHtml(string path)
        {
            var risk = _findings.Where(f => f.Severity > Severity.Info).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Record-Level Sharing Report</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                          "h1{font-size:20px}h2{font-size:15px;margin-top:22px}" +
                          "table{border-collapse:collapse;margin-top:8px;width:100%}" +
                          "th,td{border:1px solid #ccc;padding:5px 8px;text-align:left;font-size:12px;vertical-align:top}" +
                          "th{background:#f4f4f4}.sev-High,.sev-Critical{color:#a94442;font-weight:bold}" +
                          ".sev-Medium{color:#8a6d3b}.sev-Low{color:#31708f}.sev-Info{color:#3c763d}</style></head><body>");
            sb.AppendLine("<h1>Record-Level Sharing Report</h1>");
            sb.AppendLine($"<p style=\"color:#666\">Generated {DateTime.Now:u} — {SharingRiskRules.Band(risk)} sharing risk " +
                          $"(score {SharingRiskRules.Score(risk)}/100). Read-only; aggregate counts and findings only.</p>");

            sb.AppendLine("<h2>Summary</h2><table><tr><th>Metric</th><th>Value</th></tr>");
            sb.AppendLine($"<tr><td>Tables scanned</td><td>{H(string.Join(", ", _summary.ScannedTables))}</td></tr>");
            sb.AppendLine($"<tr><td>Total shares</td><td>{_summary.TotalShares}</td></tr>");
            sb.AppendLine($"<tr><td>Shared records</td><td>{_summary.DistinctRecords}</td></tr>");
            sb.AppendLine($"<tr><td>Distinct principals</td><td>{_summary.DistinctPrincipals}</td></tr>");
            sb.AppendLine("</table>");

            sb.AppendLine("<h2>Findings</h2>");
            sb.AppendLine("<table><tr><th>Severity</th><th>Finding</th><th>Evidence</th><th>Recommendation</th></tr>");
            foreach (var f in _findings.OrderByDescending(x => x.Severity))
                sb.AppendLine($"<tr><td class=\"sev-{f.Severity}\">{f.Severity}</td><td>{H(f.Title)}</td>" +
                              $"<td>{H(f.Description)}</td><td>{H(f.Recommendation)}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine("</body></html>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Helpers

        private void Warn(string message) =>
            MessageBox.Show(this, message, "Sharing Analyzer", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

        private static Color BandColor(ScoreBand band)
        {
            switch (band)
            {
                case ScoreBand.High: return Color.FromArgb(242, 222, 222);
                case ScoreBand.Medium: return Color.FromArgb(252, 248, 227);
                default: return Color.FromArgb(223, 240, 216);
            }
        }

        private static Color HeatColor(int shares)
        {
            if (shares >= 50) return Color.FromArgb(242, 200, 200);
            if (shares >= 20) return Color.FromArgb(250, 226, 200);
            if (shares >= 5) return Color.FromArgb(252, 248, 227);
            return Color.White;
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

        private sealed class TableItem
        {
            public string LogicalName { get; set; }
            public string Label { get; set; }
            public override string ToString() => Label;
        }

        #endregion
    }

    /// <summary>Persisted UI state (POCO — no controls/services/credentials).</summary>
    public class ToolSettings
    {
        public List<string> LastTables { get; set; } = new List<string>();
        public bool FullScan { get; set; }
        public int MaxPrincipalsPerRecord { get; set; } = 25;
        public int MaxInboundPerPrincipal { get; set; } = 500;
    }
}
