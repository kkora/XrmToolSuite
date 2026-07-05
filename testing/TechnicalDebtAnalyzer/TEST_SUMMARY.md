# Technical Debt Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 5 Technical-Debt cases passed, 0 failed, 0 skipped (38 total across the suite).
- **Coverage:** TC-TD-SCORE-01..04 (debt weighting/banding/cap — note debt does not force High on a single Critical, unlike deployment risk) and TC-TD-DASH-05 (per-category dashboard metrics), tracing to US-TD-3 / US-TD-4. Exercises the shared `ScoreCalculator` (custom weights, `criticalForcesHigh:false`) and `ReportModel` projection via `TechDebtReport`.
- **Analyzers (headless):** `dotnet test testing/CollectorTests/CollectorTests.csproj` — TC-TD-COL-01..11 drive the analyzers against the shared fake `IOrganizationService`: plugin performance / dead registration, draft processes, deprecated APIs, duplicate web resources, copied roles (row-driven), plus metadata-driven branches (empty custom tables via aggregate FetchXML, wide tables, publisher prefixes, secured columns) seeded with a reflection `EntityMetadata` builder. 11 passed.

```
Passed! - Failed: 0, Passed: 38, Skipped: 0, Total: 38 (whole suite)
```

## Manual run

Not executed in this environment (headless — no XrmToolBox host or Dataverse connection). The analyzers,
WinForms UI, five exporters, and the AI/offline summary must be exercised in a Windows + XrmToolBox
session against a real org; capture a screenshot per case into `screenshots/`.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated (score/metrics) | 5 | 5 | 5 | 0 | 0 |
| Automated (analyzers, headless) | 11 | 11 | 11 | 0 | 0 |
| Manual (UI/exports/summary) | 9 | 0 | 0 | 0 | 9 |

## Verdict

Automated debt-scoring logic passes and the tool builds `Release` with zero warnings across the solution.
The analyzers are now covered headlessly (TC-TD-COL-01..11, `CollectorTests`); only the WinForms UI,
exporters, and offline/AI summary remain manual. Manual GUI/Dataverse cases (TC-TD-M-01..09) are **pending
a live org** and must be run before release — no manual case is claimed as passed here.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Technical Debt Analyzer** loads and appears in the Tools list.
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Technical Debt Analyzer** v1.2026.7.1 (Kanchan Kora).
