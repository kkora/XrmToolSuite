# Deployment Risk Analyzer — UI End-to-End Test Spec (Tier-3c)

Full happy-path UI automation of the Deployment Risk Analyzer against a **live** connected XrmToolBox session,
capturing a screenshot after every step. Automated by
[`DeploymentRiskAnalyzerE2ETest`](../../UiSmokeTests/DeploymentRiskAnalyzerE2ETest.cs) (FlaUI), driven through the
[`XtbHost`](../../UiSmokeTests/Pages/XtbHost.cs) page object.

Traces to: `docs/user-stories/ALM07.DeploymentRiskAnalyzer.md`. Complements the headless analyzer tests
(`testing/AnalyzerTests`) and the load smoke test (`testing/UiSmokeTests/XrmToolBoxSmokeTest`).

## Scope & constraints

- **Local, opt-in, not in CI.** Needs a real, **unlocked** Windows desktop and a live Dataverse connection.
  The suite's `XTS-CI-*` connections are **interactive** (OnlineFederation/AD), so a human must connect first;
  the test **attaches** to that session (`XTB_ATTACH` semantics) and never closes it. Gated behind `XTB_E2E=1`.
- **Never runs against production** — the test refuses any source/target connection whose name contains `prod`.
- A locked desktop is invisible to UI Automation. Keep the session active for the ~1–3 min run.

## Preconditions

| # | Precondition |
|---|---|
| P1 | Tools deployed to XrmToolBox Plugins (`dotnet build XrmToolSuite.sln -c Release -p:DeployToXTB=true`, then `Unblock-File`). |
| P2 | XrmToolBox running and connected **by hand** to the SOURCE org (`XTS-CI-TEST` → DAS-BITS-DGOE-QA). |
| P3 | A second connection available to pick as the TARGET env (`XTS-CI-DEV`). |
| P4 | Desktop unlocked; screensaver/lock disabled or the operator keeps it active. |

## Steps & assertions

`FullOperatorWalkthrough` drives the full operator script. Each step saves a PNG under a **one-folder-per-day**
`screenshots/<yyyymmdd>/deployment-risk-analyzer/`, named `NN-step-<hhmmss>.png` (the time suffix means same-day
re-runs never overwrite). Tool steps capture the **XrmToolBox window only**; dialog/report steps capture that
specific window. Each **export** produces three sub-shots: `…-1-menu` (Export dropdown), `…-2-savedialog` (Save
As on **Downloads**), `…-3-report` (the opened report in its app).

| # | Action | Assertion | Screenshot |
|---|---|---|---|
| 1 | **Launch** XrmToolBox if not open; close the online Tool Library tab. | Main window appears. | `01-launched` |
| 2 | **Connect to DEV** — if not connected, open the selector and double-click `XTS-CI-DEV`, then OK. | Status bar `Connected to …(DAS-BITS-DGOE-DEV)`. | `02-connect-dev` |
| 3 | Find **Deployment Risk Analyzer** and **double-click** to open. | The tool's `Load solutions` toolbar appears. | `03-tool-open` |
| 3b | **Validation guard** — click **Analyze** with no solution loaded. | "Load and select a solution first" dialog appears. | `03b-guard-no-solution` |
| 4 | Click **Load solutions**. | Dropdown populates and auto-selects the first entry. | `04-solutions-loaded` |
| 5 | Use the **first solution**. | A non-empty solution is selected. | `05-solution-selected` |
| 6 | Click **Connect target env…**. | Connection selector opens. | `06-target-dialog` |
| 7 | Select **XTS-CI-TEST** and **double-click** it. | **Target:** label → `Target: XTS-CI-TEST`. | `07-target-connected` |
| 7b | **Dual-connection** — connecting the target must not drop the source. | Source connection still live. | — |
| 8 | Click **▶ Analyze**. | Analysis runs; findings grid populates. | `08-analysis-complete` |
| 8b | **Risk score / band** shown in the summary banner. | Banner text contains the score/band. | `08b-risk-band` |
| 8c | **Select a finding** row. | Detail pane shows the recommendation. | `08c-finding-detail` |
| 8d | **Toggle an analyzer** in the left checklist (then restore). | The checklist item flips. | `08d-analyzer-toggled` |
| 8e | Click **AI summary**. | Offline summary dialog opens (no key configured). | `08e-ai-summary` |
| 8f | **AI options → Set API key…**. | The Set-API-key dialog opens (then Cancel). | `08f-ai-set-key` |
| 8g | **Validation guard** — uncheck all analyzers, click **Analyze**. | "Select at least one analyzer" dialog appears. | `08g-guard-no-analyzers` |
| 9 | For each **Export** format (PDF, HTML, Excel, JSON, Fix-checklist): Export menu → **Save dialog** → save to the screenshots folder → **Yes** to open. | A new `TR-XXX-DeploymentRiskAnalyzer_*.ext` file appears. | `09-export-<ext>-1-menu` / `-2-savedialog` / `-3-report` (Excel: 3 sheets) |
| 10 | Click the **Help** button. | The **Help & Support** dialog opens. | `10-help` |
| 11 | Click the tool's **Close** button. | The tool tab tears down. | `11-tool-closed` |

Every file is prefixed with a per-run **round tag** `TR-XXX` (auto-incremented from the folder), so screenshots
and exports from different runs never collide: `TR-002-08b-risk-band-141530.png`.

> **Solution = "first".** The Solution dropdown is UIA-virtualized (86 items; enumeration returns empty) and
> `Load solutions` already auto-selects the first entry, so the test uses that rather than driving the dropdown
> (a left-open dropdown blocks every later click). To target a specific solution, extend `SelectSolutionByName`
> (keyboard type-ahead matches the start of the **display** name only).

## What makes this reliable (hard-won)

- **Screenshots are XrmToolBox-window-only** via Win32 `PrintWindow` ([`WindowCapture`](../../UiSmokeTests/Pages/WindowCapture.cs)) —
  it renders the window's own pixels even when occluded by the IDE or opened reports, so no shot ever shows the
  desktop/editor. Dialogs (connection selector, Save As, Help) are captured by their own HWND, found via
  `EnumWindows` (FlaUI's `GetAllTopLevelWindows` throws `COMException` on this host).
- **UIA cache poisoning.** Heavy WinForms tree mutations (connect/metadata-load, the 86-item dropdown, opening a
  modal) make every UIA query throw `COMException`. `XtbHost.HardReset()` rebuilds the automation + re-attaches;
  it's called before each major step and clicks retry through it.
- **The connection selector is a child of the main window** (not top-level) — driven by searching the main tree
  for the `XTS-CI-*` tile + `OK`.
- **Foreground stealing.** Each export's **Yes** opens the report in an external app that grabs foreground;
  `ForceForeground()` (Minimize→Maximize) raises XrmToolBox before the next toolbar click. Safe because captures
  use PrintWindow, not a screen grab.
- **The Help button opens a modal**, so its UIA `Invoke` can report false mid-handshake — success is verified by
  the Help dialog appearing, not the click's return.

## Running it

```powershell
# Deploy (XrmToolBox CLOSED so the DLLs aren't locked)
dotnet build XrmToolSuite.sln -c Release -p:DeployToXTB=true
Get-ChildItem "$env:APPDATA\MscrmTools\XrmToolBox\Plugins\XrmToolSuite.*.dll" | Unblock-File

# Run the E2E walkthrough (desktop unlocked; DEV token warm from a prior interactive connect)
$env:XTB_E2E = "1"
$env:XTB_EXE = "C:\devtools\XrmToolbox\XrmToolBox.exe"
$env:XTB_SOURCE = "XTS-CI-DEV"     # never prod
$env:XTB_TARGET = "XTS-CI-TEST"
$env:UISMOKE_SCREENSHOT_DIR = "$PWD\testing\UiSmokeTests\screenshots"
dotnet test testing/UiSmokeTests/UiSmokeTests.csproj --filter "FullyQualifiedName~FullOperatorWalkthrough"
```

Screenshots: `testing/UiSmokeTests/screenshots/<timestamp>/deployment-risk-analyzer/`.

## Notes / constraints

- **Interactive auth is un-automatable**, so DEV's token must be warm (connect it once by hand in the same
  Windows profile). The desktop must stay **unlocked** — a lock screen is invisible to UI Automation.
- **Never runs against production** — the test refuses any source/target whose name contains `prod`.
- Grid-row detection is best-effort (UIA `DataItem`); 0 detected is logged, not failed (the analysis and
  exports still succeed).
