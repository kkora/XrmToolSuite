# Architecture Diagram Generator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 11 (Documentation), item 1. Not in pack file (except ERD/Doc generators relate to pack #11).
> **Suggested tag:** `DOC01` · **Suggested project:** `XrmToolSuite.ArchitectureDiagramGenerator`
> **Overlaps:** **Solution Knowledge Graph** (SHIPPED) — strong diagram/graph overlap; that tool already extracts the component + dependency model and renders a graph. This tool is a *format-specific renderer* (layered/swimlane/hub-and-spoke architecture views) over the same model — reuse its extraction and dependency data, don't re-scan. Also relates to **Solution Documentation Generator** (SOLN05 candidate) which embeds diagrams, and **ERD Generator** (DOC02, the table-level counterpart).
> **Value/priority (my read):** Medium — architecture diagrams are chronically stale and valued for reviews/handoff, but this is largely a rendering layer over the already-shipped Knowledge Graph model, so incremental value depends on the extra layout styles and export formats rather than new data.

## Notes
- Shared extracted-metadata model: reuse the **Solution Knowledge Graph** component + dependency extraction (and the Environment Inventory candidate for topology) as the diagram source — the tool consumes an in-memory architecture model, it is not a new scanner.
- Core data spans many types: `solution`/`solutioncomponent`, tables/`RetrieveAllEntitiesRequest`, `appmodule` (apps), `workflow` (classic + modern flows), `pluginassembly`/`plugintype`/`sdkmessageprocessingstep`, `customapi`, Power Pages (`adx_*`/`mspp_*` where available), `webresource`, PCF, `environmentvariabledefinition`, `connectionreference`, plus external systems inferred from connection references.
- Diagram strategy: emit **Mermaid** and **PlantUML** text as the primary artifacts (Git/wiki-friendly, no native diagram NuGet), then render to **SVG/PNG** via a pure-managed renderer (GDI+ bitmap / hand-written SVG) — no external diagram engine unless it follows the ship-in-Plugins-root rule. PDF/HTML/Markdown wrap the SVG/PNG/Mermaid.
- Export chain reuse: PDF via the sanctioned MigraDoc/PdfSharp-GDI chain, HTML self-contained + theme-aware, Markdown embeds Mermaid fenced blocks — reuse Deployment Risk Analyzer's export patterns; do not add new export libraries casually.
- Read-only — reads metadata and produces diagrams; never modifies the environment.
- Retrieval off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress per diagram/section and honor cancellation; cache metadata per connection and clear on `UpdateConnection`. Keep the layout/model engine UI-free where possible so layout is testable; degrade unavailable component types (Power Pages, some flow detail) to a documented "not shown" note rather than failing.

---

## EPIC-DOC01 — Generate current architecture diagrams from live environment metadata
> **As** an **ARCH**, **I want** to auto-generate architecture diagrams from real Dataverse/Power Platform metadata, **so that** diagrams reflect the environment instead of drifting away from hand-drawn Visio/Lucidchart.

**Outcome:** for a selected environment or solution, a set of chosen diagram types (solution, component, application, integration, security, plugin/flow, Power Pages, topology) rendered in a chosen layout style and exported to SVG, PNG, PDF, Mermaid, PlantUML, HTML, and Markdown.

---

## FEAT-DOC01-1 — Scope selection and component filters `[Planned]`
- **US-DOC01.1.1** `[Planned]` **As** an ARCH, **I want** to select an environment or a specific solution as the diagram scope, **so that** I diagram exactly the components I care about.
  - **AC:** Scope selector lists solutions off the UI thread via `RunAsync`; the chosen scope persists via Load/SaveSettings.
- **US-DOC01.1.2** `[Planned]` **As** an ARCH, **I want** component-type filters (tables, apps, flows, plugins, custom APIs, Power Pages, web resources, PCFs, env vars, connection references, external systems), **so that** I can focus a diagram on one layer.
  - **AC:** Toggling a filter includes/excludes that component type from the model before layout; filter state round-trips in settings.

## FEAT-DOC01-2 — Diagram type generation `[Planned]`
- **US-DOC01.2.1** `[Planned]` **As** an ARCH, **I want** to generate a high-level solution architecture diagram and a component architecture diagram, **so that** stakeholders see structure at two levels of detail.
  - **AC:** Both diagram types render from the shared model; generation reports progress and is cancellable.
- **US-DOC01.2.2** `[Planned]` **As** an ARCH, **I want** application, integration, security, and plugin/flow diagrams, **so that** each architectural concern has its own view.
  - **AC:** Each type is selectable and produced from the same extracted model; a missing concern (e.g. no integrations) renders an explicit empty-state note, not an error.
- **US-DOC01.2.3** `[Planned]` **As** an ARCH, **I want** a Power Pages architecture diagram (where available) and an environment topology diagram, **so that** portals and cross-environment layout are captured.
  - **AC:** Power Pages detail degrades to a documented "not available" note when the tables are absent; topology uses environment/inventory data.

## FEAT-DOC01-3 — Layout styles and legend `[Planned]`
- **US-DOC01.3.1** `[Planned]` **As** an ARCH, **I want** to choose layout styles (layered, dependency graph, swimlane, hub-and-spoke, component map), **so that** the diagram suits the audience.
  - **AC:** Selected layout is applied by the UI-free layout engine; the same model re-lays out without re-querying Dataverse.
- **US-DOC01.3.2** `[Planned]` **As** an ARCH, **I want** a legend panel and a preview canvas, **so that** I can read and verify the diagram before export.
  - **AC:** Preview reflects the current layout/filter selection; a legend explains node/edge types.

## FEAT-DOC01-4 — Branding and titles `[Planned]`
- **US-DOC01.4.1** `[Planned]` **As** an ARCH, **I want** to set a diagram title and branding, **so that** exported diagrams are presentation-ready.
  - **AC:** Title/branding fields round-trip in settings and appear on rendered/exported diagrams; no credentials stored.

## FEAT-DOC01-5 — Multi-format export `[Planned]`
- **US-DOC01.5.1** `[Planned]` **As** an ARCH, **I want** to export to SVG, PNG, Mermaid, and PlantUML, **so that** diagrams drop straight into wikis, Git, and slide decks.
  - **AC:** Mermaid/PlantUML are emitted as text; SVG/PNG render via the pure-managed renderer; export runs off the UI thread.
- **US-DOC01.5.2** `[Planned]` **As** an ARCH, **I want** to export to PDF, HTML, and Markdown, **so that** diagrams sit inside formatted documents.
  - **AC:** PDF reuses the sanctioned MigraDoc/PdfSharp-GDI chain; HTML is self-contained and theme-aware; Markdown embeds Mermaid fenced blocks.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses the Solution Knowledge Graph extracted model and the sanctioned export dependency chains.
- Read-only default; layout/model engine stays UI-free where possible and degrades unavailable component types to documented notes.
- Export formats: SVG, PNG, PDF, Mermaid, PlantUML, HTML, Markdown.
- Testing skeleton under testing/ArchitectureDiagramGenerator/ when implementation starts.
