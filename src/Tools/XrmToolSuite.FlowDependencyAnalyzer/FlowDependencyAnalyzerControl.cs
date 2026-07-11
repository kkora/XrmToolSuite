using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.FlowDependencyAnalyzer.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;
// PluginControlBase pulls in a Label; disambiguate to the WinForms one (CS0104 guard for future edits).
using Label = System.Windows.Forms.Label;

namespace XrmToolSuite.FlowDependencyAnalyzer
{
    public partial class FlowDependencyAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private FlowSettings _settings;
        private FlowAnalysis _analysis;
        private List<FlowImpact> _impactMap = new List<FlowImpact>();
        private bool _suppressFilter;

        private const string All = "(all)";
        private const string AllKinds = "(all kinds)";

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public FlowDependencyAnalyzerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Flow Dependency Analyzer"));
            SetupGrids();
            SetupImpactKinds();
        }

        private void FlowDependencyAnalyzerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<FlowSettings>();
            LogInfo("Flow Dependency Analyzer loaded");
            Status("Click 'Analyze flows' to map every cloud flow's dependencies (read-only).");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService,
            ConnectionDetail detail,
            string actionName,
            object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            ClearResults();
            Status($"Connected to {detail?.ConnectionName}. Click 'Analyze flows'.");
        }


        // ----------------------------------------------------------------- Analyze (needs connection)

        private void tsbAnalyze_Click(object sender, EventArgs e) => ExecuteMethod(AnalyzeFlows);

        private void AnalyzeFlows()
        {
            RunAsync(
                "Analyzing cloud-flow dependencies...",
                worker =>
                {
                    var collector = new FlowCollector(new FlowRiskOptions());
                    return collector.Collect(Service, worker, msg => worker.ReportProgress(0, msg));
                },
                analysis =>
                {
                    _analysis = analysis ?? new FlowAnalysis();
                    _impactMap = _analysis.BuildImpactMap();
                    PopulateFilters();
                    ApplyFilter();
                    PopulateFindings();
                    PopulateImpactComponents();
                    PopulateReadiness();
                    tsbExport.Enabled = _analysis.Flows.Count > 0;

                    var direct = _analysis.Flows.Count(f => f.UsesDirectConnection);
                    Status($"Analyzed {_analysis.Flows.Count} cloud flow(s). " +
                        $"{_analysis.Findings.Count(f => f.Severity >= Severity.High)} high/critical finding(s), " +
                        $"{direct} flow(s) using a direct connection.");
                });
        }

        // ----------------------------------------------------------------- Grid / filter setup

        private void SetupGrids()
        {
            grdFlows.Columns.Clear();
            grdFlows.Columns.Add(TextCol("Flow", "Flow", 220));
            grdFlows.Columns.Add(TextCol("State", "State", 70));
            grdFlows.Columns.Add(TextCol("Trigger", "Trigger", 90));
            grdFlows.Columns.Add(TextCol("Entity", "Trigger table", 90));
            grdFlows.Columns.Add(TextCol("Connectors", "#Conn", 55));
            grdFlows.Columns.Add(TextCol("ConnRefs", "#CRefs", 55));
            grdFlows.Columns.Add(TextCol("EnvVars", "#EnvV", 55));
            grdFlows.Columns.Add(TextCol("Tables", "#Tbls", 55));
            grdFlows.Columns.Add(TextCol("Children", "#Child", 55));
            grdFlows.Columns.Add(TextCol("CustomApis", "#CApi", 55));
            grdFlows.Columns.Add(TextCol("Http", "#HTTP", 55));
            grdFlows.Columns.Add(TextCol("Direct", "Direct?", 60));

            grdFindings.Columns.Clear();
            grdFindings.Columns.Add(TextCol("Severity", "Severity", 80));
            grdFindings.Columns.Add(TextCol("Title", "Finding", 240));
            grdFindings.Columns.Add(TextCol("Component", "Component", 180));
            grdFindings.Columns.Add(TextCol("Recommendation", "Recommendation", 400));
        }

        private static DataGridViewTextBoxColumn TextCol(string name, string header, int width) =>
            new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Width = width };

        private void SetupImpactKinds()
        {
            cboImpactKind.Items.Clear();
            cboImpactKind.Items.Add(AllKinds);
            foreach (var kind in Enum.GetNames(typeof(FlowComponentKind)))
                cboImpactKind.Items.Add(kind);
            cboImpactKind.SelectedIndex = 0;
        }

        // ----------------------------------------------------------------- Filters

        private void PopulateFilters()
        {
            _suppressFilter = true;
            try
            {
                var flows = _analysis?.Flows ?? new List<FlowDependencies>();
                FillCombo(cboStatus, new[] { "Activated", "Draft" }.Where(s => flows.Any(f => f.State == s)));
                FillCombo(cboOwner, flows.Select(f => f.Owner).Where(o => !string.IsNullOrEmpty(o)).Distinct());
                FillCombo(cboConnector, flows.SelectMany(f => f.Connectors).Distinct());
                FillCombo(cboTrigger, flows.Select(f => f.TriggerType).Where(t => !string.IsNullOrEmpty(t)).Distinct());
                FillCombo(cboTable, flows.SelectMany(f => f.Tables).Distinct());
                FillCombo(cboSolution, flows.SelectMany(f => SplitSolutions(f.Solution)).Distinct());
            }
            finally { _suppressFilter = false; }
        }

        private static IEnumerable<string> SplitSolutions(string s) =>
            string.IsNullOrEmpty(s) ? Enumerable.Empty<string>() : s.Split(';').Select(x => x.Trim()).Where(x => x.Length > 0);

        private static void FillCombo(ToolStripComboBox combo, IEnumerable<string> values)
        {
            combo.Items.Clear();
            combo.Items.Add(All);
            foreach (var v in values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                combo.Items.Add(v);
            combo.SelectedIndex = 0;
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            if (_suppressFilter) return;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var flows = _analysis?.Flows ?? new List<FlowDependencies>();
            var filtered = flows.Where(PassesFilter).ToList();

            grdFlows.Rows.Clear();
            foreach (var f in filtered)
            {
                int i = grdFlows.Rows.Add(
                    f.FlowName,
                    f.State,
                    f.TriggerType,
                    f.TriggerEntity,
                    f.Connectors.Count,
                    f.ConnectionReferences.Count,
                    f.EnvironmentVariables.Count,
                    f.Tables.Count,
                    f.ChildFlows.Count,
                    f.CustomApis.Count,
                    f.HttpActions.Count,
                    f.UsesDirectConnection ? "Yes" : "");
                grdFlows.Rows[i].Tag = f;
                if (f.UsesDirectConnection)
                    grdFlows.Rows[i].Cells["Direct"].Style.BackColor = Color.FromArgb(255, 224, 178);
            }

            if (grdFlows.Rows.Count > 0)
            {
                grdFlows.ClearSelection();
                grdFlows.Rows[0].Selected = true;
            }
            else
            {
                tvDependencies.Nodes.Clear();
                lblDetail.Text = "No flows match the current filters.";
            }
        }

        private bool PassesFilter(FlowDependencies f)
        {
            if (!Match(cboStatus, f.State)) return false;
            if (!Match(cboOwner, f.Owner)) return false;
            if (!MatchAny(cboTrigger, new[] { f.TriggerType })) return false;
            if (!MatchAny(cboConnector, f.Connectors)) return false;
            if (!MatchAny(cboTable, f.Tables)) return false;
            if (!MatchAny(cboSolution, SplitSolutions(f.Solution))) return false;
            return true;
        }

        private static bool Match(ToolStripComboBox combo, string value)
        {
            var sel = combo.SelectedItem as string;
            return string.IsNullOrEmpty(sel) || sel == All ||
                   string.Equals(sel, value, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchAny(ToolStripComboBox combo, IEnumerable<string> values)
        {
            var sel = combo.SelectedItem as string;
            if (string.IsNullOrEmpty(sel) || sel == All) return true;
            return values.Any(v => string.Equals(sel, v, StringComparison.OrdinalIgnoreCase));
        }

        // ----------------------------------------------------------------- Dependency tree

        private void grdFlows_SelectionChanged(object sender, EventArgs e)
        {
            var flow = grdFlows.SelectedRows.Count > 0
                ? grdFlows.SelectedRows[0].Tag as FlowDependencies
                : null;

            tvDependencies.BeginUpdate();
            tvDependencies.Nodes.Clear();
            if (flow == null)
            {
                lblDetail.Text = "Select a flow to see its dependency tree";
                tvDependencies.EndUpdate();
                return;
            }

            lblDetail.Text = $"Dependencies for: {flow.FlowName}";
            var root = tvDependencies.Nodes.Add(flow.FlowName);

            var trigger = string.IsNullOrEmpty(flow.TriggerEntity)
                ? flow.TriggerType
                : $"{flow.TriggerType} — {flow.TriggerMessage} {flow.TriggerEntity}".Trim();
            root.Nodes.Add($"Trigger: {trigger}");

            AddBranch(root, "Tables", flow.Tables);
            AddBranch(root, "Columns", flow.Columns);
            AddBranch(root, "Connectors", flow.Connectors);
            AddBranch(root, "Connection references", flow.ConnectionReferences);
            AddBranch(root, "Environment variables", flow.EnvironmentVariables);
            AddBranch(root, "Child flows", flow.ChildFlows);
            AddBranch(root, "Custom APIs", flow.CustomApis);
            AddBranch(root, "HTTP actions (URLs redacted)", flow.HttpActions);
            AddBranch(root, "Hardcoded literals (redacted)", flow.HardcodedLiterals);
            if (flow.UsesDirectConnection)
                root.Nodes.Add("⚠ Uses a direct connection (not portable)");
            if (!string.IsNullOrEmpty(flow.Solution))
                root.Nodes.Add($"Solutions: {flow.Solution}");
            if (!string.IsNullOrEmpty(flow.ParseNote))
                root.Nodes.Add($"Note: {flow.ParseNote}");

            root.Expand();
            tvDependencies.EndUpdate();
        }

        private static void AddBranch(TreeNode root, string label, List<string> items)
        {
            var node = root.Nodes.Add($"{label} ({items.Count})");
            foreach (var item in items)
                node.Nodes.Add(item);
        }

        // ----------------------------------------------------------------- Findings

        private void PopulateFindings()
        {
            grdFindings.Rows.Clear();
            if (_analysis == null) return;
            foreach (var f in _analysis.Findings.OrderByDescending(x => x.Severity))
            {
                int i = grdFindings.Rows.Add(f.Severity.ToString(), f.Title, f.Component ?? "", f.Recommendation ?? "");
                grdFindings.Rows[i].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
        }

        // ----------------------------------------------------------------- Component impact (reverse lookup)

        private void cboImpactKind_SelectedIndexChanged(object sender, EventArgs e) => PopulateImpactComponents();

        private void PopulateImpactComponents()
        {
            cboImpactComponent.Items.Clear();
            lstImpactedFlows.Items.Clear();

            var selectedKind = cboImpactKind.SelectedItem as string;
            IEnumerable<FlowImpact> items = _impactMap;
            if (!string.IsNullOrEmpty(selectedKind) && selectedKind != AllKinds &&
                Enum.TryParse<FlowComponentKind>(selectedKind, out var kind))
                items = items.Where(m => m.Kind == kind);

            foreach (var impact in items.OrderBy(m => m.Component, StringComparer.OrdinalIgnoreCase))
                cboImpactComponent.Items.Add(new ImpactItem(impact));

            if (cboImpactComponent.Items.Count > 0)
                cboImpactComponent.SelectedIndex = 0;
        }

        private void cboImpactComponent_SelectedIndexChanged(object sender, EventArgs e)
        {
            lstImpactedFlows.Items.Clear();
            if (!(cboImpactComponent.SelectedItem is ImpactItem item)) return;

            lblImpactedHeader.Text = $"Impacted flows for {item.Impact.Kind} '{item.Impact.Component}' ({item.Impact.ImpactedFlows.Count})";
            foreach (var name in item.Impact.ImpactedFlows)
                lstImpactedFlows.Items.Add(name);
        }

        private sealed class ImpactItem
        {
            public FlowImpact Impact { get; }
            public ImpactItem(FlowImpact impact) { Impact = impact; }
            public override string ToString() => $"{Impact.Component}  ({Impact.ImpactedFlows.Count} flow(s))";
        }

        // ----------------------------------------------------------------- Deployment readiness

        private void PopulateReadiness()
        {
            lvReadiness.Items.Clear();
            if (_analysis == null) return;

            var findings = _analysis.Findings;
            var calc = ScoreCalculator.RiskDefault;
            int score = calc.Score(findings);
            var band = calc.Band(findings, score);
            bool pass = band < ScoreBand.High;

            lblReadiness.Text = $"Deployment readiness: {(pass ? "PASS" : "REVIEW REQUIRED")}   " +
                $"(risk {band}, score {score}/100 across {_analysis.Flows.Count} flow(s))";
            lblReadiness.ForeColor = pass ? Color.FromArgb(0, 120, 0) : Color.FromArgb(170, 0, 0);

            int direct = _analysis.Flows.Count(f => f.UsesDirectConnection);
            int missingCr = findings.Count(f => f.Title.Contains("missing connection reference"));
            int missingEv = findings.Count(f => f.Title.Contains("missing environment variable"));
            int missingTbl = findings.Count(f => f.Title.Contains("missing table"));
            int hardcoded = findings.Count(f => f.Title.StartsWith("Hardcoded"));
            int parseErrors = _analysis.Flows.Count(f => !string.IsNullOrEmpty(f.ParseNote));

            AddCheck(direct == 0, "No direct connections", direct == 0
                ? "All connectors bind to connection references."
                : $"{direct} flow(s) use a direct connection — not portable.");
            AddCheck(missingCr == 0, "All connection references resolve", missingCr == 0
                ? "No flow references a missing connection reference."
                : $"{missingCr} reference(s) to a missing connection reference.");
            AddCheck(missingEv == 0, "All environment variables resolve", missingEv == 0
                ? "No flow references a missing environment variable."
                : $"{missingEv} reference(s) to a missing environment variable.");
            AddCheck(missingTbl == 0, "All referenced tables exist", missingTbl == 0
                ? "No flow references a missing table."
                : $"{missingTbl} reference(s) to a missing table (Critical).");
            AddCheck(hardcoded == 0, "No hardcoded literals", hardcoded == 0
                ? "No hardcoded URLs/GUIDs detected (redacted where sensitive)."
                : $"{hardcoded} hardcoded literal(s) — move into environment variables.");
            AddCheck(parseErrors == 0, "All flow definitions parsed", parseErrors == 0
                ? "Every flow's clientdata parsed cleanly."
                : $"{parseErrors} flow(s) could not be fully parsed.");
        }

        private void AddCheck(bool ok, string check, string detail)
        {
            var item = new ListViewItem(new[] { (ok ? "✔  " : "✘  ") + check, detail });
            item.BackColor = ok ? Color.FromArgb(226, 240, 217) : Color.FromArgb(255, 224, 178);
            lvReadiness.Items.Add(item);
        }

        // ----------------------------------------------------------------- Export

        private void tsmExportExcel_Click(object sender, EventArgs e) =>
            ExportWith("Excel (*.xlsx)|*.xlsx", "flow-dependencies.xlsx",
                path => ExcelReportExporter.Export(BuildReportModel(), path));

        private void tsmExportPdf_Click(object sender, EventArgs e) =>
            ExportWith("PDF (*.pdf)|*.pdf", "flow-dependencies.pdf",
                path => PdfReportExporter.Export(BuildReportModel(), path));

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportWith("HTML (*.html)|*.html", "flow-dependencies.html",
                path => HtmlDashboardBuilder.Export(BuildReportModel(), path));

        private void tsmExportJson_Click(object sender, EventArgs e) =>
            ExportWith("JSON (*.json)|*.json", "flow-dependencies.json",
                path => System.IO.File.WriteAllText(path, BuildJson(), Encoding.UTF8));

        private void ExportWith(string filter, string fileName, Action<string> writer)
        {
            if (_analysis == null || _analysis.Flows.Count == 0)
            {
                MessageBox.Show("Analyze flows first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = fileName })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var path = dlg.FileName;
                RunAsync(
                    "Exporting report...",
                    worker => { writer(path); return path; },
                    saved => { Status("Exported flow dependency report to " + saved); PromptOpenExportedFile(saved); });
            }
        }

        private ReportModel BuildReportModel()
        {
            var flows = _analysis.Flows;
            var findings = _analysis.Findings;
            var calc = ScoreCalculator.RiskDefault;
            int score = calc.Score(findings);

            var r = new ReportModel
            {
                ToolName = "Flow Dependency Analyzer",
                ReportTitle = "Flow Dependency Analysis",
                ScoreWord = "risk",
                SubjectName = "Cloud flows",
                Score = score,
                Band = calc.Band(findings, score),
                LeadIn = "Static dependency map of every cloud flow parsed from its clientdata. " +
                         "HTTP endpoint URLs, SAS/trigger URLs and secrets are redacted in every format.",
                VerdictHigh = "Resolve the critical/high findings (missing metadata, direct connections) before deploying.",
                VerdictMedium = "Review the findings below — mostly portability (hardcoded values) concerns.",
                VerdictLow = "No significant deployment risk detected across the analyzed flows."
            };

            r.Metrics.Add(new MetricRow("Flows analyzed", flows.Count.ToString()));
            r.Metrics.Add(new MetricRow("Using direct connections", flows.Count(f => f.UsesDirectConnection).ToString()));
            r.Metrics.Add(new MetricRow("Distinct connectors", flows.SelectMany(f => f.Connectors).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString()));
            r.Metrics.Add(new MetricRow("Distinct connection references", flows.SelectMany(f => f.ConnectionReferences).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString()));
            r.Metrics.Add(new MetricRow("Distinct tables", flows.SelectMany(f => f.Tables).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString()));
            r.Metrics.Add(new MetricRow("Child-flow references", flows.SelectMany(f => f.ChildFlows).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString()));
            r.Metrics.Add(new MetricRow("Custom-API invocations", flows.SelectMany(f => f.CustomApis).Distinct(StringComparer.OrdinalIgnoreCase).Count().ToString()));
            r.Metrics.Add(new MetricRow("HTTP actions", flows.Sum(f => f.HttpActions.Count).ToString(), "URLs redacted"));
            r.Metrics.Add(new MetricRow("Unparseable flows", flows.Count(f => !string.IsNullOrEmpty(f.ParseNote)).ToString()));

            foreach (var f in findings)
                r.Findings.Add(f);

            return r;
        }

        /// <summary>CI-friendly JSON: report summary + findings + the impacted-flow map + a pass/fail readiness flag.</summary>
        private string BuildJson()
        {
            var flows = _analysis.Flows;
            var findings = _analysis.Findings;
            var calc = ScoreCalculator.RiskDefault;
            int score = calc.Score(findings);
            var band = calc.Band(findings, score);
            bool pass = band < ScoreBand.High;

            var payload = new
            {
                tool = "Flow Dependency Analyzer",
                analyzedOnUtc = DateTime.UtcNow,
                score,
                band = band.ToString(),
                readiness = new { pass, failAtBand = "High", suggestedExitCode = pass ? 0 : 1 },
                flows = flows.Select(f => new
                {
                    name = f.FlowName,
                    state = f.State,
                    owner = f.Owner,
                    solution = f.Solution,
                    trigger = new { type = f.TriggerType, table = f.TriggerEntity, message = f.TriggerMessage },
                    connectors = f.Connectors,
                    connectionReferences = f.ConnectionReferences,
                    environmentVariables = f.EnvironmentVariables,
                    tables = f.Tables,
                    columns = f.Columns,
                    childFlows = f.ChildFlows,
                    customApis = f.CustomApis,
                    httpActions = f.HttpActions,        // action names only — URLs redacted
                    usesDirectConnection = f.UsesDirectConnection,
                    hardcodedLiterals = f.HardcodedLiterals, // secrets/URLs already redacted
                    parseNote = f.ParseNote
                }),
                impactMap = _impactMap.Select(m => new
                {
                    kind = m.Kind.ToString(),
                    component = m.Component,
                    impactedFlows = m.ImpactedFlows
                }),
                findings = findings.OrderByDescending(f => f.Severity).Select(f => new
                {
                    category = f.Category,
                    severity = f.Severity.ToString(),
                    title = f.Title,
                    component = f.Component,
                    description = f.Description,
                    recommendation = f.Recommendation
                })
            };

            return JsonConvert.SerializeObject(payload, Formatting.Indented);
        }

        // ----------------------------------------------------------------- Helpers

        private void ClearResults()
        {
            _analysis = null;
            _impactMap = new List<FlowImpact>();
            grdFlows.Rows.Clear();
            grdFindings.Rows.Clear();
            tvDependencies.Nodes.Clear();
            lstImpactedFlows.Items.Clear();
            cboImpactComponent.Items.Clear();
            lvReadiness.Items.Clear();
            lblDetail.Text = "Select a flow to see its dependency tree";
            lblReadiness.Text = "Analyze flows to build the deployment-readiness checklist.";
            tsbExport.Enabled = false;
        }

        private void Status(string message)
        {
            lblStatus.Text = message;
            SetStatusMessage(message);
        }

        private static Color SeverityColor(Severity s)
        {
            switch (s)
            {
                case Severity.Critical: return Color.FromArgb(255, 205, 210);
                case Severity.High: return Color.FromArgb(255, 224, 178);
                case Severity.Medium: return Color.FromArgb(255, 245, 200);
                case Severity.Low: return Color.FromArgb(226, 240, 217);
                default: return Color.White;
            }
        }
    }

    /// <summary>Serializable settings POCO round-tripped via BaseToolControl LoadSettings/SaveSettings.</summary>
    public class FlowSettings
    {
        public string LastStatusFilter { get; set; }
    }
}
