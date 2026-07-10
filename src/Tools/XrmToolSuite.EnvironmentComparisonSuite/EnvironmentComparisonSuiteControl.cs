using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.EnvironmentComparisonSuite.Analysis;
using XrmToolSuite.EnvironmentComparisonSuite.Reporting;
// Both System.Windows.Forms and Microsoft.Xrm.Sdk define a Label type; this control only uses the
// WinForms one, so alias it to disambiguate (CS0104).
using Label = System.Windows.Forms.Label;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.EnvironmentComparisonSuite
{
    public partial class EnvironmentComparisonSuiteControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private const string TargetActionName = "TargetOrganization";
        private const string AllFilter = "(all)";

        private IOrganizationService _targetService;
        private string _targetName;
        private ComparisonReport _report;
        private EnvironmentComparisonSettings _settings = new EnvironmentComparisonSettings();

        // UI
        private ToolStrip _toolbar;
        private ToolStripButton _btnConnectTarget, _btnCompare;
        private ToolStripLabel _lblTarget;
        private ToolStripDropDownButton _btnExport;
        private CheckedListBox _lstCategories;
        private ComboBox _cboCategoryFilter, _cboClassFilter, _cboSeverityFilter;
        private DataGridView _grid;
        private DataGridView _detailGrid;
        private Label _lblScore, _lblCounts, _lblDetail, _lblRecommendation;
        private Panel _summaryPanel;

        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public EnvironmentComparisonSuiteControl()
        {
            BuildUi();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _settings = LoadSettings<EnvironmentComparisonSettings>();
            var disabled = new HashSet<string>(_settings.DisabledCategories ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < _lstCategories.Items.Count; i++)
                _lstCategories.SetItemChecked(i, !disabled.Contains(_lstCategories.Items[i].ToString()));
            LogInfo("Environment Comparison Suite loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.DisabledCategories = _lstCategories.Items.Cast<string>()
                .Where(n => !_lstCategories.CheckedItems.Contains(n))
                .ToList();
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        #region UI construction

        private void BuildUi()
        {
            SuspendLayout();
            Size = new Size(1200, 760);

            _toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(20, 20) };

            _btnConnectTarget = new ToolStripButton("Connect target env…") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _btnConnectTarget.Click += (s, e) => AddAdditionalOrganization();

            _lblTarget = new ToolStripLabel("Target: (none)") { ForeColor = Color.DimGray };

            _btnCompare = new ToolStripButton("▶ Compare") { DisplayStyle = ToolStripItemDisplayStyle.Text, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _btnCompare.Click += (s, e) => ExecuteMethod(RunComparison);

            _btnExport = new ToolStripDropDownButton("Export") { Enabled = false };
            _btnExport.DropDownItems.Add("Excel workbook (.xlsx)", null, (s, e) => Export("xlsx"));
            _btnExport.DropDownItems.Add("PDF report", null, (s, e) => Export("pdf"));
            _btnExport.DropDownItems.Add("JSON (CI/CD)", null, (s, e) => Export("json"));
            _btnExport.DropDownItems.Add("HTML report", null, (s, e) => Export("html"));

            _toolbar.Items.AddRange(new ToolStripItem[]
            {
                _btnConnectTarget, _lblTarget,
                new ToolStripSeparator(), _btnCompare, _btnExport,
                CreateHelpButton("Environment Comparison Suite")
            });

            // Summary strip (score + severity/class counts)
            _summaryPanel = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(27, 27, 47) };
            _lblScore = new Label
            {
                Text = "Connect a target environment and run a comparison.",
                Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White,
                AutoSize = true, Location = new Point(16, 12)
            };
            _lblCounts = new Label
            {
                Text = "", Font = new Font("Segoe UI", 9), ForeColor = Color.Silver, AutoSize = true, Location = new Point(16, 38)
            };
            _summaryPanel.Controls.AddRange(new Control[] { _lblScore, _lblCounts });

            // Left: category selector
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 240, Padding = new Padding(8) };
            var lblCategories = new Label { Text = "Categories to compare", Dock = DockStyle.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), Height = 22 };
            _lstCategories = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle };
            foreach (var c in ComparisonCategories.All) _lstCategories.Items.Add(c, true);
            leftPanel.Controls.Add(_lstCategories);
            leftPanel.Controls.Add(lblCategories);

            // Filter bar
            var filterBar = new Panel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(6, 4, 6, 4) };
            _cboCategoryFilter = NewFilterCombo();
            _cboClassFilter = NewFilterCombo();
            _cboSeverityFilter = NewFilterCombo();
            filterBar.Controls.Add(new Label { Text = "Filter:", AutoSize = true, Location = new Point(6, 9) });
            PlaceFilter(filterBar, "Category", _cboCategoryFilter, 52);
            PlaceFilter(filterBar, "Class", _cboClassFilter, 300);
            PlaceFilter(filterBar, "Severity", _cboSeverityFilter, 520);
            _cboCategoryFilter.SelectedIndexChanged += (s, e) => RebindGrid();
            _cboClassFilter.SelectedIndexChanged += (s, e) => RebindGrid();
            _cboSeverityFilter.SelectedIndexChanged += (s, e) => RebindGrid();

            // Difference grid
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None
            };
            _grid.Columns.Add(NewCol("Severity", 70));
            _grid.Columns.Add(NewCol("Category", 120));
            _grid.Columns.Add(NewCol("Classification", 110));
            _grid.Columns.Add(NewCol("Component", 240));
            _grid.Columns.Add(NewCol("Detail", 360));
            _grid.SelectionChanged += Grid_SelectionChanged;
            _grid.CellFormatting += Grid_CellFormatting;

            // Side-by-side detail viewer (property | source | target) + recommendation
            var detailPanel = new Panel { Dock = DockStyle.Bottom, Height = 190 };
            _lblDetail = new Label { Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 9, FontStyle.Bold), Text = "Select a difference to see source vs target", Padding = new Padding(4, 4, 0, 0) };
            _detailGrid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BackgroundColor = Color.WhiteSmoke, BorderStyle = BorderStyle.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _detailGrid.Columns.Add(NewCol("Property", 120));
            _detailGrid.Columns.Add(NewCol("Source", 200));
            _detailGrid.Columns.Add(NewCol("Target", 200));
            _lblRecommendation = new Label { Dock = DockStyle.Bottom, Height = 44, ForeColor = Color.FromArgb(0, 90, 158), Padding = new Padding(4), Text = "" };
            detailPanel.Controls.Add(_detailGrid);
            detailPanel.Controls.Add(_lblRecommendation);
            detailPanel.Controls.Add(_lblDetail);

            var detailSplitter = new Splitter { Dock = DockStyle.Bottom, Height = 5, BackColor = SystemColors.ControlLight, MinSize = 100, MinExtra = 150 };

            var centerPanel = new Panel { Dock = DockStyle.Fill };
            centerPanel.Controls.Add(_grid);
            centerPanel.Controls.Add(filterBar);
            centerPanel.Controls.Add(detailSplitter);
            centerPanel.Controls.Add(detailPanel);

            var leftSplitter = new Splitter { Dock = DockStyle.Left, Width = 5, BackColor = SystemColors.ControlLight, MinSize = 150, MinExtra = 300 };

            Controls.Add(centerPanel);
            Controls.Add(leftSplitter);
            Controls.Add(leftPanel);
            Controls.Add(_summaryPanel);
            Controls.Add(_toolbar);
            ResumeLayout();
        }

        private static ComboBox NewFilterCombo() =>
            new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };

        private static void PlaceFilter(Panel bar, string caption, ComboBox combo, int x)
        {
            bar.Controls.Add(new Label { Text = caption, AutoSize = true, Location = new Point(x, 9) });
            combo.Location = new Point(x + 60, 5);
            bar.Controls.Add(combo);
        }

        private static DataGridViewTextBoxColumn NewCol(string name, int fillWeight) =>
            new DataGridViewTextBoxColumn { HeaderText = name, Name = name, FillWeight = fillWeight, SortMode = DataGridViewColumnSortMode.Automatic };

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != 0 || e.Value == null) return;
            switch (e.Value.ToString())
            {
                case "Critical": e.CellStyle.BackColor = Color.FromArgb(164, 38, 44); e.CellStyle.ForeColor = Color.White; break;
                case "High": e.CellStyle.BackColor = Color.FromArgb(209, 52, 56); e.CellStyle.ForeColor = Color.White; break;
                case "Medium": e.CellStyle.BackColor = Color.FromArgb(247, 169, 36); e.CellStyle.ForeColor = Color.Black; break;
                case "Low": e.CellStyle.BackColor = Color.FromArgb(138, 136, 134); e.CellStyle.ForeColor = Color.White; break;
                case "Info": e.CellStyle.BackColor = Color.FromArgb(0, 120, 212); e.CellStyle.ForeColor = Color.White; break;
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            _detailGrid.Rows.Clear();
            if (_grid.SelectedRows.Count == 0 || !(_grid.SelectedRows[0].Tag is ComponentDiff d))
            {
                _lblDetail.Text = "Select a difference to see source vs target";
                _lblRecommendation.Text = "";
                return;
            }

            _lblDetail.Text = $"{d.Category} · {d.Name} · {d.Class}";
            if (d.Class == DiffClass.ManagedVsUnmanaged)
                _detailGrid.Rows.Add("managed", d.SourceManaged ? "managed" : "unmanaged", d.TargetManaged ? "managed" : "unmanaged");
            foreach (var c in d.ChangedProperties)
                _detailGrid.Rows.Add(c.Prop, c.Source, c.Target);
            if (d.Class == DiffClass.Missing) _detailGrid.Rows.Add("(presence)", "present", "MISSING");
            if (d.Class == DiffClass.Extra) _detailGrid.Rows.Add("(presence)", "MISSING", "present");

            var finding = _report?.Findings.FirstOrDefault(f =>
                f.Category == d.Category && f.Component == d.Name);
            _lblRecommendation.Text = finding?.Recommendation ?? "";
        }

        #endregion

        #region Connections

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            if (actionName == TargetActionName)
            {
                _targetService = newService;
                _targetName = detail?.ConnectionName ?? "target";
                _lblTarget.Text = $"Target: {_targetName}";
                _lblTarget.ForeColor = Color.SeaGreen;
                SetStatusMessage($"Target environment: {_targetName}");
                return; // keep the primary (source) connection untouched
            }

            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata differs between environments
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        private void AddAdditionalOrganization()
        {
            RaiseRequestConnectionEvent(new RequestConnectionEventArgs
            {
                ActionName = TargetActionName,
                Control = this
            });
        }

        #endregion

        #region Comparison

        private void RunComparison()
        {
            if (_targetService == null)
            {
                MessageBox.Show(this, "Connect a target environment first (toolbar button).",
                    "Environment Comparison Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var enabled = new HashSet<string>(_lstCategories.CheckedItems.Cast<string>(), StringComparer.OrdinalIgnoreCase);
            if (enabled.Count == 0)
            {
                MessageBox.Show(this, "Select at least one category to compare.",
                    "Environment Comparison Suite", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var target = _targetService;
            RunAsync(
                "Comparing environments…",
                worker =>
                {
                    var collector = new ComparisonCollector();
                    return collector.Compare(Service, target, enabled, worker,
                        msg => worker.ReportProgress(0, msg));
                },
                report =>
                {
                    _report = report;
                    BindReport(report);
                    _btnExport.Enabled = true;
                    SetStatusMessage($"Comparison complete — {report.Band} difference, score {report.Score}/100, " +
                                     $"{report.Diffs.Count(d => d.Class != DiffClass.Identical)} difference(s)");
                });
        }

        private void BindReport(ComparisonReport r)
        {
            // Populate filters from the report's actual content.
            RepopulateFilter(_cboCategoryFilter, r.Diffs.Select(d => d.Category));
            RepopulateFilter(_cboClassFilter, Enum.GetNames(typeof(DiffClass)));
            RepopulateFilter(_cboSeverityFilter, Enum.GetNames(typeof(Severity)));

            _lblScore.Text = $"{r.Band.ToString().ToUpperInvariant()} DIFFERENCE — score {r.Score}/100";
            _lblScore.ForeColor = r.Band == ScoreBand.High ? Color.FromArgb(255, 99, 99)
                                : r.Band == ScoreBand.Medium ? Color.FromArgb(247, 169, 36)
                                : Color.FromArgb(115, 209, 115);
            int Count(DiffClass c) => r.Diffs.Count(d => d.Class == c);
            _lblCounts.Text = $"{Count(DiffClass.Missing)} missing · {Count(DiffClass.Extra)} extra · " +
                              $"{Count(DiffClass.Changed)} changed · {Count(DiffClass.ManagedVsUnmanaged)} managed/unmanaged · " +
                              $"{Count(DiffClass.Identical)} identical";

            RebindGrid();
        }

        private static void RepopulateFilter(ComboBox combo, IEnumerable<string> values)
        {
            combo.Items.Clear();
            combo.Items.Add(AllFilter);
            foreach (var v in values.Distinct().OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                combo.Items.Add(v);
            combo.SelectedIndex = 0;
        }

        private void RebindGrid()
        {
            if (_report == null) return;

            string catF = _cboCategoryFilter.SelectedItem as string ?? AllFilter;
            string clsF = _cboClassFilter.SelectedItem as string ?? AllFilter;
            string sevF = _cboSeverityFilter.SelectedItem as string ?? AllFilter;

            _grid.Rows.Clear();
            var rows = _report.Diffs.Where(d => d.Class != DiffClass.Identical || clsF == nameof(DiffClass.Identical));
            foreach (var d in rows
                .Where(d => catF == AllFilter || d.Category == catF)
                .Where(d => clsF == AllFilter || d.Class.ToString() == clsF)
                .Where(d => sevF == AllFilter || d.Severity.ToString() == sevF)
                .OrderByDescending(d => d.Severity).ThenBy(d => d.Category).ThenBy(d => d.Name))
            {
                int idx = _grid.Rows.Add(d.Severity.ToString(), d.Category, d.Class.ToString(), d.Name, DetailText(d));
                _grid.Rows[idx].Tag = d;
            }
            SetStatusMessage($"{_grid.Rows.Count} difference(s) shown");
        }

        private static string DetailText(ComponentDiff d)
        {
            switch (d.Class)
            {
                case DiffClass.Missing: return "Present in source only";
                case DiffClass.Extra: return "Present in target only";
                case DiffClass.ManagedVsUnmanaged:
                    return $"{(d.SourceManaged ? "managed" : "unmanaged")} → {(d.TargetManaged ? "managed" : "unmanaged")}";
                default:
                    return string.Join(", ", d.ChangedProperties.Select(c => c.Prop));
            }
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_report == null) return;

            string filter, defaultName = $"EnvironmentComparison_{DateTime.Now:yyyyMMdd_HHmm}";
            switch (kind)
            {
                case "pdf": filter = "PDF report|*.pdf"; defaultName += ".pdf"; break;
                case "html": filter = "HTML report|*.html"; defaultName += ".html"; break;
                case "json": filter = "JSON|*.json"; defaultName += ".json"; break;
                default: filter = "Excel workbook|*.xlsx"; defaultName += ".xlsx"; break;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var path = dlg.FileName;
                var model = ComparisonReportModel.ToReportModel(_report,
                    ConnectionDetail?.ConnectionName ?? "source", _targetName ?? "target");

                RunAsync(
                    "Exporting report…",
                    worker =>
                    {
                        switch (kind)
                        {
                            case "pdf": PdfReportExporter.Export(model, path); break;
                            case "html": HtmlDashboardBuilder.Export(model, path); break;
                            case "json": JsonReportExporter.Export(model, path); break;
                            default: ExcelReportExporter.Export(model, path); break;
                        }
                        return path;
                    },
                    saved =>
                    {
                        if (MessageBox.Show(this, "Report exported. Open it now?", "Environment Comparison Suite",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                            System.Diagnostics.Process.Start(saved);
                    });
            }
        }

        #endregion
    }

    /// <summary>Persisted via SettingsManager (BaseToolControl.Load/SaveSettings). Plain serializable POCO.</summary>
    public class EnvironmentComparisonSettings
    {
        /// <summary>Categories the user unchecked (so a fresh install defaults to all-on).</summary>
        public List<string> DisabledCategories { get; set; } = new List<string>();
    }
}
