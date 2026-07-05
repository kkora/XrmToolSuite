using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using XrmToolSuite.FormPerformanceAnalyzer.Analysis;
using EntityItem = XrmToolSuite.FormPerformanceAnalyzer.FormPerformanceAnalyzerControl.EntityItem;

namespace XrmToolSuite.FormPerformanceAnalyzer
{
    /// <summary>
    /// A modal multi-select table picker (checked list + filter box). Selecting nothing means "all tables".
    /// Pure WinForms, no Dataverse — the table list is supplied by the caller after it loads metadata off
    /// the UI thread.
    /// </summary>
    internal sealed class TablePickerDialog : Form
    {
        private readonly CheckedListBox _list = new CheckedListBox();
        private readonly TextBox _filter = new TextBox();
        private readonly List<EntityItem> _all;

        public List<string> SelectedLogicalNames { get; private set; } = new List<string>();

        public TablePickerDialog(List<EntityItem> tables, IEnumerable<string> preselected)
        {
            _all = tables ?? new List<EntityItem>();
            var selectedSet = new HashSet<string>(preselected ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            Text = "Select tables to analyze";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            ClientSize = new Size(420, 480);
            MinimizeBox = false;
            MaximizeBox = false;

            var lblHint = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(8, 8, 8, 0),
                Text = "Check the tables to scope the scan. Leave everything unchecked to analyze every main form."
            };

            _filter.Dock = DockStyle.Top;
            _filter.Margin = new Padding(8);
            _filter.TextChanged += (s, e) => ApplyFilter(selectedSet);

            _list.Dock = DockStyle.Fill;
            _list.CheckOnClick = true;
            _list.IntegralHeight = false;

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(8)
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var clear = new Button { Text = "Clear all", AutoSize = true };
            clear.Click += (s, e) => { for (int i = 0; i < _list.Items.Count; i++) _list.SetItemChecked(i, false); };
            pnlButtons.Controls.Add(ok);
            pnlButtons.Controls.Add(cancel);
            pnlButtons.Controls.Add(clear);

            Controls.Add(_list);
            Controls.Add(_filter);
            Controls.Add(lblHint);
            Controls.Add(pnlButtons);

            AcceptButton = ok;
            CancelButton = cancel;

            ApplyFilter(selectedSet);
            FormClosing += (s, e) =>
            {
                if (DialogResult == DialogResult.OK)
                    SelectedLogicalNames = _list.CheckedItems.Cast<EntityItem>()
                        .Select(i => i.LogicalName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            };
        }

        private void ApplyFilter(HashSet<string> selectedSet)
        {
            // Preserve current checks before repopulating.
            foreach (EntityItem item in _list.CheckedItems)
                selectedSet.Add(item.LogicalName);

            var text = _filter.Text?.Trim();
            IEnumerable<EntityItem> items = _all;
            if (!string.IsNullOrWhiteSpace(text))
                items = _all.Where(i => i.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);

            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var i in items)
                _list.Items.Add(i, selectedSet.Contains(i.LogicalName));
            _list.EndUpdate();
        }
    }

    /// <summary>
    /// A modal editor for the scoring weights, band thresholds, and rule-trigger thresholds, with a
    /// reset-to-defaults button. Works on a clone; <see cref="Result"/> holds the edited settings on OK.
    /// </summary>
    internal sealed class ScoreSettingsDialog : Form
    {
        private readonly List<(NumericUpDown ctrl, Action<FormSettings, decimal> set, Func<FormSettings, decimal> get)> _binders
            = new List<(NumericUpDown, Action<FormSettings, decimal>, Func<FormSettings, decimal>)>();
        private FormSettings _working;
        private readonly TableLayoutPanel _grid;

        public FormSettings Result { get; private set; }

        public ScoreSettingsDialog(FormSettings current)
        {
            _working = (current ?? new FormSettings()).Clone();

            Text = "Form scoring settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            ClientSize = new Size(440, 560);
            MinimizeBox = false;
            MaximizeBox = false;

            _grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            AddHeader("Per-unit weights");
            AddWeight("Visible field (above the fold)", s => s.WeightPerVisibleField, (s, v) => s.WeightPerVisibleField = v);
            AddWeight("Hidden field", s => s.WeightPerHiddenField, (s, v) => s.WeightPerHiddenField = v);
            AddWeight("Tab", s => s.WeightPerTab, (s, v) => s.WeightPerTab = v);
            AddWeight("Section", s => s.WeightPerSection, (s, v) => s.WeightPerSection = v);
            AddWeight("Custom / PCF control", s => s.WeightPerCustomControl, (s, v) => s.WeightPerCustomControl = v);
            AddWeight("Subgrid", s => s.WeightPerSubgrid, (s, v) => s.WeightPerSubgrid = v);
            AddWeight("Quick-view control", s => s.WeightPerQuickView, (s, v) => s.WeightPerQuickView = v);
            AddWeight("Script library", s => s.WeightPerJsLibrary, (s, v) => s.WeightPerJsLibrary = v);
            AddWeight("OnLoad handler", s => s.WeightPerOnLoadHandler, (s, v) => s.WeightPerOnLoadHandler = v);
            AddWeight("OnChange handler", s => s.WeightPerOnChangeHandler, (s, v) => s.WeightPerOnChangeHandler = v);
            AddWeight("Tab state-change handler", s => s.WeightPerTabStateChangeHandler, (s, v) => s.WeightPerTabStateChangeHandler = v);
            AddWeight("Business rule", s => s.WeightPerBusinessRule, (s, v) => s.WeightPerBusinessRule = v);

            AddHeader("Band thresholds (score ≥ value)");
            AddInt("Moderate", s => s.ModerateThreshold, (s, v) => s.ModerateThreshold = v, 100);
            AddInt("Heavy", s => s.HeavyThreshold, (s, v) => s.HeavyThreshold = v, 100);
            AddInt("Critical", s => s.CriticalThreshold, (s, v) => s.CriticalThreshold = v, 100);

            AddHeader("Rule-trigger thresholds");
            AddInt("Max above-the-fold fields", s => s.MaxAboveFoldFields, (s, v) => s.MaxAboveFoldFields = v, 500);
            AddInt("Max tabs", s => s.MaxTabs, (s, v) => s.MaxTabs = v, 100);
            AddInt("Max subgrids", s => s.MaxSubgrids, (s, v) => s.MaxSubgrids = v, 100);
            AddInt("Max quick views", s => s.MaxQuickViews, (s, v) => s.MaxQuickViews = v, 100);
            AddInt("Max custom controls", s => s.MaxCustomControls, (s, v) => s.MaxCustomControls = v, 100);
            AddInt("Max script libraries", s => s.MaxScriptLibraries, (s, v) => s.MaxScriptLibraries = v, 100);

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 46,
                Padding = new Padding(8)
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var reset = new Button { Text = "Reset to defaults", AutoSize = true };
            reset.Click += (s, e) =>
            {
                _working.ApplyDefaults();
                foreach (var b in _binders) b.ctrl.Value = Clamp(b.ctrl, b.get(_working));
            };
            pnlButtons.Controls.Add(ok);
            pnlButtons.Controls.Add(cancel);
            pnlButtons.Controls.Add(reset);

            Controls.Add(_grid);
            Controls.Add(pnlButtons);
            AcceptButton = ok;
            CancelButton = cancel;

            FormClosing += (s, e) =>
            {
                if (DialogResult != DialogResult.OK) return;
                foreach (var b in _binders) b.set(_working, b.ctrl.Value);
                Result = _working;
            };
        }

        private void AddHeader(string text)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font(Font, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 10, 0, 4)
            };
            _grid.Controls.Add(lbl);
            _grid.SetColumnSpan(lbl, 2);
        }

        private void AddWeight(string label, Func<FormSettings, double> get, Action<FormSettings, double> set)
        {
            var num = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 1,
                Increment = 0.1m,
                Minimum = 0m,
                Maximum = 100m,
                Value = (decimal)Math.Max(0, Math.Min(100, get(_working)))
            };
            AddRow(label, num);
            _binders.Add((num, (s, v) => set(s, (double)v), s => (decimal)get(s)));
        }

        private void AddInt(string label, Func<FormSettings, int> get, Action<FormSettings, int> set, int max)
        {
            var num = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                DecimalPlaces = 0,
                Increment = 1m,
                Minimum = 0m,
                Maximum = max,
                Value = Math.Max(0, Math.Min(max, get(_working)))
            };
            AddRow(label, num);
            _binders.Add((num, (s, v) => set(s, (int)v), s => get(s)));
        }

        private void AddRow(string label, Control editor)
        {
            var lbl = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false };
            _grid.Controls.Add(lbl);
            _grid.Controls.Add(editor);
        }

        private static decimal Clamp(NumericUpDown ctrl, decimal value) =>
            value < ctrl.Minimum ? ctrl.Minimum : value > ctrl.Maximum ? ctrl.Maximum : value;
    }

    /// <summary>Side-by-side metric comparison of two scored forms (read-only, no writes).</summary>
    internal sealed class CompareDialog : Form
    {
        public CompareDialog(FormScore a, FormScore b)
        {
            Text = "Compare forms";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 480);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            MinimizeBox = false;
            MaximizeBox = false;

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8),
                Text = $"A: {a.FormName} [{a.Entity}] — score {a.Score} ({a.Band})\r\n" +
                       $"B: {b.FormName} [{b.Entity}] — score {b.Score} ({b.Band})",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("m", "Metric");
            grid.Columns.Add("a", "A");
            grid.Columns.Add("b", "B");
            grid.Columns.Add("d", "Delta (B−A)");
            grid.Columns["a"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["b"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            grid.Columns["d"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            grid.Rows.Add("Score", a.Score, b.Score, Delta(a.Score, b.Score));

            // Metric rows are produced in the same order by the scorer, so pair them by label.
            var mapA = a.Metrics.ToDictionary(x => x.Label, x => x.Value, StringComparer.OrdinalIgnoreCase);
            foreach (var mb in b.Metrics)
            {
                mapA.TryGetValue(mb.Label, out var av);
                int? na = ParseInt(av);
                int? nb = ParseInt(mb.Value);
                string delta = (na.HasValue && nb.HasValue) ? Delta(na.Value, nb.Value) : "";
                int rowIndex = grid.Rows.Add(mb.Label, av ?? "0", mb.Value, delta);
                if (na.HasValue && nb.HasValue && nb.Value != na.Value)
                    grid.Rows[rowIndex].Cells["d"].Style.BackColor =
                        nb.Value > na.Value ? Color.FromArgb(255, 224, 178) : Color.FromArgb(226, 240, 217);
            }

            var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 32 };

            Controls.Add(grid);
            Controls.Add(header);
            Controls.Add(close);
            AcceptButton = close;
        }

        private static string Delta(int a, int b)
        {
            int d = b - a;
            return d > 0 ? "+" + d.ToString(CultureInfo.InvariantCulture) : d.ToString(CultureInfo.InvariantCulture);
        }

        private static int? ParseInt(string s) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : (int?)null;
    }
}
