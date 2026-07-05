# Architecture Diagram Generator — User Stories (As-Built)

> **Tool:** `XrmToolSuite.ArchitectureDiagramGenerator` · **Tag:** `DOC01`
> **Source spec:** [docs/backlog/11-Documentation/DOC01.ArchitectureDiagramGenerator.md](../backlog/11-Documentation/DOC01.ArchitectureDiagramGenerator.md)
> **Testing:** [testing/ArchitectureDiagramGenerator/](../../testing/ArchitectureDiagramGenerator/)

Read-only tool that turns a Dataverse solution's components + platform dependencies into an architecture
diagram. Components are classified into architectural **layers** (Apps, UI, Automation, Code, Data, Security,
Configuration, Other); edges come from the platform `dependency` table. The diagram is previewed and exported
to **Mermaid, PlantUML, DOT/Graphviz, Markdown, self-contained HTML, and JSON**. It reuses the same extraction
approach as the Solution Knowledge Graph, keeps its model + emitters UI-free / SDK-free (unit-tested), and is
BCL-only — no export dependency chain. Follows the suite patterns (BaseToolControl, RunAsync, Load/SaveSettings).

The SDK-free model (`Diagram/ArchModel.cs`) + emitters (`Diagram/DiagramEmitters.cs`) are unit-tested in
`testing/UnitTests/ArchitectureDiagramGeneratorTests.cs`. The SDK collector (`Diagram/ArchCollector.cs`) and
the WinForms host are manual-tested (need a live connection / the XrmToolBox host).

---

## EPIC-DOC01 — Generate current architecture diagrams from live environment metadata
> **As** an **ARCH**, **I want** to auto-generate architecture diagrams from real Dataverse metadata, **so that**
> diagrams reflect the environment instead of drifting away from hand-drawn Visio/Lucidchart.

**Outcome:** for a selected solution, an architecture diagram (components grouped into layers, dependency edges)
rendered in a chosen layout style and exported to Mermaid, PlantUML, DOT, Markdown, HTML, and JSON.

---

## FEAT-DOC01-1 — Scope selection `[Implemented]`
- **US-DOC01.1.1** `[Implemented]` **As** an ARCH, **I want** to load and select a solution as the diagram scope,
  **so that** I diagram exactly the components I care about.
  - **AC:** Solutions load off the UI thread via `RunAsync` (system solutions excluded); the picker shows
    friendly name / unique name / version / managed flag. Layout, direction, and hide-orphans preferences
    round-trip via Load/SaveSettings. **Manual** (needs a live connection).

## FEAT-DOC01-2 — Model extraction and layer classification `[Implemented]`
- **US-DOC01.2.1** `[Implemented]` **As** an ARCH, **I want** each solution component mapped to a friendly type
  and an architectural layer, **so that** the diagram reads as an architecture, not a flat blob.
  - **AC:** `ComponentCatalog` maps `solutioncomponent.componenttype` to a label + layer; unknown types degrade
    to a generic label in the *Other* layer. **Automated** — `TC-DOC01-CAT-01`.
- **US-DOC01.2.2** `[Implemented]` **As** an ARCH, **I want** dependency edges from the platform `dependency`
  table, **so that** the diagram shows how components rely on each other.
  - **AC:** `ArchCollector` reads `solutioncomponent` + `dependency` fail-soft (a query gap degrades to a
    documented note, never a crash); self-loops and duplicate edges are removed. **Manual** (needs Dataverse);
    edge handling covered indirectly by the emitter/orphan tests.

## FEAT-DOC01-3 — Layout styles and filtering `[Implemented]`
- **US-DOC01.3.1** `[Implemented]` **As** an ARCH, **I want** to choose a layout (Layered by layer, or flat
  Dependency graph) and flow direction (LR/TD), **so that** the diagram suits the audience — re-laid out
  without re-querying Dataverse.
  - **AC:** Layout/direction re-render from the in-memory model; layers come back in canonical Apps→…→Other
    order. **Automated** — `TC-DOC01-LAYER-03` (+ layout asserts in the emitter tests).
- **US-DOC01.3.2** `[Implemented]` **As** an ARCH, **I want** to hide unconnected (orphan) nodes, **so that**
  large diagrams declutter to just the dependency structure.
  - **AC:** `HideOrphans` drops nodes with no edge (default keeps them); emitters and JSON honour the filter;
    all node text is escaped. **Automated** — `TC-DOC01-ORPHAN-02`, `TC-DOC01-SVG-09`, `TC-DOC01-JSON-10`.

## FEAT-DOC01-4 — Preview `[Implemented]`
- **US-DOC01.4.1** `[Implemented]` **As** an ARCH, **I want** to preview the diagram source (Mermaid / PlantUML /
  DOT / HTML) before export, **so that** I can verify it.
  - **AC:** A preview combo re-renders the current model/layout in the chosen format; generation reports
    progress and node/edge counts. **Manual** (WinForms host).

## FEAT-DOC01-5 — Multi-format export `[Implemented]`
- **US-DOC01.5.1** `[Implemented]` **As** an ARCH, **I want** to export to Mermaid, PlantUML, DOT, and JSON,
  **so that** diagrams drop straight into wikis, Git, and diagram tooling.
  - **AC:** Mermaid emits layered `subgraph`s + edges; PlantUML is a well-formed `@startuml`/`@enduml` doc with
    `package`s; DOT is a clustered `digraph`; JSON carries nodes + edges honouring the orphan filter. Export runs
    off the UI thread. **Automated** — `TC-DOC01-MER-04`, `TC-DOC01-PUML-05`, `TC-DOC01-DOT-06`, `TC-DOC01-JSON-10`.
- **US-DOC01.5.2** `[Implemented]` **As** an ARCH, **I want** to export to Markdown and a self-contained HTML
  page, **so that** diagrams sit inside formatted docs and browse offline.
  - **AC:** Markdown embeds a fenced Mermaid block + a per-layer legend; HTML is self-contained, theme-aware
    (light/dark), and renders a hand-laid-out **inline SVG** offline (no external engine/CDN) with the Mermaid
    source embedded for re-rendering. **Automated** — `TC-DOC01-MD-07`, `TC-DOC01-HTML-08`.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel, Help
  button, required plugin icons); reuses the Solution Knowledge Graph's extraction approach.
- Read-only default; the model + emitters stay UI-free / SDK-free and degrade unavailable data to documented
  notes; HTML/SVG render offline with no external CDN. BCL-only (no export dependency chain).
- Export formats: Mermaid, PlantUML, DOT/Graphviz, Markdown, self-contained HTML, JSON.
- Testing artifacts under `testing/ArchitectureDiagramGenerator/`; SDK-free tests in `testing/UnitTests`.
