# Privilege Gap Analyzer - Test Plan

Traces to [`docs/user-stories/PrivilegeGapAnalyzer.md`](../../docs/user-stories/PrivilegeGapAnalyzer.md).

## Scope

<TODO: what this tool does and what these tests verify.>

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (pure helpers) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse queries and UI | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher).

## Entry / exit criteria

- **Entry:** tool builds in Release.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
