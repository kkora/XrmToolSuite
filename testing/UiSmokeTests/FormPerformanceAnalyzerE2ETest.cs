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
    /// TIER-3c — full END-TO-END walkthrough of the Form Performance Analyzer tool (opt-in, LOCAL only),
    /// following the operator script: launch XrmToolBox if needed, connect DEV, open the tool, exercise the
    /// "Compare…" pick-two guard, "Analyze forms" over the whole environment (confirming the "analyze all main
    /// forms" destructive-read prompt), select the heaviest form and read its metric/summary detail, open the
    /// "Score settings…" dialog, export EACH format (CSV, then HTML report) alongside the screenshots, then open
    /// Help and close the tab. This tool needs a live connection but NO solution and NO target env.
    ///
    /// The results live in a WinForms DataGridView (grdForms); selecting a row fills the metric-breakdown and
    /// recommendation grids plus the summary textbox. The two exports are dropdown items (tsbExport) that each
    /// open a Save As dialog and write the file directly with no "Open it now?" prompt — so the export helper
    /// below drives the menu + Save and just verifies the file landed. A screenshot of the XrmToolBox window
    /// ONLY is captured after every step (via PrintWindow, so the IDE/desktop never appear), under
    /// screenshots/&lt;yyyyMMdd&gt;/form-performance-analyzer/NN-step.png.
    ///
    /// Constraints (see README Tier-3b): interactive connections can't authenticate unattended, so DEV must have
    /// a warm token; desktop must stay UNLOCKED. Gated behind XTB_E2E=1.
    ///
    /// Env vars: XTB_E2E=1 to run; XTB_EXE; XTB_SOURCE (default "XTS-CI-DEV"); UISMOKE_SCREENSHOT_DIR.
    /// </summary>
    public sealed class FormPerformanceAnalyzerE2ETest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly XtbHost _host = new XtbHost();
        private readonly string _dateStamp = DateTime.Now.ToString("yyyyMMdd");
        private readonly List<string> _failures = new List<string>();
        private int _shot;
        private string _round = "TR-001";   // test-round tag, prefixed on every screenshot + exported file

        private const string Tool = "Form Performance Analyzer";

        // Export menu items (popup text fragment, 1-based position in the dropdown) -> saved file extension.
        // Order matches the Designer's tsbExport.DropDownItems: CSV (forms + metrics), then HTML report.
        private static readonly (string Menu, int Index, string Ext)[] Exports =
        {
            ("CSV", 1, "csv"),
            ("HTML report", 2, "html"),
        };

        public FormPerformanceAnalyzerE2ETest(ITestOutputHelper output) => _output = output;

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
            // …\UiSmokeTests\screenshots\<yyyymmdd>\form-performance-analyzer\  (created if missing).
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

            // 3) Find the tool and double-click to open. Verify it REALLY opened by finding its primary "Analyze
            //    forms" toolbar button (a tool-only control) rather than a tab caption. Retry the open if needed.
            var toolOpen = false;
            for (var i = 0; i < 3 && !toolOpen; i++)
            {
                if (i > 0) _host.HardReset();
                if (!_host.WaitForClickable("Analyze forms", TimeSpan.FromSeconds(2)))
                    _host.OpenTool(Tool);
                toolOpen = _host.WaitForClickable("Analyze forms", TimeSpan.FromSeconds(25));
            }
            Shot("03-tool-open");
            Check(toolOpen, $"'{Tool}' did not open (no 'Analyze forms' toolbar after retries).");

            // 3b) Validation guard — "Compare…" with fewer than two forms selected (the grid is empty here) must pop
            //     the "Pick two forms" info dialog rather than compare. Do this before any analysis.
            _host.ClickByPartialName("Compare");
            Thread.Sleep(1200);
            var guard = _host.DialogHwnd();
            ShotHwnd("03b-guard-pick-two", guard);
            Check(guard != IntPtr.Zero, "Compare with no selection did not show the 'Pick two forms' dialog.");
            _host.ClickProcessDialogButton("OK", TimeSpan.FromSeconds(5));
            _host.HardReset();

            // 4) Analyze forms. With no table scope this first pops an OK/Cancel "Analyze all main forms"
            //    confirmation (the large-read guard); screenshot it, click OK to proceed. Retry the click a few
            //    times: it can transiently no-op on this flaky host before the dialog appears.
            IntPtr confirm = IntPtr.Zero;
            for (var i = 0; i < 3 && confirm == IntPtr.Zero; i++)
            {
                if (i > 0) _host.HardReset();
                _host.ClickByPartialName("Analyze forms");
                Thread.Sleep(1500);
                confirm = _host.DialogHwnd();
            }
            ShotHwnd("04-analyze-confirm", confirm);
            Check(confirm != IntPtr.Zero, "Analyze with no scope did not show the 'Analyze all main forms' confirmation.");
            _host.ClickProcessDialogButton("OK", TimeSpan.FromSeconds(5));
            _host.HardReset();    // the async scan mutates the tree heavily

            // 5) Wait for the results grid to gain data rows (every main form scored and ranked).
            var hasRows = _host.WaitForGridRows(TimeSpan.FromSeconds(120));
            Shot("05-analyzed");
            _output.WriteLine(hasRows ? "Analysis produced scored form rows." : "Analysis grid rows not detected via UIA (still exportable).");
            _host.HardReset();

            // 6) Select the first (heaviest) form row so the metric-breakdown/recommendation grids and the summary
            //    textbox fill; read the summary/detail text back as evidence it populated.
            _host.SelectFirstFinding();
            Thread.Sleep(1000);
            var detail = _host.ReadDetailPane();
            Shot("06-form-detail");
            Check(!string.IsNullOrWhiteSpace(detail), "Selecting a form did not populate the metric/summary detail.");
            _output.WriteLine("Step 6 - metric breakdown + summary detail populated.");
            _host.HardReset();

            // 7) Score settings — opens the scoring-weights/thresholds dialog; screenshot it and Cancel (no change).
            _host.ClickByPartialName("Score settings");
            Thread.Sleep(1200);
            var settingsHwnd = _host.DialogHwnd();
            ShotHwnd("07-score-settings", settingsHwnd);
            Check(settingsHwnd != IntPtr.Zero, "'Score settings…' did not open a dialog.");
            _host.ClickProcessDialogButton("Cancel", TimeSpan.FromSeconds(5));
            _host.HardReset();

            // 8) Export each format -> menu shot -> Save dialog shot -> save to the screenshots folder -> verify the
            //    file landed. Retry once per format: the menu-item selection can transiently miss on this flaky host.
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

            // 10) Close the tool via the host's own tab-close (Ctrl+F4) — the per-tool "Close" button was removed
            //     from the suite, so tear the tab down through XrmToolBox itself.
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
            if (string.IsNullOrWhiteSpace(defaultName)) defaultName = $"form-performance.{ext}";
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
            Path.Combine(ScreenshotRoot(), _dateStamp, "form-performance-analyzer");

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
