# Field Security Profiler — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 3. Not in pack file.
> **Suggested tag:** `SEC3` · **Suggested project:** `XrmToolSuite.FieldSecurityProfiler`
> **Overlaps:** Sensitive Data Scanner (SEC7) flags unsecured sensitive columns — SEC3 owns the secured-column analysis they cross-reference. Security Matrix Generator (SEC6) includes FSP mappings.
> **Value/priority (my read):** Medium-High — field security is genuinely hard to audit in-product; a matrix of secured columns × profiles × principals fills a real gap.

## Notes
- Core tables/metadata: `fieldsecurityprofile`, `fieldpermission` (Read/Update/Create + CanReadUnmasked), `systemuserprofiles`, `teamprofiles`; secured columns via `RetrieveEntityMetadata` where `AttributeMetadata.IsSecured == true`.
- "Secured column" is metadata; "who can access it" comes from profiles → users/teams via the profile-assignment intersects.
- Risk rules: sensitive-named columns not secured (cross-ref SEC7 patterns), secured columns granted to too many principals, profiles assigned to too many users, unused profiles (no members, no permissions), secured columns undocumented.
- Read-only. Metadata reads cached; profile/assignment reads via `RetrieveAll` off the UI thread with progress. Mask any sample values if ever shown.
- Keep detection rules UI-free and unit-testable against fixture metadata.

---

## EPIC-SEC3 — Audit field-level security coverage and exposure across the environment
> **As** a SEC lead / ADM, **I want** to see every secured column, its profiles, and who can read/update/create it, **so that** I can confirm sensitive fields are protected and not over-exposed.

**Outcome:** a secured-fields dashboard plus a column × profile × permission matrix and a recommendations list, exportable for compliance review.

---

## FEAT-SEC3-1 — Secured fields inventory `[Planned]`
- **US-SEC3.1.1** `[Planned]` **As** an ADM, **I want** a dashboard of all secured columns across tables, **so that** I have a single inventory of field-level security.
  - **AC:** Inventory built from cached entity metadata (`IsSecured`); table/type/managed shown per row.
- **US-SEC3.1.2** `[Planned]` **As** a SEC lead, **I want** to list all field security profiles with member and permission counts, **so that** I can see the profile landscape at a glance.

## FEAT-SEC3-2 — Permission matrix `[Planned]`
- **US-SEC3.2.1** `[Planned]` **As** a SEC lead, **I want** a matrix of secured column × profile showing Read/Update/Create (and unmasked-read), **so that** I can verify each column's exposure.
  - **AC:** Matrix loads via `RetrieveAll` off the UI thread with progress and cancellation.
- **US-SEC3.2.2** `[Planned]` **As** an ADM, **I want** a users/teams assignment grid per profile, **so that** I know exactly who each profile reaches.

## FEAT-SEC3-3 — Risk detection `[Planned]`
- **US-SEC3.3.1** `[Planned]` **As** a SEC lead, **I want** flags for sensitive-named columns without security, secured columns with overly broad access, profiles assigned to too many users, and unused profiles, **so that** I get a prioritized review list.
  - **AC:** Each finding carries severity (Critical/High/Medium/Low/Info) and evidence.
- **US-SEC3.3.2** `[Planned]` **As** an ADM, **I want** secured columns missing from documentation flagged, **so that** governance records stay complete.

## FEAT-SEC3-4 — Compare profiles `[Planned]`
- **US-SEC3.4.1** `[Planned]` **As** an ADM, **I want** to diff two field security profiles, **so that** I can consolidate near-duplicates.

## FEAT-SEC3-5 — Recommendations `[Planned]`
- **US-SEC3.5.1** `[Planned]` **As** a SEC lead, **I want** remediation suggestions (secure this column, trim these assignments), **so that** each finding has a next step.
  - **AC:** Recommendations are read-only; the tool never changes profiles.

## FEAT-SEC3-6 — Export `[Planned]`
- **US-SEC3.6.1** `[Planned]` **As** a SEC lead, **I want** to export the field security matrix to Excel/PDF/CSV/HTML, **so that** I can hand it to auditors.
  - **AC:** Sample values (if any) are masked in every export; export runs off the UI thread.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/FieldSecurityProfiler/ when implementation starts.
