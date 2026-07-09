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
    /// TIER-3c — full END-TO-END walkthrough of the View Performance Analyzer (opt-in, LOCAL only), following the
    /// exact operator script: launch XrmToolBox if needed, connect DEV, open the tool, "Refresh tables", pick the
    /// first table, "Analyze views", inspect the first view's finding/FetchXML detail panes, "Time selected view"
    /// (opt-in, read-only), export EACH format from the Export dropdown to the screenshots folder, then open Help
    /// and close the tab. This tool needs a live connection but NO solution and NO target env. A screenshot of the
    /// XrmToolBox window ONLY is captured after every step (via PrintWindow, so the IDE/desktop never appear),
    /// under screenshots/&lt;yyyyMMdd&gt;/view-performance-analyzer/NN-step.png.
    ///
    /// Unlike the analyzer tools driven by a "Solution:" combo, this tool selects a TABLE (its combo surfaces via
    /// the host's generic combo fallback), and its six exports live under one "Export" dropdown button — each opens
    /// a Save As dialog and writes the file directly with NO "Open it now?" prompt (so the export helper below
    /// drives the dropdown + Save, and never a report-open prompt or multi-sheet capture).
    ///
    /// Constraints (see README Tier-3b): interactive connections can't authenticate unattended, so DEV must have
    /// a warm token; desktop must stay UNLOCKED. Gated behind XTB_E2E=1.
    ///
    /// Env vars: XTB_E2E=1 to run; XTB_EXE; XTB_SOURCE (default "XTS-CI-DEV"); UISMOKE_SCREENSHOT_DIR.
    /// </summary>
    public sealed class ViewPerformanceAnalyzerE2ETest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly XtbHost _host = new XtbHost();
        private readonly string _dateStamp = DateTime.Now.ToString("yyyyMMdd");
        private readonly List<string> _failures = new List<string>();
        private int _shot;
        private string _round = "TR-001";   // test-round tag, prefixed on every screenshot + exported file

        private const string Tool = "View Performance Analyzer";

        // Export dropdown items (popup text fragment, 1-based position in the dropdown) -> saved file extension,
        // in Designer order. Each opens a Save As dialog directly (no "Open it now?" prompt).
        private static readonly (string Menu, int Index, string Ext)[] Exports =
        {
            ("Excel", 1, "xlsx"),
            ("PDF", 2, "pdf"),
            ("JSON", 3, "json"),
            ("HTML", 4, "html"),
            ("Markdown", 5, "md"),
            ("CSV", 6, "csv"),
        };

        public ViewPerformanceAnalyzerE2ETest(ITestOutputHelper output) => _output = output;

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
            // …\UiSmokeTests\screenshots\<yyyymmdd>\view-performance-analyzer\  (created if missing).
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

            // 3) Find the tool and double-click to open. Verify it REALLY opened by finding its primary "Refresh
            //    tables" toolbar button (a tool-only control) rather than a tab caption. Retry the open if needed.
            var toolOpen = false;
            for (var i = 0; i < 3 && !toolOpen; i++)
            {
                if (i > 0) _host.HardReset();
                if (!_host.WaitForClickable("Refresh tables", TimeSpan.FromSeconds(2)))
                    _host.OpenTool(Tool);
                toolOpen = _host.WaitForClickable("Refresh tables", TimeSpan.FromSeconds(25));
            }
            Shot("03-tool-open");
            Check(toolOpen, $"'{Tool}' did not open (no 'Refresh tables' toolbar after retries).");

            // 3b) Validation guard — "Analyze views" with NO table selected must pop the "No table selected" dialog
            //     rather than analyze. Do this before any table is loaded/selected.
            _host.ClickByPartialName("Analyze views");
            Thread.Sleep(1200);
            var g = _host.DialogHwnd();
            ShotHwnd("03b-guard-no-table", g);
            Check(g != IntPtr.Zero, "Analyze with no table did not show the validation dialog.");
            _host.ClickProcessDialogButton("OK", TimeSpan.FromSeconds(5));
            _host.HardReset();

            // 4) Click "Refresh tables" and pick the FIRST table. The combo doesn't auto-select, so drive the
            //    generic combo helper (which finds this tool's only combo via its non-"All connections" fallback)
            //    to expand + select index 0. Retry: the click or the async table load can transiently no-op.
            string chosen = null;
            for (var i = 0; i < 4 && string.IsNullOrWhiteSpace(chosen); i++)
            {
                if (i > 0) { _host.HardReset(); Thread.Sleep(2000); }
                _host.ClickByPartialName("Refresh tables");
                Thread.Sleep(3500);
                chosen = _host.SelectFirstSolution();   // picks table index 0 (host combo helper, fallback-scoped)
            }
            _host.HardReset(); // loading tables poisons the UIA cache
            Shot("04-tables-loaded");
            Check(!string.IsNullOrWhiteSpace(chosen), "Tables dropdown never populated / first table not selected.");

            // 5) Confirm a table is selected.
            chosen = _host.ReadComboValue();
            Shot("05-table-selected");
            Check(!string.IsNullOrWhiteSpace(chosen), "No table is selected after Refresh tables.");
            _output.WriteLine($"Step 5 - using first table: \"{chosen}\".");

            // 5b) Flip the "Include personal views" toolbar toggle (also analyze users' personal views), then
            //     restore it. It's a CheckOnClick ToolStripButton, so a click toggles it.
            var toggled = _host.ClickByPartialName("Include personal views");
            Thread.Sleep(800);
            Shot("05b-include-personal");
            Check(toggled, "Could not toggle the 'Include personal views' filter.");
            _host.ClickByPartialName("Include personal views");   // restore
            Thread.Sleep(600);
            _host.HardReset();

            // 6) Analyze views — retrieves and scores every view for the selected table.
            Check(_host.ClickByPartialName("Analyze views"), "Could not find 'Analyze views'.");
            Thread.Sleep(6000);   // let the async analysis run
            _host.HardReset();    // it mutates the tree heavily
            var hasRows = _host.WaitForGridRows(TimeSpan.FromSeconds(120));
            Shot("06-analyzed");
            _output.WriteLine(hasRows ? "Analysis produced scored view rows." : "View grid rows not detected via UIA (still exportable).");
            _host.HardReset();

            // 6b) Select the first view row so the finding / FetchXML / layout detail panes fill.
            _host.SelectFirstFinding();
            Thread.Sleep(800);
            var detail = _host.ReadDetailPane();
            Shot("06b-view-detail");
            Check(!string.IsNullOrWhiteSpace(detail), "Selecting a view did not populate the detail pane (FetchXML).");
            _host.HardReset();

            // 6c) "Time selected view" — opt-in, read-only capped execution of the selected view's FetchXML. On
            //     success it only updates the status bar (no dialog); treat the click as the signal.
            var timed = _host.ClickByPartialName("Time selected view");
            Thread.Sleep(6000);   // let the read-only timing query run
            _host.HardReset();
            Shot("06c-timed-view");
            Check(timed, "Could not find 'Time selected view'.");

            // 7) Export each option -> menu shot -> Save dialog shot -> save to the screenshots folder -> verify the
            //    file landed. Retry once per format: the menu-item selection can transiently miss on this flaky host.
            //    (No "Open it now?" prompt — each Save writes the file directly.)
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

            // 8) Click Help. The button opens a MODAL Help & Support dialog; the UIA Invoke can report false because
            // the modal blocks the UI thread mid-handshake — so verify by the dialog APPEARING (which we screenshot),
            // not by the click's return value. Then close it so it can't block a re-run.
            _host.ForceForeground();
            _host.HardReset();
            _host.ClickHelp();
            Thread.Sleep(1800);
            var helpHwnd = _host.DialogHwnd();
            ShotHwnd("08-help", helpHwnd);
            Check(helpHwnd != IntPtr.Zero, "Help & Support dialog did not open.");
            _host.ClickProcessDialogButton("Close", TimeSpan.FromSeconds(5));

            // 9) Close the tool via the host's own tab-close (Ctrl+F4) — the per-tool "Close" button was removed
            //    from the suite, so tear the tab down through XrmToolBox itself.
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
        /// verify a new <c>&lt;round&gt;-*.ext</c> file landed there (the tool supplies the default name). This
        /// tool's exports write the file directly with no "Open it now?" prompt. Returns true on success.
        /// </summary>
        private bool ExportOne(string menuText, int index, string ext, string saveDir)
        {
            var before = ExportCount(saveDir, ext);
            _host.ForceForeground();
            _host.HardReset();

            // (a) open the Export dropdown and screenshot the MENU
            if (!_host.OpenExportMenu()) return false;
            ShotHwnd($"07-export-{ext}-1-menu", _host.PopupHwnd());

            // (b) pick the format, wait for the Save As dialog, then set the File name to the FULL path inside the
            //     screenshots folder (typing an absolute path routes the save there) and screenshot the SAVE DIALOG.
            if (!_host.SelectExportItem(menuText, index)) return false;
            var saveHwnd = _host.WaitForSaveDialog(TimeSpan.FromSeconds(20));
            if (saveHwnd == IntPtr.Zero) return false;
            Thread.Sleep(1500);   // let the shell dialog finish rendering
            var defaultName = _host.ReadSaveFileName();
            if (string.IsNullOrWhiteSpace(defaultName)) defaultName = $"view-performance.{ext}";
            _host.SetSaveFileName(Path.Combine(saveDir, $"{_round}-{Path.GetFileName(new string(defaultName.Where(ch => !Path.GetInvalidPathChars().Contains(ch)).ToArray()))}"));
            ShotHwnd($"07-export-{ext}-2-savedialog", _host.SaveDialogHwnd());

            // (c) Save — writes the file directly (no open prompt). Screenshot the host (status message) after.
            if (!_host.ClickSaveInDialog()) return false;
            Thread.Sleep(2000);
            _host.ForceForeground();
            _host.HardReset();
            ShotHwnd($"07-export-{ext}-3-saved", _host.MainHwnd);

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
            Path.Combine(ScreenshotRoot(), _dateStamp, "view-performance-analyzer");

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
