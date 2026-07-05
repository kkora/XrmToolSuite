# Sharing Analyzer — User Stories

> **Status:** Implemented (v1.2026.7.2).
> **Tag:** `SEC4` · **Project:** `XrmToolSuite.SharingAnalyzer`
> **Spec:** `docs/backlog/02-Security-Governance/SEC4.SharingAnalyzer.md`
> **Reuse:** Shares the analysis primitives (`XrmToolSuite.Core.Analysis` — `Finding`, `Severity`,
> `ScoreCalculator`, `ReportModel`) and the shared reporters (Excel/PDF/JSON) with the rest of the suite.
> **Overlaps:** the sharing-risk score feeds a future Environment Governance Score (SEC9); the
> table × principal intensity view shares patterns with a future User Access Heatmap (SEC10).

Personas: **SEC** (security lead), **ADM** (Dataverse admin), **TOOLDEV**.

---

## EPIC-SEC4 — Surface record-level sharing so excessive and stale shares can be found and cleaned up

> **As** a SEC lead / ADM, **I want** to analyze `PrincipalObjectAccess` to see what is shared, with
> whom, and at what access level, **so that** I can find excessive or risky sharing and recommend cleanup.

**Outcome:** a sharing dashboard by table and principal, a risk-findings list with severities, a
table × principal intensity view, and preview-only cleanup recommendations — all read-only.

---

## FEAT-SEC4-0 — Scaffold & shared wiring `[Done]`

- **US-SEC4.0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background execution via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Tool appears in XTB, connects, runs scans off-thread, persists settings (last tables, full-scan
    toggle, thresholds) on close. No template leftovers (`your-github-username`, "Load sample").

## FEAT-SEC4-1 — Scoped sharing scan `[Done]`

- **US-SEC4.1.1** `[Done]` **As** an ADM, **I want** to pick tables (and optionally a principal filter)
  before scanning POA, **so that** the scan stays within service-protection limits.
  - **AC:** Scan is scoped by table by default (a checked-list table picker); a full-environment scan
    requires an explicit opt-in that warns before enabling.
- **US-SEC4.1.2** `[Done]` **As** a SEC lead, **I want** the scan to run off the UI thread with progress
  and cancellation, **so that** a large POA table does not freeze the tool.
  - **AC:** Paging via `RetrieveAll`; `worker.ReportProgress` updates the spinner; cancellation is honored
    between pages and tables. Per-table read failures degrade to an Info note rather than throwing.

## FEAT-SEC4-2 — Shared-records view `[Done]`

- **US-SEC4.2.1** `[Done]` **As** an ADM, **I want** a grid of shared records by table showing who shared
  with whom and the access rights granted, **so that** I can review the actual shares.
  - **AC:** `accessrightsmask` is decoded to named rights (Read/Write/Append/AppendTo/Create/Delete/
    Share/Assign) and summarized compactly (e.g. `R/W/D`).
- **US-SEC4.2.2** `[Done]` **As** a SEC lead, **I want** access-rights summary cards, **so that** I see the
  score, totals, and the mix of granted rights at a glance.

## FEAT-SEC4-3 — Risk detection `[Done]`

- **US-SEC4.3.1** `[Done]` **As** a SEC lead, **I want** flags for excessive sharing, sharing with inactive
  users, sharing with disabled/empty teams, and records with unusually many shared principals, **so that**
  I get a prioritized cleanup list.
  - **AC:** Excessive sharing (> threshold principals per record) → High; inactive-user share → Medium;
    disabled/empty-team share → Medium; statistical-outlier record → Medium. Each finding carries the
    record/principal evidence, and the set rolls up to a composite score/band via `ScoreCalculator`.
- **US-SEC4.3.2** `[Done]` **As** an ADM, **I want** users with unusually high inbound shared access
  highlighted, **so that** I can spot access sprawl.
  - **AC:** A user with more inbound shared records than the threshold (default 500) → Medium.

## FEAT-SEC4-4 — Intensity visualization `[Done]`

- **US-SEC4.4.1** `[Done]` **As** a SEC lead, **I want** a table × principal intensity view, **so that**
  sharing hotspots are visible without reading every row.
  - **AC:** A ranked, heat-shaded grid of (table, principal, share-count); no external charting dependency.

## FEAT-SEC4-5 — Cleanup recommendations `[Done]`

- **US-SEC4.5.1** `[Done]` **As** an ADM, **I want** recommended cleanup actions as a preview list,
  **so that** I can plan remediation.
  - **AC:** Recommendations are **preview-only** — the tool performs no revoke/mutation. If a revoke action
    is ever added it requires an explicit confirmation dialog stating scope and record count (suite rule 8);
    the default stays read-only.

## FEAT-SEC4-6 — Export `[Done]`

- **US-SEC4.6.1** `[Done]` **As** a SEC lead, **I want** to export the sharing report to
  Excel/PDF/JSON/HTML/CSV, **so that** I can document sharing debt for governance.
  - **AC:** Export runs off the UI thread via a `SaveFileDialog`; the report carries findings + aggregate
    metrics only (no raw dump of every share) so full principal lists are not leaked. Excel/PDF/JSON go
    through the shared reporters; HTML/CSV via BCL writers.

## Definition of Done

- Follows suite conventions; read-only default; sensitive values limited in exports (aggregate counts and
  findings, not full share dumps); export formats Excel, PDF, JSON, HTML, CSV.
- SDK-free logic (`AccessRights`, `SharingModels`, `SharingRiskRules`) unit-tested in `testing/UnitTests/`;
  collector + UI + exports covered by manual cases under `testing/SharingAnalyzer/`.
