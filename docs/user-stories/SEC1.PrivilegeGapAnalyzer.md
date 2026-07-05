# Privilege Gap Analyzer — User Stories

> **Status:** Implemented (Phase A). SDK-free effective-privilege engine + WinForms tool.
> **Tag:** `SEC1` · **Project:** `XrmToolSuite.PrivilegeGapAnalyzer`
> **Source backlog:** [`docs/backlog/02-Security-Governance/SEC1.PrivilegeGapAnalyzer.md`](../backlog/02-Security-Governance/SEC1.PrivilegeGapAnalyzer.md)
> **Read-only tool.** It never edits roles; recommendations are suggestions only. Sensitive principal names are masked in exports.

## Notes
- Core tables/messages: `role`, `roleprivileges`, `privilege`, `systemuserroles`, `teamroles`, `systemuser`, `team`, `teammembership`; `RetrieveEntity` with `EntityFilters.Privileges` for `SecurityPrivilegeMetadata`.
- Effective-privilege calc = union of direct-role + team-inherited privileges, resolved to the deepest access scope per privilege (Basic/Local/Deep/Global), then mapped against the operation's required privilege(s).
- Append/AppendTo needs BOTH privileges across the two related tables — modelled explicitly (append A→B = Append on A AND AppendTo on B); a common silent gap.
- The resolution engine (`Privileges/PrivilegeEngine.cs` + `PrivilegeModels.cs`) is UI-free and SDK-free so it is unit-testable without a live org and liftable into a console/CI check. The Dataverse collector (`PrivilegeCollector.cs`) is kept separate.

---

## EPIC-SEC1 — Diagnose exactly why a principal can or cannot perform an operation
> **As** a SEC lead / ADM, **I want** to compute a principal's effective privileges for a table+operation and see the precise missing privilege or scope, **so that** I can resolve access issues without trial-and-error role edits.

**Outcome:** for any user/team/role + table + operation, a verdict (Allowed / Denied) with the specific missing privilege, insufficient-scope, or append-pair reason and a recommended role change.

---

## FEAT-SEC1-1 — Select principal, table, and operation `[Done]`
- **US-SEC1.1.1** `[Done]` **As** a SEC lead, **I want** to pick a user, team, or role as the subject, **so that** I can diagnose access for the exact principal in question.
  - **AC:** Selector lists users/teams/roles loaded via `RetrieveAll`; selection persists in settings.
- **US-SEC1.1.2** `[Done]` **As** an ADM, **I want** to pick a table and an operation (Create, Read, Write, Delete, Append, Append To, Assign, Share), **so that** I scope the diagnosis to one action.
  - **AC:** Operation maps to the concrete required privilege(s), including the Append/AppendTo pair (a related-table selector appears for Append).

## FEAT-SEC1-2 — Effective privilege calculation `[Done]`
- **US-SEC1.2.1** `[Done]` **As** a SEC lead, **I want** the tool to union direct-role and team-inherited privileges and resolve the deepest scope per privilege, **so that** the effective level reflects reality.
  - **AC:** Team-inherited privileges are labelled with their source team/role; the resolved entry is flagged `ViaTeam` only when there is no direct grant.
- **US-SEC1.2.2** `[Done]` **As** an ADM, **I want** an effective-privilege summary grid (privilege × scope × source), **so that** I see the whole picture, not just the failing one.

## FEAT-SEC1-3 — Gap detection & explanation `[Done]`
- **US-SEC1.3.1** `[Done]` **As** a SEC lead, **I want** the verdict classified as Access allowed / Missing privilege / Insufficient scope / Team inheritance only / Append mismatch / Business unit boundary, **so that** the cause is unambiguous.
  - **AC:** Each denial lists the exact privilege name and the scope required vs held.
- **US-SEC1.3.2** `[Done]` **As** an ADM, **I want** a plain-language explanation panel, **so that** I can paste the reason into a support ticket.

## FEAT-SEC1-4 — Recommendations `[Done]`
- **US-SEC1.4.1** `[Done]` **As** a SEC lead, **I want** a recommended fix (which role to grant / which scope to raise), **so that** remediation is one clear step.
  - **AC:** Recommendations are read-only suggestions; the tool never edits roles.

## FEAT-SEC1-5 — Compare two principals `[Done]`
- **US-SEC1.5.1** `[Done]` **As** an ADM, **I want** to diff two users/teams/roles side by side, **so that** I can see why one works and the other does not.
  - **AC:** Diff highlights privileges present in one but not the other, and differing scopes.

## FEAT-SEC1-6 — Export `[Done]`
- **US-SEC1.6.1** `[Done]` **As** a SEC lead, **I want** to export the gap report to Excel/PDF/CSV/JSON/HTML, **so that** I can attach findings to a change request.
  - **AC:** Export runs from a Save dialog; principal names are masked; Excel/PDF/JSON reuse the shared `ReportModel` (one Finding for the gap). Excel (ClosedXML) and native PDF (PdfSharp/MigraDoc-GDI) ship the export dependency chains in the Plugins root, mirroring Deployment Risk Analyzer.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; export formats: Excel, PDF, CSV, JSON, HTML.
- Testing artifacts under `testing/PrivilegeGapAnalyzer/`; SDK-free engine covered by xUnit in `testing/UnitTests/`.
