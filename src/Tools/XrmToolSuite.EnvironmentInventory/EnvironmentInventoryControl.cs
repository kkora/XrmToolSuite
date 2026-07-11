using System;
using System.Collections.Generic;
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

        private void Filter_Changed(object sender, EventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (_snapshot == null) return;

            string category = cboCategory.SelectedIndex > 0 ? cboCategory.SelectedItem as string : null;
            bool? managed = cboManaged.SelectedIndex == 1 ? true
                          : cboManaged.SelectedIndex == 2 ? false
                          : (bool?)null;

            var rows = _snapshot.Filter(txtSearch.Text, category, managed)
                                .OrderBy(i => i.Category, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(i => i.ComponentType, StringComparer.OrdinalIgnoreCase)
                                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                                .ToList();

            grdInventory.SuspendLayout();
            grdInventory.Rows.Clear();
            foreach (var i in rows)
            {
                int r = grdInventory.Rows.Add(
                    i.Category,
                    i.ComponentType,
                    i.Name,
                    i.SchemaName,
                    i.IsManaged.HasValue ? (i.IsManaged.Value ? "Managed" : "Unmanaged") : "",
                    i.ModifiedOn?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "");
                grdInventory.Rows[r].Tag = i;
            }
            grdInventory.ResumeLayout();

            SetStatusMessage($"Showing {rows.Count} of {_snapshot.Total} component(s)");
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

        private void tsmiExportCsv_Click(object sender, EventArgs e) =>
            Export("CSV file (*.csv)|*.csv", "environment-inventory.csv", InventoryExporter.ToCsv, useBom: true);

        private void tsmiExportJson_Click(object sender, EventArgs e) =>
            Export("JSON file (*.json)|*.json", "environment-inventory.json", InventoryExporter.ToJson, useBom: false);

        private void tsmiExportMarkdown_Click(object sender, EventArgs e) =>
            Export("Markdown file (*.md)|*.md", "environment-inventory.md", InventoryExporter.ToMarkdown, useBom: false);

        private void tsmiExportHtml_Click(object sender, EventArgs e) =>
            Export("HTML report (*.html)|*.html", "environment-inventory.html", InventoryExporter.ToHtml, useBom: false);

        // Excel: FULL inventory grid (richer than the summary report).
        private void tsmiExportExcel_Click(object sender, EventArgs e) =>
            ExportFile("Excel workbook (*.xlsx)|*.xlsx", "environment-inventory.xlsx",
                path => InventoryExcelExporter.Export(_snapshot, path));

        // Word + PDF: summary-level report from the shared ReportModel exporters.
        private void tsmiExportWord_Click(object sender, EventArgs e) =>
            ExportFile("Word document (*.docx)|*.docx", "environment-inventory.docx",
                path => WordReportExporter.Export(InventorySummary.ToReportModel(_snapshot), path));

        private void tsmiExportPdf_Click(object sender, EventArgs e) =>
            ExportFile("PDF document (*.pdf)|*.pdf", "environment-inventory.pdf",
                path => PdfReportExporter.Export(InventorySummary.ToReportModel(_snapshot), path));

        private void Export(string filter, string defaultName, Func<InventorySnapshot, string> render, bool useBom)
        {
            if (_snapshot == null) return;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dlg.FileName, render(_snapshot), new UTF8Encoding(useBom));
                    SetStatusMessage($"Exported {_snapshot.Total} component(s) to {Path.GetFileName(dlg.FileName)}");
                    PromptOpenExportedFile(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex, "Export failed"); }
            }
        }

        /// <summary>Binary/document export (Excel/Word/PDF): the writer owns the file path directly.
        /// ClosedXML/PdfSharp/MigraDoc types stay inside the writer delegate, never in a signature here.</summary>
        private void ExportFile(string filter, string defaultName, Action<string> write)
        {
            if (_snapshot == null) return;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    write(dlg.FileName);
                    SetStatusMessage($"Exported {_snapshot.Total} component(s) to {Path.GetFileName(dlg.FileName)}");
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
