using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
using XrmToolSuite.FormPerformanceAnalyzer.Analysis;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;
// PluginControlBase pulls in a Label; disambiguate to the WinForms one (CS0104 guard).
using Label = System.Windows.Forms.Label;

namespace XrmToolSuite.FormPerformanceAnalyzer
{
    public partial class FormPerformanceAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private FormSettings _settings;
        private List<FormScore> _forms = new List<FormScore>();
        private List<EntityItem> _tableCache = new List<EntityItem>();
        private List<string> _scopeEntities = new List<string>();

        // Report a bug / help links surfaced in XrmToolBox.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public FormPerformanceAnalyzerControl()
        {
            InitializeComponent();
            // Suite convention: every tool carries a right-aligned Help button (shared dialog).
            toolStrip.Items.Add(CreateHelpButton("Form Performance Analyzer"));
        }

        private void FormPerformanceAnalyzerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<FormSettings>();
            _scopeEntities = _settings.ScopeEntities?.ToList() ?? new List<string>();
            UpdateScopeLabel();
            LogInfo("Form Performance Analyzer loaded");
            SetStatusMessage("Optionally 'Select tables…', then 'Analyze forms' to score every main form.");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            if (_settings != null)
            {
                _settings.ScopeEntities = _scopeEntities?.ToList() ?? new List<string>();
                SaveSettings(_settings);
            }
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
            _tableCache = new List<EntityItem>();
            ClearResults();
            SetStatusMessage($"Connected to {detail?.ConnectionName}. Click 'Analyze forms'.");
        }


        // ----------------------------------------------------------------- Table scope (needs connection)

        private void tsbSelectTables_Click(object sender, EventArgs e) => ExecuteMethod(LoadTablesThenPick);

        private void LoadTablesThenPick()
        {
            if (_tableCache.Count > 0)
            {
                ShowTablePicker();
                return;
            }

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
                        .Where(m => m.IsValidForAdvancedFind == true)
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
                    _tableCache = items;
                    SetStatusMessage($"Loaded {items.Count} table(s).");
                    ShowTablePicker();
                });
        }

        private void ShowTablePicker()
        {
            using (var dlg = new TablePickerDialog(_tableCache, _scopeEntities))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _scopeEntities = dlg.SelectedLogicalNames;
                UpdateScopeLabel();
            }
        }

        private void UpdateScopeLabel()
        {
            tslScope.Text = _scopeEntities != null && _scopeEntities.Count > 0
                ? $"Scope: {_scopeEntities.Count} table(s)"
                : "Scope: all tables";
        }

        // ----------------------------------------------------------------- Analyze forms (needs connection)

        private void tsbAnalyze_Click(object sender, EventArgs e) => ExecuteMethod(AnalyzeForms);

        private void AnalyzeForms()
        {
            bool allTables = _scopeEntities == null || _scopeEntities.Count == 0;
            if (allTables)
            {
                var confirm = MessageBox.Show(this,
                    "No table scope selected. Analyze every main form in the environment?\n\n" +
                    "This can be a large read on big environments. Choose 'Select tables…' first to narrow it.",
                    "Analyze all main forms",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (confirm != DialogResult.OK) return;
            }

            var scope = allTables ? null : _scopeEntities.ToList();
            var options = _settings.ToOptions();

            RunAsync(
                "Analyzing forms...",
                worker =>
                {
                    var collector = new FormCollector(options);
                    return collector.Collect(Service, scope, worker, msg => worker.ReportProgress(0, msg));
                },
                forms =>
                {
                    _forms = forms ?? new List<FormScore>();
                    PopulateFormsGrid(_forms);
                    txtSummary.Text = BuildSummary(_forms);
                    tsbExport.Enabled = _forms.Count > 0;

                    if (_forms.Count == 0)
                    {
                        SetStatusMessage("No main forms found in scope.");
                        return;
                    }

                    grdForms.ClearSelection();
                    if (grdForms.Rows.Count > 0)
                        grdForms.Rows[0].Selected = true;

                    SetStatusMessage(
                        $"Analyzed {_forms.Count} form(s). Heaviest score {_forms.Max(f => f.Score)}/100.");
                });
        }

        private void PopulateFormsGrid(List<FormScore> forms)
        {
            grdForms.Rows.Clear();
            foreach (var f in forms)
            {
                var m = f.Model;
                int rowIndex = grdForms.Rows.Add(
                    f.Score,
                    f.Band.ToString(),
                    f.FormName ?? "(unnamed)",
                    f.Entity ?? "",
                    f.State ?? "",
                    m?.Fields ?? 0,
                    m?.Tabs ?? 0,
                    m?.Subgrids ?? 0,
                    m?.CustomControls ?? 0,
                    m?.JsLibraries ?? 0,
                    f.BusinessRuleCount);
                grdForms.Rows[rowIndex].Tag = f;
                grdForms.Rows[rowIndex].DefaultCellStyle.BackColor = BandColor(f.Band);
            }
        }

        private void grdForms_SelectionChanged(object sender, EventArgs e)
        {
            var form = grdForms.SelectedRows.Count > 0
                ? grdForms.SelectedRows[0].Tag as FormScore
                : null;

            grdMetrics.Rows.Clear();
            grdRecs.Rows.Clear();

            if (form == null)
            {
                lblMetricsHeader.Text = "Metric breakdown for the selected form";
                lblRecsHeader.Text = "Recommendations (sorted by impact)";
                return;
            }

            lblMetricsHeader.Text = $"Metric breakdown: {form.FormName} — score {form.Score}/100 ({form.Band})";
            foreach (var metric in form.Metrics)
                grdMetrics.Rows.Add(metric.Label, metric.Value, metric.Hint);

            lblRecsHeader.Text = form.Recommendations.Count > 0
                ? $"Recommendations ({form.Recommendations.Count}) — sorted by impact"
                : "Recommendations — none (form is within all budgets)";
            foreach (var r in form.Recommendations
                         .OrderBy(x => x.ImpactRank)
                         .ThenBy(x => x.Effort, StringComparer.OrdinalIgnoreCase))
            {
                int idx = grdRecs.Rows.Add(r.Impact, r.Effort, r.Text, r.TriggeredBy);
                grdRecs.Rows[idx].DefaultCellStyle.BackColor =
                    string.Equals(r.Impact, "Structural", StringComparison.OrdinalIgnoreCase)
                        ? Color.FromArgb(255, 245, 200)
                        : Color.FromArgb(226, 240, 217);
            }
        }

        // ----------------------------------------------------------------- Compare two forms

        private void tsbCompare_Click(object sender, EventArgs e)
        {
            var selected = grdForms.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.Tag as FormScore)
                .Where(f => f != null)
                .ToList();

            if (selected.Count != 2)
            {
                MessageBox.Show(this,
                    "Select exactly two forms in the grid (Ctrl+click) to compare them side by side.",
                    "Pick two forms", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new CompareDialog(selected[0], selected[1]))
                dlg.ShowDialog(this);
        }

        // ----------------------------------------------------------------- Score settings

        private void tsbSettings_Click(object sender, EventArgs e)
        {
            using (var dlg = new ScoreSettingsDialog(_settings))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _settings = dlg.Result;
                SetStatusMessage("Scoring settings updated. Re-run 'Analyze forms' to apply.");
                if (_forms.Count > 0)
                {
                    // Re-score the already-loaded models with the new options (no re-query), preserving state.
                    var opts = _settings.ToOptions();
                    var rescored = _forms.Select(f =>
                    {
                        var s = FormScorer.Score(f.Model, f.BusinessRuleCount, opts);
                        s.State = f.State;
                        return s;
                    });
                    _forms = FormScorer.Rank(rescored);
                    PopulateFormsGrid(_forms);
                    txtSummary.Text = BuildSummary(_forms);
                    grdForms.ClearSelection();
                    if (grdForms.Rows.Count > 0) grdForms.Rows[0].Selected = true;
                }
            }
        }

        // ----------------------------------------------------------------- Export (off the UI thread)

        private void tsmExportCsv_Click(object sender, EventArgs e) =>
            ExportWith("CSV (*.csv)|*.csv", "form-performance.csv", BuildCsv);

        private void tsmExportHtml_Click(object sender, EventArgs e) =>
            ExportWith("HTML (*.html)|*.html", "form-performance.html", BuildHtml);

        private void ExportWith(string filter, string fileName, Func<string> contentFactory)
        {
            if (_forms == null || _forms.Count == 0)
            {
                MessageBox.Show(this, "Analyze forms first.", "Nothing to export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = fileName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var path = dlg.FileName;
                // Build the content and write the file off the UI thread.
                RunAsync(
                    "Exporting report...",
                    worker =>
                    {
                        System.IO.File.WriteAllText(path, contentFactory(), new UTF8Encoding(true));
                        return path;
                    },
                    written => { SetStatusMessage("Exported form analysis to " + written); PromptOpenExportedFile(written); });
            }
        }

        // ----------------------------------------------------------------- Summary + exports

        private static string BuildSummary(List<FormScore> forms)
        {
            var sb = new StringBuilder();
            int Count(FormBand b) => forms.Count(f => f.Band == b);
            sb.AppendLine($"Forms analyzed: {forms.Count}   " +
                $"Light {Count(FormBand.Light)}   Moderate {Count(FormBand.Moderate)}   " +
                $"Heavy {Count(FormBand.Heavy)}   Critical {Count(FormBand.Critical)}   " +
                "(heuristic estimate — structural weight, not measured time)");
            var top = forms.OrderByDescending(f => f.Score).Take(10).ToList();
            if (top.Count > 0)
                sb.AppendLine("Top heaviest: " + string.Join("   ",
                    top.Select(f => $"{f.FormName} [{f.Entity}] ({f.Score})")));
            return sb.ToString();
        }

        private string BuildCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Score,Band,Form,Table,State,VisibleFields,HiddenFields,Tabs,HiddenTabs,Sections," +
                "CustomControls,Subgrids,QuickViews,ScriptLibraries,OnLoad,OnChange,TabStateChange,BusinessRules,Recommendations");
            foreach (var f in _forms)
            {
                var m = f.Model ?? new FormModel();
                var recs = string.Join(" | ", f.Recommendations.Select(r => $"[{r.Impact}/{r.Effort}] {r.Text}"));
                sb.AppendLine(string.Join(",", new[]
                {
                    f.Score.ToString(CultureInfo.InvariantCulture),
                    Csv(f.Band.ToString()),
                    Csv(f.FormName), Csv(f.Entity), Csv(f.State),
                    m.VisibleFields.ToString(), m.HiddenFields.ToString(),
                    m.Tabs.ToString(), m.HiddenTabs.ToString(), m.Sections.ToString(),
                    m.CustomControls.ToString(), m.Subgrids.ToString(), m.QuickViews.ToString(),
                    m.JsLibraries.ToString(), m.OnLoadHandlers.ToString(), m.OnChangeHandlers.ToString(),
                    m.TabStateChangeHandlers.ToString(), f.BusinessRuleCount.ToString(),
                    Csv(recs)
                }));
            }
            return sb.ToString();
        }

        private static string Csv(string s)
        {
            s = s ?? "";
            if (s.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private string BuildHtml()
        {
            int Count(FormBand b) => _forms.Count(f => f.Band == b);
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Form Performance Analysis</title>");
            sb.AppendLine("<style>" +
                "body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1f1f1f;background:#fff}" +
                "h1{font-size:22px}h2{font-size:16px;margin-top:28px}" +
                "table{border-collapse:collapse;width:100%;margin-top:12px}" +
                "th,td{border:1px solid #d0d0d0;padding:6px 8px;text-align:left;vertical-align:top;font-size:13px}" +
                "th{background:#f4f4f4}" +
                ".b-Light{background:#e2f0d9}.b-Moderate{background:#fff5c8}.b-Heavy{background:#ffe0b2}.b-Critical{background:#ffcdd2}" +
                ".pill{display:inline-block;padding:2px 8px;border-radius:10px;font-size:12px;margin-right:8px}" +
                ".num{text-align:right}" +
                "</style></head><body>");
            sb.AppendLine("<h1>Form Performance Analysis</h1>");
            sb.AppendLine($"<p>Generated {DateTime.Now.ToString("yyyy-MM-dd HH:mm")} · {_forms.Count} main form(s) scored. " +
                "Scores are a labeled heuristic (structural load weight, not measured milliseconds).</p>");
            sb.AppendLine("<p>" +
                $"<span class=\"pill b-Light\">Light {Count(FormBand.Light)}</span>" +
                $"<span class=\"pill b-Moderate\">Moderate {Count(FormBand.Moderate)}</span>" +
                $"<span class=\"pill b-Heavy\">Heavy {Count(FormBand.Heavy)}</span>" +
                $"<span class=\"pill b-Critical\">Critical {Count(FormBand.Critical)}</span></p>");

            sb.AppendLine("<h2>Forms (ranked by score)</h2>");
            sb.AppendLine("<table><tr><th>Score</th><th>Band</th><th>Form</th><th>Table</th><th>State</th>" +
                "<th>Fields</th><th>Tabs</th><th>Subgrids</th><th>Custom</th><th>Scripts</th><th>Rules</th></tr>");
            foreach (var f in _forms)
            {
                var m = f.Model ?? new FormModel();
                sb.AppendLine($"<tr class=\"b-{f.Band}\"><td class=\"num\">{f.Score}</td><td>{f.Band}</td>" +
                    $"<td>{Html(f.FormName)}</td><td>{Html(f.Entity)}</td><td>{Html(f.State)}</td>" +
                    $"<td class=\"num\">{m.Fields}</td><td class=\"num\">{m.Tabs}</td><td class=\"num\">{m.Subgrids}</td>" +
                    $"<td class=\"num\">{m.CustomControls}</td><td class=\"num\">{m.JsLibraries}</td>" +
                    $"<td class=\"num\">{f.BusinessRuleCount}</td></tr>");
            }
            sb.AppendLine("</table>");

            // Recommendations across all forms.
            var recForms = _forms.Where(f => f.Recommendations.Count > 0).ToList();
            if (recForms.Count > 0)
            {
                sb.AppendLine("<h2>Recommendations</h2>");
                sb.AppendLine("<table><tr><th>Form</th><th>Impact</th><th>Effort</th><th>Recommendation</th><th>Triggered by</th></tr>");
                foreach (var f in recForms)
                    foreach (var r in f.Recommendations.OrderBy(x => x.ImpactRank))
                        sb.AppendLine($"<tr><td>{Html(f.FormName)}</td><td>{Html(r.Impact)}</td>" +
                            $"<td>{Html(r.Effort)}</td><td>{Html(r.Text)}</td><td>{Html(r.TriggeredBy)}</td></tr>");
                sb.AppendLine("</table>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string Html(string s) => (s ?? "")
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        // ----------------------------------------------------------------- Helpers

        private void ClearResults()
        {
            _forms = new List<FormScore>();
            grdForms.Rows.Clear();
            grdMetrics.Rows.Clear();
            grdRecs.Rows.Clear();
            txtSummary.Text = "";
            tsbExport.Enabled = false;
        }

        internal static Color BandColor(FormBand band)
        {
            switch (band)
            {
                case FormBand.Critical: return Color.FromArgb(255, 205, 210);
                case FormBand.Heavy: return Color.FromArgb(255, 224, 178);
                case FormBand.Moderate: return Color.FromArgb(255, 245, 200);
                default: return Color.FromArgb(226, 240, 217);
            }
        }

        /// <summary>A table list-item: display label + logical name; ToString drives the display text.</summary>
        internal sealed class EntityItem
        {
            public string LogicalName { get; set; }
            public string Display { get; set; }

            public override string ToString() =>
                string.IsNullOrWhiteSpace(Display) ? LogicalName : $"{Display} ({LogicalName})";
        }
    }

    /// <summary>
    /// Serializable settings POCO round-tripped via BaseToolControl LoadSettings/SaveSettings. Holds the
    /// scoring weights, band thresholds, rule-trigger thresholds, and the table scope. Plain properties only
    /// (no controls/services/credentials) so it serializes cleanly.
    /// </summary>
    public class FormSettings
    {
        // weights
        public double WeightPerVisibleField { get; set; } = 0.6;
        public double WeightPerHiddenField { get; set; } = 0.2;
        public double WeightPerTab { get; set; } = 2.0;
        public double WeightPerSection { get; set; } = 0.5;
        public double WeightPerCustomControl { get; set; } = 5.0;
        public double WeightPerSubgrid { get; set; } = 5.0;
        public double WeightPerQuickView { get; set; } = 3.0;
        public double WeightPerJsLibrary { get; set; } = 4.0;
        public double WeightPerOnLoadHandler { get; set; } = 2.0;
        public double WeightPerOnChangeHandler { get; set; } = 1.0;
        public double WeightPerTabStateChangeHandler { get; set; } = 2.0;
        public double WeightPerBusinessRule { get; set; } = 2.0;

        // band thresholds
        public int ModerateThreshold { get; set; } = 25;
        public int HeavyThreshold { get; set; } = 50;
        public int CriticalThreshold { get; set; } = 75;

        // rule-trigger thresholds
        public int MaxAboveFoldFields { get; set; } = 30;
        public int MaxTabs { get; set; } = 5;
        public int MaxSubgrids { get; set; } = 3;
        public int MaxQuickViews { get; set; } = 3;
        public int MaxCustomControls { get; set; } = 5;
        public int MaxScriptLibraries { get; set; } = 3;

        public List<string> ScopeEntities { get; set; } = new List<string>();

        public FormScoreOptions ToOptions() => new FormScoreOptions
        {
            WeightPerVisibleField = WeightPerVisibleField,
            WeightPerHiddenField = WeightPerHiddenField,
            WeightPerTab = WeightPerTab,
            WeightPerSection = WeightPerSection,
            WeightPerCustomControl = WeightPerCustomControl,
            WeightPerSubgrid = WeightPerSubgrid,
            WeightPerQuickView = WeightPerQuickView,
            WeightPerJsLibrary = WeightPerJsLibrary,
            WeightPerOnLoadHandler = WeightPerOnLoadHandler,
            WeightPerOnChangeHandler = WeightPerOnChangeHandler,
            WeightPerTabStateChangeHandler = WeightPerTabStateChangeHandler,
            WeightPerBusinessRule = WeightPerBusinessRule,
            ModerateThreshold = ModerateThreshold,
            HeavyThreshold = HeavyThreshold,
            CriticalThreshold = CriticalThreshold,
            MaxAboveFoldFields = MaxAboveFoldFields,
            MaxTabs = MaxTabs,
            MaxSubgrids = MaxSubgrids,
            MaxQuickViews = MaxQuickViews,
            MaxCustomControls = MaxCustomControls,
            MaxScriptLibraries = MaxScriptLibraries,
        };

        public void ApplyDefaults()
        {
            var d = new FormSettings();
            WeightPerVisibleField = d.WeightPerVisibleField;
            WeightPerHiddenField = d.WeightPerHiddenField;
            WeightPerTab = d.WeightPerTab;
            WeightPerSection = d.WeightPerSection;
            WeightPerCustomControl = d.WeightPerCustomControl;
            WeightPerSubgrid = d.WeightPerSubgrid;
            WeightPerQuickView = d.WeightPerQuickView;
            WeightPerJsLibrary = d.WeightPerJsLibrary;
            WeightPerOnLoadHandler = d.WeightPerOnLoadHandler;
            WeightPerOnChangeHandler = d.WeightPerOnChangeHandler;
            WeightPerTabStateChangeHandler = d.WeightPerTabStateChangeHandler;
            WeightPerBusinessRule = d.WeightPerBusinessRule;
            ModerateThreshold = d.ModerateThreshold;
            HeavyThreshold = d.HeavyThreshold;
            CriticalThreshold = d.CriticalThreshold;
            MaxAboveFoldFields = d.MaxAboveFoldFields;
            MaxTabs = d.MaxTabs;
            MaxSubgrids = d.MaxSubgrids;
            MaxQuickViews = d.MaxQuickViews;
            MaxCustomControls = d.MaxCustomControls;
            MaxScriptLibraries = d.MaxScriptLibraries;
        }

        public FormSettings Clone()
        {
            var c = (FormSettings)MemberwiseClone();
            c.ScopeEntities = ScopeEntities?.ToList() ?? new List<string>();
            return c;
        }
    }
}
