# Usage Analytics — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 12 (Reporting & Analytics), item 6. Not in pack file.
> **Suggested tag:** `RPT06` · **Suggested project:** `XrmToolSuite.UsageAnalytics`
> **Overlaps:** Overlaps the candidate Admin Environment Health Dashboard (component inventory) and Security Licensing Usage Analyzer (SEC08, inactive users with access). NOTE: keep this usage/adoption-focused (active vs stale, component activity), leave licensing cost analysis to SEC08. Feeds the Executive Dashboard (RPT01) usage/inventory KPIs.
> **Value/priority (my read):** Medium — "what's actually used vs stale" is valuable to ADM/MGR for cleanup and optimization, but Dataverse exposes weak first-party usage telemetry via SDK (no per-app usage counts), so much is inferred from modified dates / status and marked "where available". Useful but data-constrained.

## Notes
- Aggregates activity/usage indicators from metadata rather than true telemetry: active/inactive users, role assignments, team membership, recently modified records/components, flow status/ownership, plugin trace usage where available.
- Native usage telemetry is limited via SDK — treat app/module access counts and plugin-trace usage as "where available"; infer staleness from `modifiedon`/status and label inferred results.
- Stale/unused detection rules: components not modified within a configurable window, inactive users still holding roles, disabled flows, unreferenced web resources — deterministic and configurable thresholds.
- Signals: `systemuser` (isdisabled/lastlogin where available), `teammembership`/`team`, `role`/`systemuserroles`, `solutioncomponent`, `workflow`/flow rows, `plugintracelog` where available, record counts via `RetrieveTotalRecordCount` where available — all via Service.RetrieveAll off the UI thread, sampled cheaply (no `ColumnSet(true)`).
- Usage score is a deterministic roll-up (adoption vs staleness); keep detection/scoring engine UI-free and unit-testable. Local snapshot storage (no credentials) for usage-over-time; charts as inline SVG/PNG.
- Read-only; heavy scans run off the UI thread via RunAsync/WorkAsync with progress + cancellation; unavailable sources degrade to Info, never abort.

---

## EPIC-RPT06 — Show how the environment is actually used and where assets are stale
> **As** an ADM / MGR, **I want** usage indicators for users, teams, roles, apps, flows, and components, **so that** I can find inactive users, stale assets, and optimization opportunities.

**Outcome:** a usage dashboard with user/team panels, a component-usage grid, an app/solution usage panel, a stale-asset panel, a usage score, recommendations, and an exportable usage report.

---

## FEAT-RPT06-1 — User & team usage `[Planned]`
- **US-RPT06.1.1** `[Planned]` **As** an ADM, **I want** active/inactive users with their role assignments and team membership, **so that** I can see who has access and whether they're active.
  - **AC:** Retrieval uses Service.RetrieveAll off the UI thread with progress and cancellation; inactive detection uses status/last-login where available and is labeled when inferred.
- **US-RPT06.1.2** `[Planned]` **As** a SEC reviewer, **I want** inactive users who still hold roles flagged, **so that** I can revoke stale access.
  - **AC:** Flag is read-only (report only, no revoke action) with Critical/High/Medium/Low/Info severity.

## FEAT-RPT06-2 — Component & solution usage `[Planned]`
- **US-RPT06.2.1** `[Planned]` **As** an ARCH, **I want** a component-usage grid from recently modified components and solution activity, **so that** I can see what's active.
- **US-RPT06.2.2** `[Planned]` **As** an ADM, **I want** flow status/ownership and app/module access shown where available, **so that** I understand automation and app usage.
  - **AC:** Unavailable sources render as "unavailable", not zero.

## FEAT-RPT06-3 — Stale & unused detection `[Planned]`
- **US-RPT06.3.1** `[Planned]` **As** an ADM, **I want** stale/unused components identified against a configurable window, **so that** I can plan cleanup.
  - **AC:** Thresholds round-trip via settings; detection engine is UI-free and unit-tested.

## FEAT-RPT06-4 — Usage score & recommendations `[Planned]`
- **US-RPT06.4.1** `[Planned]` **As** an MGR, **I want** a usage score and a recommendation panel, **so that** I get a summarized adoption/optimization view.
  - **AC:** Score is deterministic and explainable from the usage indicators.

## FEAT-RPT06-5 — Snapshots & trend `[Planned]`
- **US-RPT06.5.1** `[Planned]` **As** an MGR, **I want** usage saved locally and trended, **so that** I can track adoption over time.
  - **AC:** Snapshots persist locally with no credentials or personal data beyond aggregate counts.

## FEAT-RPT06-6 — Export `[Planned]`
- **US-RPT06.6.1** `[Planned]` **As** an ADM, **I want** to export the usage analytics report to Excel/PDF/CSV/HTML, **so that** I can share findings and cleanup plans.
  - **AC:** Export runs off the UI thread with progress; charts embedded; personal data minimized/masked where appropriate.

## Definition of Done
- Follows suite conventions; read-only default; inferred data labeled; snapshots stored locally (no credentials); detection/scoring engine UI-free and unit-tested; export formats: Excel, PDF, CSV, HTML, JSON, PNG, SVG.
- Testing skeleton under testing/UsageAnalytics/ when implementation starts.
