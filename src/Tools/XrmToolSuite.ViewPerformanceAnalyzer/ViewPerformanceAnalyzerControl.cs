using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.FetchXml;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.ViewPerformanceAnalyzer.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;
// PluginControlBase pulls in a Label; disambiguate to the WinForms one (CS0104 guard for future edits).
using Label = System.Windows.Forms.Label;

namespace XrmToolSuite.ViewPerformanceAnalyzer
{
    public partial class ViewPerformanceAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ViewSettings _settings;
        private List<ViewAnalysis> _views = new List<ViewAnalysis>();

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public ViewPerformanceAnalyzerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("View Performance Analyzer"));
        }

        private void ViewPerformanceAnalyzerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ViewSettings>();
            tsbIncludePersonal.Checked = _settings.IncludePersonal;
            LogInfo("View Performance Analyzer loaded");
            SetStatusMessage("Click 'Refresh tables', pick a table, then 'Analyze views'.");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
            {
                _settings.IncludePersonal = tsbIncludePersonal.Checked;
                _settings.LastEntity = SelectedEntity() ?? _settings.LastEntity;
            }
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
            cboEntity.Items.Clear();
            ClearResults();
            SetStatusMessage($"Connected to {detail?.ConnectionName}. Click 'Refresh tables'.");
        }

        // ----------------------------------------------------------------- Table picker (needs connection)

        private void tsbRefreshTables_Click(object sender, EventArgs e) => ExecuteMethod(LoadTables);

        private void LoadTables()
        {
            RunAsync(
                "Retrieving tables...",
                worker =>
                {
                    var response = (RetrieveAllEntitiesResponse)Service.Execute(new RetrieveAllEntitiesRequest
                    {
                        EntityFilters = EntityFilters.Entity,
                        RetrieveAsIfPublished = false
                    });

                    return response.EntityMetadata
                        .Where(m => m.IsValidForAdvancedFind == true) // tables users actually build views for
                        .Select(m => new EntityItem
                        {
                            LogicalName = m.LogicalName,
                            Display = m.DisplayName?.UserLocalizedLabel?.Label
                        })
                        .OrderBy(i => i.ToString(), StringComparer.OrdinalIgnoreCase)
                        .ToList();
                },
                items =>
                {
                    cboEntity.Items.Clear();
                    foreach (var i in items)
                        cboEntity.Items.Add(i);

                    // Restore the last-analyzed table if it's still present.
                    if (!string.IsNullOrEmpty(_settings?.LastEntity))
                    {
                        var match = items.FirstOrDefault(i =>
                            string.Equals(i.LogicalName, _settings.LastEntity, StringComparison.OrdinalIgnoreCase));
                        if (match != null) cboEntity.SelectedItem = match;
                    }

                    SetStatusMessage($"Loaded {items.Count} table(s). Pick one and click 'Analyze views'.");
                });
        }

        private string SelectedEntity() => (cboEntity.SelectedItem as EntityItem)?.LogicalName;

        // ----------------------------------------------------------------- Analyze views (needs connection)

        private void tsbAnalyze_Click(object sender, EventArgs e) => ExecuteMethod(AnalyzeViews);

        private void AnalyzeViews()
        {
            var entity = SelectedEntity();
            if (string.IsNullOrWhiteSpace(entity))
            {
                MessageBox.Show("Pick a table first (click 'Refresh tables' if the list is empty).",
                    "No table selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var includePersonal = tsbIncludePersonal.Checked;
            var options = OptionsFromSettings();

            RunAsync(
                $"Analyzing views for {entity}...",
                worker =>
                {
                    var collector = new ViewCollector(options);
                    return collector.Collect(Service, entity, includePersonal, worker,
                        msg => worker.ReportProgress(0, msg));
                },
                views =>
                {
                    _views = views ?? new List<ViewAnalysis>();
                    PopulateViewsGrid(_views);
                    txtSummary.Text = BuildEnvironmentSummary(entity, _views);
                    tsbExport.Enabled = _views.Count > 0;

                    if (_views.Count == 0)
                    {
                        SetStatusMessage($"No views found for {entity}.");
                        return;
                    }

                    grdViews.ClearSelection();
                    if (grdViews.Rows.Count > 0)
                        grdViews.Rows[0].Selected = true;

                    SetStatusMessage(
                        $"Analyzed {_views.Count} view(s) for {entity}. Worst score {_views.Max(v => v.Score)}/100.");
                });
        }

        private ViewScoreOptions OptionsFromSettings()
        {
            return new ViewScoreOptions
            {
                MaxLayoutColumns = _settings?.MaxLayoutColumns > 0 ? _settings.MaxLayoutColumns : 15,
                FetchOptions = new FetchXmlAnalysisOptions
                {
                    MaxAttributes = _settings?.MaxAttributes > 0 ? _settings.MaxAttributes : 30,
                    MaxLinkEntities = _settings?.MaxLinkEntities > 0 ? _settings.MaxLinkEntities : 4,
                    WarnLinkEntities = _settings?.WarnLinkEntities > 0 ? _settings.WarnLinkEntities : 2
                }
            };
        }

        private void PopulateViewsGrid(List<ViewAnalysis> views)
        {
            grdViews.Rows.Clear();
            foreach (var v in views)
            {
                int rowIndex = grdViews.Rows.Add(
                    v.Score,
                    v.Band.ToString(),
                    v.Name ?? "(unnamed)",
                    v.ViewType,
                    v.Entity,
                    v.FetchAttributeCount,
                    v.LayoutColumnCount,
                    v.LinkCount);
                grdViews.Rows[rowIndex].Tag = v;
                grdViews.Rows[rowIndex].DefaultCellStyle.BackColor = BandColor(v.Band);
            }
        }

        private void grdViews_SelectionChanged(object sender, EventArgs e)
        {
            var view = grdViews.SelectedRows.Count > 0
                ? grdViews.SelectedRows[0].Tag as ViewAnalysis
                : null;

            if (view == null)
            {
                grdFindings.Rows.Clear();
                txtFetchXml.Text = "";
                lstLayoutColumns.Items.Clear();
                lblFindingsHeader.Text = "Findings for the selected view";
                return;
            }

            lblFindingsHeader.Text = $"Findings for: {view.Name}";
            PopulateFindings(view);

            txtFetchXml.Text = view.FetchXml ?? "";

            lstLayoutColumns.BeginUpdate();
            lstLayoutColumns.Items.Clear();
            foreach (var c in view.LayoutColumns)
                lstLayoutColumns.Items.Add(c);
            lstLayoutColumns.EndUpdate();
            lblLayoutHeader.Text = $"Layout columns ({view.LayoutColumnCount})";
        }

        private void PopulateFindings(ViewAnalysis view)
        {
            grdFindings.Rows.Clear();
            foreach (var f in view.Findings.OrderByDescending(x => x.Severity))
            {
                int rowIndex = grdFindings.Rows.Add(
                    f.Severity.ToString(),
                    f.Title,
                    f.Component ?? "",
                    f.Recommendation ?? "");
                grdFindings.Rows[rowIndex].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
        }

        // ----------------------------------------------------------------- Time selected view (opt-in, read-only)

        private void tsbTime_Click(object sender, EventArgs e) => ExecuteMethod(TimeSelectedView);

        private void TimeSelectedView()
        {
            var view = grdViews.SelectedRows.Count > 0
                ? grdViews.SelectedRows[0].Tag as ViewAnalysis
                : null;

            if (view == null || string.IsNullOrWhiteSpace(view.FetchXml))
            {
                MessageBox.Show("Select a view with FetchXML first.", "Nothing to time",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RunAsync(
                $"Timing view '{view.Name}' (read-only)...",
                worker => ViewCollector.TimeView(Service, view.FetchXml, worker),
                r => SetStatusMessage(
                    $"'{view.Name}' executed in {r.ms} ms — {r.rows} row(s) (capped, read-only). " +
                    "Timing may reflect a limited result set."));
        }

        // ----------------------------------------------------------------- Export

        private void tsmExportExcel_Click(object sender, EventArgs e) =>
            ExportWith("Excel (*.xlsx)|*.xlsx", "view-performance.xlsx",
                path => ExcelReportExporter.Export(BuildReportModel(), path));

        private void tsmExportPdf_Click(object sender, EventArgs e) =>
            ExportWith("PDF (*.pdf)|*.pdf", "view-performance.pdf",
                path => PdfReportExporter.Export(BuildReportModel(), path));

        private void tsmExportJson_Click(object sender, EventArgs e) =>
            ExportWith("JSON (*.json)|*.json", "view-performance.json",
                path => JsonReportExporter.Export(BuildReportModel(), path));

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportWith("HTML (*.html)|*.html", "view-performance.html",
                path => System.IO.File.WriteAllText(path, BuildHtml(), Encoding.UTF8));

        private void tsmExportMarkdown_Click(object sender, EventArgs e) =>
            ExportWith("Markdown (*.md)|*.md", "view-performance.md",
                path => System.IO.File.WriteAllText(path, BuildMarkdown(), Encoding.UTF8));

        private void tsmExportCsv_Click(object sender, EventArgs e) =>
            ExportWith("CSV (*.csv)|*.csv", "view-performance.csv",
                path => System.IO.File.WriteAllText(path, BuildCsv(), Encoding.UTF8));

        private void ExportWith(string filter, string fileName, Action<string> writer)
        {
            if (_views == null || _views.Count == 0)
            {
                MessageBox.Show("Analyze a table's views first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = fileName })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    writer(dlg.FileName);
                    SetStatusMessage("Exported view analysis to " + dlg.FileName);
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }
        }

        private ReportModel BuildReportModel()
        {
            var entity = SelectedEntity() ?? _views.FirstOrDefault()?.Entity;
            int worst = _views.Count > 0 ? _views.Max(v => v.Score) : 0;

            var r = new ReportModel
            {
                ToolName = "View Performance Analyzer",
                ReportTitle = "View Performance Analysis",
                ScoreWord = "view cost",
                SubjectName = entity,
                Score = worst,
                Band = ScoreCalculator.BandFor(worst, 15, 40),
                LeadIn = "Heuristic per-view cost from the shared FetchXML engine plus LayoutXML column " +
                         "analysis. No server statistics are used — scores rank structural risk, not measured time."
            };

            r.Metrics.Add(new MetricRow("Views analyzed", _views.Count.ToString()));
            r.Metrics.Add(new MetricRow("System views", _views.Count(v => v.ViewType == "System").ToString()));
            r.Metrics.Add(new MetricRow("Personal views", _views.Count(v => v.ViewType == "Personal").ToString()));
            r.Metrics.Add(new MetricRow("High-band views", _views.Count(v => v.Band == ScoreBand.High).ToString()));
            r.Metrics.Add(new MetricRow("Medium-band views", _views.Count(v => v.Band == ScoreBand.Medium).ToString()));
            r.Metrics.Add(new MetricRow("Low-band views", _views.Count(v => v.Band == ScoreBand.Low).ToString()));
            var worstView = _views.OrderByDescending(v => v.Score).FirstOrDefault();
            if (worstView != null)
                r.Metrics.Add(new MetricRow("Worst view", worstView.Name, $"score {worstView.Score}/100"));

            // Aggregate every actionable finding across views, tagging each with its view name as Component.
            foreach (var v in _views)
                foreach (var f in v.Findings.Where(x => x.Severity >= Severity.Low))
                    r.Findings.Add(new Finding(f.Category, f.Severity, f.Title, f.Description,
                        component: v.Name, recommendation: f.Recommendation, helpUrl: f.HelpUrl));

            return r;
        }

        // ----------------------------------------------------------------- Summary + BCL exports

        private static string BuildEnvironmentSummary(string entity, List<ViewAnalysis> views)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Table: {entity}   Views: {views.Count} " +
                $"(System {views.Count(v => v.ViewType == "System")}, Personal {views.Count(v => v.ViewType == "Personal")})");
            sb.AppendLine($"Bands: High {views.Count(v => v.Band == ScoreBand.High)}   " +
                $"Medium {views.Count(v => v.Band == ScoreBand.Medium)}   " +
                $"Low {views.Count(v => v.Band == ScoreBand.Low)}   (heuristic estimate — no server statistics)");
            var top = views.OrderByDescending(v => v.Score).Take(3).ToList();
            if (top.Count > 0)
                sb.AppendLine("Worst: " + string.Join("   ", top.Select(v => $"{v.Name} ({v.Score})")));
            return sb.ToString();
        }

        private string BuildCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Score,Band,View,Type,Entity,FetchAttributes,LayoutColumns,LinkCount,AllAttributes,HighestSeverity");
            foreach (var v in _views)
            {
                var worst = v.Findings.Count > 0
                    ? v.Findings.Max(f => f.Severity).ToString()
                    : "Info";
                sb.AppendLine(string.Join(",", new[]
                {
                    v.Score.ToString(), Csv(v.Band.ToString()), Csv(v.Name), Csv(v.ViewType), Csv(v.Entity),
                    v.FetchAttributeCount.ToString(), v.LayoutColumnCount.ToString(), v.LinkCount.ToString(),
                    v.AllAttributes ? "yes" : "no", Csv(worst)
                }));
            }
            return sb.ToString();
        }

        private static string Csv(string s)
        {
            s = s ?? "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private string BuildMarkdown()
        {
            var entity = SelectedEntity() ?? _views.FirstOrDefault()?.Entity;
            var sb = new StringBuilder();
            sb.AppendLine($"# View Performance Analysis — {entity}");
            sb.AppendLine();
            sb.AppendLine($"- **Views analyzed:** {_views.Count} " +
                $"(System {_views.Count(v => v.ViewType == "System")}, Personal {_views.Count(v => v.ViewType == "Personal")})");
            sb.AppendLine($"- **Bands:** High {_views.Count(v => v.Band == ScoreBand.High)}, " +
                $"Medium {_views.Count(v => v.Band == ScoreBand.Medium)}, Low {_views.Count(v => v.Band == ScoreBand.Low)}");
            sb.AppendLine("- _Scores are labeled heuristic estimates (no server statistics)._");
            sb.AppendLine();
            sb.AppendLine("## Views (ranked by cost)");
            sb.AppendLine();
            sb.AppendLine("| Score | Band | View | Type | #Attrs | #Cols | #Links |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var v in _views)
                sb.AppendLine($"| {v.Score} | {v.Band} | {Md(v.Name)} | {v.ViewType} | " +
                    $"{v.FetchAttributeCount} | {v.LayoutColumnCount} | {v.LinkCount} |");
            return sb.ToString();
        }

        private static string Md(string s) => (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

        private string BuildHtml()
        {
            var entity = SelectedEntity() ?? _views.FirstOrDefault()?.Entity;
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>View Performance Analysis</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                "table{border-collapse:collapse;width:100%;margin-top:12px}th,td{border:1px solid #ccc;padding:6px 8px;text-align:left;vertical-align:top}" +
                "th{background:#f4f4f4}.band-High{background:#ffcdd2}.band-Medium{background:#fff5c8}.band-Low{background:#e2f0d9}</style></head><body>");
            sb.AppendLine($"<h1>View Performance Analysis — {Html(entity)}</h1>");
            sb.AppendLine($"<p><b>Views analyzed:</b> {_views.Count} " +
                $"(System {_views.Count(v => v.ViewType == "System")}, Personal {_views.Count(v => v.ViewType == "Personal")}). " +
                $"<b>Bands:</b> High {_views.Count(v => v.Band == ScoreBand.High)}, " +
                $"Medium {_views.Count(v => v.Band == ScoreBand.Medium)}, Low {_views.Count(v => v.Band == ScoreBand.Low)}. " +
                "Scores are heuristic estimates (no server statistics).</p>");
            sb.AppendLine("<table><tr><th>Score</th><th>Band</th><th>View</th><th>Type</th><th>Entity</th>" +
                "<th>#Attrs</th><th>#Cols</th><th>#Links</th></tr>");
            foreach (var v in _views)
            {
                sb.AppendLine($"<tr class=\"band-{v.Band}\"><td>{v.Score}</td><td>{v.Band}</td><td>{Html(v.Name)}</td>" +
                    $"<td>{v.ViewType}</td><td>{Html(v.Entity)}</td><td>{v.FetchAttributeCount}</td>" +
                    $"<td>{v.LayoutColumnCount}</td><td>{v.LinkCount}</td></tr>");
            }
            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }

        private static string Html(string s) => (s ?? "")
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        // ----------------------------------------------------------------- Helpers

        private void ClearResults()
        {
            _views = new List<ViewAnalysis>();
            grdViews.Rows.Clear();
            grdFindings.Rows.Clear();
            txtFetchXml.Text = "";
            lstLayoutColumns.Items.Clear();
            txtSummary.Text = "";
            tsbExport.Enabled = false;
        }

        private static Color BandColor(ScoreBand band)
        {
            switch (band)
            {
                case ScoreBand.High: return Color.FromArgb(255, 205, 210);
                case ScoreBand.Medium: return Color.FromArgb(255, 245, 200);
                default: return Color.FromArgb(226, 240, 217);
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

        /// <summary>A table list-item: display label + logical name; ToString drives the combo text.</summary>
        private sealed class EntityItem
        {
            public string LogicalName { get; set; }
            public string Display { get; set; }

            public override string ToString() =>
                string.IsNullOrWhiteSpace(Display) ? LogicalName : $"{Display} ({LogicalName})";
        }
    }

    /// <summary>Serializable settings POCO round-tripped via BaseToolControl LoadSettings/SaveSettings.</summary>
    public class ViewSettings
    {
        public string LastEntity { get; set; }
        public bool IncludePersonal { get; set; }
        public int MaxLayoutColumns { get; set; } = 15;
        public int MaxAttributes { get; set; } = 30;
        public int MaxLinkEntities { get; set; } = 4;
        public int WarnLinkEntities { get; set; } = 2;
    }
}
