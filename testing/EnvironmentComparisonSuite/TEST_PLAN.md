# Environment Comparison Suite - Test Plan

Traces to [`docs/user-stories/MIG1.EnvironmentComparisonSuite.md`](../../docs/user-stories/MIG1.EnvironmentComparisonSuite.md).

## Scope

The Environment Comparison Suite performs a **read-only** source↔target comparison across every
Dataverse component class (solutions/publishers, tables/columns/relationships/keys, forms/views/charts/
dashboards, roles/teams/business units, plugin assemblies/steps/images, processes, custom APIs,
environment variables, connection references, web resources). These tests verify:

1. **The SDK-free diff engine** (`ComparisonModels` + `SnapshotComparer`) — classification
   (Missing/Extra/Changed/ManagedVsUnmanaged/Identical), changed-property diffing, severity assignment,
   the weighted score/band roll-up, the count matrix, and **secret masking**. Deterministic and pure.
2. **The live tool** (dual connection, category selection, collection, grid/detail/summary UI, export) —
   which needs a Windows + XrmToolBox host and two Dataverse connections, so it is manual-only.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free diff engine (`SnapshotComparer`, models) | xUnit in `testing/UnitTests/EnvironmentComparisonSuiteTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dual connection, per-category collection, UI, export | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + two Dataverse envs |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`). No Dataverse, no WinForms.
- **Manual:** Windows + XrmToolBox + a **source** connection and a **target** connection (System
  Customizer or higher on both). Ideally two environments with known drift (e.g. DEV vs UAT).

## Entry / exit criteria

- **Entry:** tool builds in Release (`dotnet build ...EnvironmentComparisonSuite.csproj -c Release` → 0/0);
  the export dependency chain (ClosedXML + `-gdi` PDF assemblies) is present in `bin/Release/net48`.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
  Manual cases are **not** claimed as passed unless actually run against a live host.
