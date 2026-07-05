# Dataverse Index Advisor — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 9. Also in pack file (`prompt/2...`) idea #5 'Dataverse Index Advisor' — same tool.
> **Suggested tag:** `ADMIN09` · **Suggested project:** `XrmToolSuite.DataverseIndexAdvisor`
> **Overlaps:** Shares high-volume-table and archive-strategy signals with candidates **ADMIN04 (Table Growth Forecast)** and **ADMIN05 (Storage Cost Estimator)** — reuse the row-count helper. Alternate-key/view/FetchXML analysis is distinct. Query-pattern analysis overlaps a candidate Performance track.
> **Value/priority (my read):** Medium — real performance value via alternate-key and view/FetchXML advice, but **must not overclaim**: Dataverse does not expose SQL index management to makers, so this is *advisory* using supported features only.

## Notes
- **Critical scope constraint (from source):** this tool must **NOT** claim to create SQL indexes or manage physical indexes. It recommends Dataverse-*safe* optimizations only — alternate keys, better view filters, lookup design, archiving, query patterns.
- Data sources: `RetrieveTotalRecordCount` (high-volume tables), `EntityKeyMetadata` (alternate keys, key status), `savedquery`/`savedqueryvisualization` + FetchXML parsing (filters, sorts, link-entities), `DuplicateRule` metadata, lookup attribute metadata, calculated/rollup column metadata via `RetrieveMetadataChanges`.
- Analysis rules are UI-free and unit-testable (FetchXML parsing, filter/sort/link-entity counting, key-candidate detection) so they stay console-liftable; degrade query failures to Info findings.
- Read-only — recommends only; never writes keys, views, or config.
- FetchXML parsing is heuristic — disclaim that recommendations are advisory and should be validated against real workload.
- Report progress per table/view; respect service-protection limits when counting/sampling.

---

## EPIC-ADMIN09 — Recommend Dataverse-safe performance optimizations for high-volume tables and queries
> **As** a **PERF** engineer, **I want** high-volume tables, alternate keys, views, and FetchXML analyzed for index-friendly design improvements, **so that** I can improve performance using supported Dataverse features — without pretending to manage SQL indexes.

**Outcome:** a table volume/risk dashboard, alternate-key and view/FetchXML analysis, candidate alternate keys, query/view/archive recommendations, an index-advisory score, and an exportable report — all framed as advisory.

---

## FEAT-ADMIN09-1 — Analyze table volume and keys `[Planned]`
- **US-ADMIN09.1.1** `[Planned]` **As** a PERF, **I want** high-volume and lookup-heavy tables identified, **so that** I focus tuning where it matters.
  - **AC:** Volume uses `RetrieveTotalRecordCount` off the UI thread with progress/cancellation and an approximation disclaimer; lookup density is computed from attribute metadata.
- **US-ADMIN09.1.2** `[Planned]` **As** a PERF, **I want** existing alternate keys, duplicate-detection rules, and inactive/unused keys analyzed, **so that** I understand current key design.
  - **AC:** `EntityKeyMetadata` status is shown; inactive/failed keys and unused keys are flagged.

## FEAT-ADMIN09-2 — Analyze views and FetchXML `[Planned]`
- **US-ADMIN09.2.1** `[Planned]` **As** a PERF, **I want** views and FetchXML filters analyzed for frequently filtered/sorted columns, **so that** I know which columns queries lean on.
  - **AC:** FetchXML parsing extracts filter/sort columns across `savedquery`; the parser is UI-free and unit-testable.
- **US-ADMIN09.2.2** `[Planned]` **As** a PERF, **I want** overly broad views, many joins/link-entities, poorly-performing sort/filter columns, and calculated/rollup columns used in heavy views detected, **so that** costly query patterns surface.
  - **AC:** Each pattern is a finding with severity; link-entity count and column type inform the rating.

## FEAT-ADMIN09-3 — Recommend safe optimizations `[Planned]`
- **US-ADMIN09.3.1** `[Planned]` **As** a PERF, **I want** missing alternate-key candidates recommended, **so that** I can improve lookups and upserts with supported features.
  - **AC:** Candidate keys derive from frequently filtered columns with high selectivity; each recommendation is explicitly a supported Dataverse alternate key, not an SQL index.
- **US-ADMIN09.3.2** `[Planned]` **As** a PERF, **I want** query/view redesign and archive-strategy recommendations, **so that** I can reduce query cost the supported way.
  - **AC:** Recommendations reference their finding; the UI states nowhere that SQL/physical indexes are created; archive advice targets high-volume/old data.

## FEAT-ADMIN09-4 — Score and export `[Planned]`
- **US-ADMIN09.4.1** `[Planned]` **As** an MGR, **I want** an index-advisory score, **so that** I can prioritize tables for tuning.
  - **AC:** Score is a UI-free weighted roll-up of findings; per-table risk is shown on the dashboard.
- **US-ADMIN09.4.2** `[Planned]` **As** a PERF, **I want** the advisory report exported, **so that** I can share tuning guidance with the team.
  - **AC:** Exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; the report carries the "advisory only — not SQL index management" disclaimer.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only; recommends supported Dataverse features only and **never** claims to create/manage SQL indexes; FetchXML/analysis rules UI-free and unit-testable; degrades query failures to Info.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/DataverseIndexAdvisor/ when implementation starts.
