# Solution Knowledge Graph - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 9 Knowledge Graph cases passed, 0 failed, 0 skipped (57 total across the suite).
- **Coverage:** TC-KG-MODEL-01..02 (graph construction/dedup/auto-create), TC-KG-TRACE-03 & TC-KG-IMPACT-04 (forward/reverse transitive reachability), TC-KG-CYCLE-05..06 (Tarjan SCC circular-dependency detection), and TC-KG-EXPORT-07..09 (GraphML, SVG, and self-contained interactive HTML). Traces to US-KG-2..5.

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
| Manual (builder/PNG/interactive/UI) | 7 | 0 | 0 | 0 | 7 |

## Verdict

The SDK-free graph model, algorithms (trace/impact/cycles), and GraphML/SVG/HTML exporters pass, and the
tool builds `Release` with zero warnings across the solution. Manual GUI/Dataverse cases (TC-KG-M-01..07)
are **pending a live org** and must be run before release — no manual case is claimed as passed here.
