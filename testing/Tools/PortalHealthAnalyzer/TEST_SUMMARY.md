# Portal Health Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Result:** 14 passed, 0 failed (the `PortalHealthAnalyzerTests` SDK-free engine tests). Validated on
  .NET 8 by compiling `PortalModels.cs` + `PortalHealthRules.cs` + the shared `Core/Analysis` files against
  the test file — `Passed! - Failed: 0, Passed: 14`.
- **Prerequisite:** `UnitTests.csproj` must reference the two SDK-free source files under test. Add these two
  lines to its `<ItemGroup>` (SDK-free only — never the collector/control):

  ```xml
  <Compile Include="..\..\src\Tools\XrmToolSuite.PortalHealthAnalyzer\Analysis\PortalModels.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.PortalHealthAnalyzer\Analysis\PortalHealthRules.cs" />
  ```

  Until these are present the shared `UnitTests` project will not compile the new `PortalHealthAnalyzerTests`
  (the test references types in those files). `PortalCollector.cs` is deliberately excluded — it depends on
  `Microsoft.Xrm.Sdk` and is manual-tested.

## Manual run

Manual/live cases (TC-15 … TC-19) require a Windows + XrmToolBox session connected to a Dataverse
environment with a provisioned Power Pages website. They cannot run headlessly and are **Pending**.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 14 | 14 | 14 | 0 | 0 |
| Manual | 5 | 0 | 0 | 0 | 5 |

## Build

- `dotnet build src/Tools/XrmToolSuite.PortalHealthAnalyzer/XrmToolSuite.PortalHealthAnalyzer.csproj -c Release`
  → **Build succeeded, 0 warnings, 0 errors.**
- Export dependency chain confirmed in `bin/Release/net48/`: `ClosedXML.dll` and the five `-gdi`
  PdfSharp/MigraDoc assemblies plus their 11 supporting DLLs (17 total).

## Verdict

The SDK-free health-scoring engine is complete and fully covered by passing automated tests. The tool builds
cleanly with its Excel/PDF/Word export chain in place. Live GUI + Dataverse + export cases remain Pending a
manual Windows/XrmToolBox session against a real Power Pages site.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Portal Health Analyzer** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Portal Health Analyzer** v1.2026.7.2 (Kanchan Kora).
