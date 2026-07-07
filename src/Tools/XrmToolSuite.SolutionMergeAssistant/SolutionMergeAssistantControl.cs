using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.SolutionMergeAssistant.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;
// Microsoft.Xrm.Sdk also defines a SolutionInfo type; the tool uses its own SDK-free model (CS0104).
using SolutionInfo = XrmToolSuite.SolutionMergeAssistant.Analysis.SolutionInfo;

namespace XrmToolSuite.SolutionMergeAssistant
{
    public partial class SolutionMergeAssistantControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private MergeSettings _settings;
        private readonly List<SolutionListItem> _solutions = new List<SolutionListItem>();
        private MergeReport _report;
        private List<SolutionInfo> _compared = new List<SolutionInfo>();

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        public SolutionMergeAssistantControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Solution Merge Assistant"));
        }

        private void SolutionMergeAssistantControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<MergeSettings>();
            LogInfo("Solution Merge Assistant loaded");
            SetStatusMessage("Click 'Load solutions', check two or more, then 'Compare'.");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
                _settings.LastSelectedSolutionIds = CheckedSolutionIds().Select(g => g.ToString()).ToList();
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
            _solutions.Clear();
            lstSolutions.Items.Clear();
            ClearResults();
            SetStatusMessage($"Connected to {detail?.ConnectionName}. Click 'Load solutions'.");
        }

        // ----------------------------------------------------------------- Load solutions (needs connection)

        private void tsbLoadSolutions_Click(object sender, EventArgs e) => ExecuteMethod(LoadSolutions);

        private void LoadSolutions()
        {
            RunAsync(
                "Loading solutions...",
                worker =>
                {
                    var query = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("uniquename", "friendlyname", "version", "ismanaged", "publisherid"),
                        Criteria =
                        {
                            Conditions = { new ConditionExpression("isvisible", ConditionOperator.Equal, true) }
                        },
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };
                    return Service.RetrieveAll(query, worker: worker)
                        .Where(s =>
                        {
                            var u = s.GetAttributeValue<string>("uniquename");
                            return u != "Default" && u != "Active";
                        })
                        .Select(s => new SolutionListItem
                        {
                            Id = s.Id,
                            UniqueName = s.GetAttributeValue<string>("uniquename"),
                            FriendlyName = s.GetAttributeValue<string>("friendlyname"),
                            Version = s.GetAttributeValue<string>("version"),
                            IsManaged = s.GetAttributeValue<bool>("ismanaged")
                        })
                        .ToList();
                },
                items =>
                {
                    _solutions.Clear();
                    _solutions.AddRange(items);
                    lstSolutions.BeginUpdate();
                    lstSolutions.Items.Clear();
                    var restore = new HashSet<string>(_settings?.LastSelectedSolutionIds ?? new List<string>());
                    foreach (var item in _solutions)
                    {
                        int idx = lstSolutions.Items.Add(item);
                        if (restore.Contains(item.Id.ToString()))
                            lstSolutions.SetItemChecked(idx, true);
                    }
                    lstSolutions.EndUpdate();
                    ClearResults();
                    SetStatusMessage($"Loaded {_solutions.Count} solution(s). Check two or more and click 'Compare'.");
                });
        }

        // ----------------------------------------------------------------- Compare (needs connection)

        private void tsbCompare_Click(object sender, EventArgs e) => ExecuteMethod(CompareSolutions);

        private void CompareSolutions()
        {
            var ids = CheckedSolutionIds();
            if (ids.Count < 2)
            {
                MessageBox.Show(this,
                    "Check at least two solutions to compare (click 'Load solutions' if the list is empty).",
                    "Solution Merge Assistant", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RunAsync(
                "Comparing solutions...",
                worker =>
                {
                    var collector = new MergeCollector();
                    Action<string> progress = msg => worker.ReportProgress(0, msg);
                    var solutions = collector.LoadSolutions(Service, ids, worker, progress);
                    var configItems = collector.LoadConfigItems(Service, solutions, worker, progress);
                    progress("Evaluating merge conflicts...");
                    var report = MergeRules.Compare(solutions, configItems);
                    return new ComparisonResult { Solutions = solutions, Report = report };
                },
                result =>
                {
                    _compared = result.Solutions ?? new List<SolutionInfo>();
                    _report = result.Report;
                    RenderReport(_report);
                    tsbExport.Enabled = _report != null;
                    SetStatusMessage($"Compared {_compared.Count} solution(s): {_report.VerdictText} " +
                                     $"(score {_report.Score}/100, {_report.Findings.Count(f => f.Severity >= Severity.Low)} conflict(s)).");
                });
        }

        private void RenderReport(MergeReport report)
        {
            if (report == null) { ClearResults(); return; }

            // Verdict banner.
            lblVerdict.Text = $"  {report.VerdictText}   —   score {report.Score}/100 ({report.Band})";
            lblVerdict.BackColor = VerdictColor(report.Verdict);
            lblVerdict.ForeColor = report.Verdict == MergeVerdict.SafeToMerge || report.Verdict == MergeVerdict.MergeWithWarnings
                ? Color.FromArgb(20, 20, 20) : Color.White;

            // Conflicts grid, grouped by category (severity desc, then category, then component).
            grdConflicts.Rows.Clear();
            var conflicts = report.Findings
                .Where(f => f.Severity >= Severity.Low)
                .OrderByDescending(f => f.Severity)
                .ThenBy(f => f.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Component, StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var f in conflicts)
            {
                int idx = grdConflicts.Rows.Add(f.Severity.ToString(), f.Category, f.Component ?? "", f.Title);
                grdConflicts.Rows[idx].Cells[3].ToolTipText = f.Description ?? "";
                grdConflicts.Rows[idx].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
            lblConflicts.Text = $"Conflicts ({conflicts.Count}) — grouped by severity and category";

            txtStrategy.Text = report.RecommendedStrategy ?? "";
            txtChecklist.Text = string.Join("\r\n", report.Checklist ?? new List<string>());
        }

        // ----------------------------------------------------------------- Export

        private void tsmExportExcel_Click(object sender, EventArgs e) =>
            ExportWith("Excel (*.xlsx)|*.xlsx", "solution-merge.xlsx",
                path => ExcelReportExporter.Export(BuildReportModel(), path));

        private void tsmExportPdf_Click(object sender, EventArgs e) =>
            ExportWith("PDF (*.pdf)|*.pdf", "solution-merge.pdf",
                path => PdfReportExporter.Export(BuildReportModel(), path));

        private void tsmExportJson_Click(object sender, EventArgs e) =>
            ExportWith("JSON (*.json)|*.json", "solution-merge.json",
                path => System.IO.File.WriteAllText(path, BuildJson(), Encoding.UTF8));

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportWith("HTML (*.html)|*.html", "solution-merge.html",
                path => HtmlDashboardBuilder.Export(BuildReportModel(), path));

        private void ExportWith(string filter, string fileName, Action<string> writer)
        {
            if (_report == null)
            {
                MessageBox.Show(this, "Compare solutions first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = fileName })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                var path = dlg.FileName;
                RunAsync(
                    "Exporting merge report...",
                    worker => { writer(path); return path; },
                    saved => SetStatusMessage("Exported merge report to " + saved));
            }
        }

        /// <summary>Projects the merge report onto the shared <see cref="ReportModel"/> for Excel/PDF/HTML.</summary>
        private ReportModel BuildReportModel()
        {
            var subject = string.Join(", ", _compared.Select(s => s.UniqueName));
            var r = new ReportModel
            {
                ToolName = "Solution Merge Assistant",
                ReportTitle = "Solution Merge Report",
                ScoreWord = "merge risk",
                SubjectName = _compared.Count > 0 ? $"{_compared.Count} solutions" : "solutions",
                SubjectKey = subject,
                Score = _report.Score,
                Band = _report.Band,
                LeadIn = $"Verdict: {_report.VerdictText}. " +
                         "Read-only pairwise comparison of the selected solutions — this report recommends a merge " +
                         "strategy and lists every conflict; it does not import or write solutions.",
                VerdictLow = "Safe to merge — no significant conflicts detected.",
                VerdictMedium = "Review the conflicts below and reconcile before merging.",
                VerdictHigh = "Resolve the high-severity conflicts before attempting the merge."
            };

            foreach (var m in _report.Metrics) r.Metrics.Add(m);
            foreach (var f in _report.Findings.Where(x => x.Severity >= Severity.Low)) r.Findings.Add(f);
            foreach (var c in _report.Checklist) r.ChecklistGuidance.Add(c);
            if (!string.IsNullOrEmpty(_report.RecommendedStrategy))
                r.NextSteps.Add(new NextStep("Recommended merge strategy", _report.RecommendedStrategy));
            return r;
        }

        /// <summary>Machine-readable JSON carrying the verdict and the full conflict list.</summary>
        private string BuildJson()
        {
            var payload = new
            {
                tool = "Solution Merge Assistant",
                analyzedOnUtc = DateTime.UtcNow,
                verdict = _report.Verdict.ToString(),
                verdictText = _report.VerdictText,
                score = _report.Score,
                band = _report.Band.ToString(),
                recommendedStrategy = _report.RecommendedStrategy,
                solutions = _compared.Select(s => new
                {
                    uniqueName = s.UniqueName,
                    friendlyName = s.FriendlyName,
                    version = s.Version,
                    managed = s.IsManaged,
                    publisherPrefix = s.PublisherPrefix,
                    componentCount = s.Components?.Count ?? 0
                }),
                metrics = _report.Metrics.Select(m => new { label = m.Label, value = m.Value }),
                checklist = _report.Checklist,
                conflicts = _report.Findings
                    .OrderByDescending(f => f.Severity)
                    .Select(f => new
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

        private List<Guid> CheckedSolutionIds() =>
            lstSolutions.CheckedItems.Cast<SolutionListItem>().Select(i => i.Id).ToList();

        private void ClearResults()
        {
            _report = null;
            _compared = new List<SolutionInfo>();
            grdConflicts.Rows.Clear();
            txtStrategy.Text = "";
            txtChecklist.Text = "";
            lblConflicts.Text = "Conflicts — compare solutions to populate";
            lblVerdict.Text = "  Load and check 2+ solutions, then Compare.";
            lblVerdict.BackColor = SystemColors.Control;
            lblVerdict.ForeColor = SystemColors.ControlText;
            tsbExport.Enabled = false;
        }

        private static Color VerdictColor(MergeVerdict verdict)
        {
            switch (verdict)
            {
                case MergeVerdict.SafeToMerge: return Color.FromArgb(76, 175, 80);
                case MergeVerdict.MergeWithWarnings: return Color.FromArgb(174, 213, 129);
                case MergeVerdict.ManualReview: return Color.FromArgb(255, 202, 40);
                case MergeVerdict.HighRisk: return Color.FromArgb(245, 124, 0);
                case MergeVerdict.DoNotMerge: return Color.FromArgb(211, 47, 47);
                default: return SystemColors.Control;
            }
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

        /// <summary>A solution list-item shown in the checked list; ToString drives the row text.</summary>
        private sealed class SolutionListItem
        {
            public Guid Id { get; set; }
            public string UniqueName { get; set; }
            public string FriendlyName { get; set; }
            public string Version { get; set; }
            public bool IsManaged { get; set; }

            public override string ToString() =>
                $"{FriendlyName} ({UniqueName}) v{Version}" + (IsManaged ? " [managed]" : "");
        }

        /// <summary>Carries both the loaded solutions and the report back from the background thread.</summary>
        private sealed class ComparisonResult
        {
            public List<SolutionInfo> Solutions { get; set; }
            public MergeReport Report { get; set; }
        }
    }

    /// <summary>Serializable settings POCO round-tripped via BaseToolControl LoadSettings/SaveSettings.</summary>
    public class MergeSettings
    {
        /// <summary>Ids (string form) of the solutions checked last session, re-checked on load.</summary>
        public List<string> LastSelectedSolutionIds { get; set; } = new List<string>();
    }
}
