# Solution Complexity Score - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 6 Complexity cases passed, 0 failed, 0 skipped (44 total across the suite).
- **Coverage:** TC-SC-METRIC-01..04 (weighted points, exact score/maintainability/effort/cost values, cap, per-dimension breakdown) and TC-SC-REPORT-05..06 (report projection, band, hotspot findings), tracing to US-SC-3/4/5. The effort/cost formulas are asserted to exact values, so a change to any weight is caught.

```
Passed! - Failed: 0, Passed: 44, Skipped: 0, Total: 44 (whole suite)
```

## Manual run

Not executed in this environment (headless — no XrmToolBox host or Dataverse connection). The solution
picker, component collector, WinForms dashboard, five exporters, and the AI/offline summary must be
exercised in a Windows + XrmToolBox session against a real org; capture a screenshot per case.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated (metrics/report) | 6 | 6 | 6 | 0 | 0 |
| Manual (collector/UI/exports/summary) | 7 | 0 | 0 | 0 | 7 |

## Verdict

The SDK-free complexity/effort model passes with exact-value assertions and the tool builds `Release`
with zero warnings. Manual GUI/Dataverse cases (TC-SC-M-01..07) are **pending a live org** and must be
run before release — no manual case is claimed as passed here.
