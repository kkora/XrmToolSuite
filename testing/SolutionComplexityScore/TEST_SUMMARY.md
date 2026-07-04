# Solution Complexity Score - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 6 Complexity cases passed, 0 failed, 0 skipped (44 total across the suite).
- **Coverage:** TC-SC-METRIC-01..04 (weighted points, exact score/maintainability/effort/cost values, cap, per-dimension breakdown) and TC-SC-REPORT-05..06 (report projection, band, hotspot findings), tracing to US-SC-3/4/5. The effort/cost formulas are asserted to exact values, so a change to any weight is caught.
- **Collector (headless):** `dotnet test testing/CollectorTests/CollectorTests.csproj` — TC-SC-COL-01..08 drive `ComplexityCollector` against the shared fake `IOrganizationService` (component-type tallies, JScript web resources, dashboards vs forms, widest form, workflow categories, apps, views/charts). 8 passed.

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
| Automated (collector, headless) | 8 | 8 | 8 | 0 | 0 |
| Manual (UI/exports/summary) | 7 | 0 | 0 | 0 | 7 |

## Verdict

The SDK-free complexity/effort model passes with exact-value assertions and the tool builds `Release`
with zero warnings. The component collector is now covered headlessly (TC-SC-COL-01..08, `CollectorTests`);
only the WinForms UI, exporters, and offline/AI summary remain manual. Manual GUI/Dataverse cases
(TC-SC-M-01..07) are **pending a live org** and must be run before release — no manual case is claimed as
passed here.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Solution Complexity Score** loads and appears in the Tools list.
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Solution Complexity Score** v1.2026.7.1 (Kanchan Kora).
