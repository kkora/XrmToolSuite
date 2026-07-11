using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using IOrganizationService = Microsoft.Xrm.Sdk.IOrganizationService;
using Entity = Microsoft.Xrm.Sdk.Entity;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;
using XrmToolSuite.Core;
using XrmToolSuite.Core.Analysis;
using XrmToolSuite.Core.Reporting;
using XrmToolSuite.Core.Summarization;
using XrmToolSuite.AiSolutionReviewer.Analysis;
using XrmToolSuite.AiSolutionReviewer.Reporting;
using MetadataCache = XrmToolSuite.Core.MetadataCache;

namespace XrmToolSuite.AiSolutionReviewer
{
    /// <summary>
    /// AI-assisted architecture review of a Dataverse solution. Collectors gather structured facts across
    /// plugins, JavaScript, automation, ALM and governance; the AI layer turns them into an executive
    /// summary, recommendations, a prioritized backlog, and a sprint plan (offline deterministic fallback
    /// when no key). Exports to Word/PDF/HTML/Markdown/JSON. Follows the suite patterns.
    /// </summary>
    public partial class AiSolutionReviewerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin
    {
        public string RepositoryName => "XrmToolSuite";
        public string UserName => "kkora";
        public string HelpUrl => ToolDocsUrl; // per-tool README (BaseToolControl.ToolDocsUrl)

        private readonly List<IAnalyzer<ReviewContext>> _collectors = new List<IAnalyzer<ReviewContext>>
        {
            new PluginReviewCollector(),
            new ScriptReviewCollector(),
            new AutomationReviewCollector(),
            new AlmGovernanceReviewCollector(),
        };

        private ReviewSettings _settings = new ReviewSettings();
        private ReportModel _lastModel;
        private List<Entity> _solutions = new List<Entity>();

        private readonly ISummaryGenerator _offlineGenerator = new TemplatedSummaryGenerator();
        private readonly ISummaryGenerator _aiGenerator = new AiSummaryGenerator();
        private string _sessionApiKey;
        private bool _aiConsentGiven;

        private ComboBox _cboSolution;
        private DataGridView _gridFindings;
        private TextBox _txtDetail;
        private Label _lblScore, _lblBand;
        private ToolStripButton _btnReview, _btnAiSummary;
        private ToolStripDropDownButton _btnExport;
        private ToolStripMenuItem _miAiIncludeComponents;

        public AiSolutionReviewerControl()
        {
            BuildUi();
            Load += OnLoad;
        }

        #region UI

        private void BuildUi()
        {
            Dock = DockStyle.Fill;

            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

            var btnLoad = new ToolStripButton("Load solutions") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnLoad.Click += (s, e) => ExecuteMethod(LoadSolutions);

            _cboSolution = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };

            _btnReview = new ToolStripButton("▶ Collect facts") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            _btnReview.Click += (s, e) => ExecuteMethod(RunReview);

            _btnAiSummary = new ToolStripButton("★ Generate AI review") { DisplayStyle = ToolStripItemDisplayStyle.Text, Enabled = false };
            _btnAiSummary.Click += (s, e) => ProduceSummary(interactive: true);

            _btnExport = new ToolStripDropDownButton("Export") { DisplayStyle = ToolStripItemDisplayStyle.Text, Enabled = false };
            _btnExport.DropDownItems.Add("Word document (.docx)", null, (s, e) => Export("docx"));
            _btnExport.DropDownItems.Add("PDF report (.pdf)", null, (s, e) => Export("pdf"));
            _btnExport.DropDownItems.Add("HTML report (.html)", null, (s, e) => Export("html"));
            _btnExport.DropDownItems.Add("Markdown (.md)", null, (s, e) => Export("md"));
            _btnExport.DropDownItems.Add("JSON (.json)", null, (s, e) => Export("json"));

            var btnAiOptions = new ToolStripDropDownButton("AI options") { DisplayStyle = ToolStripItemDisplayStyle.Text };
            btnAiOptions.DropDownItems.Add("AI settings…", null, (s, e) => ShowAiSettingsDialog());
            _miAiIncludeComponents = new ToolStripMenuItem("Include component names in AI payload") { CheckOnClick = true, Checked = true };
            btnAiOptions.DropDownItems.Add(_miAiIncludeComponents);

            toolbar.Items.AddRange(new ToolStripItem[]
            {
                btnLoad, new ToolStripLabel("Solution:"),
                new ToolStripControlHost(_cboSolution), _btnReview, _btnAiSummary, new ToolStripSeparator(),
                btnAiOptions, _btnExport, CreateHelpButton("AI Solution Reviewer")
            });

            var summaryPanel = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(8, 6, 8, 6) };
            _lblBand = new Label { Text = "—", Font = new Font(Font.FontFamily, 15, FontStyle.Bold), AutoSize = true, Location = new Point(8, 6) };
            _lblScore = new Label { Text = "Load a solution, collect facts, then generate the AI review.", AutoSize = true, Location = new Point(8, 34) };
            summaryPanel.Controls.AddRange(new Control[] { _lblBand, _lblScore });

            _gridFindings = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = SystemColors.Window, BorderStyle = BorderStyle.None
            };
            _gridFindings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Severity", FillWeight = 12 });
            _gridFindings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Area", HeaderText = "Area", FillWeight = 18 });
            _gridFindings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Observation", HeaderText = "Observation", FillWeight = 40 });
            _gridFindings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Component", HeaderText = "Component", FillWeight = 30 });
            _gridFindings.CellFormatting += (s, e) =>
            {
                if (_gridFindings.Columns[e.ColumnIndex].Name == "Severity" && e.RowIndex >= 0)
                    SeverityCell(e);
            };
            _gridFindings.SelectionChanged += (s, e) =>
            {
                if (_gridFindings.CurrentRow?.Tag is Finding f)
                    _txtDetail.Text = $"[{f.Severity}] {f.Title}\r\nArea: {f.Category}\r\nComponent: {f.Component}\r\n\r\n{f.Description}\r\n\r\n→ {f.Recommendation}";
            };

            _txtDetail = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 340 };
            split.Panel1.Controls.Add(_gridFindings);
            split.Panel2.Controls.Add(_txtDetail);

            var body = new Panel { Dock = DockStyle.Fill };
            body.Controls.Add(split);
            body.Controls.Add(summaryPanel);

            Controls.Add(body);
            Controls.Add(toolbar);
        }

        private static void SeverityCell(DataGridViewCellFormattingEventArgs e)
        {
            switch (e.Value?.ToString())
            {
                case "Critical": e.CellStyle.BackColor = Color.FromArgb(164, 38, 44); e.CellStyle.ForeColor = Color.White; break;
                case "High": e.CellStyle.BackColor = Color.FromArgb(209, 52, 56); e.CellStyle.ForeColor = Color.White; break;
                case "Medium": e.CellStyle.BackColor = Color.FromArgb(247, 169, 36); e.CellStyle.ForeColor = Color.Black; break;
                case "Low": e.CellStyle.BackColor = Color.FromArgb(222, 222, 222); break;
            }
        }

        #endregion

        #region Lifecycle

        private void OnLoad(object sender, EventArgs e)
        {
            _settings = LoadSettings<ReviewSettings>();
            _miAiIncludeComponents.Checked = _settings.AiIncludeComponents;
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            _settings.AiIncludeComponents = _miAiIncludeComponents.Checked;
            SaveSettings(_settings);
            base.ClosingPlugin(info);
        }

        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail,
            string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            MetadataCache.Clear();
            _cboSolution.Items.Clear();
            _solutions.Clear();
            SetStatusMessage($"Connected to {detail?.ConnectionName}");
        }

        #endregion

        #region Data

        private void LoadSolutions()
        {
            RunAsync(
                "Loading solutions…",
                worker =>
                {
                    var qe = new QueryExpression("solution")
                    {
                        ColumnSet = new ColumnSet("uniquename", "friendlyname", "version", "ismanaged"),
                        Criteria =
                        {
                            Conditions =
                            {
                                new ConditionExpression("isvisible", ConditionOperator.Equal, true),
                                new ConditionExpression("uniquename", ConditionOperator.NotIn, "Default", "Active", "Basic")
                            }
                        },
                        Orders = { new OrderExpression("friendlyname", OrderType.Ascending) }
                    };
                    return Service.RetrieveAll(qe);
                },
                solutions =>
                {
                    _solutions = solutions;
                    _cboSolution.Items.Clear();
                    foreach (var s in solutions)
                        _cboSolution.Items.Add($"{s.GetAttributeValue<string>("friendlyname")} ({s.GetAttributeValue<string>("version")})");
                    if (_cboSolution.Items.Count > 0) _cboSolution.SelectedIndex = 0;
                    SetStatusMessage($"Loaded {solutions.Count} solution(s).");
                });
        }

        private void RunReview()
        {
            int idx = _cboSolution.SelectedIndex;
            if (idx < 0 || idx >= _solutions.Count)
            {
                MessageBox.Show(this, "Load and select a solution first.", "AI Solution Reviewer",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var solution = _solutions[idx];
            var service = Service;
            var envName = ConnectionDetail?.ConnectionName ?? "Dataverse environment";

            RunAsync(
                "Collecting solution facts…",
                worker =>
                {
                    var ctx = new ReviewContext(service, solution);
                    var run = AnalyzerRunner.Run(_collectors, ctx, msg => worker.ReportProgress(0, msg),
                        () => worker.CancellationPending);
                    return ReviewReport.Build(run, ctx.SolutionFriendlyName, ctx.SolutionUniqueName,
                        ctx.SolutionVersion, ctx.SolutionIsManaged, envName);
                },
                model =>
                {
                    _lastModel = model;
                    BindModel(model);
                    _btnExport.Enabled = true;
                    _btnAiSummary.Enabled = true;
                    SetStatusMessage($"Collected {model.Findings.Count} observation(s). Generate the AI review next.");
                });
        }

        private void BindModel(ReportModel model)
        {
            _gridFindings.Rows.Clear();
            foreach (var f in model.Findings.OrderByDescending(x => x.Severity).ThenBy(x => x.Category))
            {
                int i = _gridFindings.Rows.Add(f.Severity.ToString(), f.Category, f.Title, f.Component);
                _gridFindings.Rows[i].Tag = f;
            }
            _lblBand.Text = model.BandText();
            _lblBand.ForeColor = model.Band == ScoreBand.High ? Color.FromArgb(209, 52, 56)
                : model.Band == ScoreBand.Medium ? Color.FromArgb(200, 130, 0) : Color.FromArgb(16, 124, 16);
            _lblScore.Text = $"Concern score {model.Score}/100 · {model.Findings.Count} observations";
        }

        #endregion

        #region Export

        private void Export(string kind)
        {
            if (_lastModel == null) return;
            string filter;
            switch (kind)
            {
                case "docx": filter = "Word document|*.docx"; break;
                case "pdf": filter = "PDF report|*.pdf"; break;
                case "html": filter = "HTML report|*.html"; break;
                case "json": filter = "JSON|*.json"; break;
                default: filter = "Markdown|*.md"; break;
            }
            string defaultName = $"SolutionReview_{DateTime.Now:yyyyMMdd_HHmm}.{kind}";
            using (var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    switch (kind)
                    {
                        case "docx": WordReportExporter.Export(_lastModel, dlg.FileName); break;
                        case "pdf": PdfReportExporter.Export(_lastModel, dlg.FileName); break;
                        case "html": HtmlDashboardBuilder.Export(_lastModel, dlg.FileName); break;
                        case "json": JsonReportExporter.Export(_lastModel, dlg.FileName); break;
                        default: FixChecklistGenerator.Export(_lastModel, dlg.FileName); break;
                    }
                    if (MessageBox.Show(this, "Report exported. Open it now?", "AI Solution Reviewer",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        System.Diagnostics.Process.Start(dlg.FileName);
                }
                catch (Exception ex) { ShowError(ex); }
            }
        }

        #endregion

        #region Summary / AI review (offline default + auditable AI opt-in)

        private void ProduceSummary(bool interactive)
        {
            if (_lastModel == null)
            {
                if (interactive)
                    MessageBox.Show(this, "Collect facts first.", "AI Solution Reviewer",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var opts = TryBuildAiOptions(interactive, _lastModel);
            var generator = opts != null ? _aiGenerator : _offlineGenerator;

            RunAsync(
                "Generating solution review…",
                worker => generator.Generate(_lastModel, opts, msg => worker.ReportProgress(0, msg)),
                summary =>
                {
                    summary.Text = SummaryFormatting.ToPlainText(summary.Text);
                    _lastModel.AiSummary = summary.Text;
                    if (interactive) ShowSummary(summary);
                    SetStatusMessage(summary.FromAi
                        ? "AI review generated — included in exports."
                        : summary.Error != null ? $"Offline review (AI unavailable: {summary.Error})"
                        : "Offline review generated — included in exports.");
                });
        }

        private SummaryOptions TryBuildAiOptions(bool interactive, ReportModel model)
        {
            var provider = AiProviderCatalog.Parse(_settings.AiProvider);
            bool needsKey = AiProviderCatalog.Get(provider).RequiresApiKey; // Ollama (local) needs none
            var key = ResolveKey(provider);
            if (needsKey && string.IsNullOrWhiteSpace(key))
            {
                if (!interactive) return null;
                ShowAiSettingsDialog();
                provider = AiProviderCatalog.Parse(_settings.AiProvider);
                needsKey = AiProviderCatalog.Get(provider).RequiresApiKey;
                key = ResolveKey(provider);
                if (needsKey && string.IsNullOrWhiteSpace(key)) return null;
            }

            bool includeComponents = _miAiIncludeComponents.Checked;
            string modelId = string.IsNullOrWhiteSpace(_settings.AiModelId)
                ? AiProviderCatalog.Get(provider).Mid : _settings.AiModelId;

            if (!_aiConsentGiven)
            {
                if (!interactive) return null;
                var preview = JsonConvert.SerializeObject(
                    SummaryPayloadBuilder.Build(model, includeComponents), Formatting.Indented,
                    new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                if (!ShowConsentDialog(preview, AiProviderCatalog.Get(provider), modelId)) return null;
                _aiConsentGiven = true;
            }

            return new SummaryOptions
            {
                Provider = provider,
                ApiKey = key,
                ModelId = modelId,
                IncludeComponents = includeComponents,
                SystemPrompt = ReviewReport.AiSystemPrompt,
                MaxTokens = 2048
            };
        }

        private string ResolveKey(AiProvider provider)
        {
            if (!string.IsNullOrWhiteSpace(_sessionApiKey)) return _sessionApiKey;
            if (provider == AiProvider.Anthropic) return Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            return null;
        }

        private void ShowAiSettingsDialog()
        {
            var r = AiSettingsDialog.Show(this, _settings.AiProvider, _settings.AiModelId, _sessionApiKey);
            if (!r.Ok) return;
            _settings.AiProvider = r.Provider.ToString();
            _settings.AiModelId = r.ModelId;
            if (!string.IsNullOrWhiteSpace(r.ApiKey)) _sessionApiKey = r.ApiKey;
        }

        private bool ShowConsentDialog(string payloadJson, AiProviderCatalog.Info provider, string model)
        {
            using (var f = new Form
            {
                Text = "Send to AI service?", StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(560, 420), MinimizeBox = false, MaximizeBox = false
            })
            {
                var lbl = new Label
                {
                    Dock = DockStyle.Top, Height = 54, Padding = new Padding(8),
                    Text = $"This will send the following anonymized observations to {provider.DisplayName} ({provider.Host}), model '{model}'.\nNo record data, credentials, or environment names are included. Continue?"
                };
                var box = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Text = payloadJson, Font = new Font("Consolas", 8.5f) };
                var bar = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                var send = new Button { Text = "Send", DialogResult = DialogResult.OK, Location = new Point(384, 8), Width = 75 };
                var cancel = new Button { Text = "Cancel (use offline)", DialogResult = DialogResult.Cancel, Location = new Point(465, 8), Width = 85 };
                bar.Controls.AddRange(new Control[] { send, cancel });
                f.Controls.Add(box); f.Controls.Add(lbl); f.Controls.Add(bar);
                f.AcceptButton = send; f.CancelButton = cancel;
                return f.ShowDialog(this) == DialogResult.OK;
            }
        }

        private void ShowSummary(SummaryResult s)
        {
            using (var f = new Form
            {
                Text = s.FromAi ? "AI solution review" : "Solution review (offline)",
                StartPosition = FormStartPosition.CenterParent, ClientSize = new Size(640, 520)
            })
            {
                var box = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text = SummaryFormatting.ForTextBox(s.Text), Font = new Font("Segoe UI", 9.5f) };
                var bar = new Panel { Dock = DockStyle.Bottom, Height = 44 };
                var copy = new Button { Text = "Copy", Location = new Point(400, 8), Width = 70 };
                copy.Click += (o, e) => { if (!string.IsNullOrEmpty(s.Text)) Clipboard.SetText(s.Text); };
                var wordBtn = new Button { Text = "Export Word", Location = new Point(476, 8), Width = 90 };
                wordBtn.Click += (o, e) => Export("docx");
                var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Location = new Point(572, 8), Width = 60 };
                bar.Controls.AddRange(new Control[] { copy, wordBtn, close });
                f.Controls.Add(box); f.Controls.Add(bar);
                f.ShowDialog(this);
            }
        }

        #endregion
    }

    /// <summary>Persisted settings (plain POCO — never stores the API key or connection details).</summary>
    public class ReviewSettings
    {
        public bool AiIncludeComponents { get; set; } = true;
        public string AiProvider { get; set; } = "Anthropic";
        public string AiModelId { get; set; }
    }
}
