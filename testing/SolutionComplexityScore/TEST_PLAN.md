# Solution Complexity Score - Test Plan

Traces to [`docs/user-stories/SolutionComplexityScore.md`](../../docs/user-stories/SolutionComplexityScore.md).

## Scope

Validate the Solution Complexity Score end to end: the SDK-free metric/effort model and report
projection (automated), and the Dataverse component collector, WinForms dashboard, five exporters, and
the offline/AI executive summary (manual, against a live org).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | Complexity/maintainability score, effort/cost estimates, hotspot findings (US-SC-3..5) | xUnit over `ComplexityMetrics` + `ComplexityReport` (exact-value) | .NET 8 SDK (`testing/UnitTests`) |
| Manual | Solution picker, component collector, dashboard, exports, summary (US-SC-1, US-SC-6..7) | GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## What is NOT automatable here

The collector needs a live solution (component counts split by sub-type), and the Excel/PDF exporters +
WinForms dashboard need the net48/WinForms host. These are documented manual cases with a screenshot each.

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`) — no connection.
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher) with a real solution.

## Entry / exit criteria

- **Entry:** tool builds `Release` with zero warnings; automated tests green.
- **Exit:** all automated tests pass; all manual cases executed with a screenshot, or defects logged.

## Risks

- Component-type split (processes/web resources/forms) depends on sub-type codes — verify counts against the maker portal (TC-SC-M-03).
- Effort/cost numbers are heuristics — the report labels them as estimates; verify the labeling (TC-SC-M-05).
