using System;
using System.IO;
using System.Linq;
using System.Threading;
using XrmToolSuite.UiSmokeTests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace XrmToolSuite.UiSmokeTests
{
    /// <summary>
    /// TIER-3c — full END-TO-END walkthrough of the Deployment Risk Analyzer (opt-in, LOCAL only).
    /// Drives the real tool through its whole happy path, screenshotting after every step:
    ///
    ///   1. open the tool (connected)                -> 01-tool-opened
    ///   2. Load solutions                            -> 02-solutions-loaded
    ///   3. Connect target env… -> pick a connection  -> 03-target-connected
    ///   4. select the first solution                 -> 04-first-solution-selected
    ///   5. Analyze                                   -> 05-analysis-complete
    ///
    /// All shots land under  &lt;screenshot-dir&gt;/&lt;yyyyMMdd-HHmmss&gt;/&lt;tool-slug&gt;/NN-step.png
    /// (one timestamped run folder, one sub-folder per tool), exactly like the load smoke test.
    ///
    /// Same constraints as the connected walkthrough (see ConnectedWalkthroughTest / README Tier-3b):
    ///   * ATTACH mode only — the suite's connections are interactive, so start XrmToolBox and connect the
    ///     SOURCE org by hand first. The test attaches to that live session and never closes it.
    ///   * The desktop must stay UNLOCKED — a lock screen is invisible to UI Automation.
    ///   * Gated behind XTB_E2E=1 so it never runs by default / in CI.
    ///
    /// Env vars:
    ///   XTB_E2E                "1" to run (else the test is skipped / no-op)
    ///   XTB_TEST_CONNECTION    SOURCE connection expected connected (default "XTS-CI-TEST") — never prod
    ///   XTB_TARGET_CONNECTION  connection to pick in the "Connect target env…" dialog (default: XTS-CI-DEV;
    ///                          set to "" to just pick the first entry offered)
    ///   XTB_WALKTHROUGH_TOOL   tool display name (default "Deployment Risk Analyzer")
    ///   UISMOKE_SCREENSHOT_DIR screenshot root (default %TEMP%\xtb-ui-smoke)
    /// </summary>
    public sealed class DeploymentRiskAnalyzerE2ETest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly XtbHost _host = new XtbHost();

        // One timestamped run folder for this execution; each tool gets its own sub-folder of NN-indexed shots.
        private readonly string _runStamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        private int _shot;

        public DeploymentRiskAnalyzerE2ETest(ITestOutputHelper output) => _output = output;

        [Fact]
        public void FullWalkthrough_LoadTargetSelectAnalyze()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("XTB_E2E"), "1", StringComparison.Ordinal))
            {
                _output.WriteLine("Skipped: set XTB_E2E=1 (attach mode; XrmToolBox running + connected; desktop " +
                                  "unlocked) to run the Deployment Risk Analyzer end-to-end walkthrough. See README Tier-3b.");
                return;
            }

            var tool = EnvOr("XTB_WALKTHROUGH_TOOL", "Deployment Risk Analyzer");
            var source = EnvOr("XTB_TEST_CONNECTION", "XTS-CI-TEST");
            var target = Environment.GetEnvironmentVariable("XTB_TARGET_CONNECTION");
            // Default matches the connection's SHORT display name as shown in XrmToolBox's selector dialog
            // (e.g. "DEV"/"TEST"), not the ConnectionsV2 name ("XTS-CI-DEV"). null => default; "" => pick first.
            if (target == null) target = "DEV";

            AssertNotProd(source, "XTB_TEST_CONNECTION (source)");
            if (!string.IsNullOrWhiteSpace(target)) AssertNotProd(target, "XTB_TARGET_CONNECTION");

            // --- attach to the live, hand-connected session ---
            _host.AttachToRunning(TimeSpan.FromSeconds(30));
            _host.Maximize();
            Thread.Sleep(1500);
            _host.CloseToolLibraryTab();
            // Clear any leftover connection-selector dialog from a prior run — while it's modal, the tool's own
            // toolbar buttons read as unavailable, which is what broke an earlier run's "Connect target env…" click.
            _host.DismissOpenDialog();

            // --- step 1: open the tool (or reuse an already-open connected tab) ---
            var caption = _host.WaitForConnectedToolTab(tool, TimeSpan.FromSeconds(3));
            if (caption == null)
            {
                Assert.True(_host.OpenTool(tool),
                    $"Could not open '{tool}'. Is it deployed (-p:DeployToXTB=true) and the desktop unlocked?");
                caption = _host.WaitForConnectedToolTab(tool, TimeSpan.FromSeconds(45));
            }
            Shot(tool, "tool-opened");
            Assert.False(caption == null,
                $"'{tool}' opened but never showed a connection suffix — it opened disconnected. Connect the " +
                "SOURCE org by hand before running (attach mode). See README Tier-3b.");
            _output.WriteLine($"Step 1 — tool open & connected. Tab: \"{caption}\".");

            // --- step 2: Load solutions ---
            Assert.True(_host.ClickByPartialName("Load solutions"), "Could not find the 'Load solutions' button.");
            var selected = _host.WaitForComboPopulated(TimeSpan.FromSeconds(60));
            Shot(tool, "solutions-loaded");
            Assert.False(string.IsNullOrWhiteSpace(selected),
                "The Solutions dropdown never populated after 'Load solutions'. Does the connected org have " +
                "visible unmanaged/managed solutions, and did the connection stay alive?");
            _output.WriteLine($"Step 2 — solutions loaded. First entry: \"{selected}\".");

            // --- step 3: Connect target env… -> pick a connection ---
            Thread.Sleep(1000);  // let the toolbar settle after the load-solutions WorkAsync
            _host.HardReset();   // loading dozens of solutions poisons the UIA cache (COMException on every
                                 // query); rebuild the automation connection before touching the toolbar again.
            Shot(tool, "before-target-click");
            var clickedTarget = _host.ClickByPartialName("Connect target env")
                                || _host.ClickByPartialName("Connect target");
            if (!clickedTarget)
            {
                _output.WriteLine("DIAG top-level windows: " + string.Join(" | ", _host.DumpTopLevelWindows()));
                _output.WriteLine("DIAG clickable names: " + string.Join(" | ", _host.DumpClickableNames()));
            }
            Assert.True(clickedTarget,
                "Could not find the 'Connect target env…' button (see DIAG + before-target-click screenshot).");
            var picked = _host.PickConnectionInSelector(
                string.IsNullOrWhiteSpace(target) ? null : target, TimeSpan.FromSeconds(30));
            Shot(tool, "target-connected");
            if (!picked)
            {
                _output.WriteLine("Step 3 — WARNING: the connection selector wasn't driven automatically " +
                                  "(dialog layout differs). Screenshot 03 shows its state for tuning PickConnectionInSelector.");
            }
            // The tool sets "Target: <name>" (green) once UpdateConnection(TargetOrganization) arrives.
            var targetLabel = _host.WaitForLabel("Target:", "Target: (none)", TimeSpan.FromSeconds(30));
            Shot(tool, "target-label");
            Assert.False(targetLabel == null,
                "The 'Target:' label never left '(none)', so no target environment was connected. If the " +
                "connection selector dialog didn't drive, tune XtbHost.PickConnectionInSelector against shot 03.");
            _output.WriteLine($"Step 3 — target connected. Label: \"{targetLabel}\".");

            // --- step 4: explicitly select the first solution ---
            var first = _host.SelectFirstSolution();
            Shot(tool, "first-solution-selected");
            _output.WriteLine($"Step 4 — first solution selected: \"{first}\".");

            // --- step 5: Analyze ---
            Assert.True(_host.ClickByPartialName("Analyze"), "Could not find the 'Analyze' button.");
            var hasRows = _host.WaitForGridRows(TimeSpan.FromSeconds(120));
            Shot(tool, "analysis-complete");
            _output.WriteLine(hasRows
                ? "Step 5 — analysis complete; results grid populated."
                : "Step 5 — analysis finished but no grid rows detected (a clean solution can legitimately " +
                  "produce zero findings). Screenshot 05 shows the final state.");

            _output.WriteLine($"E2E walkthrough complete. Screenshots: {RunToolDir(tool)}");
        }

        // --- helpers ---

        private void Shot(string tool, string step)
        {
            var name = $"{_shot++:00}-{step}.png";
            _host.Screenshot(Path.Combine(RunToolDir(tool), name));
        }

        private string RunToolDir(string tool) => Path.Combine(ScreenshotRoot(), _runStamp, Slug(tool));

        private static string ScreenshotRoot()
        {
            var dir = Environment.GetEnvironmentVariable("UISMOKE_SCREENSHOT_DIR");
            if (string.IsNullOrWhiteSpace(dir)) dir = Path.Combine(Path.GetTempPath(), "xtb-ui-smoke");
            return dir;
        }

        private static string Slug(string s) =>
            new string((s ?? "").ToLowerInvariant().Replace(' ', '-').Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        private static string EnvOr(string name, string fallback)
        {
            var v = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }

        private static void AssertNotProd(string connection, string which)
        {
            Assert.False((connection ?? "").ToLowerInvariant().Contains("prod"),
                $"Refusing to run against '{connection}' for {which} — it looks like production. Use a dev/test org.");
        }

        public void Dispose() => _host.Dispose();
    }
}
