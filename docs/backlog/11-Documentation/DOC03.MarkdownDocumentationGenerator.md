# Markdown Documentation Generator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 11 (Documentation), item 3. Not in pack file (except ERD/Doc generators relate to pack #11).
> **Suggested tag:** `DOC03` · **Suggested project:** `XrmToolSuite.MarkdownDocumentationGenerator`
> **Overlaps:** **Solution Documentation Generator** (SOLN05 candidate) — STRONG overlap: this is the Markdown-format renderer over the same extracted solution/metadata model. **Word / HTML Documentation Generators** (DOC04/DOC05) are sibling renderers — share the extracted model + section engine. Mermaid diagram output overlaps **ERD Generator** (DOC02) and **Architecture Diagram Generator** (DOC01) — reuse their Mermaid emitters. **Deployment Risk Analyzer** / **Attribute Auditor** (SHIPPED) show export patterns (HTML/CSV) to reuse.
> **Value/priority (my read):** High — Git/wiki-friendly, source-control-diffable docs are the lowest-friction format for ALM handoff and the most reusable output; multi-file folder output with front matter feeds static-site portals directly.

## Notes
- Shared extracted-metadata model: reuse the same solution/environment extraction as the Solution Documentation Generator (SOLN05) and the Environment Inventory candidate — this tool is a *Markdown renderer* over that model, not a new scanner.
- Core data: `solution`/`solutioncomponent`, tables/columns/relationships/choices via `RetrieveAllEntitiesRequest`; FormXML for forms, FetchXML/LayoutXML for views/charts/dashboards; `appmodule`, security roles, `workflow` (business rules + flows), `pluginassembly`/`plugintype`/`sdkmessageprocessingstep`, `customapi`, `environmentvariabledefinition`, `connectionreference`, `webresource`, Power Pages (`adx_*`/`mspp_*` where available), PCF.
- Diagram strategy: emit **Mermaid** fenced code blocks (ERD + dependency) inline in Markdown — reuse the DOC01/DOC02 Mermaid emitters; no image rendering needed for the Markdown path (renderers like GitHub/Azure DevOps render Mermaid natively).
- Output/export: the primary artifact is a **folder of Markdown files** (multi-file) or a single combined file; support YAML **front matter** for static-site generators (MkDocs/Docusaurus/Hugo) and Git-friendly, deterministic file naming so diffs stay clean. No heavy export dependency required for pure Markdown; ClosedXML/MigraDoc chains are not needed here.
- Read-only — reads metadata and writes Markdown files to a chosen output folder; never modifies the environment.
- Off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress per section/file and honor cancellation; cache metadata per connection and clear on `UpdateConnection`. Keep the Markdown template engine UI-free so section rendering is unit-testable; degrade unavailable component types (Power Pages, some flow detail) to a documented "not available" note.

---

## EPIC-DOC03 — Generate source-control-friendly Markdown documentation
> **As** a **DEVOPS**, **I want** to generate complete Markdown documentation for a solution or environment, **so that** GitHub, Azure DevOps Wiki, and doc portals hold accurate, diffable docs instead of stale hand-written pages.

**Outcome:** a table-of-contents-linked set of Markdown files (or one combined file) covering the full component inventory, schema, UI, automation, security, and dependencies, with inline Mermaid diagrams, release notes, README, and front matter, written to a chosen output folder.

---

## FEAT-DOC03-1 — Scope, sections, and output mode `[Planned]`
- **US-DOC03.1.1** `[Planned]` **As** a DEVOPS, **I want** to pick a solution or environment and choose an output folder, **so that** docs land where my repo/wiki expects them.
  - **AC:** Scope loads off the UI thread via `RunAsync`; the output folder and scope persist via Load/SaveSettings.
- **US-DOC03.1.2** `[Planned]` **As** a DEVOPS, **I want** a section checklist and single-file vs multi-file output, **so that** I control document granularity.
  - **AC:** Section selections and output mode round-trip in settings; multi-file uses deterministic, Git-friendly file names.

## FEAT-DOC03-2 — Document components `[Planned]`
- **US-DOC03.2.1** `[Planned]` **As** a DEVOPS, **I want** tables, columns, relationships, and choices documented in Markdown tables, **so that** the data model is fully described.
  - **AC:** Schema sections render as Markdown tables sourced from entity metadata off the UI thread; each column shows type, requirement, and description.
- **US-DOC03.2.2** `[Planned]` **As** a DEVOPS, **I want** forms, views, charts, dashboards, and apps documented, **so that** the UI layer is captured.
  - **AC:** FormXML/FetchXML/LayoutXML/app metadata parse into readable sections; unavailable detail is noted, not fatal.
- **US-DOC03.2.3** `[Planned]` **As** a DEVOPS, **I want** security roles, plugins, flows, custom APIs, env vars, connection references, web resources, Power Pages metadata, and PCFs documented, **so that** automation and configuration are covered.
  - **AC:** Each is a distinct Markdown section with key configuration; missing types degrade to a documented note.

## FEAT-DOC03-3 — Structure, TOC, inventory, and dependencies `[Planned]`
- **US-DOC03.3.1** `[Planned]` **As** a DEVOPS, **I want** a generated table of contents, a component inventory, and a dependency section, **so that** readers can navigate and see relationships.
  - **AC:** TOC links resolve within the file set; inventory counts by component type; dependency section is produced from scanned data.
- **US-DOC03.3.2** `[Planned]` **As** an ARCH, **I want** inline Mermaid diagrams and ERD references, **so that** structure is visual within Markdown.
  - **AC:** Mermaid fenced blocks (ERD + dependency) reuse the DOC01/DOC02 emitters and render natively in GitHub/Azure DevOps.

## FEAT-DOC03-4 — Handoff artifacts and static-site support `[Planned]`
- **US-DOC03.4.1** `[Planned]` **As** a DEVOPS, **I want** a generated README.md, release notes, architecture summary, and support handoff guide, **so that** the repo has ready-made entry points.
  - **AC:** Each artifact is produced from the scanned data and cross-links into the section files.
- **US-DOC03.4.2** `[Planned]` **As** a DEVOPS, **I want** YAML front matter and configurable file naming, **so that** the output feeds a static-site generator directly.
  - **AC:** Front matter is optional and templated; file naming is deterministic and Git-friendly; both round-trip in settings.

## FEAT-DOC03-5 — Preview and generation `[Planned]`
- **US-DOC03.5.1** `[Planned]` **As** a DEVOPS, **I want** a preview panel and per-section progress while generating the folder, **so that** I can verify before writing many files.
  - **AC:** Generation reports progress per section/file and is cancellable; preview shows rendered Markdown before export.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses the shared extracted model (SOLN05) and DOC01/DOC02 Mermaid emitters.
- Read-only default; Markdown template engine stays UI-free where possible and degrades unavailable component types to documented notes.
- Output formats: Markdown (single-file and multi-file folder) with optional YAML front matter and inline Mermaid.
- Testing skeleton under testing/MarkdownDocumentationGenerator/ when implementation starts.
