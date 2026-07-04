# Licensing Usage Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 8. Not in pack file.
> **Suggested tag:** `SEC8` · **Suggested project:** `XrmToolSuite.LicensingUsageAnalyzer`
> **Overlaps:** Inactive-user + high-privilege signals overlap with User Access Heatmap (SEC10) and Team Permission Explorer (SEC2); feeds access-management sub-score to Environment Governance Score (SEC9).
> **Value/priority (my read):** Medium — real cost-optimization value, but with a hard caveat: Dataverse cannot see true license SKUs, so all output is an estimate, not billing truth.

## Notes
- **Key caveat:** Dataverse exposes activity/role/app signals, NOT authoritative Microsoft 365 license assignments. Every optimization figure must be clearly labelled **estimated** and separated from official licensing data.
- Core tables: `systemuser` (`isdisabled`, `accessmode`, `lastname`/`fullname`), `systemuserroles`, `teammembership`, app/module access via `appmodule`/`appmoduleroles`; last-activity indicators where available (e.g. `lastaccessedon` where surfaced, audit-derived activity, or "no signal available" honestly).
- Heuristics: inactive users with high privileges, users with roles but no recent activity, users assigned apps they don't use, service accounts (application users / `accessmode`), admin users (System Administrator role).
- Read-only. All reads via `RetrieveAll` off the UI thread with progress + cancellation. Cache metadata; clear on `UpdateConnection`.
- Keep the estimation/scoring engine UI-free and unit-testable; be explicit where a signal is unavailable rather than guessing.

---

## EPIC-SEC8 — Estimate license-optimization opportunities from Dataverse-visible activity
> **As** an ADM / MGR, **I want** to see users, their roles/apps, and activity indicators, **so that** I can estimate where licenses may be over-assigned to inactive or unused accounts.

**Outcome:** a user-usage dashboard with inactive/high-privilege flags and clearly-labelled estimated optimization opportunities, exportable for cost review.

---

## FEAT-SEC8-1 — User inventory `[Planned]`
- **US-SEC8.1.1** `[Planned]` **As** an ADM, **I want** enabled and disabled users listed with access mode and type, **so that** I have the full user population.
  - **AC:** Users load via `RetrieveAll`; enabled/disabled and service/application users distinguished.
- **US-SEC8.1.2** `[Planned]` **As** an ADM, **I want** last-activity indicators shown where available, **so that** I can gauge recency of use.
  - **AC:** Where no reliable activity signal exists, the tool says so rather than implying inactivity.

## FEAT-SEC8-2 — Roles and app access `[Planned]`
- **US-SEC8.2.1** `[Planned]` **As** an ADM, **I want** each user's security roles, team memberships, and app/module access shown, **so that** I can see what each account is provisioned for.
  - **AC:** Loaded off the UI thread with progress and cancellation.

## FEAT-SEC8-3 — Inactive & over-provisioned detection `[Planned]`
- **US-SEC8.3.1** `[Planned]` **As** an MGR, **I want** inactive users with high privileges and users with roles but no recent activity flagged, **so that** I can target reviews.
  - **AC:** Each flag carries a severity and the evidence (roles held, last-activity/no-signal).
- **US-SEC8.3.2** `[Planned]` **As** an ADM, **I want** service accounts and admin users identified, **so that** I exclude or scrutinize them appropriately.

## FEAT-SEC8-4 — Optimization estimate `[Planned]`
- **US-SEC8.4.1** `[Planned]` **As** an MGR, **I want** an estimated optimization opportunity (count of candidate reclaimable assignments), **so that** I can size potential savings.
  - **AC:** Output is labelled estimated and explicitly separated from official Microsoft licensing data.

## FEAT-SEC8-5 — Recommendations `[Planned]`
- **US-SEC8.5.1** `[Planned]` **As** an ADM, **I want** per-user recommendations (review/disable/reduce roles), **so that** each candidate has a suggested action.
  - **AC:** Recommendations are read-only; the tool disables nothing.

## FEAT-SEC8-6 — Export `[Planned]`
- **US-SEC8.6.1** `[Planned]` **As** an MGR, **I want** to export the license usage report to Excel/PDF/CSV/HTML, **so that** I can share it with licensing/procurement.
  - **AC:** Export carries the estimated-data disclaimer; runs off the UI thread with progress.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; estimates clearly separated from official licensing data; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/LicensingUsageAnalyzer/ when implementation starts.
