# Change History Dashboard — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 12 (Reporting & Analytics), item 7. Not in pack file.
> **Suggested tag:** `RPT7` · **Suggested project:** `XrmToolSuite.ChangeHistoryDashboard`
> **Overlaps:** Overlaps Deployment Analytics (RPT5, deployment timeline) and the shipped Attribute Auditor (attribute-level metadata inspection) — this is broader component-change visibility across types. NOTE: RPT5 focuses on solution imports/versions; this focuses on what metadata/config changed, by whom, and when. Complements, does not duplicate, if scoped to change-detection + baseline diff.
> **Value/priority (my read):** Medium — strong for ADM/ARCH troubleshooting, audit review, and root-cause, and `RetrieveMetadataChanges` gives a real incremental change feed. But "who changed it" is only partial via SDK (created/modified by exists for records, weaker for pure metadata), so attribution is best-effort.

## Notes
- Core mechanism: incremental metadata change detection via `RetrieveMetadataChanges` (server version token / DeletedMetadataFilters) plus `modifiedon`/`modifiedby` on `solutioncomponent` and component records; baseline snapshot diff for change-since-baseline.
- Change surfaces: tables/columns/forms/views/plugins/workflows/web resources, security roles (where available), environment variables, connection references, unmanaged changes.
- Attribution ("modified by") is best-effort — present where the source exposes it, mark "unknown" otherwise; never fabricate an actor.
- Baseline snapshot schema (local, no credentials): change token + per-component-type hashes/timestamps; the comparison engine emits added/modified/removed components since baseline — keep it UI-free and unit-testable.
- Signals: `RetrieveMetadataChanges`, `solutioncomponent`, `environmentvariabledefinition`, `connectionreference`, `webresource`, `pluginassembly`/`plugintype`, `workflow`, `role` — via Service.RetrieveAll off the UI thread with targeted ColumnSets.
- Read-only; scans run off the UI thread via RunAsync/WorkAsync with progress + cancellation; unavailable change sources degrade to Info. Timeline + changed-components charts as inline SVG/PNG for self-contained exports.

---

## EPIC-RPT7 — Give admins visibility into what changed, who changed it, and when
> **As** an ADM / ARCH, **I want** a dashboard of recent metadata/config changes with baseline comparison, **so that** I can troubleshoot, review releases, and support audits.

**Outcome:** a change-history dashboard with date-range/type/modified-by filters, a change timeline, a changed-components grid, baseline comparison, local snapshots, and an exportable change report.

---

## FEAT-RPT7-1 — Change detection `[Planned]`
- **US-RPT7.1.1** `[Planned]` **As** an ARCH, **I want** metadata changes detected incrementally via RetrieveMetadataChanges plus component modified dates, **so that** I get an accurate change feed without full re-scans.
  - **AC:** Detection runs off the UI thread with progress and cancellation; a server change token is stored locally to drive incremental pulls.
- **US-RPT7.1.2** `[Planned]` **As** an ADM, **I want** created/modified-by shown where available and "unknown" otherwise, **so that** attribution is honest.

## FEAT-RPT7-2 — Filters & changed-components grid `[Planned]`
- **US-RPT7.2.1** `[Planned]` **As** an ADM, **I want** date-range, component-type, and modified-by filters over a changed-components grid, **so that** I can narrow to the changes I care about.
  - **AC:** Filters apply client-side over retrieved data without re-querying.
- **US-RPT7.2.2** `[Planned]` **As** an ARCH, **I want** changes shown across tables/columns/forms/views/plugins/workflows/web resources/roles/env variables/connection references, **so that** coverage is broad.
  - **AC:** Unavailable change sources render as Info, not missing silently.

## FEAT-RPT7-3 — Change timeline `[Planned]`
- **US-RPT7.3.1** `[Planned]` **As** an ADM, **I want** a change timeline by date, **so that** I can correlate changes with incidents.
  - **AC:** Timeline renders as inline SVG/PNG.

## FEAT-RPT7-4 — Baseline snapshots & comparison `[Planned]`
- **US-RPT7.4.1** `[Planned]` **As** an ARCH, **I want** to save a baseline snapshot and see changes since baseline, **so that** I can prove exactly what changed between two points.
  - **AC:** Snapshots persist locally with no credentials; the comparison engine is UI-free, deterministic, and unit-tested.

## FEAT-RPT7-5 — Export `[Planned]`
- **US-RPT7.5.1** `[Planned]` **As** an ADM, **I want** to export the change report to Excel/PDF/CSV/HTML, **so that** I can attach it to audit and RCA records.
  - **AC:** Export runs off the UI thread with progress; timeline embedded.

## Definition of Done
- Follows suite conventions; read-only default; attribution honest (no fabricated actors); baseline snapshots stored locally (no credentials); comparison engine UI-free and unit-tested; export formats: Excel, PDF, CSV, HTML, JSON, PNG, SVG.
- Testing skeleton under testing/ChangeHistoryDashboard/ when implementation starts.
