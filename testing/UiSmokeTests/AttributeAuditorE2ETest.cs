using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using XrmToolSuite.UiSmokeTests.Pages;
using Xunit;
using Xunit.Abstractions;

namespace XrmToolSuite.UiSmokeTests
{
    /// <summary>
    /// TIER-3c — full END-TO-END walkthrough of the Attribute Auditor (opt-in, LOCAL only), following the exact
    /// operator script: launch XrmToolBox if needed, connect DEV, open the tool, "Run audit" over the whole
    /// environment (no solution selection, no target env), flip the "Candidates only" filter, export EACH format
    /// (CSV, then HTML report) to the screenshots folder, then open Help and close the tab. This tool needs a
    /// live connection but NO solution and NO target env. A screenshot of the XrmToolBox window ONLY is captured
    /// after every step (via PrintWindow, so the IDE/desktop never appear), under
    /// screenshots/&lt;yyyyMMdd&gt;/attribute-auditor/NN-step.png.
    ///
    /// Unlike the analyzer tools, Attribute Auditor's results live in a WinForms ListView (not a DataGrid) with
    /// no finding detail pane, and its exports are two direct toolbar buttons that each open a Save As dialog and
    /// write the file with no "Open it now?" prompt — so the export helper below drives the button directly.
    ///
    /// Constraints (see README Tier-3b): interactive connections can't authenticate unattended, so DEV must have
    /// a warm token; desktop must stay UNLOCKED. Gated behind XTB_E2E=1.
    ///
    /// Env vars: XTB_E2E=1 to run; XTB_EXE; XTB_SOURCE (default "XTS-CI-DEV"); UISMOKE_SCREENSHOT_DIR.
    /// </summary>
    public sealed class AttributeAuditorE2ETest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly XtbHost _host = new XtbHost();
        private readonly string _dateStamp = DateTime.Now.ToString("yyyyMMdd");
        private readonly List<string> _failures = new List<string>();
        private int _shot;
        private string _round = "TR-001";   // test-round tag, prefixed on every screenshot + exported file

        private const string Tool = "Attribute Auditor";

        // Export toolbar buttons (partial caption -> saved file extension), in Designer order. Each button opens
        // a Save As dialog directly (no export dropdown, no "Open it now?" prompt).
        private static readonly (string Button, string Ext)[] Exports =
        {
            ("Export CSV", "csv"),
            ("Export report (HTML)", "html"),
        };

        public AttributeAuditorE2ETest(ITestOutputHelper output) => _output = output;

        [Fact]
        public void FullOperatorWalkthrough()
        {
            if (!string.Equals(Environment.GetEnvironmentVariable("XTB_E2E"), "1", StringComparison.Ordinal))
            {
                _output.WriteLine("Skipped: set XTB_E2E=1 (desktop unlocked, DEV token warm) to run. See README Tier-3b.");
                return;
            }

            InitRound();
            _output.WriteLine($"Test round: {_round}");
            var exe = ResolveXtbExe();
            var source = EnvOr("XTB_SOURCE", "XTS-CI-DEV");
            // Exports are saved alongside the screenshots, in the same per-date tool folder:
            // …\UiSmokeTests\screenshots\<yyyymmdd>\attribute-auditor\  (created if missing).
            var exportDir = Path.GetFullPath(RunDir());
            Directory.CreateDirectory(exportDir);
            AssertNotProd(source, "source");

            // 1) Launch XrmToolBox if not open
            _host.LaunchOrAttach(exe, TimeSpan.FromSeconds(60));
            _host.Maximize();
            Thread.Sleep(8000); // let the fresh host settle (plugin scan / update check) before driving UIA
            _host.CloseToolLibraryTab();
            _host.DismissOpenDialog();
            _host.HardReset();
            Shot("01-launched");

            // 2) Connect to DEV (select DEV -> Connect if not connected). This tool needs NO solution/target env.
            var connected = _host.EnsureConnected(source, TimeSpan.FromSeconds(90));
            Shot("02-connect-dev");
            Check(connected, $"Could not connect to '{source}'. Is its token warm (connect once by hand)?");
            Thread.Sleep(4000);   // let the post-connect metadata load finish settling
            _host.HardReset();    // before touching the Tools list (connect poisons the UIA cache)

            // 3) Find the tool and double-click to open. Verify it REALLY opened by finding its primary "Run audit"
            //    toolbar button (a tool-only control) rather than a tab caption — the Tools-list tile carries a
            //    "(NN)" rating badge that can false-match a "(<conn>)" tab check. Retry the open if needed.
            var toolOpen = false;
            for (var i = 0; i < 3 && !toolOpen; i++)
            {
                if (i > 0) _host.HardReset();
                if (!_host.WaitForClickable("Run audit", TimeSpan.FromSeconds(2)))
                    _host.OpenTool(Tool);
                toolOpen = _host.WaitForClickable("Run audit", TimeSpan.FromSeconds(25));
            }
            Shot("03-tool-open");
            Check(toolOpen, $"'{Tool}' did not open (no 'Run audit' toolbar after retries).");

            // 4) Click Run audit — scans the WHOLE environment's custom columns (no solution needed). Results land
            //    in a WinForms ListView (surfaces as a UIA List, not a DataGrid) — WaitForGridRows may not see the
            //    rows, so treat it as informational; the export buttons enabling is the real "produced data" signal.
            Check(_host.ClickByPartialName("Run audit"), "Could not find 'Run audit'.");
            Thread.Sleep(6000);   // let the async audit run
            _host.HardReset();    // it mutates the tree heavily
            var hasRows = _host.WaitForGridRows(TimeSpan.FromSeconds(120));
            Shot("04-audit-complete");
            _output.WriteLine(hasRows ? "Audit produced column rows." : "Audit rows not detected via UIA (ListView; still exportable).");
            _host.HardReset();

            // 5) Flip the "Candidates only" filter toolbar toggle (shows only unused retirement candidates), then
            //    restore it. It's a CheckOnClick ToolStripButton, so a click toggles it.
            var toggled = _host.ClickByPartialName("Candidates only");
            Thread.Sleep(800);
            Shot("05-candidates-filter");
            Check(toggled, "Could not toggle the 'Candidates only' filter.");
            _host.ClickByPartialName("Candidates only");   // restore
            Thread.Sleep(600);
            _host.HardReset();

            // 6) Export each format -> click the toolbar button -> Save dialog shot -> save to the screenshots
            //    folder -> verify the file landed. Retry once per format: the click can transiently miss on this
            //    flaky host. (No dropdown menu and no "Open it now?" prompt — each button writes directly.)
            foreach (var (button, ext) in Exports)
            {
                var ok = false;
                for (var r = 0; r < 2 && !ok; r++)
                {
                    if (r > 0) { _host.PressEscape(); _host.HardReset(); }
                    ok = ExportOne(button, ext, exportDir);
                }
                Check(ok, $"Export '{button}' (.{ext}) to '{exportDir}' did not complete.");
                _host.HardReset(); // the Save dialog churns the tree
            }

            // 7) Click Help. The button opens a MODAL Help & Support dialog; the UIA Invoke can report false
            // because the modal blocks the UI thread mid-handshake — so verify by the dialog APPEARING (which we
            // screenshot), not by the click's return value. Then close it so it can't block a re-run.
            _host.ForceForeground();
            _host.HardReset();
            _host.ClickHelp();
            Thread.Sleep(1800);
            var helpHwnd = _host.DialogHwnd();
            ShotHwnd("07-help", helpHwnd);
            Check(helpHwnd != IntPtr.Zero, "Help & Support dialog did not open.");
            _host.ClickProcessDialogButton("Close", TimeSpan.FromSeconds(5));

            // 8) Close the tool via the host's own tab-close (Ctrl+F4) — the per-tool "Close" button was removed
            //    from the suite, so tear the tab down through XrmToolBox itself.
            _host.ForceForeground();
            _host.HardReset();
            var closed = _host.CloseActiveToolTab();
            Thread.Sleep(1500);
            Shot("08-tool-closed");
            Check(closed, "Could not close the tool tab (Ctrl+F4).");

            _output.WriteLine($"Screenshots: {RunDir()}");
            Assert.True(_failures.Count == 0, "Steps failed:\n - " + string.Join("\n - ", _failures));
        }

        /// <summary>
        /// Click an export toolbar button, drive the Save As dialog to the screenshots folder + Save, and verify a
        /// new <c>&lt;round&gt;-*.ext</c> file landed there (the tool supplies the default name). This tool's
        /// exports write the file directly with no "Open it now?" prompt. Returns true on success.
        /// </summary>
        private bool ExportOne(string buttonText, string ext, string saveDir)
        {
            var before = ExportCount(saveDir, ext);
            _host.ForceForeground();
            _host.HardReset();

            // (a) click the export toolbar button
            if (!_host.ClickByPartialName(buttonText)) return false;

            // (b) wait for the Save As dialog, then set the File name to the FULL path inside the screenshots
            //     folder (typing an absolute path routes the save there) and screenshot the SAVE DIALOG.
            var saveHwnd = _host.WaitForSaveDialog(TimeSpan.FromSeconds(20));
            if (saveHwnd == IntPtr.Zero) return false;
            Thread.Sleep(1500);   // let the shell dialog finish rendering
            var defaultName = _host.ReadSaveFileName();
            if (string.IsNullOrWhiteSpace(defaultName)) defaultName = $"attribute-audit.{ext}";
            _host.SetSaveFileName(Path.Combine(saveDir, $"{_round}-{Path.GetFileName(new string(defaultName.Where(ch => !Path.GetInvalidPathChars().Contains(ch)).ToArray()))}"));
            ShotHwnd($"06-export-{ext}-1-savedialog", _host.SaveDialogHwnd());

            // (c) Save — writes the file directly (no open prompt). Screenshot the host (status message) after.
            if (!_host.ClickSaveInDialog()) return false;
            Thread.Sleep(2000);
            _host.ForceForeground();
            _host.HardReset();
            ShotHwnd($"06-export-{ext}-2-saved", _host.MainHwnd);

            return ExportCount(saveDir, ext) > before;
        }

        private int ExportCount(string dir, string ext)
        {
            try { return Directory.GetFiles(dir, $"{_round}-*.{ext}").Length; }
            catch { return 0; }
        }

        // --- screenshots (via PrintWindow; window-only for tool steps, the specific dialog otherwise) ---

        /// <summary>File name: TR-XXX-NN-label-HHmmss.png (round tag + sequence + time so runs never collide).</summary>
        private string ShotPath(string label) =>
            Path.Combine(RunDir(), $"{_round}-{_shot++:00}-{label}-{DateTime.Now:HHmmss}.png");

        /// <summary>
        /// Compute this run's test-round tag (TR-001, TR-002, …) as one past the highest TR-NNN already present
        /// in the run folder, so every run's files carry a distinct round prefix.
        /// </summary>
        private void InitRound()
        {
            try
            {
                var dir = RunDir();
                Directory.CreateDirectory(dir);
                var max = 0;
                foreach (var f in Directory.GetFiles(dir))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(f), @"^TR-(\d+)-");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out var n)) max = Math.Max(max, n);
                }
                _round = $"TR-{max + 1:000}";
            }
            catch { _round = "TR-001"; }
        }

        private void Shot(string label) => _host.Screenshot(ShotPath(label));

        /// <summary>Capture a specific window (dialog) by HWND; falls back to the main window if Zero.</summary>
        private void ShotHwnd(string label, IntPtr hwnd) => _host.Screenshot(ShotPath(label), hwnd);

        // One folder per DATE (yyyymmdd); multiple runs the same day share it (files are time-stamped).
        private string RunDir() =>
            Path.Combine(ScreenshotRoot(), _dateStamp, "attribute-auditor");

        private static string ScreenshotRoot()
        {
            var dir = Environment.GetEnvironmentVariable("UISMOKE_SCREENSHOT_DIR");
            if (string.IsNullOrWhiteSpace(dir)) dir = Path.Combine(Path.GetTempPath(), "xtb-ui-smoke");
            return dir;
        }

        private void Check(bool ok, string message)
        {
            if (!ok) { _failures.Add(message); _output.WriteLine("FAIL: " + message); }
        }

        private static string EnvOr(string name, string fallback)
        {
            var v = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }

        private static void AssertNotProd(string connection, string which)
        {
            Assert.False((connection ?? "").ToLowerInvariant().Contains("prod"),
                $"Refusing to run against '{connection}' for {which} — it looks like production.");
        }

        private static string ResolveXtbExe()
        {
            var fromEnv = Environment.GetEnvironmentVariable("XTB_EXE");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv)) return fromEnv;
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var candidates = new[]
            {
                @"C:\devtools\XrmToolbox\XrmToolBox.exe",
                Path.Combine(local, "Programs", "XrmToolBox", "XrmToolBox.exe"),
                @"C:\Program Files\XrmToolBox\XrmToolBox.exe",
            };
            var hit = candidates.FirstOrDefault(File.Exists);
            if (hit != null) return hit;
            throw new FileNotFoundException("Could not find XrmToolBox.exe. Set XTB_EXE. See README.");
        }

        public void Dispose() => _host.Dispose();
    }
}
