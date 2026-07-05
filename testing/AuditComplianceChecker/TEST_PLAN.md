# Audit Compliance Checker - Test Plan

Traces to [`docs/user-stories/AuditComplianceChecker.md`](../../docs/user-stories/AuditComplianceChecker.md).

## Scope

The Audit Compliance Checker reads org/table/column audit configuration and audit activity from a
Dataverse environment, flags coverage gaps (sensitive tables/columns without auditing) and risky
activity (deletes, security-role changes, after-hours edits), and computes a deterministic 0-100
compliance readiness score (higher = more compliant) with a category breakdown. It is read-only and
exports to Excel/PDF/JSON/HTML/CSV.

These tests verify:
- the **SDK-free compliance engine** (sensitivity heuristics, rules, scoring, banding, determinism) — automated;
- the **collector + WinForms host + exports** against a live org — manual (cannot run headlessly).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic: `SensitivityHeuristics`, `AuditComplianceRules.Evaluate` (findings, score, band, category breakdown, determinism, activity rules) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Metadata/audit queries, tabs, dashboard, storage estimate, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`). The engine files
  (`Analysis/AuditModels.cs`, `SensitivityHeuristics.cs`, `AuditComplianceRules.cs`) must be listed as
  `<Compile Include=.../>` items in `UnitTests.csproj` (SDK-free only — NOT the collector).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher, with audit
  privileges to read the `audit` table).

## Entry / exit criteria

- **Entry:** tool builds in Release (0 warnings / 0 errors); export dep DLLs land in `bin/Release/net48`.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
</content>
