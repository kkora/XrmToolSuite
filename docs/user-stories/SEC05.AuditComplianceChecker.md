# Audit Compliance Checker - User Stories

Traces implementation of `src/Tools/XrmToolSuite.AuditComplianceChecker`. Source backlog:
[`docs/backlog/02-Security-Governance/SEC05.AuditComplianceChecker.md`](../backlog/02-Security-Governance/SEC05.AuditComplianceChecker.md).
Area tag: **SEC05**. Personas: SEC lead, ADM (administrator), MGR (manager), TOOLDEV.

> Read-only tool. It never changes audit settings — it reports coverage, activity, and a compliance
> readiness score, and recommends remediation. Changed/sample field values are never collected;
> storage figures are estimates from record volume, not billed Dataverse storage.

---

## EPIC-SEC05 - Verify audit is configured and active where governance requires it

> **As** a SEC lead / MGR, **I want** to check org/table/column audit settings and analyze audit
> activity, **so that** I can prove coverage, find gaps, and monitor risky changes.

**Outcome:** an audit-coverage report (org/table/column), an activity-trend analysis, a compliance
readiness score with a category breakdown, and prioritized remediation — exportable for auditors.

---

## FEAT-SEC05-0 - Scaffold & shared wiring `[Done]`

- **US-SEC05.0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background execution via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Loads in XTB, connects, runs reads through `RunAsync`/`RetrieveAll`, persists settings on close;
    `UpdateConnection` clears `MetadataCache`. No template leftovers (`UserName` = `kkora`, no "Load sample").

## FEAT-SEC05-1 - Audit settings coverage `[Done]`

- **US-SEC05.1.1** `[Done]` **As** a SEC lead, **I want** org-, table-, and column-level audit settings in
  one grid, **so that** I know where auditing is on or off.
  - **AC:** `organization.isauditenabled`, `EntityMetadata.IsAuditEnabled`, and per-`AttributeMetadata.IsAuditEnabled`
    are read via metadata; managed/custom shown per row; org status shown as a banner.
- **US-SEC05.1.2** `[Done]` **As** a SEC lead, **I want** sensitive tables and fields without audit enabled
  flagged, **so that** I can close coverage gaps.
  - **AC:** `SensitivityHeuristics` (documented name/type pattern list) flags each table/column; a sensitive
    table without audit is **High**, a sensitive column without audit on an audited table is **Medium**.

## FEAT-SEC05-2 - Audit activity analysis `[Done]`

- **US-SEC05.2.1** `[Done]` **As** a SEC lead, **I want** audit activity summarized by table, user, and date,
  **so that** I understand where change volume concentrates.
  - **AC:** `audit` queries are date-scoped and paged via `RetrieveAll`, run off the UI thread with progress and
    cancellation; results pivot by table/user/date.
- **US-SEC05.2.2** `[Done]` **As** an ADM, **I want** high-risk changes highlighted (deletes, after-hours edits,
  security-role/privilege changes), **so that** I can spot suspicious activity.
  - **AC:** High delete volume → **Medium**; security-role/privilege/team-membership changes → **Medium**;
    after-hours (outside configurable business hours / weekends) → **Low**.

## FEAT-SEC05-3 - Storage growth trend `[Done]`

- **US-SEC05.3.1** `[Done]` **As** an MGR, **I want** an estimated audit storage growth trend, **so that** I can
  anticipate retention/cost pressure.
  - **AC:** Growth is a labelled **estimate** (records × ~2 KB/record, cumulative by date), explicitly not billed storage.

## FEAT-SEC05-4 - Compliance score `[Done]`

- **US-SEC05.4.1** `[Done]` **As** an MGR, **I want** a compliance readiness score (0-100) with a category
  breakdown, **so that** I have an executive summary of audit health.
  - **AC:** Score is deterministic (same input → same output), HIGHER = MORE compliant, banded Low/Medium/High
    (High = good) via `ScoreCalculator.BandFor`; weighted blend of org (25%) / table coverage (30%) /
    column coverage (25%) / activity (20%); explainable from the listed findings.

## FEAT-SEC05-5 - Recommendations `[Done]`

- **US-SEC05.5.1** `[Done]` **As** a SEC lead, **I want** prioritized remediation (enable audit on these
  tables/fields; review these changes), **so that** each gap has a next step.
  - **AC:** Recommendations are read-only text; the tool never changes audit settings.

## FEAT-SEC05-6 - Export `[Done]`

- **US-SEC05.6.1** `[Done]` **As** an MGR, **I want** to export the audit compliance report to
  Excel/PDF/JSON/HTML/CSV, **so that** I can present it to auditors and leadership.
  - **AC:** Excel/PDF via the shared reporters (`ReportModel`); JSON via `JsonReportExporter`; HTML/CSV via BCL
    writers; no changed/sample field values appear in exports; export runs off the UI thread via `SaveFileDialog`.

---

## Definition of Done (tool-level)

- Every Dataverse call runs off the UI thread via `RunAsync` / `RetrieveAll` with progress + cancellation.
- Read-only default; the tool never mutates audit settings; no destructive operations.
- Sensitive/sample values are never collected; storage is a clearly-labelled estimate.
- Settings round-trip (load on `Load`, save in `ClosingPlugin`).
- SDK-free compliance engine (`Analysis/AuditModels.cs`, `SensitivityHeuristics.cs`, `AuditComplianceRules.cs`)
  is deterministic and unit-tested.
- nuspec id/version/description/tags correct; the tool packs cleanly with its Excel/PDF export chain.
</content>
