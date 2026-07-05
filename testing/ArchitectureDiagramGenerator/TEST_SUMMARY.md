# Architecture Diagram Generator - Test Summary

**Automated: PASS.** Manual/live and GUI cases: **PENDING** (require a Windows + XrmToolBox + Dataverse session).

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Suite:** `testing/UnitTests/ArchitectureDiagramGeneratorTests.cs` (10 cases: TC-DOC01-CAT-01,
  TC-DOC01-ORPHAN-02, TC-DOC01-LAYER-03, TC-DOC01-MER-04, TC-DOC01-PUML-05, TC-DOC01-DOT-06, TC-DOC01-MD-07,
  TC-DOC01-HTML-08, TC-DOC01-SVG-09, TC-DOC01-JSON-10).
- **Result:** **10 passed, 0 failed** (full suite: **422 passed, 0 failed**).
- **Coverage:** the `ComponentCatalog` type→label/layer mapping, the `ArchDiagram` model (orphan filtering,
  canonical layer ordering), and every emitter — Mermaid (layered subgraphs + edges), PlantUML (packages),
  DOT (clusters), Markdown (fenced mermaid + legend), self-contained theme-aware HTML with an inline SVG (no
  external fetches), SVG escaping, and JSON (nodes/edges honouring the filter).
- **Build:** `dotnet build src/Tools/XrmToolSuite.ArchitectureDiagramGenerator -c Release` — succeeds, 0 warnings.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 10 | 10 | 10 | 0 | 0 |
| Manual | 8 | 0 | 0 | 0 | 8 |

The manual cases cover the SDK collector (`ArchCollector`) against a live solution, solution loading, preview
switching, layout/direction/orphan re-render, export round-trips, permission-gap degradation, and the required
"tool loads in XrmToolBox" screenshot. They **cannot** run headlessly and are **not** claimed as passed.

## Verdict

SDK-free logic (model + all emitters) is fully automated and green. The tool builds clean and is registered in
the `testing/UiSmokeTests` `ExpectedTools` list. Manual/live verification remains pending a Windows + XrmToolBox
+ Dataverse session.
