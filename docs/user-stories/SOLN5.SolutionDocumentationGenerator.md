# Solution Documentation Generator — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/05-Solution-Management/SOLN5.SolutionDocumentationGenerator.md`](../backlog/05-Solution-Management/SOLN5.SolutionDocumentationGenerator.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.SolutionDocumentationGenerator` · **Area tag:** `SOLN5`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (Dataverse metadata / ClosedXML / MigraDoc-GDI runtime) — pending manual sign-off.

Read-only tool that scans a Dataverse solution and generates a multi-section document (component inventory,
tables/columns/relationships/choices, forms, views, apps, automation, plug-ins, web resources, custom APIs,
configuration, security roles, diagrams, release notes and an architecture summary) in a chosen
documentation mode, previews it, and exports to **Word, PDF, Markdown, HTML, Excel and JSON**. It never
modifies the solution and never reads environment-variable values or secrets.

The SDK-free document pipeline — the `SolutionScanData` DTO, the `DocBuilder` template engine (mode +
sections gating, inventory rollup, "not available" degradation) and the `DocRenderers` (Markdown / HTML /
JSON) — is unit-tested. The SDK collector (`DocCollector`) and the Word (OpenXML) / PDF (MigraDoc-GDI) /
Excel (ClosedXML) exporters are manual-tested.

---

## EPIC-SOLN5 — Generate complete, current solution documentation on demand `[Implemented]`
> **As** a **TOOLDEV**, **I want** to generate full technical and business documentation for a Dataverse
> solution in one run, **so that** support, audit, onboarding, and client handoff always have accurate,
> formatted docs.

**Outcome:** a multi-section document rendered in a chosen mode (Executive Summary … Full Solution
Reference) and exported to Word/PDF/Markdown/HTML/Excel/JSON.

---

## FEAT-SOLN5-1 — Select solution, template, format, and sections `[Implemented]`
- **US-SOLN5.1.1** `[Implemented]` Pick a solution, a documentation mode, an output format, and which
  sections to include.
  - **AC:** Documentation mode (Executive Summary / Standard Reference / Full Solution Reference), the
    sections checklist and the export format are selectable; selections persist via Load/SaveSettings.
    **Automated** — `TC-SOLN5-MODE-02/03/04`, `TC-SOLN5-SECT-05` (mode + checklist gating);
    picker/persistence *(Manual — live/GUI)*.
- **US-SOLN5.1.2** `[Implemented]` A branding/settings panel so generated docs carry the org's
  header/logo/publisher info.
  - **AC:** Branding fields (header line, logo URL, publisher override) round-trip in settings and appear in
    the rendered document header; no credentials stored. **Automated** — `TC-SOLN5-HTML-09` (branding
    header rendered); round-trip *(Manual)*.

## FEAT-SOLN5-2 — Document data, schema, and automation components `[Implemented]`
- **US-SOLN5.2.1** `[Implemented]` Tables, columns, relationships and choices/option sets documented.
  - **AC:** The Schema section lists each table with a column/relationship summary; Full Solution Reference
    adds a per-table column-detail table; global choices are listed. Sourced from entity metadata off the UI
    thread. **Automated** — `TC-SOLN5-SCHEMA-06`; live metadata *(Manual)*.
- **US-SOLN5.2.2** `[Implemented]` Forms, views, charts, dashboards and apps documented.
  - **AC:** Component-summary rows (name, type, table, managed) drive readable Forms / Views / Apps
    sections; unavailable detail is a note, not fatal. **Automated** — section gating + counts; live
    detail *(Manual)*.
- **US-SOLN5.2.3** `[Implemented]` Business rules/workflows/flows, plug-ins (assembly/type/step/image),
  web resources, custom APIs, env vars, connection references and included security roles documented.
  - **AC:** Each is a distinct documented section with key configuration; missing types degrade to a "not
    available" note; environment-variable VALUES/secrets are never read. **Automated** — `TC-SOLN5-NA-07`
    (degradation), `TC-SOLN5-COUNT-01`; live detail *(Manual)*.

## FEAT-SOLN5-3 — Diagrams and summaries `[Implemented]`
- **US-SOLN5.3.1** `[Implemented]` Dependency/ERD-style relationship diagrams generated.
  - **AC:** A deterministic Mermaid `erDiagram` of the documented tables + a relationships table is produced
    in the Diagrams section (Full Solution Reference) and embeds into Markdown/HTML/Word/PDF. **Automated** —
    `TC-SOLN5-MD-08` (fenced mermaid); rendered embed *(Manual)*.
- **US-SOLN5.3.2** `[Implemented]` A component inventory, release notes and an architecture summary
  generated.
  - **AC:** Inventory counts by component type; release-notes and architecture-summary sections are produced
    from the scanned data. **Automated** — `TC-SOLN5-COUNT-01`, `TC-SOLN5-MODE-02`.

## FEAT-SOLN5-4 — Preview and generation progress `[Implemented]`
- **US-SOLN5.4.1** `[Implemented]` A preview and per-section progress while generating.
  - **AC:** Generation runs off the UI thread via `RunAsync`, reports progress per section and is
    cancellable; the preview pane shows the rendered Markdown or HTML source before final export.
    *(Manual — GUI/live.)*

## FEAT-SOLN5-5 — Multi-format export `[Implemented]`
- **US-SOLN5.5.1** `[Implemented]` Export to Word, PDF, Markdown, HTML, Excel and JSON.
  - **AC:** Markdown/HTML/JSON via the SDK-free `DocRenderers`; Word via OpenXML (`DocWordExporter`); PDF via
    MigraDoc-GDI (`DocPdfExporter`); Excel via ClosedXML (`DocExcelExporter`) using the sanctioned dependency
    chain; HTML is self-contained + theme-aware; JSON carries the structured inventory; export runs off the
    UI thread. **Automated** — `TC-SOLN5-MD-08`, `TC-SOLN5-HTML-09`, `TC-SOLN5-JSON-10`; Word/PDF/Excel
    *(Manual — runtime)*.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel);
  reuses the sanctioned ClosedXML + PdfSharp/MigraDoc-GDI export dependency chain.
- Read-only; the documentation/template engine (`DocBuilder`) stays UI-free and SDK-free and degrades
  unavailable component types to documented notes.
- Export formats: Word, PDF, Markdown, HTML, Excel, JSON.
- Testing artifacts under `testing/SolutionDocumentationGenerator/`.
