# Dashboard Performance Checker — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 5. Not in pack file.
> **Suggested tag:** `PERF05` · **Suggested project:** `XrmToolSuite.DashboardPerformanceChecker`
> **Overlaps:** Reuses the shared FetchXML analyzer (PERF03) for referenced views/charts, and conceptually complements View Performance Analyzer (PERF04). No overlap with shipped tools.
> **Value/priority (my read):** Medium — dashboards are a real load hot spot, but affect fewer records than views/plugins; strong once PERF03/PERF04 exist to lean on.

## Notes
- Data sources: `systemform` where `type = dashboard` (FormXML), plus referenced `savedquery`/chart definitions; parse the dashboard XML for components.
- **Depends on the shared FetchXML analyzer (PERF03)** to score referenced views/charts; do not reimplement query rules.
- Component detection from dashboard XML: total component count, charts, views/subgrids, iframes, web resources, and Power BI tiles where present.
- Rules: too many components, repeated heavy views across a dashboard, and dashboards referencing inactive/missing components.
- Feasibility caveat: Power BI tile detection and "heavy" classification are heuristic; missing/inactive component detection depends on cross-referencing referenced IDs — degrade gracefully to informational when a reference can't be resolved.
- Read-only tool — parses definitions only, never modifies dashboards.
- Shared-core reuse: `RunAsync`/`RetrieveAll`, progress/cancellation, settings round-trip, shared export module, shared FetchXML analyzer.

---

## EPIC-PERF05 — Estimate dashboard load cost and flag heavy components
> **As** an ADM, **I want** to analyze dashboards for component count and heavy referenced views/charts, **so that** I can fix slow dashboards before they frustrate users.

**Outcome:** a dashboard load score, component summary and grid, referenced-view performance, risk findings, recommendations, and exports — from parsed dashboard XML.

---

## FEAT-PERF05-1 — Dashboard inventory `[Planned]`
- **US-PERF05.1.1** `[Planned]` **As** an ADM, **I want** to list system dashboards, **so that** I can pick which to analyze.
  - **AC:** Dashboard `systemform` rows load via `RetrieveAll` off the UI thread with progress.
- **US-PERF05.1.2** `[Planned]` **As** an ADM, **I want** to select a dashboard and see its parsed XML, **so that** analysis is scoped to one dashboard.
  - **AC:** Selecting a dashboard parses its FormXML into a component model.

## FEAT-PERF05-2 — Component detection `[Planned]`
- **US-PERF05.2.1** `[Planned]` **As** a PERF engineer, **I want** the count and types of components detected (charts, views/subgrids, iframes, web resources, Power BI tiles), **so that** I understand the dashboard's weight.
  - **AC:** Component summary cards and a component grid show each type with counts; Power BI tiles show where detectable, labeled best-effort.
- **US-PERF05.2.2** `[Planned]` **As** a PERF engineer, **I want** referenced views/charts analyzed with the shared FetchXML rules, **so that** heavy underlying queries surface.
  - **AC:** Each referenced view/chart FetchXML runs through the shared PERF03 engine; a referenced-view performance panel lists findings.

## FEAT-PERF05-3 — Risk rules `[Planned]`
- **US-PERF05.3.1** `[Planned]` **As** a PERF engineer, **I want** dashboards with too many components flagged, **so that** I catch overloaded layouts.
  - **AC:** Component count over a configurable threshold → Medium/High.
- **US-PERF05.3.2** `[Planned]` **As** a PERF engineer, **I want** repeated heavy views and inactive/missing referenced components flagged, **so that** I remove redundancy and broken references.
  - **AC:** The same heavy view referenced multiple times → Medium; references that can't be resolved → informational finding (never throws).

## FEAT-PERF05-4 — Load score, recommendations & export `[Planned]`
- **US-PERF05.4.1** `[Planned]` **As** a MGR, **I want** an estimated dashboard load score, **so that** I can compare dashboards.
  - **AC:** A 0–100 load score (labeled estimate) combines component count/type weights and referenced-view findings.
- **US-PERF05.4.2** `[Planned]` **As** an ADM, **I want** optimization recommendations and Excel/PDF/JSON/HTML/CSV exports, **so that** I can act and share.
  - **AC:** Each finding carries a recommendation; all export formats come from the shared reporting module.

## Definition of Done
- Follows suite conventions; read-only default (parses definitions only); export formats as listed.
- Reuses the shared FetchXML analyzer (PERF03); unresolved references degrade to informational; scores labeled estimates.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; settings round-trip.
- Testing skeleton under `testing/DashboardPerformanceChecker/` when implementation starts; XML parsing/scoring covered by `testing/UnitTests`.
