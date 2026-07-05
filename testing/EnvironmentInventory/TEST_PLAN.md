# Environment Inventory - Test Plan

Traces to [`docs/user-stories/ADMIN7.EnvironmentInventory.md`](../../docs/user-stories/ADMIN7.EnvironmentInventory.md).

## Scope

The Environment Inventory tool collects a read-only, normalized catalog of a Dataverse environment
(solutions/publishers, tables, security roles/users/teams/BUs, plugin assemblies/steps, workflows/flows,
web resources/PCF/custom APIs, environment-variable definitions and connection references), presents it in a
searchable/filterable grid with a per-component detail panel, and exports to CSV/JSON/Markdown/HTML.

These tests verify:
- **Normalization & pure logic** (SDK-free): `InventorySnapshot.Filter` (text/category/managed), `CountByCategory`,
  `Categories`/`Total`, the CSV/JSON/Markdown/HTML exporters (escaping, self-containment, category headers,
  summary counts) and the `ReportModel`/metrics projection.
- **The no-secrets guarantee:** no exporter emits a secret/value column and no `secretvalue`/`environmentvariablevalue`
  content ever appears in output.
- **Live collection & UI** (manual): off-thread collection with progress, fail-soft degradation of unavailable
  sources, grid population, client-side filtering, detail panel, export round-trips and settings persistence.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (model, exporters, summary projection) | xUnit in `testing/UnitTests/EnvironmentInventoryTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse collection + WinForms UI | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`). Requires the SDK-free files to be added
  to `UnitTests.csproj` (see the tool's implementation report — three `<Compile Include>` lines).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher).

## Entry / exit criteria

- **Entry:** tool builds in Release (`dotnet build src/Tools/XrmToolSuite.EnvironmentInventory/... -c Release`).
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
