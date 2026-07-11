using System;
using System.Drawing;
using System.Windows.Forms;

namespace XrmToolSuite.Core.Summarization
{
    /// <summary>
    /// The shared "AI settings" dialog used by every AI-summary tool (so they stay consistent): provider
    /// picker, model id with clickable Low/Mid/High suggestions, an API-key box (auto-disabled for local
    /// providers like Ollama), a provider-tailored hint, and an optional "auto-run after analysis" toggle.
    /// The API key is session-only and is returned to the caller, never stored here.
    /// </summary>
    public static class AiSettingsDialog
    {
        public sealed class Result
        {
            public bool Ok;
            public AiProvider Provider;
            public string ModelId;
            public string ApiKey;   // session-only; caller decides whether/where to hold it
            public bool AutoRun;
        }

        /// <param name="showAutoRun">show the "Auto-generate summary after each analysis" checkbox.</param>
        public static Result Show(IWin32Window owner, string currentProvider, string currentModelId,
            string currentApiKey, bool autoRun = false, bool showAutoRun = false)
        {
            var providers = AiProviderCatalog.All;
            var result = new Result { Ok = false };

            using (var f = new Form
            {
                Text = "AI settings", Width = 660, Height = 372,
                FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false
            })
            {
                var lblP = new Label { Text = "AI provider:", Location = new Point(14, 18), AutoSize = true };
                var cmb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(150, 14), Width = 476 };
                foreach (var p in providers) cmb.Items.Add(p.DisplayName);
                cmb.SelectedIndex = Math.Max(0, Array.FindIndex(providers, x => x.Provider == AiProviderCatalog.Parse(currentProvider)));

                var lblM = new Label { Text = "Model:", Location = new Point(14, 54), AutoSize = true };
                var txtModel = new TextBox { Location = new Point(150, 50), Width = 476, Text = currentModelId ?? "" };
                var lblSug = new Label { Text = "Suggested (click to use):", Location = new Point(150, 78), AutoSize = true, ForeColor = Color.DimGray };
                var bLow = new Button { Location = new Point(150, 98), Width = 154, Height = 26, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
                var bMid = new Button { Location = new Point(311, 98), Width = 154, Height = 26, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
                var bHigh = new Button { Location = new Point(472, 98), Width = 154, Height = 26, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };

                void RefreshSuggestions()
                {
                    var info = providers[cmb.SelectedIndex];
                    bLow.Text = "Low: " + info.Low; bLow.Tag = info.Low;
                    bMid.Text = "Mid: " + info.Mid; bMid.Tag = info.Mid;
                    bHigh.Text = "High: " + info.High; bHigh.Tag = info.High;
                }
                bLow.Click += (s, e) => txtModel.Text = (string)bLow.Tag;
                bMid.Click += (s, e) => txtModel.Text = (string)bMid.Tag;
                bHigh.Click += (s, e) => txtModel.Text = (string)bHigh.Tag;

                var lblK = new Label { Text = "API key:", Location = new Point(14, 146), AutoSize = true };
                var txtKey = new TextBox { UseSystemPasswordChar = true, Location = new Point(150, 142), Width = 476, Text = currentApiKey ?? "" };
                var lblKn = new Label { Location = new Point(150, 166), Width = 476, Height = 70, ForeColor = Color.DimGray };

                void SyncProvider()
                {
                    var info = providers[cmb.SelectedIndex];
                    txtKey.Enabled = info.RequiresApiKey; // local providers (Ollama) need no key
                    lblKn.Text = AiProviderCatalog.KeyHint(info.Provider);
                }
                cmb.SelectedIndexChanged += (s, e) =>
                {
                    RefreshSuggestions();
                    txtModel.Text = providers[cmb.SelectedIndex].Mid; // switching provider retargets the model
                    SyncProvider();
                };
                RefreshSuggestions();
                SyncProvider();
                if (string.IsNullOrWhiteSpace(txtModel.Text)) txtModel.Text = providers[cmb.SelectedIndex].Mid;

                var chkAuto = new CheckBox
                {
                    Text = "Auto-generate summary after each analysis",
                    Location = new Point(150, 244), AutoSize = true, Checked = autoRun, Visible = showAutoRun
                };

                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(446, 286), Size = new Size(90, 28) };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(546, 286), Size = new Size(90, 28) };
                f.Controls.AddRange(new Control[] { lblP, cmb, lblM, txtModel, lblSug, bLow, bMid, bHigh, lblK, txtKey, lblKn, chkAuto, ok, cancel });
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                if (f.ShowDialog(owner) == DialogResult.OK)
                {
                    result.Ok = true;
                    result.Provider = providers[cmb.SelectedIndex].Provider;
                    result.ModelId = txtModel.Text.Trim();
                    result.ApiKey = txtKey.Text.Trim();
                    result.AutoRun = chkAuto.Checked;
                }
                return result;
            }
        }
    }
}
