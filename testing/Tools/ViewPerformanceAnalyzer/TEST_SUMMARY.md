# View Performance Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj --filter FullyQualifiedName~ViewPerformanceAnalyzerTests`
- **Result:** 11 passed, 0 failed (the 8 test cases below expand into 11 xUnit facts/theory rows).
- **Note:** the three SDK-free source files (`LayoutXmlParser.cs`, `ViewModels.cs`, `ViewScorer.cs`) must be
  added to `testing/UnitTests/UnitTests.csproj` as `<Compile Include=.../>` entries for the run — see the
  handoff note in the PR/commit. The FetchXML engine glob is already referenced by that csproj.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 8 (11 facts) | 8 | 8 | 0 | 0 |
| Manual | 10 (TC-09..TC-18) | 0 | 0 | 0 | 10 |

## Verdict

Automated SDK-free logic (LayoutXML parsing, per-view scoring incl. shared-engine reuse, over-wide-layout
rule, parse-failure degradation, ranking) **passes**. The tool builds in Release with 0 warnings / 0 errors,
and all 17 Excel/PDF export dependency DLLs land in `bin/Release/net48/`.

Manual/live cases (table picker, batch collection over `savedquery`/`userquery`, ranked grid + score cards,
detail panels, opt-in timing, settings round-trip, and the Excel/PDF/JSON/HTML/Markdown/CSV export dialogs)
are **Pending** — they require a Windows + XrmToolBox session against a real Dataverse org and cannot run
headlessly. They must be executed and evidenced (screenshots under `screenshots/`, plus the required
`xrmtoolbox-tools-list.png`) before the tool is declared fully verified. No manual case is claimed as passed.
