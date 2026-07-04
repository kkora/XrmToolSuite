# Solution Knowledge Graph - User Stories

> **Status:** DONE (shipped tool) — filed under Solution Management. Kept its own area tag and story IDs.

Area tag: `KG`. See the [index](../../README.md) for personas, ID scheme, and status legend.

---

## EPIC-KG - Understand a solution's dependencies as an interactive graph

> **As** an architect/support engineer, **I want** to see a solution's components and their dependencies
> as an interactive graph with impact analysis and cycle detection, **so that** I can assess change
> impact, untangle circular dependencies, and communicate architecture.

**Outcome:** a dependency graph across tables, forms, views, plugin steps, web resources, workflows/flows,
security roles, and apps — with search, filters, dependency tracing, deletion-impact analysis, circular-
dependency detection, an interactive HTML view, and PNG/SVG/GraphML export.

---

## FEAT-KG-0 - Scaffold & shared wiring `[Done]`

- **US-KG-0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background execution via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Tool appears in XTB, connects, runs off-thread; no template leftovers; MEF metadata incl. both image keys.

## FEAT-KG-1 - Graph construction `[Done]`

- **US-KG-1.1** `[Done]` **As** an architect, **I want** to build a dependency graph from a selected
  solution, **so that** I can see how its components relate.
  - **AC:** Nodes are the solution's components with friendly type + resolved display name (tables via metadata; forms/views/web resources/workflows/roles/steps/apps by name).
  - **AC:** Edges come from the platform `dependency` table (dependent → required); construction is off-thread and fail-soft.

## FEAT-KG-2 - Analysis `[Done]`

- **US-KG-2** `[Done]` **As** a support engineer, **I want** to trace a node's dependencies and the impact
  of deleting it, **so that** I can assess change risk.
  - **AC:** Dependency trace = forward transitive reachability; impact = reverse transitive reachability, both excluding the node itself. *(TC-KG-TRACE-03, TC-KG-IMPACT-04)*
- **US-KG-3** `[Done]` **As** an architect, **I want** circular-dependency detection, **so that** I can
  find and break dependency loops.
  - **AC:** Cycles are the strongly-connected components of size > 1 (Tarjan); an acyclic graph reports none. *(TC-KG-CYCLE-05..06)*
- **US-KG-4** `[Done]` **As** a user, **I want** search and node-type filters, **so that** I can focus on
  the parts of the graph I care about.
  - **AC:** The node grid filters by search term and by checked node types.

## FEAT-KG-3 - Visualization & export `[Done]`

- **US-KG-5** `[Done]` **As** an architect, **I want** an interactive graph view and PNG/SVG/GraphML export,
  **so that** I can explore and share it.
  - **AC:** A self-contained interactive HTML view (offline force-directed canvas with search/filter/drag/click-to-highlight) opens in the browser. *(TC-KG-EXPORT-09)*
  - **AC:** GraphML (standard XML), SVG (circular layout, pure string), and PNG (GDI+) exports are produced. *(TC-KG-EXPORT-07..08)*

---

## Definition of Done (tool-level)

- Every Dataverse call runs off the UI thread via `RunAsync`/`RetrieveAll` (read-only tool — no destructive ops).
- Settings round-trip. Ships only its own DLL (no ClosedXML/MigraDoc; PNG uses the net48 System.Drawing GAC assembly).
- SDK-free graph model/algorithms and GraphML/SVG/HTML exporters are covered by `testing/UnitTests` (`GraphTests`); the Dataverse builder, PNG export, and interactive UI are manual-tested.
