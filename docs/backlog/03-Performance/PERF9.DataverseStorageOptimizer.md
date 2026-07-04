# Dataverse Storage Optimizer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 9. Related pack idea #19 'Dataverse Capacity Analyzer'.
> **Suggested tag:** `PERF9` · **Suggested project:** `XrmToolSuite.DataverseStorageOptimizer`
> **Overlaps:** None — no shipped tool addresses storage/capacity. Attribute Auditor covers column design, not row/storage volume.
> **Value/priority (my read):** High — storage cost is a direct, visible line item and audit/notes/attachment growth is a common, actionable culprit; recommendations translate straight to money saved.
## Notes
- Data sources: row counts via `RetrieveTotalRecordCount` (fast, cached, approximate) or aggregate `count` FetchXML per table; `annotation` (notes/attachments, `filesize`); `audit` growth where retrievable; File/Image column usage from attribute metadata (`FileAttributeMetadata`/`ImageAttributeMetadata`).
- Detection: large tables, high-growth tables, inactive records older than a configured threshold, records eligible for archiving, attachment-heavy entities, and unused file/image columns.
- **Safety is central and this tool NEVER deletes:** read-only analysis only. It produces archive recommendations and a cleanup checklist — guidance, not destructive actions. Any optional cleanup action (if ever added) would require an explicit confirmation dialog stating scope and record count; by default there are no writes at all.
- Feasibility caveat: exact storage bytes per table are not exposed via the SDK (that lives in the Power Platform Admin Center capacity APIs); this tool **estimates** storage from row counts, average note/attachment sizes, and file/image column presence, and must label figures as estimates, not billed capacity.
- Feasibility caveat: `RetrieveTotalRecordCount` is approximate and limited to some tables; audit growth depends on audit being enabled and retained — degrade to informational when data is unavailable.
- Shared-core reuse: `RunAsync`/`RetrieveAll` + aggregate fetch, progress/cancellation (whole-org scans are long), settings round-trip (thresholds), shared export module.

---

## EPIC-PERF9 — Estimate storage hot spots and recommend cost-reducing cleanup — without deleting anything
> **As** an ADM, **I want** to see which tables, notes, attachments, and audit logs drive storage, plus safe archive/cleanup recommendations, **so that** I can reduce cost and improve performance.

**Outcome:** a storage dashboard, table size/growth and attachment/audit panels, an archive-candidates grid, a cleanup checklist, and exports — all read-only with no deletes.

---

## FEAT-PERF9-1 — Storage overview `[Planned]`
- **US-PERF9.1.1** `[Planned]` **As** an ADM, **I want** a storage dashboard estimating the largest tables, **so that** I see where volume concentrates.
  - **AC:** Row counts load via `RetrieveTotalRecordCount`/aggregate fetch off the UI thread with progress; a dashboard ranks tables by estimated size (labeled estimate).
- **US-PERF9.1.2** `[Planned]` **As** an ADM, **I want** thresholds configurable, **so that** "large" and "old" fit my org.
  - **AC:** Size/age/growth thresholds are settings that round-trip on load/close.

## FEAT-PERF9-2 — Table size & growth `[Planned]`
- **US-PERF9.2.1** `[Planned]` **As** an ADM, **I want** large and high-growth tables detected, **so that** I know what to watch.
  - **AC:** Tables over the size threshold → flagged; growth (where trend data is available) over threshold → flagged; unavailable growth data → informational.
- **US-PERF9.2.2** `[Planned]` **As** an ADM, **I want** a table size/growth panel, **so that** I can drill into the biggest tables.
  - **AC:** The panel lists per-table estimated size and, where available, growth.

## FEAT-PERF9-3 — Notes, attachments & audit `[Planned]`
- **US-PERF9.3.1** `[Planned]` **As** an ADM, **I want** annotation/note and attachment usage analyzed, **so that** I can target attachment-heavy entities.
  - **AC:** `annotation` sizes aggregate per entity; an attachment/file usage panel ranks attachment-heavy entities.
- **US-PERF9.3.2** `[Planned]` **As** an ADM, **I want** audit storage growth analyzed where available, **so that** I can rein in audit bloat.
  - **AC:** An audit growth panel shows audit volume where retrievable, or states it's unavailable.
- **US-PERF9.3.3** `[Planned]` **As** an ADM, **I want** unused file/image columns and large-attachment entities detected, **so that** I can flag cleanup targets.
  - **AC:** File/image columns with no populated records → flagged; entities with large aggregate attachment size → flagged.

## FEAT-PERF9-4 — Archive candidates & cleanup guidance `[Planned]`
- **US-PERF9.4.1** `[Planned]` **As** an ADM, **I want** inactive/old records and archive candidates identified, **so that** I can plan archiving.
  - **AC:** Records older than the configured threshold surface in an archive-candidates grid; no delete action is offered.
- **US-PERF9.4.2** `[Planned]` **As** an ADM, **I want** archive recommendations and a cleanup checklist, **so that** I have a safe, actionable plan.
  - **AC:** Recommendations and a checklist are generated as guidance only; the UI states clearly that the tool performs no deletions.

## FEAT-PERF9-5 — Estimate, score & export `[Planned]`
- **US-PERF9.5.1** `[Planned]` **As** a MGR, **I want** an estimate of storage optimization opportunity, **so that** I can justify cleanup work.
  - **AC:** An estimated reclaimable-storage figure is computed and labeled an estimate (not billed capacity).
- **US-PERF9.5.2** `[Planned]` **As** a MGR, **I want** Excel/PDF/JSON/HTML/CSV exports, **so that** I can share the optimization report.
  - **AC:** All export formats come from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; **read-only, no deletes** — cleanup is guidance only; any future optional cleanup action gated behind an explicit confirmation dialog; export formats as listed.
- Storage figures labeled estimates (SDK exposes no billed bytes); unavailable row-count/audit/growth data degrades to informational, never throws.
- Whole-org scans run off the UI thread via `RunAsync` with progress/cancellation; thresholds round-trip via settings.
- Testing skeleton under `testing/DataverseStorageOptimizer/` when implementation starts; estimation/threshold logic covered by `testing/UnitTests`.
