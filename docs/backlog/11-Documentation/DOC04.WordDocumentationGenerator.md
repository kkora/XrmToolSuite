# Word Documentation Generator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 11 (Documentation), item 4. Not in pack file (except ERD/Doc generators relate to pack #11).
> **Suggested tag:** `DOC04` · **Suggested project:** `XrmToolSuite.WordDocumentationGenerator`
> **Overlaps:** **Solution Documentation Generator** (SOLN05 candidate) — STRONG overlap: this is the DOCX/PDF renderer over the same extracted model. **Markdown / HTML Documentation Generators** (DOC03/DOC05) are sibling renderers — share the extracted model + section engine. Diagram insertion overlaps **Architecture Diagram Generator** (DOC01) / **ERD Generator** (DOC02) — embed their SVG/PNG output. **Deployment Risk Analyzer** (SHIPPED) already produces PDF/Excel via the sanctioned chains — reuse those.
> **Value/priority (my read):** Medium-High — polished Word/PDF is specifically demanded by clients, auditors, and managers for handoff/audit sign-off, but DOCX generation needs the OpenXml dependency and template work, so effort is higher than the Markdown/HTML siblings.

## Notes
- Shared extracted-metadata model: reuse the Solution Documentation Generator (SOLN05) / Environment Inventory extraction — this tool is a *DOCX/PDF renderer + template engine* over that model, not a new scanner.
- **DOCX dependency note:** Word `.docx` generation needs **DocumentFormat.OpenXml**, which already ships *inside the Deployment Risk Analyzer ClosedXML chain* (ClosedXML depends on DocumentFormat.OpenXml) — reuse that sanctioned, ship-in-Plugins-root dependency rather than adding a new one. Follow the DRA packaging exception exactly (Plugins root, not a subfolder; keep nuspec `<files>` and the `AssemblyResolve` `OwnedDependencies` whitelist in sync). Do not add a separate Word library.
- **PDF export** reuses the sanctioned **MigraDoc/PdfSharp-GDI** chain (already shipped in DRA). Note: MigraDoc can render the document model to both PDF and RTF; true DOCX still goes through OpenXml. Keep ClosedXML/MigraDoc/OpenXml types out of type signatures (method-body locals only), per the DRA convention.
- Core data: `solution`/`solutioncomponent`, tables/columns/relationships/choices, forms/views/dashboards, `appmodule`, security roles, `workflow` (rules + flows), plugins (`pluginassembly`/`plugintype`/`sdkmessageprocessingstep`), `customapi`, `environmentvariabledefinition`, `connectionreference`, Power Pages metadata (where available), `publisher` for branding.
- Diagram strategy: embed diagrams as **SVG/PNG** produced by DOC01/DOC02's pure-managed renderer; OpenXml/MigraDoc insert them as images (Word prefers EMF/PNG). No native diagram NuGet.
- Read-only — reads metadata and writes DOCX/PDF; never modifies the environment. Off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress per section and honor cancellation; cache per connection, clear on `UpdateConnection`. Keep the document/template engine UI-free where possible; degrade unavailable component types to a documented note.

---

## EPIC-DOC04 — Generate professional Word/PDF solution documentation
> **As** a **MGR**, **I want** to generate a polished Word (and PDF) document for a solution or environment, **so that** clients, auditors, and support receive presentation-ready handoff/audit documentation without manual authoring.

**Outcome:** a branded DOCX (and PDF) with cover page, TOC, revision history, executive/architecture summaries, component inventory, schema, UI, automation, security, diagrams, risk/recommendations, and appendix — produced from a chosen template and section selection.

---

## FEAT-DOC04-1 — Scope, template, and section selection `[Planned]`
- **US-DOC04.1.1** `[Planned]` **As** an MGR, **I want** to pick a solution or full environment and a document template (executive, technical, audit, support handoff, release), **so that** the output matches the audience.
  - **AC:** Scope loads off the UI thread via `RunAsync`; template and scope persist via Load/SaveSettings.
- **US-DOC04.1.2** `[Planned]` **As** an MGR, **I want** a section checklist and a preview summary, **so that** I include only relevant sections and see what will be generated.
  - **AC:** Section selection round-trips in settings; preview summary lists included sections and estimated content before export.

## FEAT-DOC04-2 — Front matter and formatted sections `[Planned]`
- **US-DOC04.2.1** `[Planned]` **As** an MGR, **I want** a cover page, table of contents, revision history, and executive + architecture summaries, **so that** the document reads as a formal deliverable.
  - **AC:** These sections render via the OpenXml document engine with a live TOC field; content derives from the extracted model.
- **US-DOC04.2.2** `[Planned]` **As** a TOOLDEV, **I want** a component inventory plus tables, columns, relationships, forms, views, dashboards, plugins, flows, custom APIs, security roles, env vars, connection references, and Power Pages metadata as formatted sections, **so that** the full solution is captured.
  - **AC:** Each is a distinct, styled Word section (headings + tables); unavailable component types degrade to a documented note, not a failure.

## FEAT-DOC04-3 — Diagrams, risk, and appendix `[Planned]`
- **US-DOC04.3.1** `[Planned]` **As** an ARCH, **I want** diagrams (architecture/ERD) and data tables embedded in the document, **so that** structure is shown visually.
  - **AC:** Diagrams render as SVG/PNG via the DOC01/DOC02 renderer and insert as images through OpenXml/MigraDoc.
- **US-DOC04.3.2** `[Planned]` **As** an MGR, **I want** risk/recommendation sections and an appendix, **so that** the document supports production-readiness review.
  - **AC:** Risk/recommendation content can reuse Deployment Risk Analyzer findings where available; appendix collects reference data.

## FEAT-DOC04-4 — Branding `[Planned]`
- **US-DOC04.4.1** `[Planned]` **As** an MGR, **I want** to set logo, author, company name, version, and footer, **so that** the document carries corporate branding.
  - **AC:** Branding fields round-trip in settings and appear on cover/header/footer; no credentials stored.

## FEAT-DOC04-5 — DOCX and PDF export `[Planned]`
- **US-DOC04.5.1** `[Planned]` **As** an MGR, **I want** to export to DOCX, **so that** recipients can edit the handoff document.
  - **AC:** DOCX is generated via the DocumentFormat.OpenXml dependency already shipped in the DRA ClosedXML chain (Plugins root, `AssemblyResolve` whitelist in sync); export runs off the UI thread with progress.
- **US-DOC04.5.2** `[Planned]` **As** an MGR, **I want** to export to PDF, **so that** I can distribute a locked, final version.
  - **AC:** PDF reuses the sanctioned MigraDoc/PdfSharp-GDI chain; generation reports progress and is cancellable.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses the shared extracted model and the sanctioned OpenXml (DOCX) + MigraDoc/PdfSharp (PDF) chains per the DRA packaging exception.
- Read-only default; document/template engine stays UI-free where possible and degrades unavailable component types to documented notes; OpenXml/MigraDoc types stay out of public signatures.
- Export formats: DOCX, PDF.
- Testing skeleton under testing/WordDocumentationGenerator/ when implementation starts.
