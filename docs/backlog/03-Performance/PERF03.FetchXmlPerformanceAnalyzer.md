# FetchXML Performance Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 3. Related pack idea #9 concept (query/JS analysis).
> **Suggested tag:** `PERF03` · **Suggested project:** `XrmToolSuite.FetchXmlPerformanceAnalyzer`
> **Overlaps:** None as a standalone tool — but its core parser/rule engine should become a **shared FetchXML analyzer** in `src/Shared/Core/` reused by View Performance Analyzer (PERF04), Dashboard Performance Checker (PERF05), and any future report/portal analyzer. Build the analyzer UI-free first, wrap it here.
> **Value/priority (my read):** High — FetchXML underpins views, reports, plugins, flows, and Power Pages; a reusable analyzer amortizes across several PERF tools.

## Notes
- Data sources: pasted FetchXML (primary), plus `savedquery`/`userquery` FetchXML to load from existing views. Optional timed execution via `RetrieveMultiple`.
- **Design as a shared, UI-free analyzer:** a pure parser + rule engine operating on a FetchXML string, returning findings. The WinForms tool is a thin host; PERF04/PERF05 consume the same engine.
- Parse dimensions: entities, attributes, filters, link-entities, aliases, orders, aggregations, paging.
- Rules: all-attributes (`<all-attributes/>`) usage, too many selected columns, missing filters, broad date ranges, excessive link-entity joins, unnecessary outer joins, sorting on non-index-friendly columns, aggregation risk, paging issues.
- Feasibility caveat: "index-friendly" and "query cost" are heuristic estimates without server statistics — the tool estimates cost and must label estimates as such; optional live execution timing (opt-in) grounds them.
- Safety: analysis is read-only; the optional "execute with timing" path is opt-in and only runs the (read) query the user supplies.
- Shared-core reuse: `RunAsync` for optional execution, progress/cancellation, settings round-trip, shared export module.

---

## EPIC-PERF03 — Analyze FetchXML for performance risks and produce a reusable query analyzer
> **As** a PERF engineer, **I want** to analyze any FetchXML query for inefficient filters, joins, sorts, and payload, **so that** I can fix slow views, reports, and integrations — and reuse the same engine across tools.

**Outcome:** a parsed query summary, severity-ranked performance findings, an estimated cost, an optional live timing, a suggested optimized FetchXML, and exports — all from a shared analyzer engine.

---

## FEAT-PERF03-1 — Input & load `[Planned]`
- **US-PERF03.1.1** `[Planned]` **As** a PERF engineer, **I want** to paste FetchXML into an editor and validate its syntax, **so that** I can analyze ad-hoc queries.
  - **AC:** Invalid FetchXML shows a clear parse error; valid FetchXML enables analysis.
- **US-PERF03.1.2** `[Planned]` **As** an ADM, **I want** to load FetchXML from a system view, **so that** I can analyze what users actually run.
  - **AC:** A view picker loads `savedquery`/`userquery` FetchXML via `Service.RetrieveAll` off the UI thread.

## FEAT-PERF03-2 — Query parsing & summary `[Planned]`
- **US-PERF03.2.1** `[Planned]` **As** a PERF engineer, **I want** the query broken into entities, attributes, filters, links, aliases, orders, and aggregations, **so that** I understand its shape.
  - **AC:** A summary panel lists each element with counts.
- **US-PERF03.2.2** `[Planned]` **As** a TOOLDEV, **I want** the parser/rule engine to be UI-free and shared, **so that** View and Dashboard analyzers reuse it.
  - **AC:** The analyzer lives in `src/Shared/Core/`, depends only on a FetchXML string (no `IOrganizationService` required for static rules), and returns findings.

## FEAT-PERF03-3 — Performance rule engine `[Planned]`
- **US-PERF03.3.1** `[Planned]` **As** a PERF engineer, **I want** all-attributes usage and excessive selected columns flagged, **so that** I can trim payload.
  - **AC:** `<all-attributes/>` → High; column count over threshold → Medium.
- **US-PERF03.3.2** `[Planned]` **As** a PERF engineer, **I want** missing filters and broad date ranges flagged, **so that** I catch unbounded scans.
  - **AC:** No filter on the root entity → High; date filter spanning beyond a configurable window → Medium.
- **US-PERF03.3.3** `[Planned]` **As** a PERF engineer, **I want** excessive/outer joins and risky sorts flagged, **so that** I can restructure the query.
  - **AC:** Link-entity count over threshold → Medium/High; unnecessary outer joins and sorts on non-index-friendly columns → Medium (labeled heuristic).
- **US-PERF03.3.4** `[Planned]` **As** a PERF engineer, **I want** aggregation and paging risks flagged, **so that** large result issues surface.
  - **AC:** Aggregations without adequate filtering and missing/incorrect paging → Medium/High findings.

## FEAT-PERF03-4 — Cost estimate & optional timing `[Planned]`
- **US-PERF03.4.1** `[Planned]` **As** a PERF engineer, **I want** an estimated query cost score, **so that** I can compare queries without running them.
  - **AC:** A composite cost score derives from the rule findings and is labeled an estimate.
- **US-PERF03.4.2** `[Planned]` **As** a PERF engineer, **I want** to optionally execute the query with timing, **so that** I can ground the estimate.
  - **AC:** Execution is opt-in, runs off the UI thread with cancellation, and reports elapsed time and row count.

## FEAT-PERF03-5 — Recommendations & export `[Planned]`
- **US-PERF03.5.1** `[Planned]` **As** a PERF engineer, **I want** a suggested optimized FetchXML, **so that** I have a concrete fix to apply.
  - **AC:** A suggested-FetchXML panel proposes safe improvements (explicit columns, added filters) with an explanation.
- **US-PERF03.5.2** `[Planned]` **As** a MGR, **I want** Excel/PDF/JSON/HTML/CSV exports of the analysis, **so that** I can share it.
  - **AC:** All export formats come from the shared reporting module.

## Definition of Done
- Follows suite conventions; read-only default; optional execution opt-in; export formats as listed.
- Parser/rule engine is UI-free and lives in shared core for reuse by PERF04/PERF05; heuristic estimates labeled as estimates.
- Testing skeleton under `testing/FetchXmlPerformanceAnalyzer/` when implementation starts; the shared rule engine is heavily covered by `testing/UnitTests` (SDK-free).
