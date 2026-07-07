# Form Performance Analyzer — User Stories

> **Status:** Implemented (single-DLL tool). Scores model-driven **main forms** statically from FormXML —
> fully offline scoring; the only Dataverse reads are retrieving the forms and business-rule counts.
> **Source:** `docs/backlog/03-Performance/PERF10.FormPerformanceAnalyzer.md` · **Tag:** `PERF10` ·
> **Project:** `XrmToolSuite.FormPerformanceAnalyzer`
> **Personas:** ADM (admin/customizer), PERF (performance engineer), TOOLDEV, MGR, CUST.

Read-only tool. No destructive operations. Exports **CSV/HTML only** (hand-written — no ClosedXML/PdfSharp
chain, so it ships as a single DLL). Weights/thresholds are configurable and persisted via settings.

---

## EPIC-PERF10 — Score and optimize model-driven form load cost `[Done]`
> **As** an ADM, **I want** every main form ranked by a static "heaviness" score, **so that** I can find and
> fix the forms that make users wait before they even touch a record.

**Outcome:** A ranked list of all main forms with per-form component counts, a composite score + band
(Light/Moderate/Heavy/Critical), and specific optimization recommendations, exportable for review.

---

## FEAT-PERF10-1 — Form inventory & FormXML ingestion `[Done]`
- **US-PERF10.1.1** `[Done]` **As** an ADM, **I want** to list all main forms across selected tables (or the
  whole org), **so that** I have a complete inventory to analyze.
  - **AC:** Forms retrieved via `Service.RetrieveAll` (`systemform`, `type = 2`) off the UI thread; grid shows
    table, form name, state (active/inactive), and headline counts; progress + cancellation supported.
- **US-PERF10.1.2** `[Done]` **As** a TOOLDEV, **I want** FormXML parsed into a structured model, **so that**
  scoring never depends on the live service.
  - **AC:** `FormXmlParser.Parse` turns `formxml` into a `FormModel` (tabs, sections, fields, controls,
    subgrids, quick views, handlers) with no `IOrganizationService` dependency; malformed/blank XML degrades
    to `ParseFailed` (a single warning finding, band Light), never a crash. Covered by unit tests.
- **US-PERF10.1.3** `[Done]` **As** an ADM, **I want** to scope the scan by table set, **so that** I can focus
  on the area I own.
  - **AC:** A multi-select table picker filters retrieval by `objecttypecode`; an empty selection defaults to
    all main forms with a confirmation of intent before the (potentially large) read.

## FEAT-PERF10-2 — Component metrics extraction `[Done]`
- **US-PERF10.2.1** `[Done]` **As** an ADM, **I want** per-form counts of tabs, sections, and fields, **so
  that** I can see structural bloat.
  - **AC:** Counts distinguish visible vs. hidden-by-default tabs and above-the-fold vs. hidden fields; totals
    show per form and in the metric-breakdown detail pane.
- **US-PERF10.2.2** `[Done]` **As** an ADM, **I want** PCF/custom controls, subgrids, and quick-view controls
  counted per form, **so that** I know the expensive render elements.
  - **AC:** Control `classid` GUIDs are mapped to friendly names where resolvable; subgrids and quick views are
    counted separately; PCF/custom controls (a `<customControl>` binding) are counted; unresolved classids
    report as "unknown control".
- **US-PERF10.2.3** `[Done]` **As** an ADM, **I want** JavaScript libraries, event handlers, and attached
  business rules counted, **so that** I see the scripting load.
  - **AC:** Distinct web-resource libraries (form libraries + handler bindings) and handler counts
    (onload/onchange/tabstatechange) are counted; active form-scoped business rules on the entity
    (`workflow`, `category = 2`) are counted per entity; each metric is attributable to its source.

## FEAT-PERF10-3 — Scoring & banding `[Done]`
- **US-PERF10.3.1** `[Done]` **As** an ADM, **I want** a composite performance score per form, **so that** I
  can rank the worst offenders.
  - **AC:** `FormScorer.Score` computes a weighted, capped 0–100 score from all metrics; it is a pure,
    deterministic function — identical input always yields an identical score (unit-tested).
- **US-PERF10.3.2** `[Done]` **As** a PERF, **I want** score bands (Light/Moderate/Heavy/Critical), **so
  that** I can triage at a glance.
  - **AC:** Band thresholds are documented and configurable; the band shows with a color cue in the grid; band
    derivation is covered by automated tests.
- **US-PERF10.3.3** `[Done]` **As** a TOOLDEV, **I want** the weights/thresholds configurable and persisted,
  **so that** teams tune to their own budgets.
  - **AC:** Weights and thresholds are editable in a settings dialog with reset-to-defaults; settings
    round-trip (load in `*_Load`, save in `ClosingPlugin`) as a plain serializable POCO (`FormSettings`).

## FEAT-PERF10-4 — Recommendations `[Done]`
- **US-PERF10.4.1** `[Done]` **As** an ADM, **I want** targeted optimization suggestions per form, **so that**
  I know what to actually change.
  - **AC:** Rules emit suggestions such as "collapse/lazy-load N tabs", "reduce M above-the-fold fields",
    "defer subgrid load", "consolidate K script libraries"; each cites the metric that triggered it.
- **US-PERF10.4.2** `[Done]` **As** a PERF, **I want** quick-win vs. structural recommendations separated, **so
  that** I can plan effort.
  - **AC:** Each recommendation carries an Impact (Quick win/Structural) + Effort tag; the recommendations list
    is sortable by impact.

## FEAT-PERF10-5 — Reporting & export `[Done]`
- **US-PERF10.5.1** `[Done]` **As** an MGR, **I want** to export the scored inventory, **so that** I can share
  findings and track over time.
  - **AC:** CSV and HTML export of forms, metrics, scores, bands, and recommendations; the file write runs off
    the UI thread; the file path is chosen by the user.
- **US-PERF10.5.2** `[Done]` **As** an ADM, **I want** a summary view of band distribution, **so that** I can
  report overall form health.
  - **AC:** The summary shows count per band and the top-10 heaviest forms; it regenerates on each analysis.

## FEAT-PERF10-6 — Drill-down & detail `[Done]`
- **US-PERF10.6.1** `[Done]` **As** an ADM, **I want** to open a form's full metric breakdown, **so that** I
  can see exactly what drives its score.
  - **AC:** The detail pane lists every metric with its contribution to the score; the recommendations pane
    lists that form's suggestions.
- **US-PERF10.6.2** `[Done]` **As** a CUST, **I want** to compare two forms side by side, **so that** I can
  justify a redesign.
  - **AC:** Selecting exactly two forms opens a side-by-side metric comparison with per-metric deltas; no
    writes are performed.

## Definition of Done
- Follows suite conventions; read-only; single-DLL; CSV/HTML export only.
- Scoring/parsing engine is UI-free and covered by `testing/UnitTests/FormPerformanceAnalyzerTests.cs`.
- Testing artifacts under `testing/Tools/FormPerformanceAnalyzer/` kept current.
