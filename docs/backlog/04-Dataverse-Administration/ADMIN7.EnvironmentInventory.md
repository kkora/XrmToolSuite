# Environment Inventory — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 7. Not in pack file.
> **Suggested tag:** `ADMIN7` · **Suggested project:** `XrmToolSuite.EnvironmentInventory`
> **Overlaps:** Broad foundational tool — its normalized inventory feeds candidate **ADMIN1 (Environment Health Dashboard)** overview cards, ADMIN8 (Drift Monitor) baselines, and any Documentation/Reporting tool (cf. pack idea #11 'Solution Documentation Generator'). Component retrieval overlaps Attribute Auditor / Solution Complexity Score — reuse shared `RetrieveAll`/metadata helpers.
> **Value/priority (my read):** High — a complete, exportable environment inventory is a constant admin/consultant/audit ask, and it is the data backbone several other candidate tools consume.

## Notes
- Data sources: `RetrieveMetadataChanges` (tables/columns/relationships/keys/choices/forms/views/charts), `solution`/`publisher`/`solutioncomponent`, `systemuser`/`team`/`businessunit`/`role`, `pluginassembly`/`sdkmessageprocessingstep`/`sdkmessageprocessingstepimage`, `workflow` (workflows/business rules), `webresource`, `customapi`/`customapirequestparameter`/`customapiresponseproperty`, `environmentvariabledefinition`/`connectionreference`, PCF via `customcontrol`. Power Automate/apps/sitemap "where available".
- Normalization model unifies disparate component types into a common inventory row (type, name, schema, owner/publisher, managed state, modified-on) — UI-free and unit-testable.
- Read-only; the inventory is a report. No snapshots required for the core, but an optional saved inventory enables ADMIN8 baselining.
- Large environments = many components across many queries — retrieve via `RetrieveAll` with progress and cancellation; cache per session; prefer targeted `ColumnSet`s.
- Export engine is the headline feature: Excel, CSV, JSON, HTML, Markdown, and Word/PDF as called for in the source.

---

## EPIC-ADMIN7 — Produce a complete, exportable inventory of a Dataverse environment
> **As** an **ADM**, **I want** every asset in an environment inventoried and exported, **so that** I have a single source of truth for administration, governance, audit, documentation, and support.

**Outcome:** a normalized, searchable inventory across metadata, solutions, security, automation, and web resources, with per-component detail and multi-format export.

---

## FEAT-ADMIN7-1 — Inventory metadata and solutions `[Planned]`
- **US-ADMIN7.1.1** `[Planned]` **As** an ADM, **I want** environment metadata, solutions, and publishers inventoried, **so that** I know the ALM baseline.
  - **AC:** Solutions/publishers load via `RetrieveAll` off the UI thread with progress; managed state and version are captured.
- **US-ADMIN7.1.2** `[Planned]` **As** an ADM, **I want** tables, columns, relationships, keys, and choices inventoried, and forms/views/charts/dashboards inventoried, **so that** the data and UI model is fully captured.
  - **AC:** Metadata loads via `RetrieveMetadataChanges` with targeted properties; each row is normalized to the common inventory model.

## FEAT-ADMIN7-2 — Inventory security and automation `[Planned]`
- **US-ADMIN7.2.1** `[Planned]` **As** a SEC, **I want** security roles, users, teams, and business units inventoried, **so that** I can audit the security surface.
  - **AC:** Records load via `RetrieveAll`; counts and key attributes are captured (no credentials or secrets persisted).
- **US-ADMIN7.2.2** `[Planned]` **As** a TOOLDEV, **I want** plugins/steps/images/assemblies, workflows/business rules, and Power Automate components (where available) inventoried, **so that** automation is fully mapped.
  - **AC:** Plugin registration tree and workflow states are captured; Power Automate is included where the SDK exposes it, else marked "not available".

## FEAT-ADMIN7-3 — Inventory web/dev components `[Planned]`
- **US-ADMIN7.3.1** `[Planned]` **As** a TOOLDEV, **I want** JavaScript/web resources, PCF controls, and custom APIs inventoried, **so that** the developer surface is documented.
  - **AC:** Web resources, `customcontrol` (PCF), and `customapi` (+ parameters/responses) are captured with type and owning solution.
- **US-ADMIN7.3.2** `[Planned]` **As** an ADM, **I want** environment variables and connection references inventoried, **so that** configuration bindings are documented.
  - **AC:** `environmentvariabledefinition`/`connectionreference` are listed; values that are secrets are never persisted or exported.

## FEAT-ADMIN7-4 — Search, filter, and detail `[Planned]`
- **US-ADMIN7.4.1** `[Planned]` **As** an ADM, **I want** to search and filter the inventory by category, name, and managed state, **so that** I can find components fast in large environments.
  - **AC:** A search/filter panel drives category grids; filtering is client-side over cached results.
- **US-ADMIN7.4.2** `[Planned]` **As** an ADM, **I want** a component detail panel, **so that** I can inspect a single asset's attributes.
  - **AC:** Selecting a row shows its normalized detail plus source-specific fields.

## FEAT-ADMIN7-5 — Export engine `[Planned]`
- **US-ADMIN7.5.1** `[Planned]` **As** an MGR, **I want** the inventory exported to Excel, CSV, JSON, HTML, Markdown, and Word/PDF, **so that** I can use it for docs, audits, and support.
  - **AC:** All six formats export off the UI thread; HTML is self-contained and theme-aware; export scope (categories) is configurable and round-trips via settings.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only; no secrets/credentials persisted or exported; normalization model UI-free and unit-testable; degrades unavailable sources to "not available" rows.
- Export formats: Excel, CSV, JSON, HTML, Markdown, Word/PDF.
- Testing skeleton under testing/EnvironmentInventory/ when implementation starts.
