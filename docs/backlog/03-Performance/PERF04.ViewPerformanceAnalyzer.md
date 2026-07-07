# View Performance Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 4. Not in pack file.
> **Suggested tag:** `PERF04` · **Suggested project:** `XrmToolSuite.ViewPerformanceAnalyzer`
> **Overlaps:** Depends on the shared FetchXML analyzer from PERF03 (reuse its parser/rule engine — do not reimplement). No overlap with shipped tools; Attribute Auditor covers columns, not views.
> **Value/priority (my read):** High — slow views are the most common end-user complaint in model-driven apps, and this batch-analyzes every view an org runs.

## Notes
- Data sources: `savedquery` (system views) and `userquery` (personal views) — FetchXML + LayoutXML per view, via `Service.RetrieveAll`.
- **Depends on PERF03:** the FetchXML rule engine is the shared analyzer; this tool adds LayoutXML column analysis, per-view scoring, and batch ranking on top.
- LayoutXML analysis: count displayed columns, spot wide layouts; correlate with FetchXML column count.
- Rules: all-attributes usage, no filters, broad filters, too many columns, expensive joins, sorting risks — most delegated to the shared engine, applied per view.
- Optional view execution latency test is opt-in (read-only `RetrieveMultiple` of the view's FetchXML) with cancellation.
- Feasibility caveat: scores are heuristic (no server query stats); label estimates. Batch runs across hundreds of views must page and report progress.
- Shared-core reuse: `RunAsync`/`RetrieveAll`, progress/cancellation, settings round-trip, shared export module, shared FetchXML analyzer.

---

## EPIC-PERF04 — Rank an environment's slowest and riskiest views before users complain
> **As** an ADM, **I want** to analyze every system and personal view for heavy FetchXML and layouts, **so that** I can proactively fix the views most likely to be slow.

**Outcome:** a per-view performance score, a ranked slow/risky-view list, per-view FetchXML and layout detail, recommendations, and exports — computed in batch from a live connection.

---

## FEAT-PERF04-1 — View inventory `[Planned]`
- **US-PERF04.1.1** `[Planned]` **As** an ADM, **I want** to pick a table and list its system views, **so that** I can scope analysis.
  - **AC:** `savedquery` rows for the selected table load via `RetrieveAll` off the UI thread with progress.
- **US-PERF04.1.2** `[Planned]` **As** an ADM, **I want** to optionally include personal views, **so that** I can catch user-created heavy views.
  - **AC:** A toggle includes `userquery` rows; selection persists via settings.

## FEAT-PERF04-2 — FetchXML analysis per view `[Planned]`
- **US-PERF04.2.1** `[Planned]` **As** a PERF engineer, **I want** each view's FetchXML run through the shared analyzer, **so that** query risks are consistent with the standalone tool.
  - **AC:** Each view's findings come from the shared PERF03 FetchXML rule engine (all-attributes, missing/broad filters, joins, sorts, aggregations).
- **US-PERF04.2.2** `[Planned]` **As** an ADM, **I want** a per-view FetchXML detail panel, **so that** I can inspect what a flagged view actually queries.
  - **AC:** Selecting a view shows its FetchXML and the findings tied to it.

## FEAT-PERF04-3 — Layout & column analysis `[Planned]`
- **US-PERF04.3.1** `[Planned]` **As** a PERF engineer, **I want** LayoutXML columns and FetchXML attributes counted, **so that** I can flag over-wide views.
  - **AC:** Displayed-column count and selected-attribute count show per view; counts over threshold → Medium.
- **US-PERF04.3.2** `[Planned]` **As** an ADM, **I want** a layout column panel per view, **so that** I can see which columns drive the width.
  - **AC:** The panel lists layout columns for the selected view.

## FEAT-PERF04-4 — Scoring & ranking `[Planned]`
- **US-PERF04.4.1** `[Planned]` **As** a PERF engineer, **I want** a per-view performance score, **so that** I can compare views objectively.
  - **AC:** A 0–100 score (labeled heuristic) combines FetchXML findings and column/join counts per view.
- **US-PERF04.4.2** `[Planned]` **As** an ADM, **I want** the slowest/riskiest views ranked, **so that** I fix the worst first.
  - **AC:** A slow/risky-views panel sorts by score with score cards for the environment.

## FEAT-PERF04-5 — Optional timing, recommendations & export `[Planned]`
- **US-PERF04.5.1** `[Planned]` **As** a PERF engineer, **I want** to optionally time a view's execution, **so that** I can validate the score.
  - **AC:** Execution is opt-in, read-only, off the UI thread with cancellation; elapsed time and row count show per view.
- **US-PERF04.5.2** `[Planned]` **As** an ADM, **I want** recommendations and Excel/PDF/JSON/HTML/CSV exports, **so that** I can act and share.
  - **AC:** Each flagged view carries a recommendation; all export formats come from the shared reporting module.

## Definition of Done
- Follows suite conventions; read-only default; optional view timing opt-in; export formats as listed.
- Reuses the shared FetchXML analyzer (PERF03) rather than reimplementing rules; scores labeled heuristic.
- Batch analysis pages via `RetrieveAll` with progress/cancellation; settings round-trip.
- Testing skeleton under `testing/Tools/ViewPerformanceAnalyzer/` when implementation starts; scoring covered by `testing/UnitTests`.
