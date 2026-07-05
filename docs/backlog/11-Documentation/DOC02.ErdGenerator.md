# ERD Generator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 11 (Documentation), item 2. Not in pack file (except ERD/Doc generators relate to pack #11).
> **Suggested tag:** `DOC02` · **Suggested project:** `XrmToolSuite.ErdGenerator`
> **Overlaps:** **Solution Knowledge Graph** (SHIPPED) — diagram/graph overlap for relationship rendering; reuse its graph model/renderer. **Architecture Diagram Generator** (DOC01) is the component-level counterpart — share the pure-managed SVG/PNG renderer and Mermaid/PlantUML emitters. **Solution Documentation Generator** (SOLN05 candidate) embeds ERD references — this tool can be its ERD engine. Table/relationship metadata overlaps **Relationship Validator** (ADMIN06) extraction.
> **Value/priority (my read):** High — an accurate, auto-generated Dataverse ERD is a frequently-requested, universally useful artifact for developers, analysts, and support; Mermaid `erDiagram` output is Git/wiki-friendly and hard to get elsewhere without paid tooling.

## Notes
- Shared extracted-metadata model: build an ERD graph model (tables → columns → keys → relationships) from entity metadata; where the Solution Knowledge Graph already holds relationship data, reuse it rather than re-scanning.
- Core data: `RetrieveAllEntitiesRequest`/`RetrieveEntityRequest` (EntityFilters Attributes + Relationships) for tables, columns, primary key/name, alternate keys (`EntityKeyMetadata`), one-to-many/many-to-one/many-to-many relationships, lookup targets, cascade configuration (`CascadeConfiguration`), required-level, IsCustomEntity, and managed/unmanaged (`IsManaged`); `solution`/`solutioncomponent` and `publisher` prefix for scoping.
- Diagram strategy: emit **Mermaid `erDiagram`** and **PlantUML** text as primary artifacts, then render **SVG/PNG** via the shared pure-managed renderer (GDI+/hand-written SVG). No external diagram NuGet unless it follows the ship-in-Plugins-root rule.
- Export chain reuse: PDF via the sanctioned MigraDoc/PdfSharp-GDI chain; HTML self-contained + theme-aware; Markdown embeds Mermaid; **JSON** carries the structured ERD model (tables/keys/relationships) for downstream tooling.
- Read-only — reads metadata only; never modifies schema.
- Off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress and honor cancellation; cache metadata per connection and clear on `UpdateConnection`. Keep the ERD graph/layout engine UI-free so relationship/cascade logic is testable; degrade any missing metadata to a note rather than failing.

---

## EPIC-DOC02 — Generate accurate Dataverse ERDs from live metadata
> **As** a **TOOLDEV**, **I want** to generate entity-relationship diagrams from real Dataverse metadata, **so that** developers, architects, analysts, and support work from an accurate data model instead of stale or partial exports.

**Outcome:** for a selected solution, publisher, table group, or full environment, an ERD showing tables, keys, important columns, all relationship types, lookups, cascade behavior, and custom/managed status, rendered to SVG, PNG, PDF, Mermaid, PlantUML, HTML, Markdown, and JSON.

---

## FEAT-DOC02-1 — Scope and table selection `[Planned]`
- **US-DOC02.1.1** `[Planned]` **As** a TOOLDEV, **I want** to scope the ERD by one solution, a publisher, a table group, or the full environment, **so that** I diagram only the relevant tables.
  - **AC:** Scope selectors load off the UI thread via `RunAsync`; selection persists via Load/SaveSettings.
- **US-DOC02.1.2** `[Planned]` **As** a TOOLDEV, **I want** a table selector to add/remove specific tables, **so that** I can trim or extend the auto-scoped set.
  - **AC:** Selected tables drive the ERD model; related-table auto-inclusion (relationship targets) is optional and clearly indicated.

## FEAT-DOC02-2 — Schema, keys, and columns `[Planned]`
- **US-DOC02.2.1** `[Planned]` **As** a TOOLDEV, **I want** each table shown with its display and schema name plus primary and alternate keys, **so that** identity and uniqueness are clear.
  - **AC:** Primary key/name and each alternate key (`EntityKeyMetadata`) render on the table node; sourced from entity metadata off the UI thread.
- **US-DOC02.2.2** `[Planned]` **As** a TOOLDEV, **I want** important columns and column-display settings, **so that** the ERD is readable without listing every attribute.
  - **AC:** A column-display setting (keys/lookups only … all columns) controls what renders; each column shows type and requirement level.

## FEAT-DOC02-3 — Relationships and cascade behavior `[Planned]`
- **US-DOC02.3.1** `[Planned]` **As** an ARCH, **I want** one-to-many, many-to-one, and many-to-many relationships plus lookup columns drawn between tables, **so that** the data model structure is visible.
  - **AC:** Each relationship type renders with correct cardinality notation; lookup columns link to their target table(s).
- **US-DOC02.3.2** `[Planned]` **As** an ARCH, **I want** cascade behavior and required/optional relationships displayed, **so that** I understand delete/assign/share propagation and referential rules.
  - **AC:** Cascade configuration (assign/delete/share/reparent/merge) and the relationship required level are shown on or beside each relationship edge.

## FEAT-DOC02-4 — Filters, custom/managed status, and layout `[Planned]`
- **US-DOC02.4.1** `[Planned]` **As** a TOOLDEV, **I want** filters by solution, table prefix, relationship type, custom-only, and managed-only, **so that** I can isolate exactly the slice I need.
  - **AC:** Filters apply to the model before layout without re-querying; filter state round-trips in settings.
- **US-DOC02.4.2** `[Planned]` **As** a TOOLDEV, **I want** custom-vs-standard and managed-vs-unmanaged visually distinguished, plus layout options and a preview canvas, **so that** governance-relevant tables stand out and I can verify before export.
  - **AC:** Custom/managed status is styled distinctly; preview reflects current filters/layout.

## FEAT-DOC02-5 — Multi-format export `[Planned]`
- **US-DOC02.5.1** `[Planned]` **As** a TOOLDEV, **I want** to export the ERD to Mermaid, PlantUML, SVG, and PNG, **so that** it drops into Git, wikis, and design docs.
  - **AC:** Mermaid `erDiagram`/PlantUML emitted as text; SVG/PNG via the shared renderer; export runs off the UI thread.
- **US-DOC02.5.2** `[Planned]` **As** a TOOLDEV, **I want** to export to PDF, HTML, Markdown, and JSON, **so that** I get both formatted documents and a machine-readable model.
  - **AC:** PDF reuses the sanctioned MigraDoc/PdfSharp-GDI chain; HTML self-contained/theme-aware; JSON carries the structured ERD model.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses the shared graph model/renderer and the sanctioned export dependency chains.
- Read-only default; ERD graph/layout engine stays UI-free where possible and degrades missing metadata to documented notes.
- Export formats: SVG, PNG, PDF, Mermaid, PlantUML, HTML, Markdown, JSON.
- Testing skeleton under testing/ErdGenerator/ when implementation starts.
