# Plugin Dependency Graph - Test Plan

Traces to [`docs/user-stories/PLUGIN01.PluginDependencyGraph.md`](../../../docs/user-stories/PLUGIN01.PluginDependencyGraph.md).

## Scope

The Plugin Dependency Graph is a read-only XrmToolBox tool that builds a graph of Dataverse plugin
registrations (assembly → type → step → image → message/table → custom API → solution → config), detects
high-impact / duplicate / unmanaged risks, and exports to PNG/SVG/PDF/Excel/JSON/GraphML/HTML.

These tests verify:

- **SDK-free engine (automated):** graph builder projection, subgraph isolation, filtering, the risk rules
  (high-impact, duplicate, unmanaged), the emitters (Mermaid/GraphML/SVG/JSON/HTML), and the guarantee that
  a secure-config value is never present in the model or any export.
- **Dataverse + UI (manual):** the collector against a live org, the toolbar (load, filters, focus, export),
  the node details / dependency grid, the findings panel, and the file exporters (PNG/PDF/Excel need the
  shipped dependency chain + GDI+).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (builder, subgraph/filter, risk rules, emitters, secure-config guarantee) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse collection, WinForms UI, file exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher, with plugin
  registrations present).

## Entry / exit criteria

- **Entry:** tool builds in Release (`dotnet build ...PluginDependencyGraph.csproj -c Release` → 0/0) and
  appears in the XrmToolBox Tools list.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
