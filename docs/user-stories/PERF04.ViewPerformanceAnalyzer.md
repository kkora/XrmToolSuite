# View Performance Analyzer — User Stories

> **Status:** Active — implemented (see status tags below). Area tag `PERF04`.
> **Source:** ported from `docs/backlog/03-Performance/PERF04.ViewPerformanceAnalyzer.md`.
> **Engine reuse:** consumes the shared FetchXML parser + rule engine in `src/Shared/Core/FetchXml/` (PERF03) rather than reimplementing query rules; adds LayoutXML column analysis, per-view scoring and batch ranking on top (`src/Tools/XrmToolSuite.ViewPerformanceAnalyzer/Analysis/`).
> **Feasibility note:** per-view scores are **heuristic estimates** (no server query statistics) and are labeled as such; opt-in read-only view timing grounds them. All analysis is read-only.

---

## EPIC-PERF04 — Rank an environment's slowest and riskiest views before users complain

> **As** an ADM, **I want** to analyze every system and personal view for heavy FetchXML and layouts, **so that** I can proactively fix the views most likely to be slow.

**Outcome:** a per-view performance score, a ranked slow/risky-view list, per-view FetchXML and layout detail, recommendations, and exports — computed in batch from a live connection.

---

## FEAT-PERF04-1 — View inventory

- **US-PERF04.1.1** `[Done]` **As** an ADM, **I want** to pick a table and list its system views, **so that** I can scope analysis.
  - **AC:** `savedquery` rows for the selected table load via `RetrieveAll` off the UI thread with progress. *(`ViewCollector.Collect` filters `savedquery` by `returnedtypecode`; the table picker loads via `RetrieveAllEntitiesRequest`.)*
- **US-PERF04.1.2** `[Done]` **As** an ADM, **I want** to optionally include personal views, **so that** I can catch user-created heavy views.
  - **AC:** A toggle includes `userquery` rows; selection persists via settings. *(`tsbIncludePersonal`, round-tripped in `ViewSettings.IncludePersonal`.)*

## FEAT-PERF04-2 — FetchXML analysis per view

- **US-PERF04.2.1** `[Done]` **As** a PERF engineer, **I want** each view's FetchXML run through the shared analyzer, **so that** query risks are consistent with the standalone tool.
  - **AC:** Each view's findings come from the shared PERF03 FetchXML rule engine (all-attributes, missing/broad filters, joins, sorts, aggregations). A view whose FetchXML can't be parsed degrades to a single Info finding (score 0), never an exception. *(`ViewScorer.Analyze` → `FetchXmlRules.Analyze`; covered by `ViewPerformanceAnalyzerTests`.)*
- **US-PERF04.2.2** `[Done]` **As** an ADM, **I want** a per-view FetchXML detail panel, **so that** I can inspect what a flagged view actually queries.
  - **AC:** Selecting a view shows its FetchXML and the findings tied to it. *(FetchXML panel + per-view findings grid.)*

## FEAT-PERF04-3 — Layout & column analysis

- **US-PERF04.3.1** `[Done]` **As** a PERF engineer, **I want** LayoutXML columns and FetchXML attributes counted, **so that** I can flag over-wide views.
  - **AC:** Displayed-column count and selected-attribute count show per view; layout columns over `MaxLayoutColumns` (default 15) → Medium. *(`LayoutXmlParser.CountColumns`; `ViewScorer` "Over-wide view layout" finding + labeled layout penalty.)*
- **US-PERF04.3.2** `[Done]` **As** an ADM, **I want** a layout column panel per view, **so that** I can see which columns drive the width.
  - **AC:** The panel lists layout columns for the selected view. *(`lstLayoutColumns`.)*

## FEAT-PERF04-4 — Scoring & ranking

- **US-PERF04.4.1** `[Done]` **As** a PERF engineer, **I want** a per-view performance score, **so that** I can compare views objectively.
  - **AC:** A 0–100 score (labeled heuristic) combines the shared FetchXML cost estimate and a transparent LayoutXML column penalty, banded at 15/40 via `ScoreCalculator.BandFor`. *(`ViewScorer.Analyze`.)*
- **US-PERF04.4.2** `[Done]` **As** an ADM, **I want** the slowest/riskiest views ranked, **so that** I fix the worst first.
  - **AC:** A slow/risky-views grid sorts by score descending with environment score cards (counts by band, worst views). *(`ViewScorer.Rank`; `grdViews`; `BuildEnvironmentSummary`.)*

## FEAT-PERF04-5 — Optional timing, recommendations & export

- **US-PERF04.5.1** `[Done]` **As** a PERF engineer, **I want** to optionally time a view's execution, **so that** I can validate the score.
  - **AC:** Execution is opt-in, read-only, off the UI thread, caps an otherwise-unbounded query with a small top; elapsed time and row count show per view. *(`tsbTime` → `ViewCollector.TimeView`.)*
- **US-PERF04.5.2** `[Done]` **As** an ADM, **I want** recommendations and Excel/PDF/JSON/HTML/CSV exports, **so that** I can act and share.
  - **AC:** Each flagged view carries a recommendation; Excel/PDF/JSON come from the shared reporting module (`ExcelReportExporter`/`PdfReportExporter`/`JsonReportExporter` over a `ReportModel`), HTML/Markdown/CSV via BCL writers. The Excel + native-PDF dependency chains ship in the Plugins root next to the tool DLL, mirroring the Deployment Risk / FetchXML Performance Analyzers.

## Definition of Done

- Follows suite conventions; read-only default; optional view timing opt-in; export formats as listed.
- Reuses the shared FetchXML analyzer (PERF03) rather than reimplementing rules; scores labeled heuristic.
- Batch analysis pages via `RetrieveAll` with progress/cancellation; settings round-trip.
- Testing artifacts under `testing/Tools/ViewPerformanceAnalyzer/`; SDK-free scoring covered by `testing/UnitTests/ViewPerformanceAnalyzerTests.cs`.
