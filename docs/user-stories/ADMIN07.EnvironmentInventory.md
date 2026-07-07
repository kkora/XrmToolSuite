# Environment Inventory — User Stories

> **Status:** Implemented (v1). Read-only inventory tool with client-side search/filter and Excel/CSV/JSON/Markdown/HTML/Word/PDF export.
> **Tag:** `ADMIN07` · **Project:** `XrmToolSuite.EnvironmentInventory`
> **Source backlog:** [`docs/backlog/04-Dataverse-Administration/ADMIN07.EnvironmentInventory.md`](../backlog/04-Dataverse-Administration/ADMIN07.EnvironmentInventory.md).
> **Testing:** [`testing/Tools/EnvironmentInventory/`](../../testing/Tools/EnvironmentInventory/) — automated SDK-free tests in `testing/UnitTests/EnvironmentInventoryTests.cs`; manual GUI/live cases in `TEST_CASES.md`.

## Notes
- Data sources (each in its own fail-soft try/catch; a permission gap degrades to an "unavailable source" note, never a hard error):
  `solution`/`publisher`; table metadata via `RetrieveAllEntitiesRequest` (`EntityFilters.Entity | Attributes`, per-table column count);
  `role`/`systemuser`/`team`/`businessunit`; `pluginassembly`/`sdkmessageprocessingstep`; `workflow` (workflow/business rule/action/BPF/modern flow via category);
  `webresource`/`customcontrol` (PCF)/`customapi`; `environmentvariabledefinition`/`connectionreference`.
- **Read-only. Secret values are never read or exported** — only environment-variable *definitions* (schema name, display name, declared type) and connection-reference metadata (logical name, connector id).
- The normalization model (`InventoryItem`/`InventorySnapshot`), exporters and summary projection are **UI-free and SDK-free** so they are unit-testable; the Dataverse collector is a separate file.
- Large environments: retrieval uses the shared `RetrieveAll` (paging + cancellation) off the UI thread with progress; search/filter runs client-side over the cached snapshot.

---

## EPIC-ADMIN07 — Produce a complete, exportable inventory of a Dataverse environment
> **As** an **ADM**, **I want** every asset in an environment inventoried and exported, **so that** I have a single source of truth for administration, governance, audit, documentation, and support.

**Outcome:** a normalized, searchable inventory across metadata, solutions, security, automation, and web resources, with per-component detail and multi-format export.

---

## FEAT-ADMIN07-1 — Inventory metadata and solutions `[Done]`
- **US-ADMIN07.1.1** `[Done]` **As** an ADM, **I want** environment solutions and publishers inventoried, **so that** I know the ALM baseline.
  - **AC:** Solutions/publishers load via `RetrieveAll` off the UI thread with progress; managed state and version are captured.
- **US-ADMIN07.1.2** `[Done]` **As** an ADM, **I want** tables inventoried with their column counts and managed/custom state, **so that** the data model is captured.
  - **AC:** Metadata loads via `RetrieveAllEntitiesRequest` with a targeted filter; each row is normalized to the common inventory model (per-table column count in Details for a lightweight v1).

## FEAT-ADMIN07-2 — Inventory security and automation `[Done]`
- **US-ADMIN07.2.1** `[Done]` **As** a SEC, **I want** security roles, users, teams, and business units inventoried, **so that** I can audit the security surface.
  - **AC:** Records load via `RetrieveAll`; key attributes are captured (no credentials or secrets persisted).
- **US-ADMIN07.2.2** `[Done]` **As** a TOOLDEV, **I want** plugin assemblies/steps and workflows/business rules/flows inventoried, **so that** automation is mapped.
  - **AC:** Plugin registration and workflow states/categories are captured; where a table is absent it is marked "not available".

## FEAT-ADMIN07-3 — Inventory web/dev and configuration components `[Done]`
- **US-ADMIN07.3.1** `[Done]` **As** a TOOLDEV, **I want** web resources, PCF controls, and custom APIs inventoried, **so that** the developer surface is documented.
  - **AC:** `webresource`, `customcontrol` (PCF, degrades if absent), and `customapi` are captured with type and managed state.
- **US-ADMIN07.3.2** `[Done]` **As** an ADM, **I want** environment variables and connection references inventoried, **so that** configuration bindings are documented.
  - **AC:** `environmentvariabledefinition`/`connectionreference` are listed; **secret values are never persisted or exported** — no value/secret column is ever emitted.

## FEAT-ADMIN07-4 — Search, filter, and detail `[Done]`
- **US-ADMIN07.4.1** `[Done]` **As** an ADM, **I want** to search and filter the inventory by category, name, and managed state, **so that** I can find components fast in large environments.
  - **AC:** A search box + category and managed-state dropdowns drive the grid; filtering is client-side over the cached snapshot (`InventorySnapshot.Filter`).
- **US-ADMIN07.4.2** `[Done]` **As** an ADM, **I want** a component detail panel, **so that** I can inspect a single asset's attributes.
  - **AC:** Selecting a row shows its normalized fields plus the source-specific `Details` dictionary.

## FEAT-ADMIN07-5 — Export engine `[Done]`
- **US-ADMIN07.5.1** `[Done]` **As** an MGR, **I want** the inventory exported to Excel, CSV, JSON, Markdown, HTML, Word, and PDF, **so that** I can use it for docs, audits, and support.
  - **AC:** All formats export off a `SaveFileDialog`; CSV is RFC-4180 quoted; HTML is self-contained (inline CSS, no external references); the text formats carry a summary counts table; Excel produces the FULL inventory grid (Summary + Items worksheets, no secret column) via ClosedXML; Word and PDF produce a summary-level report from the shared `ReportModel` exporters (Word reuses DocumentFormat.OpenXml from the ClosedXML chain; PDF uses PdfSharp/MigraDoc-GDI); export scope (selected sources) round-trips via settings. The tool ships the ClosedXML + PdfSharp/MigraDoc-GDI chains in the Plugins root exactly like Deployment Risk Analyzer.

## Definition of Done
- Follows suite conventions (`BaseToolControl`, `RunAsync`/`RetrieveAll`, Load/SaveSettings, progress + cancellation).
- Read-only; no secrets/credentials persisted or exported; normalization model UI-free and unit-tested; degrades unavailable sources to a noted "unavailable source" rather than aborting.
- Export formats: Excel, CSV, JSON, HTML, Markdown, Word, PDF.
- Testing artifacts under `testing/Tools/EnvironmentInventory/`; automated tests in `testing/UnitTests/EnvironmentInventoryTests.cs`.
