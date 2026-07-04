# Solution Knowledge Graph - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated — model, algorithms & exporters (US-KG-2..5)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/GraphTests.cs`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-KG-MODEL-01 | Dedup & count | graph + duplicate edge | 4 nodes, 4 edges (dup ignored) | Automated | Pass |
| TC-KG-MODEL-02 | Auto-create endpoints | AddEdge X→Y | X and Y nodes exist | Automated | Pass |
| TC-KG-TRACE-03 | Dependency trace | trace A (A→B→C→A) | {B, C}, excludes A | Automated | Pass |
| TC-KG-IMPACT-04 | Deletion impact | impact B | {A, C, D} (3 affected) | Automated | Pass |
| TC-KG-CYCLE-05 | Cycle detection | A→B→C→A, D→B | one SCC {A,B,C}; D excluded | Automated | Pass |
| TC-KG-CYCLE-06 | Acyclic | A→B→C | no cycles | Automated | Pass |
| TC-KG-EXPORT-07 | GraphML | sample graph | well-formed, 4 nodes + 4 edges, labels present | Automated | Pass |
| TC-KG-EXPORT-08 | SVG | sample graph | a circle per node, self-contained | Automated | Pass |
| TC-KG-EXPORT-09 | Interactive HTML | sample graph | embeds DATA, node labels, no external hosts/CDN | Automated | Pass |

## Manual — builder, PNG, interactive view, UI (US-KG-1, US-KG-4..5)

Executed in XrmToolBox against a live org; screenshot per case into `screenshots/`.

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-KG-M-01 | Tool loads & lists solutions | Open tool, Load solutions | Solutions listed | Manual | Pending |
| TC-KG-M-02 | Build graph | Select solution, Build graph | Off-thread build; node/edge counts + node grid populate | Manual | Pending |
| TC-KG-M-03 | Trace & impact | Select a node | Dependency trace + deletion-impact lists shown | Manual | Pending |
| TC-KG-M-04 | Interactive view | Open interactive graph | Browser opens a draggable, searchable, filterable graph; click highlights trace/impact | Manual | Pending |
| TC-KG-M-05 | Cycle detection | Detect cycles | Circular dependency groups listed (or "none") | Manual | Pending |
| TC-KG-M-06 | Search & filter | Type a term / uncheck a type | Node grid filters accordingly | Manual | Pending |
| TC-KG-M-07 | Exports | Export GraphML / SVG / PNG / HTML | Each file opens; GraphML loads in yEd/Gephi | Manual | Pending |
