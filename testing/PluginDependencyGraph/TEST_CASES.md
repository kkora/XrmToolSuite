# Plugin Dependency Graph - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (xUnit — `testing/UnitTests/PluginDependencyGraphTests.cs`)

| ID | Case | Traces to | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-PDG-BUILD-01 | Builder emits typed nodes + pipeline edges from a hand-built `PluginRegistrationData` | US-PLUGIN1.1.x, 2.1, 3.1, 3.2 | assembly/type/step/image/table/message/customapi/solution nodes + contains/registers/on-table/on-message/image/implements/member edges | Automated | Pass |
| TC-PDG-BUILD-02 | Build is deterministic | US-PLUGIN1.2.1 | Two builds emit identical JSON | Automated | Pass |
| TC-PDG-SUB-03 | Subgraph isolates an assembly's footprint + owning solution, excludes unrelated custom API | US-PLUGIN1.2.2 | type/step/table/solution kept; custom API dropped | Automated | Pass |
| TC-PDG-FILTER-04 | Filter by table keeps only the matching step's lineage | US-PLUGIN1.2.3 | S1 + ancestors kept; sibling step + its table dropped | Automated | Pass |
| TC-PDG-FILTER-05 | No filter criteria returns the whole graph | US-PLUGIN1.2.3 | node count unchanged | Automated | Pass |
| TC-PDG-RISK-06 | High-impact assembly flagged over threshold, not under | US-PLUGIN1.4.1 | flagged at threshold 1 (component = assembly), not at default | Automated | Pass |
| TC-PDG-RISK-07 | Duplicate/overlapping steps flagged; rank collision → High | US-PLUGIN1.4.2 | duplicate finding, Severity High | Automated | Pass |
| TC-PDG-RISK-08 | Unmanaged assembly flagged High with component + owning solution | US-PLUGIN1.4.3 | High, component = assembly, description names solution | Automated | Pass |
| TC-PDG-EMIT-09 | Mermaid/GraphML/JSON well-formed; GraphML round-trips node type + edge kind | US-PLUGIN1.5.3 | `flowchart LR`, `<graphml>`, node/edge counts match, type/kind data present | Automated | Pass |
| TC-PDG-EMIT-10 | SVG is self-contained (no external hosts) | US-PLUGIN1.5.3 | `<svg>` present, no cdn / external http | Automated | Pass |
| TC-PDG-SEC-11 | Secure-config value never present in model or exports | US-PLUGIN1.3.3 | config node exposes flags only; secret sentinel absent from JSON/HTML | Automated | Pass |

## Manual (GUI + Dataverse — capture a screenshot per case under `screenshots/`)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-PDG-LOAD-01 | Load pipeline | US-PLUGIN1.1.1 | Open tool, connect, click "Load pipeline" | Loads off-thread with progress; nodes/edges + findings populate; status shows counts | Manual | Pending |
| TC-PDG-FILTER-02 | Live filter | US-PLUGIN1.2.3 | Choose a Table / Message / Stage / Mode / Solution filter | Graph text + node list update without re-querying | Manual | Pending |
| TC-PDG-FOCUS-03 | Isolate subgraph | US-PLUGIN1.2.2 | Pick an assembly/type in the Focus selector | View narrows to that component's footprint | Manual | Pending |
| TC-PDG-DETAILS-04 | Node details + dependency grid | US-PLUGIN1.5.1 | Select a step node | Details panel shows stage/mode/rank/etc.; dependency grid lists in/out edges | Manual | Pending |
| TC-PDG-EXPORT-05 | Export each format | US-PLUGIN1.5.3 | Export ▼ → PNG/SVG/PDF/Excel/JSON/GraphML/HTML | Each file writes and opens; PNG/PDF/Excel render (dependency chain resolves) | Manual | Pending |
| TC-PDG-SETTINGS-06 | Settings round-trip | US-PLUGIN1 DoD | Change threshold, close + reopen | Setting persists | Manual | Pending |

> Add/execute each automated row in `testing/UnitTests/` (done), and save a screenshot per manual case.
