using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.ErdGenerator.Erd;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.ErdGenerator
{
    /// <summary>
    /// Generates a Dataverse ERD from live metadata: scope by all tables / solution / publisher, pick the
    /// tables, choose a column-display level, filter (custom-only / managed-only), preview the Mermaid
    /// output, and export to Mermaid, PlantUML, SVG, PNG, PDF, HTML, Markdown or JSON. Follows the suite
    /// patterns (BaseToolControl, RunAsync, Load/SaveSettings). Ships only the native-PDF (MigraDoc-GDI)
    /// chain; PNG uses GDI+ (System.Drawing, referenced not shipped).
    /// </summary>
    public partial class ErdGeneratorControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        private const string ScopeAll = "All tables";
        private const string ScopeSolution = "By solution";
        private const string ScopePublisher = "By publisher";

        private readonly ErdCollector _collector = new ErdCollector();
        private ErdGeneratorSettings _settings = new ErdGeneratorSettings();

        private List<ErdTableInfo> _allTables = new List<ErdTableInfo>();
        private readonly HashSet<string> _checked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<ErdSolutionInfo> _solutions = new List<ErdSolutionInfo>();
        private List<ErdPublisherInfo> _publishers = new List<ErdPublisherInfo>();
        private ErdModel _model;      // last built model
        private bool _suppressCheck;  // suppress ItemCheck while repopulating the list

        public ErdGeneratorControl()
        {
            InitializeComponent();

            cboScope.Items.AddRange(new object[] { ScopeAll, ScopeSolution, ScopePublisher });
            cboScope.SelectedIndex = 0;
            cboColumns.Items.AddRange(new object[] { "Keys + lookups", "Important columns", "All columns" });
            cboColumns.SelectedIndex = 0;

            tsbExport.DropDownItems.Add("Mermaid (.mmd)", null, (s, e) => Export("mermaid"));
            tsbExport.DropDownItems.Add("PlantUML (.puml)", null, (s, e) => Export("plantuml"));
            tsbExport.DropDownItems.Add("SVG (.svg)", null, (s, e) => Export("svg"));
            tsbExport.DropDownItems.Add("PNG (.png)", null, (s, e) => Export("png"));
            tsbExport.DropDownItems.Add("PDF (.pdf)", null, (s, e) => Export("pdf"));
            tsbExport.DropDownItems.Add("HTML (.html)", null, (s, e) => Export("html"));
            tsbExport.DropDownItems.Add("Markdown (.md)", null, (s, e) => Export("markdown"));
            tsbExport.DropDownItems.Add("JSON (.json)", null, (s, e) => Export("json"));

            tsbLoadTables.Click += (s, e) => ExecuteMethod(LoadTables);
            tsbGenerate.Click += (s, e) => ExecuteMethod(Generate);
            cboScope.SelectedIndexChanged += (s, e) => ExecuteMethod(OnScopeChanged);
            cboColumns.SelectedIndexChanged += (s, e) => RefreshPreview();
            tsbCustomOnly.CheckedChanged += (s, e) => RefreshPreview();
            tsbManagedOnly.CheckedChanged += (s, e) => RefreshPreview();
            txtTableFilter.TextChanged += (s, e) => PopulateTableList();
            clbTables.ItemCheck += ClbTables_ItemCheck;

            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("ERD Generator"));
        }

        #region Lifecycle

        private void ErdGeneratorControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ErdGeneratorSettings>();
            ApplySettingsToUi();
            LogInfo("ERD Generator loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            CaptureSettingsFromUi();
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail,
            string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            _allTables.Clear();
            _checked.Clear();
            _solutions.Clear();
            _publishers.Clear();
            _model = null;
            clbTables.Items.Clear();
            cboScopeValue.Items.Clear();
            tsbExport.Enabled = false;
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        private void ApplySettingsToUi()
        {
            if (!string.IsNullOrEmpty(_settings.Scope) && cboScope.Items.Contains(_settings.Scope))
                cboScope.SelectedItem = _settings.Scope;
            if (_settings.ColumnDisplayIndex >= 0 && _settings.ColumnDisplayIndex < cboColumns.Items.Count)
                cboColumns.SelectedIndex = _settings.ColumnDisplayIndex;
            tsbCustomOnly.Checked = _settings.CustomOnly;
            tsbManagedOnly.Checked = _settings.ManagedOnly;
        }

        private void CaptureSettingsFromUi()
        {
            _settings.Scope = cboScope.SelectedItem?.ToString();
            _settings.ColumnDisplayIndex = cboColumns.SelectedIndex;
            _settings.CustomOnly = tsbCustomOnly.Checked;
            _settings.ManagedOnly = tsbManagedOnly.Checked;
        }

        #endregion

        #region Scope + table loading

        private void OnScopeChanged()
        {
            var scope = cboScope.SelectedItem?.ToString();
            cboScopeValue.Items.Clear();
            cboScopeValue.Enabled = scope != ScopeAll;
            if (scope == ScopeSolution)
            {
                RunAsync("Loading solutions…",
                    worker => _collector.ListSolutions(Service),
                    solutions =>
                    {
                        _solutions = solutions;
                        foreach (var s in solutions)
                            cboScopeValue.Items.Add($"{s.FriendlyName} ({s.Version})");
                        if (cboScopeValue.Items.Count > 0) cboScopeValue.SelectedIndex = 0;
                        SetStatusMessage($"Loaded {solutions.Count} solution(s). Pick one, then Load tables.");
                    });
            }
            else if (scope == ScopePublisher)
            {
                RunAsync("Loading publishers…",
                    worker => _collector.ListPublishers(Service),
                    publishers =>
                    {
                        _publishers = publishers;
                        foreach (var p in publishers)
                            cboScopeValue.Items.Add($"{p.Name} ({p.Prefix}_)");
                        if (cboScopeValue.Items.Count > 0) cboScopeValue.SelectedIndex = 0;
                        SetStatusMessage($"Loaded {publishers.Count} publisher(s). Pick one, then Load tables.");
                    });
            }
        }

        private void LoadTables()
        {
            var scope = cboScope.SelectedItem?.ToString();
            int scopeIdx = cboScopeValue.SelectedIndex;

            RunAsync("Loading tables…",
                worker =>
                {
                    var all = _collector.ListTableInfos(Service, worker);
                    if (scope == ScopeSolution && scopeIdx >= 0 && scopeIdx < _solutions.Count)
                    {
                        var ids = _collector.GetSolutionEntityIds(Service, _solutions[scopeIdx].Id);
                        all = all.Where(t => ids.Contains(t.MetadataId)).ToList();
                    }
                    else if (scope == ScopePublisher && scopeIdx >= 0 && scopeIdx < _publishers.Count)
                    {
                        var prefix = _publishers[scopeIdx].Prefix + "_";
                        all = all.Where(t => (t.LogicalName ?? "").StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
                    }
                    return all;
                },
                tables =>
                {
                    _allTables = tables;
                    _checked.Clear();
                    PopulateTableList();
                    SetStatusMessage($"Loaded {tables.Count} table(s). Check tables to include, then Generate.");
                });
        }

        // Filters by the custom/managed toggles + the text filter, preserving checked state.
        private void PopulateTableList()
        {
            _suppressCheck = true;
            clbTables.BeginUpdate();
            clbTables.Items.Clear();

            string term = (txtTableFilter.Text ?? "").Trim().ToLowerInvariant();
            bool customOnly = tsbCustomOnly.Checked;
            bool managedOnly = tsbManagedOnly.Checked;

            foreach (var t in _allTables
                .Where(t => !customOnly || t.IsCustom)
                .Where(t => !managedOnly || t.IsManaged)
                .Where(t => term.Length == 0
                    || (t.LogicalName ?? "").ToLowerInvariant().Contains(term)
                    || (t.DisplayName ?? "").ToLowerInvariant().Contains(term)))
            {
                clbTables.Items.Add($"{t.DisplayName}  [{t.LogicalName}]", _checked.Contains(t.LogicalName));
            }

            clbTables.EndUpdate();
            _suppressCheck = false;
        }

        private void ClbTables_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_suppressCheck) return;
            var logical = LogicalNameAt(e.Index);
            if (logical == null) return;
            if (e.NewValue == CheckState.Checked) _checked.Add(logical);
            else _checked.Remove(logical);
            lblStats.Text = $"{_checked.Count} table(s) selected.";
        }

        private string LogicalNameAt(int index)
        {
            if (index < 0 || index >= clbTables.Items.Count) return null;
            var text = clbTables.Items[index].ToString();
            int open = text.LastIndexOf('[');
            int close = text.LastIndexOf(']');
            return (open >= 0 && close > open) ? text.Substring(open + 1, close - open - 1) : null;
        }

        #endregion

        #region Generate + preview

        private void Generate()
        {
            var names = _checked.ToList();
            if (names.Count == 0)
            {
                MessageBox.Show(this, "Check at least one table to diagram.", "ERD Generator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var service = Service;
            RunAsync("Building ERD from metadata…",
                worker => _collector.Build(service, names, worker, msg => worker.ReportProgress(0, msg)),
                model =>
                {
                    _model = model;
                    tsbExport.Enabled = model.Tables.Count > 0;
                    RefreshPreview();
                    var filtered = Filtered();
                    SetStatusMessage($"ERD built: {filtered.Tables.Count} table(s), {filtered.Relationships.Count} relationship(s).");
                });
        }

        private ErdModel Filtered()
        {
            if (_model == null) return new ErdModel();
            return _model.Apply(new ErdFilter
            {
                CustomOnly = tsbCustomOnly.Checked,
                ManagedOnly = tsbManagedOnly.Checked
            });
        }

        private ColumnDisplay CurrentDisplay()
        {
            switch (cboColumns.SelectedIndex)
            {
                case 1: return ColumnDisplay.Important;
                case 2: return ColumnDisplay.All;
                default: return ColumnDisplay.KeysAndLookupsOnly;
            }
        }

        private void RefreshPreview()
        {
            if (_model == null) return;
            var filtered = Filtered();
            txtPreview.Text = MermaidErdEmitter.Emit(filtered, CurrentDisplay());
            lblStats.Text = $"{filtered.Tables.Count} table(s), {filtered.Relationships.Count} relationship(s). " +
                            (_model.Notes.Count > 0 ? $"{_model.Notes.Count} note(s)." : "");
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_model == null)
            {
                MessageBox.Show(this, "Generate an ERD first.", "ERD Generator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string filter, ext;
            switch (kind)
            {
                case "mermaid": filter = "Mermaid|*.mmd"; ext = "mmd"; break;
                case "plantuml": filter = "PlantUML|*.puml"; ext = "puml"; break;
                case "svg": filter = "SVG|*.svg"; ext = "svg"; break;
                case "png": filter = "PNG|*.png"; ext = "png"; break;
                case "pdf": filter = "PDF|*.pdf"; ext = "pdf"; break;
                case "html": filter = "HTML|*.html"; ext = "html"; break;
                case "markdown": filter = "Markdown|*.md"; ext = "md"; break;
                default: filter = "JSON|*.json"; ext = "json"; break;
            }

            string path;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = $"ERD_{DateTime.Now:yyyyMMdd_HHmm}.{ext}" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.FileName;
            }

            var model = Filtered();
            var display = CurrentDisplay();
            RunAsync($"Exporting {ext.ToUpperInvariant()}…",
                worker =>
                {
                    switch (kind)
                    {
                        case "mermaid": File.WriteAllText(path, MermaidErdEmitter.Emit(model, display)); break;
                        case "plantuml": File.WriteAllText(path, PlantUmlEmitter.Emit(model, display)); break;
                        case "svg": ErdSvg.Export(model, display, path); break;
                        case "png": ErdPngExporter.Export(model, display, path); break;
                        case "pdf": ErdPdfExporter.Export(model, display, path); break;
                        case "html": ErdHtml.Export(model, path); break;
                        case "markdown": ErdMarkdown.Export(model, path); break;
                        default: File.WriteAllText(path, ErdJson.Emit(model)); break;
                    }
                    return path;
                },
                written =>
                {
                    SetStatusMessage($"Exported to {written}");
                    if (MessageBox.Show(this, "ERD exported. Open it now?", "ERD Generator",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        try { System.Diagnostics.Process.Start(written); }
                        catch (Exception ex) { ShowError(ex); }
                    }
                });
        }

        #endregion
    }

    /// <summary>Persisted settings (plain serializable POCO — no controls/services/credentials).</summary>
    public class ErdGeneratorSettings
    {
        public string Scope { get; set; } = "All tables";
        public int ColumnDisplayIndex { get; set; } = 0;
        public bool CustomOnly { get; set; }
        public bool ManagedOnly { get; set; }
    }
}
