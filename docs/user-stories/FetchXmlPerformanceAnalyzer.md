# FetchXML Performance Analyzer — User Stories

> **Status:** Active — implemented (see status tags below). Area tag `PERF3`.
> **Source:** ported from `docs/backlog/03-Performance/PERF3.FetchXmlPerformanceAnalyzer.md`.
> **Engine:** a shared, UI-free parser + rule engine in `src/Shared/Core/FetchXml/` (reusable by PERF4/PERF5).
> **Feasibility note:** "index-friendly" / "query cost" are **heuristic estimates** without server statistics; the tool labels them as estimates and offers opt-in live timing to ground them. All analysis is read-only.

---

## EPIC-PERF3 — Analyze FetchXML for performance risks and produce a reusable query analyzer

> **As** a PERF engineer, **I want** to analyze any FetchXML query for inefficient filters, joins, sorts, and payload, **so that** I can fix slow views, reports, and integrations — and reuse the same engine across tools.

**Outcome:** a parsed query summary, severity-ranked findings, an estimated cost + band, optional live timing, optimization suggestions, and exports — all from a shared analyzer engine.

---

## FEAT-PERF3-1 — Input & load

- **US-PERF3.1.1** `[Done]` **As** a PERF engineer, **I want** to paste FetchXML into an editor and validate its syntax, **so that** I can analyze ad-hoc queries.
  - **AC:** Invalid FetchXML shows a clear parse error; valid FetchXML enables analysis and export. *(Parser returns `FetchXmlParseResult.Error`; the control shows it and blocks export.)*
- **US-PERF3.1.2** `[Done]` **As** an ADM, **I want** to load FetchXML from a system or personal view, **so that** I can analyze what users actually run.
  - **AC:** A view picker loads `savedquery` / `userquery` FetchXML via `Service.RetrieveAll` off the UI thread.

## FEAT-PERF3-2 — Query parsing & summary

- **US-PERF3.2.1** `[Done]` **As** a PERF engineer, **I want** the query broken into entity, attributes, filters, links, orders, aggregation, and paging, **so that** I understand its shape.
  - **AC:** A summary panel lists each element with counts (attributes across all entities, link-entities at every depth, orders, aggregate/distinct/no-lock, top/page size).
- **US-PERF3.2.2** `[Done]` **As** a TOOLDEV, **I want** the parser/rule engine to be UI-free and shared, **so that** View and Dashboard analyzers reuse it.
  - **AC:** The analyzer lives in `src/Shared/Core/FetchXml/`, depends only on a FetchXML string (`System.Xml.Linq`, no `IOrganizationService`), and returns findings. Covered by `testing/UnitTests/FetchXmlAnalyzerTests.cs`.

## FEAT-PERF3-3 — Performance rule engine

- **US-PERF3.3.1** `[Done]` **As** a PERF engineer, **I want** all-attributes usage and excessive selected columns flagged, **so that** I can trim payload.
  - **AC:** `<all-attributes/>` → High; column count over `MaxAttributes` (30) → Medium.
- **US-PERF3.3.2** `[Done]` **As** a PERF engineer, **I want** missing filters and unbounded scans flagged, **so that** I catch table scans.
  - **AC:** No filter on the root entity → High; aggregate without a filter → Medium.
- **US-PERF3.3.3** `[Done]` **As** a PERF engineer, **I want** excessive/outer joins and risky sorts flagged, **so that** I can restructure the query.
  - **AC:** Link count over `MaxLinkEntities` (4) → High, over `WarnLinkEntities` (2) → Medium; any outer join → Low (heuristic); sort on a link-entity column → Medium (heuristic).
- **US-PERF3.3.4** `[Done]` **As** a PERF engineer, **I want** aggregation and paging risks flagged, **so that** large-result issues surface.
  - **AC:** Aggregate without filter → Medium; no paging + no top + no aggregate → Low; distinct over > `WarnLinkEntities` joins → Info.

## FEAT-PERF3-4 — Cost estimate & optional timing

- **US-PERF3.4.1** `[Done]` **As** a PERF engineer, **I want** an estimated query cost score and band, **so that** I can compare queries without running them.
  - **AC:** A composite 0–100 cost derives from finding severities (via `ScoreCalculator`), bands at 15/40, and is labeled an estimate.
- **US-PERF3.4.2** `[Done]` **As** a PERF engineer, **I want** to optionally execute the query with timing, **so that** I can ground the estimate.
  - **AC:** Execution is opt-in, runs read-only off the UI thread, caps an otherwise-unbounded query with a small top, and reports elapsed ms + row count.

## FEAT-PERF3-5 — Recommendations & export

- **US-PERF3.5.1** `[Done]` **As** a PERF engineer, **I want** concrete optimization suggestions, **so that** I have fixes to apply.
  - **AC:** A suggestions panel lists plain-text improvements (explicit columns, add filter, reduce joins, etc.) tied to the findings.
- **US-PERF3.5.2** `[Done]` **As** a MGR, **I want** Excel/PDF/JSON/HTML/Markdown/CSV exports of the analysis, **so that** I can share it.
  - **AC:** Excel uses the shared `ExcelReportExporter` (ClosedXML) and PDF uses `PdfReportExporter` (PdfSharp/MigraDoc-GDI), both over the shared `ReportModel`; JSON uses `JsonReportExporter`; HTML/Markdown/CSV are BCL string builders. The Excel + native-PDF dependency chains ship in the Plugins root next to the tool DLL, mirroring the Deployment Risk Analyzer.

## Definition of Done

- Follows suite conventions; read-only default; optional execution opt-in; exports as listed.
- Parser/rule engine is UI-free and lives in shared core for reuse by PERF4/PERF5; heuristic estimates labeled as estimates.
- Testing artifacts under `testing/FetchXmlPerformanceAnalyzer/`; the shared engine is covered by `testing/UnitTests` (SDK-free).
