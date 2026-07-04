# Solution Complexity Score - User Stories

> **Status:** DONE (shipped tool) — filed under Solution Management. Kept its own area tag and story IDs.

Area tag: `SC`. See the [index](../../README.md) for personas, ID scheme, and status legend.

---

## EPIC-SC - Quantify a solution's complexity and the effort to maintain it

> **As** an architect/ALM lead, **I want** a single tool that inventories a solution and scores its
> complexity, maintainability, and upgrade/migration/testing effort, **so that** I can plan releases,
> size support, and justify refactoring.

**Outcome:** a 0–100 complexity score with maintainability, effort-in-days, and annual support-cost
estimates; a component inventory; hotspot findings; and shareable Excel/PDF/HTML/JSON reports plus an
executive summary — all from a live connection.

---

## FEAT-SC-0 - Scaffold & shared wiring `[Done]`

- **US-SC-0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background execution via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Tool appears in XTB, connects, runs off-thread; no template leftovers; MEF metadata incl. both image keys.

## FEAT-SC-1 - Component inventory `[Done]`

- **US-SC-1.1** `[Done]` **As** an architect, **I want** to pick a solution and inventory every component
  type, **so that** the score reflects the real shape of the solution.
  - **AC:** Solution picker loads visible, non-system solutions; scan is solution-scoped via `solutioncomponent` joins.
  - **AC:** Counts tables, columns, relationships, forms, views, charts, plugin steps, workflows, flows, business rules, JavaScript, PCF, custom APIs, dashboards, apps.
  - **AC:** Processes split by category (workflow/flow/business rule); web resources split JS from the rest; forms split from dashboards. All Dataverse access is off-thread and fail-soft.

## FEAT-SC-2 - Scoring, maintainability & effort `[Done]`

- **US-SC-3** `[Done]` **As** an ALM lead, **I want** a 0–100 complexity score and a maintainability
  score, **so that** I can communicate solution health at a glance.
  - **AC:** Weighted per-dimension points map to a saturating 0–100 score; maintainability = 100 − complexity. *(TC-SC-METRIC-01..03)*
- **US-SC-4** `[Done]` **As** a delivery manager, **I want** upgrade/migration/testing effort (in days)
  and an annual support-cost estimate, **so that** I can budget the next release.
  - **AC:** Effort/cost are transparent, documented linear functions of the tallies (exact-value tested). *(TC-SC-METRIC-02)*
- **US-SC-5** `[Done]` **As** an architect, **I want** hotspot findings (wide forms, high plugin counts,
  large automation/JS/data model), **so that** I know where to focus.
  - **AC:** Outliers surface as findings; an unremarkable solution reads as "no structural hotspots". *(TC-SC-REPORT-06)*

## FEAT-SC-3 - Dashboard, export & summary `[Done]`

- **US-SC-6** `[Done]` **As** an executive, **I want** a dashboard (gauge + metric strip) and Excel/PDF/
  HTML/JSON/Markdown exports, **so that** I can share the result.
  - **AC:** The HTML/PDF lead with the score gauge and the effort/dimension metric strip; all five exporters work.
- **US-SC-7** `[Done]` **As** an ALM lead, **I want** an executive summary (offline default, AI opt-in),
  **so that** I get a narrative.
  - **AC:** Offline templated summary by default; AI opt-in behind a session-only key + payload-preview consent; key never persisted.

---

## Definition of Done (tool-level)

- Every Dataverse call runs off the UI thread via `RunAsync`/`RetrieveAll` (read-only tool — no destructive ops).
- Settings round-trip; the API key is never persisted.
- nuspec id/version/description/tags correct; the dependency chain ships in the Plugins root.
- SDK-free metric/effort/report logic is covered by `testing/UnitTests` (`ComplexityScoreTests`); collector/UI are manual-tested against a live org.
