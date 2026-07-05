# Portal Health Analyzer — User Stories

> **Status:** Active — implemented (see status tags below). Area tag `PP1`.
> **Source:** ported from `docs/backlog/06-Power-Pages/PP1.PortalHealthAnalyzer.md`.
> **Engine:** a schema-normalized model + deterministic, SDK-free rule engine in
> `src/Tools/XrmToolSuite.PortalHealthAnalyzer/Analysis/` (`PortalModels.cs`, `PortalHealthRules.cs`),
> fed by an SDK collector (`PortalCollector.cs`) that detects and unifies the `adx_`/`mspp_` schemas.
> **Feasibility note:** scoring is **metadata/static analysis only** — no runtime page hits; findings
> describe configuration risk, not live availability. Read-only tool: it never creates/updates/deletes.

---

## EPIC-PP1 — Give portal admins a single health dashboard for a Power Pages website

> **As** an ADM (Portal Admin), **I want** one tool that inventories a website's configuration and scores
> its health, **so that** I can find broken config, security gaps, and deployment risks without inspecting
> each table by hand.

**Outcome:** an overall portal health score with a band, configuration summary cards, a categorized issue
grid with severities (Critical/High/Medium/Low/Info) and recommendations, and Excel/PDF/Word/JSON/CSV/HTML
exports — across both `adx_` and `mspp_` schemas from a live connection.

---

## FEAT-PP1-1 — Website discovery & selection

- **US-PP1.1.1** `[Done]` **As** an ADM, **I want** every Power Pages website in the environment listed,
  **so that** I can pick which one to analyze.
  - **AC:** Websites load via `Service.RetrieveAll` off the UI thread with progress/cancellation, listing
    both `adx_website` and `mspp_website` records with a schema badge (`[adx]` / `[mspp]`).
- **US-PP1.1.2** `[Done]` **As** an ADM, **I want** my last-selected website remembered, **so that** I
  resume where I left off.
  - **AC:** Selection persists via settings round-trip (`LoadSettings`/`SaveSettings`), storing website id +
    schema only (no credentials).

## FEAT-PP1-2 — Configuration retrieval & summary

- **US-PP1.2.1** `[Done]` **As** an ADM, **I want** the selected website's settings, roles, permissions,
  pages, templates, snippets, forms, lists, and web files retrieved, **so that** I have a complete inventory
  in one pass.
  - **AC:** All record sets load via `RunAsync`/`RetrieveAll` with progress against whichever schema the site
    uses; a table that isn't provisioned degrades to an informational finding and never throws.
- **US-PP1.2.2** `[Done]` **As** an ADM, **I want** configuration summary counts per record type, **so that**
  I can gauge site size at a glance.
  - **AC:** The summary shows counts for pages, templates, page templates, snippets, settings, roles,
    permissions, forms, lists, web files and redirects with the active schema labelled.

## FEAT-PP1-3 — Structural integrity checks

- **US-PP1.3.1** `[Done]` **As** an ADM, **I want** broken page relationships and pages without templates
  detected, **so that** I can fix rendering failures before release.
  - **AC:** Web pages with a missing parent, no page template, or a dangling page-template reference are
    flagged High with the record name/id.
- **US-PP1.3.2** `[Done]` **As** an ADM, **I want** inactive pages/templates and broken web-file references
  detected, **so that** I can clean up dead assets.
  - **AC:** Inactive (statecode-off) pages/templates → Medium; web files referenced by pages/templates but
    absent → High.
- **US-PP1.3.3** `[Done]` **As** an ADM, **I want** forms/lists referencing inactive Dataverse tables
  flagged, **so that** I catch broken data bindings.
  - **AC:** A basic/entity form or entity list bound to a non-existent or disabled table → High with the
    entity logical name (existence resolved via metadata).

## FEAT-PP1-4 — Site settings checks

- **US-PP1.4.1** `[Done]` **As** an ADM, **I want** missing required site settings detected, **so that** the
  portal has its baseline configuration.
  - **AC:** A curated required-settings list (per schema) is checked; absent settings → High with the setting
    name.
- **US-PP1.4.2** `[Done]` **As** an ADM, **I want** duplicate/conflicting site settings detected, **so that**
  I remove ambiguous config.
  - **AC:** Settings sharing a name within a website are grouped and flagged Medium, showing the conflicting
    values.

## FEAT-PP1-5 — Security surface checks (health-level summary)

- **US-PP1.5.1** `[Done]` **As** a SEC engineer, **I want** anonymous-access risks surfaced at a health
  level, **so that** I know a deeper scan is warranted.
  - **AC:** Table/entity permissions granting anonymous read/write/delete → per-permission High plus a
    Critical roll-up summary that points to the Portal Security Scanner for deep analysis.
- **US-PP1.5.2** `[Done]` **As** a SEC engineer, **I want** over-broad Global-scope permissions flagged,
  **so that** I catch grants that should be Contact/Account scoped.
  - **AC:** Permissions using Global scope → Medium with the table logical name.

## FEAT-PP1-6 — Health score, recommendations & export

- **US-PP1.6.1** `[Done]` **As** an MGR, **I want** an overall portal health score with a band, **so that** I
  can communicate readiness.
  - **AC:** Weighted severities produce a 0–100 score with a Low/Medium/High band on the dashboard; the score
    model is SDK-free, deterministic, and unit-tested. Any Critical finding forces the High band.
- **US-PP1.6.2** `[Done]` **As** an ADM, **I want** a recommendation per finding, **so that** I get concrete
  remediation steps.
  - **AC:** Each actionable finding carries a plain-language recommendation and the affected record.
- **US-PP1.6.3** `[Done]` **As** an MGR, **I want** Excel/PDF/Word/JSON/CSV/HTML exports of the health
  report, **so that** I can share it.
  - **AC:** All formats come from the shared reporting module (`ReportModel`) and open on demand off the UI
    thread.

---

## Definition of Done

- Follows suite conventions; supports `adx_` and `mspp_` schemas; read-only default; export formats
  Excel/PDF/Word/JSON/CSV/HTML.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; analyzers UI-free; missing tables
  degrade to informational, never throw.
- SDK-free scoring covered by `testing/UnitTests/PortalHealthAnalyzerTests.cs`; manual/live cases and export
  round-trips documented under `testing/PortalHealthAnalyzer/`.
