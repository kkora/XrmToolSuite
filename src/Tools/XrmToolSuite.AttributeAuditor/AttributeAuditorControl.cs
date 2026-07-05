using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.AttributeAuditor.Audit;
using XrmToolSuite.AttributeAuditor.Reporting;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Reporting;
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.AttributeAuditor
{
    public partial class AttributeAuditorControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private AuditSettings _settings;
        private AuditResult _lastResult;
        private string _envName = "environment";

        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        public AttributeAuditorControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Attribute Auditor"));
        }

        private void AttributeAuditorControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<AuditSettings>();
            tsbCustomOnly.Checked = _settings.CustomEntitiesOnly;
            tsbCandidatesOnly.Checked = _settings.CandidatesOnly;
            LogInfo("Attribute Auditor loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
            {
                _settings.CustomEntitiesOnly = tsbCustomOnly.Checked;
                _settings.CandidatesOnly = tsbCandidatesOnly.Checked;
                SaveSettings(_settings);
            }
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // metadata may differ between environments
            _envName = detail?.ConnectionName ?? "environment";
            SetStatusMessage($"Connected to {_envName}");
        }

        private void tsbClose_Click(object sender, EventArgs e) => CloseTool();

        // ExecuteMethod ensures a connection exists (prompts to connect if not).
        private void tsbRun_Click(object sender, EventArgs e) => ExecuteMethod(RunAudit);

        private void RunAudit()
        {
            bool customOnly = tsbCustomOnly.Checked;
            RunAsync(
                "Auditing columns…",
                worker =>
                {
                    var ctx = new AttributeAuditContext(Service, _envName);
                    return AttributeUsageCollector.Collect(ctx, customOnly,
                        msg => worker.ReportProgress(0, msg));
                },
                result =>
                {
                    _lastResult = result;
                    PopulateGrid();
                    bool any = result.TotalColumns > 0;
                    tsbExportCsv.Enabled = any;
                    tsbExportHtml.Enabled = any;
                    SetStatusMessage(
                        $"{result.TotalColumns} custom column(s): {result.UsedColumns} used, {result.CandidateColumns} retirement candidate(s)");
                });
        }

        private void tsbCandidatesOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (_lastResult != null) PopulateGrid();
        }

        private void PopulateGrid()
        {
            IEnumerable<ColumnAudit> rows = _lastResult.Columns;
            if (tsbCandidatesOnly.Checked) rows = rows.Where(c => c.IsRetirementCandidate);

            lvResults.BeginUpdate();
            lvResults.Items.Clear();
            foreach (var c in rows.OrderBy(x => x.Table).ThenBy(x => x.LogicalName))
            {
                var item = new ListViewItem(new[]
                {
                    c.Table,
                    c.LogicalName,
                    c.DisplayName ?? "",
                    c.AttributeType ?? "",
                    c.IsManaged ? "yes" : "no",
                    c.IsUsed ? "yes" : "no",
                    c.UsageSummary()
                });
                if (c.IsRetirementCandidate)
                    item.ForeColor = System.Drawing.Color.Firebrick; // highlight candidates
                lvResults.Items.Add(item);
            }
            lvResults.EndUpdate();
        }

        private void tsbExportCsv_Click(object sender, EventArgs e)
        {
            if (_lastResult == null) return;
            using (var dlg = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "attribute-audit.csv" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    AuditCsvExporter.Export(_lastResult, dlg.FileName);
                    SetStatusMessage($"Exported {_lastResult.TotalColumns} row(s) to CSV");
                }
                catch (Exception ex) { ShowError(ex, "CSV export failed"); }
            }
        }

        private void tsbExportHtml_Click(object sender, EventArgs e)
        {
            if (_lastResult == null) return;
            using (var dlg = new SaveFileDialog { Filter = "HTML report (*.html)|*.html", FileName = "attribute-audit.html" })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    HtmlDashboardBuilder.Export(AttributeAuditReport.ToReportModel(_lastResult), dlg.FileName);
                    SetStatusMessage("Exported HTML report");
                }
                catch (Exception ex) { ShowError(ex, "HTML export failed"); }
            }
        }
    }

    /// <summary>Persisted automatically via SettingsManager (see Load/ClosingPlugin). No credentials.</summary>
    public class AuditSettings
    {
        public bool CustomEntitiesOnly { get; set; } = true;
        public bool CandidatesOnly { get; set; }
    }
}
