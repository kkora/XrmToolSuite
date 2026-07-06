# Architecture Diagram Generator - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (SDK-free — `testing/UnitTests/ArchitectureDiagramGeneratorTests.cs`)

| ID | Case | Traces to | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-DOC01-CAT-01 | Component→label/layer catalog | US-DOC01.2.1 | Type 1→Table/Data, 60→UI, 80→Apps; unknown→Other + generic label | Automated | Pass |
| TC-DOC01-ORPHAN-02 | Hide-orphans filter | US-DOC01.3.2 | Default keeps 4 nodes; HideOrphans drops the edge-less assembly (3 nodes) | Automated | Pass |
| TC-DOC01-LAYER-03 | Canonical layer ordering | US-DOC01.3.1 | Layers returned Apps→UI→Code→Data (canonical order) | Automated | Pass |
| TC-DOC01-MER-04 | Mermaid layered + edges | US-DOC01.5.1 | `graph LR`, `subgraph`, `"Apps"`, two `-->` edges, labelled node | Automated | Pass |
| TC-DOC01-PUML-05 | PlantUML well-formed | US-DOC01.5.1 | `@startuml`…`@enduml`, `package "Data"`, `rectangle`, `-->` | Automated | Pass |
| TC-DOC01-DOT-06 | DOT clustered digraph | US-DOC01.5.1 | `digraph architecture {`, `subgraph cluster_`, `rankdir=LR`, two `->` | Automated | Pass |
| TC-DOC01-MD-07 | Markdown mermaid + legend | US-DOC01.5.2 | Title, fenced ` ```mermaid `, `\| Layer \| Components \|`, `\| Apps \| 1 \|` | Automated | Pass |
| TC-DOC01-HTML-08 | HTML self-contained + inline SVG | US-DOC01.5.2 | `<!DOCTYPE html>`, `prefers-color-scheme:dark`, `<svg`, no external fetch, embedded Mermaid source | Automated | Pass |
| TC-DOC01-SVG-09 | SVG escapes node text | US-DOC01.3.2 | `<b>Evil</b>` renders as `&lt;b&gt;…`, never a live tag | Automated | Pass |
| TC-DOC01-JSON-10 | JSON nodes/edges honour filter | US-DOC01.5.1 | `uniqueName`, `layer:"Apps"`; orphan absent; two `from` edges | Automated | Pass |

## Manual (GUI + live Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-DOC01-M01 | Tool loads in XrmToolBox | US-DOC01.1.1 | Open the tool in XrmToolBox | Appears in Tools list; loads; settings restore (see `screenshots/xrmtoolbox-tools-list.png`) | Manual | Pending |
| TC-DOC01-M02 | Load solutions | US-DOC01.1.1 | Click Load solutions | Picker populates off-thread with visible solutions (system solutions excluded) | Manual | Pending |
| TC-DOC01-M03 | Generate diagram | US-DOC01.2.1/2.2 | Pick a solution → Generate | Progress reported; status shows node/edge counts; preview shows Mermaid | Manual | Pending |
| TC-DOC01-M04 | Preview switch | US-DOC01.4.1 | Switch Preview combo Mermaid/PlantUML/DOT/HTML | Preview re-renders in each format | Manual | Pending |
| TC-DOC01-M05 | Layout + direction | US-DOC01.3.1 | Toggle Layered/Dependency + LR/TD | Diagram re-lays out from the cached model (no re-query) | Manual | Pending |
| TC-DOC01-M06 | Hide orphans | US-DOC01.3.2 | Toggle Hide orphans | Unconnected nodes drop from preview/exports | Manual | Pending |
| TC-DOC01-M07 | Export each format | US-DOC01.5.1/5.2 | Export Mermaid/PlantUML/DOT/Markdown/HTML/JSON; open | Files valid; HTML opens offline with an inline SVG diagram; Mermaid renders at mermaid.live | Manual | Pending |
| TC-DOC01-M08 | Degradation note | US-DOC01.2.2 | Run against a solution/user with a permission gap | Empty/partial diagram carries a documented note, no crash | Manual | Pending |

> Automated rows are executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Save a screenshot under
> `screenshots/` for each manual case; the required load shot is `screenshots/xrmtoolbox-tools-list.png`.
