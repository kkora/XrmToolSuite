# Technical Debt Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 13 Technical-Debt cases passed, 0 failed, 0 skipped (410 total across the suite).
- **Coverage:** TC-SOLN10-SCORE-01..04 (debt weighting/banding/cap — note debt does not force High on a single Critical, unlike deployment risk), TC-SOLN10-DASH-05 (per-category dashboard metrics), and TC-SOLN10-TREND-01..08 (RPT04 debt-trends store + analytics: append / per-env cap / same-run dedupe / per-env isolation; run-over-run delta, direction, series, best/worst), tracing to US-SOLN10-3 / US-SOLN10-4 / US-SOLN10-8. Exercises the shared `ScoreCalculator` (custom weights, `criticalForcesHigh:false`), the `ReportModel` projection via `TechDebtReport`, and the SDK-free `TrendStore`/`TrendAnalytics`.
- **Analyzers (headless):** `dotnet test testing/CollectorTests/CollectorTests.csproj` — TC-SOLN10-COL-01..11 drive the analyzers against the shared fake `IOrganizationService`: plugin performance / dead registration, draft processes, deprecated APIs, duplicate web resources, copied roles (row-driven), plus metadata-driven branches (empty custom tables via aggregate FetchXML, wide tables, publisher prefixes, secured columns) seeded with a reflection `EntityMetadata` builder. 11 passed.

```
Passed! - Failed: 0, Passed: 410, Skipped: 0, Total: 410 (whole suite)
```

## Manual run

Not executed in this environment (headless — no XrmToolBox host or Dataverse connection). The analyzers,
WinForms UI, five exporters, and the AI/offline summary must be exercised in a Windows + XrmToolBox
session against a real org; capture a screenshot per case into `screenshots/`.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated (score/metrics) | 5 | 5 | 5 | 0 | 0 |
| Automated (trends store/analytics) | 8 | 8 | 8 | 0 | 0 |
| Automated (analyzers, headless) | 11 | 11 | 11 | 0 | 0 |
| Manual (UI/exports/summary/trends) | 12 | 0 | 0 | 0 | 12 |

## Verdict

Automated debt-scoring and trend-store/analytics logic passes and the tool builds `Release` with zero
warnings across the solution. The analyzers are now covered headlessly (TC-SOLN10-COL-01..11, `CollectorTests`);
only the WinForms UI, exporters, the Trends tab/chart/live-capture, and offline/AI summary remain manual.
Manual GUI/Dataverse cases (TC-SOLN10-M-01..12) are **pending a live org** and must be run before release — no
manual case is claimed as passed here.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Technical Debt Analyzer** loads and appears in the Tools list.
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Technical Debt Analyzer** v1.2026.7.1 (Kanchan Kora).
