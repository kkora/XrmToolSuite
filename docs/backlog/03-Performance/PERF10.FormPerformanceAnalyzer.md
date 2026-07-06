# Form Performance Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `pack` — `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt`, idea #7. No direct equivalent in the ALL_PROMPTS (Doc 3) set. Merged into the Performance category (03-Performance) as item 10; kept pack-sourced.
> **Suggested tag:** `PERF10` · **Suggested project:** `XrmToolSuite.FormPerformanceAnalyzer`
> **Overlaps:** Performance track's View/Dashboard performance analyzers (ALL_PROMPTS) analyze views and dashboards but **not model-driven forms** — Forms are the gap this fills. Some FormXML-parsing plumbing could be shared with Deployment Risk Analyzer and Technical Debt Analyzer (shipped), but their scope is solution-level, not per-form load cost.
> **Value/priority (my read):** High — form load time is a top end-user complaint, is measurable statically from FormXML, and no shipped suite tool scores it.

## Notes
- Primary data source: `systemform` entity `formxml` column (FormType 2 = Main), plus `formjson` where present. Parse FormXML for tabs, sections, columns (fields), `<control>` classid GUIDs (PCF/custom controls), subgrids, quick-view controls, and `<events>`/`<Handlers>` for JavaScript library + function bindings.
- Business rules attached to a table are `workflow` rows (category = 2, scope form); count/weight those active on the entity even though they are not literally in FormXML.
- JavaScript weight: resolve referenced `webresource` libraries, count `onload`/`onchange`/`tabstatechange` handlers; optionally flag synchronous patterns already covered by a JS analyzer candidate (cross-link, do not duplicate deep JS parsing).
- All Dataverse reads off the UI thread via `RunAsync`/`WorkAsync`; page metadata + forms with `Service.RetrieveAll`; report progress per form and support cancellation.
- Scoring engine must be **UI-free** (pure `FormXml string -> FormScore`) so it is unit-testable in `testing/UnitTests/` (net8) with no SDK — the FormXML fixtures make this a strong automated-test candidate.
- Read-only tool. No destructive operations. Exports (CSV/HTML) only.
- Weights/thresholds must be configurable and persisted via settings round-trip so teams can tune to their own load budgets.

---

## EPIC-PERF10 — Score and optimize model-driven form load cost
> **As** an ADM, **I want** every main form ranked by a static "heaviness" score, **so that** I can find and fix the forms that make users wait before they even touch a record.

**Outcome:** A ranked list of all main forms with per-form component counts, a composite score + band (Light/Moderate/Heavy/Critical), and specific optimization recommendations, exportable for review.

---

## FEAT-PERF10-1 — Form inventory & FormXML ingestion `[Planned]`
- **US-PERF10.1.1** `[Planned]` **As** an ADM, **I want** to list all main forms across selected tables (or the whole org), **so that** I have a complete inventory to analyze.
  - **AC:** Forms retrieved via `Service.RetrieveAll` off the UI thread; grid shows table, form name, form type, state (active/inactive); progress + cancellation supported.
- **US-PERF10.1.2** `[Planned]` **As** a TOOLDEV, **I want** FormXML parsed into a structured model, **so that** scoring never depends on the live service.
  - **AC:** A pure parser turns `formxml` into a model (tabs, sections, fields, controls, subgrids, quick views, handlers) with no `IOrganizationService` dependency; malformed XML degrades to a warning finding, not a crash.
- **US-PERF10.1.3** `[Planned]` **As** an ADM, **I want** to scope the scan by solution or table set, **so that** I can focus on the area I own.
  - **AC:** Table/solution filter applied before retrieval; empty selection defaults to all main forms with a confirmation of count.

## FEAT-PERF10-2 — Component metrics extraction `[Planned]`
- **US-PERF10.2.1** `[Planned]` **As** an ADM, **I want** per-form counts of tabs, sections, and fields, **so that** I can see structural bloat.
  - **AC:** Counts distinguish visible vs. hidden-by-default tabs/sections; totals shown per form and in an expandable detail pane.
- **US-PERF10.2.2** `[Planned]` **As** an ADM, **I want** PCF/custom controls, subgrids, and quick-view controls counted per form, **so that** I know the expensive render elements.
  - **AC:** Control classid GUIDs mapped to friendly names where resolvable; subgrids and quick views counted separately; unresolved custom controls flagged as "unknown control."
- **US-PERF10.2.3** `[Planned]` **As** an ADM, **I want** JavaScript libraries, event handlers, and attached business rules counted, **so that** I see the scripting load.
  - **AC:** Distinct web resource libraries and handler bindings (onload/onchange/tab) counted; active form-scoped business rules on the entity counted; each metric attributable to its source.

## FEAT-PERF10-3 — Scoring & banding `[Planned]`
- **US-PERF10.3.1** `[Planned]` **As** an ADM, **I want** a composite performance score per form, **so that** I can rank the worst offenders.
  - **AC:** Weighted score computed from all metrics; deterministic pure function; identical input always yields identical score.
- **US-PERF10.3.2** `[Planned]` **As** a PERF, **I want** score bands (Light/Moderate/Heavy/Critical), **so that** I can triage at a glance.
  - **AC:** Band thresholds documented and configurable; band shown with color cue in the grid; band derivation covered by automated tests.
- **US-PERF10.3.3** `[Planned]` **As** a TOOLDEV, **I want** the weights/thresholds configurable and persisted, **so that** teams tune to their own budgets.
  - **AC:** Weights editable in a settings pane; settings round-trip (load in `*_Load`, save in `ClosingPlugin`) as a plain serializable POCO; reset-to-defaults available.

## FEAT-PERF10-4 — Recommendations `[Planned]`
- **US-PERF10.4.1** `[Planned]` **As** an ADM, **I want** targeted optimization suggestions per form, **so that** I know what to actually change.
  - **AC:** Rules emit suggestions such as "collapse/lazy-load N tabs," "reduce M above-the-fold fields," "defer subgrid load," "consolidate K script libraries"; each suggestion cites the metric that triggered it.
- **US-PERF10.4.2** `[Planned]` **As** a PERF, **I want** quick-win vs. structural recommendations separated, **so that** I can plan effort.
  - **AC:** Each recommendation carries an estimated effort/impact tag; list sortable by impact.

## FEAT-PERF10-5 — Reporting & export `[Planned]`
- **US-PERF10.5.1** `[Planned]` **As** an MGR, **I want** to export the scored inventory, **so that** I can share findings and track over time.
  - **AC:** CSV and HTML export of forms, metrics, scores, bands, and recommendations; export runs off the UI thread; file path chosen by user.
- **US-PERF10.5.2** `[Planned]` **As** an ADM, **I want** a summary view of band distribution, **so that** I can report overall form health.
  - **AC:** Summary shows count per band and top-10 heaviest forms; regenerates when filters change.

## FEAT-PERF10-6 — Drill-down & detail `[Planned]`
- **US-PERF10.6.1** `[Planned]` **As** an ADM, **I want** to open a form's full metric breakdown, **so that** I can see exactly what drives its score.
  - **AC:** Detail pane lists every metric with its contribution to the score; selecting a metric highlights the responsible components.
- **US-PERF10.6.2** `[Planned]` **As** a CUST, **I want** to compare two forms side by side, **so that** I can justify a redesign.
  - **AC:** Two selected forms shown with metric deltas; no writes performed.

## Definition of Done
- Follows suite conventions; read-only default; CSV/HTML export as useful.
- Scoring/parsing engine is UI-free and covered by `testing/UnitTests/` FormXML fixtures.
- Testing skeleton under testing/Tools/FormPerformanceAnalyzer/ when implementation starts.
