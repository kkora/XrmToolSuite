using System;
using System.Collections.Generic;
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
using XrmToolSuite.DuplicateMetadataFinder.Analysis;
using XrmToolSuite.DuplicateMetadataFinder.Reporting;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.DuplicateMetadataFinder
{
    public partial class DuplicateMetadataFinderControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private DuplicateFinderSettings _settings;
        private DuplicateScanResult _result;

        // Suite GitHub identity — powers the Help dialog's "Report a bug" / documentation links.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        // One checkable menu item per component kind (all on by default).
        private readonly Dictionary<ComponentKind, ToolStripMenuItem> _kindItems =
            new Dictionary<ComponentKind, ToolStripMenuItem>();

        public DuplicateMetadataFinderControl()
        {
            InitializeComponent();
            BuildKindMenu();
            toolStrip.Items.Add(CreateHelpButton("Duplicate Metadata Finder"));
        }

        private void BuildKindMenu()
        {
            foreach (var kind in new[]
            {
                ComponentKind.Column, ComponentKind.OptionSet, ComponentKind.Table, ComponentKind.Form,
                ComponentKind.View, ComponentKind.BusinessRule, ComponentKind.WebResource,
                ComponentKind.PluginStep, ComponentKind.Relationship
            })
            {
                var item = new ToolStripMenuItem(Humanize(kind))
                {
                    Checked = true,
                    CheckOnClick = true,
                    CheckState = CheckState.Checked
                };
                _kindItems[kind] = item;
                tsddKinds.DropDownItems.Add(item);
            }
        }

        private void DuplicateMetadataFinderControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<DuplicateFinderSettings>();
            ApplyOptionsToUi(_settings.Options ?? DuplicateScanOptions.All());
            LogInfo("Duplicate Metadata Finder loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.Options = ReadOptionsFromUi();
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        // ExecuteMethod ensures a connection exists (prompts to connect if not).
        private void tsbScan_Click(object sender, EventArgs e) => ExecuteMethod(ScanForDuplicates);

        private void ScanForDuplicates()
        {
            var options = ReadOptionsFromUi();
            RunAsync(
                "Scanning metadata for duplicates…",
                worker =>
                {
                    var collected = DuplicateCollector.Collect(Service, options, worker,
                        msg => worker.ReportProgress(0, msg));
                    worker.ReportProgress(0, "Scoring and grouping…");
                    var scan = SimilarityEngine.Group(collected.Components, options.Threshold);
                    scan.Notes.AddRange(collected.Notes);
                    scan.EnvironmentName = ConnectionDetail?.ConnectionName;
                    return scan;
                },
                scan =>
                {
                    _result = scan;
                    PopulateGroups();
                    tsddExport.Enabled = scan.GroupCount > 0;
                    var noteSuffix = scan.Notes.Count > 0 ? $" ({scan.Notes.Count} source(s) skipped)" : "";
                    SetStatusMessage(
                        $"{scan.GroupCount} duplicate group(s), {scan.DuplicateComponentCount} component(s) " +
                        $"at ≥{scan.Threshold}%{noteSuffix}");
                });
        }

        private void PopulateGroups()
        {
            grdGroups.SuspendLayout();
            grdGroups.Rows.Clear();
            txtDetail.Clear();
            if (_result != null)
            {
                foreach (var g in _result.Ranked())
                {
                    var idx = grdGroups.Rows.Add(
                        Humanize(g.Kind),
                        string.Join(", ", g.Members.Select(m => m.Key)),
                        g.TopScore.ToString(),
                        g.RecommendedPrimary?.Key ?? "");
                    grdGroups.Rows[idx].Tag = g;
                }
            }
            grdGroups.ClearSelection();
            grdGroups.ResumeLayout();
        }

        private void grdGroups_SelectionChanged(object sender, EventArgs e)
        {
            if (grdGroups.SelectedRows.Count == 0 || !(grdGroups.SelectedRows[0].Tag is DuplicateGroup g))
            {
                txtDetail.Clear();
                return;
            }
            txtDetail.Text = DescribeGroup(g);
        }

        private static string DescribeGroup(DuplicateGroup g)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Humanize(g.Kind)} duplicate group — top similarity {g.TopScore}%");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine("Members:");
            foreach (var m in g.Members)
            {
                sb.AppendLine($"  • {m.Key}");
                sb.AppendLine($"      display : {m.DisplayName}");
                if (!string.IsNullOrEmpty(m.SchemaName)) sb.AppendLine($"      schema  : {m.SchemaName}");
                if (!string.IsNullOrEmpty(m.DataType)) sb.AppendLine($"      type    : {m.DataType}");
                sb.AppendLine($"      status  : {(m.IsManaged ? "managed" : "unmanaged")}" +
                              $"{(m.UsageCount > 0 ? $", {m.UsageCount} usage(s)" : "")}");
            }
            sb.AppendLine();
            sb.AppendLine("Scored pairs:");
            foreach (var p in g.Pairs.OrderByDescending(x => x.Score))
            {
                sb.AppendLine($"  {p.Score,3}%  {p.A.Key}  ~  {p.B.Key}" + (p.IsExactContentMatch ? "  [exact content]" : ""));
                if (p.Factors.Count > 0)
                    sb.AppendLine($"        factors: {string.Join(", ", p.Factors.Select(f => f.ToString()))}");
            }
            sb.AppendLine();
            sb.AppendLine("Recommendation:");
            sb.AppendLine($"  {g.RecommendationReason()}");
            sb.AppendLine();
            sb.AppendLine("Read-only: the tool recommends a primary to keep. It never merges or deletes.");
            return sb.ToString();
        }

        // ---- exports (US-ADMIN3.5.1) ----

        private void tsmiExportExcel_Click(object sender, EventArgs e) =>
            ExportFile("Excel workbook (*.xlsx)|*.xlsx", "duplicate-metadata.xlsx",
                path => ExcelReportExporter.Export(DuplicateReport.ToReportModel(_result), path));

        private void tsmiExportPdf_Click(object sender, EventArgs e) =>
            ExportFile("PDF document (*.pdf)|*.pdf", "duplicate-metadata.pdf",
                path => PdfReportExporter.Export(DuplicateReport.ToReportModel(_result), path));

        private void tsmiExportJson_Click(object sender, EventArgs e) =>
            ExportFile("JSON file (*.json)|*.json", "duplicate-metadata.json",
                path => JsonReportExporter.Export(DuplicateReport.ToReportModel(_result), path));

        private void tsmiExportHtml_Click(object sender, EventArgs e) =>
            ExportFile("HTML report (*.html)|*.html", "duplicate-metadata.html",
                path => HtmlDashboardBuilder.Export(DuplicateReport.ToReportModel(_result), path));

        // The writer delegate owns the path so ClosedXML/PdfSharp/MigraDoc types stay out of any signature.
        private void ExportFile(string filter, string defaultName, Action<string> write)
        {
            if (_result == null || _result.GroupCount == 0) return;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    write(dlg.FileName);
                    SetStatusMessage($"Exported {_result.GroupCount} group(s) to {Path.GetFileName(dlg.FileName)}");
                    PromptOpenExportedFile(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex, "Export failed"); }
            }
        }

        // ---- options <-> UI ----

        private DuplicateScanOptions ReadOptionsFromUi()
        {
            var o = new DuplicateScanOptions
            {
                Columns = _kindItems[ComponentKind.Column].Checked,
                OptionSets = _kindItems[ComponentKind.OptionSet].Checked,
                Tables = _kindItems[ComponentKind.Table].Checked,
                Forms = _kindItems[ComponentKind.Form].Checked,
                Views = _kindItems[ComponentKind.View].Checked,
                BusinessRules = _kindItems[ComponentKind.BusinessRule].Checked,
                WebResources = _kindItems[ComponentKind.WebResource].Checked,
                PluginSteps = _kindItems[ComponentKind.PluginStep].Checked,
                Relationships = _kindItems[ComponentKind.Relationship].Checked,
                CustomOnly = tsbCustomOnly.Checked,
                Threshold = ParseThreshold(tstThreshold.Text),
            };
            return o;
        }

        private void ApplyOptionsToUi(DuplicateScanOptions o)
        {
            _kindItems[ComponentKind.Column].Checked = o.Columns;
            _kindItems[ComponentKind.OptionSet].Checked = o.OptionSets;
            _kindItems[ComponentKind.Table].Checked = o.Tables;
            _kindItems[ComponentKind.Form].Checked = o.Forms;
            _kindItems[ComponentKind.View].Checked = o.Views;
            _kindItems[ComponentKind.BusinessRule].Checked = o.BusinessRules;
            _kindItems[ComponentKind.WebResource].Checked = o.WebResources;
            _kindItems[ComponentKind.PluginStep].Checked = o.PluginSteps;
            _kindItems[ComponentKind.Relationship].Checked = o.Relationships;
            tsbCustomOnly.Checked = o.CustomOnly;
            tstThreshold.Text = o.Threshold.ToString();
        }

        private static int ParseThreshold(string text)
        {
            if (!int.TryParse((text ?? "").Trim(), out var v)) return 80;
            return v < 0 ? 0 : v > 100 ? 100 : v;
        }

        private static string Humanize(ComponentKind kind)
        {
            switch (kind)
            {
                case ComponentKind.OptionSet: return "Option Set";
                case ComponentKind.BusinessRule: return "Business Rule";
                case ComponentKind.WebResource: return "Web Resource";
                case ComponentKind.PluginStep: return "Plugin Step";
                default: return kind.ToString();
            }
        }
    }

    /// <summary>Plain serializable settings POCO — scan options only; no controls, services or credentials.</summary>
    public class DuplicateFinderSettings
    {
        public DuplicateScanOptions Options { get; set; } = DuplicateScanOptions.All();
    }
}
