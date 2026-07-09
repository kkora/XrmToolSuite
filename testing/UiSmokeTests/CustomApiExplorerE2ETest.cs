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
    /// TIER-3c — full END-TO-END walkthrough of the Custom API Explorer tool (opt-in, LOCAL only), following the
    /// operator script: launch XrmToolBox if needed, connect DEV, open the tool, Load Custom APIs, select the
    /// first discovered API, read its detail/parameters, confirm the gated Invoke console is present but LEFT
    /// UNTRIGGERED, export EACH catalog format alongside the screenshots, then open Help and close the tab.
    ///
    /// SAFETY: this tool can INVOKE Custom APIs against the connected org (a live write/execute path behind a
    /// confirmation dialog). This walkthrough exercises ONLY read-only discovery (load/list/select/detail/export)
    /// and NEVER clicks "Invoke…" — it only screenshots that the gated Invoke button exists. No API is executed.
    ///
    /// Like the Solution Complexity Score tool this needs NO target environment. A screenshot of the XrmToolBox
    /// window ONLY is captured after every step (via PrintWindow, so the IDE/desktop never appear), under
    /// screenshots/&lt;yyyyMMdd&gt;/custom-api-explorer/NN-step.png.
    ///
    /// Constraints (see README Tier-3b): interactive connections can't authenticate unattended, so DEV must have
    /// a warm token; desktop must stay UNLOCKED. Gated behind XTB_E2E=1.
    ///
    /// Env vars: XTB_E2E=1 to run; XTB_EXE; XTB_SOURCE (default "XTS-CI-DEV"); UISMOKE_SCREENSHOT_DIR.
    /// </summary>
    public sealed class CustomApiExplorerE2ETest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly XtbHost _host = new XtbHost();
        private readonly string _dateStamp = DateTime.Now.ToString("yyyyMMdd");
        private readonly List<string> _failures = new List<string>();
        private int _shot;
        private string _round = "TR-001";   // test-round tag, prefixed on every screenshot + exported file

        private const string Tool = "Custom API Explorer";

        // Export menu items (popup text fragment, 1-based position in the dropdown) -> saved file extension.
        // Order matches the Designer's tsddExport.DropDownItems: HTML, Markdown, CSV.
        private static readonly (string Menu, int Index, string Ext)[] Exports =
        {
            ("HTML", 1, "html"),
            ("Markdown", 2, "md"),
            ("CSV", 3, "csv"),
        };

        public CustomApiExplorerE2ETest(ITestOutputHelper output) => _output = output;

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
            // …\UiSmokeTests\screenshots\<yyyymmdd>\custom-api-explorer\  (created if missing).
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

            // 2) Connect to DEV (select DEV -> Connect if not connected). This tool needs NO target env.
            var connected = _host.EnsureConnected(source, TimeSpan.FromSeconds(90));
            Shot("02-connect-dev");
            Check(connected, $"Could not connect to '{source}'. Is its token warm (connect once by hand)?");
            Thread.Sleep(4000);   // let the post-connect metadata load finish settling
            _host.HardReset();    // before touching the Tools list (connect poisons the UIA cache)

            // 3) Find the tool and double-click to open. Verify it REALLY opened by finding its "Load Custom APIs"
            //    toolbar button (a tool-only control) rather than a tab caption. Retry the open if needed.
            var toolOpen = false;
            for (var i = 0; i < 3 && !toolOpen; i++)
            {
                if (i > 0) _host.HardReset();
                if (!_host.WaitForClickable("Load Custom APIs", TimeSpan.FromSeconds(2)))
                    _host.OpenTool(Tool);
                toolOpen = _host.WaitForClickable("Load Custom APIs", TimeSpan.FromSeconds(25));
            }
            Shot("03-tool-open");
            Check(toolOpen, $"'{Tool}' did not open (no 'Load Custom APIs' toolbar after retries).");

            // 4) Click Load Custom APIs — retry: the click or the async load can transiently no-op on this flaky
            //    host. Confirm success by the discovery grid gaining data rows.
            var hasRows = false;
            for (var i = 0; i < 3 && !hasRows; i++)
            {
                if (i > 0) { _host.HardReset(); Thread.Sleep(2000); }
                _host.ClickByPartialName("Load Custom APIs");
                hasRows = _host.WaitForGridRows(TimeSpan.FromSeconds(90));
            }
            _host.HardReset(); // loading the catalog poisons the UIA cache
            Shot("04-apis-loaded");
            Check(hasRows, "Custom API discovery grid never populated (after retries).");

            // 5) Select the first discovered Custom API row so the detail + parameter panes fill (read-only).
            var selected = _host.SelectFirstFinding();
            Thread.Sleep(1000);
            Shot("05-api-selected");
            Check(selected, "Could not select the first Custom API row.");

            // 6) Detail pane — selecting an API renders its unique name, kind, binding, request parameters,
            //    response properties and a sample-call snippet into the read-only detail textbox.
            var detail = _host.ReadDetailPane();
            Shot("06-api-detail");
            Check(!string.IsNullOrWhiteSpace(detail), "Selecting an API did not populate the detail pane.");
            _output.WriteLine("Step 6 - detail pane populated (read-only discovery).");
            _host.HardReset();

            // 7) Gated Invoke console — SAFETY: this tool can execute Custom APIs against the org behind a
            //    confirmation dialog. We ONLY screenshot that the gated "Invoke…" button is present and DO NOT
            //    click it — no API is ever executed by this walkthrough.
            var invokePresent = _host.WaitForClickable("Invoke", TimeSpan.FromSeconds(5));
            Shot("07-invoke-console-gated");
            Check(invokePresent, "Gated 'Invoke…' console button not found (expected present but untriggered).");
            _output.WriteLine("Step 7 - gated Invoke button present; intentionally NOT triggered (read-only run).");
            _host.HardReset();

            // 8) Export each catalog format -> menu shot -> Save dialog shot -> save to the screenshots folder ->
            //    report shot. Retry once per format: the menu-item selection can transiently miss on this flaky host.
            foreach (var (menu, index, ext) in Exports)
            {
                var ok = false;
                for (var r = 0; r < 2 && !ok; r++)
                {
                    if (r > 0) { _host.PressEscape(); _host.HardReset(); }
                    ok = ExportOne(menu, index, ext, exportDir);
                }
                Check(ok, $"Export '{menu}' (.{ext}) to '{exportDir}' did not complete.");
                _host.HardReset(); // the Save dialog churns the tree
            }

            // 9) Click Help. The button opens a MODAL Help & Support dialog; the UIA Invoke can report false
            // because the modal blocks the UI thread mid-handshake — so verify by the dialog APPEARING (which we
            // screenshot), not by the click's return value. Then close it so it can't block a re-run.
            _host.ForceForeground();
            _host.HardReset();
            _host.ClickHelp();
            Thread.Sleep(1800);
            var helpHwnd = _host.DialogHwnd();
            ShotHwnd("09-help", helpHwnd);
            Check(helpHwnd != IntPtr.Zero, "Help & Support dialog did not open.");
            _host.ClickProcessDialogButton("Close", TimeSpan.FromSeconds(5));

            // 10) Close the tool via the host's own tab-close (Ctrl+F4).
            _host.ForceForeground();
            _host.HardReset();
            var closed = _host.CloseActiveToolTab();
            Thread.Sleep(1500);
            Shot("10-tool-closed");
            Check(closed, "Could not close the tool tab (Ctrl+F4).");

            _output.WriteLine($"Screenshots: {RunDir()}");
            Assert.True(_failures.Count == 0, "Steps failed:\n - " + string.Join("\n - ", _failures));
        }

        /// <summary>
        /// Open the Export dropdown, pick the format, drive the Save As dialog to the target folder + Save.
        /// Verifies a new <c>&lt;round&gt;-*.ext</c> file landed in the save folder (the tool supplies the default
        /// name). This tool writes the file silently (no "Open it now?" prompt), so there's no report to capture.
        /// Returns true on success.
        /// </summary>
        private bool ExportOne(string menuText, int index, string ext, string saveDir)
        {
            var before = ExportCount(saveDir, ext);
            _host.ForceForeground();
            _host.HardReset();

            // (a) open the Export dropdown and screenshot the MENU
            if (!_host.OpenExportMenu()) return false;
            ShotHwnd($"08-export-{ext}-1-menu", _host.PopupHwnd());

            // (b) pick the format, wait for the Save As dialog, then set the File name to the FULL path inside the
            //     screenshots folder (typing an absolute path routes the save there) and screenshot the SAVE DIALOG
            if (!_host.SelectExportItem(menuText, index)) return false;
            var saveHwnd = _host.WaitForSaveDialog(TimeSpan.FromSeconds(20));
            if (saveHwnd == IntPtr.Zero) return false;
            Thread.Sleep(1500);   // let the shell dialog finish rendering
            var defaultName = _host.ReadSaveFileName();
            if (string.IsNullOrWhiteSpace(defaultName)) defaultName = $"custom-api-catalog.{ext}";
            _host.SetSaveFileName(Path.Combine(saveDir, $"{_round}-{Path.GetFileName(new string(defaultName.Where(ch => !Path.GetInvalidPathChars().Contains(ch)).ToArray()))}"));
            ShotHwnd($"08-export-{ext}-2-savedialog", _host.SaveDialogHwnd());

            // (c) Save. The tool writes the file without opening it, so just confirm the file landed.
            if (!_host.ClickSaveInDialog()) return false;
            Thread.Sleep(2000);
            _host.ForceForeground();

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
            Path.Combine(ScreenshotRoot(), _dateStamp, "custom-api-explorer");

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
