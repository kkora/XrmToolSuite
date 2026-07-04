# Web Template Dependency Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 6 (Power Pages), item 2. Not in pack file.
> **Suggested tag:** `PP2` · **Suggested project:** `XrmToolSuite.WebTemplateDependencyAnalyzer`
> **Overlaps:** Solution Knowledge Graph visualizes solution-component dependencies but does not parse Liquid or Power Pages web-template internals; this tool is the portal-content dependency analyzer. Reuse the graph-model/visualization patterns where practical; the Liquid parser is net-new.
> **Value/priority (my read):** High — refactoring or deleting a web template blind is a common cause of portal breakage; a Liquid-aware dependency + impact view is a strong differentiator.

## Notes
- Data sources (dual schema): `adx_webtemplate`/`mspp_webtemplate` (the Liquid source), plus `adx_contentsnippet`/`mspp_contentsnippet`, `adx_sitesetting`/`mspp_sitesetting`, `adx_webfile`/`mspp_webfile`, `adx_webpage`/`mspp_webpage`, `adx_pagetemplate`/`mspp_pagetemplate`, `adx_weblink`/`adx_weblinkset` for reference resolution.
- Liquid parsing is text/static analysis: detect `{% include %}` / `{% block %}` / `{% render %}`, `{{ snippets['...'] }}`, `{{ settings['...'] }}`, `{% fetchxml %}` blocks, `entities`/`entityview`/`entitylist` table references, `weblinks`, web-file/URL paths, hardcoded GUIDs (regex), and hardcoded environment URLs. It is best-effort — dynamic/computed names may be unresolved; report those as informational.
- Read-only tool — never modifies templates or records. No destructive ops. Impact analysis is advisory only.
- Missing referenced snippets/settings/templates are the point of the tool — surface them as findings, not exceptions; missing tables degrade to informational.
- Shared-core reuse: `Service.RetrieveAll`, progress + cancellation, settings round-trip for last website/template, and the shared reporting/export module.
- The Liquid parser + dependency-graph builder stay UI-free (pure functions over template text + `IOrganizationService` lookups) so they can be unit-tested SDK-free and lifted into CI.

---

## EPIC-PP2 — Map web-template Liquid dependencies before refactor or deletion
> **As** a MAKER (portal developer), **I want** to see everything a web template references and everything that references it, **so that** I can change or delete templates without breaking the site.

**Outcome:** a parsed reference set per template, a dependency tree, a missing-dependency grid, an impact-analysis panel (what breaks if this changes), recommendations, and Excel/PDF/JSON/CSV/HTML/Markdown exports — across `adx_` and `mspp_` schemas.

---

## FEAT-PP2-1 — Template discovery & Liquid viewer `[Planned]`
- **US-PP2.1.1** `[Planned]` **As** a MAKER, **I want** web templates listed by website, **so that** I can pick one to analyze.
  - **AC:** Templates load via `Service.RetrieveAll` off the UI thread with progress; both `adx_webtemplate` and `mspp_webtemplate` are listed with a schema badge.
- **US-PP2.1.2** `[Planned]` **As** a MAKER, **I want** the selected template's Liquid source shown in a code viewer, **so that** I can read it in context.
  - **AC:** The Liquid source (from the schema's source column) renders in a read-only viewer; selection persists via settings.

## FEAT-PP2-2 — Liquid reference detection `[Planned]`
- **US-PP2.2.1** `[Planned]` **As** a MAKER, **I want** include/block/render references and content-snippet references detected, **so that** I know which fragments a template depends on.
  - **AC:** `{% include/block/render %}` targets and `snippets['...']` keys are extracted and listed; parser is a pure function with unit tests.
- **US-PP2.2.2** `[Planned]` **As** a MAKER, **I want** site-setting references, FetchXML blocks, and Dataverse table references detected, **so that** I understand the template's data dependencies.
  - **AC:** `settings['...']` keys, `{% fetchxml %}` blocks, and entity/table names in Liquid objects are extracted per template.
- **US-PP2.2.3** `[Planned]` **As** a MAKER, **I want** web-file references, page/page-template usage, and URL/path references detected, **so that** I see asset and navigation coupling.
  - **AC:** Web-file names/paths, page-template usage, and URL paths are extracted and mapped to resolved records where they exist (`adx_`/`mspp_`).

## FEAT-PP2-3 — Hygiene & anti-pattern detection `[Planned]`
- **US-PP2.3.1** `[Planned]` **As** a MAKER, **I want** hardcoded GUIDs and hardcoded environment URLs flagged, **so that** I remove non-portable values before moving between environments.
  - **AC:** GUID-shaped literals → Medium; absolute org/environment URLs → High, each with line/offset context.
- **US-PP2.3.2** `[Planned]` **As** a MAKER, **I want** references to missing snippets/settings/templates flagged, **so that** I catch broken dependencies.
  - **AC:** Each reference resolved against retrieved records; unresolved ones → High in a missing-dependency grid.
- **US-PP2.3.3** `[Planned]` **As** a MAKER, **I want** circular template includes detected where possible, **so that** I avoid infinite include loops.
  - **AC:** The include graph is cycle-checked; detected cycles → High listing the loop path; unresolved dynamic includes noted as informational.

## FEAT-PP2-4 — Dependency tree & impact analysis `[Planned]`
- **US-PP2.4.1** `[Planned]` **As** a MAKER, **I want** a dependency tree for the selected template, **so that** I can see its full reference chain.
  - **AC:** A tree/graph shows outbound references (snippets, settings, files, templates, tables) expandable by node.
- **US-PP2.4.2** `[Planned]` **As** a MAKER, **I want** reverse impact analysis (who uses this template), **so that** I know what breaks if I change or delete it.
  - **AC:** An impact panel lists pages, page templates, and templates that include/reference the selected template across `adx_`/`mspp_`.

## FEAT-PP2-5 — Recommendations & export `[Planned]`
- **US-PP2.5.1** `[Planned]` **As** a MAKER, **I want** a recommendation per finding, **so that** I get concrete next steps.
  - **AC:** Each finding carries a plain-language recommendation (e.g. replace GUID with lookup, create missing snippet).
- **US-PP2.5.2** `[Planned]` **As** an MGR, **I want** the dependency/impact report exported, **so that** I can attach it to a change request.
  - **AC:** Excel/PDF/JSON/CSV/HTML/Markdown exports come from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; supports `adx_` and `mspp_` schemas; read-only default; export formats Excel/PDF/JSON/CSV/HTML/Markdown.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; Liquid parser + graph builder UI-free and unit-tested; unresolved references degrade to informational, never throw.
- Testing skeleton under `testing/WebTemplateDependencyAnalyzer/` when implementation starts; SDK-free Liquid parsing/graph logic covered by `testing/UnitTests`.
