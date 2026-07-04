# HTML Documentation Portal — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 11 (Documentation), item 5. Not in pack file (except ERD/Doc generators relate to pack #11).
> **Suggested tag:** `DOC5` · **Suggested project:** `XrmToolSuite.HtmlDocumentationPortal`
> **Overlaps:** **Solution Documentation Generator** (SOLN5 candidate) — STRONG overlap: this is the static-HTML-site renderer over the same extracted model. **Markdown / Word Documentation Generators** (DOC3/DOC4) are sibling renderers — share the extracted model + section engine. Diagram/dependency pages overlap **Architecture Diagram Generator** (DOC1) / **ERD Generator** (DOC2) / **Solution Knowledge Graph** (SHIPPED) — embed their SVG/Mermaid. **Deployment Risk Analyzer** / **Attribute Auditor** (SHIPPED) already emit self-contained HTML — reuse that pattern.
> **Value/priority (my read):** Medium-High — a searchable, offline, browser-based portal is a strong client-handoff/training/audit artifact and needs no server; value is high but overlaps heavily with the shared doc model, so it is mostly a static-site-generator layer.

## Notes
- Shared extracted-metadata model: reuse the Solution Documentation Generator (SOLN5) / Environment Inventory extraction — this tool is a *static-site generator* over that model, not a new scanner.
- Output/export: emit a **static folder** of HTML/CSS/JS (or a ZIP) with an `index.html` entry point; must browse **offline** (no server, no external CDN — inline or bundle all assets, matching the self-contained-HTML pattern the Deployment Risk Analyzer and Attribute Auditor already use). Search is a **client-side index** (prebuilt JSON + JS), so it works from `file://`.
- Diagram strategy: embed **SVG** (from DOC1/DOC2's pure-managed renderer) and/or **Mermaid** rendered client-side; dependency-graph pages reuse the Solution Knowledge Graph data. No native diagram NuGet.
- Core data: `solution`/`solutioncomponent`, tables/columns/relationships/choices, forms/views/dashboards, `appmodule`, security roles, `workflow` (rules + flows), plugins, `customapi`, `environmentvariabledefinition`, `connectionreference`, `webresource`, Power Pages metadata (where available), PCF, `publisher` for branding. Also emit a **downloadable JSON** metadata dump.
- Read-only — reads metadata and writes a static site to a chosen folder/ZIP; never modifies the environment. Heavy export chains (ClosedXML/MigraDoc) are not required for the HTML path; only `System.IO.Packaging`/`System.IO.Compression` for the ZIP.
- Off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress per page/section and honor cancellation; cache per connection, clear on `UpdateConnection`. Keep the site/template + search-index engine UI-free so page rendering is testable; degrade unavailable component types to a documented "not available" page/note.

---

## EPIC-DOC5 — Generate a searchable, offline HTML documentation portal
> **As** an **ARCH**, **I want** to generate a static, searchable HTML portal for a solution or environment, **so that** support, training, audit, and client teams browse accurate documentation in a browser without a live app or server.

**Outcome:** a self-contained static site (home, navigation, client-side search, component/table/form/view/plugin/flow/Power Pages/security/integration pages, dependency-graph pages, downloadable JSON) with responsive layout and optional dark/light mode, exported as a folder or ZIP with `index.html`.

---

## FEAT-DOC5-1 — Scope, template, and section selection `[Planned]`
- **US-DOC5.1.1** `[Planned]` **As** an ARCH, **I want** to pick a solution or environment, a portal template, and an output folder/ZIP, **so that** the portal covers the right scope in my chosen packaging.
  - **AC:** Scope loads off the UI thread via `RunAsync`; scope, template, and output target persist via Load/SaveSettings.
- **US-DOC5.1.2** `[Planned]` **As** an ARCH, **I want** a section checklist, **so that** I include only the pages I need.
  - **AC:** Section selection round-trips in settings and controls which pages are generated.

## FEAT-DOC5-2 — Site structure and pages `[Planned]`
- **US-DOC5.2.1** `[Planned]` **As** an ARCH, **I want** a home page, navigation, and a responsive layout with `index.html`, **so that** the portal is immediately browsable.
  - **AC:** Generated site opens from `index.html`, navigation links resolve locally, and layout is responsive; no external CDN dependency.
- **US-DOC5.2.2** `[Planned]` **As** a TOOLDEV, **I want** component pages for tables, forms/views, plugins, flows, Power Pages, security, and integrations, **so that** each component has a dedicated page.
  - **AC:** Each component type renders a page from the extracted model; unavailable types degrade to a documented "not available" page/note.

## FEAT-DOC5-3 — Search, diagrams, and dependency pages `[Planned]`
- **US-DOC5.3.1** `[Planned]` **As** an ARCH, **I want** a client-side search index, **so that** I can find components without a server.
  - **AC:** A prebuilt JSON index + JS provides search that works from `file://` offline.
- **US-DOC5.3.2** `[Planned]` **As** an ARCH, **I want** diagram and dependency-graph pages plus component relationship pages, **so that** structure is navigable.
  - **AC:** Diagrams embed as SVG/Mermaid (reusing DOC1/DOC2 and Solution Knowledge Graph data) and render offline.

## FEAT-DOC5-4 — Branding, theming, and JSON export `[Planned]`
- **US-DOC5.4.1** `[Planned]` **As** an ARCH, **I want** branding, custom CSS, and dark/light mode (where feasible), **so that** the portal matches corporate style and reader preference.
  - **AC:** Branding/custom CSS round-trip in settings; a theme toggle switches dark/light without external assets.
- **US-DOC5.4.2** `[Planned]` **As** a DEVOPS, **I want** a downloadable JSON metadata file included in the portal, **so that** the data behind the docs is machine-readable.
  - **AC:** A structured JSON metadata dump is generated and linked from the portal.

## FEAT-DOC5-5 — Packaging and preview `[Planned]`
- **US-DOC5.5.1** `[Planned]` **As** an ARCH, **I want** to export as a static folder or ZIP and preview the result, **so that** I can publish internally and verify before distribution.
  - **AC:** Export produces a self-contained folder or ZIP off the UI thread with per-page progress and cancellation; a preview link opens `index.html`.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses the shared extracted model and the self-contained-HTML pattern from Deployment Risk Analyzer / Attribute Auditor.
- Read-only default; site/template + search-index engine stays UI-free where possible and degrades unavailable component types to documented notes; portal browses offline with no external CDN.
- Output formats: static HTML site (folder or ZIP) with `index.html`, client-side search, and downloadable JSON.
- Testing skeleton under testing/HtmlDocumentationPortal/ when implementation starts.
