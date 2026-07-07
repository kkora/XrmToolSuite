# Dependency Heatmap — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 5 (Solution Management), item 2. Not in pack file.
> **Suggested tag:** `SOLN02` · **Suggested project:** `XrmToolSuite.DependencyHeatmap`
> **Overlaps:** **STRONGLY overlaps the shipped Solution Knowledge Graph** — that tool already scans a solution/environment, builds the cross-component dependency map, and computes dependent/required counts and high-impact/orphan detection. A separate Dependency Heatmap risks duplicating ~70% of the retrieval and graph model. **Recommendation: build this as a new *visualization/heatmap view mode* on top of the existing Solution Knowledge Graph data model, not a fresh scan.** Only the heatmap rendering, the risk-scoring weights, and PNG/SVG export are genuinely new. Also overlaps SOLN01 (Component Usage Explorer) which drills one component; this is the aggregate/hotspot view.
> **Value/priority (my read):** Medium — the heatmap visualization adds real value for architects, but given the Knowledge Graph overlap the incremental scope is a view layer, so prioritize as an *extension* rather than a standalone tool.

## Notes
- Reuse the Solution Knowledge Graph's dependency retrieval and node/edge model: `solution`, `solutioncomponent`, `RetrieveDependentComponents`/`RetrieveRequiredComponents`, `msdyn_solutioncomponentsummary`, `publisher`. Do **not** re-implement scanning.
- Heatmap is an aggregate of per-component metrics: dependent count, required-dependency count, component-type weight, managed/unmanaged, publisher, solution-layering risk — bucketed into color bands.
- Read-only; the tool visualizes and scores, it never changes components.
- Retrieval off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress and honor cancellation; cache the graph per connection and clear on `UpdateConnection`.
- Keep the scoring/aggregation logic UI-free so it is testable and CI-liftable; degrade missing dependency data (e.g. types the API can't resolve, historical change frequency when unavailable) to Info, not a crash.
- Rendering must stay pure-managed on net48 (GDI+ bitmap for PNG, hand-written SVG string) — no external chart NuGet unless it follows the suite's ship-in-Plugins-root rule; prefer no new dependency.
- Cross-references Solution Knowledge Graph (data core) and SOLN01 Component Usage Explorer (per-node drill-down).

---

## EPIC-SOLN02 — Visualize solution dependency hotspots as an interactive heatmap
> **As** an **ARCH**, **I want** a heatmap of component dependency density and risk across a solution or environment, **so that** I can spot highly-connected, high-impact, and clustered components that make deployments risky.

**Outcome:** a rendered heatmap (by type / solution / publisher / risk), a ranked hotspot list with dependent/required counts, and export to PNG/SVG/Excel/PDF/JSON/HTML.

---

## FEAT-SOLN02-1 — Scope selection and dependency scan `[Planned]`
- **US-SOLN02.1.1** `[Planned]` **As** an ARCH, **I want** to scan a selected solution or the whole environment, **so that** I can scope the heatmap to what I'm reviewing.
  - **AC:** Scan runs off the UI thread with progress and cancellation; reuses the Solution Knowledge Graph data model rather than a second scan where possible.
- **US-SOLN02.1.2** `[Planned]` **As** an ARCH, **I want** dependent-count, required-count, and total-degree computed per component, **so that** the heatmap has quantitative inputs.
  - **AC:** Each node carries dependent count, required count, component type, publisher, and managed/unmanaged state.

## FEAT-SOLN02-2 — Hotspot, cluster, and orphan detection `[Planned]`
- **US-SOLN02.2.1** `[Planned]` **As** an ARCH, **I want** high-impact components and dependency clusters identified, **so that** I know which nodes concentrate deployment risk.
  - **AC:** Components above a configurable dependent-count threshold are flagged high-impact; clusters of mutually-dependent components are grouped.
- **US-SOLN02.2.2** `[Planned]` **As** an ARCH, **I want** orphaned components and circular/suspicious chains surfaced where detectable, **so that** I can clean up or investigate them.
  - **AC:** Zero-dependency orphans and detectable cycles are listed; undetectable cases are noted as Info, not asserted absent.

## FEAT-SOLN02-3 — Heatmap rendering and dimensions `[Planned]`
- **US-SOLN02.3.1** `[Planned]` **As** an ARCH, **I want** the heatmap colored by risk with a legend, **so that** hotspots read at a glance.
  - **AC:** Color bands map to a risk score; component-type and risk-color legends are shown; rendering is pure-managed (no external chart dependency).
- **US-SOLN02.3.2** `[Planned]` **As** an ARCH, **I want** to switch the heatmap axis by component type, solution, publisher, or risk level, **so that** I can view density from different angles.
  - **AC:** At least type / solution / publisher / risk-level modes are selectable and re-render without a re-scan.
- **US-SOLN02.3.3** `[Planned]` **As** an ARCH, **I want** to drill from a heatmap cell into the underlying components, **so that** I can move from pattern to specifics.
  - **AC:** Selecting a cell populates a drill-down grid and detail panel with the components in that bucket.

## FEAT-SOLN02-4 — Risk scoring model `[Planned]`
- **US-SOLN02.4.1** `[Planned]` **As** a release manager, **I want** a per-component risk score, **so that** the heatmap and hotspot ranking are consistent.
  - **AC:** Score weights dependent count, required count, component-type criticality, managed/unmanaged, and solution-layering risk; weights live in settings and round-trip.
- **US-SOLN02.4.2** `[Planned]` **As** a release manager, **I want** production and security-sensitive components weighted higher, **so that** the riskiest changes rank first.
  - **AC:** Security-sensitive component types and (where available) production/change-frequency signals raise the score; missing signals default gracefully.

## FEAT-SOLN02-5 — Export `[Planned]`
- **US-SOLN02.5.1** `[Planned]` **As** an ARCH, **I want** the heatmap and hotspot list exported to PNG, SVG, Excel, PDF, JSON, and HTML, **so that** I can drop it into an architecture review.
  - **AC:** PNG/SVG capture the rendered heatmap; HTML is self-contained and theme-aware; JSON carries per-component scores and counts; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses Solution Knowledge Graph data core instead of duplicating the scan.
- Read-only default; scoring/aggregation analyzers stay UI-free and degrade query failures to Info findings.
- Export formats: PNG, SVG, Excel, PDF, JSON, HTML.
- Testing skeleton under testing/DependencyHeatmap/ when implementation starts.
