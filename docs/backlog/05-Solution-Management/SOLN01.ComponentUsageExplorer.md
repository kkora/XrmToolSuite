# Component Usage Explorer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 5 (Solution Management), item 1. Not in pack file.
> **Suggested tag:** `SOLN01` · **Suggested project:** `XrmToolSuite.ComponentUsageExplorer`
> **Overlaps:** Shipped **Solution Knowledge Graph** already builds the cross-component dependency graph (tables, plugins, SDK steps, custom APIs, flows, JavaScript) — reuse its retrieval/graph core rather than re-scanning. Also overlaps the **Flow Dependency Analyzer** candidate for flow/connection-reference references. This tool is the *single-component "where used" drill-down* (pick one component, see its full footprint + change-safety verdict), not a whole-environment graph.
> **Value/priority (my read):** High — "what breaks if I touch this?" is the most common pre-change question and Microsoft's built-in dependency view is incomplete and not business-friendly.

## Notes
- Core data: `solution`, `solutioncomponent` (componenttype + objectid) for membership; `RetrieveDependenciesForDeleteRequest` / `RetrieveDependentComponentsRequest` / `RetrieveRequiredComponentsRequest` for the dependency graph; `msdyn_solutioncomponentsummary` for a fast enriched inventory; `publisher` for prefix/ownership.
- Deep usage that dependency APIs miss comes from parsing artifacts: FormXML (form scripts/libraries, PCF, subgrids), LayoutXML/FetchXML (views/charts), `webresource` form-library references, `pluginassembly`/`plugintype`/`sdkmessageprocessingstep`/`sdkmessageprocessingstepimage`, `workflow` (business rules + classic + modern flow), `customapi`, `environmentvariabledefinition`, `connectionreference`, `appmodule`.
- Read-only tool — it explains footprint and emits a change-safety verdict; it never modifies or deletes components.
- All retrieval off the UI thread via `RunAsync`/`WorkAsync`; page with shared-core `Service.RetrieveAll`; report progress and honor `BackgroundWorker` cancellation; cache metadata per connection and clear on `UpdateConnection`.
- Keep usage-detection/impact-scoring logic UI-free (analyzer style) so it stays liftable into a console/CI wrapper; degrade unsupported queries (e.g. Power Pages, dependency APIs on some types) to Info findings instead of throwing.
- Cross-references Solution Knowledge Graph (graph core) and the Dependency Heatmap candidate (SOLN02, aggregate view of the same data).

---

## EPIC-SOLN01 — Show the full usage footprint and change-safety of any solution component
> **As** a **MAKER**, **I want** to pick one Dataverse component and see everywhere it is used plus a change-safety verdict, **so that** I can modify, replace, or delete it without breaking something unseen.

**Outcome:** for a selected component, a required/dependent inventory, a per-type usage count, and a verdict (Safe to change / Change with caution / High impact / Do not delete / Requires dependency review / Requires ALM review), exportable to Excel/PDF/JSON/HTML.

---

## FEAT-SOLN01-1 — Find and select a component `[Planned]`
- **US-SOLN01.1.1** `[Planned]` **As** a MAKER, **I want** to search by display name, schema name, GUID, type, or solution, **so that** I can locate the exact component I am about to change.
  - **AC:** Search runs off the UI thread with progress; results show component type, owning solution(s), and managed/unmanaged state; component-type and solution filters narrow results.
- **US-SOLN01.1.2** `[Planned]` **As** a DEVOPS engineer, **I want** the component's solution membership listed, **so that** I know which packages ship it before I touch it.
  - **AC:** Every owning `solution` is listed with publisher and `ismanaged`; sourced from `solutioncomponent`/`msdyn_solutioncomponentsummary`.

## FEAT-SOLN01-2 — Enumerate required and dependent components `[Planned]`
- **US-SOLN01.2.1** `[Planned]` **As** a CUST developer, **I want** the components the selected one *requires* and the components that *depend on* it, **so that** I see both directions of the dependency chain.
  - **AC:** Required and dependent lists come from the platform dependency APIs, are grouped by component type, and each row links back to its own explorer view.
- **US-SOLN01.2.2** `[Planned]` **As** a CUST developer, **I want** unsupported or empty dependency results surfaced as informational, **so that** an incomplete platform answer never reads as "safe".
  - **AC:** Types the dependency API cannot resolve are shown as an Info note ("dependency data incomplete"), not silently omitted.

## FEAT-SOLN01-3 — Deep artifact-level usage detection `[Planned]`
- **US-SOLN01.3.1** `[Planned]` **As** a MAKER, **I want** JavaScript/web-resource references on form events and libraries detected, **so that** I catch usage the dependency graph misses.
  - **AC:** FormXML is parsed for form libraries, event handlers, subgrids, and PCF; matches list form + event + solution.
- **US-SOLN01.3.2** `[Planned]` **As** a MAKER, **I want** table/column usage across forms, views, charts, dashboards, business rules, plugin steps, custom APIs, environment variables, connection references, and model-driven apps, **so that** I see every place a change lands.
  - **AC:** Each usage class is a distinct category with its own count; LayoutXML/FetchXML and step/image config are parsed; missing sources degrade to Info.
- **US-SOLN01.3.3** `[Planned]` **As** an ADM, **I want** a usage-count-by-component-type summary, **so that** I can gauge blast radius at a glance.
  - **AC:** Summary cards show total usages and per-type counts; the tree view drills from summary into individual usages.

## FEAT-SOLN01-4 — Impact scoring and change-safety verdict `[Planned]`
- **US-SOLN01.4.1** `[Planned]` **As** an ALM engineer, **I want** a change-safety verdict for the selected component, **so that** I get a single go/caution/stop signal.
  - **AC:** Verdict is one of Safe to change / Change with caution / High impact / Do not delete / Requires dependency review / Requires ALM review, driven by dependent count, component-type weight, and managed/unmanaged state.
- **US-SOLN01.4.2** `[Planned]` **As** an ALM engineer, **I want** a recommendation panel explaining the verdict, **so that** I know *why* and what to check next.
  - **AC:** Recommendations name the highest-impact dependents and the review steps (dependency review / ALM sign-off) required before the change.

## FEAT-SOLN01-5 — Export usage report `[Planned]`
- **US-SOLN01.5.1** `[Planned]` **As** a release manager, **I want** the usage + impact report exported to Excel, PDF, JSON, and HTML, **so that** I can attach it to a change record.
  - **AC:** HTML is self-contained and theme-aware; JSON carries the verdict and machine-readable required/dependent lists; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); settings round-trip in Load/ClosingPlugin.
- Read-only default (no write/delete); usage-detection and impact-scoring analyzers stay UI-free and degrade query failures to Info findings.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/Tools/ComponentUsageExplorer/ when implementation starts.
