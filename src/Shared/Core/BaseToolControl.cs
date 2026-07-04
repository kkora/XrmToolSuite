using System;
using System.ComponentModel;
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
