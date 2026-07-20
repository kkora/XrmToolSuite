using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.EnvironmentInventory.Inventory;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.EnvironmentInventory
{
    public partial class EnvironmentInventoryControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;
        private InventorySnapshot _snapshot;
        private string _envName = "environment";

        // The filtered + sorted rows currently backing the grid. Column-header sorting re-orders THIS list
        // and re-binds (fast) instead of invoking DataGridView's built-in unbound sort, which freezes the
        // UI on large environments (100k+ rows).
        private List<InventoryItem> _view;
        private int _sortColumn = -1;                    // -1 = default composite order (Category/Type/Name)
        private SortOrder _sortDirection = SortOrder.Ascending;

        // Debounces search-box keystrokes so we filter once the user pauses, not on every character.
        private Timer _searchTimer;
        // A lightweight "processing" badge shown over the grid during a filter/sort/re-bind.
        private System.Windows.Forms.Label _busyLabel;

        // The order and identity of the source toggles in the "Sources" dropdown.
        private static readonly Tuple<string, string>[] SourceDefs =
        {
            Tuple.Create("Solutions", "Solutions & publishers"),
            Tuple.Create("Tables", "Tables"),
            Tuple.Create("SecurityRoles", "Security roles"),
            Tuple.Create("UsersTeamsBU", "Users, teams & business units"),
            Tuple.Create("Plugins", "Plugins & steps"),
            Tuple.Create("Workflows", "Workflows & flows"),
            Tuple.Create("WebResources", "Web resources & PCF"),
            Tuple.Create("CustomApis", "Custom APIs"),
            Tuple.Create("EnvVarsConnRefs", "Environment variables & connection references"),
        };

        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public EnvironmentInventoryControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Environment Inventory"));
            SetupInteractivity();
        }

        /// <summary>
        /// Wires the grid's client-side sorting, the debounced search, and the "processing" badge. Runs once
        /// after <see cref="InitializeComponent"/> so the grid, its columns, and the split panel all exist.
        /// </summary>
        private void SetupInteractivity()
        {
            // Programmatic sort mode: the header still raises a click (and shows the sort glyph we set), but
            // the grid does NOT run its own O(n log n) unbound sort — we sort the model in GrdInventory_
            // ColumnHeaderMouseClick instead. Automatic mode here would freeze the UI on 100k+ rows.
            foreach (DataGridViewColumn c in grdInventory.Columns)
                c.SortMode = DataGridViewColumnSortMode.Programmatic;
            grdInventory.ColumnHeaderMouseClick += GrdInventory_ColumnHeaderMouseClick;
            EnableDoubleBuffer(grdInventory);

            _searchTimer = new Timer { Interval = 300 };
            _searchTimer.Tick += (s, e) => { _searchTimer.Stop(); ApplyFilter(); };

            _busyLabel = new System.Windows.Forms.Label
            {
                Text = "Filtering…",
                AutoSize = false,
                Size = new Size(150, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = SystemColors.Info,
                ForeColor = SystemColors.InfoText,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            split.Panel1.Controls.Add(_busyLabel);
            _busyLabel.BringToFront();
        }

        // DataGridView.DoubleBuffered is protected; enabling it cuts flicker and paint cost during a bulk fill.
        private static void EnableDoubleBuffer(DataGridView grid)
        {
            typeof(DataGridView)
                .GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(grid, true, null);
        }

        private void EnvironmentInventoryControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();

            cboManaged.Items.AddRange(new object[] { "All", "Managed", "Unmanaged" });
            cboManaged.SelectedIndex = 0;
            cboCategory.Items.Add("(all categories)");
            cboCategory.SelectedIndex = 0;

            BuildSourcesMenu();
            ApplySettingsToUi();

            LogInfo("Environment Inventory loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
            {
                _settings.Scope = ReadScopeFromMenu();
                _settings.LastSearch = txtSearch.Text;
                _settings.LastCategory = cboCategory.SelectedItem as string;
                _settings.LastManaged = cboManaged.SelectedIndex;
                SaveSettings(_settings);
            }
            _searchTimer?.Dispose();
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

        // ---- sources dropdown ----

        private void BuildSourcesMenu()
        {
            tsdSources.DropDownItems.Clear();
            foreach (var def in SourceDefs)
            {
                var item = new ToolStripMenuItem(def.Item2)
                {
                    CheckOnClick = true,
                    Checked = true,
                    Tag = def.Item1
                };
                tsdSources.DropDownItems.Add(item);
            }
        }

        private void ApplySettingsToUi()
        {
            var scope = _settings.Scope ?? InventoryScope.All();
            foreach (ToolStripMenuItem item in tsdSources.DropDownItems.OfType<ToolStripMenuItem>())
                item.Checked = GetScopeFlag(scope, (string)item.Tag);

            if (!string.IsNullOrEmpty(_settings.LastSearch)) txtSearch.Text = _settings.LastSearch;
            if (_settings.LastManaged >= 0 && _settings.LastManaged < cboManaged.Items.Count)
                cboManaged.SelectedIndex = _settings.LastManaged;
        }

        private InventoryScope ReadScopeFromMenu()
        {
            var scope = new InventoryScope();
            foreach (ToolStripMenuItem item in tsdSources.DropDownItems.OfType<ToolStripMenuItem>())
                SetScopeFlag(scope, (string)item.Tag, item.Checked);
            return scope;
        }

        private static bool GetScopeFlag(InventoryScope scope, string name)
        {
            var prop = typeof(InventoryScope).GetProperty(name);
            return prop != null && (bool)prop.GetValue(scope, null);
        }

        private static void SetScopeFlag(InventoryScope scope, string name, bool value)
        {
            var prop = typeof(InventoryScope).GetProperty(name);
            if (prop != null) prop.SetValue(scope, value, null);
        }

        // ---- collect ----

        private void tsbCollect_Click(object sender, EventArgs e) => ExecuteMethod(CollectInventory);

        private void CollectInventory()
        {
            var scope = ReadScopeFromMenu();
            var envName = _envName;
            RunAsync(
                "Collecting environment inventory…",
                worker =>
                {
                    var snap = InventoryCollector.Collect(Service, scope, worker, msg => worker.ReportProgress(0, msg));
                    snap.EnvironmentName = envName;
                    return snap;
                },
                snapshot =>
                {
                    _snapshot = snapshot;
                    RefreshCategoryFilter();
                    ApplyFilter();
                    tsdExport.Enabled = snapshot.Total > 0;

                    var status = new StringBuilder($"{snapshot.Total} component(s) inventoried");
                    if (snapshot.UnavailableSources.Count > 0)
                        status.Append($" — unavailable: {string.Join(", ", snapshot.UnavailableSources)}");
                    SetStatusMessage(status.ToString());
                });
        }

        // ---- filtering (client-side over the cached snapshot) ----

        private void RefreshCategoryFilter()
        {
            var previous = cboCategory.SelectedItem as string;
            cboCategory.Items.Clear();
            cboCategory.Items.Add("(all categories)");
            if (_snapshot != null)
                foreach (var c in _snapshot.Categories())
                    cboCategory.Items.Add(c);

            var idx = previous != null ? cboCategory.Items.IndexOf(previous) : -1;
            cboCategory.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            // Debounce the search box (re-filter once typing pauses); apply dropdown changes immediately.
            if (sender == txtSearch && _searchTimer != null)
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            }
            else
            {
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            if (_snapshot == null) return;

            string category = cboCategory.SelectedIndex > 0 ? cboCategory.SelectedItem as string : null;
            bool? managed = cboManaged.SelectedIndex == 1 ? true
                          : cboManaged.SelectedIndex == 2 ? false
                          : (bool?)null;

            _view = _snapshot.Filter(txtSearch.Text, category, managed).ToList();
            SortAndBind();
        }

        private void GrdInventory_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_view == null || e.ColumnIndex < 0) return;

            if (_sortColumn == e.ColumnIndex)
                _sortDirection = _sortDirection == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortColumn = e.ColumnIndex;
                _sortDirection = SortOrder.Ascending;
            }
            SortAndBind();
        }

        /// <summary>Sorts <see cref="_view"/> in place and re-binds the grid, with the processing badge up.</summary>
        private void SortAndBind()
        {
            if (_view == null) return;

            ShowBusy(true);
            try
            {
                SortView(_view);
                BindGrid(_view);
                UpdateSortGlyphs();
                SetStatusMessage($"Showing {_view.Count} of {_snapshot.Total} component(s)");
            }
            finally
            {
                ShowBusy(false);
            }
        }

        private void SortView(List<InventoryItem> rows)
        {
            Comparison<InventoryItem> cmp;
            switch (_sortColumn)
            {
                case 0: cmp = (a, b) => Str(a.Category, b.Category); break;
                case 1: cmp = (a, b) => Str(a.ComponentType, b.ComponentType); break;
                case 2: cmp = (a, b) => Str(a.Name, b.Name); break;
                case 3: cmp = (a, b) => Str(a.SchemaName, b.SchemaName); break;
                case 4: cmp = (a, b) => Str(ManagedText(a), ManagedText(b)); break;
                case 5: cmp = (a, b) => Nullable.Compare(a.ModifiedOn, b.ModifiedOn); break;
                default: rows.Sort(DefaultOrder); return; // no column chosen → stable Category/Type/Name order
            }
            int dir = _sortDirection == SortOrder.Descending ? -1 : 1;
            rows.Sort((a, b) => dir * cmp(a, b));
        }

        private static int DefaultOrder(InventoryItem a, InventoryItem b)
        {
            int c = Str(a.Category, b.Category);
            if (c != 0) return c;
            c = Str(a.ComponentType, b.ComponentType);
            if (c != 0) return c;
            return Str(a.Name, b.Name);
        }

        private static int Str(string a, string b) =>
            string.Compare(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        private static string ManagedText(InventoryItem i) =>
            i.IsManaged.HasValue ? (i.IsManaged.Value ? "Managed" : "Unmanaged") : "";

        // Fills the grid in one AddRange pass — far faster than N Rows.Add calls on a large filtered set.
        private void BindGrid(List<InventoryItem> rows)
        {
            grdInventory.SuspendLayout();
            grdInventory.Rows.Clear();

            var gridRows = new DataGridViewRow[rows.Count];
            for (int k = 0; k < rows.Count; k++)
            {
                var i = rows[k];
                var row = new DataGridViewRow();
                row.CreateCells(grdInventory,
                    i.Category,
                    i.ComponentType,
                    i.Name,
                    i.SchemaName,
                    ManagedText(i),
                    i.ModifiedOn?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "");
                row.Tag = i;
                gridRows[k] = row;
            }

            if (gridRows.Length > 0) grdInventory.Rows.AddRange(gridRows);
            grdInventory.ResumeLayout();
        }

        private void UpdateSortGlyphs()
        {
            foreach (DataGridViewColumn c in grdInventory.Columns)
                c.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (_sortColumn >= 0 && _sortColumn < grdInventory.Columns.Count)
                grdInventory.Columns[_sortColumn].HeaderCell.SortGlyphDirection = _sortDirection;
        }

        // Shows/hides the centered "Filtering…" badge and wait cursor. Update() forces a synchronous paint so
        // the badge is visible BEFORE the grid re-bind blocks the UI thread.
        private void ShowBusy(bool on)
        {
            if (_busyLabel == null) return;
            if (on)
            {
                _busyLabel.Location = new Point(
                    Math.Max(0, (split.Panel1.ClientSize.Width - _busyLabel.Width) / 2),
                    Math.Max(0, (split.Panel1.ClientSize.Height - _busyLabel.Height) / 2));
                _busyLabel.Visible = true;
                _busyLabel.BringToFront();
                _busyLabel.Update();
                Cursor.Current = Cursors.WaitCursor;
            }
            else
            {
                _busyLabel.Visible = false;
                Cursor.Current = Cursors.Default;
            }
        }

        private void grdInventory_SelectionChanged(object sender, EventArgs e)
        {
            var item = grdInventory.SelectedRows.Count > 0
                ? grdInventory.SelectedRows[0].Tag as InventoryItem
                : null;
            if (item == null) { txtDetail.Clear(); return; }

            var sb = new StringBuilder();
            sb.AppendLine($"Category:     {item.Category}");
            sb.AppendLine($"Type:         {item.ComponentType}");
            sb.AppendLine($"Name:         {item.Name}");
            sb.AppendLine($"Schema name:  {item.SchemaName}");
            sb.AppendLine($"Owner:        {item.Owner}");
            sb.AppendLine($"Managed:      {(item.IsManaged.HasValue ? (item.IsManaged.Value ? "Managed" : "Unmanaged") : "(n/a)")}");
            sb.AppendLine($"Modified:     {item.ModifiedOn?.ToLocalTime().ToString("F", CultureInfo.CurrentCulture) ?? "(n/a)"}");
            if (item.Details != null && item.Details.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Details:");
                foreach (var kv in item.Details)
                    sb.AppendLine($"  {kv.Key}: {TextBoxFormat.CrLf(kv.Value?.ToString())}");
            }
            txtDetail.Text = sb.ToString();
            txtDetail.SelectionStart = 0;
        }

        // ---- export ----
        //
        // Every export reflects the CURRENT filtered/sorted view (what the grid shows), not the full
        // inventory. ExportSnapshot() wraps _view while preserving the environment name, collection time,
        // and unavailable-source notes so summaries stay accurate.

        private void tsmiExportCsv_Click(object sender, EventArgs e) =>
            Export("CSV file (*.csv)|*.csv", "environment-inventory.csv", InventoryExporter.ToCsv, useBom: true);

        private void tsmiExportJson_Click(object sender, EventArgs e) =>
            Export("JSON file (*.json)|*.json", "environment-inventory.json", InventoryExporter.ToJson, useBom: false);

        private void tsmiExportMarkdown_Click(object sender, EventArgs e) =>
            Export("Markdown file (*.md)|*.md", "environment-inventory.md", InventoryExporter.ToMarkdown, useBom: false);

        private void tsmiExportHtml_Click(object sender, EventArgs e) =>
            Export("HTML report (*.html)|*.html", "environment-inventory.html", InventoryExporter.ToHtml, useBom: false);

        // Excel: full grid of the filtered rows (richer than the summary report).
        private void tsmiExportExcel_Click(object sender, EventArgs e) =>
            ExportFile("Excel workbook (*.xlsx)|*.xlsx", "environment-inventory.xlsx",
                (snap, path) => InventoryExcelExporter.Export(snap, path));

        // Word + PDF: inventory catalog (title + key-metric counts + records grouped by category). These use
        // the tool-local catalog exporters, NOT the suite's score/severity analyzer template — an inventory
        // is a catalog, not an assessment.
        private void tsmiExportWord_Click(object sender, EventArgs e) =>
            ExportFile("Word document (*.docx)|*.docx", "environment-inventory.docx",
                (snap, path) => InventoryWordExporter.Export(snap, path));

        private void tsmiExportPdf_Click(object sender, EventArgs e) =>
            ExportFile("PDF document (*.pdf)|*.pdf", "environment-inventory.pdf",
                (snap, path) => InventoryPdfExporter.Export(snap, path));

        /// <summary>
        /// The set to export: the currently filtered + sorted rows (<see cref="_view"/>), wrapped in an
        /// <see cref="InventorySnapshot"/> that carries over the environment name, collection time, and
        /// unavailable-source list so exported summaries and counts match the on-screen grid.
        /// </summary>
        private InventorySnapshot ExportSnapshot()
        {
            if (_snapshot == null) return null;
            return new InventorySnapshot
            {
                EnvironmentName = _snapshot.EnvironmentName,
                CollectedOnUtc = _snapshot.CollectedOnUtc,
                Items = _view ?? new List<InventoryItem>(),
                UnavailableSources = _snapshot.UnavailableSources
            };
        }

        private void Export(string filter, string defaultName, Func<InventorySnapshot, string> render, bool useBom)
        {
            var snap = ExportSnapshot();
            if (snap == null) return;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dlg.FileName, render(snap), new UTF8Encoding(useBom));
                    SetStatusMessage($"Exported {snap.Total} component(s) to {Path.GetFileName(dlg.FileName)}");
                    PromptOpenExportedFile(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex, "Export failed"); }
            }
        }

        /// <summary>Binary/document export (Excel/Word/PDF): the writer receives the filtered snapshot and the
        /// file path. ClosedXML/PdfSharp/MigraDoc types stay inside the writer delegate, never in a signature here.</summary>
        private void ExportFile(string filter, string defaultName, Action<InventorySnapshot, string> write)
        {
            var snap = ExportSnapshot();
            if (snap == null) return;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    write(snap, dlg.FileName);
                    SetStatusMessage($"Exported {snap.Total} component(s) to {Path.GetFileName(dlg.FileName)}");
                    PromptOpenExportedFile(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex, "Export failed"); }
            }
        }
    }

    /// <summary>Persisted automatically via SettingsManager (see Load/ClosingPlugin). No credentials or secrets.</summary>
    public class ToolSettings
    {
        public InventoryScope Scope { get; set; } = new InventoryScope();
        public string LastSearch { get; set; }
        public string LastCategory { get; set; }
        public int LastManaged { get; set; }
    }
}
