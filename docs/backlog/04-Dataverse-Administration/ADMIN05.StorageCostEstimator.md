# Storage Cost Estimator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 5. Not in pack file.
> **Suggested tag:** `ADMIN05` · **Suggested project:** `XrmToolSuite.StorageCostEstimator`
> **Overlaps:** Overlaps a candidate **Performance Storage Optimizer** and pack idea #19 'Dataverse Capacity Analyzer'; shares row-count/largest-table/archive-candidate data with candidate **ADMIN04 (Table Growth Forecast)** — reuse ADMIN04's snapshot store and row-count helper. ADMIN04 forecasts rows; ADMIN05 converts rows/files/audit into estimated storage and cost.
> **Value/priority (my read):** Medium — cost visibility is valued by managers, but Dataverse does not expose exact storage bytes or Microsoft billing to the SDK, so everything is an estimate with disclaimers; usefulness hinges on honest modeling.

## Notes
- Data sources: `RetrieveTotalRecordCount` (row counts), `annotation`/`filesize`, file-column and image-column metadata via `RetrieveMetadataChanges`, `audit` volume signals, per-table create trends. Exact database/file/log storage bytes are **not** exposed via SDK — estimate from rows × modeled avg row size and file/attachment sizes.
- Estimation model and cost calculation are UI-free and unit-testable: category estimates (database, file, log/audit) × configurable per-GB pricing assumptions → monthly/annual cost.
- **Safety:** read-only by default; never export sensitive file *contents* (only sizes/counts/metadata); show prominent estimation disclaimers wherever exact Microsoft billing data is unavailable.
- Pricing assumptions (per-GB DB/file/log) round-trip via settings; before/after savings estimates model the effect of proposed archiving.
- Prefer targeted `ColumnSet`s; report progress per category/table and support cancellation.

---

## EPIC-ADMIN05 — Estimate Dataverse storage usage, cost drivers, and savings opportunities
> **As** an **MGR**, **I want** estimated storage usage and cost broken down by driver with archive/cleanup savings, **so that** I can see what is consuming storage and where optimization reduces spend.

**Outcome:** category storage estimates (database/file/log-audit), largest/attachment-heavy/audit-heavy grids, configurable-pricing monthly/annual cost, before/after savings estimates, and an exportable cost report — all clearly labeled as estimates.

---

## FEAT-ADMIN05-1 — Estimate storage by category `[Planned]`
- **US-ADMIN05.1.1** `[Planned]` **As** an ADM, **I want** database storage estimated by high-volume tables, **so that** I see which tables drive DB usage.
  - **AC:** Estimate = row count (`RetrieveTotalRecordCount`) × modeled avg row size; runs off the UI thread with progress and an estimation disclaimer.
- **US-ADMIN05.1.2** `[Planned]` **As** an ADM, **I want** file storage estimated from annotations, file columns, and image columns, and log/audit growth estimated, **so that** non-DB storage is visible.
  - **AC:** File estimate uses annotation `filesize` and file/image column metadata; audit/log estimate uses audit volume; each category is separately disclaimed.

## FEAT-ADMIN05-2 — Identify cost drivers `[Planned]`
- **US-ADMIN05.2.1** `[Planned]` **As** an ADM, **I want** largest tables, attachment-heavy tables, and audit-heavy tables identified, **so that** I know where to act.
  - **AC:** Three ranked grids surface the top drivers by estimated bytes; each row shows the estimate basis.
- **US-ADMIN05.2.2** `[Planned]` **As** an ADM, **I want** inactive/old data identified, **so that** I can target retention.
  - **AC:** Old-data detection uses age thresholds on `createdon`/`modifiedon`; threshold is configurable.

## FEAT-ADMIN05-3 — Cost calculation and savings `[Planned]`
- **US-ADMIN05.3.1** `[Planned]` **As** an MGR, **I want** monthly/annual cost estimated from configurable pricing assumptions, **so that** I can translate storage into budget.
  - **AC:** Cost model is UI-free and unit-testable; per-GB pricing assumptions round-trip via Load/SaveSettings; growth-adjusted projections are disclaimed.
- **US-ADMIN05.3.2** `[Planned]` **As** an MGR, **I want** a before/after savings estimate for proposed cleanup/archive, **so that** I can justify the effort.
  - **AC:** Savings panel models the delta from archiving selected tables/attachments and shows estimated cost reduction.

## FEAT-ADMIN05-4 — Recommendations and export `[Planned]`
- **US-ADMIN05.4.1** `[Planned]` **As** an ADM, **I want** cleanup and archive recommendations, **so that** I have a concrete optimization plan.
  - **AC:** Recommendations rank by estimated savings and reference their driving finding.
- **US-ADMIN05.4.2** `[Planned]` **As** an MGR, **I want** the storage cost report exported without sensitive file contents, **so that** I can share it safely.
  - **AC:** Exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; exports contain sizes/counts/estimates only — never file contents — and carry estimation disclaimers.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only; no sensitive file contents exported; estimation model UI-free and unit-testable; prominent disclaimers where billing/byte data is unavailable.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/StorageCostEstimator/ when implementation starts.
