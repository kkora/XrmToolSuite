# Solution Merge Assistant - Test Plan

Traces to [`docs/user-stories/ALM01.SolutionMergeAssistant.md`](../../../docs/user-stories/ALM01.SolutionMergeAssistant.md).

## Scope

Solution Merge Assistant does a **read-only** comparison of two or more solutions from one environment and
produces a pre-merge verdict, a recommended merge strategy, and a merged-component checklist. These tests
verify (a) the SDK-free comparison engine that classifies conflicts and rolls them into a verdict, and (b) the
WinForms tool that loads solutions, runs the comparison against a live org, and exports the report.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free comparison engine (`MergeRules.Compare`, `MergeModels`) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Solution/component loading, verdict UI, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

The comparison logic (overlap keying, severity grading, publisher/version/managed-state/config conflicts,
verdict selection, strategy/checklist) is pure and deterministic, so it is fully covered by automated tests.
Anything needing Dataverse (`MergeCollector`) or the WinForms host (grid, verdict banner, Excel/PDF/JSON/HTML
export) is manual and cannot run headlessly.

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher), an environment with
  at least two solutions that share components / environment variables / connection references.

## Entry / exit criteria

- **Entry:** tool builds in Release (`dotnet build src/Tools/XrmToolSuite.SolutionMergeAssistant/... -c Release`
  → 0 warnings / 0 errors) and the automated tests are wired into `testing/UnitTests`.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.

## Notes

- The engine's SDK-free source files (`Analysis/MergeModels.cs`, `Analysis/MergeRules.cs`) must be present as
  `<Compile Include=.../>` lines in `testing/UnitTests/UnitTests.csproj` for the automated suite to run them
  (they compile with no Dataverse/WinForms dependency).
