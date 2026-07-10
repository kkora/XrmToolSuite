using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.AttributeAuditor.Audit;
using XrmToolSuite.AttributeAuditor.Reporting;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Reporting;
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.AttributeAuditor
{
    public partial class AttributeAuditorControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private AuditSettings _settings;
        private AuditResult _lastResult;
        private string _envName = "environment";

        // Backing store for the virtual ListView: the currently filtered + sorted rows.
        private List<ColumnAudit> _view = new List<ColumnAudit>();
        private int _sortColumn;
        private bool _sortAscending = true;

        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (base BaseToolControl.ToolDocsUrl)

        public AttributeAuditorControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Attribute Auditor"));
        }

        private void AttributeAuditorControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<AuditSettings>();
            tsbCustomOnly.Checked = _settings.CustomEntitiesOnly;
            tsbCandidatesOnly.Checked = _settings.CandidatesOnly;
            LogInfo("Attribute Auditor loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
            {
                _settings.CustomEntitiesOnly = tsbCustomOnly.Checked;
                _settings.CandidatesOnly = tsbCandidatesOnly.Checked;
                SaveSettings(_settings);
            }
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            _envName = detail?.ConnectionName ?? "environment";
            SetStatusMessage($"Connected to {_envName}");
        }

        // ExecuteMethod ensures a connection exists (prompts to connect if not).
        private void tsbRun_Click(object sender, EventArgs e) => ExecuteMethod(RunAudit);

        private void RunAudit()
        {
            bool customOnly = tsbCustomOnly.Checked;
            // Exclusions are applied as a live view filter (see PopulateGrid), so the collector audits the
            // full set — that keeps the table counts accurate and lets exclusions re-filter without a re-run.
            RunAsync(
                "Auditing columns…",
                worker =>
                {
                    var ctx = new AttributeAuditContext(Service, _envName);
                    return AttributeUsageCollector.Collect(ctx, customOnly,
                        msg => worker.ReportProgress(0, msg));
                },
                result =>
                {
                    _lastResult = result;
                    PopulateGrid();
                    bool any = result.TotalColumns > 0;
                    tsbExportCsv.Enabled = any;
                    tsbExportHtml.Enabled = any;
                });
        }

        private void tsbCandidatesOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (_lastResult != null) PopulateGrid();
        }

        private void lvResults_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sortColumn) _sortAscending = !_sortAscending;
            else { _sortColumn = e.Column; _sortAscending = true; }
            if (_lastResult != null) PopulateGrid();
        }

        private void lvResults_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (e.ItemIndex < 0 || e.ItemIndex >= _view.Count) { e.Item = new ListViewItem(); return; }
            var c = _view[e.ItemIndex];
            var item = new ListViewItem(new[]
            {
                c.Table,
                c.LogicalName,
                c.DisplayName ?? "",
                c.AttributeType ?? "",
                c.IsManaged ? "yes" : "no",
                c.IsUsed ? "yes" : "no",
                c.UsageSummary()
            });
            if (c.IsRetirementCandidate)
                item.ForeColor = System.Drawing.Color.Firebrick; // highlight candidates
            e.Item = item;
        }

        private void tsbSettings_Click(object sender, EventArgs e)
        {
            using (var dlg = new ExclusionSettingsForm(_settings.ExcludeTablePrefixes, _settings.ExcludeColumnPrefixes))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _settings.ExcludeTablePrefixes = dlg.TablePrefixes;
                _settings.ExcludeColumnPrefixes = dlg.ColumnPrefixes;
                // Virtual-mode grid re-filters instantly (no re-run needed); PopulateGrid also refreshes the
                // status bar. Run it directly so the status message persists (a WorkAsync round-trip cleared it).
                if (_lastResult != null)
                    PopulateGrid();
                else
                    SetStatusMessage("Exclusions saved — run the audit to apply.");
            }
        }

        private static IEnumerable<string> SplitCsv(string csv) =>
            string.IsNullOrWhiteSpace(csv)
                ? Enumerable.Empty<string>()
                : csv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0);

        private static bool StartsWithAny(string value, List<string> prefixes)
        {
            if (string.IsNullOrEmpty(value)) return false;
            foreach (var p in prefixes)
                if (value.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>
        /// Returns <paramref name="src"/> with the current table/column exclusion prefixes applied, so exports
        /// match the visible grid even when exclusions were changed without re-running the audit. Returns the
        /// original instance when no exclusions are set.
        /// </summary>
        private AuditResult ApplyExclusions(AuditResult src)
        {
            var tablePrefixes = SplitCsv(_settings.ExcludeTablePrefixes).ToList();
            var columnPrefixes = SplitCsv(_settings.ExcludeColumnPrefixes).ToList();
            if (tablePrefixes.Count == 0 && columnPrefixes.Count == 0) return src;

            var filtered = new AuditResult { EnvironmentName = src.EnvironmentName, AuditedOnUtc = src.AuditedOnUtc };
            foreach (var c in src.Columns)
            {
                if (StartsWithAny(c.Table, tablePrefixes)) continue;
                if (StartsWithAny(c.LogicalName, columnPrefixes)) continue;
                filtered.Columns.Add(c);
            }
            return filtered;
        }

        private void PopulateGrid()
        {
            var tablePrefixes = SplitCsv(_settings.ExcludeTablePrefixes).ToList();
            var columnPrefixes = SplitCsv(_settings.ExcludeColumnPrefixes).ToList();

            IEnumerable<ColumnAudit> rows = _lastResult.Columns;
            if (tsbCandidatesOnly.Checked) rows = rows.Where(c => c.IsRetirementCandidate);
            // Exclusion prefixes are a live view filter so changing them updates the grid instantly (no re-run).
            if (tablePrefixes.Count > 0)
                rows = rows.Where(c => !StartsWithAny(c.Table, tablePrefixes));
            if (columnPrefixes.Count > 0)
                rows = rows.Where(c => !StartsWithAny(c.LogicalName, columnPrefixes));

            _view = rows.ToList();
            SortView();

            // Virtual mode: set the size and repaint — items are built on demand in RetrieveVirtualItem, so
            // even a multi-thousand-row audit stays responsive (no per-row ListViewItem creation up front).
            lvResults.VirtualListSize = _view.Count;
            lvResults.Invalidate();

            UpdateStatusCounts(tablePrefixes);
        }

        private void SortView()
        {
            int dir = _sortAscending ? 1 : -1;
            _view.Sort((a, b) =>
            {
                int r = string.Compare(SortKey(a, _sortColumn), SortKey(b, _sortColumn), StringComparison.OrdinalIgnoreCase);
                if (r != 0) return dir * r;
                // Stable, predictable tie-break: table then logical name.
                r = string.Compare(a.Table, b.Table, StringComparison.OrdinalIgnoreCase);
                if (r != 0) return r;
                return string.Compare(a.LogicalName, b.LogicalName, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static string SortKey(ColumnAudit c, int column)
        {
            switch (column)
            {
                case 0: return c.Table ?? "";
                case 1: return c.LogicalName ?? "";
                case 2: return c.DisplayName ?? "";
                case 3: return c.AttributeType ?? "";
                case 4: return c.IsManaged ? "yes" : "no";
                case 5: return c.IsUsed ? "yes" : "no";
                case 6: return c.UsageSummary();
                default: return c.Table ?? "";
            }
        }

        /// <summary>Reports the active filters plus table-level and column-level counts on the status bar.</summary>
        private void UpdateStatusCounts(List<string> tablePrefixes)
        {
            if (_lastResult == null) return;

            var columnPrefixes = SplitCsv(_settings.ExcludeColumnPrefixes).ToList();
            int excludedTables = tablePrefixes.Count == 0
                ? 0
                : _lastResult.Columns.Select(c => c.Table)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count(t => StartsWithAny(t, tablePrefixes));
            int shownTables = _view.Select(c => c.Table).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            int usedShown = _view.Count(c => c.IsUsed);
            int candidateShown = _view.Count(c => c.IsRetirementCandidate);

            // Active-filter summary so it's clear what the shown counts reflect.
            var filters = new List<string>();
            if (tsbCustomOnly.Checked) filters.Add("custom tables only");
            if (tsbCandidatesOnly.Checked) filters.Add("candidates only");
            if (tablePrefixes.Count > 0 || columnPrefixes.Count > 0) filters.Add("exclusions");
            string filterText = filters.Count == 0 ? "none" : string.Join(", ", filters);

            SetStatusMessage(
                $"Filters: {filterText}  •  " +
                $"Tables: {_lastResult.TotalTables} total, {_lastResult.NonCustomTables} non-custom, " +
                $"{excludedTables} excluded, {shownTables} shown  •  " +
                $"Columns: {_view.Count} shown ({usedShown} used, {candidateShown} candidate(s))");
        }

        private void tsbExportCsv_Click(object sender, EventArgs e)
        {
            if (_lastResult == null) return;
            using (var dlg = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "attribute-audit.csv" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var data = ApplyExclusions(_lastResult);
                    AuditCsvExporter.Export(data, dlg.FileName);
                    SetStatusMessage($"Exported {data.TotalColumns} row(s) to CSV");
                    PromptOpenExportedFile(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex, "CSV export failed"); }
            }
        }

        private void tsbExportHtml_Click(object sender, EventArgs e)
        {
            if (_lastResult == null) return;
            using (var dlg = new SaveFileDialog { Filter = "HTML report (*.html)|*.html", FileName = "attribute-audit.html" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    HtmlDashboardBuilder.Export(AttributeAuditReport.ToReportModel(ApplyExclusions(_lastResult)), dlg.FileName);
                    SetStatusMessage("Exported HTML report");
                    PromptOpenExportedFile(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex, "HTML export failed"); }
            }
        }
    }

    /// <summary>Persisted automatically via SettingsManager (see Load/ClosingPlugin). No credentials.</summary>
    public class AuditSettings
    {
        public bool CustomEntitiesOnly { get; set; } = true;
        public bool CandidatesOnly { get; set; }

        /// <summary>Comma-separated logical-name prefixes; tables whose name starts with any are excluded.</summary>
        public string ExcludeTablePrefixes { get; set; } = "";

        /// <summary>Comma-separated logical-name prefixes; columns whose name starts with any are excluded.</summary>
        public string ExcludeColumnPrefixes { get; set; } = "";
    }

    /// <summary>
    /// Small modal dialog for the two exclusion lists (table/column logical-name prefixes, comma-separated).
    /// Kept UI-only; the actual filtering lives in <see cref="AttributeUsageCollector"/>.
    /// </summary>
    internal sealed class ExclusionSettingsForm : Form
    {
        private readonly TextBox _txtTables;
        private readonly TextBox _txtColumns;

        public string TablePrefixes => _txtTables.Text.Trim();
        public string ColumnPrefixes => _txtColumns.Text.Trim();

        public ExclusionSettingsForm(string tablePrefixes, string columnPrefixes)
        {
            Text = "Exclusions";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new System.Drawing.Size(460, 210);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 5
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "Exclude tables whose logical name starts with (comma-separated):",
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 2)
            });
            _txtTables = new TextBox { Dock = DockStyle.Top, Text = tablePrefixes ?? "" };
            layout.Controls.Add(_txtTables);

            layout.Controls.Add(new System.Windows.Forms.Label
            {
                Text = "Exclude columns whose logical name starts with (comma-separated):",
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 2)
            });
            _txtColumns = new TextBox { Dock = DockStyle.Top, Text = columnPrefixes ?? "" };
            layout.Controls.Add(_txtColumns);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 12, 0, 0)
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            layout.Controls.Add(buttons);

            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
