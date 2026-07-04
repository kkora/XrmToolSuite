# testing/UiSmokeTests — Tier-3 UI smoke tests (FlaUI)

Interactive, opt-in end-to-end smoke tests that drive the **real XrmToolBox host** with UI Automation
([FlaUI](https://github.com/FlaUI/FlaUI)) and assert the suite's plugins loaded.

## What it proves (and why it matters)

`AllSuiteTools_AppearInXrmToolBoxToolsList` launches XrmToolBox and checks that every suite tool
(Deployment Risk Analyzer, Technical Debt Analyzer, Solution Complexity Score, AI Solution Reviewer,
Solution Knowledge Graph, Attribute Auditor, FetchXML Performance Analyzer, Environment Inventory,
Privilege Gap Analyzer) appears in the Tools list.

That is exactly the failure mode the headless suites **cannot** see and that CLAUDE.md warns about: a
plugin missing a required `ExportMetadata` key (e.g. `SmallImageBase64`/`BigImageBase64`), or with an
unresolved dependency at scan time, is **silently dropped** from the Tools list — no error, no crash, the
assembly just doesn't appear. This test turns that into a red build.

**No Dataverse connection is required** — XrmToolBox loads and lists plugins at startup, before you connect.

## Why it is separate from `test` / `test-windows`

- It needs a **real, logged-in, unlocked Windows desktop session** — UI Automation cannot drive a headless
  agent or a service session. The GitHub-hosted `windows-latest` runners do not provide one out of the box.
- It needs **XrmToolBox installed** and the **suite DLLs deployed** to its Plugins folder.

So it is **not** wired into CI. Run it deliberately on a dev box, or on a self-hosted Windows runner set up
for interactive UI automation (below). It is also deliberately **not** part of `XrmToolSuite.sln`.

## Running it locally

1. **Build + deploy the tools into XrmToolBox** (copies each tool DLL — and, for the tools that ship them,
   their dependency chain — into `%AppData%\MscrmTools\XrmToolBox\Plugins`):

   ```powershell
   dotnet build XrmToolSuite.sln -c Release -p:DeployToXTB=true
   Get-ChildItem "$env:AppData\MscrmTools\XrmToolBox\Plugins\XrmToolSuite.*.dll" | Unblock-File
   ```

2. **Point the test at your XrmToolBox executable** (or install it to a default location the test probes):

   ```powershell
   $env:XTB_EXE = "C:\path\to\XrmToolBox.exe"
   ```

3. **Run it** (from a normal, interactive PowerShell — not over a locked RDP session):

   ```powershell
   $env:XTB_EXE = "C:\path\to\XrmToolBox.exe"          # your local install
   $env:UISMOKE_SCREENSHOT_DIR = "$PWD\testing\UiSmokeTests\screenshots"   # optional; where the PNG lands
   dotnet test testing/UiSmokeTests/UiSmokeTests.csproj
   ```

   Leave the desktop alone while it runs; UI Automation drives the real mouse/keyboard focus.

   **Screenshot evidence.** The test always captures a PNG of the XrmToolBox window (pass *or* fail) — it
   brings the window to the foreground first, then grabs it. It saves to `UISMOKE_SCREENSHOT_DIR` if set,
   otherwise `%TEMP%\xtb-ui-smoke`, and prints the full path (`[ui-smoke] Screenshot saved: …`). Files are
   named `xrmtoolbox-tools_<found>of<total>_<timestamp>.png`, so a failure screenshot shows exactly which
   tools were missing from the list. The images are regenerated each run and are git-ignored.

## Setting it up on a self-hosted CI runner

The workflow already exists: **[`.github/workflows/ui-smoke.yml`](../../.github/workflows/ui-smoke.yml)**
(`ui-smoke` job, `runs-on: [self-hosted, windows]`). It builds + deploys the tools, then runs this test.
It is **manual only** (`workflow_dispatch`) — an interactive GUI test should not fire on every push. Trigger
it from the Actions tab (or `gh workflow run "UI smoke (self-hosted)"`).

To make it runnable you need a Windows machine that keeps an **interactive, unlocked** session alive:

1. **Register a self-hosted runner** on the repo (Settings → Actions → Runners → New self-hosted runner).
   Install it as a **per-user process you start from a logged-in desktop** (`.\run.cmd`) — **not** as a
   Windows service. A service runs in session 0 (non-interactive) and UI Automation will fail there. The
   default runner labels (`self-hosted`, `Windows`) satisfy `runs-on: [self-hosted, windows]`.
2. **Keep the session unlocked** — enable autologon and disable the lock screen / screen saver (a small
   "keep-awake" utility helps). UI Automation cannot drive a locked session.
3. **XrmToolBox itself needs nothing pre-installed** — the workflow **downloads the official XrmToolBox
   release that matches the version the tools build against** (the `XrmToolBoxPackage` version in
   `src/Tools/Directory.Build.props`) from GitHub, so the runner stays self-contained. To use a preinstalled
   copy instead (e.g. to skip the download), set a **repo variable** `XTB_EXE` (Settings → Secrets and
   variables → Actions → Variables) to the full `XrmToolBox.exe` path, or pass `xtb_exe` on dispatch. The
   build+deploy step puts the tool DLLs into the Plugins folder either way.

   > First-run note: a freshly downloaded XrmToolBox may show a one-time "what's new" dialog on top of the
   > main window, which can trip the automation. If the CI run is flaky on first use, pre-seed the runner's
   > `%AppData%\MscrmTools\XrmToolBox` (or run XrmToolBox once manually there) so subsequent runs skip it.

Keep it **advisory** — do NOT add `ui-smoke` to `main`'s required checks. UI tests are inherently flakier
than the headless tiers, and a single smoke test ("do the tools load") is the right scope, not full UI
coverage. The required gate stays `test` + `test-windows`.

## Status & caveats

- **Executed and green.** Run against a real XrmToolBox (portable install at `C:\devtools\XrmToolbox`) it
  finds **6/6** suite tools in the Tools list and passes. XrmToolBox's tool tiles **do** expose their display
  name to UI Automation, so the simple `Name`-match in `XrmToolBoxSmokeTest.VisibleNames` is sufficient — no
  tool-search / AutomationId workaround was needed.
- **Self-relaunch handled.** XrmToolBox bootstraps and relaunches itself at startup, abandoning the PID we
  launched, so the harness attaches to the live `XrmToolBox` process **by name** (`WaitForXtbProcess` →
  `Application.Attach`) rather than the launched handle. Launching the FlaUI handle directly fails with
  "process … not running".
- **Don't use the NuGet-packaged host.** The `XrmToolBox.exe` under
  `~\.nuget\packages\xrmtoolboxpackage\<ver>\lib\net48\` crashes on standalone launch (exit `0xE0434352`);
  use a real portable/installed XrmToolBox and point `XTB_EXE` at it.
- UI automation is timing-sensitive. The test polls for up to ~45s after the window appears to allow the
  plugin scan to finish before asserting, and `Dispose` kills any XrmToolBox process it started.
