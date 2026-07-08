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
    /// TIER-3c — full END-TO-END walkthrough of the Technical Debt Analyzer (opt-in, LOCAL only), following
    /// the exact operator script: launch XrmToolBox if needed, connect DEV, open the tool, Analyze the WHOLE
    /// environment (no solution selection, no target env), toggle an analyzer, open the executive summary and
    /// AI settings dialogs, export EACH format and open it, then open Help. A screenshot of the XrmToolBox
    /// window ONLY is captured after every step (via PrintWindow, so the IDE/desktop never appear), under
    /// screenshots/&lt;yyyyMMdd-HHmmss&gt;/technical-debt-analyzer/NN-step.png.
    ///
    /// Constraints (see README Tier-3b): interactive connections can't authenticate unattended, so DEV must
    /// have a warm token; desktop must stay UNLOCKED. Gated behind XTB_E2E=1.
    ///
    /// Env vars: XTB_E2E=1 to run; XTB_EXE; XTB_SOURCE (default "XTS-CI-DEV"); UISMOKE_SCREENSHOT_DIR.
    /// </summary>
    public sealed class TechnicalDebtAnalyzerE2ETest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly XtbHost _host = new XtbHost();
        private readonly string _dateStamp = DateTime.Now.ToString("yyyyMMdd");
        private readonly List<string> _failures = new List<string>();
        private int _shot;
        private string _round = "TR-001";   // test-round tag, prefixed on every screenshot + exported file

        private const string Tool = "Technical Debt Analyzer";

        // Export menu items (popup text fragment, 1-based position in the dropdown) -> exported file extension.
        private static readonly (string Menu, int Index, string Ext)[] Exports =
        {
            ("PDF report", 1, "pdf"),
            ("HTML dashboard", 2, "html"),
            ("Excel workbook", 3, "xlsx"),
            ("JSON", 4, "json"),
            ("Cleanup checklist", 5, "md"),
        };

        public TechnicalDebtAnalyzerE2ETest(ITestOutputHelper output) => _output = output;

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
            // …\UiSmokeTests\screenshots\<yyyymmdd>\technical-debt-analyzer\  (created if missing).
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

            // 2) Connect to DEV (select DEV -> Connect if not connected)
            var connected = _host.EnsureConnected(source, TimeSpan.FromSeconds(90));
            Shot("02-connect-dev");
            Check(connected, $"Could not connect to '{source}'. Is its token warm (connect once by hand)?");
            Thread.Sleep(4000);   // let the post-connect metadata load finish settling
            _host.HardReset();    // before touching the Tools list (connect poisons the UIA cache)

            // 3) Find the tool and double-click to open. Verify it REALLY opened by finding its primary
            //    "Analyze environment" toolbar button (a tool-only control) rather than a tab caption — the
            //    Tools-list tile carries a "(NN)" rating badge that can false-match a "(<conn>)" tab check.
            //    Retry the open if needed.
            var toolOpen = false;
            for (var i = 0; i < 3 && !toolOpen; i++)
            {
                if (i > 0) _host.HardReset();
                if (!_host.WaitForClickable("Analyze environment", TimeSpan.FromSeconds(2)))
                    _host.OpenTool(Tool);
                toolOpen = _host.WaitForClickable("Analyze environment", TimeSpan.FromSeconds(25));
            }
            Shot("03-tool-open");
            Check(toolOpen, $"'{Tool}' did not open (no 'Analyze environment' toolbar after retries).");

            // 4) Click Analyze environment — scans the WHOLE environment (no solution needed).
            Check(_host.ClickByPartialName("Analyze environment"), "Could not find 'Analyze environment'.");
            Thread.Sleep(6000);   // let the async analysis run
            _host.HardReset();    // it mutates the tree heavily
            var hasRows = _host.WaitForGridRows(TimeSpan.FromSeconds(120));
            Shot("04-analysis-complete");
            _output.WriteLine(hasRows ? "Analysis produced findings." : "Analysis grid rows not detected via UIA (still exportable).");

            // 5) Toggle an analyzer in the left checklist (uncheck, screenshot, re-check to restore).
            var toggled = _host.ToggleAnalyzer("Unused Metadata");
            Shot("05-analyzer-toggled");
            Check(toggled, "Could not toggle an analyzer in the checklist.");
            _host.ToggleAnalyzer("Unused Metadata");   // restore
            _host.HardReset();

            // 6) Executive summary -> offline summary dialog (no API key configured -> deterministic offline summary).
            _host.ClickByPartialName("Executive summary");
            Thread.Sleep(3500);
            var aiHwnd = _host.DialogHwnd();
            ShotHwnd("06-exec-summary", aiHwnd);
            Check(aiHwnd != IntPtr.Zero, "Executive summary did not open a summary dialog.");
            _host.ClickProcessDialogButton("Close", TimeSpan.FromSeconds(5));
            _host.HardReset();

            // 7) AI options -> "AI settings…" dialog (open, screenshot, cancel).
            _host.ClickByPartialName("AI options");
            Thread.Sleep(1000);
            _host.ClickPopupItem("AI settings");
            Thread.Sleep(1200);
            var keyHwnd = _host.DialogHwnd();
            ShotHwnd("07-ai-settings", keyHwnd);
            Check(keyHwnd != IntPtr.Zero, "AI options 'AI settings…' did not open a dialog.");
            _host.ClickProcessDialogButton("Cancel", TimeSpan.FromSeconds(5));
            _host.HardReset();

            // 8) Export each option -> menu shot -> Save dialog shot -> save to the screenshots folder -> Yes -> report shot.
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

            // 9) Click Help. The button opens a MODAL Help & Support dialog; the UIA Invoke can report false
            // because the modal blocks the UI thread mid-handshake — so verify by the dialog APPEARING (which we
            // screenshot), not by the click's return value. Then close it so it can't block a re-run.
            _host.ForceForeground();   // raise XrmToolBox above the opened reports
            _host.HardReset();
            _host.ClickHelp();
            Thread.Sleep(1800);
            var helpHwnd = _host.DialogHwnd();
            ShotHwnd("10-help", helpHwnd);
            Check(helpHwnd != IntPtr.Zero, "Help & Support dialog did not open.");
            _host.ClickProcessDialogButton("Close", TimeSpan.FromSeconds(5));

            // 10) Close the tool via the host's own tab-close (Ctrl+F4) — the per-tool "Close" button was
            //     removed from the suite, so tear the tab down through XrmToolBox itself.
            _host.ForceForeground();
            _host.HardReset();
            var closed = _host.CloseActiveToolTab();
            Thread.Sleep(1500);
            Shot("11-tool-closed");
            Check(closed, "Could not close the tool tab (Ctrl+F4).");

            _output.WriteLine($"Screenshots: {RunDir()}");
            Assert.True(_failures.Count == 0, "Steps failed:\n - " + string.Join("\n - ", _failures));
        }

        /// <summary>
        /// Open the Export dropdown, pick the format, drive the Save As dialog to the screenshots folder + Save,
        /// then click Yes on the "Open it now?" prompt. Verifies a new <c>&lt;round&gt;-*.ext</c> file landed in
        /// the save folder (the tool supplies the default name). Returns true on success.
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
            ShotHwnd($"09-export-{ext}-1-menu", _host.PopupHwnd());

            // (b) pick the format, wait for the Save As dialog, then set the File name to the FULL path inside the
            //     screenshots folder (typing an absolute path routes the save there) and screenshot the SAVE DIALOG
            if (!_host.SelectExportItem(menuText, index)) return false;
            var saveHwnd = _host.WaitForSaveDialog(TimeSpan.FromSeconds(20));
            if (saveHwnd == IntPtr.Zero) return false;
            Thread.Sleep(1500);   // let the shell dialog finish rendering
            var defaultName = _host.ReadSaveFileName();
            if (string.IsNullOrWhiteSpace(defaultName)) defaultName = $"TechnicalDebt.{ext}";
            _host.SetSaveFileName(Path.Combine(saveDir, $"{_round}-{Path.GetFileName(defaultName)}"));
            ShotHwnd($"09-export-{ext}-2-savedialog", _host.SaveDialogHwnd());

            // (c) Save, then "Open it now?" -> Yes, then MAXIMIZE the opened report and screenshot it. Guard the
            //     maximize/minimize to the REPORT's process only — never XrmToolBox — so we don't accidentally
            //     minimize the host and break the next export.
            if (!_host.ClickSaveInDialog()) return false;
            _host.ClickProcessDialogButton("Yes", TimeSpan.FromSeconds(15));
            Thread.Sleep(4000);   // let the report open in its default app
            var reportHwnd = _host.ForegroundReportHwnd();  // foreground window, but only if it's NOT XrmToolBox
            if (reportHwnd != IntPtr.Zero) { _host.MaximizeWindow(reportHwnd); Thread.Sleep(1500); }

            if (ext == "xlsx")
            {
                // Excel workbook has three worksheets — capture each (Ctrl+PageDown between them).
                var sheets = new[] { "summary", "findings", "fixchecklist" };
                for (var s = 0; s < sheets.Length; s++)
                {
                    ShotHwnd($"09-export-xlsx-3-report-{s + 1}-{sheets[s]}", _host.ForegroundHwnd());
                    if (s < sheets.Length - 1) { _host.NextExcelSheet(); Thread.Sleep(1500); }
                }
            }
            else
            {
                ShotHwnd($"09-export-{ext}-3-report", reportHwnd != IntPtr.Zero ? reportHwnd : _host.ForegroundHwnd());
            }

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

        /// <summary>Capture the open connection dialog if there is one (it's XrmToolBox's), else the main window.</summary>
        private void ShotDialogOrMain(string label) => _host.Screenshot(ShotPath(label), _host.DialogHwnd());

        // One folder per DATE (yyyymmdd); multiple runs the same day share it (files are time-stamped).
        private string RunDir() =>
            Path.Combine(ScreenshotRoot(), _dateStamp, "technical-debt-analyzer");

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
