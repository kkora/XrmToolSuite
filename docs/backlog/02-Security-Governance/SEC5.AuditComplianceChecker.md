# Audit Compliance Checker — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 5. Related pack idea #18 'Audit Log Analyzer' (activity/storage-growth trends).
> **Suggested tag:** `SEC5` · **Suggested project:** `XrmToolSuite.AuditComplianceChecker`
> **Overlaps:** Audit-coverage sub-score feeds Environment Governance Score (SEC9); sensitive-table/field coverage cross-refs Sensitive Data Scanner (SEC7).
> **Value/priority (my read):** High — proving audit coverage is a hard compliance requirement, and audit gaps are otherwise invisible until an incident.

## Notes
- Settings/metadata: org audit flags (`organization.isauditenabled`), table-level `EntityMetadata.IsAuditEnabled`, column-level `AttributeMetadata.IsAuditEnabled`; activity via `audit` table (`RetrieveRecordChangeHistoryRequest` for a record; `audit` queries for trends) and `RetrieveAuditDetailsRequest`.
- Compliance rules: sensitive tables/fields without audit enabled, org audit off, retention gaps; risk activity rules: high-risk changes, after-hours changes, delete activity, security-role/privilege changes.
- Audit storage growth is estimated from audit record volume over time — clearly labelled as an estimate, not billed storage.
- Read-only. Audit queries can be heavy — scope by table/date, page via `RetrieveAll`, run off the UI thread with progress + cancellation. Mask changed values in exports.
- Keep the compliance-scoring model UI-free and unit-testable against fixtures.

---

## EPIC-SEC5 — Verify audit is configured and active where governance requires it
> **As** a SEC lead / MGR, **I want** to check org/table/column audit settings and analyze audit activity, **so that** I can prove coverage, find gaps, and monitor risky changes.

**Outcome:** an audit coverage report (org/table/column), an activity-trend analysis, a compliance readiness score, and prioritized remediation — exportable for auditors.

---

## FEAT-SEC5-1 — Audit settings coverage `[Planned]`
- **US-SEC5.1.1** `[Planned]` **As** a SEC lead, **I want** to see org-, table-, and column-level audit settings in one grid, **so that** I know where auditing is on or off.
  - **AC:** Settings read from cached metadata + org record; managed/custom shown per row.
- **US-SEC5.1.2** `[Planned]` **As** a SEC lead, **I want** sensitive tables and fields without audit enabled flagged, **so that** I can close coverage gaps.
  - **AC:** Sensitivity heuristics are shared with SEC7 patterns; each gap carries a severity.

## FEAT-SEC5-2 — Audit activity analysis `[Planned]`
- **US-SEC5.2.1** `[Planned]` **As** a SEC lead, **I want** audit activity summarized by table, user, and date, **so that** I understand where change volume concentrates.
  - **AC:** Activity queries are date-scoped and paged; run off the UI thread with progress and cancellation.
- **US-SEC5.2.2** `[Planned]` **As** an ADM, **I want** high-risk changes highlighted (deletes, after-hours edits, security-role/privilege changes), **so that** I can spot suspicious activity.

## FEAT-SEC5-3 — Storage growth trend `[Planned]`
- **US-SEC5.3.1** `[Planned]` **As** an MGR, **I want** an estimated audit storage growth trend, **so that** I can anticipate retention/cost pressure.
  - **AC:** Growth is clearly labelled estimated (derived from audit volume), not official storage billing.

## FEAT-SEC5-4 — Compliance score `[Planned]`
- **US-SEC5.4.1** `[Planned]` **As** an MGR, **I want** a compliance readiness score (0-100) with category breakdown, **so that** I have an executive summary of audit health.
  - **AC:** Score is deterministic from the coverage + activity rules and explainable from listed evidence.

## FEAT-SEC5-5 — Recommendations `[Planned]`
- **US-SEC5.5.1** `[Planned]` **As** a SEC lead, **I want** prioritized remediation (enable audit on these tables/fields), **so that** each gap has a next step.
  - **AC:** Recommendations are read-only; the tool never changes audit settings.

## FEAT-SEC5-6 — Export `[Planned]`
- **US-SEC5.6.1** `[Planned]` **As** an MGR, **I want** to export the audit compliance report to Excel/PDF/CSV/HTML, **so that** I can present it to auditors and leadership.
  - **AC:** Changed/sample values are masked in exports; export runs off the UI thread.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/AuditComplianceChecker/ when implementation starts.
