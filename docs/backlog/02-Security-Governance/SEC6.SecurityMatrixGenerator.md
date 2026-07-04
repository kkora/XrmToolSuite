# Security Matrix Generator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 6. Not in pack file.
> **Suggested tag:** `SEC6` · **Suggested project:** `XrmToolSuite.SecurityMatrixGenerator`
> **Overlaps:** Reuses the effective-privilege core from Privilege Gap Analyzer (SEC1) and team logic from Team Permission Explorer (SEC2); includes FSP mappings from Field Security Profiler (SEC3). This is the documentation/export sibling of those analytical tools.
> **Value/priority (my read):** Medium-High — exportable, current security documentation is a perennial audit request that is otherwise built by hand and immediately stale.

## Notes
- Core tables: `role`, `roleprivileges`, `privilege`, `systemuserroles`, `teamroles`, `systemuser`, `team`, `businessunit`, `fieldsecurityprofile`; privilege depth/scope from the privilege records; miscellaneous (non-table) privileges included.
- Deliverables are matrices: role→table, user→role, team→role, table→privilege, plus role-vs-role comparison. Matrices can be large — build them from cached metadata + paged `RetrieveAll` reads.
- No risk scoring required (this is documentation-first) — accuracy and completeness matter most; still surface obvious anomalies via reused SEC1/SEC2 rules if cheap.
- Read-only. Generation off the UI thread with progress + cancellation. Export is the headline feature — Excel, PDF, CSV, HTML (reuse the DeploymentRiskAnalyzer ClosedXML + PdfSharp/MigraDoc chains already sanctioned in the suite).
- Keep matrix-building UI-free and unit-testable so a console/CI export is possible.

---

## EPIC-SEC6 — Produce complete, current, exportable Dataverse security documentation
> **As** a SEC lead / auditor (SEC), **I want** to generate role/table/privilege/user/team matrices from the live environment, **so that** security documentation is accurate and review-ready on demand.

**Outcome:** a set of exportable security matrices (role→table, user→role, team→role, table→privilege) with privilege depth/scope and role comparison, in Excel/PDF/CSV/HTML.

---

## FEAT-SEC6-1 — Role and privilege inventory `[Planned]`
- **US-SEC6.1.1** `[Planned]` **As** a SEC lead, **I want** all security roles with their table privileges and depth/scope listed, **so that** I have the raw material for every matrix.
  - **AC:** Inventory built from cached metadata + `RetrieveAll`; miscellaneous privileges included.
- **US-SEC6.1.2** `[Planned]` **As** an auditor, **I want** role assignments by user and by team shown, **so that** I can trace who holds each role.

## FEAT-SEC6-2 — Matrix generation `[Planned]`
- **US-SEC6.2.1** `[Planned]` **As** a SEC lead, **I want** a role→table privilege matrix (with depth/scope), **so that** I can see each role's reach in one grid.
  - **AC:** Generation runs off the UI thread with progress and cancellation.
- **US-SEC6.2.2** `[Planned]` **As** an auditor, **I want** user→role and team→role matrices, **so that** assignment coverage is documented.
- **US-SEC6.2.3** `[Planned]` **As** a SEC lead, **I want** a table→privilege matrix, **so that** I can review access from the table's perspective.

## FEAT-SEC6-3 — Filtering `[Planned]`
- **US-SEC6.3.1** `[Planned]` **As** an auditor, **I want** to filter matrices by table, module, or role, **so that** large environments produce reviewable slices.
  - **AC:** Filter selections persist in settings.

## FEAT-SEC6-4 — Field security mappings `[Planned]`
- **US-SEC6.4.1** `[Planned]` **As** a SEC lead, **I want** field security profile mappings and team role inheritance included, **so that** the matrix reflects column-level and inherited access too.

## FEAT-SEC6-5 — Compare roles `[Planned]`
- **US-SEC6.5.1** `[Planned]` **As** a SEC lead, **I want** to diff two roles' privileges side by side, **so that** I can justify or consolidate them.

## FEAT-SEC6-6 — Export `[Planned]`
- **US-SEC6.6.1** `[Planned]` **As** an auditor, **I want** to export every matrix to Excel, PDF, CSV, and HTML, **so that** documentation lands in whatever format the review needs.
  - **AC:** Export runs off the UI thread with progress; large matrices scroll/paginate rather than block.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/SecurityMatrixGenerator/ when implementation starts.
