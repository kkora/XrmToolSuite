# Form Performance Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj --filter FullyQualifiedName~FormPerformanceAnalyzerTests`
- **Result:** 14 passed, 0 failed (the 10 test cases below expand into 14 xUnit facts/theory rows — the
  blank/malformed theory contributes 5 rows).
- **Note:** the three SDK-free source files (`FormXmlParser.cs`, `FormModels.cs`, `FormScorer.cs`) must be
  added to `testing/UnitTests/UnitTests.csproj` as `<Compile Include=.../>` entries for the run:

  ```xml
  <Compile Include="..\..\src\Tools\XrmToolSuite.FormPerformanceAnalyzer\Analysis\FormXmlParser.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.FormPerformanceAnalyzer\Analysis\FormModels.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.FormPerformanceAnalyzer\Analysis\FormScorer.cs" />
  ```

  The shared `Analysis\*.cs` glob (Finding/Severity/ScoreCalculator/ReportModel/MetricRow) is already
  referenced by that csproj. The SDK collector (`FormCollector.cs`) is **not** included — it needs Dataverse
  and is manual-tested. The run above was verified with an equivalent standalone project.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 10 (14 facts) | 10 | 10 | 0 | 0 |
| Manual | 10 (TC-11..TC-20) | 0 | 0 | 0 | 10 |

## Verdict

Automated SDK-free logic (FormXML parsing of tabs/sections/fields/hidden/controls/subgrids/quick-views/
handlers, blank/malformed degradation, deterministic scoring, band thresholds, recommendation triggers, and
ranking) **passes** — 14/14. The tool builds in Release with **0 warnings / 0 errors**, and only
`XrmToolSuite.FormPerformanceAnalyzer.dll` lands in `bin/Release/net48/` (single-DLL — no ClosedXML/PdfSharp
dependency chain, matching the nuspec).

Manual/live cases (tool load, table-scope picker, batch form + business-rule collection, ranked grid + band
summary, metric-breakdown and recommendations panes, two-form compare, scoring-settings round-trip, and the
CSV/HTML export dialogs) are **Pending** — they require a Windows + XrmToolBox session against a real Dataverse
org and cannot run headlessly. They must be executed and evidenced (screenshots under `screenshots/`, plus the
required `xrmtoolbox-tools-list.png`) before the tool is declared fully verified. No manual case is claimed as
passed.
