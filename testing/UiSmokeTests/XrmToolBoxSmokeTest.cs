using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
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
            var deadline = DateTime.UtcNow.AddSeconds(45);
            while (DateTime.UtcNow < deadline && found.Count < ExpectedTools.Length)
            {
                var names = VisibleNames(window);
                foreach (var tool in ExpectedTools)
                    if (names.Any(n => n.IndexOf(tool, StringComparison.OrdinalIgnoreCase) >= 0))
                        found.Add(tool);
                if (found.Count < ExpectedTools.Length) Thread.Sleep(1000);
            }

            var missing = ExpectedTools.Where(t => !found.Contains(t)).ToList();
            _output.WriteLine($"Found {found.Count}/{ExpectedTools.Length} suite tools in the XrmToolBox Tools list.");
            Assert.True(missing.Count == 0,
                "Suite tools missing from the XrmToolBox Tools list (plugin failed to load): " + string.Join(", ", missing) +
                ". Verify the DLLs (and any dependencies) are deployed to the Plugins root and all required " +
                "ExportMetadata keys are present.");
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
