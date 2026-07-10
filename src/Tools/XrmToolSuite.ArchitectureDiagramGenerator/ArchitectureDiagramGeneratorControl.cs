using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.ArchitectureDiagramGenerator.Diagram;
using XrmToolSuite.Core;
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.ArchitectureDiagramGenerator
{
    /// <summary>
    /// Read-only tool that turns a Dataverse solution's components + platform dependencies into an
    /// architecture diagram (components classified into layers), previews it, and exports to Mermaid,
    /// PlantUML, DOT/Graphviz, Markdown, a self-contained theme-aware HTML page (hand-laid-out inline
    /// SVG, offline), or JSON. Reuses the same extraction the Solution Knowledge Graph uses. Follows the
    /// suite patterns (BaseToolControl, RunAsync, Load/SaveSettings). BCL-only — no export dependency chain.
    /// </summary>
    public partial class ArchitectureDiagramGeneratorControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        private const string PreviewMermaid = "Mermaid";
        private const string PreviewPlantUml = "PlantUML";
        private const string PreviewDot = "DOT";
        private const string PreviewHtml = "HTML source";

        private const string LayoutLayered = "Layered (by layer)";
        private const string LayoutDependency = "Dependency graph";
        private const string DirLeftRight = "Left → right";
        private const string DirTopDown = "Top → down";

        private readonly ArchCollector _collector = new ArchCollector();
        private ArchSettings _settings = new ArchSettings();
        private List<DiagramSolutionInfo> _solutions = new List<DiagramSolutionInfo>();
        private ArchDiagram _diagram;

        public ArchitectureDiagramGeneratorControl()
        {
            InitializeComponent();

            tscLayout.Items.AddRange(new object[] { LayoutLayered, LayoutDependency });
            tscLayout.SelectedIndex = 0;
            tscDirection.Items.AddRange(new object[] { DirLeftRight, DirTopDown });
            tscDirection.SelectedIndex = 0;
            tscPreview.Items.AddRange(new object[] { PreviewMermaid, PreviewPlantUml, PreviewDot, PreviewHtml });
            tscPreview.SelectedIndex = 0;

            tsbExport.DropDownItems.Add("Mermaid (.mmd)", null, (s, e) => Export("mermaid"));
            tsbExport.DropDownItems.Add("PlantUML (.puml)", null, (s, e) => Export("plantuml"));
            tsbExport.DropDownItems.Add("DOT / Graphviz (.dot)", null, (s, e) => Export("dot"));
            tsbExport.DropDownItems.Add("Markdown (.md)", null, (s, e) => Export("markdown"));
            tsbExport.DropDownItems.Add("HTML (self-contained) (.html)", null, (s, e) => Export("html"));
            tsbExport.DropDownItems.Add("JSON (.json)", null, (s, e) => Export("json"));

            tsbLoadSolutions.Click += (s, e) => ExecuteMethod(LoadSolutions);
            tsbGenerate.Click += (s, e) => ExecuteMethod(Generate);
            tscLayout.SelectedIndexChanged += (s, e) => RefreshPreview();
            tscDirection.SelectedIndexChanged += (s, e) => RefreshPreview();
            tsbHideOrphans.CheckedChanged += (s, e) => RefreshPreview();
            tscPreview.SelectedIndexChanged += (s, e) => RefreshPreview();

            // Suite convention: right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton());
        }

        private void ArchitectureDiagramGeneratorControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ArchSettings>() ?? new ArchSettings();
            if (_settings.LayoutIndex >= 0 && _settings.LayoutIndex < tscLayout.Items.Count)
                tscLayout.SelectedIndex = _settings.LayoutIndex;
            if (_settings.DirectionIndex >= 0 && _settings.DirectionIndex < tscDirection.Items.Count)
                tscDirection.SelectedIndex = _settings.DirectionIndex;
            if (_settings.PreviewIndex >= 0 && _settings.PreviewIndex < tscPreview.Items.Count)
                tscPreview.SelectedIndex = _settings.PreviewIndex;
            tsbHideOrphans.Checked = _settings.HideOrphans;
            LogInfo("Architecture Diagram Generator loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.LayoutIndex = tscLayout.SelectedIndex;
            _settings.DirectionIndex = tscDirection.SelectedIndex;
            _settings.PreviewIndex = tscPreview.SelectedIndex;
            _settings.HideOrphans = tsbHideOrphans.Checked;
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail,
            string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear();
            _solutions.Clear();
            tscSolution.Items.Clear();
            _diagram = null;
            tsbExport.Enabled = false;
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        #region Load solutions

        private void LoadSolutions()
        {
            RunAsync("Loading solutions…",
                worker =>
                {
                    var qe = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("solutionid", "uniquename", "friendlyname", "version", "ismanaged"),
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                                new ConditionExpression("uniquename", ConditionOperator.NotIn, "Default", "Active", "Basic")
                            }
                        },
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };
                    var pub = qe.AddLink("publisher", "publisherid", "publisherid", JoinOperator.LeftOuter);
                    pub.EntityAlias = "pub";
                    pub.Columns = new ColumnSet("friendlyname");

                    return Service.RetrieveAll(qe).Select(s => new DiagramSolutionInfo
                    {
                        Id = s.Id,
                        UniqueName = s.GetAttributeValue<string>("uniquename"),
                        FriendlyName = s.GetAttributeValue<string>("friendlyname"),
                        Version = s.GetAttributeValue<string>("version"),
                        IsManaged = s.GetAttributeValue<bool>("ismanaged"),
                        Publisher = s.GetAttributeValue<AliasedValue>("pub.friendlyname")?.Value as string
                    }).ToList();
                },
                solutions =>
                {
                    _solutions = solutions;
                    tscSolution.Items.Clear();
                    foreach (var s in _solutions)
                        tscSolution.Items.Add(s.ToString());
                    if (tscSolution.Items.Count > 0) tscSolution.SelectedIndex = 0;
                    SetStatusMessage($"{_solutions.Count} solution(s) loaded.");
                });
        }

        #endregion

        #region Generate

        private void Generate()
        {
            int idx = tscSolution.SelectedIndex;
            if (idx < 0 || idx >= _solutions.Count)
            {
                MessageBox.Show(this, "Load and select a solution first.", "Architecture Diagram Generator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var info = _solutions[idx];
            var service = Service;
            RunAsync("Scanning solution and building diagram…",
                worker => _collector.Build(service, info.Id, info, msg => worker.ReportProgress(0, msg)),
                diagram =>
                {
                    _diagram = diagram;
                    tsbExport.Enabled = _diagram != null && _diagram.Nodes.Count > 0;
                    RefreshPreview();
                    SetStatusMessage($"Diagram for '{diagram.SolutionName ?? info.FriendlyName}': " +
                                     $"{diagram.Nodes.Count} node(s), {diagram.Edges.Count} edge(s).");
                });
        }

        private DiagramOptions CurrentOptions() => new DiagramOptions
        {
            Layout = tscLayout.SelectedItem?.ToString() == LayoutDependency
                ? DiagramLayout.DependencyGraph : DiagramLayout.Layered,
            Direction = tscDirection.SelectedItem?.ToString() == DirTopDown
                ? DiagramDirection.TopToBottom : DiagramDirection.LeftToRight,
            HideOrphans = tsbHideOrphans.Checked
        };

        private void RefreshPreview()
        {
            if (_diagram == null) { txtPreview.Clear(); return; }
            var options = CurrentOptions();
            var mode = tscPreview.SelectedItem?.ToString() ?? PreviewMermaid;
            switch (mode)
            {
                case PreviewPlantUml: txtPreview.Text = DiagramEmitters.PlantUml(_diagram, options); break;
                case PreviewDot: txtPreview.Text = DiagramEmitters.Dot(_diagram, options); break;
                case PreviewHtml: txtPreview.Text = DiagramEmitters.Html(_diagram, options); break;
                default: txtPreview.Text = DiagramEmitters.Mermaid(_diagram, options); break;
            }
            txtPreview.SelectionStart = 0;
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_diagram == null)
            {
                MessageBox.Show(this, "Generate a diagram first.", "Architecture Diagram Generator",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string filter, ext;
            switch (kind)
            {
                case "mermaid": filter = "Mermaid|*.mmd"; ext = "mmd"; break;
                case "plantuml": filter = "PlantUML|*.puml"; ext = "puml"; break;
                case "dot": filter = "DOT / Graphviz|*.dot"; ext = "dot"; break;
                case "markdown": filter = "Markdown|*.md"; ext = "md"; break;
                case "html": filter = "HTML|*.html"; ext = "html"; break;
                default: filter = "JSON|*.json"; ext = "json"; break;
            }

            var baseName = SafeFileName(_diagram.UniqueName ?? _diagram.SolutionName ?? "Solution");
            string path;
            using (var dlg = new SaveFileDialog
            {
                Filter = filter,
                FileName = $"{baseName}_architecture_{DateTime.Now:yyyyMMdd_HHmm}.{ext}"
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.FileName;
            }

            var diagram = _diagram;
            var options = CurrentOptions();
            RunAsync($"Exporting {ext.ToUpperInvariant()}…",
                worker =>
                {
                    switch (kind)
                    {
                        case "mermaid": File.WriteAllText(path, DiagramEmitters.Mermaid(diagram, options)); break;
                        case "plantuml": File.WriteAllText(path, DiagramEmitters.PlantUml(diagram, options)); break;
                        case "dot": File.WriteAllText(path, DiagramEmitters.Dot(diagram, options)); break;
                        case "markdown": File.WriteAllText(path, DiagramEmitters.Markdown(diagram, options)); break;
                        case "html": File.WriteAllText(path, DiagramEmitters.Html(diagram, options)); break;
                        default: File.WriteAllText(path, DiagramEmitters.Json(diagram, options)); break;
                    }
                    return path;
                },
                written =>
                {
                    SetStatusMessage($"Exported to {written}");
                    if (MessageBox.Show(this, "Diagram exported. Open it now?", "Architecture Diagram Generator",
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
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        #endregion
    }

    /// <summary>Persisted UI preferences (plain serializable POCO — never carries credentials).</summary>
    public class ArchSettings
    {
        public int LayoutIndex { get; set; }
        public int DirectionIndex { get; set; }
        public int PreviewIndex { get; set; }
        public bool HideOrphans { get; set; }
    }
}
