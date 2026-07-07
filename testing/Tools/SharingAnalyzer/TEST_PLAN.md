# Sharing Analyzer - Test Plan

Traces to [`docs/user-stories/SEC04.SharingAnalyzer.md`](../../../docs/user-stories/SEC04.SharingAnalyzer.md).

## Scope

The Sharing Analyzer scans `PrincipalObjectAccess` (record-level sharing) scoped by table, decodes the
access-rights mask, resolves principals to users/teams and their active state, and evaluates sharing-risk
rules (excessive sharing, inactive-user shares, disabled/empty-team shares, outlier records, inbound
sprawl). These tests verify:

- **SDK-free logic** (automated): `AccessRights.Decode`/`Summary`, `SharingSummary` aggregations, and every
  `SharingRiskRules` rule plus the composite score/band.
- **Collector + UI + export** (manual): POA paging with progress/cancellation, principal resolution, the
  shares/findings/intensity/recommendations views, the full-scan opt-in warning, and Excel/PDF/JSON/HTML/CSV
  export — all requiring a live Dataverse connection and the WinForms host.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (rights decode, aggregations, risk rules, score/band) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | POA scan, principal resolution, UI, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`). The three SDK-free files
  (`AccessRights.cs`, `SharingModels.cs`, `SharingRiskRules.cs`) compile directly into the test project — no
  Dataverse SDK, net48 pack, or connection required.
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher) with some shared
  records present.

## Entry / exit criteria

- **Entry:** tool builds in Release with 0 warnings/0 errors; the export dependency chain (ClosedXML + the
  `-gdi` PDF assemblies) lands in `bin/Release/net48`.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
  A manual GUI case is never reported as passed unless it was actually run in a Windows + XrmToolBox session.
