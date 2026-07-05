# Solution Complexity Score — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/05-Solution-Management/SOLN08.SolutionComplexityScore.md`](../backlog/05-Solution-Management/SOLN08.SolutionComplexityScore.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.SolutionComplexityScore` · **Area tag:** `SOLN08`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (GDI/MigraDoc runtime) — pending manual sign-off.

Inventories a single solution's components and computes a weighted 0–100 complexity score (with a
maintainability score, upgrade/migration/testing effort in person-days, and a rough annual support-cost
estimate) from the tallies via a UI-free, SDK-free scoring engine. Surfaces structural hotspots (wide
forms, high plugin-step counts, large automation/JS/data-model), shows a score + metric-strip + hotspot
dashboard, and exports to PDF, HTML, Excel, JSON and Markdown plus an offline (or opt-in AI) executive
summary. Also computes a sibling **build-quality score** (0–100, higher = better) over the same tallies
(FEAT-SOLN08-4). Read-only (counts components; never modifies the solution). The `ComplexityMetrics`
scoring/effort model, the `QualityScore` engine and the `ComplexityReport` projection are SDK-free and
unit-tested; the Dataverse collector, WinForms dashboard and exporters are manual-tested (the collector also
has a headless fake-service suite).

---

## EPIC-SOLN08 — Quantify a solution's complexity and the effort to maintain it `[Implemented]`
> **As** an architect / ALM lead, **I want** a single tool that inventories a solution and scores its
> complexity, maintainability, and upgrade/migration/testing effort, **so that** I can plan releases,
> size support, and justify refactoring.

**Outcome:** for a selected solution, a 0–100 complexity score with a maintainability score, effort-in-days
and annual support-cost estimates, a per-dimension component tally, hotspot findings, and shareable
PDF/HTML/Excel/JSON/Markdown reports plus an executive summary — all from a live connection.

---

## FEAT-SOLN08-0 — Scaffold & shared wiring `[Implemented]`
- **US-SOLN08-0.1** `[Implemented]` Load in XrmToolBox with connection, settings and background execution via
  `BaseToolControl`, so feature work starts from a working shell.
  - **AC:** Derives from `BaseToolControl`; MEF metadata includes both `SmallImageBase64`/`BigImageBase64`
    keys; no "Template Tool" leftovers; `UpdateConnection` clears `MetadataCache` and resets the solution
    list. Confirmed loading in a live host — `TC-SOLN08-M-01` and the UiSmokeTests harness
    (`xrmtoolbox-tools-list.png`). *(Manual — live XrmToolBox.)*

## FEAT-SOLN08-1 — Component inventory `[Implemented]`
- **US-SOLN08-1.1** `[Implemented]` Pick a solution and inventory every component type, so the score reflects
  the real shape of the solution.
  - **AC:** The solution picker loads visible, non-system solutions (`isvisible = true`, excludes
    `Default`/`Active`/`Basic`) off the UI thread via `RunAsync` + `Service.RetrieveAll`.
  - **AC:** `ComplexityCollector.Collect` tallies tables, columns, relationships, plugin steps, PCF controls,
    views, charts, JavaScript web resources, forms, dashboards, workflows, flows, business rules, custom APIs
    and apps (model-driven + canvas) into a plain `ComponentCounts` POCO.
  - **AC:** Sub-typed rows are split correctly — `webresourcetype == 3` counts as JavaScript; `systemform
    type == 0` is a Dashboard (else a Form); `workflow category` splits business rules (2) / flows (5) /
    workflows (0, 3, other). The widest form (by `<control>` count in `formxml`) is captured as an outlier
    signal. Every query is solution-scoped and fail-soft (degrades to zero, not an exception).
    **Automated (headless fake service)** — `TC-SOLN08-COL-01..08` in `testing/CollectorTests`; also `TC-SOLN08-M-03`
    (compare to the maker portal). *(Collector against live metadata: manual.)*

## FEAT-SOLN08-2 — Scoring, maintainability & effort `[Implemented]`
- **US-SOLN08-3** `[Implemented]` A 0–100 complexity score and a maintainability score, to communicate solution
  health at a glance.
  - **AC:** Each dimension contributes `count × weight` complexity points (documented per-unit weights, e.g.
    Table 3.0, Plugin step 2.5, PCF 3.0, Column 0.2); the total maps to a score that saturates at 100 when
    points reach `PointsForMax` (600); `MaintainabilityScore = 100 − ComplexityScore`. Per-dimension
    contributions are exposed as `DimensionScore` rows. **Automated** — `TC-SOLN08-METRIC-01/03/04`
    (`Empty_ScoreZero_MaintainabilityFull`, `LargeSolution_ScoreCapsAt100`,
    `Dimensions_CarryWeightedContribution`).
- **US-SOLN08-4** `[Implemented]` Upgrade / migration / testing effort (in person-days) and an annual
  support-cost estimate, to budget the next release.
  - **AC:** Effort/cost are transparent linear functions of the tallies — testing days from a weighted sum of
    forms/views/plugin-steps/flows/workflows/business-rules/custom-APIs/JS/PCF; upgrade = points × 0.05;
    migration = points × 0.08 + tables × 0.5; support cost = (testing + upgrade days) × `DayRate` (800) × 2.
    Asserted to exact values so any weight change is caught. **Automated** — `TC-SOLN08-METRIC-02`
    (`KnownCounts_ProduceExactModel`).
- **US-SOLN08-5** `[Implemented]` Hotspot findings (wide forms, high plugin counts, large automation / JS / data
  model), so I know where to focus.
  - **AC:** Outliers surface as `Finding`s at graded severity — wide form (≥100 controls, Medium), high
    plugin-step count (≥30, Medium), large automation surface (flows+workflows+business rules ≥40, Low),
    heavy scripting (JS ≥25, Low), large data model (tables ≥50, Low); an unremarkable solution reads as
    "No structural hotspots" (Info). **Automated** — `TC-SOLN08-REPORT-05/06` (`Report_ProjectsScore_MetricsAndBand`,
    `Report_FlagsWideForm_ElseNoHotspots`).

## FEAT-SOLN08-3 — Dashboard, export & summary `[Implemented*]`
- **US-SOLN08-6** `[Implemented*]` A dashboard (score + band + metric strip + hotspot grid) and
  PDF/HTML/Excel/JSON/Markdown exports, to share the result.
  - **AC:** `ComplexityReport.Build` projects the counts + computed model onto the suite-shared `ReportModel`
    — the score drives the gauge/band (thirds at 34/67 via `ScoreCalculator.BandFor`), the effort/cost
    estimates and raw tallies become metric rows, hotspots become findings. The dashboard binds these to two
    grids with a score/band header; exports run off the UI thread through the shared
    `PdfReportExporter`/`HtmlDashboardBuilder`/`ExcelReportExporter`/`JsonReportExporter`/`FixChecklistGenerator`.
    The report projection is **automated** (`TC-SOLN08-REPORT-05`); the WinForms dashboard and the five exporters
    (incl. the GDI/MigraDoc PDF and ClosedXML Excel chains) are manual — `TC-SOLN08-M-02/04/05/06`.
- **US-SOLN08-7** `[Implemented*]` An executive summary (offline templated default, AI opt-in), for a narrative.
  - **AC:** Offline `TemplatedSummaryGenerator` is the default; AI is opt-in behind a session-only API key
    (env var or the AI-settings dialog, never persisted) and a payload-preview consent dialog that shows the
    anonymized JSON (no record data, credentials, or environment names) before anything is sent; component
    names in the payload are toggleable. The chosen provider/model-id persist; the key does not.
    Manual — `TC-SOLN08-M-07`. *(Live host + external AI call.)*

## FEAT-SOLN08-4 — Solution Quality Score `[Implemented]`
> Formerly the standalone **SOLN04 Solution Quality Score** candidate; built as an extension here (it reuses
> the same `ComponentCounts`, so a separate tool/collector would have duplicated the plumbing).
- **US-SOLN08-8** `[Implemented]` A 0–100 **build-quality** score (with a Low/Med/High band) alongside the
  complexity score, so I can see how *well-built* a solution is, not just how big.
  - **AC:** `QualityScore.Compute(counts, complexityResult)` starts at 100 and deducts for best-practice
    violations derived from the SAME tallies (oversized forms, plugin-step density per table, automation
    sprawl, client-script weight, legacy-workflow reliance, schema sprawl / wide tables, low maintainability);
    the band splits at 80 (High) / 60 (Medium). Higher score = better. SDK-free — no new Dataverse queries.
    **Automated** — `TC-SOLN08-QUALITY-07..11` (empty = 100/High; band cutoffs; exact multi-violation score;
    projection adds the quality metric + a clean note; violations become `Solution Quality` findings).
  - **AC:** `ComplexityReport.Build` folds the grade into the existing `ReportModel` — a "Quality score"
    metric plus one `Solution Quality` finding per deduction — so the current PDF/HTML/Excel/JSON/Markdown
    exporters carry it with no new dependencies; the dashboard shows it in the header + metric strip. *(Dashboard/exports: manual.)*
  - **Deferred (needs a collector change):** naming-prefix consistency, description coverage and
    managed/unmanaged layering would be stronger quality signals but require extending
    `ComplexityCollector`/`ComponentCounts` — a phase-2 item, not in this pass.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll semantics, Load/SaveSettings, progress
  reporting). Read-only tool — no destructive operations, so no confirmation dialog is required.
- The complexity/effort model (`ComplexityMetrics`), the `ComponentCounts` POCO and the `ComplexityReport`
  projection stay UI-free and SDK-free; the collector is UI-free and degrades query failures to zero counts.
- Settings round-trip (Load/ClosingPlugin); the AI API key is never persisted.
- Export formats: PDF, HTML, Excel, JSON, Markdown, plus the offline/AI executive summary. — **Done.**
- Testing under `testing/SolutionComplexityScore/`; SDK-free logic covered by
  `testing/UnitTests/ComplexityScoreTests.cs` and the collector by `testing/CollectorTests`. — **Done**
  (dashboard, exporters, live collector and AI summary pending manual sign-off, `TC-SOLN08-M-01..07`).
