# Component Usage Explorer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj` (SDK-free rules in
  `ComponentUsageExplorerTests.cs`, run against `UsageModels.cs` + `UsageVerdictRules.cs`).
- **Result:** 13 passed, 0 failed, 0 skipped (verified via an isolated probe project compiling the same
  source files, since the tools target net48/WinForms and the test project targets net8.0). The two
  `<Compile Include=.../>` lines for `UsageModels.cs` and `UsageVerdictRules.cs` must be added to
  `testing/UnitTests/UnitTests.csproj` for the case to run in the shared suite test project.
- **Coverage:** every verdict (SafeToChange, ChangeWithCaution, HighImpact, DoNotDelete,
  RequiresAlmReview, RequiresDependencyReview), score/band ordering, incomplete-data handling, the
  usage-by-type tally, the dependent-count metric, and the explanation text.

## Build

- **Command:** `dotnet build src/Tools/XrmToolSuite.ComponentUsageExplorer/XrmToolSuite.ComponentUsageExplorer.csproj -c Release`
- **Result:** Build succeeded — 0 warnings, 0 errors. ClosedXML + the PdfSharp/MigraDoc `-gdi` chain
  land in `bin/Release/net48` as expected.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 13 | 13 | 13 | 0 | 0 |
| Manual | 6 | 0 | 0 | 0 | 6 |

The 6 manual cases (TC-M1..TC-M6) require a live Dataverse connection and the WinForms host and **cannot
run headlessly** — they are documented in `TEST_CASES.md` and remain Pending until executed in a
Windows + XrmToolBox session, with a screenshot per case saved under `screenshots/`.

## Verdict

SDK-free change-safety logic is fully covered and passing; the tool builds clean in Release with its
export chain. Manual GUI/Dataverse cases are pending execution against a real org.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Component Usage Explorer** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Component Usage Explorer** v1.2026.7.2 (Kanchan Kora).
