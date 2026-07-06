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
        /// Force the XrmToolBox window to the foreground so its controls are clickable. Needed between exports:
        /// clicking "Yes" opens each report in an external app that steals foreground, after which a mouse click
        /// on XrmToolBox's toolbar would otherwise land on the wrong window. Minimize→Maximize reliably raises it
        /// (screenshots use PrintWindow, so this aggressive focus juggling doesn't affect captured images).
        /// </summary>
        public void ForceForeground()
        {
            try
            {
                var wp = Window.Patterns.Window.PatternOrDefault;
                if (wp != null)
                {
                    wp.SetWindowVisualState(WindowVisualState.Minimized);
                    Thread.Sleep(250);
                    wp.SetWindowVisualState(WindowVisualState.Maximized);
                    Thread.Sleep(500);
                }
            }
            catch { }
            try { Window.SetForeground(); } catch { }
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
            // Retry with a UIA rebuild between attempts: connecting/metadata-load poisons the cache so the
            // search box / tiles can transiently vanish from the tree right after connect.
            for (var attempt = 0; attempt < 4; attempt++)
            {
                if (attempt > 0) HardReset();
                SelectToolsTab();
                Thread.Sleep(500);
                SetSearchText(FindSearchBox(), toolName);
                Thread.Sleep(1400);

                var tile = FindToolTile(toolName);
                if (tile == null)
                {
                    SelectToolsTab();
                    Thread.Sleep(600);
                    SetSearchText(FindSearchBox(), toolName);
                    Thread.Sleep(1400);
                    tile = FindToolTile(toolName);
                }
                if (tile == null) { Thread.Sleep(800); continue; }

                try { tile.DoubleClick(); return true; }
                catch
                {
                    try { tile.Focus(); FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER); return true; }
                    catch { Thread.Sleep(800); }
                }
            }
            return false;
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

        /// <summary>Dump the Edit/Button/Tree/List/Text elements of a dialog window (by HWND) — diagnostic.</summary>
        public System.Collections.Generic.List<string> DumpHwndElements(IntPtr hwnd)
        {
            var r = new System.Collections.Generic.List<string>();
            try
            {
                if (hwnd == IntPtr.Zero) { r.Add("<no dialog hwnd>"); return r; }
                var el = _automation.FromHandle(hwnd);
                foreach (var ct in new[] { ControlType.Edit, ControlType.Button, ControlType.TreeItem, ControlType.ListItem, ControlType.Text, ControlType.ComboBox })
                {
                    try
                    {
                        foreach (var e in el.FindAllDescendants(cf => cf.ByControlType(ct)))
                            try { var n = (e.Name ?? "").Trim(); if (n.Length > 0) r.Add($"[{ct}] \"{n}\""); } catch { }
                    }
                    catch { }
                }
            }
            catch (Exception ex) { r.Add("<FromHandle threw: " + ex.GetType().Name + ">"); }
            return r;
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
            // The compact selector's controls (connection tiles + OK/Cancel) live in the MAIN window's UIA tree
            // (verified via a diagnostic dump) — NOT as a separate top-level window. So search Window descendants
            // and never touch GetAllTopLevelWindows (it throws COMException on this host). Rebuild the cache first:
            // opening a modal poisons it the same way loading solutions does.
            Thread.Sleep(500);
            HardReset();

            // Wait for the dialog: its OK button appears in the tree.
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline && FindButtonExact("OK") == null) { RefreshWindow(); Thread.Sleep(400); }
            if (FindButtonExact("OK") == null) return false;

            // Find and double-click the connection tile (a Text element named e.g. "XTS-CI-DEV").
            AutomationElement entry = null;
            for (var i = 0; i < 8 && entry == null; i++)
            {
                entry = FindConnectionEntry(connectionName);
                if (entry == null) { RefreshWindow(); Thread.Sleep(400); }
            }
            if (entry == null) return false;

            try { entry.DoubleClick(); } catch { try { entry.Click(); } catch { } }
            Thread.Sleep(700);

            // Double-click may only select; confirm with OK if the dialog is still open.
            var ok = FindButtonExact("OK");
            if (ok != null) { try { ok.AsButton().Invoke(); } catch { try { ok.Click(); } catch { } } Thread.Sleep(500); }
            return true;
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

        /// <summary>Native HWND of the main window (for window-only PrintWindow capture).</summary>
        public IntPtr MainHwnd
        {
            get { try { return Window.Properties.NativeWindowHandle.ValueOrDefault; } catch { return IntPtr.Zero; } }
        }

        /// <summary>
        /// Native HWND of the active dialog owned by XrmToolBox (connection selector, Save As, message box),
        /// or Zero if none — found via Win32 EnumWindows (GetAllTopLevelWindows throws COMException here).
        /// </summary>
        public IntPtr DialogHwnd()
        {
            try { return WindowCapture.FindProcessDialog(_app.ProcessId, MainHwnd); }
            catch { return IntPtr.Zero; }
        }

        /// <summary>Save a PNG of the XrmToolBox main window ONLY (occlusion-proof). Never throws.</summary>
        public void Screenshot(string path) => Screenshot(path, MainHwnd);

        /// <summary>
        /// Save a PNG of the given window (main or a dialog) using PrintWindow — captures that window's own
        /// pixels even when it is behind the IDE, so shots are always XrmToolBox, never the desktop/editor.
        /// Falls back to a foreground screen-region grab only if PrintWindow returns blank.
        /// </summary>
        public void Screenshot(string path, IntPtr hwnd)
        {
            try
            {
                if (WindowCapture.CaptureWindow(hwnd == IntPtr.Zero ? MainHwnd : hwnd, path)) return;

                // Fallback: bring the window forward and grab its on-screen rectangle.
                try { Window.SetForeground(); } catch { }
                try { Window.Focus(); } catch { }
                Thread.Sleep(600);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
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

        // ---------------------------------------------------------------------------------------------------
        // Full end-to-end flow helpers (launch, connect, select solution, export, help)
        // ---------------------------------------------------------------------------------------------------

        /// <summary>Attach if XrmToolBox is already running, otherwise launch it. Returns the main window.</summary>
        public AutomationElement LaunchOrAttach(string exePath, TimeSpan timeout)
        {
            var running = Process.GetProcessesByName("XrmToolBox")
                .Any(p => { try { return !p.HasExited; } catch { return false; } });
            if (running) { Attached = true; return AttachToRunning(timeout); }
            return LaunchAndAttach(exePath, timeout);
        }

        /// <summary>True if the status bar shows a live connection whose text contains <paramref name="hint"/>.</summary>
        public bool IsConnectedTo(string hint)
        {
            try
            {
                return Window.FindAllDescendants().Any(e =>
                {
                    try
                    {
                        var n = e.Name ?? "";
                        return n.IndexOf("Connected to", StringComparison.OrdinalIgnoreCase) >= 0
                               && (string.IsNullOrEmpty(hint) || n.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    catch { return false; }
                });
            }
            catch { return false; }
        }

        /// <summary>
        /// Ensure XrmToolBox is connected to <paramref name="connName"/>. If not, open the main Connect selector,
        /// double-click that connection, and wait for the status bar to report it connected. Rebuilds the UIA
        /// cache afterwards (metadata load poisons it).
        /// </summary>
        public bool EnsureConnected(string connName, TimeSpan timeout)
        {
            // The status bar shows the ORG (e.g. "…(DAS-BITS-DGOE-DEV)"), not the connection name, so we can only
            // check for a live connection generically — the specific env is guaranteed by double-clicking connName.
            if (IsConnectedTo("")) return true;

            // Open the connection selector ONLY if it isn't already up. Use the EXACT "Connect" button — a partial
            // "Connect" match also hits "Open Connection Manager" (its "Connection" contains "Connect"), which
            // would wrongly launch the Connections Manager on top of the already-open dialog.
            if (FindButtonExact("OK") == null)
            {
                ClickByName("Connect");
                Thread.Sleep(800);
            }
            PickConnectionInSelector(connName, TimeSpan.FromSeconds(30));

            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                if (IsConnectedTo("")) { HardReset(); return true; }
                RefreshWindow();
                Thread.Sleep(1000);
            }
            HardReset();
            return IsConnectedTo("");
        }

        /// <summary>
        /// Open the Solutions dropdown and select the first item whose text contains <paramref name="substr"/>.
        /// Retries with a UIA rebuild (the dropdown's items can vanish from the tree after the big load). Returns
        /// the selected text, or the current value if no match was found.
        /// </summary>
        public string SelectSolutionByName(string substr)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0) HardReset();
                var combo = FindSolutionsCombo();
                if (combo == null) { Thread.Sleep(600); continue; }

                // DropDownList type-ahead WITHOUT opening the list: focus the combo and type the name. A closed
                // DropDownList jumps its selection to the item whose text starts with what you type — so this
                // never leaves a dropdown hanging (which was blocking the next steps). The 86-item list is
                // UIA-virtualized so enumerating Items is unreliable; type-ahead sidesteps that entirely.
                try { combo.Focus(); } catch { }
                Thread.Sleep(300);
                try { FlaUI.Core.Input.Keyboard.Type(substr); } catch { }
                Thread.Sleep(800);
                // Escape closes the list if type-ahead happened to open it, without changing the selection.
                try { FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE); } catch { }
                Thread.Sleep(300);

                var v = ReadComboValue();
                if (!string.IsNullOrEmpty(v) && v.IndexOf(substr, StringComparison.OrdinalIgnoreCase) >= 0) return v;
            }
            return ReadComboValue();
        }

        /// <summary>All item texts currently in the Solutions dropdown (diagnostic — helps confirm a name exists).</summary>
        public System.Collections.Generic.List<string> DumpComboItems()
        {
            var result = new System.Collections.Generic.List<string>();
            try
            {
                var combo = FindSolutionsCombo();
                if (combo == null) return result;
                var cb = combo.AsComboBox();
                try { cb.Expand(); Thread.Sleep(700); } catch { }
                foreach (var i in cb.Items ?? Array.Empty<FlaUI.Core.AutomationElements.ComboBoxItem>())
                    try { result.Add(i.Text ?? ""); } catch { }
                try { cb.Collapse(); } catch { }
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Click an item in a popup (a ToolStrip dropdown menu) whose Name contains <paramref name="contains"/>.
        /// Dropdown menus are separate top-level popups, so we search the whole desktop, not just the main window.
        /// </summary>
        public bool ClickPopupItem(string contains)
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                // Look in BOTH the main window tree (ToolStrip dropdowns often surface there) and the desktop
                // (true popup windows). MenuItem first, then any element by name as a fallback.
                foreach (var root in new[] { SafeWindow(), SafeDesktop() })
                {
                    if (root == null) continue;
                    var item = FindByNameContains(root, ControlType.MenuItem, contains)
                               ?? FindByNameContains(root, null, contains);
                    if (item != null)
                    {
                        try { item.AsMenuItem().Invoke(); return true; } catch { }
                        try { item.Click(); return true; } catch { }
                    }
                }
                Thread.Sleep(500);
                if (attempt == 1) RefreshWindow();
            }
            return false;
        }

        private AutomationElement SafeWindow() { try { return Window; } catch { return null; } }
        private AutomationElement SafeDesktop() { try { return _automation.GetDesktop(); } catch { return null; } }

        private static AutomationElement FindByNameContains(AutomationElement root, ControlType? ct, string contains)
        {
            try
            {
                var all = ct.HasValue
                    ? root.FindAllDescendants(cf => cf.ByControlType(ct.Value))
                    : root.FindAllDescendants();
                return all.FirstOrDefault(e => { try { return !e.IsOffscreen && (e.Name ?? "").IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0; } catch { return false; } });
            }
            catch { return null; }
        }

        /// <summary>Wait for a top-level window whose title contains any of <paramref name="titleContains"/>.</summary>
        public AutomationElement WaitForTopLevelWindow(string[] titleContains, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var win = _automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                        .FirstOrDefault(w => { try { return titleContains.Any(t => (w.Name ?? "").IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0); } catch { return false; } });
                    if (win != null) return win;
                }
                catch { }
                Thread.Sleep(400);
            }
            return null;
        }

        /// <summary>In a Save As dialog, set the File name field to a full path and click Save.</summary>
        public bool SaveAsToPath(AutomationElement saveDialog, string fullPath)
        {
            if (saveDialog == null) return false;
            try
            {
                var edit = saveDialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit))
                    .FirstOrDefault(e => { try { return e.IsEnabled && !e.IsOffscreen; } catch { return false; } });
                if (edit != null)
                {
                    try { edit.Focus(); } catch { }
                    var vp = edit.Patterns.Value.PatternOrDefault;
                    if (vp != null) vp.SetValue(fullPath);
                    else { try { edit.AsTextBox().Text = fullPath; } catch { } }
                    Thread.Sleep(400);
                }
                var save = saveDialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                    .FirstOrDefault(e => { try { return string.Equals((e.Name ?? "").Trim(), "Save", StringComparison.OrdinalIgnoreCase); } catch { return false; } });
                if (save == null) return false;
                try { save.AsButton().Invoke(); } catch { try { save.Click(); } catch { return false; } }
                Thread.Sleep(800);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Click a button (e.g. Yes/No/OK) on a top-level message-box dialog. Returns true if clicked.</summary>
        public bool ClickDialogButton(string[] titleContains, string buttonLabel, TimeSpan timeout)
        {
            var dlg = WaitForTopLevelWindow(titleContains, timeout);
            if (dlg == null) return false;
            try
            {
                var btn = dlg.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                    .FirstOrDefault(e => { try { return string.Equals((e.Name ?? "").Trim(), buttonLabel, StringComparison.OrdinalIgnoreCase); } catch { return false; } });
                if (btn == null) return false;
                try { btn.AsButton().Invoke(); } catch { try { btn.Click(); } catch { return false; } }
                Thread.Sleep(600);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Poll until a clickable element whose Name contains <paramref name="contains"/> exists (rebuilding the tree).</summary>
        public bool WaitForClickable(string contains, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                if (FindClickableContaining(contains) != null) return true;
                RefreshWindow();
                Thread.Sleep(1000);
            }
            return false;
        }

        /// <summary>The first clickable (Button/SplitButton/MenuItem/Text) whose Name contains <paramref name="contains"/>.</summary>
        private AutomationElement FindClickableContaining(string contains)
        {
            foreach (var ct in ClickableTypes)
            {
                try
                {
                    var hit = Window.FindAllDescendants(cf => cf.ByControlType(ct))
                        .FirstOrDefault(e => { try { return !e.IsOffscreen && (e.Name ?? "").IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0; } catch { return false; } });
                    if (hit != null) return hit;
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Open the Export dropdown with a real mouse click (Invoke doesn't reliably drop a ToolStrip menu), then
        /// find and invoke the item whose Name contains <paramref name="itemContains"/> in the popup WINDOW
        /// (a separate top-level window found via EnumWindows + FromHandle). Retries with a rebuild.
        /// </summary>
        /// <summary>
        /// Open the Export dropdown with a real mouse click (Invoke doesn't reliably drop a ToolStrip menu).
        /// Retries with ForceForeground + HardReset: after a maximized report is captured, XrmToolBox can be
        /// occluded / its UIA poisoned, so the first find of the "Export" button may fail.
        /// </summary>
        public bool OpenExportMenu()
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                if (attempt > 0) { ForceForeground(); HardReset(); }
                var export = FindClickableContaining("Export");
                if (export != null)
                {
                    try { export.Click(); Thread.Sleep(1000); return true; } catch { }
                    try { export.Patterns.ExpandCollapse.Pattern.Expand(); Thread.Sleep(1000); return true; } catch { }
                }
                Thread.Sleep(700);
            }
            return false;
        }

        /// <summary>
        /// Select an item in the already-open Export dropdown: find + invoke it by name in the popup window,
        /// falling back to keyboard (arrow-down to the 1-based position + Enter) if the popup's UIA is flaky.
        /// </summary>
        public bool SelectExportItem(string itemContains, int oneBasedIndex)
        {
            for (var poll = 0; poll < 5; poll++)
            {
                var item = FindMenuItemInPopup(itemContains);
                if (item != null)
                {
                    try { item.AsMenuItem().Invoke(); return true; } catch { }
                    try { item.Click(); return true; } catch { }
                }
                Thread.Sleep(300);
            }
            try
            {
                for (var i = 0; i < oneBasedIndex; i++)
                {
                    FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.DOWN);
                    Thread.Sleep(200);
                }
                FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ENTER);
                Thread.Sleep(600);
                return true; // Save-dialog appearance is the real confirmation (checked by the caller)
            }
            catch { return false; }
        }

        /// <summary>HWND of the open dropdown/menu popup (small non-main window), or Zero.</summary>
        public IntPtr PopupHwnd() => WindowCapture.FindProcessWindow(_app.ProcessId, MainHwnd, 60, 60);

        /// <summary>HWND of the open Save As dialog (a sizeable non-main window), or Zero.</summary>
        public IntPtr SaveDialogHwnd() => WindowCapture.FindProcessWindow(_app.ProcessId, MainHwnd, 400, 250);

        /// <summary>HWND of the current foreground window — e.g. the opened report in its external app.</summary>
        public IntPtr ForegroundHwnd() => WindowCapture.ForegroundWindow();

        /// <summary>
        /// Foreground window ONLY if it's an external report app (a different process than XrmToolBox) — else
        /// Zero. Used to guard maximize/minimize so we never accidentally minimize the host and break the flow.
        /// </summary>
        public IntPtr ForegroundReportHwnd()
        {
            var fg = WindowCapture.ForegroundWindow();
            if (fg == IntPtr.Zero || fg == MainHwnd) return IntPtr.Zero;
            try { if (WindowCapture.ProcessIdOf(fg) == _app.ProcessId) return IntPtr.Zero; } catch { }
            return fg;
        }

        /// <summary>Press Escape (e.g. to close a stuck dropdown menu before retrying an export).</summary>
        public void PressEscape()
        {
            try { FlaUI.Core.Input.Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE); Thread.Sleep(300); } catch { }
        }

        /// <summary>Maximize a window by HWND (e.g. the opened report) before capturing it.</summary>
        public void MaximizeWindow(IntPtr hwnd) => WindowCapture.Maximize(hwnd);

        /// <summary>Minimize a window by HWND (e.g. the opened report) to clear the foreground.</summary>
        public void MinimizeWindow(IntPtr hwnd) => WindowCapture.Minimize(hwnd);

        /// <summary>Close a window by HWND (e.g. the opened report) to fully clear the foreground.</summary>
        public void CloseWindow(IntPtr hwnd) => WindowCapture.Close(hwnd);

        /// <summary>Send Ctrl+PageDown to the foreground app — moves to the next worksheet in Excel.</summary>
        public void NextExcelSheet()
        {
            try
            {
                FlaUI.Core.Input.Keyboard.TypeSimultaneously(
                    FlaUI.Core.WindowsAPI.VirtualKeyShort.CONTROL,
                    FlaUI.Core.WindowsAPI.VirtualKeyShort.NEXT); // NEXT = Page Down
            }
            catch { }
        }

        /// <summary>Poll until a Save As dialog is up; returns its HWND or Zero on timeout.</summary>
        public IntPtr WaitForSaveDialog(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                var h = SaveDialogHwnd();
                if (h != IntPtr.Zero) return h;
                Thread.Sleep(400);
            }
            return IntPtr.Zero;
        }

        private AutomationElement FindMenuItemInPopup(string contains)
        {
            try
            {
                var h = WindowCapture.FindProcessWindow(_app.ProcessId, MainHwnd, 60, 60);
                if (h == IntPtr.Zero) return null;
                var pop = _automation.FromHandle(h);
                return pop.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                           .FirstOrDefault(e => { try { return (e.Name ?? "").IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0; } catch { return false; } })
                       ?? pop.FindAllDescendants()
                           .FirstOrDefault(e => { try { return (e.Name ?? "").IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0; } catch { return false; } });
            }
            catch { return null; }
        }

        /// <summary>
        /// In an open Save As dialog, click "Downloads" in the Quick-access left pane so the file saves there.
        /// Waits for the dialog to finish rendering first. Returns true if the dialog was found.
        /// </summary>
        public bool SelectDownloadsInSaveDialog(TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            AutomationElement dlg = null;
            while (DateTime.UtcNow < deadline)
            {
                var h = WindowCapture.FindProcessWindow(_app.ProcessId, MainHwnd, 400, 250);
                if (h != IntPtr.Zero) { try { dlg = _automation.FromHandle(h); } catch { } if (dlg != null) break; }
                Thread.Sleep(400);
            }
            if (dlg == null) return false;
            Thread.Sleep(1200); // let the shell dialog finish its "Working on it…" render

            try
            {
                var downloads = dlg.FindAllDescendants()
                    .Where(e => { try { return !e.IsOffscreen && string.Equals((e.Name ?? "").Trim(), "Downloads", StringComparison.OrdinalIgnoreCase)
                                       && (e.ControlType == ControlType.TreeItem || e.ControlType == ControlType.ListItem || e.ControlType == ControlType.Text); } catch { return false; } })
                    .OrderBy(e => { try { return e.BoundingRectangle.Top; } catch { return int.MaxValue; } })
                    .FirstOrDefault();
                if (downloads != null) { try { downloads.Click(); } catch { } Thread.Sleep(1000); }
            }
            catch { }
            return true;
        }

        /// <summary>The bottom-most enabled Edit in the Save As dialog — the "File name" field.</summary>
        private AutomationElement FindSaveFileNameEdit(AutomationElement dlg)
        {
            try
            {
                return dlg.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit))
                    .Where(e => { try { return e.IsEnabled && !e.IsOffscreen; } catch { return false; } })
                    .OrderByDescending(e => { try { return e.BoundingRectangle.Top; } catch { return int.MinValue; } })
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        /// <summary>Current text of the Save As "File name" field (the tool's default file name).</summary>
        public string ReadSaveFileName()
        {
            try
            {
                var h = SaveDialogHwnd();
                if (h == IntPtr.Zero) return "";
                var edit = FindSaveFileNameEdit(_automation.FromHandle(h));
                if (edit == null) return "";
                var vp = edit.Patterns.Value.PatternOrDefault;
                if (vp != null) return vp.Value ?? "";
                return edit.AsTextBox().Text ?? "";
            }
            catch { return ""; }
        }

        /// <summary>
        /// Set the Save As "File name" field to a full path. Typing an absolute path (whose folder already
        /// exists) then clicking Save writes the file there regardless of the folder currently shown.
        /// </summary>
        public bool SetSaveFileName(string fullPath)
        {
            try
            {
                var h = SaveDialogHwnd();
                if (h == IntPtr.Zero) return false;
                var edit = FindSaveFileNameEdit(_automation.FromHandle(h));
                if (edit == null) return false;
                try { edit.Focus(); } catch { }
                var vp = edit.Patterns.Value.PatternOrDefault;
                if (vp != null) vp.SetValue(fullPath);
                else edit.AsTextBox().Text = fullPath;
                Thread.Sleep(500);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Click the Save button in the open Save As dialog. Returns true if clicked.</summary>
        public bool ClickSaveInDialog()
        {
            try
            {
                var h = WindowCapture.FindProcessWindow(_app.ProcessId, MainHwnd, 400, 250);
                if (h == IntPtr.Zero) return false;
                var dlg = _automation.FromHandle(h);
                var save = dlg.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                    .FirstOrDefault(e => { try { return string.Equals((e.Name ?? "").Trim(), "Save", StringComparison.OrdinalIgnoreCase); } catch { return false; } });
                if (save == null) return false;
                try { save.AsButton().Invoke(); } catch { try { save.Click(); } catch { return false; } }
                Thread.Sleep(1000);
                return true;
            }
            catch { return false; }
        }

        /// <summary>Click a button (Yes/No/OK) on the active process dialog (found via EnumWindows + FromHandle).</summary>
        public bool ClickProcessDialogButton(string buttonLabel, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var h = WindowCapture.FindProcessWindow(_app.ProcessId, MainHwnd, 200, 100);
                    if (h != IntPtr.Zero)
                    {
                        var dlg = _automation.FromHandle(h);
                        var btn = dlg.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                            .FirstOrDefault(e => { try { return string.Equals((e.Name ?? "").Trim(), buttonLabel, StringComparison.OrdinalIgnoreCase); } catch { return false; } });
                        if (btn != null)
                        {
                            try { btn.AsButton().Invoke(); } catch { try { btn.Click(); } catch { } }
                            Thread.Sleep(700);
                            return true;
                        }
                    }
                }
                catch { }
                Thread.Sleep(400);
            }
            return false;
        }

        // ---------------------------------------------------------------------------------------------------
        // Additional DRA UI features (analyzer checklist, findings grid + detail, AI summary/options, guards)
        // ---------------------------------------------------------------------------------------------------

        /// <summary>Read the risk summary banner text (e.g. contains "Score" / "HIGH"), or "" if not found.</summary>
        public string ReadRiskSummary()
        {
            try
            {
                foreach (var e in Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Text)))
                {
                    try { var n = e.Name ?? ""; if (n.IndexOf("Score", StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("risk", StringComparison.OrdinalIgnoreCase) >= 0) return n; }
                    catch { }
                }
            }
            catch { }
            return "";
        }

        /// <summary>Toggle an analyzer in the left checklist by its item name (CheckOnClick — a click flips it).</summary>
        public bool ToggleAnalyzer(string name)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0) HardReset();
                try
                {
                    var item = Window.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                        .FirstOrDefault(e => { try { return !e.IsOffscreen && (e.Name ?? "").IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0; } catch { return false; } });
                    if (item != null)
                    {
                        try { var tp = item.Patterns.Toggle.PatternOrDefault; if (tp != null) { tp.Toggle(); return true; } } catch { }
                        try { item.Click(); return true; } catch { }
                    }
                }
                catch { }
                Thread.Sleep(400);
            }
            return false;
        }

        /// <summary>Select the first finding row in the results grid so the detail pane populates. Returns true if a row was selected.</summary>
        public bool SelectFirstFinding()
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (attempt > 0) HardReset();
                try
                {
                    var grid = Window.FindAllDescendants(cf => cf.ByControlType(ControlType.DataGrid)).FirstOrDefault()
                               ?? Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Table)).FirstOrDefault();
                    var row = grid?.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem)).FirstOrDefault(e => { try { return !e.IsOffscreen; } catch { return false; } });
                    if (row != null)
                    {
                        try { row.Patterns.SelectionItem.Pattern.Select(); return true; } catch { }
                        try { row.Click(); return true; } catch { }
                    }
                }
                catch { }
                Thread.Sleep(400);
            }
            return false;
        }

        /// <summary>Read the finding detail pane (the read-only multiline Edit that shows the selected finding's recommendation).</summary>
        public string ReadDetailPane()
        {
            try
            {
                var edit = Window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit))
                    .Where(e => { try { var v = e.Patterns.Value.PatternOrDefault?.Value ?? ""; return v.Length > 20; } catch { return false; } })
                    .OrderByDescending(e => { try { return (e.Patterns.Value.PatternOrDefault?.Value ?? "").Length; } catch { return 0; } })
                    .FirstOrDefault();
                return edit?.Patterns.Value.PatternOrDefault?.Value ?? "";
            }
            catch { return ""; }
        }

        /// <summary>Click the "AI summary" button (produces an offline summary + shows a summary dialog when no API key).</summary>
        public bool ClickAiSummary() => ClickByPartialName("AI summary");

        /// <summary>Open the "AI options" dropdown, then click an item whose name contains <paramref name="itemContains"/>.</summary>
        public bool ClickAiOption(string itemContains)
        {
            if (!ClickByPartialName("AI options")) return false;
            Thread.Sleep(800);
            return ClickPopupItem(itemContains);
        }

        /// <summary>Click the tool's own "Close" button (tears down the tool tab). Do this last.</summary>
        public bool ClickToolClose() => ClickByName("Close");

        /// <summary>Click the tool's right-aligned Help button (opens the Help &amp; Support dialog).</summary>
        public bool ClickHelp() => ClickByName("Help") || ClickByPartialName("Help");

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
