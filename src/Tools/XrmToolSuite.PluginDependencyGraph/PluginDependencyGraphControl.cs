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
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.PluginDependencyGraph.Graph;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.PluginDependencyGraph
{
    /// <summary>
    /// Read-only dependency graph of Dataverse plugin registrations: assembly → type → step → image, and
    /// step → table/message/config, custom API → type, solution → member. Loads the pipeline off the UI
    /// thread, filters/isolates without re-querying, flags high-impact/duplicate/unmanaged risks, and
    /// exports to PNG/SVG/PDF/Excel/JSON/GraphML/HTML. Follows the suite patterns (BaseToolControl,
    /// RunAsync, Load/SaveSettings, shared exporters). Never renders or exports secure-config values.
    /// </summary>
    public partial class PluginDependencyGraphControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private const string All = "(all)";

        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        private readonly PluginCollector _collector = new PluginCollector();
        private ToolSettings _settings = new ToolSettings();

        private PluginRegistrationData _data;
        private PluginGraph _fullGraph;
        private PluginGraph _viewGraph;                 // last graph shown (filtered/isolated)
        private List<Finding> _findings = new List<Finding>();
        private readonly Dictionary<string, string> _focusIds = new Dictionary<string, string>(); // combo label → node id
        private bool _suppress;                         // suppress combo events during repopulation

        public PluginDependencyGraphControl()
        {
            InitializeComponent();

            tsbExport.DropDownItems.Add("PNG (.png)", null, (s, e) => Export("png"));
            tsbExport.DropDownItems.Add("SVG (.svg)", null, (s, e) => Export("svg"));
            tsbExport.DropDownItems.Add("PDF (.pdf)", null, (s, e) => Export("pdf"));
            tsbExport.DropDownItems.Add("Excel (.xlsx)", null, (s, e) => Export("xlsx"));
            tsbExport.DropDownItems.Add("JSON (.json)", null, (s, e) => Export("json"));
            tsbExport.DropDownItems.Add("GraphML (.graphml)", null, (s, e) => Export("graphml"));
            tsbExport.DropDownItems.Add("HTML (.html)", null, (s, e) => Export("html"));
            tsbExport.DropDownItems.Add("Mermaid (.mmd)", null, (s, e) => Export("mmd"));

            tsbLoad.Click += (s, e) => ExecuteMethod(LoadPipeline);
            cboTable.SelectedIndexChanged += (s, e) => OnFilterChanged();
            cboMessage.SelectedIndexChanged += (s, e) => OnFilterChanged();
            cboStage.SelectedIndexChanged += (s, e) => OnFilterChanged();
            cboMode.SelectedIndexChanged += (s, e) => OnFilterChanged();
            cboSolution.SelectedIndexChanged += (s, e) => OnFilterChanged();
            cboFocus.SelectedIndexChanged += (s, e) => OnFilterChanged();
            lvNodes.SelectedIndexChanged += (s, e) => OnNodeSelected();

            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Plugin Dependency Graph"));
        }

        #region Lifecycle

        private void PluginDependencyGraphControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();
            LogInfo("Plugin Dependency Graph loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            CaptureSettings();
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail,
            string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            _data = null;
            _fullGraph = null;
            _viewGraph = null;
            _findings = new List<Finding>();
            _suppress = true;
            lvNodes.Items.Clear();
            lvDetails.Items.Clear();
            lvDependencies.Items.Clear();
            lvFindings.Items.Clear();
            foreach (var c in new[] { cboTable, cboMessage, cboStage, cboMode, cboSolution, cboFocus })
                c.Items.Clear();
            _suppress = false;
            tsbExport.Enabled = false;
            txtPreview.Text = "Click \"Load pipeline\" to build the plugin dependency graph.";
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        private void CaptureSettings()
        {
            _settings.HighImpactThreshold = Math.Max(1, _settings.HighImpactThreshold);
        }

        #endregion

        #region Load

        private void LoadPipeline()
        {
            var service = Service;
            RunAsync("Retrieving plugin registrations…",
                worker => _collector.Collect(service, worker, msg => worker.ReportProgress(0, msg)),
                data =>
                {
                    _data = data;
                    _fullGraph = PluginGraphBuilder.Build(data);
                    _findings = PluginRiskRules.Evaluate(_fullGraph, data,
                        new PluginRiskOptions { HighImpactThreshold = Math.Max(1, _settings.HighImpactThreshold) });
                    PopulateFilterCombos();
                    PopulateFindings();
                    tsbExport.Enabled = _fullGraph.Nodes.Count > 0;
                    RefreshView();
                    var noteText = data.Notes.Count > 0 ? $"  {data.Notes.Count} note(s)." : "";
                    SetStatusMessage($"Loaded {data.Assemblies.Count} assemblies, {data.Steps.Count} steps, " +
                                     $"{_fullGraph.Nodes.Count} nodes, {_fullGraph.Edges.Count} edges.{noteText}");
                });
        }

        private void PopulateFilterCombos()
        {
            _suppress = true;
            void Fill(ToolStripComboBox cbo, IEnumerable<string> values)
            {
                cbo.Items.Clear();
                cbo.Items.Add(All);
                foreach (var v in values.Where(v => !string.IsNullOrWhiteSpace(v))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
                    cbo.Items.Add(v);
                cbo.SelectedIndex = 0;
            }

            Fill(cboTable, _data.Steps.Select(s => s.PrimaryEntity));
            Fill(cboMessage, _data.Steps.Select(s => s.MessageName));
            Fill(cboStage, _data.Steps.Select(s => s.Stage));
            Fill(cboMode, _data.Steps.Select(s => s.Mode));
            Fill(cboSolution, _data.Steps.Select(s => s.OwningSolution)
                .Concat(_data.Assemblies.Select(a => a.OwningSolution)));

            // Focus = isolate one assembly or plugin type's subgraph.
            _focusIds.Clear();
            cboFocus.Items.Clear();
            cboFocus.Items.Add(All);
            foreach (var n in _fullGraph.Nodes
                .Where(n => n.Type == PluginNodeType.Assembly || n.Type == PluginNodeType.PluginType)
                .OrderBy(n => (int)n.Type).ThenBy(n => n.Label, StringComparer.OrdinalIgnoreCase))
            {
                var prefix = n.Type == PluginNodeType.Assembly ? "[Asm] " : "[Type] ";
                var label = prefix + (n.Label ?? n.Id);
                if (!_focusIds.ContainsKey(label)) { _focusIds[label] = n.Id; cboFocus.Items.Add(label); }
            }
            cboFocus.SelectedIndex = 0;
            _suppress = false;
        }

        #endregion

        #region View / filter

        private string Sel(ToolStripComboBox cbo)
        {
            var v = cbo.SelectedItem as string;
            return (v == null || v == All) ? null : v;
        }

        private void OnFilterChanged()
        {
            if (_suppress || _fullGraph == null) return;
            RefreshView();
        }

        private void RefreshView()
        {
            if (_fullGraph == null) return;

            var g = _fullGraph.Filter(Sel(cboTable), Sel(cboMessage), Sel(cboStage), Sel(cboMode), Sel(cboSolution));

            var focusLabel = cboFocus.SelectedItem as string;
            if (focusLabel != null && focusLabel != All && _focusIds.TryGetValue(focusLabel, out var focusId))
            {
                // Isolate the focused node's subgraph, then intersect with the active filter set.
                var iso = _fullGraph.Subgraph(focusId);
                var keep = new HashSet<string>(g.Nodes.Select(n => n.Id), StringComparer.OrdinalIgnoreCase);
                g = new PluginGraph
                {
                    Nodes = iso.Nodes.Where(n => keep.Contains(n.Id)).ToList(),
                    Edges = iso.Edges.Where(e => keep.Contains(e.FromId) && keep.Contains(e.ToId)).ToList()
                };
            }

            _viewGraph = g;
            txtPreview.Text = PluginGraphEmitters.Mermaid(g);
            PopulateNodes(g);
            lblStatus.Text = $"{g.Nodes.Count} node(s), {g.Edges.Count} edge(s) shown. {_findings.Count} risk finding(s).";
        }

        private void PopulateNodes(PluginGraph g)
        {
            lvNodes.BeginUpdate();
            lvNodes.Items.Clear();
            foreach (var n in g.Nodes.OrderBy(n => (int)n.Type).ThenBy(n => n.Label, StringComparer.OrdinalIgnoreCase))
            {
                lvNodes.Items.Add(new ListViewItem(new[] { n.Type.ToString(), n.Label ?? n.Id }) { Tag = n.Id });
            }
            lvNodes.EndUpdate();
            lvDetails.Items.Clear();
            lvDependencies.Items.Clear();
        }

        private void OnNodeSelected()
        {
            if (_viewGraph == null || lvNodes.SelectedItems.Count == 0) return;
            var id = lvNodes.SelectedItems[0].Tag as string;
            var node = _viewGraph.Node(id) ?? _fullGraph?.Node(id);
            if (node == null) return;

            lvDetails.BeginUpdate();
            lvDetails.Items.Clear();
            lvDetails.Items.Add(new ListViewItem(new[] { "Type", node.Type.ToString() }));
            lvDetails.Items.Add(new ListViewItem(new[] { "Label", node.Label ?? "" }));
            lvDetails.Items.Add(new ListViewItem(new[] { "Managed", node.IsManaged ? "Yes" : "No" }));
            foreach (var kv in node.Props.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                if (!string.IsNullOrEmpty(kv.Value))
                    lvDetails.Items.Add(new ListViewItem(new[] { kv.Key, kv.Value }));
            lvDetails.EndUpdate();

            lvDependencies.BeginUpdate();
            lvDependencies.Items.Clear();
            var source = _fullGraph ?? _viewGraph;
            foreach (var e in source.Edges.Where(e => string.Equals(e.FromId, id, StringComparison.OrdinalIgnoreCase)))
            {
                var to = source.Node(e.ToId);
                lvDependencies.Items.Add(new ListViewItem(new[] { "→ out", e.Kind, to?.Label ?? e.ToId, to?.Type.ToString() ?? "" }));
            }
            foreach (var e in source.Edges.Where(e => string.Equals(e.ToId, id, StringComparison.OrdinalIgnoreCase)))
            {
                var from = source.Node(e.FromId);
                lvDependencies.Items.Add(new ListViewItem(new[] { "← in", e.Kind, from?.Label ?? e.FromId, from?.Type.ToString() ?? "" }));
            }
            lvDependencies.EndUpdate();
        }

        private void PopulateFindings()
        {
            lvFindings.BeginUpdate();
            lvFindings.Items.Clear();
            foreach (var f in _findings)
            {
                var item = new ListViewItem(new[] { f.Severity.ToString(), f.Title, f.Component ?? "" })
                {
                    ToolTipText = f.Description
                };
                lvFindings.Items.Add(item);
            }
            lvFindings.EndUpdate();
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_viewGraph == null || _viewGraph.Nodes.Count == 0)
            {
                MessageBox.Show(this, "Load the pipeline first.", "Plugin Dependency Graph",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string filter, ext;
            switch (kind)
            {
                case "png": filter = "PNG|*.png"; ext = "png"; break;
                case "svg": filter = "SVG|*.svg"; ext = "svg"; break;
                case "pdf": filter = "PDF|*.pdf"; ext = "pdf"; break;
                case "xlsx": filter = "Excel|*.xlsx"; ext = "xlsx"; break;
                case "json": filter = "JSON|*.json"; ext = "json"; break;
                case "graphml": filter = "GraphML|*.graphml"; ext = "graphml"; break;
                case "html": filter = "HTML|*.html"; ext = "html"; break;
                default: filter = "Mermaid|*.mmd"; ext = "mmd"; break;
            }

            string path;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = $"PluginGraph_{DateTime.Now:yyyyMMdd_HHmm}.{ext}" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.FileName;
            }

            var graph = _viewGraph;
            var report = BuildReport();
            RunAsync($"Exporting {ext.ToUpperInvariant()}…",
                worker =>
                {
                    switch (kind)
                    {
                        case "png": PluginPngExporter.Export(graph, path); break;
                        case "svg": PluginGraphEmitters.ExportSvg(graph, path); break;
                        case "pdf": PdfReportExporter.Export(report, path); break;
                        case "xlsx": ExcelReportExporter.Export(report, path); break;
                        case "json": PluginGraphEmitters.ExportJson(graph, path); break;
                        case "graphml": PluginGraphEmitters.ExportGraphML(graph, path); break;
                        case "html": PluginGraphEmitters.ExportHtml(graph, path); break;
                        default: PluginGraphEmitters.ExportMermaid(graph, path); break;
                    }
                    return path;
                },
                written =>
                {
                    SetStatusMessage($"Exported to {written}");
                    if (MessageBox.Show(this, "Graph exported. Open it now?", "Plugin Dependency Graph",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        try { System.Diagnostics.Process.Start(written); }
                        catch (Exception ex) { ShowError(ex); }
                    }
                });
        }

        /// <summary>Builds the shared <see cref="ReportModel"/> (findings + metrics) for the PDF/Excel exporters.</summary>
        private ReportModel BuildReport()
        {
            var r = new ReportModel
            {
                ToolName = "Plugin Dependency Graph",
                ToolVersion = GetType().Assembly.GetName().Version?.ToString(),
                ReportTitle = "Plugin Dependency Report",
                Subtitle = "Dataverse plugin registration footprint",
                ScoreWord = "risk",
                SubjectName = ConnectionDetail?.ConnectionName ?? "Connected environment",
                SourceEnvironment = ConnectionDetail?.ConnectionName,
                AnalyzedOnUtc = DateTime.UtcNow
            };
            r.Findings.AddRange(_findings);

            if (_data != null)
            {
                r.Metrics.Add(new MetricRow("Plugin assemblies", _data.Assemblies.Count.ToString()));
                r.Metrics.Add(new MetricRow("Plugin types", _data.Types.Count.ToString()));
                r.Metrics.Add(new MetricRow("Processing steps", _data.Steps.Count.ToString()));
                r.Metrics.Add(new MetricRow("Step images", _data.Images.Count.ToString()));
                r.Metrics.Add(new MetricRow("Custom APIs", _data.CustomApis.Count.ToString()));
                r.Metrics.Add(new MetricRow("Tables touched",
                    _data.Steps.Where(s => !string.IsNullOrWhiteSpace(s.PrimaryEntity))
                        .Select(s => s.PrimaryEntity).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString()));
                r.Metrics.Add(new MetricRow("Messages used",
                    _data.Steps.Where(s => !string.IsNullOrWhiteSpace(s.MessageName))
                        .Select(s => s.MessageName).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString()));
            }

            r.Score = Score(_findings);
            r.Band = r.Score >= 60 ? ScoreBand.High : r.Score >= 30 ? ScoreBand.Medium : ScoreBand.Low;
            r.AnalyzersRun.Add("Plugin risk rules");
            return r;
        }

        private static int Score(IEnumerable<Finding> findings)
        {
            int total = 0;
            foreach (var f in findings)
            {
                switch (f.Severity)
                {
                    case Severity.Critical: total += 40; break;
                    case Severity.High: total += 25; break;
                    case Severity.Medium: total += 12; break;
                    case Severity.Low: total += 5; break;
                    default: total += 1; break;
                }
            }
            return Math.Min(100, total);
        }

        #endregion
    }

    /// <summary>Persisted settings (plain serializable POCO — no controls/services/credentials).</summary>
    public class ToolSettings
    {
        public int HighImpactThreshold { get; set; } = 5;
    }
}
