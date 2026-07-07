using System;
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
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        private const string PreviewMarkdown = "Markdown";
        private const string PreviewHtml = "HTML source";
        private const string PreviewOpenApi = "OpenAPI JSON";

        private readonly ApiCollector _collector = new ApiCollector();
        private ApiSettings _settings = new ApiSettings();
        private ApiCatalog _catalog;
        private string _environmentName;

        public ApiDocumentationBuilderControl()
        {
            InitializeComponent();

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
            _environmentName = detail?.ConnectionName;
            tsbExport.Enabled = false;
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
                    tsbExport.Enabled = _catalog != null && _catalog.Count > 0;
                    RefreshPreview();
                    SetStatusMessage($"Documented {catalog.Count} Custom API(s).");
                });
        }

        private void RefreshPreview()
        {
            if (_catalog == null) { txtPreview.Clear(); return; }
            var options = CurrentOptions();
            var mode = tscPreview.SelectedItem?.ToString() ?? PreviewMarkdown;
            switch (mode)
            {
                case PreviewHtml: txtPreview.Text = ApiDocEmitters.Html(_catalog, options); break;
                case PreviewOpenApi: txtPreview.Text = OpenApiEmitter.Generate(_catalog, options); break;
                default: txtPreview.Text = ApiDocEmitters.Markdown(_catalog, options); break;
            }
            txtPreview.SelectionStart = 0;
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

            var catalog = _catalog;
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
