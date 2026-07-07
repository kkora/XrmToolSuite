# Table Growth Forecast — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 4. Not in pack file.
> **Suggested tag:** `ADMIN04` · **Suggested project:** `XrmToolSuite.TableGrowthForecast`
> **Overlaps:** Shares data (row counts, largest tables, archive candidates) with candidate **ADMIN05 (Storage Cost Estimator)** and pack idea #19 'Dataverse Capacity Analyzer'; ADMIN05 turns rows into cost, ADMIN04 turns rows into a time-series forecast. Consider a shared snapshot store.
> **Value/priority (my read):** Medium — genuinely useful for capacity planning, but forecast quality depends on locally-captured snapshot history that only accrues over time, so value is low on day one and grows with use.

## Notes
- Data sources: `RetrieveTotalRecordCount` for per-table row counts (fast, approximate), `RetrieveMetadataChanges` for table list/display names, `createdon`-trend sampling via `RetrieveAll` on high-value tables, `audit`/annotation signals for churn.
- **Local snapshot storage is the core asset:** each scan saves a timestamped snapshot (row counts per table) locally; forecasts compare snapshots over time. Snapshots are append-only — never delete history.
- Forecasting is UI-free and unit-testable: linear/CAGR growth from snapshot deltas → 3/6/12/24-month projections; disclaim that `RetrieveTotalRecordCount` is approximate and Microsoft billing/storage bytes are not exposed.
- Row counts, not bytes — this tool forecasts *rows*; storage-byte estimation is ADMIN05's job (may share the estimation helper).
- Read-only against Dataverse; the only writes are to the local snapshot file.
- Report progress per table and support cancellation; counts can be many tables.

---

## EPIC-ADMIN04 — Forecast Dataverse table growth from snapshot history so teams can plan archiving
> **As** an **ADM**, **I want** per-table row counts captured over time and projected forward, **so that** I know which tables are growing fastest, when they may cause storage/performance issues, and what to archive.

**Outcome:** a local snapshot history, per-table growth rates, 3/6/12/24-month row forecasts, fastest-growing and archive-candidate grids, a growth score, and an exportable forecast report.

---

## FEAT-ADMIN04-1 — Capture and store snapshots `[Planned]`
- **US-ADMIN04.1.1** `[Planned]` **As** an ADM, **I want** current row counts captured per table on demand, **so that** I have a data point to forecast from.
  - **AC:** Counts use `RetrieveTotalRecordCount` off the UI thread with progress/cancellation; an approximation disclaimer is shown.
- **US-ADMIN04.1.2** `[Planned]` **As** an ADM, **I want** each scan saved as a timestamped local snapshot, **so that** history accrues for trend analysis.
  - **AC:** Snapshots persist to local storage append-only (no deletes); a snapshot-history panel lists prior scans with dates.

## FEAT-ADMIN04-2 — Compute growth and forecast `[Planned]`
- **US-ADMIN04.2.1** `[Planned]` **As** an ADM, **I want** daily/weekly/monthly/yearly growth estimated from snapshot deltas, **so that** I understand each table's velocity.
  - **AC:** Growth rates derive from paired snapshots; the calculation is UI-free and unit-testable; single-snapshot tables show "insufficient history".
- **US-ADMIN04.2.2** `[Planned]` **As** an MGR, **I want** 3/6/12/24-month row-count forecasts per table, **so that** I can plan capacity ahead.
  - **AC:** Forecast model (linear/CAGR) is documented and testable; forecasts render on a chart with a confidence/estimation disclaimer.

## FEAT-ADMIN04-3 — Identify hotspots and archive candidates `[Planned]`
- **US-ADMIN04.3.1** `[Planned]` **As** an ADM, **I want** fastest-growing tables and high-create-volume tables identified, **so that** I focus on the real drivers.
  - **AC:** A fastest-growing grid ranks by growth rate; high-create-volume uses `createdon`-trend sampling.
- **US-ADMIN04.3.2** `[Planned]` **As** an ADM, **I want** archive candidates and tables with large notes/files/audit activity flagged, **so that** I know where retention will help.
  - **AC:** Archive-candidate grid combines size, growth, and age signals; annotation/file/audit-heavy tables are noted.

## FEAT-ADMIN04-4 — Scoring, assumptions, and export `[Planned]`
- **US-ADMIN04.4.1** `[Planned]` **As** an ADM, **I want** a table growth score and configurable growth assumptions, **so that** I can tune forecasts to my org's reality.
  - **AC:** Growth score is a UI-free roll-up; assumptions (growth rate overrides, horizon) round-trip via Load/SaveSettings.
- **US-ADMIN04.4.2** `[Planned]` **As** an MGR, **I want** archive/retention recommendations and a forecast report exported, **so that** I can drive a data-management decision.
  - **AC:** Exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; charts render in HTML/PDF.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only against Dataverse; snapshots stored locally, append-only; forecasting engine UI-free and unit-testable; estimation disclaimers present.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/TableGrowthForecast/ when implementation starts.
