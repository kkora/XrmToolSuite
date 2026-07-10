using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.ServiceModel;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Args;
using XrmToolBox.Extensibility.Interfaces;

namespace XrmToolSuite.Core
{
    /// <summary>
    /// Base class for all XrmToolSuite tool controls.
    /// Wraps the common XrmToolBox plumbing: async work, status bar,
    /// settings persistence, and error handling.
    /// </summary>
    public abstract class BaseToolControl : PluginControlBase, IStatusBarMessenger
    {
        public event EventHandler<StatusBarMessageEventArgs> SendMessageToStatusBar;

        #region Help &amp; support

        // Suite-wide Help &amp; Support links. Every tool's Help button opens the same dialog so the
        // whole suite is consistent (see the Deployment Risk Analyzer for the original visual design).
        protected const string SuiteRepoUrl = "https://github.com/kkora/XrmToolSuite";
        protected const string SuiteDocsUrl = SuiteRepoUrl + "#readme";
        protected const string SuiteIssuesUrl = SuiteRepoUrl + "/issues/new";
        protected const string SuiteSupportUrl = "https://www.buymeacoffee.com/kkora";

        /// <summary>
        /// The per-tool README on GitHub (main branch), derived from the tool's assembly name — which matches
        /// its project folder across the suite (e.g. <c>XrmToolSuite.AttributeAuditor</c>). Used as the default
        /// target for the Help dialog's Documentation link and for each tool's <c>HelpUrl</c>, so every tool
        /// points at its own docs with no per-tool wiring. Falls back to the suite readme if the name is unknown.
        /// </summary>
        protected string ToolDocsUrl
        {
            get
            {
                var asm = GetType().Assembly.GetName().Name;
                return string.IsNullOrEmpty(asm)
                    ? SuiteDocsUrl
                    : $"{SuiteRepoUrl}/blob/main/src/Tools/{asm}/README.md";
            }
        }

        /// <summary>
        /// Builds the standard right-aligned "Help" toolbar button that opens <see cref="ShowHelpDialog"/>.
        /// Every tool MUST place one on its toolbar (a non-negotiable suite convention): just add the
        /// returned button to the tool's <see cref="ToolStrip"/>.
        /// </summary>
        protected ToolStripButton CreateHelpButton(string toolName = null, string docsUrl = null)
        {
            var btn = new ToolStripButton("Help")
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Alignment = ToolStripItemAlignment.Right,
                ToolTipText = "Documentation, report an issue, and support the plugin"
            };
            btn.Click += (s, e) => ShowHelpDialog(toolName, docsUrl);
            return btn;
        }

        /// <summary>
        /// Shows the shared Help &amp; Support dialog: documentation, a "report an issue" (GitHub) link,
        /// and a support link. Links open in the default browser via <see cref="Process.Start(string)"/>.
        /// Pass <paramref name="docsUrl"/> to override the Documentation link target; by default it points at
        /// the tool's own README (<see cref="ToolDocsUrl"/>).
        /// </summary>
        protected void ShowHelpDialog(string toolName = null, string docsUrl = null)
        {
            var title = string.IsNullOrEmpty(toolName) ? "Help & Support" : $"{toolName} — Help & Support";

            using (var dlg = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(460, 210),
                ShowInTaskbar = false
            })
            {
                var layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(16),
                    ColumnCount = 1,
                    RowCount = 4
                };

                // Use BMP symbols (Segoe UI / Segoe UI Symbol, present on all Windows) rather than
                // SMP emoji like 📖/🐞 (U+1F4xx): those fall back to a tofu box in the default WinForms
                // font on many machines, whereas ☕ (U+2615, BMP) renders — so keep all three in the BMP.
                layout.Controls.Add(HelpLink("ℹ Documentation",
                    "Read the suite documentation and per-tool guides.", docsUrl ?? ToolDocsUrl));
                layout.Controls.Add(HelpLink("⚠ Report an issue",
                    "Found a bug or have a request? Open a GitHub issue.", SuiteIssuesUrl));
                layout.Controls.Add(HelpLink("☕ Support the project",
                    "If these tools save you time, consider buying a coffee.", SuiteSupportUrl));

                var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Right, AutoSize = true };
                layout.Controls.Add(close);
                dlg.AcceptButton = close;

                dlg.Controls.Add(layout);
                dlg.ShowDialog(this);
            }
        }

        private static LinkLabel HelpLink(string title, string description, string url)
        {
            var link = new LinkLabel
            {
                Text = $"{title}\r\n{description}",
                AutoSize = false,
                Height = 46,
                Dock = DockStyle.Top,
                LinkArea = new LinkArea(0, title.Length)
            };
            link.LinkClicked += (s, e) => OpenUrl(url);
            return link;
        }

        /// <summary>Opens a URL in the default browser; swallows shell failures so the tool never crashes.
        /// Private (not protected) so it never collides with a tool's own URL-opening helper.</summary>
        private static void OpenUrl(string url)
        {
            try { Process.Start(url); }
            catch { /* no browser / blocked shell — nothing actionable for the user here */ }
        }

        /// <summary>
        /// After a successful export, asks the user whether to open the produced file and, if so, opens it
        /// with its default application. Swallows shell failures so the tool never crashes.
        /// </summary>
        protected void PromptOpenExportedFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var answer = MessageBox.Show(
                this,
                $"Export complete:\r\n{path}\r\n\r\nOpen the file now?",
                "Open exported file",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
            try { Process.Start(path); }
            catch (Exception ex) { LogError("Could not open exported file: " + ex); }
        }

        #endregion

        #region Status bar

        protected void SetStatusMessage(string message)
            => SendMessageToStatusBar?.Invoke(this, new StatusBarMessageEventArgs(message));

        protected void SetStatusProgress(int percent, string message)
            => SendMessageToStatusBar?.Invoke(this, new StatusBarMessageEventArgs(percent, message));

        #endregion

        #region Async work

        /// <summary>
        /// Runs work on a background thread with the standard XrmToolBox spinner.
        /// Report progress inside <paramref name="work"/> via worker.ReportProgress(0, "message").
        /// </summary>
        protected void RunAsync<TResult>(
            string message,
            Func<BackgroundWorker, TResult> work,
            Action<TResult> onCompleted,
            Action<Exception> onError = null)
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = message,
                Work = (worker, args) => args.Result = work(worker),
                ProgressChanged = args =>
                {
                    if (args.UserState is string s && s.Length > 0)
                        SetWorkingMessage(s);
                },
                PostWorkCallBack = args =>
                {
                    if (args.Error != null)
                    {
                        LogError(args.Error.ToString());
                        if (onError != null) onError(args.Error);
                        else ShowError(args.Error);
                        return;
                    }
                    onCompleted((TResult)args.Result);
                }
            });
        }

        /// <summary>Variant for work with no result.</summary>
        protected void RunAsync(
            string message,
            Action<BackgroundWorker> work,
            Action onCompleted = null,
            Action<Exception> onError = null)
        {
            RunAsync<object>(
                message,
                worker => { work(worker); return null; },
                _ => onCompleted?.Invoke(),
                onError);
        }

        #endregion

        #region Errors

        protected void ShowError(Exception ex, string caption = "Error")
        {
            var message = ex is FaultException<OrganizationServiceFault> fault
                ? fault.Detail?.Message ?? ex.Message
                : ex.Message;

            MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion

        #region Settings

        /// <summary>Loads tool settings, returning a fresh instance if none exist yet.</summary>
        protected TSettings LoadSettings<TSettings>() where TSettings : new()
        {
            return SettingsManager.Instance.TryLoad(GetType(), out TSettings settings)
                ? settings
                : new TSettings();
        }

        protected void SaveSettings<TSettings>(TSettings settings)
            => SettingsManager.Instance.Save(GetType(), settings);

        #endregion
    }
}
