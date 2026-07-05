# Privilege Gap Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 1. Related pack idea #2 'Security Role Simulator' (effective-privilege calc / why-denied).
> **Suggested tag:** `SEC01` · **Suggested project:** `XrmToolSuite.PrivilegeGapAnalyzer`
> **Overlaps:** Security Matrix Generator (SEC06) and User Access Heatmap (SEC10) both read role privileges — share the effective-privilege core. Environment Governance Score (SEC09) can consume the gap findings.
> **Value/priority (my read):** High — "why can't this user do X?" is a daily support question with no first-class answer in the product.

## Notes
- Core tables/messages: `role`, `roleprivileges`, `privilege`, `systemuserroles`, `teamroles`, `systemuser`, `team`, `teammembership`, `businessunit`; `RetrieveEntityMetadata` for entity privilege metadata (`SecurityPrivilegeMetadata`).
- Effective-privilege calc = union of direct-role + team-inherited privileges, resolved to the deepest access level per privilege (Basic/Local/Deep/Global) then mapped against the requested operation's required privilege(s).
- Append/Append To needs BOTH privileges on the two related tables — model the pair explicitly; a common silent gap.
- Read-only tool. All reads via `Service.RetrieveAll`; heavy role expansion off the UI thread via `RunAsync`. Cache metadata; clear on `UpdateConnection`.
- Keep the privilege-resolution engine UI-free so it is liftable into a console/CI check and unit-testable without a live org.

---

## EPIC-SEC01 — Diagnose exactly why a principal can or cannot perform an operation
> **As** a SEC lead / ADM, **I want** to compute a principal's effective privileges for a table+operation and see the precise missing privilege or scope, **so that** I can resolve access issues without trial-and-error role edits.

**Outcome:** for any user/team/role + table + operation, a verdict (Allowed / Denied) with the specific missing privilege, insufficient-scope, or append-pair reason and a recommended role change.

---

## FEAT-SEC01-1 — Select principal, table, and operation `[Planned]`
- **US-SEC01.1.1** `[Planned]` **As** a SEC lead, **I want** to pick a user, team, or role as the subject, **so that** I can diagnose access for the exact principal in question.
  - **AC:** Selector lists users/teams/roles loaded via `RetrieveAll`; searchable; selection persists in settings.
- **US-SEC01.1.2** `[Planned]` **As** an ADM, **I want** to pick a table and an operation (Create, Read, Write, Delete, Append, Append To, Assign, Share), **so that** I scope the diagnosis to one action.
  - **AC:** Operation maps to the concrete required privilege(s), including the Append/Append To pair.

## FEAT-SEC01-2 — Effective privilege calculation `[Planned]`
- **US-SEC01.2.1** `[Planned]` **As** a SEC lead, **I want** the tool to union direct-role and team-inherited privileges and resolve the deepest scope per privilege, **so that** the effective level reflects reality.
  - **AC:** Team-inherited privileges are labelled with their source team/role.
- **US-SEC01.2.2** `[Planned]` **As** an ADM, **I want** an effective-privilege summary grid (privilege × scope), **so that** I see the whole picture, not just the failing one.

## FEAT-SEC01-3 — Gap detection & explanation `[Planned]`
- **US-SEC01.3.1** `[Planned]` **As** a SEC lead, **I want** the verdict classified as Access allowed / Access denied / Missing privilege / Insufficient scope / Team inheritance issue / Append mismatch / Business unit boundary, **so that** the cause is unambiguous.
  - **AC:** Each denial lists the exact privilege name and the scope required vs held.
- **US-SEC01.3.2** `[Planned]` **As** an ADM, **I want** a plain-language explanation panel, **so that** I can paste the reason into a support ticket.

## FEAT-SEC01-4 — Recommendations `[Planned]`
- **US-SEC01.4.1** `[Planned]` **As** a SEC lead, **I want** a recommended fix (which role to grant / which scope to raise), **so that** remediation is one clear step.
  - **AC:** Recommendations are read-only suggestions; the tool never edits roles.

## FEAT-SEC01-5 — Compare two principals `[Planned]`
- **US-SEC01.5.1** `[Planned]` **As** an ADM, **I want** to diff two users or roles side by side, **so that** I can see why one works and the other does not.
  - **AC:** Diff highlights privileges present in one but not the other, with scope.

## FEAT-SEC01-6 — Export `[Planned]`
- **US-SEC01.6.1** `[Planned]` **As** a SEC lead, **I want** to export the gap report to Excel/PDF/CSV/HTML, **so that** I can attach findings to a change request.
  - **AC:** Export runs off the UI thread with progress and cancellation.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/PrivilegeGapAnalyzer/ when implementation starts.
