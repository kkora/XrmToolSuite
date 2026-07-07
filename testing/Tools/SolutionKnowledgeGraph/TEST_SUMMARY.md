# Solution Knowledge Graph - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 9 Knowledge Graph cases passed, 0 failed, 0 skipped (57 total across the suite).
- **Coverage:** TC-SOLN09-MODEL-01..02 (graph construction/dedup/auto-create), TC-SOLN09-TRACE-03 & TC-SOLN09-IMPACT-04 (forward/reverse transitive reachability), TC-SOLN09-CYCLE-05..06 (Tarjan SCC circular-dependency detection), and TC-SOLN09-EXPORT-07..09 (GraphML, SVG, and self-contained interactive HTML). Traces to US-SOLN09-2..5.
- **Builder (headless):** `dotnet test testing/CollectorTests/CollectorTests.csproj` — TC-SOLN09-COL-01..09 drive `GraphBuilder` against the shared fake `IOrganizationService`: typed-node naming, metadata-named tables, fail-soft fallback labels, dependency edges (within / into / outside the solution), and empty/empty-object-id guards. 9 passed. PNG export stays manual (System.Drawing).

```
Passed! - Failed: 0, Passed: 57, Skipped: 0, Total: 57 (whole suite)
```

## Manual run

Not executed in this environment (headless — no XrmToolBox host or Dataverse connection). The Dataverse
graph builder, PNG export, the interactive browser view, and the WinForms UI (search/filter/trace/impact/
cycles) must be exercised in a Windows + XrmToolBox session; capture a screenshot per case.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated (model/algorithms/export) | 9 | 9 | 9 | 0 | 0 |
| Automated (builder, headless) | 9 | 9 | 9 | 0 | 0 |
| Manual (PNG/interactive/UI) | 7 | 0 | 0 | 0 | 7 |

## Verdict

The SDK-free graph model, algorithms (trace/impact/cycles), and GraphML/SVG/HTML exporters pass, and the
tool builds `Release` with zero warnings across the solution. The Dataverse graph builder is now covered
headlessly (TC-SOLN09-COL-01..09, `CollectorTests`); only PNG export and the interactive/WinForms UI remain
manual. Manual GUI/Dataverse cases (TC-SOLN09-M-01..07) are **pending a live org** and must be run before
release — no manual case is claimed as passed here.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Solution Knowledge Graph** loads and appears in the Tools list.
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Solution Knowledge Graph** v1.2026.7.1 (Kanchan Kora).
