# User Access Heatmap — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 10. Not in pack file.
> **Suggested tag:** `SEC10` · **Suggested project:** `XrmToolSuite.UserAccessHeatmap`
> **Overlaps:** Reuses the effective-privilege core from Privilege Gap Analyzer (SEC1) and team inheritance from Team Permission Explorer (SEC2); shares heatmap UI patterns with Sharing Analyzer (SEC4); access-intensity signals feed Environment Governance Score (SEC9); inactive-high-access overlaps Licensing Usage Analyzer (SEC8).
> **Value/priority (my read):** Medium-High — a visual "who has broad/admin-like access" view answers a common security question fast, though it leans on the same privilege engine as SEC1/SEC6.

## Notes
- Core tables: `systemuser`, `systemuserroles`, `teammembership`, `teamroles`, `role`, `roleprivileges`, `privilege`, `businessunit`; resolve each user's effective privilege depth per table (union of direct + team-inherited, deepest scope).
- Access scoring model (from source): Global = highest weight, Deep = high, Local = medium, Basic = low; Delete/Assign/Share weighted higher risk; inactive user with high access = Critical. Produce a per-user access-intensity score.
- Heatmaps: user × table, user × privilege, and by business unit. Highlight excessive-access users, admin-equivalent users, inactive-but-high-access users.
- Read-only. Role/privilege expansion is heavy — page via `RetrieveAll`, run off the UI thread with progress + cancellation, cache metadata (clear on `UpdateConnection`).
- Reuse the SEC1 effective-privilege engine; keep the scoring engine UI-free and unit-testable against fixtures.

---

## EPIC-SEC10 — Visualize access intensity to spot over-privileged and admin-like users fast
> **As** a SEC lead / ADM, **I want** a heatmap of user access intensity across tables, privileges, and business units, **so that** I can immediately see who holds broad or admin-equivalent access.

**Outcome:** per-user access-intensity scores and heatmaps (user×table, user×privilege, by BU) with excessive/admin/inactive-high-access users highlighted, exportable for review.

---

## FEAT-SEC10-1 — Effective access calculation `[Planned]`
- **US-SEC10.1.1** `[Planned]` **As** a SEC lead, **I want** each user's effective privilege depth per table computed from direct and team-inherited roles, **so that** the heatmap reflects real access.
  - **AC:** Team-inherited access is included and resolved to the deepest scope; computed off the UI thread with progress.
- **US-SEC10.1.2** `[Planned]` **As** an ADM, **I want** an access-intensity score per user using the weighted model (Global>Deep>Local>Basic; Delete/Assign/Share higher), **so that** users are comparable by a single number.
  - **AC:** Scoring is deterministic and explainable from the contributing privileges.

## FEAT-SEC10-2 — Heatmap visualizations `[Planned]`
- **US-SEC10.2.1** `[Planned]` **As** a SEC lead, **I want** a user×table heatmap, **so that** I can see access concentration across the schema.
- **US-SEC10.2.2** `[Planned]` **As** a SEC lead, **I want** user×privilege and per-business-unit heatmaps, **so that** I can view access from different angles.

## FEAT-SEC10-3 — Filters `[Planned]`
- **US-SEC10.3.1** `[Planned]` **As** an ADM, **I want** to filter by user, table, business unit, and privilege, **so that** large environments produce readable views.
  - **AC:** Filter selections persist in settings.

## FEAT-SEC10-4 — Risk highlighting `[Planned]`
- **US-SEC10.4.1** `[Planned]` **As** a SEC lead, **I want** excessive-access users, admin-equivalent users, and inactive users with high access highlighted, **so that** the riskiest accounts stand out.
  - **AC:** Inactive-user-with-high-access is scored Critical per the model; each highlight carries its evidence.

## FEAT-SEC10-5 — User detail & compare `[Planned]`
- **US-SEC10.5.1** `[Planned]` **As** an ADM, **I want** a user detail panel and a two-user comparison, **so that** I can investigate and justify a specific account.

## FEAT-SEC10-6 — Export `[Planned]`
- **US-SEC10.6.1** `[Planned]` **As** a SEC lead, **I want** to export the access heatmap report to Excel/PDF/CSV/HTML, **so that** I can share access findings with audit.
  - **AC:** Export runs off the UI thread with progress and cancellation.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; access scores explainable from listed evidence; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/UserAccessHeatmap/ when implementation starts.
