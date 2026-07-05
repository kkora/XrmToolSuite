# Architecture Diagram Generator - Test Plan

Traces to [`docs/user-stories/DOC01.ArchitectureDiagramGenerator.md`](../../docs/user-stories/DOC01.ArchitectureDiagramGenerator.md).

## Scope

The Architecture Diagram Generator turns a Dataverse solution's components + platform dependencies into an
architecture diagram (components classified into layers) and exports it to Mermaid, PlantUML, DOT/Graphviz,
Markdown, self-contained HTML (inline SVG, offline), and JSON. These tests verify:

- **Automated (SDK-free):** the component→layer catalog, the diagram model (orphan filtering, canonical layer
  ordering), and every text emitter (Mermaid/PlantUML/DOT/Markdown/HTML/SVG/JSON) — shape, escaping, and that
  the HTML/SVG are self-contained (no external fetches).
- **Manual (live):** solution loading, the SDK collector (`ArchCollector`) against a real solution, preview
  switching, layout/direction/hide-orphans re-render, export round-trips, and that the tool loads in XrmToolBox.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free model + emitters | xUnit in `testing/UnitTests/ArchitectureDiagramGeneratorTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse queries and UI | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher).

## Entry / exit criteria

- **Entry:** tool builds in Release with zero warnings.
- **Exit:** all automated tests pass; all manual cases executed with Pass (incl. the required
  `screenshots/xrmtoolbox-tools-list.png` load shot), or defects logged in the summary.
