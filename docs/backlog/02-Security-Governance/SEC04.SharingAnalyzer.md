# Sharing Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 4. Not in pack file.
> **Suggested tag:** `SEC04` · **Suggested project:** `XrmToolSuite.SharingAnalyzer`
> **Overlaps:** Sharing-risk score feeds Environment Governance Score (SEC09); heatmap visualization shares UI patterns with User Access Heatmap (SEC10).
> **Value/priority (my read):** High — manual sharing is invisible technical debt with real security and performance impact, and no built-in view exposes `PrincipalObjectAccess` cleanly.

## Notes
- Core table: `principalobjectaccess` (POA) — `objectid`/`objecttypecode`, `principalid`/`principaltypecode`, `accessrightsmask` (bitmask: Read/Write/Delete/Append/AppendTo/Assign/Share). POA is large and un-indexed for ad-hoc queries — scope by table and page carefully.
- Resolve principals to `systemuser`/`team` and their active/enabled state; resolve object type codes to logical names via metadata.
- Risk rules: excessive sharing (record shared with many principals), sharing with inactive users, sharing with disabled/empty teams, records with unusually high shared-principal counts, users with unusually high inbound shared access.
- Performance-critical: POA can hold millions of rows. Default to per-table scoped scans with `RetrieveAll` paging, aggregate counts where possible, off the UI thread with progress + cancellation. Never full-scan by default.
- Read-only; any cleanup is preview/recommendation only. Keep scoring UI-free and unit-testable.

---

## EPIC-SEC04 — Surface record-level sharing so excessive and stale shares can be found and cleaned up
> **As** a SEC lead / ADM, **I want** to analyze `PrincipalObjectAccess` to see what is shared, with whom, and at what access level, **so that** I can find excessive or risky sharing and recommend cleanup.

**Outcome:** a sharing dashboard by table and principal, a risk-findings list with severities, an access heatmap, and cleanup recommendations — all read-only.

---

## FEAT-SEC04-1 — Scoped sharing scan `[Planned]`
- **US-SEC04.1.1** `[Planned]` **As** an ADM, **I want** to pick tables (and optionally a user/team filter) before scanning POA, **so that** the scan stays within service-protection limits.
  - **AC:** Scan is scoped by table by default; a full-environment scan requires an explicit opt-in with a warning.
- **US-SEC04.1.2** `[Planned]` **As** a SEC lead, **I want** the scan to run off the UI thread with progress and cancellation, **so that** a large POA table does not freeze the tool.
  - **AC:** Paging via `RetrieveAll`; `worker.ReportProgress` updates the spinner and status bar.

## FEAT-SEC04-2 — Shared-records view `[Planned]`
- **US-SEC04.2.1** `[Planned]` **As** an ADM, **I want** a grid of shared records by table showing who shared with whom and the access rights granted, **so that** I can review the actual shares.
  - **AC:** `accessrightsmask` is decoded to named rights (Read/Write/Delete/Append/AppendTo/Assign/Share).
- **US-SEC04.2.2** `[Planned]` **As** a SEC lead, **I want** access-rights summary cards, **so that** I see the mix of granted rights at a glance.

## FEAT-SEC04-3 — Risk detection `[Planned]`
- **US-SEC04.3.1** `[Planned]` **As** a SEC lead, **I want** flags for excessive sharing, sharing with inactive users, sharing with disabled teams, and records with many shared principals, **so that** I get a prioritized cleanup list.
  - **AC:** Each finding carries severity (Critical/High/Medium/Low/Info) and the principal/record evidence.
- **US-SEC04.3.2** `[Planned]` **As** an ADM, **I want** users with unusually high inbound shared access highlighted, **so that** I can spot access sprawl.

## FEAT-SEC04-4 — Heatmap visualization `[Planned]`
- **US-SEC04.4.1** `[Planned]` **As** a SEC lead, **I want** a sharing heatmap (table × principal intensity), **so that** hotspots are visible without reading every row.

## FEAT-SEC04-5 — Cleanup recommendations `[Planned]`
- **US-SEC04.5.1** `[Planned]` **As** an ADM, **I want** recommended cleanup actions (revoke stale shares) as a preview list, **so that** I can plan remediation.
  - **AC:** If a revoke action is ever added, it requires an explicit confirmation dialog stating scope and record count (suite rule 8); default remains read-only.

## FEAT-SEC04-6 — Export `[Planned]`
- **US-SEC04.6.1** `[Planned]` **As** a SEC lead, **I want** to export the sharing report to Excel/PDF/CSV/HTML, **so that** I can document sharing debt for governance.
  - **AC:** Export runs off the UI thread with progress and cancellation.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/SharingAnalyzer/ when implementation starts.
