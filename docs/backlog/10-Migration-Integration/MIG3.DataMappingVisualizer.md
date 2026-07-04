# Data Mapping Visualizer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 10 (Migration & Integration), item 3. Not in pack file.
> **Suggested tag:** `MIG3` · **Suggested project:** `XrmToolSuite.DataMappingVisualizer`
> **Overlaps:** None significant. Shares metadata-retrieval helpers with Attribute Auditor (Entity/Attribute metadata) but the value (validating an imported source→target mapping file against live metadata) is unique to this tool.
> **Value/priority (my read):** Medium — very useful for migration/integration projects that keep mappings in spreadsheets, but the audience is narrower (active migration projects) and the whole feature depends on a well-formed imported mapping file.

## Notes
- Data sources: an **imported mapping file** (Excel/CSV/JSON) describing source→target table/column mappings, validated against live Dataverse Entity/Attribute metadata (`RetrieveMetadataChanges`/`RetrieveEntity`) including datatypes, required levels, option sets, lookups, and alternate keys.
- File import is local and read-only against Dataverse: parse the mapping file, then retrieve only the target tables/columns referenced (targeted `ColumnSet`s / metadata filters, not full-org metadata dumps).
- Validation engine is UI-free and unit-testable: it takes a parsed mapping model + a metadata snapshot and emits typed findings (missing target column, datatype mismatch, unmapped required field, choice/lookup/alternate-key gap, duplicate mapping, unmapped source field); no Dataverse or WinForms dependency so it lifts into a console/CI wrapper.
- Metadata retrieval runs off the UI thread via `RunAsync`/`WorkAsync` with progress + cancellation; the imported mapping file path/last-used options round-trip via Load/SaveSettings (never persist file contents or credentials).
- Read-only — the tool validates and visualizes mappings; it never writes records or metadata. Sensitive sample/default values referenced in a mapping file are masked in exports.

---

## EPIC-MIG3 — Validate and visualize source-to-target data mappings against live metadata
> **As** a migration **CUST** (functional/data lead), **I want** my mapping spreadsheet validated against the real Dataverse schema and drawn as a mapping diagram, **so that** I catch mapping gaps and type mismatches before a migration or integration load runs.

**Outcome:** an imported mapping validated field-by-field against live metadata, a mapping-completeness score, a datatype/lookup/choice gap inventory, a visual table/field mapping diagram, and an exportable validated-mapping report.

---

## FEAT-MIG3-1 — Import and parse mapping files `[Planned]`
- **US-MIG3.1.1** `[Planned]` **As** a migration CUST, **I want** to import a mapping file from Excel, CSV, or JSON, **so that** I can reuse the mapping my project already maintains.
  - **AC:** Import parses source table/column, target table/column, and optional transform/notes columns; malformed rows are reported as findings rather than aborting the import; parsing runs off the UI thread.
- **US-MIG3.1.2** `[Planned]` **As** a migration CUST, **I want** to select which target Dataverse tables the mapping covers, **so that** only relevant metadata is retrieved.
  - **AC:** Target metadata retrieval is scoped to referenced tables/columns via targeted metadata filters; progress + cancellation are supported.

## FEAT-MIG3-2 — Validate table and column mappings `[Planned]`
- **US-MIG3.2.1** `[Planned]` **As** a migration CUST, **I want** source→target table and column mappings validated against metadata, **so that** I know which mappings resolve to real schema.
  - **AC:** Each mapping is classified Valid / Missing-target-table / Missing-target-column; the validation engine is UI-free and unit-testable.
- **US-MIG3.2.2** `[Planned]` **As** a migration CUST, **I want** datatype mismatches detected, **so that** I avoid load failures from incompatible types.
  - **AC:** Source-declared type vs target attribute type mismatches (string→lookup, number→text, length overflow) are findings with severity.

## FEAT-MIG3-3 — Detect completeness and special-field gaps `[Planned]`
- **US-MIG3.3.1** `[Planned]` **As** a migration CUST, **I want** required Dataverse fields that are not mapped detected, **so that** I don't miss mandatory columns.
  - **AC:** Target attributes with required level = ApplicationRequired that have no incoming mapping are findings.
- **US-MIG3.3.2** `[Planned]` **As** a migration CUST, **I want** choice/option-set, lookup, and alternate-key mapping gaps detected, **so that** reference and key mappings are complete.
  - **AC:** Unmapped option-set values, lookups without a resolution/key strategy, and alternate-key columns not fully mapped are separate finding types.
- **US-MIG3.3.3** `[Planned]` **As** a migration CUST, **I want** duplicate mappings and unmapped source fields detected, **so that** the mapping is unambiguous and complete.
  - **AC:** Two source fields mapped to the same target (and vice versa) and source fields with no target are findings.

## FEAT-MIG3-4 — Visualize, score, and export `[Planned]`
- **US-MIG3.4.1** `[Planned]` **As** a migration CUST, **I want** the mapping drawn as a table/field diagram, **so that** I can see coverage and gaps at a glance.
  - **AC:** Diagram renders source entities/fields linked to target entities/fields, color-coded by validation status; unmapped/invalid links are visually distinct.
- **US-MIG3.4.2** `[Planned]` **As** an **MGR**, **I want** a mapping-completeness score, **so that** I get a readiness signal for the load.
  - **AC:** Score is a UI-free roll-up of mapped/required/valid ratios with severities Critical/High/Medium/Low/Info.
- **US-MIG3.4.3** `[Planned]` **As** a migration CUST, **I want** an exported validated-mapping report, **so that** I can hand a corrected mapping back to the project.
  - **AC:** Exports to Excel, CSV, JSON, and self-contained HTML run off the UI thread; sensitive default/sample values are masked; read-only.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; validation engine UI-free/unit-testable and liftable to console/CI; export formats Excel, CSV, JSON, HTML.
- Settings round-trip (last file/options) without persisting file contents or credentials.
- Testing skeleton under testing/DataMappingVisualizer/ when implementation starts.
