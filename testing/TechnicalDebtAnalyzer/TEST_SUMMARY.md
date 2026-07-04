# Technical Debt Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 5 Technical-Debt cases passed, 0 failed, 0 skipped (38 total across the suite).
- **Coverage:** TC-TD-SCORE-01..04 (debt weighting/banding/cap — note debt does not force High on a single Critical, unlike deployment risk) and TC-TD-DASH-05 (per-category dashboard metrics), tracing to US-TD-3 / US-TD-4. Exercises the shared `ScoreCalculator` (custom weights, `criticalForcesHigh:false`) and `ReportModel` projection via `TechDebtReport`.

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
| Manual (analyzers/UI/exports/summary) | 9 | 0 | 0 | 0 | 9 |

## Verdict

Automated debt-scoring logic passes and the tool builds `Release` with zero warnings across the solution.
Manual GUI/Dataverse cases (TC-TD-M-01..09) are **pending a live org** and must be run before release —
no manual case is claimed as passed here.
