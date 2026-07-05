using System;
using System.Collections.Generic;
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
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.PortalHealthAnalyzer.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.PortalHealthAnalyzer
{
    public partial class PortalHealthAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private PortalSettings _settings;
        private readonly PortalCollector _collector = new PortalCollector();
        private PortalInventory _inventory;
        private PortalHealthReport _report;

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        public PortalHealthAnalyzerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Portal Health Analyzer"));
        }

        private void PortalHealthAnalyzerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<PortalSettings>();
            LogInfo("Portal Health Analyzer loaded");
            SetStatusMessage("Click 'Load websites', pick a Power Pages website, then 'Analyze'.");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
            {
                var sel = SelectedWebsite();
                if (sel != null)
                {
                    _settings.LastWebsiteId = sel.Id.ToString();
                    _settings.LastSchema = sel.Schema.ToString();
                }
            }
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            cboWebsite.Items.Clear();
            ClearResults();
            SetStatusMessage($"Connected to {detail?.ConnectionName}. Click 'Load websites'.");
        }

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();

        // ----------------------------------------------------------------- Website discovery

        private void tsbLoadWebsites_Click(object sender, EventArgs e) => ExecuteMethod(LoadWebsites);

        private void LoadWebsites()
        {
            RunAsync(
                "Discovering Power Pages websites (adx_ / mspp_)…",
                worker => _collector.ListWebsites(Service, worker),
                sites =>
                {
                    cboWebsite.Items.Clear();
                    foreach (var s in sites)
                        cboWebsite.Items.Add(new WebsiteItem { Id = s.id, Name = s.name, Schema = s.schema });

                    if (cboWebsite.Items.Count == 0)
                    {
                        SetStatusMessage("No Power Pages websites found (neither adx_website nor mspp_website is provisioned).");
                        return;
                    }

                    RestoreLastSelection();
                    if (cboWebsite.SelectedIndex < 0) cboWebsite.SelectedIndex = 0;
                    SetStatusMessage($"Found {cboWebsite.Items.Count} website(s). Pick one and click 'Analyze'.");
                });
        }

        private void RestoreLastSelection()
        {
            if (string.IsNullOrEmpty(_settings?.LastWebsiteId)) return;
            if (!Guid.TryParse(_settings.LastWebsiteId, out var id)) return;
            for (int i = 0; i < cboWebsite.Items.Count; i++)
            {
                if (cboWebsite.Items[i] is WebsiteItem w
                    && w.Id == id
                    && string.Equals(w.Schema.ToString(), _settings.LastSchema, StringComparison.OrdinalIgnoreCase))
                {
                    cboWebsite.SelectedIndex = i;
                    return;
                }
            }
        }

        private WebsiteItem SelectedWebsite() => cboWebsite.SelectedItem as WebsiteItem;

        // ----------------------------------------------------------------- Analyze

        private void tsbAnalyze_Click(object sender, EventArgs e) => ExecuteMethod(Analyze);

        private void Analyze()
        {
            var site = SelectedWebsite();
            if (site == null)
            {
                MessageBox.Show("Pick a website first (click 'Load websites' if the list is empty).",
                    "No website selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RunAsync(
                $"Analyzing '{site.Name}' ({site.Schema})…",
                worker =>
                {
                    var inv = _collector.Collect(Service, site.Id, site.Schema, worker, msg => worker.ReportProgress(0, msg));
                    inv.WebsiteName = string.IsNullOrEmpty(inv.WebsiteName) ? site.Name : inv.WebsiteName;
                    var report = PortalHealthRules.Evaluate(inv);
                    return new Tuple<PortalInventory, PortalHealthReport>(inv, report);
                },
                result =>
                {
                    _inventory = result.Item1;
                    _report = result.Item2;
                    PopulateDashboard();
                    tsbExport.Enabled = true;

                    var msg = $"Health {_report.Band} (score {_report.Score}/100) — {_report.Findings.Count} finding(s) for '{_inventory.WebsiteName}'.";
                    if (_inventory.UnavailableTables.Count > 0)
                        msg += $" Unavailable: {string.Join(", ", _inventory.UnavailableTables)}.";
                    SetStatusMessage(msg);
                });
        }

        private void PopulateDashboard()
        {
            lblScore.Text = $"Health: {_report.Band}   (score {_report.Score}/100)   —   {_inventory.SchemaLabel}";
            lblScore.BackColor = BandColor(_report.Band);
            txtSummary.Text = BuildSummary();
            PopulateFindings();
        }

        private string BuildSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Website:  {_inventory.WebsiteName}   [{_inventory.SchemaLabel}]");
            sb.AppendLine(ScoreCalculator.Explain(_report.Findings, _report.Score, _report.Band, "health"));
            sb.AppendLine();
            sb.AppendLine("Configuration:");
            foreach (var m in _report.Metrics.Where(m => m.Label != "Schema"))
                sb.AppendLine($"  {m.Label,-20} {m.Value}{(string.IsNullOrEmpty(m.Hint) ? "" : "   (" + m.Hint + ")")}");
            if (_inventory.UnavailableTables.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Unavailable tables (skipped): " + string.Join(", ", _inventory.UnavailableTables));
            }
            return sb.ToString();
        }

        private void PopulateFindings()
        {
            grdFindings.Rows.Clear();
            foreach (var f in _report.Findings.OrderByDescending(x => x.Severity).ThenBy(x => x.Category))
            {
                int r = grdFindings.Rows.Add(
                    f.Category,
                    f.Severity.ToString(),
                    f.Component ?? "",
                    f.Title,
                    f.Recommendation ?? "");
                grdFindings.Rows[r].DefaultCellStyle.BackColor = SeverityColor(f.Severity);
            }
        }

        // ----------------------------------------------------------------- Export

        private void tsmExportExcel_Click(object sender, EventArgs e) =>
            ExportFile("Excel workbook (*.xlsx)|*.xlsx", "portal-health.xlsx",
                path => ExcelReportExporter.Export(BuildReportModel(), path));

        private void tsmExportPdf_Click(object sender, EventArgs e) =>
            ExportFile("PDF document (*.pdf)|*.pdf", "portal-health.pdf",
                path => PdfReportExporter.Export(BuildReportModel(), path));

        private void tsmExportWord_Click(object sender, EventArgs e) =>
            ExportFile("Word document (*.docx)|*.docx", "portal-health.docx",
                path => WordReportExporter.Export(BuildReportModel(), path));

        private void tsmExportJson_Click(object sender, EventArgs e) =>
            ExportFile("JSON file (*.json)|*.json", "portal-health.json",
                path => JsonReportExporter.Export(BuildReportModel(), path));

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportFile("HTML report (*.html)|*.html", "portal-health.html",
                path => File.WriteAllText(path, HtmlDashboardBuilder.Build(BuildReportModel()), Encoding.UTF8));

        private void tsmExportCsv_Click(object sender, EventArgs e) =>
            ExportFile("CSV file (*.csv)|*.csv", "portal-health.csv",
                path => File.WriteAllText(path, BuildCsv(), new UTF8Encoding(true)));

        /// <summary>SaveFileDialog + write via the delegate. ClosedXML/PdfSharp/MigraDoc types stay
        /// inside the writer delegate (a method-body local), never in a signature here.</summary>
        private void ExportFile(string filter, string defaultName, Action<string> write)
        {
            if (_report == null)
            {
                MessageBox.Show("Analyze a website first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    write(dlg.FileName);
                    SetStatusMessage("Exported portal health report to " + Path.GetFileName(dlg.FileName));
                }
                catch (Exception ex) { ShowError(ex, "Export failed"); }
            }
        }

        private ReportModel BuildReportModel()
        {
            var r = new ReportModel
            {
                ToolName = "Portal Health Analyzer",
                ReportTitle = "Portal Health Report",
                ScoreWord = "health",
                SubjectName = _inventory.WebsiteName,
                SubjectKey = _inventory.SchemaLabel,
                Score = _report.Score,
                Band = _report.Band,
                LeadIn = "Read-only, metadata-only health scoring of a Power Pages website across the " +
                         "adx_/mspp_ schemas. Findings describe configuration risk (structure, site settings, " +
                         "and the security surface) — not live page availability.",
                VerdictLow = "No significant portal-health issues detected.",
                VerdictMedium = "Review and fix the flagged configuration issues before the next release.",
                VerdictHigh = "Address the critical and high findings — the portal has broken or unsafe configuration."
            };

            foreach (var m in _report.Metrics)
                r.Metrics.Add(m);
            foreach (var f in _report.Findings)
                r.Findings.Add(f);

            return r;
        }

        private string BuildCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Category,Severity,Record,Title,Recommendation");
            foreach (var f in _report.Findings.OrderByDescending(x => x.Severity).ThenBy(x => x.Category))
                sb.AppendLine(string.Join(",", new[]
                {
                    Csv(f.Category), Csv(f.Severity.ToString()), Csv(f.Component),
                    Csv(f.Title), Csv(f.Recommendation)
                }));
            return sb.ToString();
        }

        private static string Csv(string s)
        {
            s = s ?? "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        // ----------------------------------------------------------------- Helpers

        private void ClearResults()
        {
            _inventory = null;
            _report = null;
            grdFindings.Rows.Clear();
            txtSummary.Clear();
            lblScore.Text = "No analysis yet";
            lblScore.BackColor = SystemColors.Control;
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

        /// <summary>A website list item across either schema; ToString drives the combo text + schema badge.</summary>
        private sealed class WebsiteItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public PortalSchema Schema { get; set; }

            public override string ToString() =>
                $"{Name}   [{(Schema == PortalSchema.Adx ? "adx" : "mspp")}]";
        }
    }

    /// <summary>Serializable settings POCO round-tripped via BaseToolControl LoadSettings/SaveSettings.
    /// Stores only the last-selected website id + schema — never credentials.</summary>
    public class PortalSettings
    {
        public string LastWebsiteId { get; set; }
        public string LastSchema { get; set; }
    }
}
