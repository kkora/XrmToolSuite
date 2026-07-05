using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using IOrganizationService = Microsoft.Xrm.Sdk.IOrganizationService;
using Entity = Microsoft.Xrm.Sdk.Entity;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.SolutionKnowledgeGraph.Graph;
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.SolutionKnowledgeGraph
{
    /// <summary>
    /// Builds an interactive dependency graph for a Dataverse solution: search/filter the nodes, trace a
    /// node's dependencies and deletion impact, detect circular dependencies, open a self-contained
    /// interactive HTML view, and export to GraphML / SVG / PNG. Follows the suite patterns; ships only
    /// its own DLL (no ClosedXML/MigraDoc — GraphML/SVG/HTML are pure strings, PNG uses GDI+).
    /// </summary>
    public partial class SolutionKnowledgeGraphControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => SuiteDocsUrl;

        private GraphSettings _settings = new GraphSettings();
        private GraphModel _graph;
        private List<Entity> _solutions = new List<Entity>();
        private string _solutionName = "Solution";

        private ComboBox _cboSolution;
        private TextBox _txtSearch, _txtDetail;
        private CheckedListBox _lstTypes;
        private DataGridView _grid;
        private Label _lblStats;
        private ToolStripDropDownButton _btnExport;
        private ToolStripButton _btnInteractive, _btnCycles;

        public SolutionKnowledgeGraphControl()
        {
            BuildUi();
            Load += (s, e) => _settings = LoadSettings<GraphSettings>();
        }

        #region UI

        private void BuildUi()
        {
            Dock = DockStyle.Fill;

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            var btnClose = new ToolStripButton("Close") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnClose.Click += (s, e) => CloseTool();
            var btnLoad = new ToolStripButton("Load solutions") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnLoad.Click += (s, e) => ExecuteMethod(LoadSolutions);
            _cboSolution = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
            var btnBuild = new ToolStripButton("▶ Build graph") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnBuild.Click += (s, e) => ExecuteMethod(BuildGraph);

            _btnInteractive = new ToolStripButton("Open interactive graph") { DisplayStyle = ToolStripItemDisplayStyle.Text, Enabled = false };
            _btnInteractive.Click += (s, e) => OpenInteractive();
            _btnCycles = new ToolStripButton("Detect cycles") { DisplayStyle = ToolStripItemDisplayStyle.Text, Enabled = false };
            _btnCycles.Click += (s, e) => DetectCycles();

            _btnExport = new ToolStripDropDownButton("Export") { DisplayStyle = ToolStripItemDisplayStyle.Text, Enabled = false };
            _btnExport.DropDownItems.Add("GraphML (.graphml)", null, (s, e) => Export("graphml"));
            _btnExport.DropDownItems.Add("SVG (.svg)", null, (s, e) => Export("svg"));
            _btnExport.DropDownItems.Add("PNG (.png)", null, (s, e) => Export("png"));
            _btnExport.DropDownItems.Add("Interactive HTML (.html)", null, (s, e) => Export("html"));

            toolbar.Items.AddRange(new ToolStripItem[]
            {
                btnClose, new ToolStripSeparator(), btnLoad, new ToolStripLabel("Solution:"),
                new ToolStripControlHost(_cboSolution), btnBuild, new ToolStripSeparator(),
                _btnInteractive, _btnCycles, _btnExport, CreateHelpButton("Solution Knowledge Graph")
            });

            _lblStats = new Label { Dock = DockStyle.Top, Height = 24, Padding = new Padding(6, 4, 0, 0), Text = "Build a graph from a solution." };

            // Left: search + type filters
            _txtSearch = new TextBox { Dock = DockStyle.Top };
            _txtSearch.TextChanged += (s, e) => ApplyFilter();
            _lstTypes = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
            _lstTypes.ItemCheck += (s, e) => BeginInvoke((Action)ApplyFilter);
            var leftPanel = new Panel { Dock = DockStyle.Fill };
            leftPanel.Controls.Add(_lstTypes);
            leftPanel.Controls.Add(_txtSearch);
            leftPanel.Controls.Add(new Label { Text = "Search & node types", Dock = DockStyle.Top, Height = 22, Font = new Font(Font, FontStyle.Bold), Padding = new Padding(4, 4, 0, 0) });

            // Middle: node grid
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.None
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", FillWeight = 22 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Node", HeaderText = "Node", FillWeight = 46 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Deps", HeaderText = "Depends on", FillWeight = 16 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Impact", HeaderText = "Impact", FillWeight = 16 });
            _grid.SelectionChanged += (s, e) => ShowNodeDetail();

            _txtDetail = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            var midRight = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 520 };
            midRight.Panel1.Controls.Add(_grid);
            midRight.Panel2.Controls.Add(_txtDetail);

            var mainSplit = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 220 };
            mainSplit.Panel1.Controls.Add(leftPanel);
            mainSplit.Panel2.Controls.Add(midRight);

            var body = new Panel { Dock = DockStyle.Fill };
            body.Controls.Add(mainSplit);
            body.Controls.Add(_lblStats);

            Controls.Add(body);
            Controls.Add(toolbar);
        }

        #endregion

        #region Lifecycle

        public override void ClosingPlugin(PluginCloseInfo info) { SaveSettings(_settings); base.ClosingPlugin(info); }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail,
            string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear();
            _cboSolution.Items.Clear();
            _solutions.Clear();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        #endregion

        #region Data

        private void LoadSolutions()
        {
            RunAsync("Loading solutions…",
                worker =>
                {
                    var qe = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("uniquename", "friendlyname", "version"),
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
                    return Service.RetrieveAll(qe);
                },
                solutions =>
                {
                    _solutions = solutions;
                    _cboSolution.Items.Clear();
                    foreach (var s in solutions)
                        _cboSolution.Items.Add($"{s.GetAttributeValue<string>("friendlyname")} ({s.GetAttributeValue<string>("version")})");
                    if (_cboSolution.Items.Count > 0) _cboSolution.SelectedIndex = 0;
                    SetStatusMessage($"Loaded {solutions.Count} solution(s).");
                });
        }

        private void BuildGraph()
        {
            int idx = _cboSolution.SelectedIndex;
            if (idx < 0 || idx >= _solutions.Count)
            {
                MessageBox.Show(this, "Load and select a solution first.", "Solution Knowledge Graph",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var solution = _solutions[idx];
            _solutionName = solution.GetAttributeValue<string>("friendlyname") ?? solution.GetAttributeValue<string>("uniquename");
            var service = Service;
            var solutionId = solution.Id;

            RunAsync("Building dependency graph…",
                worker => GraphBuilder.Build(service, solutionId, msg => worker.ReportProgress(0, msg)),
                graph =>
                {
                    _graph = graph;
                    PopulateTypes();
                    ApplyFilter();
                    bool has = graph.NodeCount > 0;
                    _btnExport.Enabled = _btnInteractive.Enabled = _btnCycles.Enabled = has;
                    _lblStats.Text = $"{graph.NodeCount} nodes · {graph.EdgeCount} edges";
                    SetStatusMessage(has ? $"Graph built: {graph.NodeCount} nodes, {graph.EdgeCount} edges."
                        : "No components found in the selected solution.");
                });
        }

        private void PopulateTypes()
        {
            _lstTypes.Items.Clear();
            foreach (var t in _graph.Nodes.Select(n => n.Type).Distinct().OrderBy(t => t))
                _lstTypes.Items.Add(t, true);
        }

        private void ApplyFilter()
        {
            if (_graph == null) return;
            var allowed = new HashSet<string>(_lstTypes.CheckedItems.Cast<string>());
            string term = _txtSearch.Text?.Trim().ToLowerInvariant() ?? "";

            _grid.Rows.Clear();
            foreach (var n in _graph.Nodes
                .Where(n => allowed.Contains(n.Type))
                .Where(n => term.Length == 0 || (n.Label ?? "").ToLowerInvariant().Contains(term))
                .OrderBy(n => n.Type).ThenBy(n => n.Label))
            {
                int i = _grid.Rows.Add(n.Type, n.Label,
                    _graph.DirectDependencies(n.Id).Count, _graph.Impact(n.Id).Count);
                _grid.Rows[i].Tag = n.Id;
            }
        }

        private void ShowNodeDetail()
        {
            if (_graph == null || !(_grid.CurrentRow?.Tag is string id)) return;
            var node = _graph.Node(id);
            if (node == null) return;

            string Names(IEnumerable<string> ids) => string.Join("\r\n  ", ids
                .Select(x => _graph.Node(x)).Where(x => x != null)
                .OrderBy(x => x.Type).ThenBy(x => x.Label)
                .Select(x => $"[{x.Type}] {x.Label}").DefaultIfEmpty("(none)"));

            var trace = _graph.DependencyTrace(id);
            var impact = _graph.Impact(id);
            _txtDetail.Text =
                $"{node.Label}  ({node.Type})\r\n\r\n" +
                $"DEPENDS ON ({trace.Count} transitive):\r\n  {Names(trace)}\r\n\r\n" +
                $"IMPACT OF DELETING THIS ({impact.Count} would be affected):\r\n  {Names(impact)}";
        }

        private void DetectCycles()
        {
            if (_graph == null) return;
            var cycles = _graph.Cycles();
            if (cycles.Count == 0)
            {
                _txtDetail.Text = "No circular dependencies detected. ✔";
                SetStatusMessage("No circular dependencies detected.");
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{cycles.Count} circular dependency group(s) detected:\r\n");
            int i = 1;
            foreach (var cycle in cycles.OrderByDescending(c => c.Count))
            {
                sb.AppendLine($"Cycle {i++} ({cycle.Count} components):");
                foreach (var id in cycle)
                {
                    var n = _graph.Node(id);
                    sb.AppendLine($"  • [{n?.Type}] {n?.Label}");
                }
                sb.AppendLine();
            }
            _txtDetail.Text = sb.ToString();
            SetStatusMessage($"{cycles.Count} circular dependency group(s) detected.");
        }

        #endregion

        #region Export / interactive

        private void OpenInteractive()
        {
            if (_graph == null) return;
            try
            {
                var path = Path.Combine(Path.GetTempPath(), $"KnowledgeGraph_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                HtmlGraphBuilder.Export(_graph, _solutionName, path);
                System.Diagnostics.Process.Start(path);
                SetStatusMessage("Opened interactive graph in your browser.");
            }
            catch (Exception ex) { ShowError(ex); }
        }

        private void Export(string kind)
        {
            if (_graph == null) return;
            string filter;
            switch (kind)
            {
                case "graphml": filter = "GraphML|*.graphml"; break;
                case "svg": filter = "SVG|*.svg"; break;
                case "png": filter = "PNG|*.png"; break;
                default: filter = "HTML|*.html"; break;
            }
            string ext = kind == "graphml" ? "graphml" : kind;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = $"KnowledgeGraph_{DateTime.Now:yyyyMMdd_HHmm}.{ext}" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    switch (kind)
                    {
                        case "graphml": GraphMlExporter.Export(_graph, dlg.FileName); break;
                        case "svg": SvgExporter.Export(_graph, dlg.FileName); break;
                        case "png": PngExporter.Export(_graph, dlg.FileName); break;
                        default: HtmlGraphBuilder.Export(_graph, _solutionName, dlg.FileName); break;
                    }
                    if (MessageBox.Show(this, "Graph exported. Open it now?", "Solution Knowledge Graph",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        System.Diagnostics.Process.Start(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex); }
            }
        }

        #endregion
    }

    /// <summary>Persisted settings (plain POCO).</summary>
    public class GraphSettings
    {
        public bool Placeholder { get; set; }
    }
}
