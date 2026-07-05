# Managed Solution Impact Checker - Test Plan

Traces to [`docs/user-stories/ALM4.ManagedSolutionImpactChecker.md`](../../docs/user-stories/ALM4.ManagedSolutionImpactChecker.md).

## Scope

Read-only impact analysis for importing / upgrading / patching / deleting a managed solution. These tests
verify the **SDK-free layering engine** (`LayerImpactRules` over `LayerAnalysisInput` in
`src/Tools/XrmToolSuite.ManagedSolutionImpactChecker/Analysis/`) deterministically — every rule fires with
the right severity per deployment path (especially the Upgrade-deletes vs Update/Patch-does-not semantics),
the score bands correctly, and the pre-upgrade checklist + rollback guidance are generated. The live
`ImpactCollector` (Dataverse reads), WinForms dashboard, and the Excel/PDF/JSON/HTML exports are covered by
manual GUI cases against a real org (they cannot run headlessly).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free layering rules (findings, severity-per-path, score/band, checklist + rollback) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Solution/layer queries, dashboard, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection with at least one managed solution installed (System Customizer or higher).

## Entry / exit criteria

- **Entry:** tool builds in Release with 0 warnings / 0 errors.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
