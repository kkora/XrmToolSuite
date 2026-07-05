# Solution Merge Assistant - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Result:** 14 passed, 0 failed (`SolutionMergeAssistantTests`) — verified in an isolated net8 project
  that compiles `Analysis/MergeModels.cs`, `Analysis/MergeRules.cs`, and the shared `Core/Analysis` engine
  alongside the test file.
- **Wiring note:** `testing/UnitTests/UnitTests.csproj` must include the two SDK-free source files under test
  for the suite run to pick these up:
  ```xml
  <Compile Include="..\..\src\Tools\XrmToolSuite.SolutionMergeAssistant\Analysis\MergeModels.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.SolutionMergeAssistant\Analysis\MergeRules.cs" />
  ```
  (These compile with no Dataverse/WinForms dependency. `MergeCollector.cs` is SDK-bound and is manual-tested.)

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 14 | 14 | 14 | 0 | 0 |
| Manual | 10 | 0 | 0 | 0 | 10 |

Manual cases (TC-M1..TC-M10) require Windows + XrmToolBox + a live Dataverse connection and cannot run
headlessly in this environment; they are **Pending** execution in an interactive session, with a screenshot
to be captured per case under `screenshots/`.

## Build

- `dotnet build src/Tools/XrmToolSuite.SolutionMergeAssistant/XrmToolSuite.SolutionMergeAssistant.csproj -c Release`
  → **0 warnings, 0 errors**. The Excel (ClosedXML) and native-PDF (PdfSharp/MigraDoc `-gdi`) dependency
  chains are present in `bin/Release/net48`.

## Verdict

Automated comparison-engine coverage is **complete and passing** (14/14). The tool builds cleanly with its
export chain. Live-org and export UI cases remain **Pending** manual execution.
