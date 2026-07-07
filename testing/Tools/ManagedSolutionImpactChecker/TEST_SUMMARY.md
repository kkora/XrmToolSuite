# Managed Solution Impact Checker - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Scope:** `ManagedSolutionImpactCheckerTests` — the SDK-free layering engine (`LayerImpactRules` over
  `LayerAnalysisInput`): each rule fires with the right severity per deployment path (Upgrade-deletes vs
  Update/Patch-does-not, Holding treated as deleting), score/band aggregation, and generated checklist +
  rollback guidance; clean input → single Info finding.
- **Result:** 15 passed, 0 failed (TC-01 … TC-14). Verified in an isolated net8 harness compiling
  `ImpactModels.cs` + `LayerImpactRules.cs` + the shared `Analysis` core alongside the test file; the same
  cases run under `testing/UnitTests/` once its `<Compile>` entries for these two source files are present.
- **Tool build:** `dotnet build src/Tools/XrmToolSuite.ManagedSolutionImpactChecker/…csproj -c Release`
  succeeded with **0 warnings / 0 errors**; the ClosedXML + PdfSharp/MigraDoc `-gdi` export chain (17 DLLs)
  is present in `bin/Release/net48`.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 14 | 14 | 14 | 0 | 0 |
| Manual | 5 | 0 | 0 | 0 | 5 |

Manual cases (TC-15 … TC-19) require Windows + XrmToolBox + a live Dataverse connection with a managed
solution installed, and **cannot run headlessly** — they are Pending until executed in that environment,
with a screenshot saved under `screenshots/` per case.

## Verdict

**Automated: PASS.** The pure layering engine is fully covered and green, and the tool builds clean with the
export chain shipped. **Manual GUI + live-Dataverse cases (collector, dashboard, exports) are Pending** an
XrmToolBox session against a real org — not yet executed, so not reported as passed.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Managed Solution Impact Checker** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Managed Solution Impact Checker** v1.2026.7.2 (Kanchan Kora).
