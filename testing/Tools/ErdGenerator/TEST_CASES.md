# ERD Generator - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (xUnit — `testing/UnitTests/ErdGeneratorTests.cs`)

| ID | Case | Traces to | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ERD-MERMAID-01 | Mermaid `erDiagram` with cardinality + PK/FK | US-DOC02.3.1 / 2.5.1 | Output starts `erDiagram`; contains `\|\|--o{`, `}o--o{`, `accountid PK`, `new_accountid FK` | Automated | Pass |
| TC-ERD-MERMAID-02 | Column-display level controls columns | US-DOC02.2.2 | Keys+lookups hides optional `telephone1`; All shows it | Automated | Pass |
| TC-ERD-PLANTUML-03 | PlantUML entities + relationships | US-DOC02.5.1 | `@startuml`/`@enduml`, 2 `entity` blocks, `\|\|--o{`, `<<PK>>` | Automated | Pass |
| TC-ERD-JSON-04 | JSON carries counts + names | US-DOC02.5.2 | `"tableCount":2`, `"relationshipCount":2`, table/relationship names present | Automated | Pass |
| TC-ERD-FILTER-05 | `Apply(customOnly)` trims | US-DOC02.4.1 | Only custom table kept; dangling relationships dropped | Automated | Pass |
| TC-ERD-FILTER-06 | `Apply(managedOnly)` trims | US-DOC02.4.1 | Only managed table kept | Automated | Pass |
| TC-ERD-FILTER-07 | `Apply(relationshipType)` trims | US-DOC02.4.1 | Both tables kept; only allowed relationship type kept | Automated | Pass |
| TC-ERD-SVG-08 | SVG well-formed, box per table | US-DOC02.4.2 / 2.5.1 | `<svg>`…`</svg>`; one header rect per table | Automated | Pass |
| TC-ERD-COLSELECT-09 | Alternate-key members included in Keys+lookups | US-DOC02.2.2 | Alt-key column shown; unrelated optional column hidden | Automated | Pass |

## Manual (GUI + live Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-ERD-M01 | Tool loads + connects | US-DOC02.1.1 | Open the tool in XrmToolBox, connect | Loads, connects; settings restore | Manual | Pending |
| TC-ERD-M02 | Load tables (All) | US-DOC02.1.2 | Scope = All tables → Load tables | Checked list populates off-thread; text filter works | Manual | Pending |
| TC-ERD-M03 | Scope by solution | US-DOC02.1.1 | Scope = By solution → pick solution → Load tables | Only the solution's tables listed | Manual | Pending |
| TC-ERD-M04 | Scope by publisher | US-DOC02.1.1 | Scope = By publisher → pick publisher → Load tables | Only tables with the publisher prefix listed | Manual | Pending |
| TC-ERD-M05 | Generate + preview | US-DOC02.2.1 / 2.3.1 | Check a few related tables → Generate | Preview shows Mermaid; keys, lookups, relationships correct | Manual | Pending |
| TC-ERD-M06 | Custom/managed filters | US-DOC02.4.1 | Toggle Custom only / Managed only | Preview + counts update without re-query | Manual | Pending |
| TC-ERD-M07 | Export SVG/HTML/MD/JSON/Mermaid/PlantUML | US-DOC02.5.1 / 2.5.2 | Export each; open result | Files valid; HTML embeds the SVG; MD renders on GitHub | Manual | Pending |
| TC-ERD-M08 | Export PNG (GDI+) | US-DOC02.5.1 | Export PNG; open | Readable raster ERD with boxes + edges | Manual | Pending |
| TC-ERD-M09 | Export PDF (MigraDoc-GDI) | US-DOC02.5.2 | Export PDF; open | Titled PDF with diagram image + per-table/relationship detail | Manual | Pending |
| TC-ERD-M10 | Settings round-trip | US-DOC02.1.1 | Change scope/columns/filters, close, reopen | Choices restored | Manual | Pending |
| TC-ERD-M11 | Missing-metadata degrades | US-DOC02.3.2 | Include a table that errors on retrieve | Note recorded; no crash; other tables still render | Manual | Pending |

> Save a screenshot under `screenshots/` for each executed manual case.
