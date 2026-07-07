using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.CustomApiExplorer.Analysis;
using XrmToolSuite.CustomApiExplorer.Reporting;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.CustomApiExplorer
{
    public partial class CustomApiExplorerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private CustomApiExplorerSettings _settings;
        private CustomApiCatalog _catalog;
        private CustomApiInfo _selected;

        // Suite GitHub identity — powers the Help dialog's documentation / "Report a bug" links.
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => "https://github.com/kkora/XrmToolSuite";

        public CustomApiExplorerControl()
        {
            InitializeComponent();
            toolStrip.Items.Add(CreateHelpButton("Custom API Explorer"));
        }

        private void CustomApiExplorerControl_Load(object sender, EventArgs e)
        {
            _settings = LoadSettings<CustomApiExplorerSettings>();
            tstSearch.Text = _settings.LastFilter ?? string.Empty;
            LogInfo("Custom API Explorer loaded");
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.LastFilter = tstSearch.Text;
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(
            IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        // ---- inventory (US-PLUGIN6.1) ----

        private void tsbLoad_Click(object sender, EventArgs e) => ExecuteMethod(LoadCatalog);

        private void LoadCatalog()
        {
            RunAsync(
                "Reading Custom APIs…",
                worker =>
                {
                    var catalog = CustomApiCollector.Collect(Service, worker, msg => worker.ReportProgress(0, msg));
                    catalog.EnvironmentName = ConnectionDetail?.ConnectionName;
                    return catalog;
                },
                catalog =>
                {
                    _catalog = catalog;
                    ApplyFilter();
                    tsddExport.Enabled = catalog.Count > 0;
                    var noteSuffix = catalog.Notes.Count > 0 ? $" ({catalog.Notes.Count} source(s) skipped)" : "";
                    SetStatusMessage($"{catalog.Count} Custom API(s){noteSuffix}");
                });
        }

        private void tstSearch_TextChanged(object sender, EventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            grdApis.SuspendLayout();
            grdApis.Rows.Clear();
            if (_catalog != null)
            {
                var filter = (tstSearch.Text ?? string.Empty).Trim();
                foreach (var api in _catalog.Apis
                    .Where(a => filter.Length == 0
                                || (a.UniqueName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                                || (a.DisplayName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(a => a.UniqueName, StringComparer.OrdinalIgnoreCase))
                {
                    var idx = grdApis.Rows.Add(
                        api.UniqueName,
                        (api.IsFunction ? "Function" : "Action") + (api.IsPrivate ? " (private)" : ""),
                        api.BindingSummary(),
                        api.PluginTypeName ?? "(none)");
                    grdApis.Rows[idx].Tag = api;
                }
            }
            grdApis.ClearSelection();
            grdApis.ResumeLayout();
        }

        // ---- detail + parameter form (US-PLUGIN6.2 / US-PLUGIN6.4.1) ----

        private void grdApis_SelectionChanged(object sender, EventArgs e)
        {
            _selected = grdApis.SelectedRows.Count > 0 ? grdApis.SelectedRows[0].Tag as CustomApiInfo : null;
            RenderDetail();
            RenderParameterForm();
        }

        private void RenderDetail()
        {
            if (_selected == null) { txtDetail.Clear(); return; }
            var a = _selected;
            var sb = new StringBuilder();
            sb.AppendLine(a.UniqueName);
            sb.AppendLine(new string('-', 60));
            if (!string.IsNullOrWhiteSpace(a.DisplayName)) sb.AppendLine("Display    : " + a.DisplayName);
            if (!string.IsNullOrWhiteSpace(a.Description)) sb.AppendLine("Description: " + a.Description);
            sb.AppendLine("Kind       : " + (a.IsFunction ? "Function" : "Action") + (a.IsPrivate ? " (private)" : ""));
            sb.AppendLine("Binding    : " + a.BindingSummary());
            sb.AppendLine("Plugin     : " + (a.PluginTypeName ?? "(none)"));
            sb.AppendLine();
            sb.AppendLine("Request parameters:");
            if (a.Parameters.Count == 0) sb.AppendLine("  (none)");
            foreach (var p in a.Parameters)
                sb.AppendLine($"  {p.LogicalName} : {p.Type}{(p.IsOptional ? " (optional)" : " (required)")}");
            sb.AppendLine();
            sb.AppendLine("Response properties:");
            if (a.ResponseProperties.Count == 0) sb.AppendLine("  (none)");
            foreach (var r in a.ResponseProperties)
                sb.AppendLine($"  {r.LogicalName} : {r.Type}");
            sb.AppendLine();
            sb.AppendLine("Sample call:");
            sb.AppendLine(RequestBuilder.GenerateSnippet(a, ReadInputs()));
            txtDetail.Text = sb.ToString();
        }

        private void RenderParameterForm()
        {
            grdParams.Rows.Clear();
            txtResult.Clear();
            txtTarget.Enabled = _selected?.RequiresTarget ?? false;
            btnInvoke.Enabled = _selected != null;
            if (_selected == null) return;
            foreach (var p in _selected.Parameters)
            {
                var idx = grdParams.Rows.Add(p.LogicalName, p.Type.ToString(), p.IsOptional ? "yes" : "no", "");
                grdParams.Rows[idx].Tag = p;
            }
        }

        private Dictionary<string, string> ReadInputs()
        {
            var inputs = new Dictionary<string, string>();
            foreach (DataGridViewRow row in grdParams.Rows)
            {
                if (!(row.Tag is CustomApiParameter p)) continue;
                inputs[p.LogicalName] = Convert.ToString(row.Cells[colParamValue.Index].Value) ?? string.Empty;
            }
            return inputs;
        }

        // ---- gated invoke (US-PLUGIN6.4.2 / US-PLUGIN6.4.3) ----

        private void btnInvoke_Click(object sender, EventArgs e)
        {
            if (_selected == null) return;

            var inputs = ReadInputs();
            var binding = RequestBuilder.Bind(_selected, inputs);
            if (!binding.CanInvoke)
            {
                var problems = binding.MissingRequired.Select(m => "Required: " + m)
                    .Concat(binding.Errors).ToList();
                MessageBox.Show(this,
                    "Cannot invoke — fix these first:\n\n" + string.Join("\n", problems),
                    "Invalid parameters", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EntityReference target = null;
            if (_selected.RequiresTarget)
            {
                target = ParseTarget(txtTarget.Text);
                if (target == null)
                {
                    MessageBox.Show(this,
                        $"This API is {_selected.BindingSummary().ToLowerInvariant()} and needs a target as 'entity:guid'.",
                        "Target required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Explicit confirmation — this is the tool's only write/execute path.
            var scope = target != null ? $"{target.LogicalName} {target.Id}" : "the connected environment";
            var confirm = MessageBox.Show(this,
                $"Invoke Custom API '{_selected.UniqueName}' against {scope}?\n\n" +
                $"Environment: {ConnectionDetail?.ConnectionName}\n\n" +
                "This executes live and may modify data or trigger side effects.",
                "Confirm invocation", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (confirm != DialogResult.OK) return;

            var api = _selected;
            var ctarget = target;
            btnInvoke.Enabled = false;
            RunAsync(
                $"Invoking {api.UniqueName}…",
                worker => CustomApiInvoker.Invoke(Service, api, binding, ctarget),
                result =>
                {
                    btnInvoke.Enabled = true;
                    txtResult.Text = FormatResult(api, result);
                    SetStatusMessage(result.Success ? $"{api.UniqueName} succeeded" : $"{api.UniqueName} faulted");
                },
                ex =>
                {
                    btnInvoke.Enabled = true;
                    txtResult.Text = "Invocation error:\r\n" + ex;
                });
        }

        private static EntityReference ParseTarget(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var parts = raw.Split(':');
            return parts.Length == 2 && Guid.TryParse(parts[1].Trim(), out var id)
                ? new EntityReference(parts[0].Trim(), id) : null;
        }

        private static string FormatResult(CustomApiInfo api, InvokeResult result)
        {
            var sb = new StringBuilder();
            if (!result.Success)
            {
                sb.AppendLine("FAULT:");
                sb.AppendLine(result.Fault);
                return sb.ToString();
            }
            sb.AppendLine($"{api.UniqueName} — success");
            if (result.Results.Count == 0) sb.AppendLine("(no output properties)");
            foreach (var kv in result.Results)
                sb.AppendLine($"  {kv.Key} = {kv.Value}");
            return sb.ToString();
        }

        // ---- catalog export (US-PLUGIN6.5.1) ----

        private void tsmiExportHtml_Click(object sender, EventArgs e) =>
            Export("HTML report (*.html)|*.html", "custom-api-catalog.html", CustomApiDoc.ToHtml, useBom: false);

        private void tsmiExportMarkdown_Click(object sender, EventArgs e) =>
            Export("Markdown file (*.md)|*.md", "custom-api-catalog.md", CustomApiDoc.ToMarkdown, useBom: false);

        private void tsmiExportCsv_Click(object sender, EventArgs e) =>
            Export("CSV file (*.csv)|*.csv", "custom-api-catalog.csv", CustomApiDoc.ToCsv, useBom: true);

        private void Export(string filter, string defaultName, Func<CustomApiCatalog, string> render, bool useBom)
        {
            if (_catalog == null || _catalog.Count == 0) return;
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dlg.FileName, render(_catalog), new UTF8Encoding(useBom));
                    SetStatusMessage($"Exported {_catalog.Count} API(s) to {Path.GetFileName(dlg.FileName)}");
                }
                catch (Exception ex) { ShowError(ex, "Export failed"); }
            }
        }
    }

    /// <summary>Plain serializable settings POCO — last filter only. No presets with secret values; no connection details.</summary>
    public class CustomApiExplorerSettings
    {
        public string LastFilter { get; set; }
    }
}
