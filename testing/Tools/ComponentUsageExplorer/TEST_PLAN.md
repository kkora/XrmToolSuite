# Component Usage Explorer - Test Plan

Traces to [`docs/user-stories/SOLN01.ComponentUsageExplorer.md`](../../../docs/user-stories/SOLN01.ComponentUsageExplorer.md).

## Scope

The Component Usage Explorer picks one Dataverse solution component and shows its full "where used"
footprint (required + dependent components, per-type usage) plus a single change-safety verdict. These
tests verify (a) the SDK-free change-safety rules and usage tally, and (b) the Dataverse-backed search,
footprint building, UI, and exports.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free verdict rules + usage-by-type tally (`UsageVerdictRules`, `UsageFootprint`) | xUnit in `testing/UnitTests/ComponentUsageExplorerTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Search, footprint building (dependency APIs), verdict banner, grids, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## What the automated tests cover

- Every verdict path: no-deps → SafeToChange; a few plain deps → ChangeWithCaution; high-value / many
  deps → HighImpact; managed or cross-solution deps → RequiresAlmReview; table with many deps →
  DoNotDelete; incomplete dependency data → RequiresDependencyReview (and that incomplete data does not
  downgrade a higher verdict).
- Score increases with verdict severity and stays within 0–100; band mapping.
- `UsageFootprint.BuildUsageByType` tallies dependents by friendly type name; `Evaluate` populates the
  dependent-count metric and a non-empty explanation.

## What only manual testing can cover

- `UsageCollector.Search` against metadata + `solutioncomponent` (name/GUID/type resolution, owning
  solutions, managed state).
- `BuildFootprint` calling the three platform dependency APIs and degrading unsupported types to an
  incomplete flag.
- The verdict banner colour, the four grids, and Excel/PDF/JSON/HTML export off the UI thread.

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher).

## Entry / exit criteria

- **Entry:** tool builds in Release with 0 warnings / 0 errors.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
