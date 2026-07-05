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
using XrmToolSuite.SolutionDocumentationGenerator.Doc;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.SolutionDocumentationGenerator
{
    /// <summary>
    /// Read-only tool that scans a Dataverse solution and generates a multi-section document (inventory,
    /// schema, forms/views, apps, automation, plug-ins, web resources, custom APIs, configuration, roles,
    /// diagrams, release notes, architecture summary) in a chosen documentation mode, previews it, and
    /// exports to Word, PDF, Markdown, HTML, Excel or JSON. Follows the suite patterns (BaseToolControl,
    /// RunAsync, Load/SaveSettings). Ships the full ClosedXML + PdfSharp/MigraDoc-GDI export chain.
    /// </summary>
    public partial class SolutionDocumentationGeneratorControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        private const string PreviewMarkdown = "Markdown";
        private const string PreviewHtml = "HTML source";

        private readonly DocCollector _collector = new DocCollector();
        private SolutionDocGeneratorSettings _settings = new SolutionDocGeneratorSettings();
        private List<DocSolutionInfo> _solutions = new List<DocSolutionInfo>();
        private SolutionDoc _doc; // last generated document

        public SolutionDocumentationGeneratorControl()
        {
            InitializeComponent();

            cboMode.Items.AddRange(new object[] { "Executive Summary", "Standard Reference", "Full Solution Reference" });
            cboMode.SelectedIndex = 1;

            foreach (var kind in SectionKinds.All)
                clbSections.Items.Add(SectionKinds.Title(kind), true);

            tscPreview.Items.AddRange(new object[] { PreviewMarkdown, PreviewHtml });
            tscPreview.SelectedIndex = 0;

            tsbExport.DropDownItems.Add("Word (.docx)", null, (s, e) => Export("word"));
            tsbExport.DropDownItems.Add("PDF (.pdf)", null, (s, e) => Export("pdf"));
            tsbExport.DropDownItems.Add("Excel (.xlsx)", null, (s, e) => Export("excel"));
            tsbExport.DropDownItems.Add("Markdown (.md)", null, (s, e) => Export("markdown"));
            tsbExport.DropDownItems.Add("HTML (.html)", null, (s, e) => Export("html"));
            tsbExport.DropDownItems.Add("JSON (.json)", null, (s, e) => Export("json"));

            tsbLoadSolutions.Click += (s, e) => ExecuteMethod(LoadSolutions);
            tsbGenerate.Click += (s, e) => ExecuteMethod(Generate);
            tsbClose.Click += (s, e) => CloseTool();
            tscPreview.SelectedIndexChanged += (s, e) => RefreshPreview();

            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Solution Documentation Generator"));
        }

        #region Lifecycle

        private void SolutionDocumentationGeneratorControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<SolutionDocGeneratorSettings>();
            ApplySettingsToUi();
            LogInfo("Solution Documentation Generator loaded");
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
            _solutions.Clear();
            cboSolution.Items.Clear();
            _doc = null;
            tsbExport.Enabled = false;
            txtPreview.Clear();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        private void ApplySettingsToUi()
        {
            if (_settings.ModeIndex >= 0 && _settings.ModeIndex < cboMode.Items.Count)
                cboMode.SelectedIndex = _settings.ModeIndex;

            var sections = _settings.Sections ?? DocSections.All();
            for (int i = 0; i < SectionKinds.All.Length && i < clbSections.Items.Count; i++)
                clbSections.SetItemChecked(i, sections.IsEnabled(SectionKinds.All[i]));

            txtBrandHeader.Text = _settings.BrandHeader ?? "";
            txtLogoUrl.Text = _settings.LogoUrl ?? "";
            txtPublisher.Text = _settings.Publisher ?? "";
            if (_settings.PreviewIndex >= 0 && _settings.PreviewIndex < tscPreview.Items.Count)
                tscPreview.SelectedIndex = _settings.PreviewIndex;
        }

        private void CaptureSettingsFromUi()
        {
            _settings.ModeIndex = cboMode.SelectedIndex;
            _settings.Sections = CurrentSections();
            _settings.BrandHeader = txtBrandHeader.Text;
            _settings.LogoUrl = txtLogoUrl.Text;
            _settings.Publisher = txtPublisher.Text;
            _settings.PreviewIndex = tscPreview.SelectedIndex;
        }

        #endregion

        #region Options from UI

        private DocMode CurrentMode()
        {
            switch (cboMode.SelectedIndex)
            {
                case 0: return DocMode.ExecutiveSummary;
                case 2: return DocMode.FullReference;
                default: return DocMode.StandardReference;
            }
        }

        private DocSections CurrentSections()
        {
            var s = new DocSections();
            // Order matches SectionKinds.All.
            var kinds = SectionKinds.All;
            for (int i = 0; i < kinds.Length; i++)
            {
                bool on = i < clbSections.Items.Count && clbSections.GetItemChecked(i);
                Apply(s, kinds[i], on);
            }
            return s;
        }

        private static void Apply(DocSections s, string kind, bool on)
        {
            switch (kind)
            {
                case SectionKinds.Architecture: s.Architecture = on; break;
                case SectionKinds.Inventory: s.Inventory = on; break;
                case SectionKinds.Schema: s.Schema = on; break;
                case SectionKinds.Forms: s.Forms = on; break;
                case SectionKinds.Views: s.Views = on; break;
                case SectionKinds.Apps: s.Apps = on; break;
                case SectionKinds.Automation: s.Automation = on; break;
                case SectionKinds.Plugins: s.Plugins = on; break;
                case SectionKinds.WebResources: s.WebResources = on; break;
                case SectionKinds.CustomApis: s.CustomApis = on; break;
                case SectionKinds.Config: s.Config = on; break;
                case SectionKinds.Roles: s.Roles = on; break;
                case SectionKinds.Diagrams: s.Diagrams = on; break;
                case SectionKinds.ReleaseNotes: s.ReleaseNotes = on; break;
            }
        }

        private DocOptions CurrentOptions() => new DocOptions
        {
            Mode = CurrentMode(),
            Sections = CurrentSections(),
            BrandingHeader = string.IsNullOrWhiteSpace(txtBrandHeader.Text) ? null : txtBrandHeader.Text.Trim(),
            LogoUrl = string.IsNullOrWhiteSpace(txtLogoUrl.Text) ? null : txtLogoUrl.Text.Trim(),
            Publisher = string.IsNullOrWhiteSpace(txtPublisher.Text) ? null : txtPublisher.Text.Trim()
        };

        #endregion

        #region Solutions + generate

        private void LoadSolutions()
        {
            RunAsync("Loading solutions…",
                worker => _collector.ListSolutions(Service),
                solutions =>
                {
                    _solutions = solutions ?? new List<DocSolutionInfo>();
                    cboSolution.Items.Clear();
                    foreach (var s in _solutions)
                        cboSolution.Items.Add($"{s.FriendlyName} ({s.Version}) {(s.IsManaged ? "· managed" : "· unmanaged")}");
                    if (cboSolution.Items.Count > 0) cboSolution.SelectedIndex = 0;
                    SetStatusMessage($"Loaded {_solutions.Count} solution(s). Pick one, set options, then Generate.");
                });
        }

        private void Generate()
        {
            int idx = cboSolution.SelectedIndex;
            if (idx < 0 || idx >= _solutions.Count)
            {
                MessageBox.Show(this, "Load and select a solution first.", "Solution Documentation Generator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var solution = _solutions[idx];
            var options = CurrentOptions();
            var service = Service;

            RunAsync("Scanning solution and building document…",
                worker =>
                {
                    var scan = _collector.Scan(service, solution.Id, options, worker, msg => worker.ReportProgress(0, msg));
                    return DocBuilder.Build(scan, options);
                },
                doc =>
                {
                    _doc = doc;
                    tsbExport.Enabled = _doc != null && _doc.Sections.Count > 0;
                    RefreshPreview();
                    SetStatusMessage($"Documented '{doc.SolutionName ?? solution.FriendlyName}': {doc.Sections.Count} section(s).");
                });
        }

        private void RefreshPreview()
        {
            if (_doc == null) { txtPreview.Clear(); return; }
            var mode = tscPreview.SelectedItem?.ToString() ?? PreviewMarkdown;
            txtPreview.Text = mode == PreviewHtml
                ? DocRenderers.Html(_doc)
                : DocRenderers.Markdown(_doc);
            txtPreview.SelectionStart = 0;
            lblStats.Text = $"{_doc.Sections.Count} section(s) · mode: {_doc.ModeLabel} · previewing {mode}.";
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_doc == null)
            {
                MessageBox.Show(this, "Generate a document first.", "Solution Documentation Generator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string filter, ext;
            switch (kind)
            {
                case "word": filter = "Word document|*.docx"; ext = "docx"; break;
                case "pdf": filter = "PDF|*.pdf"; ext = "pdf"; break;
                case "excel": filter = "Excel workbook|*.xlsx"; ext = "xlsx"; break;
                case "markdown": filter = "Markdown|*.md"; ext = "md"; break;
                case "html": filter = "HTML|*.html"; ext = "html"; break;
                default: filter = "JSON|*.json"; ext = "json"; break;
            }

            var baseName = SafeFileName(_doc.UniqueName ?? _doc.SolutionName ?? "Solution");
            string path;
            using (var dlg = new SaveFileDialog
            {
                Filter = filter,
                FileName = $"{baseName}_documentation_{DateTime.Now:yyyyMMdd_HHmm}.{ext}"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.FileName;
            }

            var doc = _doc;
            RunAsync($"Exporting {ext.ToUpperInvariant()}…",
                worker =>
                {
                    switch (kind)
                    {
                        case "word": DocWordExporter.Export(doc, path); break;
                        case "pdf": DocPdfExporter.Export(doc, path); break;
                        case "excel": DocExcelExporter.Export(doc, path); break;
                        case "markdown": File.WriteAllText(path, DocRenderers.Markdown(doc)); break;
                        case "html": File.WriteAllText(path, DocRenderers.Html(doc)); break;
                        default: File.WriteAllText(path, DocRenderers.Json(doc)); break;
                    }
                    return path;
                },
                written =>
                {
                    SetStatusMessage($"Exported to {written}");
                    if (MessageBox.Show(this, "Document exported. Open it now?", "Solution Documentation Generator",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        try { System.Diagnostics.Process.Start(written); }
                        catch (Exception ex) { ShowError(ex); }
                    }
                });
        }

        private static string SafeFileName(string name)
        {
            name = name ?? "Solution";
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        #endregion
    }

    /// <summary>Persisted settings (plain serializable POCO — no controls/services/credentials).</summary>
    public class SolutionDocGeneratorSettings
    {
        public int ModeIndex { get; set; } = 1;
        public DocSections Sections { get; set; } = new DocSections();
        public string BrandHeader { get; set; }
        public string LogoUrl { get; set; }
        public string Publisher { get; set; }
        public int PreviewIndex { get; set; }
    }
}
