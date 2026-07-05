# ERD Generator — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/11-Documentation/DOC02.ErdGenerator.md`](../backlog/11-Documentation/DOC02.ErdGenerator.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.ErdGenerator` · **Area tag:** `DOC02`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (GDI/MigraDoc runtime) — pending manual sign-off.

Generates Dataverse entity-relationship diagrams from live metadata: scope by all tables / solution /
publisher, pick tables, choose a column-display level, filter (custom-only / managed-only), preview the
Mermaid output, and export to Mermaid, PlantUML, SVG, PNG, PDF, HTML, Markdown or JSON. Read-only (reads
metadata; never modifies schema). The SDK-free ERD model, emitters and the `ErdModel.Apply` filter are
unit-tested; the SDK collector, PNG (GDI+) and PDF (MigraDoc-GDI) renderers are manual-tested.

---

## EPIC-DOC02 — Generate accurate Dataverse ERDs from live metadata `[Implemented]`
> **As** a **TOOLDEV**, **I want** to generate entity-relationship diagrams from real Dataverse metadata,
> **so that** developers, architects, analysts and support work from an accurate data model.

**Outcome:** for a selected solution, publisher, table group or full environment, an ERD showing tables,
keys, columns, all relationship types, lookups, cascade behavior and custom/managed status, rendered to
SVG, PNG, PDF, Mermaid, PlantUML, HTML, Markdown and JSON.

---

## FEAT-DOC02-1 — Scope and table selection `[Implemented]`
- **US-DOC02.1.1** `[Implemented]` Scope the ERD by all tables, one solution, or a publisher.
  - **AC:** Scope selectors and lists load off the UI thread via `RunAsync`; the scope choice and column/
    filter settings persist via Load/SaveSettings. *(Manual — live metadata.)*
- **US-DOC02.1.2** `[Implemented]` A checked-list table selector (with a text filter) adds/removes tables.
  - **AC:** Checked tables drive the ERD model; relationship targets outside the selected set are still
    listed on lookup columns (marked) but not drawn as edges (both-ends rule). *(Manual + automated for the model rule.)*

## FEAT-DOC02-2 — Schema, keys, and columns `[Implemented]`
- **US-DOC02.2.1** `[Implemented]` Each table shows display + schema name, primary id/name and alternate keys.
  - **AC:** `EntityKeyMetadata` → `ErdKey`; primary id/name captured from entity metadata. *(Collector: manual; model/emit: automated.)*
- **US-DOC02.2.2** `[Implemented]` A column-display setting (Keys+lookups / Important / All) controls columns;
  each column shows type and required level.
  - **AC:** `ErdModel.SelectColumns` selects per display level (keys/lookups/alt-key members; +required for
    Important; all for All). **Automated** — `TC-ERD-MERMAID-02`, `TC-ERD-COLSELECT-09`.

## FEAT-DOC02-3 — Relationships and cascade behavior `[Implemented]`
- **US-DOC02.3.1** `[Implemented]` 1:N, N:1 and N:N plus lookup columns are drawn with correct cardinality.
  - **AC:** Mermaid/PlantUML cardinality tokens `||--o{` (1:N), `}o--||` (N:1), `}o--o{` (N:N); lookup
    columns link to targets. **Automated** — `TC-ERD-MERMAID-01`, `TC-ERD-PLANTUML-03`.
- **US-DOC02.3.2** `[Implemented]` Cascade behavior and required level are shown per relationship.
  - **AC:** `CascadeConfiguration` (assign/delete/merge/reparent/share/unshare) → `CascadeSummary`; the
    lookup's required level is carried on the relationship and rendered in HTML/PDF/JSON. *(Collector: manual; carried through emitters: automated via JSON.)*

## FEAT-DOC02-4 — Filters, custom/managed status, and layout `[Implemented]`
- **US-DOC02.4.1** `[Implemented]` Filters by custom-only, managed-only and relationship type apply to the
  model before layout without re-querying; filter state round-trips in settings.
  - **AC:** `ErdModel.Apply(ErdFilter)` trims tables then drops dangling/disallowed relationships.
    **Automated** — `TC-ERD-FILTER-05/06/07`.
- **US-DOC02.4.2** `[Implemented]` Custom vs standard tables are visually distinguished; a text preview
  reflects the current column-display/filter choices before export.
  - **AC:** SVG/PNG colour custom table headers distinctly; the preview pane shows the live Mermaid. *(Preview/PNG styling: manual; SVG box-per-table: automated `TC-ERD-SVG-08`.)*

## FEAT-DOC02-5 — Multi-format export `[Implemented]`
- **US-DOC02.5.1** `[Implemented]` Export to Mermaid, PlantUML, SVG and PNG.
  - **AC:** Mermaid `erDiagram` / PlantUML emitted as text; SVG hand-written; PNG via GDI+. Export runs off
    the UI thread. **Automated** for Mermaid/PlantUML/SVG (`TC-ERD-MERMAID-01`, `TC-ERD-PLANTUML-03`,
    `TC-ERD-SVG-08`); **PNG `[Implemented*]`** — GDI+ raster, manual-tested.
- **US-DOC02.5.2** `[Implemented]` Export to PDF, HTML, Markdown and JSON.
  - **AC:** PDF via the sanctioned MigraDoc/PdfSharp-GDI chain; HTML self-contained (embeds the SVG);
    Markdown embeds a `mermaid` erDiagram block; JSON carries the structured model. **Automated** for JSON
    (`TC-ERD-JSON-04`); HTML/Markdown share the automated SVG/Mermaid emitters; **PDF `[Implemented*]`** — manual-tested.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll semantics, Load/SaveSettings, progress + cancellation).
- Read-only; the ERD model/emitters/filter stay UI-free and SDK-free and degrade missing metadata to notes.
- Export formats: SVG, PNG, PDF, Mermaid, PlantUML, HTML, Markdown, JSON. — **Done.**
- Testing under `testing/ErdGenerator/`; SDK-free logic covered by `testing/UnitTests/ErdGeneratorTests.cs`. — **Done** (PNG/PDF/GUI/collector pending manual sign-off).
