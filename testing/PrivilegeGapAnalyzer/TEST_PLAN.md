# Privilege Gap Analyzer - Test Plan

Traces to [`docs/user-stories/PrivilegeGapAnalyzer.md`](../../docs/user-stories/PrivilegeGapAnalyzer.md).

## Scope

The Privilege Gap Analyzer answers "why can (or can't) this user/team/role perform operation X on table Y?"
It computes a principal's **effective privileges** (union of direct-role + team-inherited grants, resolved to the
deepest scope per privilege), maps them against the operation's required privilege(s), and returns a verdict with
the exact missing privilege / insufficient scope / Append mismatch and a read-only recommendation.

These tests verify:

- **The SDK-free engine** (`PrivilegeEngine` over `PrivilegeModels`): deepest-scope resolution, team-only flagging,
  and the verdict classifications (AccessAllowed, MissingPrivilege, InsufficientScope, TeamInheritanceOnly,
  AppendMismatch) plus principal diff. Deterministic → asserted exactly with no live org.
- **The Dataverse collector + UI** (`PrivilegeCollector`, the WinForms control): role/team expansion off the UI
  thread, entity privilege metadata retrieval, the verdict panel / effective grid / recommendation, Compare, and the
  CSV/JSON/HTML exports. Requires Windows + XrmToolBox + a live org.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free engine (`PrivilegeEngine`, `PrivilegeModels`) | xUnit in `testing/UnitTests/PrivilegeEngineTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Collector queries, verdict UI, Compare, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`). The engine files are compiled directly
  into the test project (no Dataverse/WinForms), so no org or net48 pack is needed.
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Administrator, or a role with read on
  `role`/`roleprivileges`/`privilege`/`systemuserroles`/`teamroles`/`teammembership` and metadata read).

## Entry / exit criteria

- **Entry:** tool builds in Release with zero new warnings; `PrivilegeEngineTests` compiled into `UnitTests.csproj`.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
