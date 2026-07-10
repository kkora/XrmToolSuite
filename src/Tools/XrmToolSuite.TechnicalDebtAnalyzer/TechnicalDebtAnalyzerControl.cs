using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using IOrganizationService = Microsoft.Xrm.Sdk.IOrganizationService;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.Core.Summarization;
using XrmToolSuite.TechnicalDebtAnalyzer.Analysis;
using XrmToolSuite.TechnicalDebtAnalyzer.Reporting;
using XrmToolSuite.TechnicalDebtAnalyzer.Trends;
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.TechnicalDebtAnalyzer
{
    /// <summary>
    /// Scans a whole Dataverse environment, scores its technical debt (0–100), and produces prioritized
    /// cleanup findings with Excel/PDF/HTML/JSON/Markdown exports and an offline-or-AI executive summary.
    /// Follows the suite patterns: all Dataverse work goes through <see cref="BaseToolControl.RunAsync{T}"/>,
    /// analyzers are UI-free (<see cref="IAnalyzer{T}"/>), and reporting/summarization are shared modules.
    /// </summary>
    public partial class TechnicalDebtAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        // Every analyzer in the suite, in display order. Drives the picker and the run loop.
        private readonly List<IAnalyzer<TechDebtContext>> _allAnalyzers = new List<IAnalyzer<TechDebtContext>>
        {
            new UnusedMetadataAnalyzer(),
            new DuplicateArtifactsAnalyzer(),
            new DeprecatedApiAnalyzer(),
            new OrphanedComponentsAnalyzer(),
            new DeadPluginsAnalyzer(),
            new PerformanceAnalyzer(),
            new NamingViolationsAnalyzer(),
            new SecurityAnalyzer(),
        };

        private TechDebtSettings _settings = new TechDebtSettings();
        private ReportModel _lastModel;

        // Summary (offline default + auditable AI opt-in). Key is session-only, never persisted.
        private readonly ISummaryGenerator _offlineGenerator = new TemplatedSummaryGenerator(TechDebtReport.Scorer);
        private readonly ISummaryGenerator _aiGenerator = new AiSummaryGenerator(TechDebtReport.Scorer);
        private string _sessionApiKey;
        private bool _aiConsentGiven;

        // UI
        private CheckedListBox _lstAnalyzers;
        private DataGridView _grid;
        private TextBox _txtDetail;
        private Label _lblScore, _lblBand, _lblCounts;
        private ToolStripButton _btnAnalyze, _btnAiSummary;
        private ToolStripDropDownButton _btnExport;
        private ToolStripMenuItem _miAiIncludeComponents;

        // RPT4 Trends — per-machine JSON snapshot history + the Trends tab.
        private List<DebtSnapshot> _history = new List<DebtSnapshot>();
        private DataGridView _trendGrid;
        private TrendChart _trendChart;
        private Label _lblTrendDelta;

        public TechnicalDebtAnalyzerControl()
        {
            BuildUi();
            Load += OnLoad;
        }

        #region UI

        private void BuildUi()
        {
            Dock = DockStyle.Fill;

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            _btnAnalyze = new ToolStripButton("▶ Analyze environment") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _btnAnalyze.Click += (s, e) => ExecuteMethod(RunAnalysis);

            _btnExport = new ToolStripDropDownButton("Export") { DisplayStyle = ToolStripItemDisplayStyle.Text, Enabled = false };
            _btnExport.DropDownItems.Add("PDF report (.pdf)", null, (s, e) => Export("pdf"));
            _btnExport.DropDownItems.Add("HTML dashboard (.html)", null, (s, e) => Export("html"));
            _btnExport.DropDownItems.Add("Excel workbook (.xlsx)", null, (s, e) => Export("xlsx"));
            _btnExport.DropDownItems.Add("JSON (.json)", null, (s, e) => Export("json"));
            _btnExport.DropDownItems.Add("Cleanup checklist (.md)", null, (s, e) => Export("md"));
            _btnExport.DropDownItems.Add(new ToolStripSeparator());
            _btnExport.DropDownItems.Add("Trend history (.csv)", null, (s, e) => ExportTrends("csv"));
            _btnExport.DropDownItems.Add("Trend history (.json)", null, (s, e) => ExportTrends("json"));

            _btnAiSummary = new ToolStripButton("Executive summary") { DisplayStyle = ToolStripItemDisplayStyle.Text, Enabled = false };
            _btnAiSummary.Click += (s, e) => ProduceSummary(interactive: true);

            var btnAiOptions = new ToolStripDropDownButton("AI options") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnAiOptions.DropDownItems.Add("AI settings…", null, (s, e) => ShowAiSettingsDialog());
            _miAiIncludeComponents = new ToolStripMenuItem("Include component names in AI payload") { CheckOnClick = true, Checked = true };
            btnAiOptions.DropDownItems.Add(_miAiIncludeComponents);

            toolbar.Items.AddRange(new ToolStripItem[]
            {
                _btnAnalyze, new ToolStripSeparator(),
                _btnExport, new ToolStripSeparator(), _btnAiSummary, btnAiOptions,
                CreateHelpButton("Technical Debt Analyzer")
            });

            // Left: analyzer picker
            _lstAnalyzers = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
            foreach (var a in _allAnalyzers) _lstAnalyzers.Items.Add(a.Name, true);
            var leftPanel = new Panel { Dock = DockStyle.Fill };
            leftPanel.Controls.Add(_lstAnalyzers);
            leftPanel.Controls.Add(new Label { Text = "Analyzers", Dock = DockStyle.Top, Height = 22, Font = new Font(Font, FontStyle.Bold), Padding = new Padding(4, 4, 0, 0) });

            // Summary strip
            var summaryPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(8, 6, 8, 6) };
            _lblBand = new Label { Text = "—", Font = new Font(Font.FontFamily, 15, FontStyle.Bold), AutoSize = true, Location = new Point(8, 6) };
            _lblScore = new Label { Text = "Run an analysis to score technical debt.", AutoSize = true, Location = new Point(8, 34) };
            _lblCounts = new Label { Text = "", AutoSize = true, Location = new Point(220, 8) };
            summaryPanel.Controls.AddRange(new Control[] { _lblBand, _lblScore, _lblCounts });

            // Grid + detail
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.None
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Severity", FillWeight = 12 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", FillWeight = 20 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Finding", HeaderText = "Finding", FillWeight = 38 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Component", HeaderText = "Component", FillWeight = 30 });
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.SelectionChanged += Grid_SelectionChanged;

            _txtDetail = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            var gridDetailSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 340 };
            gridDetailSplit.Panel1.Controls.Add(_grid);
            gridDetailSplit.Panel2.Controls.Add(_txtDetail);

            var rightPanel = new Panel { Dock = DockStyle.Fill };
            rightPanel.Controls.Add(gridDetailSplit);
            rightPanel.Controls.Add(summaryPanel);

            var mainSplit = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 220 };
            mainSplit.Panel1.Controls.Add(leftPanel);
            mainSplit.Panel2.Controls.Add(rightPanel);

            // Dashboard tab (the existing view) + a Trends tab (RPT4).
            var tabDashboard = new TabPage("Dashboard");
            tabDashboard.Controls.Add(mainSplit);
            var tabTrends = new TabPage("Trends");
            tabTrends.Controls.Add(BuildTrendsPanel());
            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(tabDashboard);
            tabs.TabPages.Add(tabTrends);

            Controls.Add(tabs);
            Controls.Add(toolbar);
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Columns[e.ColumnIndex].Name != "Severity" || e.RowIndex < 0) return;
            var sev = e.Value?.ToString();
            switch (sev)
            {
                case "Critical": e.CellStyle.BackColor = Color.FromArgb(164, 38, 44); e.CellStyle.ForeColor = Color.White; break;
                case "High": e.CellStyle.BackColor = Color.FromArgb(209, 52, 56); e.CellStyle.ForeColor = Color.White; break;
                case "Medium": e.CellStyle.BackColor = Color.FromArgb(247, 169, 36); e.CellStyle.ForeColor = Color.Black; break;
                case "Low": e.CellStyle.BackColor = Color.FromArgb(222, 222, 222); break;
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.CurrentRow?.Tag is Finding f)
                _txtDetail.Text =
                    $"[{f.Severity}] {f.Title}\r\nCategory: {f.Category}\r\nComponent: {f.Component}\r\n\r\n" +
                    $"{f.Description}\r\n\r\n→ {f.Recommendation}" +
                    (string.IsNullOrEmpty(f.HelpUrl) ? "" : $"\r\n\r\nDocs: {f.HelpUrl}");
        }

        #endregion

        #region Lifecycle

        private void OnLoad(object sender, EventArgs e)
        {
            _settings = LoadSettings<TechDebtSettings>();
            if (_settings.UncheckedAnalyzers != null)
                for (int i = 0; i < _lstAnalyzers.Items.Count; i++)
                    if (_settings.UncheckedAnalyzers.Contains(_allAnalyzers[i].Name))
                        _lstAnalyzers.SetItemChecked(i, false);
            _miAiIncludeComponents.Checked = _settings.AiIncludeComponents;

            _history = TrendHistoryFile.Load();
            RefreshTrends();
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.UncheckedAnalyzers = _allAnalyzers
                .Where((a, i) => !_lstAnalyzers.GetItemChecked(i)).Select(a => a.Name).ToList();
            _settings.AiIncludeComponents = _miAiIncludeComponents.Checked;
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail,
            string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        #endregion

        #region Analysis

        private void RunAnalysis()
        {
            var selected = _allAnalyzers.Where((a, i) => _lstAnalyzers.GetItemChecked(i)).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Select at least one analyzer.", "Technical Debt Analyzer",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var service = Service;
            var envName = ConnectionDetail?.ConnectionName ?? "Dataverse environment";

            RunAsync(
                "Analyzing technical debt…",
                worker =>
                {
                    var ctx = new TechDebtContext(service, envName);
                    var run = AnalyzerRunner.Run(selected, ctx,
                        msg => worker.ReportProgress(0, msg),
                        () => worker.CancellationPending);
                    return TechDebtReport.Build(run, envName);
                },
                model =>
                {
                    _lastModel = model;
                    BindModel(model);
                    CaptureSnapshot(model);
                    _btnExport.Enabled = true;
                    _btnAiSummary.Enabled = true;
                    SetStatusMessage($"Technical debt score {model.Score}/100 ({model.Band}) — {model.Findings.Count} findings.");
                });
        }

        private void BindModel(ReportModel model)
        {
            _grid.Rows.Clear();
            foreach (var f in model.Findings.OrderByDescending(x => x.Severity).ThenBy(x => x.Category))
            {
                int i = _grid.Rows.Add(f.Severity.ToString(), f.Category, f.Title, f.Component);
                _grid.Rows[i].Tag = f;
            }

            _lblBand.Text = model.BandText();
            _lblBand.ForeColor = model.Band == ScoreBand.High ? Color.FromArgb(209, 52, 56)
                : model.Band == ScoreBand.Medium ? Color.FromArgb(200, 130, 0) : Color.FromArgb(16, 124, 16);
            _lblScore.Text = $"Score {model.Score}/100 · {model.Findings.Count} findings";
            _lblCounts.Text = ScoreCalculator.Explain(model.Findings, model.Score, model.Band, "technical debt");
        }

        #endregion

        #region Trends (RPT4)

        private Control BuildTrendsPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };

            var top = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(8, 6, 8, 0) };
            _lblTrendDelta = new Label { Dock = DockStyle.Left, AutoSize = true, Font = new Font(Font, FontStyle.Bold), Text = "No runs recorded yet." };
            var btnClear = new Button { Text = "Clear history…", Dock = DockStyle.Right, AutoSize = true };
            btnClear.Click += (s, e) => ClearHistory();
            top.Controls.Add(_lblTrendDelta);
            top.Controls.Add(btnClear);

            _trendChart = new TrendChart { Dock = DockStyle.Top, Height = 200 };

            _trendGrid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.None
            };
            _trendGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "When", FillWeight = 34 });
            _trendGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Score", FillWeight = 16 });
            _trendGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Band", FillWeight = 22 });
            _trendGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Findings", FillWeight = 20 });

            // Add fill first, then the top-docked chart, then the top banner (last-added top is outermost).
            panel.Controls.Add(_trendGrid);
            panel.Controls.Add(_trendChart);
            panel.Controls.Add(top);
            return panel;
        }

        private string CurrentEnvironment() =>
            _lastModel?.SourceEnvironment ?? ConnectionDetail?.ConnectionName;

        /// <summary>Record this run as a snapshot (per environment) and persist locally. Never throws into the UI.</summary>
        private void CaptureSnapshot(ReportModel model)
        {
            try
            {
                var snap = new DebtSnapshot
                {
                    TimestampUtc = DateTime.UtcNow,
                    EnvironmentName = model.SourceEnvironment ?? ConnectionDetail?.ConnectionName ?? "Dataverse environment",
                    Score = model.Score,
                    Band = model.Band,
                    TotalFindings = model.Findings.Count,
                    CategoryCounts = model.Findings.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.Count()),
                };
                if (TrendStore.Append(_history, snap))
                    TrendHistoryFile.Save(_history);
                RefreshTrends();
            }
            catch (Exception ex) { LogError("Failed to record trend snapshot: {0}", ex.Message); }
        }

        private void RefreshTrends()
        {
            if (_trendGrid == null) return; // called before the Trends tab is built
            var ordered = TrendStore.ForEnvironment(_history, CurrentEnvironment());

            _trendGrid.Rows.Clear();
            foreach (var s in ordered.Reverse()) // newest first in the grid
                _trendGrid.Rows.Add(s.TimestampUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                    s.Score.ToString(CultureInfo.InvariantCulture), s.Band.ToString(),
                    s.TotalFindings.ToString(CultureInfo.InvariantCulture));

            _trendChart.SetSeries(TrendAnalytics.ScoreSeries(ordered));

            var d = TrendAnalytics.SincePrevious(ordered);
            if (d == null)
            {
                _lblTrendDelta.Text = ordered.Count == 0
                    ? "No runs recorded yet for this environment."
                    : "One run recorded — analyze again to see a trend.";
                _lblTrendDelta.ForeColor = Color.Gray;
            }
            else
            {
                var arrow = d.Direction == TrendDirection.Improving ? "▼ improving"
                          : d.Direction == TrendDirection.Worsening ? "▲ worsening" : "▬ flat";
                _lblTrendDelta.Text = $"Since last run: {(d.ScoreChange > 0 ? "+" : "")}{d.ScoreChange} debt ({arrow})";
                _lblTrendDelta.ForeColor = d.Direction == TrendDirection.Worsening ? Color.FromArgb(209, 52, 56)
                    : d.Direction == TrendDirection.Improving ? Color.FromArgb(16, 124, 16) : Color.Gray;
            }
        }

        private void ClearHistory()
        {
            var env = CurrentEnvironment();
            if (string.IsNullOrEmpty(env)) return;
            if (MessageBox.Show(this,
                    $"Clear all recorded trend snapshots for '{env}'?\n\nThis removes local history only — it does not touch Dataverse.",
                    "Clear trend history", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;
            _history.RemoveAll(s => string.Equals(s.EnvironmentName ?? "", env, StringComparison.OrdinalIgnoreCase));
            TrendHistoryFile.Save(_history);
            RefreshTrends();
        }

        private void ExportTrends(string kind)
        {
            var ordered = TrendStore.ForEnvironment(_history, CurrentEnvironment());
            if (ordered.Count == 0)
            {
                MessageBox.Show(this, "No trend history to export for this environment yet.", "Trends",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var filter = kind == "csv" ? "CSV|*.csv" : "JSON|*.json";
            var defaultName = $"TechnicalDebtTrends_{DateTime.Now:yyyyMMdd_HHmm}.{kind}";
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    if (kind == "csv")
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("TimestampUtc,Environment,Score,Band,TotalFindings");
                        foreach (var s in ordered)
                            sb.AppendLine($"{s.TimestampUtc:o},{CsvCell(s.EnvironmentName)},{s.Score},{s.Band},{s.TotalFindings}");
                        File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                    }
                    else
                    {
                        File.WriteAllText(dlg.FileName, JsonConvert.SerializeObject(ordered, Formatting.Indented));
                    }
                    SetStatusMessage($"Exported {ordered.Count} trend snapshot(s) to {System.IO.Path.GetFileName(dlg.FileName)}.");
                }
                catch (Exception ex) { ShowError(ex, "Export failed"); }
            }
        }

        private static string CsvCell(string s)
        {
            s = s ?? string.Empty;
            return s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0 ? s : "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>A dependency-free GDI score-over-time line chart (0–100 axis).</summary>
        private sealed class TrendChart : Panel
        {
            private IReadOnlyList<KeyValuePair<DateTime, int>> _series = new List<KeyValuePair<DateTime, int>>();

            public TrendChart()
            {
                DoubleBuffered = true;
                BackColor = SystemColors.Window;
            }

            public void SetSeries(IReadOnlyList<KeyValuePair<DateTime, int>> series)
            {
                _series = series ?? new List<KeyValuePair<DateTime, int>>();
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var plot = new Rectangle(40, 12, Math.Max(10, Width - 55), Math.Max(10, Height - 34));

                using (var axis = new Pen(Color.LightGray))
                {
                    g.DrawLine(axis, plot.Left, plot.Top, plot.Left, plot.Bottom);
                    g.DrawLine(axis, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
                }
                using (var grid = new Pen(Color.FromArgb(238, 238, 238)))
                using (var lbl = new SolidBrush(Color.Gray))
                using (var font = new Font("Segoe UI", 7f))
                {
                    foreach (var v in new[] { 0, 50, 100 })
                    {
                        int y = plot.Bottom - (int)(plot.Height * (v / 100.0));
                        g.DrawLine(grid, plot.Left, y, plot.Right, y);
                        g.DrawString(v.ToString(), font, lbl, 14, y - 7);
                    }
                }

                if (_series.Count == 0)
                {
                    using (var br = new SolidBrush(Color.Gray))
                    using (var f = new Font("Segoe UI", 8.5f))
                        g.DrawString("No trend data yet — analyze the environment to record a point.",
                            f, br, plot.Left + 8, plot.Top + 8);
                    return;
                }

                int n = _series.Count;
                float X(int i) => n == 1 ? plot.Left + plot.Width / 2f : plot.Left + plot.Width * i / (float)(n - 1);
                float Y(int score) => plot.Bottom - plot.Height * (Math.Min(100, Math.Max(0, score)) / 100f);

                var pts = new PointF[n];
                for (int i = 0; i < n; i++) pts[i] = new PointF(X(i), Y(_series[i].Value));

                if (n >= 2)
                    using (var line = new Pen(Color.FromArgb(0, 120, 212), 2f))
                        g.DrawLines(line, pts);
                using (var dot = new SolidBrush(Color.FromArgb(0, 120, 212)))
                    foreach (var p in pts) g.FillEllipse(dot, p.X - 3, p.Y - 3, 6, 6);
            }
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_lastModel == null) return;
            string ext = kind == "md" ? "md" : kind;
            string filter;
            switch (kind)
            {
                case "pdf": filter = "PDF report|*.pdf"; break;
                case "html": filter = "HTML report|*.html"; break;
                case "xlsx": filter = "Excel workbook|*.xlsx"; break;
                case "json": filter = "JSON|*.json"; break;
                default: filter = "Markdown|*.md"; break;
            }
            string defaultName = $"TechnicalDebt_{DateTime.Now:yyyyMMdd_HHmm}.{ext}";

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    switch (kind)
                    {
                        case "pdf": PdfReportExporter.Export(_lastModel, dlg.FileName); break;
                        case "html": HtmlDashboardBuilder.Export(_lastModel, dlg.FileName); break;
                        case "xlsx": ExcelReportExporter.Export(_lastModel, dlg.FileName); break;
                        case "json": JsonReportExporter.Export(_lastModel, dlg.FileName); break;
                        default: FixChecklistGenerator.Export(_lastModel, dlg.FileName); break;
                    }
                    if (MessageBox.Show(this, "Report exported. Open it now?", "Technical Debt Analyzer",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        System.Diagnostics.Process.Start(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex); }
            }
        }

        #endregion

        #region Summary (offline default + auditable AI opt-in)

        private void ProduceSummary(bool interactive)
        {
            if (_lastModel == null)
            {
                if (interactive)
                    MessageBox.Show(this, "Run an analysis first.", "Technical Debt Analyzer",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var opts = TryBuildAiOptions(interactive, _lastModel);
            var generator = opts != null ? _aiGenerator : _offlineGenerator;

            RunAsync(
                "Generating executive summary…",
                worker => generator.Generate(_lastModel, opts, msg => worker.ReportProgress(0, msg)),
                summary =>
                {
                    summary.Text = SummaryFormatting.ToPlainText(summary.Text);
                    _lastModel.AiSummary = summary.Text;
                    if (interactive) ShowSummary(summary);
                    SetStatusMessage(summary.FromAi
                        ? "AI summary generated — included in exports."
                        : summary.Error != null
                            ? $"Offline summary (AI unavailable: {summary.Error})"
                            : "Offline summary generated — included in exports.");
                });
        }

        private SummaryOptions TryBuildAiOptions(bool interactive, ReportModel model)
        {
            var provider = AiProviderCatalog.Parse(_settings.AiProvider);
            var key = ResolveKey(provider);
            if (string.IsNullOrWhiteSpace(key))
            {
                if (!interactive) return null;
                ShowAiSettingsDialog();
                provider = AiProviderCatalog.Parse(_settings.AiProvider);
                key = ResolveKey(provider);
                if (string.IsNullOrWhiteSpace(key)) return null;
            }

            bool includeComponents = _miAiIncludeComponents.Checked;
            string modelId = string.IsNullOrWhiteSpace(_settings.AiModelId)
                ? AiProviderCatalog.Get(provider).Mid : _settings.AiModelId;

            if (!_aiConsentGiven)
            {
                if (!interactive) return null;
                var preview = JsonConvert.SerializeObject(
                    SummaryPayloadBuilder.Build(model, includeComponents), Formatting.Indented,
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                if (!ShowConsentDialog(preview, AiProviderCatalog.Get(provider), modelId)) return null;
                _aiConsentGiven = true;
            }

            return new SummaryOptions
            {
                Provider = provider,
                ApiKey = key,
                ModelId = modelId,
                IncludeComponents = includeComponents,
                SystemPrompt = TechDebtReport.AiSystemPrompt
            };
        }

        private string ResolveKey(AiProvider provider)
        {
            if (!string.IsNullOrWhiteSpace(_sessionApiKey)) return _sessionApiKey;
            if (provider == AiProvider.Anthropic) return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            return null;
        }

        private void ShowAiSettingsDialog()
        {
            var providers = AiProviderCatalog.All;
            using (var f = new Form
            {
                Text = "AI settings", FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ClientSize = new Size(430, 250)
            })
            {
                var cmb = new ComboBox { Location = new Point(150, 16), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var p in providers) cmb.Items.Add(p.DisplayName);
                cmb.SelectedIndex = Math.Max(0, Array.FindIndex(providers, x => x.Provider == AiProviderCatalog.Parse(_settings.AiProvider)));

                var txtModel = new TextBox { Location = new Point(150, 52), Width = 250, Text = _settings.AiModelId };
                var txtKey = new TextBox { Location = new Point(150, 88), Width = 250, UseSystemPasswordChar = true, Text = _sessionApiKey };
                var chkAuto = new CheckBox { Text = "Auto-generate after each analysis", Location = new Point(150, 120), AutoSize = true, Checked = _settings.AiSummaryAutoRun };
                var lblHint = new Label { Location = new Point(150, 146), Width = 260, Height = 40, ForeColor = Color.Gray, Font = new Font(Font.FontFamily, 7.5f), Text = "The API key is used this session only and is never saved. Only anonymized finding metadata is sent." };

                cmb.SelectedIndexChanged += (s, e) => { if (string.IsNullOrWhiteSpace(txtModel.Text)) txtModel.Text = providers[cmb.SelectedIndex].Mid; };

                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(244, 205), Width = 75 };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(325, 205), Width = 75 };

                f.Controls.AddRange(new Control[]
                {
                    new Label { Text = "Provider", Location = new Point(16, 19), AutoSize = true }, cmb,
                    new Label { Text = "Model id", Location = new Point(16, 55), AutoSize = true }, txtModel,
                    new Label { Text = "API key", Location = new Point(16, 91), AutoSize = true }, txtKey,
                    chkAuto, lblHint, ok, cancel
                });
                f.AcceptButton = ok; f.CancelButton = cancel;

                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    _settings.AiProvider = providers[cmb.SelectedIndex].Provider.ToString();
                    _settings.AiModelId = txtModel.Text?.Trim();
                    _settings.AiSummaryAutoRun = chkAuto.Checked;
                    _sessionApiKey = txtKey.Text?.Trim();
                }
            }
        }

        private bool ShowConsentDialog(string payloadJson, AiProviderCatalog.Info provider, string model)
        {
            using (var f = new Form
            {
                Text = "Send to AI service?", StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(560, 420), MinimizeBox = false, MaximizeBox = false
            })
            {
                var lbl = new Label
                {
                    Dock = DockStyle.Top, Height = 54, Padding = new Padding(8),
                    Text = $"This will send the following anonymized payload to {provider.DisplayName} ({provider.Host}), model '{model}'.\nNo record data, credentials, or environment names are included. Continue?"
                };
                var box = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Text = payloadJson, Font = new Font("Consolas", 8.5f) };
                var bar = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                var send = new Button { Text = "Send", DialogResult = DialogResult.OK, Location = new Point(384, 8), Width = 75 };
                var cancel = new Button { Text = "Cancel (use offline)", DialogResult = DialogResult.Cancel, Location = new Point(465, 8), Width = 85 };
                bar.Controls.AddRange(new Control[] { send, cancel });
                f.Controls.Add(box); f.Controls.Add(lbl); f.Controls.Add(bar);
                f.AcceptButton = send; f.CancelButton = cancel;
                return f.ShowDialog(this) == DialogResult.OK;
            }
        }

        private void ShowSummary(SummaryResult s)
        {
            using (var f = new Form
            {
                Text = s.FromAi ? "AI executive summary" : "Executive summary (offline)",
                StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(560, 420)
            })
            {
                var box = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text = s.Text };
                var bar = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                var copy = new Button { Text = "Copy", Location = new Point(400, 8), Width = 70 };
                copy.Click += (o, e) => { if (!string.IsNullOrEmpty(s.Text)) Clipboard.SetText(s.Text); };
                var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Location = new Point(476, 8), Width = 74 };
                bar.Controls.AddRange(new Control[] { copy, close });
                f.Controls.Add(box); f.Controls.Add(bar);
                f.ShowDialog(this);
            }
        }

        #endregion
    }

    /// <summary>Persisted settings (plain POCO — never stores the API key or connection details).</summary>
    public class TechDebtSettings
    {
        public List<string> UncheckedAnalyzers { get; set; }
        public bool AiSummaryAutoRun { get; set; }
        public bool AiIncludeComponents { get; set; } = true;
        public string AiProvider { get; set; } = "Anthropic";
        public string AiModelId { get; set; }
    }
}
