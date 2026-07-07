# Team Permission Explorer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 2. Not in pack file.
> **Suggested tag:** `SEC02` · **Suggested project:** `XrmToolSuite.TeamPermissionExplorer`
> **Overlaps:** Shares the effective-privilege core with Privilege Gap Analyzer (SEC01) and Security Matrix Generator (SEC06). Team-inheritance findings feed Environment Governance Score (SEC09).
> **Value/priority (my read):** High — owner/access/AAD-group/BU teams are the least-understood security surface; a clear per-team view is rare and valuable.

## Notes
- Core tables: `team` (`teamtype`: owner / access / AAD security-group / AAD office-group), `teammembership`, `teamroles`, `role`, `roleprivileges`, `systemuser`, `businessunit`; owned records via `ownerid`/`owningteam` per table.
- "Users inheriting team permissions" = team members × the team's role privileges; resolve to effective scope per table.
- Risk rules: no members, no roles, over-privileged (Global/Deep on many tables), duplicate role assignments (same role via multiple teams), inactive/orphaned teams (disabled or zero-member + zero-owned-records).
- Read-only. Load teams and memberships via `RetrieveAll`; expand roles/privileges off the UI thread with progress + cancellation. Cache metadata; clear on `UpdateConnection`.
- Reuse the SEC01 privilege engine rather than re-deriving effective privileges.

---

## EPIC-SEC02 — Make team access, membership, and inheritance visible and reviewable
> **As** a SEC lead / ADM, **I want** to see what every team can access and which users inherit that access, **so that** I can find over-privileged, empty, and orphaned teams.

**Outcome:** a per-team profile (members, roles, effective privileges, owned records, inheriting users) plus a risk-findings list, exportable for review.

---

## FEAT-SEC02-1 — Browse and filter teams `[Planned]`
- **US-SEC02.1.1** `[Planned]` **As** an ADM, **I want** a list of all teams filterable by team type (owner / access / AAD group / BU), **so that** I can focus on one team model.
  - **AC:** Team list loads via `RetrieveAll`; filter and selection persist in settings.
- **US-SEC02.1.2** `[Planned]` **As** a SEC lead, **I want** to search teams by name/BU, **so that** I find the relevant team fast.

## FEAT-SEC02-2 — Team detail: members and roles `[Planned]`
- **US-SEC02.2.1** `[Planned]` **As** an ADM, **I want** a members grid and an assigned-roles grid per team, **so that** I know who is in it and what it grants.
  - **AC:** Grids load lazily on selection; counts shown in the team header.
- **US-SEC02.2.2** `[Planned]` **As** a SEC lead, **I want** an effective table-privilege matrix for the team, **so that** I see resolved access, not just role names.

## FEAT-SEC02-3 — Inheritance and ownership `[Planned]`
- **US-SEC02.3.1** `[Planned]` **As** a SEC lead, **I want** the list of users inheriting each team's permissions, **so that** I understand the true reach of a team's roles.
- **US-SEC02.3.2** `[Planned]` **As** an ADM, **I want** a summary of team-owned records by table, **so that** I can gauge dependency before changing a team.
  - **AC:** Owned-record counts use aggregate/count queries, not full retrieves.

## FEAT-SEC02-4 — Risk findings `[Planned]`
- **US-SEC02.4.1** `[Planned]` **As** a SEC lead, **I want** teams flagged for no members, no roles, over-privilege, duplicate roles, and inactive/orphaned status, **so that** I get a cleanup shortlist.
  - **AC:** Each finding carries a severity (Critical/High/Medium/Low/Info) and the evidence behind it.
- **US-SEC02.4.2** `[Planned]` **As** an ADM, **I want** the rules to degrade a failed query to an informational finding, **so that** one blocked table does not abort the scan.

## FEAT-SEC02-5 — Compare teams `[Planned]`
- **US-SEC02.5.1** `[Planned]` **As** an ADM, **I want** to diff two teams' effective privileges and roles, **so that** I can consolidate duplicates.

## FEAT-SEC02-6 — Export `[Planned]`
- **US-SEC02.6.1** `[Planned]` **As** a SEC lead, **I want** to export the team security report to Excel/PDF/CSV/HTML, **so that** I can share it with audit.
  - **AC:** Export runs off the UI thread with progress and cancellation.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/Tools/TeamPermissionExplorer/ when implementation starts.
