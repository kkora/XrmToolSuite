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
    /// TIER-3c — full END-TO-END walkthrough of the ERD Generator tool (opt-in, LOCAL only), following the
    /// operator script: launch XrmToolBox if needed, connect DEV, open the tool, Load tables, filter + check a
    /// table, Generate the ERD, read the Mermaid preview, then export EACH format (Mermaid, PlantUML, SVG, PNG,
    /// PDF, HTML, Markdown, JSON — in the toolbar's Export-menu order) alongside the screenshots and open it,
    /// then open Help and close the tab. Like the Solution Complexity Score this tool needs NO target
    /// environment. A screenshot of the XrmToolBox window ONLY is captured after every step (via PrintWindow,
    /// so the IDE/desktop never appear), under screenshots/&lt;yyyyMMdd&gt;/erd-generator/NN-step.png.
    ///
    /// Constraints (see README Tier-3b): interactive connections can't authenticate unattended, so DEV must
    /// have a warm token; desktop must stay UNLOCKED. Gated behind XTB_E2E=1.
    ///
    /// Env vars: XTB_E2E=1 to run; XTB_EXE; XTB_SOURCE (default "XTS-CI-DEV"); UISMOKE_SCREENSHOT_DIR.
    /// </summary>
    public sealed class ErdGeneratorE2ETest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly XtbHost _host = new XtbHost();
        private readonly string _dateStamp = DateTime.Now.ToString("yyyyMMdd");
        private readonly List<string> _failures = new List<string>();
        private int _shot;
        private string _round = "TR-001";   // test-round tag, prefixed on every screenshot + exported file

        private const string Tool = "ERD Generator";

        // Export menu items (popup text fragment, 1-based position in the dropdown) -> saved file extension.
        // Order MUST match the toolbar's Export dropdown (see ErdGeneratorControl ctor / Designer).
        private static readonly (string Menu, int Index, string Ext)[] Exports =
        {
            ("Mermaid", 1, "mmd"),
            ("PlantUML", 2, "puml"),
            ("SVG", 3, "svg"),
            ("PNG", 4, "png"),
            ("PDF", 5, "pdf"),
            ("HTML", 6, "html"),
            ("Markdown", 7, "md"),
            ("JSON", 8, "json"),
        };

        public ErdGeneratorE2ETest(ITestOutputHelper output) => _output = output;

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
            // …\UiSmokeTests\screenshots\<yyyymmdd>\erd-generator\  (created if missing).
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

            // 3) Find the tool and double-click to open. Verify it REALLY opened by finding its "Load tables"
            //    toolbar button (a tool-only control) rather than a tab caption. Retry the open if needed.
            var toolOpen = false;
            for (var i = 0; i < 3 && !toolOpen; i++)
            {
                if (i > 0) _host.HardReset();
                if (!_host.WaitForClickable("Load tables", TimeSpan.FromSeconds(2)))
                    _host.OpenTool(Tool);
                toolOpen = _host.WaitForClickable("Load tables", TimeSpan.FromSeconds(25));
            }
            Shot("03-tool-open");
            Check(toolOpen, $"'{Tool}' did not open (no 'Load tables' toolbar after retries).");

            // 3b) Validation guard — Generate with NO table checked must pop the "Check at least one table to
            //     diagram." dialog rather than build. Do this before any table is loaded/checked.
            _host.ClickByPartialName("Generate");
            Thread.Sleep(1200);
            var g = _host.DialogHwnd();
            ShotHwnd("03b-guard-no-table", g);
            Check(g != IntPtr.Zero, "Generate with no table checked did not show the validation dialog.");
            _host.ClickProcessDialogButton("OK", TimeSpan.FromSeconds(5));
            _host.HardReset();

            // 4) Load tables, then filter to a universally-present table (account) and CHECK it. Retry the whole
            //    load+check: the click, the async metadata scan, or the CheckedListBox toggle can transiently
            //    no-op on this flaky host.
            var tableChecked = false;
            for (var i = 0; i < 3 && !tableChecked; i++)
            {
                if (i > 0) { _host.HardReset(); Thread.Sleep(2000); }
                _host.ClickByPartialName("Load tables");
                Thread.Sleep(9000);      // ListTableInfos scans all entity metadata — allow time
                _host.HardReset();       // the big list load poisons the UIA cache
                _host.SetToolTextBox("account");   // filter the CheckedListBox down so the row is on-screen
                Thread.Sleep(1500);
                tableChecked = _host.ToggleAnalyzer("[account]");  // check the account row (CheckOnClick list)
            }
            _host.HardReset();
            Shot("04-tables-loaded-and-checked");
            Check(tableChecked, "Could not load tables and check the 'account' row (after retries).");

            // 5) Generate the ERD from the checked table(s); Export enables once the model has tables.
            Check(_host.ClickByPartialName("Generate"), "Could not find 'Generate'.");
            Thread.Sleep(6000);   // let the async metadata build run
            _host.HardReset();    // it mutates the tree heavily
            var exportReady = false;
            var deadline = DateTime.UtcNow.AddSeconds(120);
            while (DateTime.UtcNow < deadline && !exportReady)
            {
                exportReady = _host.ButtonEnabled("Export");
                if (!exportReady) { Thread.Sleep(1500); _host.RefreshWindow(); }
            }
            Shot("05-generated");
            Check(exportReady, "ERD did not build (Export never enabled after Generate).");
            _host.HardReset();

            // 6) Read the Mermaid preview pane (the large read-only text box) to confirm the diagram rendered.
            var preview = _host.ReadDetailPane();
            Shot("06-preview");
            Check(!string.IsNullOrWhiteSpace(preview), "Mermaid preview pane is empty after Generate.");
            _output.WriteLine("Step 6 - preview length: " + (preview ?? "").Length + " chars.");
            _host.HardReset();

            // 7) Export each option -> menu shot -> Save dialog shot -> save to the screenshots folder -> Yes -> report shot.
            //    Retry once per format: the menu-item selection can transiently miss on this flaky host.
            foreach (var (menu, index, ext) in Exports)
            {
                var ok = false;
                for (var r = 0; r < 2 && !ok; r++)
                {
                    if (r > 0) { _host.PressEscape(); _host.HardReset(); }
                    ok = ExportOne(menu, index, ext, exportDir);
                }
                Check(ok, $"Export '{menu}' (.{ext}) to '{exportDir}' did not complete.");
                _host.HardReset(); // the Save dialog + open prompt churn the tree
            }

            // 8) Click Help. The button opens a MODAL Help & Support dialog; the UIA Invoke can report false
            // because the modal blocks the UI thread mid-handshake — so verify by the dialog APPEARING (which we
            // screenshot), not by the click's return value. Then close it so it can't block a re-run.
            _host.ForceForeground();   // raise XrmToolBox above the opened reports
            _host.HardReset();
            _host.ClickHelp();
            Thread.Sleep(1800);
            var helpHwnd = _host.DialogHwnd();
            ShotHwnd("08-help", helpHwnd);
            Check(helpHwnd != IntPtr.Zero, "Help & Support dialog did not open.");
            _host.ClickProcessDialogButton("Close", TimeSpan.FromSeconds(5));

            // 9) Close the tool via the host's own tab-close (Ctrl+F4) — the per-tool "Close" button was
            //     removed from the suite, so tear the tab down through XrmToolBox itself.
            _host.ForceForeground();
            _host.HardReset();
            var closed = _host.CloseActiveToolTab();
            Thread.Sleep(1500);
            Shot("09-tool-closed");
            Check(closed, "Could not close the tool tab (Ctrl+F4).");

            _output.WriteLine($"Screenshots: {RunDir()}");
            Assert.True(_failures.Count == 0, "Steps failed:\n - " + string.Join("\n - ", _failures));
        }

        /// <summary>
        /// Open the Export dropdown, pick the format, drive the Save As dialog to the target folder + Save, then
        /// click Yes on the "Open it now?" prompt. Verifies a new <c>&lt;round&gt;-*.ext</c> file landed in the
        /// save folder (the tool supplies the default name). Returns true on success.
        /// </summary>
        private bool ExportOne(string menuText, int index, string ext, string saveDir)
        {
            var before = ExportCount(saveDir, ext);
            // Each prior "Yes" opened a report in an external app that grabbed foreground — raise XrmToolBox so
            // the toolbar click lands on it, not the opened report.
            _host.ForceForeground();
            _host.HardReset();

            // (a) open the Export dropdown and screenshot the MENU
            if (!_host.OpenExportMenu()) return false;
            ShotHwnd($"07-export-{ext}-1-menu", _host.PopupHwnd());

            // (b) pick the format, wait for the Save As dialog, then set the File name to the FULL path inside the
            //     screenshots folder (typing an absolute path routes the save there) and screenshot the SAVE DIALOG
            if (!_host.SelectExportItem(menuText, index)) return false;
            var saveHwnd = _host.WaitForSaveDialog(TimeSpan.FromSeconds(20));
            if (saveHwnd == IntPtr.Zero) return false;
            Thread.Sleep(1500);   // let the shell dialog finish rendering
            var defaultName = _host.ReadSaveFileName();
            if (string.IsNullOrWhiteSpace(defaultName)) defaultName = $"ERD.{ext}";
            _host.SetSaveFileName(Path.Combine(saveDir, $"{_round}-{Path.GetFileName(new string(defaultName.Where(ch => !Path.GetInvalidPathChars().Contains(ch)).ToArray()))}"));
            ShotHwnd($"07-export-{ext}-2-savedialog", _host.SaveDialogHwnd());

            // (c) Save, then "Open it now?" -> Yes, then MAXIMIZE the opened report and screenshot it. Guard the
            //     maximize/minimize to the REPORT's process only — never XrmToolBox — so we don't accidentally
            //     minimize the host and break the next export.
            if (!_host.ClickSaveInDialog()) return false;
            _host.ClickProcessDialogButton("Yes", TimeSpan.FromSeconds(15));
            Thread.Sleep(4000);   // let the report open in its default app
            var reportHwnd = _host.ForegroundReportHwnd();  // foreground window, but only if it's NOT XrmToolBox
            if (reportHwnd != IntPtr.Zero) { _host.MaximizeWindow(reportHwnd); Thread.Sleep(1500); }

            ShotHwnd($"07-export-{ext}-3-report", reportHwnd != IntPtr.Zero ? reportHwnd : _host.ForegroundHwnd());

            // Close the opened report (report process only) so it can't occlude XrmToolBox for the next export.
            if (reportHwnd != IntPtr.Zero) { _host.CloseWindow(reportHwnd); Thread.Sleep(800); }
            _host.ForceForeground();

            return ExportCount(saveDir, ext) > before;
        }

        private int ExportCount(string dir, string ext)
        {
            try { return Directory.GetFiles(dir, $"{_round}-*.{ext}").Length; }
            catch { return 0; }
        }

        // --- screenshots (via PrintWindow; window-only for tool steps, the specific dialog/report otherwise) ---

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

        /// <summary>Capture a specific window (dialog/report) by HWND; falls back to the main window if Zero.</summary>
        private void ShotHwnd(string label, IntPtr hwnd) => _host.Screenshot(ShotPath(label), hwnd);

        // One folder per DATE (yyyymmdd); multiple runs the same day share it (files are time-stamped).
        private string RunDir() =>
            Path.Combine(ScreenshotRoot(), _dateStamp, "erd-generator");

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
