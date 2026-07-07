using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.FetchXml;
using XrmToolSuite.Core.Reporting;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.FetchXmlPerformanceAnalyzer
{
    public partial class FetchXmlPerformanceAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private ToolSettings _settings;
        private ParsedFetchXml _lastParsed;
        private FetchXmlAnalysis _lastAnalysis;

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        private const string SampleFetchXml =
            "<fetch>\r\n  <entity name=\"account\">\r\n    <attribute name=\"name\" />\r\n    <attribute name=\"createdon\" />\r\n  </entity>\r\n</fetch>";

        public FetchXmlPerformanceAnalyzerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("FetchXML Performance Analyzer"));
        }

        private void FetchXmlPerformanceAnalyzerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<ToolSettings>();
            txtFetchXml.Text = string.IsNullOrWhiteSpace(_settings.LastFetchXml)
                ? SampleFetchXml
                : _settings.LastFetchXml;
            LogInfo("FetchXML Performance Analyzer loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
                _settings.LastFetchXml = txtFetchXml.Text;
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
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        // ----------------------------------------------------------------- Analyze (pure, no connection)

        private void tsbAnalyze_Click(object sender, EventArgs e) => AnalyzeCurrent();

        private void AnalyzeCurrent()
        {
            _lastParsed = null;
            _lastAnalysis = null;
            tsbExport.Enabled = false;

            var parse = FetchXmlParser.Parse(txtFetchXml.Text);
            if (!parse.Success)
            {
                grdFindings.Rows.Clear();
                txtSummary.Text = "Parse error:\r\n" + parse.Error;
                txtSuggestions.Text = "";
                SetStatusMessage("Parse error — fix the FetchXML and analyze again.");
                MessageBox.Show(parse.Error, "FetchXML parse error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _lastParsed = parse.Query;
            _lastAnalysis = FetchXmlRules.Analyze(_lastParsed, ThresholdsFromSettings());

            PopulateFindings(_lastAnalysis);
            txtSummary.Text = BuildSummaryText(_lastParsed, _lastAnalysis);
            txtSuggestions.Text = _lastAnalysis.Suggestions.Count == 0
                ? "No suggestions — no structural risks detected."
                : string.Join("\r\n", _lastAnalysis.Suggestions.Select(s => "• " + s));

            tsbExport.Enabled = true;
            SetStatusMessage(
                $"Analyzed {_lastParsed.RootEntity}: {_lastAnalysis.Findings.Count} finding(s), " +
                $"estimated cost {_lastAnalysis.CostEstimate}/100 ({_lastAnalysis.Band}).");
        }

        private FetchXmlAnalysisOptions ThresholdsFromSettings()
        {
            return new FetchXmlAnalysisOptions
            {
                MaxAttributes = _settings?.MaxAttributes > 0 ? _settings.MaxAttributes : 30,
                MaxLinkEntities = _settings?.MaxLinkEntities > 0 ? _settings.MaxLinkEntities : 4,
                WarnLinkEntities = _settings?.WarnLinkEntities > 0 ? _settings.WarnLinkEntities : 2
            };
        }

        private void PopulateFindings(FetchXmlAnalysis analysis)
        {
            grdFindings.Rows.Clear();
            foreach (var f in analysis.Findings.OrderByDescending(x => x.Severity))
            {
                int rowIndex = grdFindings.Rows.Add(
                    f.Severity.ToString(),
                    f.Title,
                    f.Component ?? "",
                    f.Recommendation ?? "");
                grdFindings.Rows[rowIndex].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
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

        private static string BuildSummaryText(ParsedFetchXml q, FetchXmlAnalysis a)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Root entity      : {q.RootEntity}");
            sb.AppendLine($"Attributes       : {q.TotalAttributeCount}{(q.AllAttributes ? "  (<all-attributes/>)" : "")}");
            sb.AppendLine($"Root filter      : {(q.HasRootFilter ? "yes" : "NO")}");
            sb.AppendLine($"Link-entities    : {q.LinkCount}");
            sb.AppendLine($"Orders           : {q.Orders.Count}" +
                (q.Orders.Any(o => o.OnLinkEntity) ? "  (some on link-entity)" : ""));
            sb.AppendLine($"Aggregate        : {(q.HasAggregate ? "yes" : "no")}");
            sb.AppendLine($"Distinct         : {(q.Distinct ? "yes" : "no")}");
            sb.AppendLine($"No-lock          : {(q.NoLock ? "yes" : "no")}");
            sb.AppendLine($"Top / Page size  : {(q.Top?.ToString() ?? "-")} / {(q.PageSize?.ToString() ?? "-")}");
            sb.AppendLine();
            sb.AppendLine($"Estimated cost   : {a.CostEstimate}/100  ({a.Band})");
            sb.AppendLine("(heuristic estimate — no server statistics; use Execute with timing to ground it)");
            return sb.ToString();
        }

        // ----------------------------------------------------------------- Load from view (needs connection)

        private void tsbLoadView_Click(object sender, EventArgs e) => ExecuteMethod(LoadViews);

        private void LoadViews()
        {
            RunAsync(
                "Retrieving saved views...",
                worker =>
                {
                    var views = new List<ViewItem>();

                    var system = Service.RetrieveAll(new QueryExpression("savedquery")
                    {
                        ColumnSet = new ColumnSet("name", "fetchxml", "returnedtypecode")
                    }, worker: worker);
                    views.AddRange(system
                        .Where(v => !string.IsNullOrWhiteSpace(v.GetAttributeValue<string>("fetchxml")))
                        .Select(v => new ViewItem
                        {
                            Name = v.GetAttributeValue<string>("name"),
                            EntityName = v.GetAttributeValue<string>("returnedtypecode"),
                            FetchXml = v.GetAttributeValue<string>("fetchxml"),
                            Kind = "System"
                        }));

                    var personal = Service.RetrieveAll(new QueryExpression("userquery")
                    {
                        ColumnSet = new ColumnSet("name", "fetchxml", "returnedtypecode")
                    }, worker: worker);
                    views.AddRange(personal
                        .Where(v => !string.IsNullOrWhiteSpace(v.GetAttributeValue<string>("fetchxml")))
                        .Select(v => new ViewItem
                        {
                            Name = v.GetAttributeValue<string>("name"),
                            EntityName = v.GetAttributeValue<string>("returnedtypecode"),
                            FetchXml = v.GetAttributeValue<string>("fetchxml"),
                            Kind = "Personal"
                        }));

                    return views
                        .OrderBy(v => v.Kind)
                        .ThenBy(v => v.EntityName)
                        .ThenBy(v => v.Name)
                        .ToList();
                },
                views =>
                {
                    if (views.Count == 0)
                    {
                        SetStatusMessage("No saved views with FetchXML found.");
                        return;
                    }

                    var chosen = PromptSelectView(views);
                    if (chosen != null)
                    {
                        txtFetchXml.Text = chosen.FetchXml;
                        SetStatusMessage($"Loaded FetchXML from view '{chosen.Name}'. Click Analyze.");
                    }
                });
        }

        private static ViewItem PromptSelectView(List<ViewItem> views)
        {
            using (var dlg = new Form())
            using (var combo = new ComboBox())
            using (var ok = new Button())
            using (var cancel = new Button())
            {
                dlg.Text = "Select a view to analyze";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ClientSize = new Size(520, 90);

                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.SetBounds(12, 15, 496, 24);
                combo.DropDownWidth = 640;
                foreach (var v in views)
                    combo.Items.Add($"[{v.Kind}] {v.EntityName} — {v.Name}");
                combo.SelectedIndex = 0;

                ok.Text = "Load";
                ok.DialogResult = DialogResult.OK;
                ok.SetBounds(332, 52, 84, 26);

                cancel.Text = "Cancel";
                cancel.DialogResult = DialogResult.Cancel;
                cancel.SetBounds(424, 52, 84, 26);

                dlg.Controls.AddRange(new Control[] { combo, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;

                return dlg.ShowDialog() == DialogResult.OK && combo.SelectedIndex >= 0
                    ? views[combo.SelectedIndex]
                    : null;
            }
        }

        // ----------------------------------------------------------------- Execute with timing (opt-in, read-only)

        private void tsbExecute_Click(object sender, EventArgs e) => ExecuteMethod(ExecuteWithTiming);

        private void ExecuteWithTiming()
        {
            var fetch = txtFetchXml.Text;
            var parse = FetchXmlParser.Parse(fetch);
            if (!parse.Success)
            {
                MessageBox.Show(parse.Error, "FetchXML parse error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Cap an otherwise-unbounded query so timing stays a safe, read-only probe.
            var toRun = (!parse.Query.Top.HasValue && !parse.Query.PageSize.HasValue && !parse.Query.HasAggregate)
                ? AddTopLimit(fetch, 50)
                : fetch;

            RunAsync(
                "Executing query (read-only timing)...",
                worker =>
                {
                    var sw = Stopwatch.StartNew();
                    var result = Service.RetrieveMultiple(new FetchExpression(toRun));
                    sw.Stop();
                    return new TimingResult { ElapsedMs = sw.ElapsedMilliseconds, RowCount = result.Entities.Count, MoreRecords = result.MoreRecords };
                },
                r =>
                {
                    SetStatusMessage(
                        $"Executed in {r.ElapsedMs} ms — {r.RowCount} row(s){(r.MoreRecords ? "+ (more available)" : "")}. " +
                        "Timing is read-only and may reflect a capped result set.");
                });
        }

        /// <summary>Adds a <c>top</c> to the fetch element when the query has no limit, keeping timing bounded.</summary>
        private static string AddTopLimit(string fetchXml, int top)
        {
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(fetchXml);
                if (doc.Root != null && doc.Root.Attribute("top") == null && doc.Root.Attribute("count") == null)
                    doc.Root.SetAttributeValue("top", top);
                return doc.ToString();
            }
            catch
            {
                return fetchXml;
            }
        }

        // ----------------------------------------------------------------- Export

        private void tsmExportExcel_Click(object sender, EventArgs e) =>
            ExportWith("Excel (*.xlsx)|*.xlsx", "fetchxml-analysis.xlsx",
                path => ExcelReportExporter.Export(BuildReportModel(), path));

        private void tsmExportPdf_Click(object sender, EventArgs e) =>
            ExportWith("PDF (*.pdf)|*.pdf", "fetchxml-analysis.pdf",
                path => PdfReportExporter.Export(BuildReportModel(), path));

        private void tsmExportJson_Click(object sender, EventArgs e) =>
            ExportWith("JSON (*.json)|*.json", "fetchxml-analysis.json",
                path => JsonReportExporter.Export(BuildReportModel(), path));

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportWith("HTML (*.html)|*.html", "fetchxml-analysis.html",
                path => System.IO.File.WriteAllText(path, BuildHtml(), Encoding.UTF8));

        private void tsmExportMarkdown_Click(object sender, EventArgs e) =>
            ExportWith("Markdown (*.md)|*.md", "fetchxml-analysis.md",
                path => System.IO.File.WriteAllText(path, BuildMarkdown(), Encoding.UTF8));

        private void tsmExportCsv_Click(object sender, EventArgs e) =>
            ExportWith("CSV (*.csv)|*.csv", "fetchxml-findings.csv",
                path => System.IO.File.WriteAllText(path, BuildCsv(), Encoding.UTF8));

        private void ExportWith(string filter, string fileName, Action<string> writer)
        {
            if (_lastAnalysis == null)
            {
                MessageBox.Show("Analyze a query first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = fileName })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                try
                {
                    writer(dlg.FileName);
                    SetStatusMessage("Exported analysis to " + dlg.FileName);
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }
        }

        private ReportModel BuildReportModel()
        {
            var r = new ReportModel
            {
                ToolName = "FetchXML Performance Analyzer",
                ReportTitle = "FetchXML Performance Analysis",
                ScoreWord = "cost",
                SubjectName = _lastParsed?.RootEntity,
                Score = _lastAnalysis.CostEstimate,
                Band = _lastAnalysis.Band
            };
            foreach (var f in _lastAnalysis.Findings)
                r.Findings.Add(f);
            foreach (var s in _lastAnalysis.Suggestions)
                r.ChecklistGuidance.Add(s);
            return r;
        }

        private string BuildCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Severity,Category,Title,Component,Description,Recommendation");
            foreach (var f in _lastAnalysis.Findings.OrderByDescending(x => x.Severity))
            {
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(f.Severity.ToString()), Csv(f.Category), Csv(f.Title),
                    Csv(f.Component), Csv(f.Description), Csv(f.Recommendation)
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
            var q = _lastParsed;
            var a = _lastAnalysis;
            var sb = new StringBuilder();
            sb.AppendLine("# FetchXML Performance Analysis");
            sb.AppendLine();
            sb.AppendLine($"- **Root entity:** {q.RootEntity}");
            sb.AppendLine($"- **Attributes:** {q.TotalAttributeCount}{(q.AllAttributes ? " (all-attributes)" : "")}");
            sb.AppendLine($"- **Root filter:** {(q.HasRootFilter ? "yes" : "no")}");
            sb.AppendLine($"- **Link-entities:** {q.LinkCount}");
            sb.AppendLine($"- **Orders:** {q.Orders.Count}");
            sb.AppendLine($"- **Aggregate / Distinct:** {(q.HasAggregate ? "yes" : "no")} / {(q.Distinct ? "yes" : "no")}");
            sb.AppendLine($"- **Estimated cost (heuristic):** {a.CostEstimate}/100 ({a.Band})");
            sb.AppendLine();
            sb.AppendLine("## Findings");
            sb.AppendLine();
            sb.AppendLine("| Severity | Title | Component | Recommendation |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var f in a.Findings.OrderByDescending(x => x.Severity))
                sb.AppendLine($"| {f.Severity} | {Md(f.Title)} | {Md(f.Component)} | {Md(f.Recommendation)} |");
            sb.AppendLine();
            if (a.Suggestions.Count > 0)
            {
                sb.AppendLine("## Suggestions");
                sb.AppendLine();
                foreach (var s in a.Suggestions)
                    sb.AppendLine($"- {s}");
            }
            return sb.ToString();
        }

        private static string Md(string s) => (s ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

        private string BuildHtml()
        {
            var q = _lastParsed;
            var a = _lastAnalysis;
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>FetchXML Performance Analysis</title>");
            sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#222}" +
                "table{border-collapse:collapse;width:100%;margin-top:12px}th,td{border:1px solid #ccc;padding:6px 8px;text-align:left;vertical-align:top}" +
                "th{background:#f4f4f4}.sev-Critical{background:#ffcdd2}.sev-High{background:#ffe0b2}.sev-Medium{background:#fff5c8}.sev-Low{background:#e2f0d9}" +
                ".meta li{margin:2px 0}</style></head><body>");
            sb.AppendLine("<h1>FetchXML Performance Analysis</h1>");
            sb.AppendLine($"<p><b>Estimated cost (heuristic):</b> {a.CostEstimate}/100 &mdash; <b>{a.Band}</b>. " +
                "No server statistics were used; run \"Execute with timing\" to ground the estimate.</p>");
            sb.AppendLine("<ul class=\"meta\">");
            sb.AppendLine($"<li><b>Root entity:</b> {Html(q.RootEntity)}</li>");
            sb.AppendLine($"<li><b>Attributes:</b> {q.TotalAttributeCount}{(q.AllAttributes ? " (all-attributes)" : "")}</li>");
            sb.AppendLine($"<li><b>Root filter:</b> {(q.HasRootFilter ? "yes" : "no")}</li>");
            sb.AppendLine($"<li><b>Link-entities:</b> {q.LinkCount}</li>");
            sb.AppendLine($"<li><b>Orders:</b> {q.Orders.Count}</li>");
            sb.AppendLine($"<li><b>Aggregate / Distinct:</b> {(q.HasAggregate ? "yes" : "no")} / {(q.Distinct ? "yes" : "no")}</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("<h2>Findings</h2><table><tr><th>Severity</th><th>Title</th><th>Component</th><th>Description</th><th>Recommendation</th></tr>");
            foreach (var f in a.Findings.OrderByDescending(x => x.Severity))
            {
                sb.AppendLine($"<tr class=\"sev-{f.Severity}\"><td>{f.Severity}</td><td>{Html(f.Title)}</td>" +
                    $"<td>{Html(f.Component)}</td><td>{Html(f.Description)}</td><td>{Html(f.Recommendation)}</td></tr>");
            }
            sb.AppendLine("</table>");
            if (a.Suggestions.Count > 0)
            {
                sb.AppendLine("<h2>Suggestions</h2><ul>");
                foreach (var s in a.Suggestions)
                    sb.AppendLine($"<li>{Html(s)}</li>");
                sb.AppendLine("</ul>");
            }
            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string Html(string s) => (s ?? "")
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        private sealed class ViewItem
        {
            public string Name { get; set; }
            public string EntityName { get; set; }
            public string FetchXml { get; set; }
            public string Kind { get; set; }
        }

        private sealed class TimingResult
        {
            public long ElapsedMs { get; set; }
            public int RowCount { get; set; }
            public bool MoreRecords { get; set; }
        }
    }

    /// <summary>Serializable settings POCO round-tripped via BaseToolControl LoadSettings/SaveSettings.</summary>
    public class ToolSettings
    {
        public string LastFetchXml { get; set; }
        public int MaxAttributes { get; set; } = 30;
        public int MaxLinkEntities { get; set; } = 4;
        public int WarnLinkEntities { get; set; } = 2;
    }
}
