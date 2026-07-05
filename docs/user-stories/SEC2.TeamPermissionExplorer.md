# Team Permission Explorer — User Stories

> **Status:** Implemented (v1.2026.7.2).
> **Tag:** `SEC2` · **Project:** `XrmToolSuite.TeamPermissionExplorer`
> **Spec:** `docs/backlog/02-Security-Governance/SEC2.TeamPermissionExplorer.md`
> **Reuse:** Shares the effective-privilege engine (`XrmToolSuite.Core.Privileges`) with Privilege Gap
> Analyzer (SEC1) — effective privileges are resolved by `PrivilegeEngine.ResolveEffective`, not re-derived.

Personas: **SEC** (security lead), **ADM** (Dataverse admin), **TOOLDEV**.

---

## EPIC-SEC2 — Make team access, membership, and inheritance visible and reviewable

> **As** a SEC lead / ADM, **I want** to see what every team can access and which users inherit that
> access, **so that** I can find over-privileged, empty, and orphaned teams.

**Outcome:** a per-team profile (members, roles, effective privileges, owned records, inheriting users)
plus a risk-findings list, exportable for review.

---

## FEAT-SEC2-0 — Scaffold & shared wiring `[Done]`

- **US-SEC2.0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background execution via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Loads in XTB, connects, runs reads off the UI thread via `RunAsync`/`RetrieveAll`, persists
    settings on close, carries the shared Help button. No template leftovers (real `UserName`/`HelpUrl`, no sample-data command).

## FEAT-SEC2-1 — Browse and filter teams `[Done]`

- **US-SEC2.1.1** `[Done]` **As** an ADM, **I want** a list of all teams filterable by team type
  (owner / access / AAD security / AAD office), **so that** I can focus on one team model.
  - **AC:** Team list loads via `RetrieveAll`; the team-type filter and last selection persist in settings.
- **US-SEC2.1.2** `[Done]` **As** a SEC lead, **I want** to search teams by name/BU, **so that** I find
  the relevant team fast.
  - **AC:** In-memory search box filters the loaded grid by name or business unit (instant, no re-query).

## FEAT-SEC2-2 — Team detail: members and roles `[Done]`

- **US-SEC2.2.1** `[Done]` **As** an ADM, **I want** a members grid and an assigned-roles grid per team,
  **so that** I know who is in it and what it grants.
  - **AC:** Grids load on selection; member and role counts show in the team header. Members are fetched
    lazily off the UI thread.
- **US-SEC2.2.2** `[Done]` **As** a SEC lead, **I want** an effective table-privilege matrix for the team,
  **so that** I see resolved access, not just role names.
  - **AC:** Matrix is `PrivilegeEngine.ResolveEffective` over the team's grants (deepest scope per privilege).

## FEAT-SEC2-3 — Inheritance and ownership `[Done]`

- **US-SEC2.3.1** `[Done]` **As** a SEC lead, **I want** the list of users inheriting each team's
  permissions, **so that** I understand the true reach of a team's roles.
  - **AC:** Inheriting users = team members (teammembership → systemuser fullname); shown on the Members tab.
- **US-SEC2.3.2** `[Done]` **As** an ADM, **I want** a summary of team-owned records by table, **so that**
  I can gauge dependency before changing a team.
  - **AC:** Owned-record counts use aggregate/count FetchXML grouped by `owningteam` (no full retrieves);
    a failed table degrades to 0.

## FEAT-SEC2-4 — Risk findings `[Done]`

- **US-SEC2.4.1** `[Done]` **As** a SEC lead, **I want** teams flagged for no members, no roles,
  over-privilege, duplicate roles, and inactive/orphaned status, **so that** I get a cleanup shortlist.
  - **AC:** Each finding carries a severity and the evidence behind it. Rules (in `TeamRiskRules`):
    no members (non-AAD) → Medium; no roles → Medium; over-privileged (Deep/Global on ≥10 privileges) →
    High; duplicate role (same role via multiple teams / listed twice) → Low; orphaned (0 members AND
    0 owned records) → Medium; otherwise Info "No team risks detected".
- **US-SEC2.4.2** `[Done]` **As** an ADM, **I want** the rules to degrade a failed query to an
  informational finding, **so that** one blocked table does not abort the scan.
  - **AC:** The collector never throws; per-source failures become progress notes / Info findings.

## FEAT-SEC2-5 — Compare teams `[Done]`

- **US-SEC2.5.1** `[Done]` **As** an ADM, **I want** to diff two teams' effective privileges and roles,
  **so that** I can consolidate duplicates.
  - **AC:** Compare picks a second team and shows `PrivilegeEngine.Diff` plus roles unique to each side.

## FEAT-SEC2-6 — Export `[Done]`

- **US-SEC2.6.1** `[Done]` **As** a SEC lead, **I want** to export the team security report to
  Excel/PDF/CSV/HTML, **so that** I can share it with audit.
  - **AC:** Export runs off the UI thread via a `SaveFileDialog`; a `ReportModel` drives the shared
    Excel/PDF exporters; CSV/HTML via BCL writers. Only names and counts are emitted (no secrets).

## Definition of Done

- Follows suite conventions; read-only; export formats Excel, PDF, CSV, HTML.
- SDK-free risk rules unit-tested in `testing/UnitTests/TeamPermissionExplorerTests.cs`.
- Testing artifacts under `testing/TeamPermissionExplorer/`.
