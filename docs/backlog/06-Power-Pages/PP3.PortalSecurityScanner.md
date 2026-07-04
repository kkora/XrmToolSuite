# Portal Security Scanner — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 6 (Power Pages), item 3. Not in pack file.
> **Suggested tag:** `PP3` · **Suggested project:** `XrmToolSuite.PortalSecurityScanner`
> **Overlaps:** Deployment Risk Analyzer hints at anonymous-access risk during Power Pages readiness, and Portal Health Analyzer (PP1) surfaces a security summary. This tool owns the deep, dedicated security analysis (web-role matrix, table-permission matrix, sensitive-table exposure, remediation checklist). PP1's security cards should link here; do not duplicate the deep logic in PP1.
> **Value/priority (my read):** High — Power Pages exposes Dataverse data to external/anonymous users, so misconfiguration is a direct data-leak/compliance risk; security scanning is the highest-stakes PP capability.

## Notes
- Data sources (dual schema): `adx_webrole`/`mspp_webrole` (incl. anonymous/authenticated flags), `adx_entitypermission`/`mspp_tablepermission` (scope: Global/Contact/Account/Parent/Self; read/write/create/delete/append/appendto rights; parent/child links), `adx_website`/`mspp_website` authentication settings, `adx_sitesetting`/`mspp_sitesetting` (Authentication/Registration/CAPTCHA/upload settings), `adx_entityform`/`adx_webform`/`mspp_basicform`, `adx_entitylist`/`mspp_list`, `adx_webpage`/`mspp_webpage`.
- Severity rules (from source): anonymous write/delete = Critical; anonymous read to sensitive table = Critical; broad authenticated delete = High; form/list without permission = High; missing parent permission where required = Medium/High; duplicate/conflicting permissions = Medium.
- "Sensitive table" is heuristic/config-driven (e.g. `contact`, `account`, `systemuser`, `annotation`, custom tables on a configurable watchlist) — findings are advisory; let users edit the watchlist via settings.
- Read-only tool — never changes roles, permissions, or settings. Remediation is a checklist, not an automated fix. No destructive ops.
- Missing tables / one-model-only environments degrade to informational, never throw.
- Shared-core reuse: `Service.RetrieveAll`, progress + cancellation, settings round-trip (website + sensitive-table watchlist, no credentials), and the shared reporting/export module. The scoring + severity rules engine stays UI-free and SDK-free for unit testing.

---

## EPIC-PP3 — Scan a Power Pages website for data-exposure and permission risks
> **As** a SEC engineer, **I want** a scanner that evaluates web roles, table permissions, anonymous access, and authentication settings, **so that** I can catch external data-exposure risks before they reach production.

**Outcome:** a portal security score, a web-role matrix, a table-permission matrix, anonymous-access and sensitive-exposure findings with severities (Critical/High/Medium/Low/Info), a remediation checklist, and Excel/PDF/Word/JSON/CSV/HTML exports — across `adx_` and `mspp_` schemas.

---

## FEAT-PP3-1 — Web-role & authentication analysis `[Planned]`
- **US-PP3.1.1** `[Planned]` **As** a SEC engineer, **I want** all web roles listed with their anonymous/authenticated flags, **so that** I understand the site's role model.
  - **AC:** Roles load via `Service.RetrieveAll` off the UI thread with progress; both `adx_webrole` and `mspp_webrole` supported, anonymous and authenticated roles clearly marked.
- **US-PP3.1.2** `[Planned]` **As** a SEC engineer, **I want** website authentication settings analyzed, **so that** I can spot gaps (open registration, missing external auth).
  - **AC:** Authentication/registration site settings are evaluated against a baseline; risky values flagged with severity and the setting name.

## FEAT-PP3-2 — Table-permission analysis `[Planned]`
- **US-PP3.2.1** `[Planned]` **As** a SEC engineer, **I want** a table-permission matrix (table × scope × rights × role), **so that** I can see who can do what.
  - **AC:** Matrix built from `adx_entitypermission`/`mspp_tablepermission`, showing scope and read/write/create/delete/append rights per role.
- **US-PP3.2.2** `[Planned]` **As** a SEC engineer, **I want** broad Global-scope read/write/delete grants flagged, **so that** I catch over-permissive rules.
  - **AC:** Global scope + write/delete → High; broad authenticated delete → High per severity rules.
- **US-PP3.2.3** `[Planned]` **As** a SEC engineer, **I want** missing parent permissions and duplicate/conflicting permissions detected, **so that** I fix incomplete or ambiguous scoping.
  - **AC:** Parent/child scope requiring a parent permission that is absent → Medium/High; permissions colliding on table+role → Medium.

## FEAT-PP3-3 — Anonymous & sensitive-data exposure `[Planned]`
- **US-PP3.3.1** `[Planned]` **As** a SEC engineer, **I want** anonymous write/delete access flagged Critical, **so that** the worst exposures top the list.
  - **AC:** Any permission granting the anonymous role write/delete → Critical with table and rights shown.
- **US-PP3.3.2** `[Planned]` **As** a SEC engineer, **I want** anonymous read to sensitive tables flagged Critical, **so that** I catch PII/data leaks.
  - **AC:** Anonymous read on a watchlisted sensitive table → Critical; watchlist editable via settings.
- **US-PP3.3.3** `[Planned]` **As** a SEC engineer, **I want** public pages exposing forms/lists without proper permissions detected, **so that** I close unguarded data surfaces.
  - **AC:** A form/list on an anonymously-accessible page with no backing table permission → High.

## FEAT-PP3-4 — Site-setting & upload risks `[Planned]`
- **US-PP3.4.1** `[Planned]` **As** a SEC engineer, **I want** disabled CAPTCHA/anti-bot settings flagged where applicable, **so that** I harden public forms.
  - **AC:** Relevant CAPTCHA/anti-bot site settings evaluated; disabled-where-expected → Medium/High.
- **US-PP3.4.2** `[Planned]` **As** a SEC engineer, **I want** file-upload security risks flagged, **so that** I prevent unsafe uploads.
  - **AC:** Upload-related settings (allowed types/size/anonymous upload) checked against a baseline; risky values flagged with severity.

## FEAT-PP3-5 — Score, remediation & export `[Planned]`
- **US-PP3.5.1** `[Planned]` **As** an MGR, **I want** a portal security score with a band, **so that** I can report posture.
  - **AC:** Weighted severities produce a 0–100 score with a Low/Medium/High band; scoring engine is SDK-free and unit-tested.
- **US-PP3.5.2** `[Planned]` **As** a SEC engineer, **I want** a remediation checklist, **so that** I have an ordered action plan.
  - **AC:** Each finding maps to a remediation item with the affected role/table/permission and recommended change.
- **US-PP3.5.3** `[Planned]` **As** an MGR, **I want** the security report exported, **so that** I can hand it to auditors.
  - **AC:** Excel/PDF/Word/JSON/CSV/HTML exports come from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; supports `adx_` and `mspp_` schemas; read-only default (no permission/role/setting changes); export formats Excel/PDF/Word/JSON/CSV/HTML.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; severity/scoring rules engine UI-free and unit-tested; missing tables degrade to informational, never throw.
- Testing skeleton under `testing/PortalSecurityScanner/` when implementation starts; SDK-free severity/scoring logic covered by `testing/UnitTests`.
