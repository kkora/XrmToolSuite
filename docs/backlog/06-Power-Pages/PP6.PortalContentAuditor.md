# Portal Content Auditor — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 6 (Power Pages), item 6. Not in pack file.
> **Suggested tag:** `PP6` · **Suggested project:** `XrmToolSuite.PortalContentAuditor`
> **Overlaps:** Web Template Dependency Analyzer (PP2) resolves template→snippet/file references — PP6 reuses that reference resolution to find unused/missing snippets and unreferenced web files, but focuses on content quality (orphan/stale pages, duplicate URLs, SEO gaps) rather than refactor impact. Portal Metadata Explorer (PP5) supplies the shared retrieval layer. Do not re-implement Liquid parsing; consume PP2's reference index.
> **Value/priority (my read):** Medium — strong pre-release/handoff value (content cleanup checklist), but findings are content-hygiene heuristics rather than hard errors; overlaps meaningfully with PP2's reference resolution.

## Notes
- Data sources (dual schema): `adx_webpage`/`mspp_webpage` (title, partial URL, statecode, parent, publishing state, meta fields where present), `adx_contentsnippet`/`mspp_contentsnippet`, `adx_webfile`/`mspp_webfile`, `adx_weblink`/`adx_weblinkset` for navigation, `adx_pagetemplate`/`mspp_pagetemplate`, plus PP2's template-reference index for snippet/file usage.
- SEO/alt-text/meta-description checks are best-effort against metadata that Power Pages stores (page title, meta description where available, image alt attributes in template/content HTML). Where a field is not present in the schema, report the check as not-applicable, not a failure.
- Read-only auditor — never edits or deletes pages, snippets, or files. The output is a cleanup checklist, not automated remediation. No destructive ops.
- Missing tables / one-model-only environments degrade to informational, never throw.
- Shared-core reuse: shared Power Pages retrieval layer (PP5), PP2 reference index, `Service.RetrieveAll`, progress + cancellation, settings round-trip (website + staleness threshold), and the shared reporting/export module. Content rules engine stays UI-free and SDK-free for unit testing.

---

## EPIC-PP6 — Audit Power Pages content quality and publishing readiness
> **As** an ADM (content owner), **I want** an audit of pages, snippets, files, links, and SEO metadata, **so that** I can clean up a site before a production release or client handoff.

**Outcome:** a content quality score, page/snippet/web-file findings grids, a broken-reference panel, an SEO/content-quality panel, a cleanup checklist, and Excel/PDF/Word/JSON/CSV/HTML exports — across `adx_` and `mspp_` schemas.

---

## FEAT-PP6-1 — Page audit `[Planned]`
- **US-PP6.1.1** `[Planned]` **As** an ADM, **I want** orphan pages and pages not linked in navigation detected, **so that** I find unreachable content.
  - **AC:** Pages with no parent and not referenced by any web link/navigation → Medium in a page-findings grid; both schemas supported; retrieval off the UI thread with progress.
- **US-PP6.1.2** `[Planned]` **As** an ADM, **I want** duplicate partial URLs and inactive/stale pages detected, **so that** I resolve routing conflicts and remove dead content.
  - **AC:** Pages sharing a partial URL under the same parent → High; inactive or unmodified-beyond-threshold pages → Low/Medium with the staleness threshold from settings.
- **US-PP6.1.3** `[Planned]` **As** an ADM, **I want** broken internal page references detected, **so that** I fix dead links between pages.
  - **AC:** Internal references (parent/redirect/link targets) pointing to a missing page → High with source and target.

## FEAT-PP6-2 — Snippet audit `[Planned]`
- **US-PP6.2.1** `[Planned]` **As** an ADM, **I want** missing referenced snippets detected, **so that** I create content that templates expect.
  - **AC:** Snippet keys referenced by templates (from PP2's index) with no matching record → High.
- **US-PP6.2.2** `[Planned]` **As** an ADM, **I want** unused and duplicate snippets detected, **so that** I clean up content.
  - **AC:** Snippets referenced nowhere → Low; snippets duplicated by name/language → Medium.

## FEAT-PP6-3 — Web-file audit `[Planned]`
- **US-PP6.3.1** `[Planned]` **As** an ADM, **I want** web files not referenced by any page/template detected, **so that** I remove unused assets.
  - **AC:** Files absent from the page/template reference index → Low/Medium in a web-file findings grid; both schemas supported.
- **US-PP6.3.2** `[Planned]` **As** an ADM, **I want** broken web-file references detected, **so that** I fix missing assets.
  - **AC:** Page/template references to a file that does not exist → High with the referencing record.

## FEAT-PP6-4 — SEO & publishing-readiness `[Planned]`
- **US-PP6.4.1** `[Planned]` **As** an ADM, **I want** missing page titles and missing meta descriptions detected where available, **so that** I improve SEO.
  - **AC:** Pages with empty title → Medium; empty meta description (where the schema exposes it) → Low; not-applicable where the field is absent.
- **US-PP6.4.2** `[Planned]` **As** an ADM, **I want** missing image alt-text patterns and unpublished/draft-like configuration detected, **so that** I flag pre-publish gaps.
  - **AC:** Images in content/templates without alt attributes → Medium (static pattern match); draft/unpublished-state pages → Info/Low.

## FEAT-PP6-5 — Score, checklist & export `[Planned]`
- **US-PP6.5.1** `[Planned]` **As** an MGR, **I want** a content quality score with a band, **so that** I can gauge release readiness.
  - **AC:** Weighted severities produce a 0–100 score with a Low/Medium/High band; scoring engine SDK-free and unit-tested.
- **US-PP6.5.2** `[Planned]` **As** an ADM, **I want** a content cleanup checklist, **so that** I have an ordered task list.
  - **AC:** Each finding maps to a checklist item with the affected record and recommended action.
- **US-PP6.5.3** `[Planned]` **As** an MGR, **I want** the content audit report exported, **so that** I can share it for sign-off.
  - **AC:** Excel/PDF/Word/JSON/CSV/HTML exports come from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; supports `adx_` and `mspp_` schemas; read-only default; export formats Excel/PDF/Word/JSON/CSV/HTML.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; content rules engine UI-free and unit-tested; missing fields/tables degrade to not-applicable/informational, never throw.
- Testing skeleton under `testing/PortalContentAuditor/` when implementation starts; SDK-free content-rule/scoring logic covered by `testing/UnitTests`.
