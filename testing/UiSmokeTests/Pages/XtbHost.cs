using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace XrmToolSuite.UiSmokeTests.Pages
{
    /// <summary>
    /// Page object for the XrmToolBox host window — launch/attach, the local "Tools" tab, filtering and
    /// opening a plugin, and reading a plugin's connected-state marker. Shared by the connected walkthrough
    /// (Tier-3b); the load-only smoke test (<see cref="XrmToolBoxSmokeTest"/>) keeps its own inlined helpers
    /// so this file can evolve without touching the green gate.
    ///
    /// The UIA quirks encoded here mirror the load smoke test: XrmToolBox self-relaunches at startup (attach
    /// by process name, not the launched handle), its tabs are DockPanelSuite tabs (not UIA TabItems, so we
    /// match by Name), and the Tools filter is driven via the Value pattern (typing Enter flips the host to
    /// the online Tool Library tab).
    /// </summary>
    public sealed class XtbHost : IDisposable
    {
        private UIA3Automation _automation = new UIA3Automation();
        private Application _app;

        /// <summary>True when we attached to a pre-existing XrmToolBox we did NOT launch (so Dispose won't kill it).</summary>
        public bool Attached { get; private set; }

        public AutomationElement Window { get; private set; }

        /// <summary>Launch XrmToolBox, attach to the live (post-relaunch) process, and return its main window.</summary>
        public AutomationElement LaunchAndAttach(string exePath, TimeSpan timeout)
        {
            Application.Launch(exePath);
            var proc = WaitForXtbProcess(timeout)
                ?? throw new InvalidOperationException("XrmToolBox process did not appear within the timeout.");
            _app = Application.Attach(proc);
            Window = _app.GetMainWindow(_automation, timeout)
                ?? throw new InvalidOperationException("XrmToolBox main window did not appear within the timeout.");
            return Window;
        }

        /// <summary>
        /// Attach to an ALREADY-RUNNING XrmToolBox (started and connected by hand). This is the reliable path
        /// for the suite's interactive (OnlineFederation/AD) connections: a human completes the OAuth/MFA that
        /// can't be automated, and the test drives the live, connected session. Dispose leaves it running.
        /// </summary>
        public AutomationElement AttachToRunning(TimeSpan timeout)
        {
            var proc = WaitForXtbProcess(timeout)
                ?? throw new InvalidOperationException(
                    "No running XrmToolBox found to attach to. Start XrmToolBox and connect your test org first, " +
                    "then run with XTB_ATTACH=1. See README.md Tier-3b.");
            _app = Application.Attach(proc);
            Attached = true;
            Window = _app.GetMainWindow(_automation, timeout)
                ?? throw new InvalidOperationException("Running XrmToolBox has no main window (is it minimized to tray?).");
            return Window;
        }

        public void Maximize()
        {
            try { Window.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Maximized); } catch { }
            try { Window.SetForeground(); } catch { }
        }

        /// <summary>
        /// Re-resolve the cached main-window element. FlaUI element handles can go STALE after XrmToolBox mutates
        /// its tree (e.g. once dozens of solutions load), after which every descendant query off the old handle
        /// throws/returns empty — even for always-present controls like the main "Connect" button. Call this
        /// before an interaction if a query unexpectedly finds nothing.
        /// </summary>
        public void RefreshWindow()
        {
            try
            {
                var w = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(10));
                if (w != null) Window = w;
            }
            catch { }
        }

        /// <summary>
        /// Rebuild the UIA connection from scratch: dispose and recreate the UIA3Automation, re-attach to the
        /// live XrmToolBox process, and re-resolve the main window. A big WinForms tree mutation (e.g. loading
        /// dozens of solutions into a toolbar combo) can poison the automation's element cache so that EVERY
        /// query throws COMException even after GetMainWindow — only a fresh automation clears it. Heavy, so
        /// call it only as a fallback when RefreshWindow didn't help.
        /// </summary>
        public void HardReset()
        {
            try { _automation.Dispose(); } catch { }
            _automation = new UIA3Automation();
            try
            {
                var proc = Process.GetProcessesByName("XrmToolBox")
                    .FirstOrDefault(p => { try { return !p.HasExited && p.MainWindowHandle != IntPtr.Zero; } catch { return false; } });
                if (proc != null)
                {
                    _app = Application.Attach(proc);
                    var w = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(20));
                    if (w != null) Window = w;
                }
            }
            catch { }
        }

        /// <summary>Close the online "Tool Library" tab (restored from the prior session) so only "Tools" remains.</summary>
        public void CloseToolLibraryTab()
        {
            try
            {
                var lib = Window.FindAllDescendants().FirstOrDefault(e =>
                {
                    try { return string.Equals(e.Name, "Tool Library", StringComparison.OrdinalIgnoreCase) && !e.IsOffscreen; }
                    catch { return false; }
                });
                if (lib == null) return;
                try { lib.Click(); } catch { }
                Thread.Sleep(600);
                try
                {
                    FlaUI.Core.Input.Keyboard.TypeSimultaneously(
                        FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL,
                        FlaUI.Core.WindowsAPI.VirtualKeyShort.F4);
                }
                catch { }
                Thread.Sleep(800);
            }
            catch { }
        }

        /// <summary>Select the local "Tools" tab (deployed plugins), not the online Tool Library.</summary>
        public void SelectToolsTab()
        {
            try
            {
                var tab = Window.FindAllDescendants()
                    .Where(e => { try { return string.Equals(e.Name, "Tools", StringComparison.OrdinalIgnoreCase) && !e.IsOffscreen; } catch { return false; } })
                    .OrderBy(e => { try { return e.BoundingRectangle.Top; } catch { return int.MaxValue; } })
                    .FirstOrDefault();
                if (tab == null) return;
                try { tab.Click(); } catch { try { tab.AsTabItem().Select(); } catch { } }
            }
            catch { }
        }

        /// <summary>
        /// Filter the Tools list to one plugin and double-click its tile to open it. Returns true if a tile
        /// with that exact name was found and activated. Self-corrects once if the list is stale.
        /// </summary>
        public bool OpenTool(string toolName)
        {
            SelectToolsTab();
            Thread.Sleep(500);
            SetSearchText(FindSearchBox(), toolName);
            Thread.Sleep(1400);

            var tile = FindToolTile(toolName);
            if (tile == null)
            {
                // Stale search box or the host flipped to the online Tool Library — re-assert and retry once.
                SelectToolsTab();
                Thread.Sleep(600);
                SetSearchText(FindSearchBox(), toolName);
                Thread.Sleep(1400);
                tile = FindToolTile(toolName);
            }
            if (tile == null) return false;

            try { tile.DoubleClick(); }
            catch
            {
                try { tile.Focus(); FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER); }
                catch { return false; }
            }
            return true;
        }

        /// <summary>
        /// Poll (up to <paramref name="timeout"/>) for a visible element whose Name contains
        /// <paramref name="marker"/> — e.g. "Connected to XTS-CI-TEST", written by a plugin's UpdateConnection.
        /// This is how we assert the opened tool actually received a live IOrganizationService.
        /// </summary>
        public bool WaitForText(string marker, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                if (VisibleNames().Any(n => n.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)) return true;
                Thread.Sleep(1000);
            }
            return false;
        }

        /// <summary>
        /// Poll for the opened tool's tab caption showing a connection suffix — XrmToolBox titles a plugin tab
        /// "&lt;Tool&gt; (&lt;ConnectionName&gt;)" when it opens WITH a live connection (just "&lt;Tool&gt;" when
        /// disconnected). This is the host's own, tool-agnostic "connected" signal. Returns the matched caption
        /// (e.g. "Deployment Risk Analyzer (XTS-CI-TEST)") or null on timeout.
        /// </summary>
        public string WaitForConnectedToolTab(string toolName, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                var caption = FindConnectedToolTabCaption(toolName);
                if (caption != null) return caption;
                Thread.Sleep(750);
            }
            return null;
        }

        private string FindConnectedToolTabCaption(string toolName)
        {
            var prefix = toolName + " (";
            try
            {
                foreach (var e in Window.FindAllDescendants())
                {
                    try
                    {
                        var name = (e.Name ?? "").Trim();
                        // "Tool (Conn)" or truncated "Tool (Con…" — both start with "<tool> (" and mean connected.
                        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return name;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>True if a visible, enabled control with this exact Name (e.g. a toolbar button) exists.</summary>
        public bool ButtonEnabled(string buttonName)
        {
            try
            {
                return Window.FindAllDescendants().Any(e =>
                {
                    try
                    {
                        return !e.IsOffscreen
                            && string.Equals((e.Name ?? "").Trim(), buttonName, StringComparison.OrdinalIgnoreCase)
                            && e.IsEnabled;
                    }
                    catch { return false; }
                });
            }
            catch { return false; }
        }

        // Control types that represent a clickable toolbar/menu item, in match-preference order.
        private static readonly ControlType[] ClickableTypes =
            { ControlType.Button, ControlType.SplitButton, ControlType.MenuItem, ControlType.Text };

        /// <summary>Click a visible control by exact Name (toolbar button label). Returns true if clicked.</summary>
        public bool ClickByName(string name) => ClickMatch(n => string.Equals(n.Trim(), name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Click a visible control whose Name CONTAINS the given text (case-insensitive). Use for toolbar
        /// buttons whose captions carry glyphs/ellipses ("▶ Analyze", "Connect target env…") that are awkward
        /// to match exactly. Returns true if something was clicked.
        /// </summary>
        public bool ClickByPartialName(string contains) =>
            ClickMatch(n => n.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>
        /// Find and click the first clickable element whose Name satisfies <paramref name="matches"/>. Queries
        /// are SCOPED by control type (ByControlType) rather than an unconditioned full-tree walk — the latter
        /// is unreliable on XrmToolBox's large WinForms tree (it can intermittently return an empty set after
        /// the tree mutates, e.g. once dozens of solutions load). Retries a few times to ride out transients.
        /// </summary>
        private bool ClickMatch(Func<string, bool> matches)
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                // Escalating recovery: re-resolve the window, then (if still broken) rebuild the whole UIA
                // connection — a heavy tree mutation can throw COMException on every query until we do.
                if (attempt == 1) RefreshWindow();
                if (attempt >= 2) HardReset();
                foreach (var ct in ClickableTypes)
                {
                    AutomationElement el = null;
                    try
                    {
                        el = Window.FindAllDescendants(cf => cf.ByControlType(ct))
                            .FirstOrDefault(e =>
                            {
                                try { return !e.IsOffscreen && matches(e.Name ?? ""); }
                                catch { return false; }
                            });
                    }
                    catch { }
                    if (el == null) continue;
                    try { el.AsButton().Invoke(); return true; } catch { }
                    try { el.Click(); return true; } catch { }
                }
                Thread.Sleep(700);
            }
            return false;
        }

        /// <summary>All top-level window titles for this process (main + any modal dialogs) — diagnostic.</summary>
        public System.Collections.Generic.List<string> DumpTopLevelWindows()
        {
            var result = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var w in _app.GetAllTopLevelWindows(_automation))
                {
                    try { result.Add($"\"{w.Title}\" (off={w.IsOffscreen}, modal={w.Patterns.Window.PatternOrDefault?.IsModal.ValueOrDefault})"); }
                    catch { result.Add("<unreadable window>"); }
                }
            }
            catch (Exception ex) { result.Add("<GetAllTopLevelWindows threw: " + ex.GetType().Name + ">"); }
            return result;
        }

        /// <summary>
        /// Names of all clickable (Button/SplitButton/MenuItem/Text) elements currently in the window — for
        /// diagnosing why a ClickByPartialName didn't match. Re-resolves the window first (in case it's stale)
        /// and uses scoped queries. Guarded; never throws.
        /// </summary>
        public System.Collections.Generic.List<string> DumpClickableNames()
        {
            RefreshWindow();
            var result = new System.Collections.Generic.List<string>();
            foreach (var ct in ClickableTypes)
            {
                try
                {
                    foreach (var e in Window.FindAllDescendants(cf => cf.ByControlType(ct)))
                    {
                        try { var n = (e.Name ?? "").Trim(); if (n.Length > 0) result.Add($"[{ct}] \"{n}\""); }
                        catch { }
                    }
                }
                catch { }
            }
            return result;
        }

        /// <summary>
        /// Every non-empty descendant Name that contains <paramref name="substr"/> (case-insensitive), with its
        /// control type — for diagnosing why a ClickByPartialName didn't match. Guarded; never throws.
        /// </summary>
        public System.Collections.Generic.List<string> DumpNames(string substr)
        {
            var result = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var e in Window.FindAllDescendants())
                {
                    try
                    {
                        var name = e.Name ?? "";
                        if (name.IndexOf(substr, StringComparison.OrdinalIgnoreCase) >= 0)
                            result.Add($"[{e.ControlType}] off={e.IsOffscreen} \"{name}\"");
                    }
                    catch { }
                }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// The DRA Solutions dropdown — the ComboBox in the SAME toolbar as the "Solution:" label. Scoping to
        /// that label's parent is essential: XrmToolBox's connection-selector dialog also has a ComboBox ("All
        /// connections"), and a naive "first ComboBox" grab picks the wrong one when that dialog is open.
        /// </summary>
        private AutomationElement FindSolutionsCombo()
        {
            try
            {
                var label = Window.FindAllDescendants().FirstOrDefault(e =>
                {
                    try { return string.Equals((e.Name ?? "").Trim(), "Solution:", StringComparison.OrdinalIgnoreCase); }
                    catch { return false; }
                });
                var parent = label?.Parent;
                if (parent != null)
                {
                    var combo = parent.FindAllDescendants(cf => cf.ByControlType(ControlType.ComboBox))
                        .FirstOrDefault(e => { try { return e.IsEnabled && !e.IsOffscreen; } catch { return false; } });
                    if (combo != null) return combo;
                }
            }
            catch { }

            // Fallback: any enabled ComboBox whose value is NOT the connection dialog's "All connections" filter.
            try
            {
                return Window.FindAllDescendants(cf => cf.ByControlType(ControlType.ComboBox))
                    .FirstOrDefault(e =>
                    {
                        try
                        {
                            if (!e.IsEnabled || e.IsOffscreen) return false;
                            var v = e.Patterns.Value.PatternOrDefault?.Value ?? "";
                            return v.IndexOf("All connections", StringComparison.OrdinalIgnoreCase) < 0;
                        }
                        catch { return false; }
                    });
            }
            catch { return null; }
        }

        /// <summary>
        /// Poll until the Solutions combo has a non-empty value (i.e. "Load solutions" finished and auto-selected
        /// the first entry). Returns the selected text, or null on timeout.
        /// </summary>
        public string WaitForComboPopulated(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                var v = ReadComboValue();
                if (!string.IsNullOrWhiteSpace(v)) return v;
                Thread.Sleep(750);
            }
            return null;
        }

        /// <summary>Current text/value of the Solutions combo (empty string if none/unreadable).</summary>
        public string ReadComboValue()
        {
            var combo = FindSolutionsCombo();
            if (combo == null) return "";
            try { var vp = combo.Patterns.Value.PatternOrDefault; if (vp != null) return vp.Value ?? ""; } catch { }
            try { return combo.AsComboBox().SelectedItem?.Text ?? ""; } catch { return ""; }
        }

        /// <summary>
        /// Explicitly select the FIRST item of the Solutions combo (Load auto-selects index 0, but this honours
        /// an explicit "select first solution" step). Returns the selected text, or null if it couldn't.
        /// </summary>
        public string SelectFirstSolution()
        {
            var combo = FindSolutionsCombo();
            if (combo == null) return null;
            try
            {
                var cb = combo.AsComboBox();
                try { cb.Expand(); Thread.Sleep(400); } catch { }
                if (cb.Items != null && cb.Items.Length > 0)
                {
                    try { cb.Items[0].Select(); } catch { try { cb.Select(0); } catch { } }
                }
                try { cb.Collapse(); } catch { }
                Thread.Sleep(300);
                return ReadComboValue();
            }
            catch { return ReadComboValue(); }
        }

        /// <summary>
        /// Drive XrmToolBox's connection-selector dialog raised by "Connect target env…". The dialog is a modal
        /// whose controls are reachable from the main window's tree; it lists connections by name (DEV, TEST, …)
        /// with OK/Cancel at the bottom. Waits for the dialog (an exact "OK" button appears), double-clicks the
        /// entry whose name starts with <paramref name="connectionName"/> (or the first connection when null),
        /// then clicks OK if the dialog is still open. Returns true if it drove a selection.
        /// </summary>
        public bool PickConnectionInSelector(string connectionName, TimeSpan timeout)
        {
            // Opening the modal selector poisons the UIA cache the same way loading solutions did, so rebuild
            // the automation connection before trying to read the dialog's controls.
            Thread.Sleep(1500);
            HardReset();

            // The selector is a SEPARATE top-level window (not under the main window's tree), so search all of
            // the process's top-level windows for the one carrying an "OK" button.
            var dialog = WaitForDialogWindow(timeout);
            if (dialog == null) return false;

            AutomationElement entry = null;
            for (var i = 0; i < 6 && entry == null; i++)
            {
                entry = FindConnectionEntryIn(dialog, connectionName);
                if (entry == null) { Thread.Sleep(600); dialog = FindDialogWindow() ?? dialog; }
            }
            if (entry == null) return false;

            // User's flow: double-click the connection (e.g. TEST) — falls back to select + OK.
            try { entry.DoubleClick(); } catch { try { entry.Click(); } catch { } }
            Thread.Sleep(1200);

            // If the dialog is still open, the entry was only selected — confirm with its OK button.
            var live = FindDialogWindow();
            if (live != null)
            {
                var ok = live.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                    .FirstOrDefault(e => { try { return string.Equals((e.Name ?? "").Trim(), "OK", StringComparison.OrdinalIgnoreCase); } catch { return false; } });
                if (ok != null) { try { ok.AsButton().Invoke(); } catch { try { ok.Click(); } catch { } } Thread.Sleep(800); }
            }
            return true;
        }

        /// <summary>Poll for the selector dialog (a top-level window bearing an "OK" button).</summary>
        private AutomationElement WaitForDialogWindow(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                var d = FindDialogWindow();
                if (d != null) return d;
                RefreshWindow();
                Thread.Sleep(500);
            }
            return null;
        }

        /// <summary>The process's modal-ish top-level window that has an OK button (the connection selector).</summary>
        private AutomationElement FindDialogWindow()
        {
            try
            {
                foreach (var w in _app.GetAllTopLevelWindows(_automation))
                {
                    try
                    {
                        if (w.Equals(Window)) continue;
                        var hasOk = w.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                            .Any(b => { try { return string.Equals((b.Name ?? "").Trim(), "OK", StringComparison.OrdinalIgnoreCase); } catch { return false; } });
                        if (hasOk) return w;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        /// <summary>Find a connection entry within the dialog window whose Name starts with the connection name.</summary>
        private AutomationElement FindConnectionEntryIn(AutomationElement dialog, string connectionName)
        {
            try
            {
                var candidates = dialog.FindAllDescendants().Where(e =>
                {
                    try
                    {
                        return !e.IsOffscreen && e.ControlType != ControlType.Edit && e.ControlType != ControlType.Button
                               && !string.IsNullOrWhiteSpace(e.Name);
                    }
                    catch { return false; }
                }).ToList();

                if (!string.IsNullOrWhiteSpace(connectionName))
                {
                    return candidates.FirstOrDefault(e =>
                    {
                        try { return (e.Name ?? "").Trim().StartsWith(connectionName, StringComparison.OrdinalIgnoreCase); }
                        catch { return false; }
                    });
                }
                return candidates.FirstOrDefault(e =>
                {
                    try { return e.ControlType == ControlType.ListItem || e.ControlType == ControlType.DataItem; }
                    catch { return false; }
                });
            }
            catch { return null; }
        }

        /// <summary>
        /// A connection entry in the selector: an on-screen element whose Name starts with the connection name
        /// (so "DEV" matches the "DEV" tile but not the toolbar's "Target: DEV" label). When no name is given,
        /// returns the first list/data item. Excludes buttons/edits.
        /// </summary>
        private AutomationElement FindConnectionEntry(string connectionName)
        {
            try
            {
                var candidates = Window.FindAllDescendants().Where(e =>
                {
                    try
                    {
                        return !e.IsOffscreen && e.ControlType != ControlType.Edit && e.ControlType != ControlType.Button
                               && !string.IsNullOrWhiteSpace(e.Name);
                    }
                    catch { return false; }
                }).ToList();

                if (!string.IsNullOrWhiteSpace(connectionName))
                {
                    return candidates.FirstOrDefault(e =>
                    {
                        try { return (e.Name ?? "").Trim().StartsWith(connectionName, StringComparison.OrdinalIgnoreCase); }
                        catch { return false; }
                    });
                }

                return candidates.FirstOrDefault(e =>
                {
                    try { return e.ControlType == ControlType.ListItem || e.ControlType == ControlType.DataItem; }
                    catch { return false; }
                });
            }
            catch { return null; }
        }

        /// <summary>First on-screen Button whose Name is EXACTLY <paramref name="label"/> (case-insensitive).</summary>
        private AutomationElement FindButtonExact(string label)
        {
            try
            {
                return Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                    .FirstOrDefault(e => { try { return !e.IsOffscreen && string.Equals((e.Name ?? "").Trim(), label, StringComparison.OrdinalIgnoreCase); } catch { return false; } });
            }
            catch { return null; }
        }

        /// <summary>
        /// Dismiss a leftover connection-selector dialog (Cancel) if one is open, so accumulated state from a
        /// prior run doesn't corrupt the next. No-op if nothing is open.
        /// </summary>
        public void DismissOpenDialog()
        {
            var cancel = FindButtonExact("Cancel");
            if (cancel == null) return;
            try { cancel.AsButton().Invoke(); } catch { try { cancel.Click(); } catch { } }
            Thread.Sleep(600);
        }

        /// <summary>
        /// Poll for a label whose Name starts with <paramref name="prefix"/> and is NOT <paramref name="notValue"/>
        /// — e.g. the DRA "Target: &lt;name&gt;" label once it stops being "Target: (none)". Returns the matched
        /// text, or null on timeout.
        /// </summary>
        public string WaitForLabel(string prefix, string notValue, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    foreach (var e in Window.FindAllDescendants())
                    {
                        try
                        {
                            var name = (e.Name ?? "").Trim();
                            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(name, notValue, StringComparison.OrdinalIgnoreCase))
                                return name;
                        }
                        catch { }
                    }
                }
                catch { }
                Thread.Sleep(600);
            }
            return null;
        }

        /// <summary>True once the DRA results grid has at least one data row (analysis produced findings).</summary>
        public bool WaitForGridRows(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var grid = Window.FindAllDescendants(cf => cf.ByControlType(ControlType.DataGrid)).FirstOrDefault()
                               ?? Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Table)).FirstOrDefault();
                    if (grid != null)
                    {
                        var rows = grid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem));
                        if (rows != null && rows.Length > 0) return true;
                    }
                }
                catch { }
                Thread.Sleep(1000);
            }
            return false;
        }

        /// <summary>Save a PNG of the host window (brings it forward first). Never throws.</summary>
        public void Screenshot(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                try { Window.SetForeground(); } catch { }
                try { Window.Focus(); } catch { }
                Thread.Sleep(700);
                try { Capture.Element(Window).ToFile(path); }
                catch { Capture.Screen().ToFile(path); }
            }
            catch { }
        }

        private AutomationElement FindToolTile(string toolName)
        {
            try
            {
                return Window.FindAllDescendants().FirstOrDefault(e =>
                {
                    try
                    {
                        if (e.ControlType == ControlType.Edit) return false; // exclude the search box's own text
                        return !e.IsOffscreen && string.Equals((e.Name ?? "").Trim(), toolName, StringComparison.OrdinalIgnoreCase);
                    }
                    catch { return false; }
                });
            }
            catch { return null; }
        }

        private AutomationElement FindSearchBox()
        {
            try
            {
                return Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit))
                    .Where(e => { try { return e.IsEnabled && !e.IsOffscreen; } catch { return false; } })
                    .OrderBy(e => { try { return e.BoundingRectangle.Top; } catch { return int.MaxValue; } })
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        private static bool SetSearchText(AutomationElement search, string text)
        {
            if (search == null) return false;
            try { search.Focus(); } catch { }
            try
            {
                var vp = search.Patterns.Value.PatternOrDefault;
                if (vp != null) { vp.SetValue(text); return true; }
            }
            catch { }
            try { search.AsTextBox().Text = text; return true; } catch { return false; }
        }

        private List<string> VisibleNames()
        {
            var result = new List<string>();
            foreach (var e in Window.FindAllDescendants())
            {
                try { if (!string.IsNullOrWhiteSpace(e.Name)) result.Add(e.Name); }
                catch { }
            }
            return result;
        }

        private static Process WaitForXtbProcess(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            Process any = null;
            while (DateTime.UtcNow < deadline)
            {
                var procs = Process.GetProcessesByName("XrmToolBox").Where(p => { try { return !p.HasExited; } catch { return false; } }).ToList();
                var withWindow = procs.FirstOrDefault(p => { try { return p.MainWindowHandle != IntPtr.Zero; } catch { return false; } });
                if (withWindow != null) return withWindow;
                if (procs.Count > 0) any = procs[0];
                Thread.Sleep(500);
            }
            return any;
        }

        public void Dispose()
        {
            // If we ATTACHED to the user's already-running session, leave it alone — never close/kill it.
            if (Attached)
            {
                try { _automation.Dispose(); } catch { }
                return;
            }

            try { if (_app != null && !_app.HasExited) _app.Close(); } catch { }
            try { _app?.Dispose(); } catch { }
            try { _automation.Dispose(); } catch { }
            foreach (var p in Process.GetProcessesByName("XrmToolBox"))
                try { p.Kill(); } catch { }
        }
    }
}
