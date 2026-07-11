using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.ApiDocumentationBuilder.Api;
using XrmToolSuite.Core;
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.ApiDocumentationBuilder
{
    /// <summary>
    /// Read-only tool that documents a Dataverse environment's Custom APIs (parameters, responses, binding,
    /// backing plugin) as a redaction-safe reference and a best-effort OpenAPI-style JSON spec, previews it,
    /// and exports to Markdown, self-contained theme-aware HTML, raw JSON, or OpenAPI JSON. Never invokes an
    /// API; secret-named values are masked (with user-controlled extra redaction terms). Follows the suite
    /// patterns (BaseToolControl, RunAsync, Load/SaveSettings). BCL-only — no export dependency chain.
    /// </summary>
    public partial class ApiDocumentationBuilderControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        private const string PreviewMarkdown = "Markdown";
        private const string PreviewHtml = "HTML source";
        private const string PreviewOpenApi = "OpenAPI JSON";

        private readonly ApiCollector _collector = new ApiCollector();
        private ApiSettings _settings = new ApiSettings();
        private ApiCatalog _catalog;
        private string _environmentName;

        // Left-panel API picker: the full list, and the set of unique names currently checked.
        private CheckedListBox _clbApis;
        private TextBox _txtApiSearch;
        private CheckBox _chkSelectAll;
        private readonly HashSet<string> _checkedApis = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<ApiDoc> _allApis = new List<ApiDoc>();
        private readonly List<ApiDoc> _visible = new List<ApiDoc>(); // APIs currently shown (after the search filter)
        private bool _suppressCheck;

        private static string Key(ApiDoc a) => a?.UniqueName ?? a?.DisplayName ?? "";

        public ApiDocumentationBuilderControl()
        {
            InitializeComponent();
            BuildApiPanel();

            tscPreview.Items.AddRange(new object[] { PreviewMarkdown, PreviewHtml, PreviewOpenApi });
            tscPreview.SelectedIndex = 0;

            tsbExport.DropDownItems.Add("Markdown (.md)", null, (s, e) => Export("markdown"));
            tsbExport.DropDownItems.Add("HTML (self-contained) (.html)", null, (s, e) => Export("html"));
            tsbExport.DropDownItems.Add("JSON — raw model (.json)", null, (s, e) => Export("json"));
            tsbExport.DropDownItems.Add("OpenAPI-style spec (.json)", null, (s, e) => Export("openapi"));

            tsbLoad.Click += (s, e) => ExecuteMethod(LoadApis);
            tsbIncludeExamples.CheckedChanged += (s, e) => RefreshPreview();
            tstRedact.TextChanged += (s, e) => RefreshPreview();
            tscPreview.SelectedIndexChanged += (s, e) => RefreshPreview();

            // Suite convention: right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton());
        }

        // ------------------------------------------------------------- Left panel: select which APIs to document

        private void BuildApiPanel()
        {
            _clbApis = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
            _clbApis.ItemCheck += ClbApis_ItemCheck;

            _chkSelectAll = new CheckBox { Dock = DockStyle.Top, Text = "Select all", Checked = false, Height = 24, Padding = new Padding(4, 2, 0, 2) };
            _chkSelectAll.CheckedChanged += ChkSelectAll_CheckedChanged;

            _txtApiSearch = new TextBox { Dock = DockStyle.Top };
            _txtApiSearch.TextChanged += (s, e) => PopulateApiList();

            var header = new System.Windows.Forms.Label { Dock = DockStyle.Top, Text = "Custom APIs — search & select", Height = 22, Font = new Font(Font, FontStyle.Bold), Padding = new Padding(4, 4, 0, 0) };

            var panel = new Panel { Dock = DockStyle.Left, Width = 300 };
            panel.Controls.Add(_clbApis);      // fill
            panel.Controls.Add(_chkSelectAll); // top
            panel.Controls.Add(_txtApiSearch); // top
            panel.Controls.Add(header);        // top (rendered topmost)

            var splitter = new Splitter { Dock = DockStyle.Left, Width = 4, MinSize = 180 };

            Controls.Add(panel);
            Controls.Add(splitter);
            // Dock resolves highest child index first: keep toolStrip top-full-width, then panel|splitter left,
            // then txtPreview fills the rest.
            Controls.SetChildIndex(toolStrip, 3);
            Controls.SetChildIndex(panel, 2);
            Controls.SetChildIndex(splitter, 1);
            Controls.SetChildIndex(txtPreview, 0);
        }

        private sealed class ApiListItem
        {
            public ApiDoc Api;
            public override string ToString() => Api?.DisplayName ?? Api?.UniqueName ?? "(unnamed)";
        }

        /// <summary>Rebuilds the visible list from the current search term, restoring each item's checked state.</summary>
        private void PopulateApiList()
        {
            if (_clbApis == null) return;
            var term = (_txtApiSearch.Text ?? "").Trim();
            _visible.Clear();
            _suppressCheck = true;
            _clbApis.BeginUpdate();
            _clbApis.Items.Clear();
            foreach (var api in _allApis)
            {
                var label = api.DisplayName ?? "";
                var uname = api.UniqueName ?? "";
                if (term.Length > 0
                    && label.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0
                    && uname.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _visible.Add(api);
                int i = _clbApis.Items.Add(new ApiListItem { Api = api });
                _clbApis.SetItemChecked(i, _checkedApis.Contains(Key(api)));
            }
            _clbApis.EndUpdate();
            _suppressCheck = false;
            SyncSelectAllLabel();
        }

        private void ClbApis_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_suppressCheck) return;
            if (!(_clbApis.Items[e.Index] is ApiListItem item) || item.Api == null) return;
            var key = Key(item.Api);
            if (e.NewValue == CheckState.Checked) _checkedApis.Add(key); else _checkedApis.Remove(key);
            // ItemCheck fires BEFORE the state is applied; refresh after it settles.
            BeginInvoke((Action)(() => { SyncSelectAllLabel(); RefreshFromSelection(); }));
        }

        // Select-all is scoped to the currently FILTERED list: it checks/unchecks only the visible APIs and
        // leaves anything filtered out as-is.
        private void ChkSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressCheck) return;
            foreach (var a in _visible)
                if (_chkSelectAll.Checked) _checkedApis.Add(Key(a)); else _checkedApis.Remove(Key(a));
            PopulateApiList();
            RefreshFromSelection();
        }

        private void SyncSelectAllLabel()
        {
            int visibleChecked = _visible.Count(a => _checkedApis.Contains(Key(a)));
            _suppressCheck = true;
            _chkSelectAll.Checked = _visible.Count > 0 && visibleChecked == _visible.Count;
            _chkSelectAll.Text = _visible.Count == _allApis.Count
                ? $"Select all ({_checkedApis.Count}/{_allApis.Count})"
                : $"Select all filtered ({visibleChecked}/{_visible.Count})"; // count reflects the search
            _suppressCheck = false;
        }

        private void RefreshFromSelection()
        {
            tsbExport.Enabled = _checkedApis.Count > 0;
            RefreshPreview();
        }

        /// <summary>A catalog containing only the checked APIs — what preview and export operate on.</summary>
        private ApiCatalog SelectedCatalog()
        {
            if (_catalog == null) return null;
            var c = new ApiCatalog { EnvironmentName = _catalog.EnvironmentName, GeneratedUtc = _catalog.GeneratedUtc };
            c.Notes.AddRange(_catalog.Notes);
            foreach (var a in _catalog.OrderedApis)
                if (_checkedApis.Contains(Key(a))) c.Apis.Add(a);
            return c;
        }

        private void ApiDocumentationBuilderControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ApiSettings>() ?? new ApiSettings();
            tsbIncludeExamples.Checked = _settings.IncludeExamples;
            tstRedact.Text = _settings.RedactTerms ?? "";
            if (_settings.PreviewIndex >= 0 && _settings.PreviewIndex < tscPreview.Items.Count)
                tscPreview.SelectedIndex = _settings.PreviewIndex;
            LogInfo("API Documentation Builder loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.IncludeExamples = tsbIncludeExamples.Checked;
            _settings.RedactTerms = tstRedact.Text;
            _settings.PreviewIndex = tscPreview.SelectedIndex;
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail,
            string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear();
            _catalog = null;
            _allApis = new List<ApiDoc>();
            _checkedApis.Clear();
            PopulateApiList();
            _environmentName = detail?.ConnectionName;
            tsbExport.Enabled = false;
            tsbOpenBrowser.Enabled = false;
            txtPreview.Clear();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        private ApiDocOptions CurrentOptions()
        {
            var terms = (tstRedact.Text ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();
            return new ApiDocOptions { IncludeExamples = tsbIncludeExamples.Checked, AdditionalRedactTerms = terms };
        }

        #region Load

        private void LoadApis()
        {
            var service = Service;
            var env = _environmentName;
            RunAsync("Documenting Custom APIs…",
                worker => _collector.Build(service, env, msg => worker.ReportProgress(0, msg)),
                catalog =>
                {
                    _catalog = catalog;
                    _allApis = _catalog.OrderedApis.ToList();
                    // Start with NOTHING selected so "check 5, export 5" is unambiguous (search only hides
                    // rows — it never unselects them, so an all-checked default would silently export hidden APIs).
                    _checkedApis.Clear();
                    PopulateApiList();
                    RefreshFromSelection();
                    SetStatusMessage($"Documented {catalog.Count} Custom API(s) — check the ones to include (or Select all).");
                });
        }

        private void RefreshPreview()
        {
            var catalog = SelectedCatalog();
            if (catalog == null || catalog.Count == 0)
            {
                txtPreview.Text = _catalog == null
                    ? ""
                    : "Check one or more Custom APIs in the left panel to preview and export them.";
                UpdateOpenBrowserState();
                return;
            }
            var options = CurrentOptions();
            var mode = tscPreview.SelectedItem?.ToString() ?? PreviewMarkdown;
            switch (mode)
            {
                // Preview panes pretty-print for readability; exports write the original emitter output.
                case PreviewHtml: txtPreview.Text = HtmlFormat.Pretty(ApiDocEmitters.Html(catalog, options)); break;
                case PreviewOpenApi: txtPreview.Text = JsonFormat.Pretty(OpenApiEmitter.Generate(catalog, options)); break;
                default: txtPreview.Text = ApiDocEmitters.Markdown(catalog, options); break;
            }
            txtPreview.SelectionStart = 0;
            UpdateOpenBrowserState();
        }

        // "Open in browser" renders the real HTML — only meaningful in the HTML source preview mode with a selection.
        private void UpdateOpenBrowserState() =>
            tsbOpenBrowser.Enabled = _checkedApis.Count > 0 && PreviewHtml.Equals(tscPreview.SelectedItem?.ToString());

        private void tsbOpenBrowser_Click(object sender, EventArgs e)
        {
            var catalog = SelectedCatalog();
            if (catalog == null || catalog.Count == 0) return;
            OpenHtmlInBrowser(ApiDocEmitters.Html(catalog, CurrentOptions()), "custom-api-reference");
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_catalog == null)
            {
                MessageBox.Show(this, "Load Custom APIs first.", "API Documentation Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var selected = SelectedCatalog();
            if (selected == null || selected.Count == 0)
            {
                MessageBox.Show(this, "Select at least one Custom API to export.", "API Documentation Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string filter, ext, suffix;
            switch (kind)
            {
                case "markdown": filter = "Markdown|*.md"; ext = "md"; suffix = "api-reference"; break;
                case "html": filter = "HTML|*.html"; ext = "html"; suffix = "api-reference"; break;
                case "openapi": filter = "OpenAPI JSON|*.json"; ext = "json"; suffix = "openapi"; break;
                default: filter = "JSON|*.json"; ext = "json"; suffix = "api-model"; break;
            }

            var baseName = SafeFileName(_catalog.EnvironmentName ?? "Environment");
            string path;
            using (var dlg = new SaveFileDialog
            {
                Filter = filter,
                FileName = $"{baseName}_{suffix}_{DateTime.Now:yyyyMMdd_HHmm}.{ext}"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.FileName;
            }

            var catalog = selected; // export only the checked APIs
            var options = CurrentOptions();
            RunAsync($"Exporting {ext.ToUpperInvariant()}…",
                worker =>
                {
                    switch (kind)
                    {
                        case "markdown": File.WriteAllText(path, ApiDocEmitters.Markdown(catalog, options)); break;
                        case "html": File.WriteAllText(path, ApiDocEmitters.Html(catalog, options)); break;
                        case "openapi": File.WriteAllText(path, OpenApiEmitter.Generate(catalog, options)); break;
                        default: File.WriteAllText(path, ApiDocEmitters.Json(catalog, options)); break;
                    }
                    return path;
                },
                written =>
                {
                    SetStatusMessage($"Exported to {written}");
                    if (MessageBox.Show(this, "Documentation exported. Open it now?", "API Documentation Builder",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        try { System.Diagnostics.Process.Start(written); }
                        catch (Exception ex) { ShowError(ex); }
                    }
                });
        }

        private static string SafeFileName(string name)
        {
            name = name ?? "Environment";
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        #endregion
    }

    /// <summary>Persisted UI preferences (plain serializable POCO — never carries credentials).</summary>
    public class ApiSettings
    {
        public bool IncludeExamples { get; set; } = true;
        public string RedactTerms { get; set; }
        public int PreviewIndex { get; set; }
    }
}
