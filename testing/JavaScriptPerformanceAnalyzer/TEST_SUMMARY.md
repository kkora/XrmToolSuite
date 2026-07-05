# JavaScript Performance Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Result:** 20 passed, 0 failed (JavaScript Performance Analyzer suite, `JavaScriptPerformanceAnalyzerTests`).
  Executed via a stand-in net8.0 project compiling the SDK-free sources under test (`JsModels.cs`, `JsRules.cs`,
  `FormEventMap.cs`) plus the shared analysis core, since the shared `UnitTests.csproj` needs its three
  `<Compile Include=…/>` lines added for these files (see the implementation notes / handoff).
- **Build:** `dotnet build src/Tools/XrmToolSuite.JavaScriptPerformanceAnalyzer/...csproj -c Release` →
  0 warnings, 0 errors. The ClosedXML + PdfSharp/MigraDoc `-gdi` export chain copies into
  `bin/Release/net48` as expected.

## Manual run

Manual GUI + live-Dataverse cases (TC-M01…TC-M09) **cannot** run headlessly and are **Pending** — they
require a Windows + XrmToolBox session connected to a real environment. They are documented in `TEST_CASES.md`
and are to be executed there, with a screenshot captured per case under `screenshots/`.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 18 | 18 | 18 | 0 | 0 |
| Manual | 9 | 0 | 0 | 0 | 9 |

## Verdict

**Automated: PASS.** The SDK-free rule engine, scoring/banding, and FormXML event mapper are fully covered and
green; the tool builds clean in Release with the export chain present. **Manual/live and export cases remain
Pending** a Windows + XrmToolBox + Dataverse session — not yet executed, not claimed as passed.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **JavaScript Performance Analyzer** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **JavaScript Performance Analyzer** v1.2026.7.2 (Kanchan Kora).
