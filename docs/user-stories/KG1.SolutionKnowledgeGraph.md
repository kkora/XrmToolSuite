# Solution Knowledge Graph — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/05-Solution-Management/KG1.SolutionKnowledgeGraph.md`](../backlog/05-Solution-Management/KG1.SolutionKnowledgeGraph.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.SolutionKnowledgeGraph` · **Area tag:** `— (pre-tagging; SOLN track)`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (GDI/browser runtime) — pending manual sign-off.

Builds a directed dependency graph of a Dataverse solution's components: nodes are the solution's tables,
forms, views, plugin steps, web resources, workflows/flows, security roles and apps (friendly type +
resolved display name); edges come from the platform `dependency` table (dependent → required). From the
graph the tool traces a node's transitive dependencies, computes deletion impact (reverse reachability),
detects circular dependencies (Tarjan SCC), and filters by search term / node type. It renders a
self-contained offline interactive HTML view and exports to GraphML, SVG and PNG. Read-only (reads
components, dependencies and metadata; never modifies the solution). The SDK-free graph model, algorithms
and GraphML/SVG/HTML exporters are unit-tested; the `GraphBuilder` is covered headlessly against a fake
`IOrganizationService`; PNG (GDI+), the browser view and the WinForms UI are manual-tested.

---

## EPIC-KG — Understand a solution's dependencies as an interactive graph `[Implemented]`
> **As** an architect/support engineer, **I want** to see a solution's components and their dependencies
> as an interactive graph with impact analysis and cycle detection, **so that** I can assess change
> impact, untangle circular dependencies, and communicate architecture.

**Outcome:** a dependency graph across tables, forms, views, plugin steps, web resources, workflows/flows,
security roles and apps — with search, type filters, dependency tracing, deletion-impact analysis,
circular-dependency detection, an interactive HTML view, and PNG/SVG/GraphML export.

---

## FEAT-KG-0 — Scaffold & shared wiring `[Implemented]`
- **US-KG-0.1** `[Implemented]` The tool loads in XrmToolBox with connection, settings and background
  execution via `BaseToolControl`, so feature work starts from a working shell.
  - **AC:** `SolutionKnowledgeGraphPlugin` exports `Name`/`Description` plus both required
    `SmallImageBase64`/`BigImageBase64` image keys; `SolutionKnowledgeGraphControl : BaseToolControl`
    handles `UpdateConnection` (clears `MetadataCache`, resets the solution list) and round-trips
    `GraphSettings` via `LoadSettings`/`SaveSettings`; no template leftovers. *(Manual — live XTB load; `TC-KG-M-01`.)*

## FEAT-KG-1 — Graph construction `[Implemented]`
- **US-KG-1.1** `[Implemented]` Build a dependency graph from a selected solution to see how its
  components relate.
  - **AC:** Solutions load off the UI thread via `Service.RetrieveAll` (visible, non-Default/Active/Basic);
    `GraphBuilder.Build` reads `solutioncomponent` rows and creates one node per component with a friendly
    type (`Table`, `Form`, `View`, `Plugin Step`, `Web Resource`, `Workflow / Flow`, `Security Role`,
    `Model-driven App`, …) and a resolved display name — tables from entity metadata, the rest by name
    query — falling back to `type + short id` when a name is missing. **Automated (builder)** —
    `TC-KG-COL-01/02/03/09`; *(build off-thread + UI: manual `TC-KG-M-02`).*
  - **AC:** Edges come from the platform `dependency` table (`dependentcomponentobjectid` →
    `requiredcomponentobjectid`, kind `requires`), only when the dependent is in the solution; construction
    is off-thread (`RunAsync`) and fail-soft — a failed query degrades the graph instead of aborting.
    **Automated (builder)** — `TC-KG-COL-04/05/06/07/08`.

## FEAT-KG-2 — Analysis `[Implemented]`
- **US-KG-2** `[Implemented]` Trace a node's dependencies and the impact of deleting it, to assess change risk.
  - **AC:** `GraphModel.DependencyTrace` = forward transitive reachability (BFS over out-edges);
    `GraphModel.Impact` = reverse transitive reachability (BFS over in-edges); both exclude the node
    itself. The node grid shows direct depends-on / impact counts and the detail pane lists the transitive
    sets. **Automated** — `TC-KG-TRACE-03`, `TC-KG-IMPACT-04`; *(detail pane: manual `TC-KG-M-03`).*
- **US-KG-3** `[Implemented]` Detect circular dependencies to find and break dependency loops.
  - **AC:** `GraphModel.Cycles` returns the strongly-connected components of size > 1 (iterative Tarjan,
    stack-safe on large graphs); an acyclic graph reports none. The **Detect cycles** button lists each
    group's components (or a "none" message). **Automated** — `TC-KG-CYCLE-05`, `TC-KG-CYCLE-06`;
    *(UI list: manual `TC-KG-M-05`).*
- **US-KG-4** `[Implemented]` Search and node-type filters to focus on the parts of the graph that matter.
  - **AC:** `ApplyFilter` filters the node grid by a case-insensitive label search term and by the
    checked node types in the type list (populated from the graph's distinct types). *(Manual — WinForms
    grid; `TC-KG-M-06`.)*

## FEAT-KG-3 — Visualization & export `[Implemented]`
- **US-KG-5** `[Implemented]` An interactive graph view plus PNG/SVG/GraphML export, to explore and share it.
  - **AC:** `HtmlGraphBuilder` emits a self-contained offline HTML page — node/edge data embedded as
    hand-built JSON (with `<`/`>` and control-char escaping so a label can't break the `<script>` block),
    drawn by a vanilla-JS force-directed canvas with search, type filters, node drag and click-to-highlight
    (green = depends-on, red = impacted); no external CSS/JS/fonts/CDN. Opens via `Process.Start`.
    **Automated** — `TC-KG-EXPORT-09`; *(browser interaction: manual `TC-KG-M-04`).*
  - **AC:** `GraphMlExporter` writes standard directed GraphML (label/type/kind data keys, XML-safe
    encoding) readable in yEd/Gephi/Cytoscape; `SvgExporter` writes a deterministic circular-layout SVG as
    a pure string (invariant-culture coordinates, per-type node colours); export runs from a
    `SaveFileDialog`. **Automated** — `TC-KG-EXPORT-07` (GraphML), `TC-KG-EXPORT-08` (SVG). **PNG
    `[Implemented*]`** — `PngExporter` rasterises the same circular layout via GDI+ (System.Drawing),
    manual-tested (`TC-KG-M-07`).

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll semantics, Load/SaveSettings, off-thread build with progress).
- Read-only; the graph model, algorithms (trace/impact/cycles) and GraphML/SVG/HTML exporters stay UI-free and SDK-free, and the builder degrades query failures instead of throwing.
- Ships only its own DLL — no ClosedXML/MigraDoc; GraphML/SVG/HTML are pure strings and PNG uses the net48 System.Drawing GAC assembly. — **Done.**
- Export formats: GraphML, SVG, PNG, interactive HTML. — **Done.**
- Testing under `testing/SolutionKnowledgeGraph/`; SDK-free logic covered by `testing/UnitTests` (`GraphTests`, `TC-KG-MODEL/TRACE/IMPACT/CYCLE/EXPORT`) and the builder by `testing/CollectorTests` (`GraphBuilderTests`, `TC-KG-COL-01..09`). — **Done** (PNG/browser/WinForms UI pending manual sign-off).
