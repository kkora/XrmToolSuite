# Environment Comparison Suite - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Result:** **Pass — 307 passed, 0 failed** for the whole suite; the 14 Environment Comparison Suite
  cases (`EnvironmentComparisonSuiteTests.cs`) are included and all pass.
- **Scope:** SDK-free diff engine only (`Analysis/ComparisonModels.cs` + `Analysis/SnapshotComparer.cs`) —
  classification, changed-property diffing, severity assignment, score/band roll-up, count matrix, and
  secret masking. The Dataverse collector (`Analysis/ComparisonCollector.cs`) and the WinForms host are
  SDK/UI-bound and are covered by the manual cases.
- **Note for maintainers:** the two SDK-free engine files must be added to `testing/UnitTests/UnitTests.csproj`
  for the automated cases to compile there (see the exact `<Compile Include=.../>` lines in the tool README /
  implementation notes). Verified locally by temporarily adding them and running `dotnet test` (307/307).

## Build

- **Command:** `dotnet build src/Tools/XrmToolSuite.EnvironmentComparisonSuite/XrmToolSuite.EnvironmentComparisonSuite.csproj -c Release`
- **Result:** **Build succeeded — 0 Warning(s), 0 Error(s).**
- **Export chain:** `ClosedXML.dll` and the `-gdi` PdfSharp/MigraDoc assemblies confirmed present in
  `bin/Release/net48`.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 14 | 14 | 14 | 0 | 0 |
| Manual | 12 | 0 | 0 | 0 | 12 |

The manual cases (TC-M1…TC-M12) require a Windows + XrmToolBox host with **two** live Dataverse
connections and cannot run headlessly in this environment. They are documented in `TEST_CASES.md` and
remain **Pending** — to be executed in an XrmToolBox session with a screenshot captured per case under
`screenshots/`.

## Verdict

**Automated: Pass** (SDK-free diff engine fully green, build clean, export chain present).
**Manual: Pending** — the live dual-connection collection, UI, secret masking in the grid/exports, and
read-only guarantee must be verified in an XrmToolBox session before the tool is declared fully done.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Environment Comparison Suite** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Environment Comparison Suite** v1.2026.7.2 (Kanchan Kora).
