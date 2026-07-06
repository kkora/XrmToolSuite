# Deployment Risk Analyzer — UI End-to-End Test Spec (Tier-3c)

Full happy-path UI automation of the Deployment Risk Analyzer against a **live** connected XrmToolBox session,
capturing a screenshot after every step. Automated by
[`DeploymentRiskAnalyzerE2ETest`](../UiSmokeTests/DeploymentRiskAnalyzerE2ETest.cs) (FlaUI), driven through the
[`XtbHost`](../UiSmokeTests/Pages/XtbHost.cs) page object.

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

Each step captures `NN-step.png` under `screenshots/<yyyyMMdd-HHmmss>/deployment-risk-analyzer/`.

| Step | Action | Assertion | Screenshot |
|---|---|---|---|
| 1 | Open the tool (reuse an already-open connected tab if present). | Tab caption shows a connection suffix `Deployment Risk Analyzer (<conn>)` — i.e. opened **connected**. | `00-tool-opened.png` |
| 2 | Click **Load solutions**. | Solutions dropdown populates and auto-selects the first entry (non-empty combo value) within 60 s. | `01-solutions-loaded.png` |
| 3 | Click **Connect target env…**, then pick a connection in the selector dialog. | The **Target:** toolbar label leaves `(none)` and shows `Target: <name>` (green). | `02-target-connected.png`, `03-target-label.png` |
| 4 | Explicitly select the **first solution** in the dropdown. | Combo value equals the first solution entry. | `04-first-solution-selected.png` |
| 5 | Click **▶ Analyze**. | Results grid populates with finding rows within 120 s (a genuinely clean solution may legitimately yield 0 — the shot records the final state either way). | `05-analysis-complete.png` |

> Steps map to the tool's toolbar order: `Load solutions · Solution: [combo] · Connect target env… · Target: · ▶ Analyze`.
> The **Connect target env…** dialog is XrmToolBox's own connection selector (raised via `RaiseRequestConnectionEvent`,
> `actionName="TargetOrganization"`); `XtbHost.PickConnectionInSelector` filters to the named connection and
> double-clicks it. If a future XrmToolBox changes that dialog's layout, shot `02` shows its state for re-tuning.

## Running it

```powershell
# P1 — deploy (XrmToolBox CLOSED so the DLLs aren't locked), then reopen + connect the SOURCE org by hand
dotnet build XrmToolSuite.sln -c Release -p:DeployToXTB=true
Get-ChildItem "$env:APPDATA\MscrmTools\XrmToolBox\Plugins\XrmToolSuite.*.dll" | Unblock-File

# run the E2E walkthrough (attach mode; desktop unlocked)
$env:XTB_E2E                = "1"
$env:XTB_TEST_CONNECTION    = "XTS-CI-DEV"     # SOURCE (already connected by hand); never prod
$env:XTB_TARGET_CONNECTION  = "XTS-CI-TEST"      # TARGET to pick in the dialog ("" = pick first offered)
$env:UISMOKE_SCREENSHOT_DIR = "$PWD\testing\UiSmokeTests\screenshots"
dotnet test testing/UiSmokeTests/UiSmokeTests.csproj --filter "FullyQualifiedName~DeploymentRiskAnalyzerE2ETest"
```

Screenshots: `testing/UiSmokeTests/screenshots/<timestamp>/deployment-risk-analyzer/`.

## Notes / known fragilities

- **Interactive auth is un-automatable**, so the source connection is warmed by a human once; the test never
  authenticates. This is by design (see README Tier-3b).
- **The connection-selector dialog is the most layout-sensitive step.** It's driven best-effort with a
  filter-then-double-click and a button fallback (`Load/Open/Connect/Select/OK`). If it fails to drive, the
  test fails at the **Target:** assertion and shot `02` captures the open dialog for tuning.
- Grid-row detection uses UIA `DataItem` count; if a solution produces zero findings the step logs that rather
  than failing (zero findings is a valid analyzer outcome).
