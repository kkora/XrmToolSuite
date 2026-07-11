using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using XrmToolSuite.DeploymentRiskAnalyzer.Analyzers;
using XrmToolSuite.DeploymentRiskAnalyzer.Models;
using XrmToolSuite.DeploymentRiskAnalyzer.Reporting;
using XrmToolSuite.DeploymentRiskAnalyzer.Scoring;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.Core.Summarization;
using ReportModel = XrmToolSuite.Core.Analysis.ReportModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
// Both System.Windows.Forms and Microsoft.Xrm.Sdk define a Label type; this control only
// uses the WinForms one, so alias it to disambiguate (CS0104).
using Label = System.Windows.Forms.Label;
// McTools.Xrm.Connection also defines a MetadataCache; the suite uses its own (CS0104).
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.DeploymentRiskAnalyzer
{
    public partial class DeploymentRiskAnalyzerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        private const string TargetActionName = "TargetOrganization";

        private IOrganizationService _targetService;
        private string _targetName;
        private List<Entity> _solutions = new List<Entity>();
        private AnalysisResult _lastResult;
        private GuardSettings _settings = new GuardSettings();

        private readonly List<IAnalyzer> _allAnalyzers = new List<IAnalyzer>
        {
            new DependencyAnalyzer(),
            new EnvironmentVariableAnalyzer(),
            new FlowPluginAnalyzer(),
            new SecurityAnalyzer(),
            new SchemaConflictAnalyzer(),
            new DeletedComponentAnalyzer(),
            new FormAnalyzer(),
            new RibbonAnalyzer(),
            new PowerPagesAnalyzer()
        };

        // UI
        private ToolStrip _toolbar;
        private ToolStripButton _btnLoadSolutions, _btnAnalyze, _btnConnectTarget;
        private ToolStripComboBox _cmbSolutions;
        private ToolStripLabel _lblTarget;
        private ToolStripDropDownButton _btnExport;
        private ToolStripButton _btnAiSummary;
        private ToolStripDropDownButton _btnAiOptions;
        private ToolStripMenuItem _miAiIncludeComponents;
        private CheckedListBox _lstAnalyzers;

        // Summary generation (offline default + auditable AI opt-in). Key is session-only, never persisted.
        private readonly ISummaryGenerator _offlineGenerator = new TemplatedSummaryGenerator();
        private readonly ISummaryGenerator _aiGenerator = new AiSummaryGenerator();
        private string _sessionApiKey;
        private bool _aiConsentGiven;
        private DataGridView _grid;
        private TextBox _txtDetail;
        private Label _lblRisk, _lblScore, _lblCounts;
        private Panel _summaryPanel;

        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        public DeploymentRiskAnalyzerControl()
        {
            BuildUi();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _settings = LoadSettings<GuardSettings>();
            // Restore analyzer selection (default: all checked)
            for (var i = 0; i < _lstAnalyzers.Items.Count; i++)
                _lstAnalyzers.SetItemChecked(i,
                    !_settings.UncheckedAnalyzers.Contains(_lstAnalyzers.Items[i].ToString()));
            // Restore summary options (provider/model/auto persist; the API key never does)
            _miAiIncludeComponents.Checked = _settings.AiIncludeComponents;
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.UncheckedAnalyzers = _lstAnalyzers.Items.Cast<string>()
                .Where(n => !_lstAnalyzers.CheckedItems.Contains(n))
                .ToList();
            _settings.AiIncludeComponents = _miAiIncludeComponents.Checked;
            // Provider / model / auto-run are written into _settings by the AI settings dialog.
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        #region UI construction

        private void BuildUi()
        {
            SuspendLayout();
            Size = new Size(1200, 760);

            _toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, ImageScalingSize = new Size(20, 20) };

            _btnLoadSolutions = new ToolStripButton("Load solutions") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _btnLoadSolutions.Click += (s, e) => ExecuteMethod(LoadSolutions);

            _cmbSolutions = new ToolStripComboBox { Width = 380, DropDownStyle = ComboBoxStyle.DropDownList };

            _btnConnectTarget = new ToolStripButton("Connect target env…") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _btnConnectTarget.Click += (s, e) => AddAdditionalOrganization();

            _lblTarget = new ToolStripLabel("Target: (none)") { ForeColor = Color.DimGray };

            _btnAnalyze = new ToolStripButton("▶ Analyze") { DisplayStyle = ToolStripItemDisplayStyle.Text, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            _btnAnalyze.Click += (s, e) => ExecuteMethod(RunAnalysis);

            _btnExport = new ToolStripDropDownButton("Export") { Enabled = false };
            _btnExport.DropDownItems.Add("PDF report (executive)", null, (s, e) => Export("pdf"));
            _btnExport.DropDownItems.Add("HTML report (print → PDF)", null, (s, e) => Export("html"));
            _btnExport.DropDownItems.Add("Excel workbook (.xlsx)", null, (s, e) => Export("xlsx"));
            _btnExport.DropDownItems.Add("JSON (CI/CD)", null, (s, e) => Export("json"));
            _btnExport.DropDownItems.Add("Fix checklist (.md)", null, (s, e) => Export("md"));

            // AI / offline summary cluster
            _btnAiSummary = new ToolStripButton("AI summary")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text, Enabled = false,
                ToolTipText = "Generate an executive summary. Offline by default; AI is opt-in with a data-preview consent step."
            };
            _btnAiSummary.Click += (s, e) => ProduceSummary(interactive: true);

            _miAiIncludeComponents = new ToolStripMenuItem("Include component names") { CheckOnClick = true, Checked = true };
            var miSettings = new ToolStripMenuItem("Set API key…");
            miSettings.Click += (s, e) => ShowAiSettingsDialog();
            _btnAiOptions = new ToolStripDropDownButton("AI options");
            _btnAiOptions.DropDownItems.AddRange(new ToolStripItem[]
            {
                _miAiIncludeComponents, new ToolStripSeparator(), miSettings
            });

            _toolbar.Items.AddRange(new ToolStripItem[]
            {
                _btnLoadSolutions, new ToolStripLabel("Solution:"), _cmbSolutions,
                new ToolStripSeparator(), _btnConnectTarget, _lblTarget,
                new ToolStripSeparator(), _btnAnalyze,
                new ToolStripSeparator(), _btnAiSummary, _btnAiOptions, _btnExport,
                CreateHelpButton("Deployment Risk Analyzer")
            });

            // Summary strip
            _summaryPanel = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(27, 27, 47) };
            _lblRisk = new Label
            {
                Text = "—", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.White,
                AutoSize = true, Location = new Point(16, 12), Visible = false // shown once a result is bound
            };
            _lblScore = new Label
            {
                Text = "Run an analysis to see the deployment risk score.",
                Font = new Font("Segoe UI", 10), ForeColor = Color.Gainsboro, AutoSize = true, Location = new Point(140, 12)
            };
            _lblCounts = new Label
            {
                Text = "", Font = new Font("Segoe UI", 9), ForeColor = Color.Silver, AutoSize = true, Location = new Point(140, 36)
            };
            _summaryPanel.Controls.AddRange(new Control[] { _lblRisk, _lblScore, _lblCounts });

            // Left: analyzer selection
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 260, Padding = new Padding(8) };
            var lblAnalyzers = new Label { Text = "Analyzers", Dock = DockStyle.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), Height = 22 };
            _lstAnalyzers = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle };
            foreach (var a in _allAnalyzers) _lstAnalyzers.Items.Add(a.Name, true);
            leftPanel.Controls.Add(_lstAnalyzers);
            leftPanel.Controls.Add(lblAnalyzers);

            // Center: grid + detail
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                RowHeadersVisible = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None
            };
            _grid.Columns.Add(NewCol("Severity", 70));
            _grid.Columns.Add(NewCol("Category", 110));
            _grid.Columns.Add(NewCol("Finding", 220));
            _grid.Columns.Add(NewCol("Component", 160));
            _grid.Columns.Add(NewCol("Description", 380));
            _grid.SelectionChanged += Grid_SelectionChanged;
            _grid.CellFormatting += Grid_CellFormatting;

            _txtDetail = new TextBox
            {
                Dock = DockStyle.Bottom, Height = 110, Multiline = true, ReadOnly = true,
                ScrollBars = ScrollBars.Vertical, BackColor = Color.WhiteSmoke, Font = new Font("Segoe UI", 9)
            };

            // Draggable divider between the findings grid and the detail pane. Add order matters:
            // Fill first, splitter next, edge control last — the splitter resizes the edge control.
            var detailSplitter = new Splitter
            {
                Dock = DockStyle.Bottom, Height = 5, BackColor = SystemColors.ControlLight,
                MinSize = 60, MinExtra = 120
            };

            var centerPanel = new Panel { Dock = DockStyle.Fill };
            centerPanel.Controls.Add(_grid);
            centerPanel.Controls.Add(detailSplitter);
            centerPanel.Controls.Add(_txtDetail);

            // Draggable divider between the analyzers list and the results area.
            var leftSplitter = new Splitter
            {
                Dock = DockStyle.Left, Width = 5, BackColor = SystemColors.ControlLight,
                MinSize = 150, MinExtra = 300
            };

            Controls.Add(centerPanel);
            Controls.Add(leftSplitter);
            Controls.Add(leftPanel);
            Controls.Add(_summaryPanel);
            Controls.Add(_toolbar);
            ResumeLayout();
        }

        private static DataGridViewTextBoxColumn NewCol(string name, int fillWeight) =>
            new DataGridViewTextBoxColumn { HeaderText = name, Name = name, FillWeight = fillWeight, SortMode = DataGridViewColumnSortMode.Automatic };

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != 0 || e.Value == null) return;
            switch (e.Value.ToString())
            {
                case "Critical": e.CellStyle.BackColor = Color.FromArgb(164, 38, 44); e.CellStyle.ForeColor = Color.White; break;
                case "High": e.CellStyle.BackColor = Color.FromArgb(209, 52, 56); e.CellStyle.ForeColor = Color.White; break;
                case "Medium": e.CellStyle.BackColor = Color.FromArgb(247, 169, 36); e.CellStyle.ForeColor = Color.Black; break;
                case "Low": e.CellStyle.BackColor = Color.FromArgb(138, 136, 134); e.CellStyle.ForeColor = Color.White; break;
                case "Info": e.CellStyle.BackColor = Color.FromArgb(0, 120, 212); e.CellStyle.ForeColor = Color.White; break;
            }
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0 || !(_grid.SelectedRows[0].Tag is RiskFinding f))
            {
                _txtDetail.Text = "";
                return;
            }
            _txtDetail.Text =
                $"[{f.Severity}] {f.Title}\r\nComponent: {f.AffectedComponent}\r\n\r\n{f.Description}\r\n\r\nRECOMMENDATION: {f.Recommendation}" +
                (string.IsNullOrEmpty(f.HelpUrl) ? "" : $"\r\nDocs: {f.HelpUrl}");
        }

        #endregion

        #region Connections

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            if (actionName == TargetActionName)
            {
                _targetService = newService;
                _targetName = detail?.ConnectionName ?? "target";
                _lblTarget.Text = $"Target: {_targetName}";
                _lblTarget.ForeColor = Color.SeaGreen;
                SetStatusMessage($"Target environment: {_targetName}");
                return; // keep the primary (source) connection untouched
            }

            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear(); // suite-level cache; environments differ
            _solutions.Clear();
            _cmbSolutions.Items.Clear();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        private void AddAdditionalOrganization()
        {
            // Raises the standard XrmToolBox connection dialog; result arrives in UpdateConnection with our action name.
            RaiseRequestConnectionEvent(new RequestConnectionEventArgs
            {
                ActionName = TargetActionName,
                Control = this
            });
        }

        #endregion

        #region Load solutions

        private void LoadSolutions()
        {
            RunAsync(
                "Loading unmanaged & managed solutions…",
                worker =>
                {
                    var qe = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("friendlyname", "uniquename", "version", "ismanaged", "publisherid"),
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("isvisible", ConditionOperator.Equal, true)
                            }
                        },
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };
                    return Service.RetrieveAll(qe, worker: worker)
                        .Where(s =>
                        {
                            var u = s.GetAttributeValue<string>("uniquename");
                            return u != "Default" && u != "Active";
                        })
                        .ToList();
                },
                solutions =>
                {
                    _solutions = solutions;
                    _cmbSolutions.Items.Clear();
                    foreach (var s in _solutions)
                        _cmbSolutions.Items.Add(
                            $"{s.GetAttributeValue<string>("friendlyname")} ({s.GetAttributeValue<string>("uniquename")}) v{s.GetAttributeValue<string>("version")}" +
                            (s.GetAttributeValue<bool?>("ismanaged") == true ? " [managed]" : ""));
                    // Widen the dropdown list to the longest name so full solution names are visible
                    // (independent of the toolbar combo's own width).
                    int dropWidth = _cmbSolutions.Width;
                    foreach (var item in _cmbSolutions.Items)
                        dropWidth = Math.Max(dropWidth, TextRenderer.MeasureText(item.ToString(), _cmbSolutions.Font).Width);
                    _cmbSolutions.ComboBox.DropDownWidth = dropWidth + SystemInformation.VerticalScrollBarWidth + 8;
                    if (_cmbSolutions.Items.Count > 0) _cmbSolutions.SelectedIndex = 0;
                    SetStatusMessage($"{_solutions.Count} solution(s) loaded");
                });
        }

        #endregion

        #region Analysis

        private void RunAnalysis()
        {
            if (_cmbSolutions.SelectedIndex < 0)
            {
                MessageBox.Show(this, "Load and select a solution first.", "Deployment Risk Analyzer",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var solution = _solutions[_cmbSolutions.SelectedIndex];
            var selected = _lstAnalyzers.CheckedItems.Cast<string>().ToHashSet();
            var analyzers = _allAnalyzers.Where(a => selected.Contains(a.Name)).ToList();
            if (analyzers.Count == 0)
            {
                MessageBox.Show(this, "Select at least one analyzer.", "Deployment Risk Analyzer",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var target = _targetService; // capture for the worker thread
            var sourceName = ConnectionDetail?.ConnectionName ?? "source";
            var targetName = _targetName;

            RunAsync(
                "Analyzing solution…",
                worker =>
                {
                    var ctx = new AnalyzerContext(Service, target, solution);
                    worker.ReportProgress(0, "Loading solution components…");
                    ctx.LoadComponents();

                    var result = new AnalysisResult
                    {
                        SolutionUniqueName = ctx.SolutionUniqueName,
                        SolutionFriendlyName = solution.GetAttributeValue<string>("friendlyname"),
                        SolutionVersion = ctx.SolutionVersion,
                        SolutionIsManaged = ctx.SolutionIsManaged,
                        SourceEnvironment = sourceName,
                        TargetEnvironment = targetName
                    };

                    foreach (var analyzer in analyzers)
                    {
                        worker.ReportProgress(0, $"Running: {analyzer.Name}…");
                        try
                        {
                            var findings = analyzer.Analyze(ctx, msg => worker.ReportProgress(0, msg));
                            result.Findings.AddRange(findings);
                            result.AnalyzersRun.Add(analyzer.Name);
                        }
                        catch (Exception ex)
                        {
                            result.AnalyzersSkipped.Add(analyzer.Name);
                            result.Findings.Add(new RiskFinding(analyzer.Category, Severity.Info,
                                $"{analyzer.Name} failed",
                                ex.Message, ctx.SolutionUniqueName,
                                "Check permissions/connectivity and re-run this analyzer."));
                        }
                    }

                    RiskScoreCalculator.Apply(result);
                    return result;
                },
                result =>
                {
                    _lastResult = result;
                    BindResult(_lastResult);
                    _btnExport.Enabled = true;
                    _btnAiSummary.Enabled = true;
                    SetStatusMessage($"Analysis complete — {result.Risk} risk, score {result.Score}/100, {result.Findings.Count} finding(s)");
                    if (_settings.AiSummaryAutoRun) ProduceSummary(interactive: false);
                });
        }

        private void BindResult(AnalysisResult r)
        {
            _grid.Rows.Clear();
            foreach (var f in r.Findings.OrderByDescending(x => x.Severity).ThenBy(x => x.Category))
            {
                int idx = _grid.Rows.Add(f.Severity.ToString(), f.Category.ToString(), f.Title, f.AffectedComponent, f.Description);
                _grid.Rows[idx].Tag = f;
            }

            _lblRisk.Visible = true;
            _lblRisk.Text = r.Risk.ToString().ToUpperInvariant();
            _lblRisk.ForeColor = r.Risk == OverallRisk.High ? Color.FromArgb(255, 99, 99)
                               : r.Risk == OverallRisk.Medium ? Color.FromArgb(247, 169, 36)
                               : Color.FromArgb(115, 209, 115);
            // Push the score/counts text clear of the risk word (its width varies: LOW/MEDIUM/HIGH),
            // so a wider label like "MEDIUM" no longer overlaps the text.
            int textLeft = _lblRisk.Location.X + TextRenderer.MeasureText(_lblRisk.Text, _lblRisk.Font).Width + 20;
            _lblScore.Left = textLeft;
            _lblCounts.Left = textLeft;
            _lblScore.Text = $"Score {r.Score}/100 — {r.SolutionFriendlyName} v{r.SolutionVersion}" +
                             (r.TargetEnvironment == null ? "   (no target connected — schema & target checks limited)" : $"   vs target '{r.TargetEnvironment}'");
            _lblCounts.Text = RiskScoreCalculator.Explain(r);
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_lastResult == null) return;

            string filter, defaultName = $"DeploymentRiskAnalyzer_{_lastResult.SolutionUniqueName}_{DateTime.Now:yyyyMMdd_HHmm}";
            switch (kind)
            {
                case "pdf": filter = "PDF report|*.pdf"; defaultName += ".pdf"; break;
                case "html": filter = "HTML report|*.html"; defaultName += ".html"; break;
                case "xlsx": filter = "Excel workbook|*.xlsx"; defaultName += ".xlsx"; break;
                case "json": filter = "JSON|*.json"; defaultName += ".json"; break;
                default: filter = "Markdown|*.md"; defaultName += ".md"; break;
            }

            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    var model = DeploymentReportModel.ToReportModel(_lastResult);
                    switch (kind)
                    {
                        case "pdf": PdfReportExporter.Export(model, dlg.FileName); break;
                        case "html": HtmlDashboardBuilder.Export(model, dlg.FileName); break;
                        case "xlsx": ExcelReportExporter.Export(model, dlg.FileName); break;
                        case "json": JsonReportExporter.Export(model, dlg.FileName); break;
                        default: FixChecklistGenerator.Export(model, dlg.FileName); break;
                    }
                    if (MessageBox.Show(this, "Report exported. Open it now?", "Deployment Risk Analyzer",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        System.Diagnostics.Process.Start(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex); }
            }
        }

        #endregion

        #region Summary (offline default + auditable AI opt-in)

        /// <summary>
        /// Produces a deployment summary. Offline template unless the AI opt-in is satisfied
        /// (key available + payload-preview consent given this session). <paramref name="interactive"/>
        /// false (auto-run) never shows dialogs — it uses AI only if already authorized, else offline.
        /// </summary>
        private void ProduceSummary(bool interactive)
        {
            if (_lastResult == null)
            {
                if (interactive)
                    MessageBox.Show(this, "Run an analysis first.", "Deployment Risk Analyzer",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var model = DeploymentReportModel.ToReportModel(_lastResult);
            var opts = TryBuildAiOptions(interactive, model);   // null => produce the offline summary
            var generator = opts != null ? _aiGenerator : _offlineGenerator;

            RunAsync(
                "Generating deployment summary…",
                worker => generator.Generate(model, opts, msg => worker.ReportProgress(0, msg)),
                summary =>
                {
                    summary.Text = SummaryFormatting.ToPlainText(summary.Text);
                    _lastResult.AiSummary = summary.Text;
                    if (interactive) ShowSummary(summary);
                    SetStatusMessage(summary.FromAi
                        ? "AI summary generated — included in exports."
                        : summary.Error != null
                            ? $"Offline summary (AI unavailable: {summary.Error})"
                            : "Offline summary generated — included in exports.");
                });
        }

        /// <summary>
        /// Resolves AI options if the opt-in is satisfied; returns null to fall back to the offline
        /// template. Interactive mode may prompt for the key and show the consent preview; auto-run
        /// mode never prompts (uses AI only if key + consent are already in place this session).
        /// </summary>
        private SummaryOptions TryBuildAiOptions(bool interactive, ReportModel model)
        {
            var provider = AiProviderCatalog.Parse(_settings.AiProvider);
            bool needsKey = AiProviderCatalog.Get(provider).RequiresApiKey; // Ollama (local) needs none
            var key = ResolveKey(provider);
            if (needsKey && string.IsNullOrWhiteSpace(key))
            {
                if (!interactive) return null;          // auto-run without a key → offline, no prompt
                ShowAiSettingsDialog();                 // asks for provider/model/key/auto
                provider = AiProviderCatalog.Parse(_settings.AiProvider); // may have changed
                needsKey = AiProviderCatalog.Get(provider).RequiresApiKey;
                key = ResolveKey(provider);
                if (needsKey && string.IsNullOrWhiteSpace(key)) return null; // still none → offline
            }

            bool includeComponents = _miAiIncludeComponents.Checked;
            string modelId = string.IsNullOrWhiteSpace(_settings.AiModelId)
                ? AiProviderCatalog.Get(provider).Mid : _settings.AiModelId;

            if (!_aiConsentGiven)
            {
                if (!interactive) return null;          // auto-run before consent → offline
                var preview = JsonConvert.SerializeObject(
                    SummaryPayloadBuilder.Build(model, includeComponents), Formatting.Indented,
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                if (!ShowConsentDialog(preview, AiProviderCatalog.Get(provider), modelId)) return null; // declined → offline
                _aiConsentGiven = true;
            }

            return new SummaryOptions
            {
                Provider = provider,
                ApiKey = key,
                ModelId = modelId,
                IncludeComponents = includeComponents,
                SystemPrompt = DeploymentReportModel.AiSystemPrompt
            };
        }

        /// <summary>Session key first; the ANTHROPIC_API_KEY env var is a convenience only for Anthropic.</summary>
        private string ResolveKey(AiProvider provider)
        {
            if (!string.IsNullOrWhiteSpace(_sessionApiKey)) return _sessionApiKey;
            if (provider == AiProvider.Anthropic) return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            return null;
        }

        /// <summary>
        /// AI settings dialog: pick a provider, choose/enter a model (with low/mid/high suggestions per
        /// provider), enter a session-only API key, and toggle auto-generate. Provider/model/auto persist;
        /// the key never does.
        /// </summary>
        private void ShowAiSettingsDialog()
        {
            var r = AiSettingsDialog.Show(this, _settings.AiProvider, _settings.AiModelId, _sessionApiKey,
                _settings.AiSummaryAutoRun, showAutoRun: true);
            if (!r.Ok) return;
            _settings.AiProvider = r.Provider.ToString();
            _settings.AiModelId = r.ModelId;
            _settings.AiSummaryAutoRun = r.AutoRun;
            if (!string.IsNullOrWhiteSpace(r.ApiKey)) _sessionApiKey = r.ApiKey;
        }

        /// <summary>Shows the exact JSON that will be sent (and the provider/host/model) for approval before the first AI call.</summary>
        private bool ShowConsentDialog(string payloadJson, AiProviderCatalog.Info provider, string model)
        {
            using (var f = new Form
            {
                Text = "Review data before sending to AI", Width = 660, Height = 540,
                FormBorderStyle = FormBorderStyle.Sizable, StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = true
            })
            {
                var lbl = new Label
                {
                    Dock = DockStyle.Top, Height = 60, Padding = new Padding(8),
                    Text = $"The JSON below (and nothing else) will be sent over HTTPS to {provider.DisplayName} " +
                           $"({provider.Host}), model '{model}'. It contains finding metadata only — " +
                           "no record data, credentials, or environment names. Proceed?"
                };
                var box = new TextBox
                {
                    Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false,
                    Dock = DockStyle.Fill, Font = new Font("Consolas", 9), Text = payloadJson
                };
                var bar = new Panel { Dock = DockStyle.Bottom, Height = 46 };
                var send = new Button { Text = "Send", DialogResult = DialogResult.OK, Size = new Size(120, 28), Top = 9 };
                var cancel = new Button { Text = "Cancel (offline)", DialogResult = DialogResult.Cancel, Size = new Size(130, 28), Top = 9 };
                void Layout() { send.Left = bar.Width - send.Width - 8; cancel.Left = send.Left - cancel.Width - 8; }
                bar.Resize += (s, e) => Layout();
                bar.Controls.AddRange(new Control[] { send, cancel });
                f.Controls.Add(box);
                f.Controls.Add(lbl);
                f.Controls.Add(bar);
                f.AcceptButton = send;
                f.CancelButton = cancel;
                Layout();
                return f.ShowDialog(this) == DialogResult.OK;
            }
        }

        /// <summary>Read-only summary viewer (formatted) with Copy / Export PDF; indicates AI vs offline source.</summary>
        private void ShowSummary(SummaryResult s)
        {
            using (var f = new Form
            {
                Text = s.FromAi ? "AI deployment summary" : "Deployment summary (offline)",
                Width = 680, Height = 520, StartPosition = FormStartPosition.CenterParent
            })
            {
                var box = new RichTextBox
                {
                    ReadOnly = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
                    Font = new Font("Segoe UI", 10.5f), BackColor = System.Drawing.SystemColors.Window,
                    Text = s.Text ?? "", WordWrap = true
                };
                box.Select(0, 0);
                BoldPrefixLines(box, new[] { "RECOMMENDATION", "Recommendation:", "Top risks:" });
                // Even padding on all sides via a container panel (RichTextBox ignores its own Padding).
                var pad = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14), BackColor = System.Drawing.SystemColors.Window };
                pad.Controls.Add(box);

                var bar = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                var note = new Label
                {
                    Dock = DockStyle.Left, Width = 260, TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 0, 0),
                    Text = s.FromAi ? "Generated by Anthropic — in exports."
                                    : s.Error != null ? "AI unavailable — offline summary."
                                                      : "Offline summary — in exports.",
                    ForeColor = s.Error != null ? Color.DarkRed : Color.DimGray
                };
                var close = new Button { Text = "Close", Dock = DockStyle.Right, Width = 90, DialogResult = DialogResult.OK };
                var copy = new Button { Text = "Copy", Dock = DockStyle.Right, Width = 90 };
                copy.Click += (a, e) => { try { Clipboard.SetText(s.Text ?? ""); } catch { /* clipboard busy */ } };
                var exportPdf = new Button { Text = "Export PDF", Dock = DockStyle.Right, Width = 110 };
                exportPdf.Click += (a, e) => Export("pdf"); // full executive PDF; embeds this summary
                bar.Controls.AddRange(new Control[] { note, close, copy, exportPdf });
                f.Controls.Add(pad);
                f.Controls.Add(bar);
                f.AcceptButton = close;
                f.ShowDialog(this);
            }
        }

        /// <summary>Bolds whole lines that start with any of the given prefixes (e.g. the recommendation line).</summary>
        private static void BoldPrefixLines(RichTextBox rtb, string[] prefixes)
        {
            int start = 0;
            foreach (var line in rtb.Lines)
            {
                var trimmed = line.TrimStart();
                foreach (var p in prefixes)
                {
                    if (trimmed.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    {
                        rtb.Select(start, line.Length);
                        rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
                        break;
                    }
                }
                start += line.Length + 1; // account for the newline separator
            }
            rtb.Select(0, 0);
        }

        #endregion

        // ShowError, LoadSettings, SaveSettings, SetStatusMessage inherited from BaseToolControl
    }

    /// <summary>Persisted via SettingsManager (BaseToolControl.Load/SaveSettings).</summary>
    public class GuardSettings
    {
        public List<string> UncheckedAnalyzers { get; set; } = new List<string>();

        // Summary options (the API key is session-only and never persisted).
        public bool AiSummaryAutoRun { get; set; }
        public bool AiIncludeComponents { get; set; } = true;
        public string AiProvider { get; set; } = "Anthropic";
        public string AiModelId { get; set; }
    }
}
