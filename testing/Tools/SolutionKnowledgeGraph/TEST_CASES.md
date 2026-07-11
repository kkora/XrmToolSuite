# Solution Knowledge Graph - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated — model, algorithms & exporters (US-SOLN09-2..5)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/GraphTests.cs`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN09-MODEL-01 | Dedup & count | graph + duplicate edge | 4 nodes, 4 edges (dup ignored) | Automated | Pass |
| TC-SOLN09-MODEL-02 | Auto-create endpoints | AddEdge X→Y | X and Y nodes exist | Automated | Pass |
| TC-SOLN09-TRACE-03 | Dependency trace | trace A (A→B→C→A) | {B, C}, excludes A | Automated | Pass |
| TC-SOLN09-IMPACT-04 | Deletion impact | impact B | {A, C, D} (3 affected) | Automated | Pass |
| TC-SOLN09-CYCLE-05 | Cycle detection | A→B→C→A, D→B | one SCC {A,B,C}; D excluded | Automated | Pass |
| TC-SOLN09-CYCLE-06 | Acyclic | A→B→C | no cycles | Automated | Pass |
| TC-SOLN09-EXPORT-07 | GraphML | sample graph | well-formed, 4 nodes + 4 edges, labels present | Automated | Pass |
| TC-SOLN09-EXPORT-08 | SVG + legend | sample graph | a circle per node plus a colour-legend swatch per distinct type; "Legend" header present | Automated | Pass |
| TC-SOLN09-EXPORT-09 | Interactive HTML | sample graph | embeds DATA, node labels, no external hosts/CDN | Automated | Pass |

## Automated — graph builder against a fake connection (US-SOLN09-1)

Executed via `dotnet test testing/CollectorTests/CollectorTests.csproj`. Source: `testing/CollectorTests/GraphBuilderTests.cs` (`GraphBuilder` over a fake `IOrganizationService`).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN09-COL-01 | Typed node naming | type 29 component + workflow row | 1 node, Type "Workflow / Flow", Label from row | Automated | Pass |
| TC-SOLN09-COL-02 | Unmapped type | component type 999 | node Type "Component (999)", fallback label | Automated | Pass |
| TC-SOLN09-COL-03 | Missing name row | type 60 (Form), no systemform seeded | node created, Type "Form", label "Form …" | Automated | Pass |
| TC-SOLN09-COL-04 | Edge within solution | A,B components + dependency A→B | 1 edge A→B kind "requires" | Automated | Pass |
| TC-SOLN09-COL-05 | Required outside solution | dependency A→external (no type) | edge added; external auto-created "Unknown" | Automated | Pass |
| TC-SOLN09-COL-06 | Dependent outside solution | dependency other→A | no edge | Automated | Pass |
| TC-SOLN09-COL-07 | Empty solution | no components | 0 nodes, 0 edges | Automated | Pass |
| TC-SOLN09-COL-08 | Empty object id | component objectid Guid.Empty | skipped (0 nodes) | Automated | Pass |
| TC-SOLN09-COL-09 | Table named from metadata | type 1 component + EntityMetadata | node Type "Table", Label "Account" | Automated | Pass |
| TC-SOLN09-COL-10 | External required typed | dependency A→external with requiredcomponenttype 1 + metadata | external node Type "Table", Label "Contact" (not "Unknown") | Automated | Pass |
| TC-SOLN09-COL-11 | Column named from metadata | type 2 component + attribute metadata | node Type "Column", Label "new_widget.Amount" | Automated | Pass |

## Manual — builder, PNG, interactive view, UI (US-SOLN09-1, US-SOLN09-4..5)

Executed in XrmToolBox against a live org; screenshot per case into `screenshots/`.

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN09-M-01 | Tool loads & lists solutions | Open tool, Load solutions | Solutions listed | Manual | Pending |
| TC-SOLN09-M-02 | Build graph | Select solution, Build graph | Off-thread build; node/edge counts + node grid populate | Manual | Pending |
| TC-SOLN09-M-03 | Trace & impact | Select a node | Dependency trace + deletion-impact lists shown | Manual | Pending |
| TC-SOLN09-M-04 | Interactive view | Open interactive graph | Browser opens a rendered graph (no console errors): labelled nodes, scroll-zoom, drag-pan, Fit; click highlights trace/impact | Manual | Pending |
| TC-SOLN09-M-05 | Cycle detection | Detect cycles | Circular dependency groups listed (or "none") | Manual | Pending |
| TC-SOLN09-M-06 | Search & filter | Type a term / uncheck a type | Node grid filters accordingly | Manual | Pending |
| TC-SOLN09-M-07 | Exports | Export GraphML / SVG / PNG / HTML | Each file opens; SVG/PNG carry the colour legend; GraphML loads in yEd/Gephi | Manual | Pending |
| TC-SOLN09-M-08 | Filtered exports | Uncheck "Column", export SVG + open interactive | Column nodes (and their edges) absent from every output | Manual | Pending |
| TC-SOLN09-M-09 | Equal panels + splitters | Open tool | Filters / grid / detail start as equal thirds; both splitters draggable | Manual | Pending |
