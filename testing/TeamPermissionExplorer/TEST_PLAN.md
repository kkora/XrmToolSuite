# Team Permission Explorer - Test Plan

Traces to [`docs/user-stories/SEC2.TeamPermissionExplorer.md`](../../docs/user-stories/SEC2.TeamPermissionExplorer.md)
(EPIC-SEC2).

## Scope

Team Permission Explorer loads every Dataverse team, resolves what each team can access (effective
table privileges via the shared `PrivilegeEngine`), who inherits it (members), which records it owns,
and flags empty / no-role / over-privileged / duplicate-role / orphaned teams. It compares two teams
and exports a team security report (Excel/PDF/CSV/HTML).

These tests verify: (1) the SDK-free risk rules and effective-privilege resolution (automated), and
(2) the Dataverse reads, UI grids, compare dialog, and export dialogs (manual, live org).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free risk rules + `TeamProfile.Effective()` | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Team/membership/role/privilege reads, owned-record counts, grids, compare, export | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Administrator, to read teams,
  memberships, roles, and role privileges).

## Entry / exit criteria

- **Entry:** tool builds in Release (0 warnings/0 errors); the Excel/PDF dependency chain is present in `bin/Release/net48/`.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
