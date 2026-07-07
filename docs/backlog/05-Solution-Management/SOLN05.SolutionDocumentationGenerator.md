# Solution Documentation Generator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 5 (Solution Management), item 5. Also in pack file (`prompt/2...`) idea #11 'Solution Documentation Generator' — same tool.
> **Suggested tag:** `SOLN05` · **Suggested project:** `XrmToolSuite.SolutionDocumentationGenerator`
> **Overlaps:** Overlaps Documentation-section candidates (any tool that emits solution/metadata docs). Reuse the shipped **Solution Complexity Score** / **Solution Knowledge Graph** component inventory and dependency graph as the documentation data source and diagram input rather than re-scanning. Diagram/ERD generation overlaps Dependency Heatmap (SOLN02) rendering — share the pure-managed rendering helper.
> **Value/priority (my read):** High — solution documentation is universally missing/stale and demanded for audit, onboarding, and client handoff; a one-click generator is a strong, broadly-useful deliverable.

## Notes
- Core data: `solution` + `solutioncomponent` (+ `msdyn_solutioncomponentsummary`) for the inventory; per-type metadata via `RetrieveEntityRequest`/`RetrieveAllEntitiesRequest` for tables/columns/relationships/choices; FormXML for forms, FetchXML/LayoutXML for views/charts, `webresource` for JS, `pluginassembly`/`plugintype`/`sdkmessageprocessingstep`/image for plugins, `workflow` for business rules + classic/modern flows, `customapi`, `environmentvariabledefinition`, `connectionreference`, `appmodule`, and security roles in the solution; `publisher` for header/branding.
- Dependency and ERD diagrams reuse the Solution Knowledge Graph dependency data and a pure-managed renderer (GDI+ bitmap / hand-written SVG) — no external diagram NuGet unless it follows the ship-in-Plugins-root rule.
- Read-only — the tool reads metadata and produces documents; it never modifies the solution.
- Retrieval off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress per section and honor cancellation; cache metadata per connection and clear on `UpdateConnection`.
- Keep the documentation-generation/template engine UI-free where possible so section rendering is testable; degrade unavailable component types (Power Pages, some flow detail) to a documented "not available" note rather than failing the whole document.
- Export is the heart of this tool: Word, PDF, Markdown, HTML, Excel, JSON — reuse the DeploymentRiskAnalyzer ClosedXML (Excel) and MigraDoc/PdfSharp (PDF/Word-ish) export chains per the sanctioned dependency exception; do not add new export libraries casually.

---

## EPIC-SOLN05 — Generate complete, current solution documentation on demand
> **As** a **TOOLDEV**, **I want** to generate full technical and business documentation for a Dataverse solution in one run, **so that** support, audit, onboarding, and client handoff always have accurate, formatted docs.

**Outcome:** a multi-section document (component inventory, tables/columns/relationships, forms/views/dashboards, apps, automation, plugins, web resources, custom APIs, env vars, connection refs, roles, dependency + ERD diagrams, release notes, architecture summary) rendered in a chosen mode and exported to Word/PDF/Markdown/HTML/Excel/JSON.

---

## FEAT-SOLN05-1 — Select solution, template, format, and sections `[Planned]`
- **US-SOLN05.1.1** `[Planned]` **As** a TOOLDEV, **I want** to pick a solution, a documentation mode, an output format, and which sections to include, **so that** I generate exactly the document I need.
  - **AC:** Documentation mode (Executive Summary … Full Solution Reference), output format, and a sections checklist are selectable; selection persists via Load/SaveSettings.
- **US-SOLN05.1.2** `[Planned]` **As** a TOOLDEV, **I want** a branding/settings panel, **so that** generated docs carry my org's header/logo/publisher info.
  - **AC:** Branding fields round-trip in settings and appear in the rendered document header; no credentials stored.

## FEAT-SOLN05-2 — Document data, schema, and automation components `[Planned]`
- **US-SOLN05.2.1** `[Planned]` **As** a TOOLDEV, **I want** tables, columns, relationships, and choices/option sets documented, **so that** the data model is fully described.
  - **AC:** Schema section lists each table with its columns (type, requirement, description), relationships, and global/ local option sets; sourced from entity metadata off the UI thread.
- **US-SOLN05.2.2** `[Planned]` **As** a TOOLDEV, **I want** forms, views, charts, dashboards, and apps documented, **so that** the UI layer is captured.
  - **AC:** FormXML/FetchXML/LayoutXML and app-module metadata are parsed into readable sections; unavailable detail is noted, not fatal.
- **US-SOLN05.2.3** `[Planned]` **As** a TOOLDEV, **I want** business rules, workflows/flows, plugins (assembly/type/step/image), web resources/JS, custom APIs, env vars, connection refs, and included security roles documented, **so that** automation and configuration are covered.
  - **AC:** Each of these is a distinct documented section with the key configuration; missing types degrade to a "not available" note.

## FEAT-SOLN05-3 — Diagrams and summaries `[Planned]`
- **US-SOLN05.3.1** `[Planned]` **As** an ARCH, **I want** dependency and ERD-style relationship diagrams generated, **so that** the document shows structure visually.
  - **AC:** Diagrams render from the Solution Knowledge Graph dependency/relationship data via the pure-managed renderer and embed into Word/PDF/HTML.
- **US-SOLN05.3.2** `[Planned]` **As** an ARCH, **I want** a component inventory, release notes, and an architecture summary generated, **so that** the document has executive-level framing.
  - **AC:** Inventory counts by component type; release notes and architecture summary sections are produced from the scanned data.

## FEAT-SOLN05-4 — Preview and generation progress `[Planned]`
- **US-SOLN05.4.1** `[Planned]` **As** a TOOLDEV, **I want** a preview and per-section progress while generating, **so that** I can verify content before exporting a large document.
  - **AC:** Generation reports progress per section and is cancellable; a preview panel shows rendered content before final export.

## FEAT-SOLN05-5 — Multi-format export `[Planned]`
- **US-SOLN05.5.1** `[Planned]` **As** a TOOLDEV, **I want** the document exported to Word, PDF, Markdown, HTML, Excel, and JSON, **so that** I can meet audit, handoff, and machine-readable needs.
  - **AC:** Export reuses the sanctioned ClosedXML/MigraDoc-PdfSharp chains; HTML is self-contained and theme-aware; JSON carries the structured inventory; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses Solution Complexity Score / Knowledge Graph data and the sanctioned export dependency chains.
- Read-only default; documentation/template engine stays UI-free where possible and degrades unavailable component types to documented notes.
- Export formats: Word, PDF, Markdown, HTML, Excel, JSON.
- Testing skeleton under testing/Tools/SolutionDocumentationGenerator/ when implementation starts.
