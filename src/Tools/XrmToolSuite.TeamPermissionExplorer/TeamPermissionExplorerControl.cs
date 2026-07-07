using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
using XrmToolSuite.Core.Privileges;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.TeamPermissionExplorer.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;
using Label = System.Windows.Forms.Label;

namespace XrmToolSuite.TeamPermissionExplorer
{
    public partial class TeamPermissionExplorerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;

        // All teams loaded from the org; the grid shows a type/search-filtered view.
        private readonly List<TeamProfile> _allTeams = new List<TeamProfile>();

        // Powers "Report a bug" / help links in XrmToolBox
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        // (display label, TeamType filter value or null for "All")
        private static readonly (string Label, string Value)[] TypeFilters =
        {
            ("All types", null),
            ("Owner", "Owner"),
            ("Access", "Access"),
            ("AAD security group", "AadSecurityGroup"),
            ("AAD office group", "AadOfficeGroup"),
        };

        public TeamPermissionExplorerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Team Permission Explorer"));
        }

        #region Lifecycle

        private void TeamPermissionExplorerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();

            tscbTeamType.Items.Clear();
            foreach (var f in TypeFilters) tscbTeamType.Items.Add(f.Label);
            var idx = Array.FindIndex(TypeFilters, f =>
                string.Equals(f.Value, _settings.TeamTypeFilter, StringComparison.OrdinalIgnoreCase));
            tscbTeamType.SelectedIndex = idx >= 0 ? idx : 0;

            LogInfo("Team Permission Explorer loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.TeamTypeFilter = SelectedTypeFilter();
            _settings.LastTeamName = (grdTeams.CurrentRow?.Tag as TeamProfile)?.Name;
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
            MetadataCache.Clear(); // metadata & privileges differ between environments
            _allTeams.Clear();
            grdTeams.Rows.Clear();
            ClearDetails();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        #endregion

        #region Load teams

        private void tsbLoad_Click(object sender, EventArgs e) => ExecuteMethod(LoadTeams);

        private void LoadTeams()
        {
            RunAsync(
                "Loading teams and permissions...",
                worker =>
                {
                    Action<string> progress = s => worker.ReportProgress(0, s);
                    var collector = new TeamCollector();
                    // Load ALL teams; type filtering happens in-memory so the combo is instant.
                    return collector.Collect(Service, null, worker, progress);
                },
                teams =>
                {
                    _allTeams.Clear();
                    _allTeams.AddRange(teams);
                    ApplyFilter();
                    SetStatusMessage($"Loaded {_allTeams.Count} team(s)");

                    // restore last selection
                    if (!string.IsNullOrEmpty(_settings.LastTeamName))
                        SelectTeamByName(_settings.LastTeamName);
                });
        }

        #endregion

        #region Filtering / grid

        private void tscbTeamType_SelectedIndexChanged(object sender, EventArgs e) => ApplyFilter();
        private void tstSearch_TextChanged(object sender, EventArgs e) => ApplyFilter();

        private string SelectedTypeFilter()
        {
            var i = tscbTeamType.SelectedIndex;
            return i >= 0 && i < TypeFilters.Length ? TypeFilters[i].Value : null;
        }

        private void ApplyFilter()
        {
            var typeFilter = SelectedTypeFilter();
            var search = (tstSearch.Text ?? string.Empty).Trim();

            var view = _allTeams.Where(t =>
                    (string.IsNullOrEmpty(typeFilter) ||
                     string.Equals(t.TeamType, typeFilter, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(search) ||
                     (t.Name ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     (t.BusinessUnit ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            grdTeams.SuspendLayout();
            grdTeams.Rows.Clear();
            foreach (var t in view)
            {
                var i = grdTeams.Rows.Add(
                    t.Name,
                    t.IsDefault ? $"{t.TeamType} (default)" : t.TeamType,
                    t.BusinessUnit,
                    t.MemberCount.ToString(),
                    t.RoleNames.Count.ToString(),
                    TopRisk(t));
                grdTeams.Rows[i].Tag = t;
            }
            grdTeams.ResumeLayout();

            if (grdTeams.Rows.Count == 0) ClearDetails();
        }

        private static string TopRisk(TeamProfile t)
        {
            if (t.Findings == null || t.Findings.Count == 0) return "";
            var worst = t.Findings.OrderByDescending(f => f.Severity).First();
            return worst.Severity == Severity.Info ? "OK" : $"{worst.Severity}: {worst.Title}";
        }

        private void SelectTeamByName(string name)
        {
            foreach (DataGridViewRow row in grdTeams.Rows)
            {
                if (row.Tag is TeamProfile t && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    grdTeams.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }

        private void grdTeams_SelectionChanged(object sender, EventArgs e)
        {
            var team = grdTeams.CurrentRow?.Tag as TeamProfile;
            if (team == null) return;
            BindDetails(team);
        }

        #endregion

        #region Detail binding

        private void ClearDetails()
        {
            lblHeader.Text = "Select a team to see members, roles, effective privileges, and risks.";
            grdMembers.Rows.Clear();
            grdRoles.Rows.Clear();
            grdEffective.Rows.Clear();
            grdOwned.Rows.Clear();
            grdFindings.Rows.Clear();
        }

        private void BindDetails(TeamProfile t)
        {
            lblHeader.Text = $"{t.Name}  —  {t.TeamType}{(t.IsDefault ? " (default)" : "")}  ·  BU: {t.BusinessUnit}  ·  " +
                             $"{t.MemberCount} member(s)  ·  {t.RoleNames.Count} role(s)  ·  {t.Grants.Count} grant(s)";

            // Roles
            grdRoles.Rows.Clear();
            foreach (var r in t.RoleNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                grdRoles.Rows.Add(r);

            // Effective table-privilege matrix (deepest scope per privilege)
            grdEffective.Rows.Clear();
            foreach (var g in t.Effective().Values.OrderBy(x => x.PrivilegeName, StringComparer.OrdinalIgnoreCase))
                grdEffective.Rows.Add(g.PrivilegeName, ScopeText(g.Scope), g.SourceRole);

            // Owned records
            grdOwned.Rows.Clear();
            foreach (var kv in t.OwnedRecordCounts.OrderByDescending(x => x.Value))
                grdOwned.Rows.Add(kv.Key, kv.Value.ToString());
            if (t.OwnedRecordCounts.Count == 0)
                grdOwned.Rows.Add("(none of the counted tables)", "0");

            // Findings
            grdFindings.Rows.Clear();
            foreach (var f in t.Findings.OrderByDescending(x => x.Severity))
            {
                var i = grdFindings.Rows.Add(f.Severity.ToString(), f.Title, f.Description);
                grdFindings.Rows[i].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }

            // Members / inheriting users — fetched on demand (Dataverse read off the UI thread)
            grdMembers.Rows.Clear();
            if (t.MemberCount == 0)
                grdMembers.Rows.Add("(no members)");
            else
                LoadMembers(t);
        }

        private void LoadMembers(TeamProfile t)
        {
            if (!Guid.TryParse(t.TeamId, out var teamId)) return;
            RunAsync(
                "Loading team members...",
                worker => new TeamCollector().InheritingUsers(Service, teamId, worker),
                users =>
                {
                    // Only apply if the same team is still selected.
                    if ((grdTeams.CurrentRow?.Tag as TeamProfile)?.TeamId != t.TeamId) return;
                    grdMembers.Rows.Clear();
                    if (users.Count == 0) { grdMembers.Rows.Add("(no members)"); return; }
                    foreach (var u in users) grdMembers.Rows.Add(u.User);
                });
        }

        #endregion

        #region Compare

        private void tsbCompare_Click(object sender, EventArgs e)
        {
            var first = grdTeams.CurrentRow?.Tag as TeamProfile;
            if (first == null)
            {
                Warn("Load teams and select the first team, then click Compare.");
                return;
            }
            var second = PromptSecondTeam(first);
            if (second == null) return;
            ShowCompareDialog(first, second);
        }

        private TeamProfile PromptSecondTeam(TeamProfile exclude)
        {
            var options = _allTeams.Where(t => t.TeamId != exclude.TeamId).ToArray();
            if (options.Length == 0)
            {
                Warn("Load teams and ensure at least two exist before comparing.");
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
                var lbl = new Label { Text = "Second team:", AutoSize = true, Left = 12, Top = 14 };
                var cbo = new ComboBox { Left = 12, Top = 34, Width = 336, DropDownStyle = ComboBoxStyle.DropDownList };
                cbo.Items.AddRange(options.Select(t => (object)t.Name).ToArray());
                cbo.SelectedIndex = 0;
                var ok = new Button { Text = "Compare", DialogResult = DialogResult.OK, Left = 192, Top = 64, Width = 75 };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 273, Top = 64, Width = 75 };
                dlg.Controls.AddRange(new Control[] { lbl, cbo, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;
                if (dlg.ShowDialog(this) != DialogResult.OK) return null;
                return options[cbo.SelectedIndex];
            }
        }

        private void ShowCompareDialog(TeamProfile a, TeamProfile b)
        {
            var diff = PrivilegeEngine.Diff(ToSet(a), ToSet(b));
            var rolesOnlyA = a.RoleNames.Except(b.RoleNames, StringComparer.OrdinalIgnoreCase).ToList();
            var rolesOnlyB = b.RoleNames.Except(a.RoleNames, StringComparer.OrdinalIgnoreCase).ToList();

            using (var dlg = new Form
            {
                Text = $"Team diff: {a.Name} vs {b.Name}",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(760, 500)
            })
            {
                var lblRoles = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 54,
                    Padding = new Padding(6),
                    Text = $"Roles only in '{a.Name}': {(rolesOnlyA.Count == 0 ? "(none)" : string.Join(", ", rolesOnlyA))}\r\n" +
                           $"Roles only in '{b.Name}': {(rolesOnlyB.Count == 0 ? "(none)" : string.Join(", ", rolesOnlyB))}"
                };

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
                grid.Columns.Add("A", Truncate(a.Name, 26));
                grid.Columns.Add("B", Truncate(b.Name, 26));
                grid.Columns[0].FillWeight = 50;

                if (diff.Count == 0) grid.Rows.Add("(no effective-privilege differences)", "", "");
                foreach (var d in diff) grid.Rows.Add(d.privilege, ScopeText(d.a), ScopeText(d.b));

                dlg.Controls.Add(grid);
                dlg.Controls.Add(lblRoles);
                dlg.ShowDialog(this);
            }
            SetStatusMessage($"{diff.Count} privilege difference(s) between '{a.Name}' and '{b.Name}'");
        }

        private static PrincipalPrivilegeSet ToSet(TeamProfile t) => new PrincipalPrivilegeSet
        {
            PrincipalName = t.Name,
            PrincipalType = "Team",
            Grants = t.Grants
        };

        #endregion

        #region Export

        private void miExportExcel_Click(object sender, EventArgs e) => Export("xlsx");
        private void miExportPdf_Click(object sender, EventArgs e) => Export("pdf");
        private void miExportCsv_Click(object sender, EventArgs e) => Export("csv");
        private void miExportHtml_Click(object sender, EventArgs e) => Export("html");

        private void Export(string kind)
        {
            if (_allTeams.Count == 0)
            {
                Warn("Load teams before exporting.");
                return;
            }

            string filter = kind == "xlsx" ? "Excel workbook (*.xlsx)|*.xlsx"
                : kind == "pdf" ? "PDF document (*.pdf)|*.pdf"
                : kind == "csv" ? "CSV (*.csv)|*.csv"
                : "HTML report (*.html)|*.html";
            string defaultName = $"TeamPermissions_{DateTime.Now:yyyyMMdd_HHmm}.{kind}";

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var path = dlg.FileName;

                RunAsync(
                    "Exporting team security report...",
                    worker =>
                    {
                        switch (kind)
                        {
                            case "xlsx": ExcelReportExporter.Export(BuildReportModel(), path); break;
                            case "pdf": PdfReportExporter.Export(BuildReportModel(), path); break;
                            case "html": WriteHtml(path); break;
                            default: WriteCsv(path); break;
                        }
                        return path;
                    },
                    written =>
                    {
                        if (MessageBox.Show(this, "Report exported. Open it now?", "Team Permission Explorer",
                                MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                            System.Diagnostics.Process.Start(written);
                        SetStatusMessage($"Exported {_allTeams.Count} team(s) to {Path.GetFileName(written)}");
                    });
            }
        }

        /// <summary>Projects every team's findings into the shared ReportModel (team name as Component).</summary>
        private ReportModel BuildReportModel()
        {
            var model = new ReportModel
            {
                ToolName = "Team Permission Explorer",
                ToolVersion = GetType().Assembly.GetName().Version?.ToString(),
                ReportTitle = "Team Security Report",
                Subtitle = "Team access, membership, and inheritance",
                ScoreWord = "risk",
                SubjectName = "All teams",
                LeadIn = "Per-team members, roles, effective privileges, owned records, and risk findings."
            };

            foreach (var t in _allTeams)
                foreach (var f in t.Findings)
                    model.Findings.Add(new Finding(
                        category: f.Category,
                        severity: f.Severity,
                        title: f.Title,
                        description: f.Description,
                        component: t.Name,
                        recommendation: f.Recommendation));

            int high = model.CountBySeverity(Severity.Critical) + model.CountBySeverity(Severity.High);
            int medium = model.CountBySeverity(Severity.Medium);
            if (high > 0) { model.Band = ScoreBand.High; model.Score = Math.Min(90, 60 + high * 5); }
            else if (medium > 0) { model.Band = ScoreBand.Medium; model.Score = Math.Min(59, 30 + medium * 3); }
            else { model.Band = ScoreBand.Low; model.Score = 10; }

            model.Metrics.Add(new MetricRow("Teams", _allTeams.Count.ToString()));
            model.Metrics.Add(new MetricRow("Empty teams",
                _allTeams.Count(t => t.MemberCount == 0 && !t.IsAadGroupTeam).ToString()));
            model.Metrics.Add(new MetricRow("Roleless teams",
                _allTeams.Count(t => t.RoleNames.Count == 0).ToString()));
            model.Metrics.Add(new MetricRow("High-risk findings", high.ToString()));
            model.Metrics.Add(new MetricRow("Medium-risk findings", medium.ToString()));
            return model;
        }

        private void WriteCsv(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Team,Type,IsDefault,BusinessUnit,Members,Roles,OwnedRecords,Severity,Finding,Evidence,Recommendation");
            foreach (var t in _allTeams)
            {
                foreach (var f in t.Findings.OrderByDescending(x => x.Severity))
                {
                    sb.AppendLine(string.Join(",",
                        Csv(t.Name), Csv(t.TeamType), t.IsDefault ? "Yes" : "No", Csv(t.BusinessUnit),
                        t.MemberCount, t.RoleNames.Count, t.TotalOwnedRecords,
                        Csv(f.Severity.ToString()), Csv(f.Title), Csv(f.Description), Csv(f.Recommendation)));
                }
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private void WriteHtml(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Team Security Report</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                          "h1{font-size:20px}h2{font-size:15px;margin-top:22px}" +
                          "table{border-collapse:collapse;margin-top:8px;width:100%}" +
                          "th,td{border:1px solid #ccc;padding:5px 8px;text-align:left;font-size:12px;vertical-align:top}" +
                          "th{background:#f4f4f4}.sev-High,.sev-Critical{color:#a94442;font-weight:bold}" +
                          ".sev-Medium{color:#8a6d3b}.sev-Low{color:#31708f}.sev-Info{color:#3c763d}</style></head><body>");
            sb.AppendLine("<h1>Team Security Report</h1>");
            sb.AppendLine($"<p style=\"color:#666\">Generated {DateTime.Now:u} — {_allTeams.Count} team(s). Read-only; names and counts only.</p>");

            foreach (var t in _allTeams.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"<h2>{H(t.Name)} <span style=\"font-weight:normal;color:#666\">— {H(t.TeamType)}" +
                              $"{(t.IsDefault ? " (default)" : "")}, BU {H(t.BusinessUnit)}, {t.MemberCount} member(s), " +
                              $"{t.RoleNames.Count} role(s), {t.TotalOwnedRecords} owned record(s)</span></h2>");
                sb.AppendLine("<table><tr><th>Severity</th><th>Finding</th><th>Evidence</th><th>Recommendation</th></tr>");
                foreach (var f in t.Findings.OrderByDescending(x => x.Severity))
                    sb.AppendLine($"<tr><td class=\"sev-{f.Severity}\">{f.Severity}</td><td>{H(f.Title)}</td>" +
                                  $"<td>{H(f.Description)}</td><td>{H(f.Recommendation)}</td></tr>");
                sb.AppendLine("</table>");
            }
            sb.AppendLine("</body></html>");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Helpers

        private void Warn(string message) =>
            MessageBox.Show(this, message, "Team Permission Explorer", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static Color SeverityColor(Severity s)
        {
            switch (s)
            {
                case Severity.Critical:
                case Severity.High: return Color.FromArgb(242, 222, 222);
                case Severity.Medium: return Color.FromArgb(252, 248, 227);
                case Severity.Low: return Color.FromArgb(217, 237, 247);
                default: return Color.FromArgb(223, 240, 216);
            }
        }

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

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? s : (s.Length <= max ? s : s.Substring(0, max - 1) + "…");

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

        #endregion
    }

    /// <summary>Persisted UI state (POCO — no controls/services/credentials).</summary>
    public class ToolSettings
    {
        public string TeamTypeFilter { get; set; }
        public string LastTeamName { get; set; }
    }
}
