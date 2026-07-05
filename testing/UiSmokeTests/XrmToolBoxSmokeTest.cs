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
using Xunit;
using Xunit.Abstractions;

namespace XrmToolSuite.UiSmokeTests
{
    /// <summary>
    /// TIER-3 UI smoke test: launches the real XrmToolBox host and asserts every suite plugin loaded into
    /// the Tools list. This catches the "MEF silently dropped my tool" class of failure (a missing required
    /// metadata key, or an unresolved dependency at scan time) that no headless test can see. It needs a
    /// logged-in Windows desktop + XrmToolBox with the suite DLLs deployed — see README.md. Not run in CI.
    /// </summary>
    public sealed class XrmToolBoxSmokeTest : IDisposable
    {
        // Display names exactly as each plugin exports them via ExportMetadata("Name", ...).
        private static readonly string[] ExpectedTools =
        {
            "Deployment Risk Analyzer",
            "Technical Debt Analyzer",
            "Solution Complexity Score",
            "AI Solution Reviewer",
            "Solution Knowledge Graph",
            "Attribute Auditor",
            "FetchXML Performance Analyzer",
            "Environment Inventory",
            "Privilege Gap Analyzer",
            "View Performance Analyzer",
            "Team Permission Explorer",
            "ERD Generator",
            "JavaScript Performance Analyzer",
            "Form Performance Analyzer",
            "Sharing Analyzer",
            "Audit Compliance Checker",
            "Managed Solution Impact Checker",
            "Portal Health Analyzer",
            "Solution Merge Assistant",
            "Flow Dependency Analyzer",
            "Plugin Dependency Graph",
            "Component Usage Explorer",
            "Environment Comparison Suite",
            "Solution Documentation Generator",
            "Duplicate Metadata Finder",
            "Custom API Explorer",
        };

        private readonly ITestOutputHelper _output;
        private readonly UIA3Automation _automation = new UIA3Automation();
        private Application _app;

        public XrmToolBoxSmokeTest(ITestOutputHelper output) => _output = output;

        [Fact]
        public void AllSuiteTools_AppearInXrmToolBoxToolsList()
        {
            var exe = ResolveXtbExe();

            // XrmToolBox bootstraps and relaunches itself at startup, abandoning the PID we launched, so
            // FlaUI's launched handle goes stale. Launch it, then ATTACH to the live XrmToolBox process by
            // name (preferring one that already has a main window).
            Application.Launch(exe);
            var proc = WaitForXtbProcess(TimeSpan.FromSeconds(60))
                ?? throw new InvalidOperationException("XrmToolBox process did not appear within 60s.");
            _app = Application.Attach(proc);

            var window = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(60))
                ?? throw new InvalidOperationException("XrmToolBox main window did not appear within 60s.");
            Assert.Contains("XrmToolBox", window.Title ?? "", StringComparison.OrdinalIgnoreCase);

            // The Tools list renders shortly after the plugin scan; poll until all names resolve (or time out).
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(45);
                while (DateTime.UtcNow < deadline && found.Count < ExpectedTools.Length)
                {
                    var names = VisibleNames(window);
                    foreach (var tool in ExpectedTools)
                        if (names.Any(n => n.IndexOf(tool, StringComparison.OrdinalIgnoreCase) >= 0))
                            found.Add(tool);
                    if (found.Count < ExpectedTools.Length) Thread.Sleep(1000);
                }
            }
            finally
            {
                // Always capture evidence — pass or fail — into the screenshots folder.
                CaptureScreenshot(window, found.Count);
            }

            // Per-tool evidence: filter the Tools list to EACH suite tool via XrmToolBox's search box and
            // screenshot it in place, so every tool has a "this loads in XrmToolBox" image (the overview shot
            // above is scrolled to the top of the alphabetical list). Guarded — never affects the assertion.
            try
            {
                // Maximize so the shot is the full XrmToolBox window (not the desktop behind it), and give
                // the startup "Checking for update…" splash time to close before we capture.
                try { window.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Maximized); } catch { }
                try { window.SetForeground(); } catch { }
                Thread.Sleep(12000);

                // XrmToolBox restores the online "Tool Library" tab from the prior session; it steals
                // activation between captures. Close it once so only the local "Tools" tab remains.
                CloseToolLibraryTab(window);

                foreach (var tool in ExpectedTools)
                {
                    // Re-assert the local "Tools" tab and re-find its search box every iteration: the box
                    // reference goes stale after the list re-renders, and XrmToolBox can re-activate the
                    // online "Tool Library" tab between captures.
                    SelectToolsTab(window);
                    Thread.Sleep(500);
                    var search = FindSearchBox(window);
                    FilterAndCaptureTool(window, search, tool);
                }
            }
            catch (Exception ex) { _output.WriteLine("Per-tool capture failed (non-fatal): " + ex.Message); }

            var missing = ExpectedTools.Where(t => !found.Contains(t)).ToList();
            _output.WriteLine($"Found {found.Count}/{ExpectedTools.Length} suite tools in the XrmToolBox Tools list.");
            Assert.True(missing.Count == 0,
                "Suite tools missing from the XrmToolBox Tools list (plugin failed to load): " + string.Join(", ", missing) +
                ". Verify the DLLs (and any dependencies) are deployed to the Plugins root and all required " +
                "ExportMetadata keys are present.");
        }

        /// <summary>
        /// Save a PNG of the XrmToolBox window as evidence. Directory: env UISMOKE_SCREENSHOT_DIR if set,
        /// else &lt;temp&gt;\xtb-ui-smoke. Falls back to a full-screen grab if the window capture fails, and
        /// never throws (evidence capture must not fail the test).
        /// </summary>
        private void CaptureScreenshot(AutomationElement window, int foundCount)
        {
            try
            {
                // Bring XrmToolBox to the foreground first — Capture.Element grabs the SCREEN region at the
                // window's rectangle, so an occluded window would otherwise photograph whatever is on top.
                try { window.SetForeground(); } catch { }
                try { window.Focus(); } catch { }
                Thread.Sleep(800);

                var dir = ScreenshotDir();
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var path = Path.Combine(dir, $"xrmtoolbox-tools_{foundCount}of{ExpectedTools.Length}_{stamp}.png");
                try { Capture.Element(window).ToFile(path); }
                catch { Capture.Screen().ToFile(path); } // window capture can fail if minimized/occluded
                _output.WriteLine($"Screenshot saved: {path}");
                Console.WriteLine($"[ui-smoke] Screenshot saved: {path}");
            }
            catch (Exception ex)
            {
                _output.WriteLine("Screenshot capture failed (non-fatal): " + ex.Message);
            }
        }

        /// <summary>
        /// XrmToolBox's Tools tab has a filter/search textbox at the top. Find the topmost enabled Edit
        /// control in the window — that's the plugin filter. Returns null if none is found (capture then
        /// falls back to scroll-into-view).
        /// </summary>
        private static AutomationElement FindSearchBox(AutomationElement window)
        {
            try
            {
                return window.FindAllDescendants(cf => cf.ByControlType(ControlType.Edit))
                    .Where(e => { try { return e.IsEnabled && !e.IsOffscreen; } catch { return false; } })
                    .OrderBy(e => { try { return e.BoundingRectangle.Top; } catch { return int.MaxValue; } })
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        /// <summary>Close the online "Tool Library" tab (activate it, then Ctrl+F4) so only "Tools" remains.</summary>
        private static void CloseToolLibraryTab(AutomationElement window)
        {
            try
            {
                var lib = window.FindAllDescendants()
                    .FirstOrDefault(e => { try { return string.Equals(e.Name, "Tool Library", StringComparison.OrdinalIgnoreCase) && !e.IsOffscreen; } catch { return false; } });
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

        /// <summary>
        /// Select the local "Tools" tab (where deployed plugins appear), not the online Tool Library.
        /// XrmToolBox's tabs are DockPanelSuite tabs, not UIA TabItems, so match a clickable element
        /// literally named "Tools" in the tab strip (topmost such element) and click it.
        /// </summary>
        private static void SelectToolsTab(AutomationElement window)
        {
            try
            {
                var tab = window.FindAllDescendants()
                    .Where(e => { try { return string.Equals(e.Name, "Tools", StringComparison.OrdinalIgnoreCase) && !e.IsOffscreen; } catch { return false; } })
                    .OrderBy(e => { try { return e.BoundingRectangle.Top; } catch { return int.MaxValue; } })
                    .FirstOrDefault();
                if (tab != null)
                {
                    try { tab.Click(); }
                    catch { try { tab.AsTabItem().Select(); } catch { } }
                }
            }
            catch { }
        }

        /// <summary>
        /// Filter the Tools list to a single tool and screenshot it. Sets the filter text via the Value
        /// pattern (which raises the WinForms TextChanged filter) rather than typing — typing an Enter
        /// keystroke into the box switches XrmToolBox to the online Tool Library tab.
        /// </summary>
        private void FilterAndCaptureTool(AutomationElement window, AutomationElement search, string tool)
        {
            SetSearchText(search, tool);
            Thread.Sleep(1400); // let the list re-render / animate

            // Self-correct: if the tool's tile isn't actually on screen (stale search box, or XrmToolBox
            // flipped to the online Tool Library tab), re-assert the Tools tab and re-filter once.
            if (!ToolTileVisible(window, tool))
            {
                SelectToolsTab(window);
                Thread.Sleep(600);
                SetSearchText(FindSearchBox(window), tool);
                Thread.Sleep(1400);
            }

            try { window.SetForeground(); } catch { }
            Thread.Sleep(500);

            var path = Path.Combine(ScreenshotDir(), $"tool_{Slug(tool)}_{DateTime.Now:yyyyMMdd-HHmmss}.png");
            try { Capture.Element(window).ToFile(path); }
            catch { Capture.Screen().ToFile(path); }
            _output.WriteLine($"Per-tool screenshot ({tool}): {path}");
            Console.WriteLine($"[ui-smoke] Per-tool screenshot: {path}");
        }

        /// <summary>
        /// True if a visible (on-screen) tool tile with exactly this name is present — i.e. we're on the
        /// filtered local Tools tab, not the online Tool Library (whose catalog doesn't contain our tools).
        /// Excludes Edit controls so the search box's own text doesn't count as a match.
        /// </summary>
        private static bool ToolTileVisible(AutomationElement window, string tool)
        {
            try
            {
                return window.FindAllDescendants().Any(e =>
                {
                    try
                    {
                        if (e.ControlType == ControlType.Edit) return false;
                        return !e.IsOffscreen && string.Equals((e.Name ?? "").Trim(), tool, StringComparison.OrdinalIgnoreCase);
                    }
                    catch { return false; }
                });
            }
            catch { return false; }
        }

        /// <summary>Sets the search/filter text via the UIA Value pattern (no Enter keystroke). Returns success.</summary>
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

        private static void ClearSearch(AutomationElement search) => SetSearchText(search, "");

        private static string Slug(string s) =>
            new string((s ?? "").Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray())
                .Trim().Replace(' ', '-').ToLowerInvariant();

        private static string ScreenshotDir()
        {
            var dir = Environment.GetEnvironmentVariable("UISMOKE_SCREENSHOT_DIR");
            if (string.IsNullOrWhiteSpace(dir)) dir = Path.Combine(Path.GetTempPath(), "xtb-ui-smoke");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Wait for a live XrmToolBox process, preferring one that already has a main window.</summary>
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
            return any; // may be a windowless process; GetMainWindow will then wait for its handle
        }

        /// <summary>Every non-empty AutomationElement name currently in the window (name reads can throw; guarded).</summary>
        private static List<string> VisibleNames(AutomationElement window)
        {
            var result = new List<string>();
            foreach (var e in window.FindAllDescendants())
            {
                try
                {
                    var name = e.Name;
                    if (!string.IsNullOrWhiteSpace(name)) result.Add(name);
                }
                catch { /* some elements throw on Name; ignore */ }
            }
            return result;
        }

        /// <summary>XTB_EXE env var, then common install locations; throws with guidance if not found.</summary>
        private static string ResolveXtbExe()
        {
            var fromEnv = Environment.GetEnvironmentVariable("XTB_EXE");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv)) return fromEnv;

            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new[]
            {
                Path.Combine(local, "Programs", "XrmToolBox", "XrmToolBox.exe"),
                Path.Combine(local, "XrmToolBox", "XrmToolBox.exe"),
                @"C:\Program Files\XrmToolBox\XrmToolBox.exe",
                @"C:\Tools\XrmToolBox\XrmToolBox.exe",
            };
            var hit = candidates.FirstOrDefault(File.Exists);
            if (hit != null) return hit;

            throw new FileNotFoundException(
                "Could not find XrmToolBox.exe. Set the XTB_EXE environment variable to its full path " +
                "(e.g. $env:XTB_EXE = 'C:\\path\\to\\XrmToolBox.exe') and ensure the suite DLLs are deployed " +
                "to %AppData%\\MscrmTools\\XrmToolBox\\Plugins. See testing/UiSmokeTests/README.md.");
        }

        public void Dispose()
        {
            try { if (_app != null && !_app.HasExited) _app.Close(); } catch { /* best effort */ }
            try { _app?.Dispose(); } catch { }
            try { _automation.Dispose(); } catch { }
            // We launched XrmToolBox, so make sure no window is left behind (it may have relaunched).
            foreach (var p in Process.GetProcessesByName("XrmToolBox"))
                try { p.Kill(); } catch { }
        }
    }
}
