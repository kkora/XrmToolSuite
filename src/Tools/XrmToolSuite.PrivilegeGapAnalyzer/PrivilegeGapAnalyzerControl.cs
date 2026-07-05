using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.Core.Privileges;            // shared effective-privilege model + engine
using XrmToolSuite.PrivilegeGapAnalyzer.Privileges; // tool-local SDK collector
// Both McTools.Xrm.Connection and Microsoft.Xrm.Sdk.Metadata expose types that clash (CS0104);
// pin the ones the suite uses.
using MetadataCache = XrmToolSuite.Core.MetadataCache;
using Label = System.Windows.Forms.Label;

namespace XrmToolSuite.PrivilegeGapAnalyzer
{
    public partial class PrivilegeGapAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;

        private readonly List<PrincipalItem> _principals = new List<PrincipalItem>();
        private List<string> _entities;   // logical names, loaded once and cached

        private PrincipalPrivilegeSet _lastSet;
        private GapVerdict _lastVerdict;
        private Dictionary<string, GrantedPrivilege> _lastEffective;
        private string _lastTable;
        private CrmOperation _lastOperation;
        private AccessScope _lastRequiredScope;

        // Powers "Report a bug" / help links in XrmToolBox
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        public PrivilegeGapAnalyzerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Privilege Gap Analyzer"));
        }

        #region Lifecycle

        private void PrivilegeGapAnalyzerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();

            cboPrincipalType.Items.Clear();
            cboPrincipalType.Items.AddRange(new object[] { PrincipalKind.User, PrincipalKind.Team, PrincipalKind.Role });

            cboOperation.Items.Clear();
            foreach (CrmOperation op in Enum.GetValues(typeof(CrmOperation)))
                cboOperation.Items.Add(op);

            cboScope.Items.Clear();
            cboScope.Items.AddRange(new object[]
            {
                AccessScope.Basic, AccessScope.Local, AccessScope.Deep, AccessScope.Global
            });

            // Restore prior selections
            cboPrincipalType.SelectedItem = ParseEnum(_settings.LastPrincipalType, PrincipalKind.User);
            cboOperation.SelectedItem = ParseEnum(_settings.LastOperation, CrmOperation.Read);
            cboScope.SelectedItem = ParseEnum(_settings.LastRequiredScope, AccessScope.Basic);
            if (!string.IsNullOrEmpty(_settings.LastTable)) cboTable.Text = _settings.LastTable;
            if (!string.IsNullOrEmpty(_settings.LastRelatedTable)) cboRelatedTable.Text = _settings.LastRelatedTable;

            UpdateRelatedTableEnabled();
            LogInfo("Privilege Gap Analyzer loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.LastPrincipalType = cboPrincipalType.SelectedItem?.ToString();
            _settings.LastPrincipalName = (cboPrincipal.SelectedItem as PrincipalItem)?.Name;
            _settings.LastTable = cboTable.Text;
            _settings.LastRelatedTable = cboRelatedTable.Text;
            _settings.LastOperation = cboOperation.SelectedItem?.ToString();
            _settings.LastRequiredScope = cboScope.SelectedItem?.ToString();
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
            MetadataCache.Clear();          // metadata & privileges differ between environments
            _entities = null;               // force reload of the table list
            _principals.Clear();
            cboPrincipal.Items.Clear();
            ResetResults();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        #endregion

        #region Selector events

        private void cboPrincipalType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Principal list depends on the type; require an explicit reload (needs a connection).
            _principals.Clear();
            cboPrincipal.Items.Clear();
            cboPrincipal.Text = string.Empty;
        }

        private void cboOperation_SelectedIndexChanged(object sender, EventArgs e) => UpdateRelatedTableEnabled();

        private void UpdateRelatedTableEnabled()
        {
            bool isAppend = (cboOperation.SelectedItem is CrmOperation op) && op == CrmOperation.Append;
            cboRelatedTable.Enabled = isAppend;
            if (!isAppend) return;
        }

        #endregion

        #region Load reference data (principals + tables)

        private void tsbLoad_Click(object sender, EventArgs e) => ExecuteMethod(LoadReferenceData);

        private void LoadReferenceData()
        {
            var kind = cboPrincipalType.SelectedItem is PrincipalKind k ? k : PrincipalKind.User;
            bool needEntities = _entities == null;

            RunAsync(
                "Loading principals and tables...",
                worker =>
                {
                    var result = new ReferenceData();

                    if (needEntities)
                    {
                        worker.ReportProgress(0, "Loading table list...");
                        result.Entities = LoadEntityNames();
                    }

                    worker.ReportProgress(0, $"Loading {kind}s...");
                    result.Principals = LoadPrincipals(kind, worker);
                    return result;
                },
                result =>
                {
                    if (result.Entities != null)
                    {
                        _entities = result.Entities;
                        cboTable.Items.Clear();
                        cboRelatedTable.Items.Clear();
                        cboTable.Items.AddRange(_entities.Cast<object>().ToArray());
                        cboRelatedTable.Items.AddRange(_entities.Cast<object>().ToArray());
                        if (!string.IsNullOrEmpty(_settings.LastTable)) cboTable.Text = _settings.LastTable;
                        if (!string.IsNullOrEmpty(_settings.LastRelatedTable)) cboRelatedTable.Text = _settings.LastRelatedTable;
                    }

                    _principals.Clear();
                    _principals.AddRange(result.Principals);
                    cboPrincipal.Items.Clear();
                    cboPrincipal.Items.AddRange(_principals.Cast<object>().ToArray());

                    // restore last principal by name
                    if (!string.IsNullOrEmpty(_settings.LastPrincipalName))
                    {
                        var match = _principals.FirstOrDefault(p =>
                            string.Equals(p.Name, _settings.LastPrincipalName, StringComparison.OrdinalIgnoreCase));
                        if (match != null) cboPrincipal.SelectedItem = match;
                    }

                    SetStatusMessage($"Loaded {_principals.Count} {kind.ToString().ToLowerInvariant()}(s)" +
                                     (_entities != null ? $" and {_entities.Count} tables" : string.Empty));
                });
        }

        private List<string> LoadEntityNames()
        {
            var resp = (RetrieveAllEntitiesResponse)Service.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = false
            });
            return resp.EntityMetadata
                .Where(m => !string.IsNullOrEmpty(m.LogicalName))
                .Select(m => m.LogicalName)
                .Distinct()
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private List<PrincipalItem> LoadPrincipals(PrincipalKind kind, BackgroundWorker worker)
        {
            switch (kind)
            {
                case PrincipalKind.Team:
                {
                    var q = new QueryExpression("team") { ColumnSet = new ColumnSet("name") };
                    q.AddOrder("name", OrderType.Ascending);
                    return Service.RetrieveAll(q, null, worker)
                        .Select(t => new PrincipalItem(t.Id, t.GetAttributeValue<string>("name")))
                        .Where(p => !string.IsNullOrEmpty(p.Name))
                        .ToList();
                }
                case PrincipalKind.Role:
                {
                    var q = new QueryExpression("role") { ColumnSet = new ColumnSet("name") };
                    q.AddOrder("name", OrderType.Ascending);
                    // roles are duplicated per business unit; list distinct names (privileges match across copies)
                    return Service.RetrieveAll(q, null, worker)
                        .Select(r => new PrincipalItem(r.Id, r.GetAttributeValue<string>("name")))
                        .Where(p => !string.IsNullOrEmpty(p.Name))
                        .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToList();
                }
                default:
                {
                    var q = new QueryExpression("systemuser") { ColumnSet = new ColumnSet("fullname") };
                    q.Criteria.AddCondition("isdisabled", ConditionOperator.Equal, false);
                    q.AddOrder("fullname", OrderType.Ascending);
                    return Service.RetrieveAll(q, null, worker)
                        .Select(u => new PrincipalItem(u.Id, u.GetAttributeValue<string>("fullname")))
                        .Where(p => !string.IsNullOrEmpty(p.Name))
                        .ToList();
                }
            }
        }

        #endregion

        #region Analyze

        private void tsbAnalyze_Click(object sender, EventArgs e) => ExecuteMethod(RunAnalyze);

        private void RunAnalyze()
        {
            if (!TryGetSelection(out var kind, out var principal, out var table, out var op, out var scope, out var related))
                return;

            RunAsync(
                "Analyzing effective privileges...",
                worker =>
                {
                    Action<string> progress = s => worker.ReportProgress(0, s);
                    var collector = new PrivilegeCollector(Service);

                    var set = collector.Build(kind, principal.Id, worker, progress);
                    var target = collector.BuildEntityPrivilege(table);
                    EntityPrivilege appendTo = null;
                    if (op == CrmOperation.Append && !string.IsNullOrWhiteSpace(related))
                    {
                        progress($"Reading privileges for related table {related}...");
                        appendTo = collector.BuildEntityPrivilege(related);
                    }

                    var verdict = PrivilegeEngine.Evaluate(set, target, op, scope, appendTo);
                    var effective = PrivilegeEngine.ResolveEffective(set);
                    return new AnalysisResult
                    {
                        Set = set,
                        Verdict = verdict,
                        Effective = effective,
                        Table = table,
                        Operation = op,
                        RequiredScope = scope
                    };
                },
                result =>
                {
                    _lastSet = result.Set;
                    _lastVerdict = result.Verdict;
                    _lastEffective = result.Effective;
                    _lastTable = result.Table;
                    _lastOperation = result.Operation;
                    _lastRequiredScope = result.RequiredScope;

                    BindVerdict(result);
                    tsbExport.Enabled = true;
                    SetStatusMessage(result.Verdict.Allowed ? "Access allowed" : "Access denied — see explanation");
                });
        }

        private bool TryGetSelection(out PrincipalKind kind, out PrincipalItem principal, out string table,
            out CrmOperation op, out AccessScope scope, out string related)
        {
            kind = cboPrincipalType.SelectedItem is PrincipalKind k ? k : PrincipalKind.User;
            principal = cboPrincipal.SelectedItem as PrincipalItem;
            table = (cboTable.Text ?? string.Empty).Trim();
            op = cboOperation.SelectedItem is CrmOperation o ? o : CrmOperation.Read;
            scope = cboScope.SelectedItem is AccessScope s ? s : AccessScope.Basic;
            related = (cboRelatedTable.Text ?? string.Empty).Trim();

            if (principal == null)
            {
                Warn("Select a principal (click 'Load principals' first).");
                return false;
            }
            if (string.IsNullOrEmpty(table))
            {
                Warn("Select or type a table (logical name).");
                return false;
            }
            return true;
        }

        private void BindVerdict(AnalysisResult result)
        {
            var v = result.Verdict;
            lblVerdict.Text = (v.Allowed ? "✔ ALLOWED" : "✖ DENIED") +
                              $"  —  {SplitType(v.Type)}   ({result.Set.PrincipalType} '{result.Set.PrincipalName}' → " +
                              $"{result.Operation} on '{result.Table}')";
            var green = Color.FromArgb(223, 240, 216);
            var red = Color.FromArgb(242, 222, 222);
            var amber = Color.FromArgb(252, 248, 227);
            pnlVerdict.BackColor = !v.Allowed ? red
                : v.Type == GapVerdictType.TeamInheritanceOnly ? amber : green;
            lblVerdict.ForeColor = !v.Allowed ? Color.FromArgb(169, 68, 66)
                : v.Type == GapVerdictType.TeamInheritanceOnly ? Color.FromArgb(138, 109, 59)
                : Color.FromArgb(60, 118, 61);

            grdEffective.Rows.Clear();
            foreach (var g in result.Effective.Values.OrderBy(x => x.PrivilegeName, StringComparer.OrdinalIgnoreCase))
            {
                grdEffective.Rows.Add(
                    g.PrivilegeName,
                    ScopeText(g.Scope),
                    g.SourceRole,
                    g.SourceTeam ?? string.Empty,
                    g.ViaTeam ? "Yes" : "No");
            }

            txtExplanation.Text = v.Explanation;
            txtRecommendation.Text = v.Recommendation;
        }

        #endregion

        #region Compare

        private void tsbCompare_Click(object sender, EventArgs e)
        {
            var first = cboPrincipal.SelectedItem as PrincipalItem;
            if (first == null)
            {
                Warn("Select the first principal, then click Compare.");
                return;
            }
            var second = PromptSecondPrincipal(first);
            if (second == null) return;

            var kind = cboPrincipalType.SelectedItem is PrincipalKind k ? k : PrincipalKind.User;
            ExecuteMethod(() => RunCompare(kind, first, second));
        }

        private void RunCompare(PrincipalKind kind, PrincipalItem a, PrincipalItem b)
        {
            RunAsync(
                "Comparing effective privileges...",
                worker =>
                {
                    Action<string> progress = s => worker.ReportProgress(0, s);
                    var collector = new PrivilegeCollector(Service);
                    var setA = collector.Build(kind, a.Id, worker, progress);
                    var setB = collector.Build(kind, b.Id, worker, progress);
                    return new CompareResult { A = setA, B = setB, Diff = PrivilegeEngine.Diff(setA, setB) };
                },
                result =>
                {
                    ShowCompareDialog(result);
                    SetStatusMessage($"{result.Diff.Count} privilege difference(s) between the two principals");
                });
        }

        private PrincipalItem PromptSecondPrincipal(PrincipalItem exclude)
        {
            var options = _principals.Where(p => p.Id != exclude.Id).Cast<object>().ToArray();
            if (options.Length == 0)
            {
                Warn("Load principals and ensure at least two exist before comparing.");
                return null;
            }

            using (var dlg = new Form
            {
                Text = "Compare with…",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(360, 96)
            })
            {
                var lbl = new Label { Text = "Second principal:", AutoSize = true, Left = 12, Top = 14 };
                var cbo = new ComboBox
                {
                    Left = 12, Top = 34, Width = 336,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cbo.Items.AddRange(options);
                cbo.SelectedIndex = 0;
                var ok = new Button { Text = "Compare", DialogResult = DialogResult.OK, Left = 192, Top = 64, Width = 75 };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 273, Top = 64, Width = 75 };
                dlg.Controls.AddRange(new Control[] { lbl, cbo, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                return dlg.ShowDialog(this) == DialogResult.OK ? cbo.SelectedItem as PrincipalItem : null;
            }
        }

        private void ShowCompareDialog(CompareResult result)
        {
            using (var dlg = new Form
            {
                Text = $"Privilege diff: {result.A.PrincipalName} vs {result.B.PrincipalName}",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(720, 460)
            })
            {
                var grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    RowHeadersVisible = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect
                };
                grid.Columns.Add("Privilege", "Privilege");
                grid.Columns.Add("A", $"{Truncate(result.A.PrincipalName, 24)}");
                grid.Columns.Add("B", $"{Truncate(result.B.PrincipalName, 24)}");
                grid.Columns[0].FillWeight = 50;

                if (result.Diff.Count == 0)
                    grid.Rows.Add("(no differences)", "", "");
                foreach (var d in result.Diff)
                    grid.Rows.Add(d.privilege, ScopeText(d.a), ScopeText(d.b));

                dlg.Controls.Add(grid);
                dlg.ShowDialog(this);
            }
        }

        #endregion

        #region Export

        private void miExportExcel_Click(object sender, EventArgs e) => Export("xlsx");
        private void miExportPdf_Click(object sender, EventArgs e) => Export("pdf");
        private void miExportCsv_Click(object sender, EventArgs e) => Export("csv");
        private void miExportJson_Click(object sender, EventArgs e) => Export("json");
        private void miExportHtml_Click(object sender, EventArgs e) => Export("html");

        private void Export(string kind)
        {
            if (_lastVerdict == null)
            {
                Warn("Run an analysis before exporting.");
                return;
            }

            string filter = kind == "xlsx" ? "Excel workbook (*.xlsx)|*.xlsx"
                : kind == "pdf" ? "PDF document (*.pdf)|*.pdf"
                : kind == "csv" ? "CSV (*.csv)|*.csv"
                : kind == "json" ? "JSON (*.json)|*.json"
                : "HTML report (*.html)|*.html";
            string defaultName = $"PrivilegeGap_{Sanitize(_lastTable)}_{_lastOperation}_{DateTime.Now:yyyyMMdd_HHmm}.{kind}";

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    switch (kind)
                    {
                        case "xlsx": ExcelReportExporter.Export(BuildReportModel(), dlg.FileName); break;
                        case "pdf": PdfReportExporter.Export(BuildReportModel(), dlg.FileName); break;
                        case "json": JsonReportExporter.Export(BuildReportModel(), dlg.FileName); break;
                        case "html": WriteHtml(dlg.FileName); break;
                        default: WriteCsv(dlg.FileName); break;
                    }
                    if (MessageBox.Show(this, "Report exported. Open it now?", "Privilege Gap Analyzer",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        System.Diagnostics.Process.Start(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex); }
            }
        }

        /// <summary>Projects the verdict into the shared ReportModel (one Finding for the gap). Principal name masked.</summary>
        private ReportModel BuildReportModel()
        {
            var v = _lastVerdict;
            var model = new ReportModel
            {
                ToolName = "Privilege Gap Analyzer",
                ToolVersion = GetType().Assembly.GetName().Version?.ToString(),
                ReportTitle = "Privilege Gap Report",
                Subtitle = "Effective-privilege diagnosis",
                ScoreWord = "privilege",
                SubjectName = Mask(_lastSet.PrincipalName),
                SubjectKey = _lastSet.PrincipalType,
                LeadIn = v.Explanation
            };

            Severity sev;
            if (!v.Allowed) { model.Score = 80; model.Band = ScoreBand.High; sev = Severity.High; }
            else if (v.Type == GapVerdictType.TeamInheritanceOnly) { model.Score = 40; model.Band = ScoreBand.Medium; sev = Severity.Medium; }
            else { model.Score = 0; model.Band = ScoreBand.Low; sev = Severity.Info; }

            model.Findings.Add(new Finding(
                category: "Privilege Gap",
                severity: sev,
                title: SplitType(v.Type),
                description: v.Explanation,
                component: $"{_lastTable} / {_lastOperation}",
                recommendation: v.Recommendation));

            model.Metrics.Add(new MetricRow("Verdict", v.Allowed ? "Allowed" : "Denied"));
            model.Metrics.Add(new MetricRow("Operation", _lastOperation.ToString()));
            model.Metrics.Add(new MetricRow("Table", _lastTable));
            model.Metrics.Add(new MetricRow("Required scope", ScopeText(v.RequiredScope)));
            model.Metrics.Add(new MetricRow("Held scope", ScopeText(v.HeldScope)));
            model.Metrics.Add(new MetricRow("Required privilege", v.RequiredPrivilege ?? "(none)"));
            return model;
        }

        private void WriteCsv(string path)
        {
            var v = _lastVerdict;
            var sb = new StringBuilder();
            sb.AppendLine("Privilege Gap Report");
            sb.AppendLine($"Principal,{Csv(Mask(_lastSet.PrincipalName))}");
            sb.AppendLine($"Principal type,{Csv(_lastSet.PrincipalType)}");
            sb.AppendLine($"Table,{Csv(_lastTable)}");
            sb.AppendLine($"Operation,{Csv(_lastOperation.ToString())}");
            sb.AppendLine($"Verdict,{(v.Allowed ? "Allowed" : "Denied")}");
            sb.AppendLine($"Verdict type,{Csv(SplitType(v.Type))}");
            sb.AppendLine($"Required privilege,{Csv(v.RequiredPrivilege)}");
            sb.AppendLine($"Required scope,{Csv(ScopeText(v.RequiredScope))}");
            sb.AppendLine($"Held scope,{Csv(ScopeText(v.HeldScope))}");
            sb.AppendLine($"Explanation,{Csv(v.Explanation)}");
            sb.AppendLine($"Recommendation,{Csv(v.Recommendation)}");
            sb.AppendLine();
            sb.AppendLine("Effective privileges");
            sb.AppendLine("Privilege,Scope,Source role,Source team,Via team");
            foreach (var g in _lastEffective.Values.OrderBy(x => x.PrivilegeName, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine(string.Join(",",
                    Csv(g.PrivilegeName), Csv(ScopeText(g.Scope)), Csv(g.SourceRole),
                    Csv(g.SourceTeam), g.ViaTeam ? "Yes" : "No"));

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private void WriteHtml(string path)
        {
            var v = _lastVerdict;
            string color = !v.Allowed ? "#a94442" : v.Type == GapVerdictType.TeamInheritanceOnly ? "#8a6d3b" : "#3c763d";
            string bg = !v.Allowed ? "#f2dede" : v.Type == GapVerdictType.TeamInheritanceOnly ? "#fcf8e7" : "#dff0d8";
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Privilege Gap Report</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                          "h1{font-size:20px}table{border-collapse:collapse;margin-top:8px;width:100%}" +
                          "th,td{border:1px solid #ccc;padding:6px 8px;text-align:left;font-size:13px}" +
                          "th{background:#f4f4f4}.verdict{padding:12px 16px;border-radius:6px;font-weight:bold;font-size:16px}" +
                          ".meta td:first-child{font-weight:bold;width:200px}</style></head><body>");
            sb.AppendLine("<h1>Privilege Gap Report</h1>");
            sb.AppendLine($"<div class=\"verdict\" style=\"background:{bg};color:{color}\">" +
                          $"{(v.Allowed ? "ALLOWED" : "DENIED")} — {H(SplitType(v.Type))}</div>");
            sb.AppendLine("<table class=\"meta\">");
            AppendRow(sb, "Principal", Mask(_lastSet.PrincipalName));
            AppendRow(sb, "Principal type", _lastSet.PrincipalType);
            AppendRow(sb, "Table / Operation", $"{_lastTable} / {_lastOperation}");
            AppendRow(sb, "Required privilege", v.RequiredPrivilege);
            AppendRow(sb, "Required scope", ScopeText(v.RequiredScope));
            AppendRow(sb, "Held scope", ScopeText(v.HeldScope));
            AppendRow(sb, "Explanation", v.Explanation);
            AppendRow(sb, "Recommendation", v.Recommendation);
            sb.AppendLine("</table>");

            sb.AppendLine("<h2 style=\"font-size:16px\">Effective privileges</h2>");
            sb.AppendLine("<table><tr><th>Privilege</th><th>Scope</th><th>Source role</th><th>Source team</th><th>Via team</th></tr>");
            foreach (var g in _lastEffective.Values.OrderBy(x => x.PrivilegeName, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"<tr><td>{H(g.PrivilegeName)}</td><td>{H(ScopeText(g.Scope))}</td>" +
                              $"<td>{H(g.SourceRole)}</td><td>{H(g.SourceTeam)}</td><td>{(g.ViaTeam ? "Yes" : "No")}</td></tr>");
            sb.AppendLine("</table>");
            sb.AppendLine($"<p style=\"color:#888;font-size:11px;margin-top:16px\">Generated {DateTime.Now:u} — " +
                          "read-only diagnosis; principal name masked. Recommendations are suggestions only.</p>");
            sb.AppendLine("</body></html>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static void AppendRow(StringBuilder sb, string k, string val) =>
            sb.AppendLine($"<tr><td>{H(k)}</td><td>{H(val)}</td></tr>");

        #endregion

        #region Helpers

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();

        private void ResetResults()
        {
            _lastSet = null; _lastVerdict = null; _lastEffective = null;
            grdEffective.Rows.Clear();
            txtExplanation.Clear();
            txtRecommendation.Clear();
            lblVerdict.Text = "No analysis yet — pick a principal, table, and operation, then click Analyze.";
            pnlVerdict.BackColor = SystemColors.ControlLight;
            lblVerdict.ForeColor = SystemColors.ControlText;
            tsbExport.Enabled = false;
        }

        private void Warn(string message) =>
            MessageBox.Show(this, message, "Privilege Gap Analyzer", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static string ScopeText(AccessScope s)
        {
            switch (s)
            {
                case AccessScope.Basic: return "Basic (User)";
                case AccessScope.Local: return "Local (BU)";
                case AccessScope.Deep: return "Deep (Parent:Child)";
                case AccessScope.Global: return "Global (Org)";
                default: return "None";
            }
        }

        private static string SplitType(GapVerdictType t)
        {
            // "InsufficientScope" -> "Insufficient Scope"
            var s = t.ToString();
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0 && char.IsUpper(s[i])) sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private static string Mask(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            var t = s.Trim();
            if (t.Length <= 2) return "*";
            return t[0] + new string('*', Math.Min(t.Length - 2, 6)) + t[t.Length - 1];
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max - 1) + "…");

        private static string Sanitize(string s) =>
            string.IsNullOrEmpty(s) ? "table" : new string(s.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

        private static string Csv(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            bool needsQuote = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            var v = s.Replace("\"", "\"\"");
            return needsQuote ? $"\"{v}\"" : v;
        }

        private static string H(string s) => string.IsNullOrEmpty(s)
            ? string.Empty
            : s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private static object ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct =>
            Enum.TryParse<TEnum>(value, out var parsed) ? (object)parsed : fallback;

        #endregion

        #region Inner types

        private sealed class PrincipalItem
        {
            public Guid Id { get; }
            public string Name { get; }
            public PrincipalItem(Guid id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }

        private sealed class ReferenceData
        {
            public List<string> Entities;
            public List<PrincipalItem> Principals = new List<PrincipalItem>();
        }

        private sealed class AnalysisResult
        {
            public PrincipalPrivilegeSet Set;
            public GapVerdict Verdict;
            public Dictionary<string, GrantedPrivilege> Effective;
            public string Table;
            public CrmOperation Operation;
            public AccessScope RequiredScope;
        }

        private sealed class CompareResult
        {
            public PrincipalPrivilegeSet A;
            public PrincipalPrivilegeSet B;
            public List<(string privilege, AccessScope a, AccessScope b)> Diff;
        }

        #endregion
    }

    /// <summary>Persisted UI state (POCO — no controls/services/credentials).</summary>
    public class ToolSettings
    {
        public string LastPrincipalType { get; set; }
        public string LastPrincipalName { get; set; }
        public string LastTable { get; set; }
        public string LastRelatedTable { get; set; }
        public string LastOperation { get; set; }
        public string LastRequiredScope { get; set; }
    }
}
