# Portal Metadata Explorer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 6 (Power Pages), item 5. Not in pack file.
> **Suggested tag:** `PP5` · **Suggested project:** `XrmToolSuite.PortalMetadataExplorer`
> **Overlaps:** Portal Health Analyzer (PP1), Security Scanner (PP3), and Performance Analyzer (PP4) all retrieve the same portal tables — PP5 is the plain browse/search/inventory tool with no scoring. It is the natural home for the shared Power Pages retrieval/normalization layer that PP1/PP3/PP4/PP6/PP7 reuse. Solution Knowledge Graph handles solution components, not portal-content browsing.
> **Value/priority (my read):** Medium — high day-to-day utility as a "one place to find any portal record," and it factors out the shared retrieval layer the other PP tools depend on, but it is analysis-light (browse/export rather than findings).

## Notes
- Data sources (dual schema, full breadth): websites, web pages, web templates, page templates, content snippets, site settings, web roles, table/entity permissions, basic forms, multistep/web forms + steps, lists, web files, web links + web link sets, redirects, access control rules — across `adx_*` and `mspp_*` naming.
- This tool defines the shared Power Pages retrieval + normalization model (detect schema, map `adx_`/`mspp_` fields into one neutral record shape) that PP1/PP3/PP4/PP6/PP7 consume. Keep that layer UI-free in Shared/Core.
- Read-only explorer — never creates/updates/deletes. No destructive ops. Export is inventory, not change.
- Missing tables / one-model-only environments degrade to informational (empty node with a note), never throw.
- Shared-core reuse: `Service.RetrieveAll`, progress + cancellation, settings round-trip (website + last search/tree state), and the shared reporting/export module.
- Search runs client-side over retrieved metadata plus targeted server queries for GUID/URL lookups; no scoring or rules engine in this tool.

---

## EPIC-PP5 — Give one searchable explorer for all Power Pages metadata
> **As** a MAKER, **I want** a single tree + search + detail view over every portal metadata table, **so that** I can understand a website's structure without querying each Dataverse table by hand.

**Outcome:** a metadata navigation tree, a search panel (name/URL/partial URL/template/snippet/setting/GUID), a record grid, a record detail viewer, a relationship/dependency panel, and a full metadata-inventory export — across `adx_` and `mspp_` schemas.

---

## FEAT-PP5-1 — Website selection & metadata tree `[Planned]`
- **US-PP5.1.1** `[Planned]` **As** a MAKER, **I want** to select a website and see a navigation tree of its metadata categories, **so that** I can drill into any record type.
  - **AC:** Tree loads via `Service.RetrieveAll` off the UI thread with progress; nodes cover pages, templates, page templates, snippets, settings, roles, permissions, forms, multistep forms/steps, lists, web files, web links/sets, redirects, access rules; both schemas supported with a schema badge.
- **US-PP5.1.2** `[Planned]` **As** a MAKER, **I want** counts on each tree node and my selection remembered, **so that** I navigate efficiently.
  - **AC:** Each node shows a record count; selected website + expanded nodes persist via settings.

## FEAT-PP5-2 — Record grid & detail viewer `[Planned]`
- **US-PP5.2.1** `[Planned]` **As** a MAKER, **I want** a grid of records for the selected category, **so that** I can scan and sort them.
  - **AC:** Grid lists records with key columns per type, sortable/filterable in-grid, populated from the normalized model.
- **US-PP5.2.2** `[Planned]` **As** a MAKER, **I want** a detail viewer for a selected record, **so that** I can inspect all its fields.
  - **AC:** Detail panel shows the record's fields (neutralized across `adx_`/`mspp_`) including its GUID and the underlying schema names.

## FEAT-PP5-3 — Search & filter `[Planned]`
- **US-PP5.3.1** `[Planned]` **As** a MAKER, **I want** to search by name, URL, or partial URL, **so that** I can find a page/record fast.
  - **AC:** Search matches across name/URL/partial-URL fields and highlights results in the tree/grid; runs off the UI thread when it needs server queries.
- **US-PP5.3.2** `[Planned]` **As** a MAKER, **I want** to search by template, snippet, setting, or GUID, **so that** I can locate any referenced object.
  - **AC:** Search resolves a GUID to its record and type, and matches template/snippet/setting names across both schemas.

## FEAT-PP5-4 — Relationships & dependencies `[Planned]`
- **US-PP5.4.1** `[Planned]` **As** a MAKER, **I want** parent/child relationships shown for a record, **so that** I understand site structure.
  - **AC:** The relationship panel shows parent page, child pages, owning website, and related templates/forms/lists for the selected record.
- **US-PP5.4.2** `[Planned]` **As** a MAKER, **I want** associated Dataverse table/entity references shown, **so that** I know what data a form/list/permission binds to.
  - **AC:** Detail/relationship panel lists the bound entity logical name(s) for forms, lists, and permissions.

## FEAT-PP5-5 — Inventory export `[Planned]`
- **US-PP5.5.1** `[Planned]` **As** an ARCH, **I want** the full portal metadata inventory exported, **so that** I can document or diff a site.
  - **AC:** Excel/JSON/CSV/HTML/Markdown exports of the complete inventory come from the shared reporting module and open on demand.
- **US-PP5.5.2** `[Planned]` **As** an ARCH, **I want** to export the currently filtered/selected category, **so that** I can share a focused slice.
  - **AC:** Export respects the active category/search filter and records which schema the data came from.

## Definition of Done
- Follows suite conventions; supports `adx_` and `mspp_` schemas; read-only default; export formats Excel/JSON/CSV/HTML/Markdown.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; the shared retrieval/normalization layer is UI-free and reusable by other PP tools; missing tables degrade to informational, never throw.
- Testing skeleton under `testing/PortalMetadataExplorer/` when implementation starts; SDK-free normalization/search logic covered by `testing/UnitTests`.
