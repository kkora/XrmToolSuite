# Plugin Dependency Graph — User Stories

> **Status:** Implemented (Phase-A). Area tag `PLUGIN01` · Project `XrmToolSuite.PluginDependencyGraph`.
> **Read-only:** inspects plugin-registration + solution metadata; never modifies steps or assemblies.
> Traces to [`testing/PluginDependencyGraph/`](../../testing/PluginDependencyGraph). SDK-free graph
> model / builder / risk rules / emitters are covered by `testing/UnitTests/PluginDependencyGraphTests.cs`.

## Notes

- **Data sources:** `pluginassembly`, `plugintype`, `sdkmessageprocessingstep` (stage/mode/rank/
  filteringattributes/impersonatinguserid/statecode/supporteddeployment), `sdkmessageprocessingstepimage`,
  `sdkmessage`, `sdkmessagefilter` (primaryobjecttypecode → table), `customapi` (plugintypeid), and
  `solutioncomponent`/`solution` for membership.
- **Secure config is never exposed.** A step's secure/unsecure config usage is shown as a flag/edge only.
  The secure-config *value* is never retrieved; the unsecure config is displayed with a redacted preview
  (secrets, GUIDs and long tokens masked).
- Custom-API relationships come from `customapi.plugintypeid`; they degrade silently when absent.
- All Dataverse access runs off the UI thread via `RunAsync`/`RetrieveAll` with progress + cancellation;
  any query failure degrades to an informational note rather than failing the graph.

---

## EPIC-PLUGIN01 — Visualize the full dependency footprint of Dataverse plugin registrations

> **As** an ARCH, **I want** a graph of what every plugin assembly, type, and step touches, **so that**
> I can safely refactor, merge, remove, or deploy plugins without missing hidden dependencies.

**Outcome:** a dependency graph (assembly → type → step → image → message/table → custom API → solution →
config), high-impact / duplicate / unmanaged detection with severities, and PNG/SVG/PDF/Excel/JSON/GraphML/
HTML exports — from a live connection with no hand-written queries.

---

## FEAT-PLUGIN01-1 — Plugin metadata retrieval `[Done]`

- **US-PLUGIN01.1.1** `[Done]` **As** a TOOLDEV, **I want** to load all plugin assemblies, types and SDK
  steps for the connected org, **so that** I have a complete pipeline inventory.
  - **AC:** Assemblies, types and steps load via `Service.RetrieveAll` off the UI thread with progress and
    cancellation. *(PluginCollector; TC-PDG-BUILD-01, TC-PDG-LOAD-01)*
- **US-PLUGIN01.1.2** `[Done]` **As** an ARCH, **I want** each step's table, message, stage, mode, rank,
  filtering attributes, impersonating user, deployment type and status resolved, **so that** nodes carry
  accurate context.
  - **AC:** Step nodes expose stage/mode/rank/message/entity/filteringattributes/impersonation/
    supporteddeployment/statecode. *(TC-PDG-BUILD-01, TC-PDG-DETAILS-04)*
- **US-PLUGIN01.1.3** `[Done]` **As** a TOOLDEV, **I want** pre/post images and their attributes attached
  to their steps, **so that** image dependencies appear in the graph.
  - **AC:** Each `sdkmessageprocessingstepimage` links to its step with imagetype + attribute list.
    *(TC-PDG-BUILD-01)*

## FEAT-PLUGIN01-2 — Graph model, filtering & isolation `[Done]`

- **US-PLUGIN01.2.1** `[Done]` **As** an ARCH, **I want** a typed dependency graph (assembly, type, step,
  table, message, image, custom API, solution, config), **so that** I can explore relationships.
  - **AC:** Nodes are typed and colour-coded; edges connect assembly→type→step→image and step→table/message/
    config; the graph renders as Mermaid text + SVG/PNG. *(TC-PDG-BUILD-01, TC-PDG-EMIT-09)*
- **US-PLUGIN01.2.2** `[Done]` **As** a TOOLDEV, **I want** to select an assembly or plugin type and see its
  subgraph, **so that** I can focus on one component's footprint.
  - **AC:** The Focus selector isolates a node's subgraph (footprint + owning solution) without re-querying.
    *(PluginGraph.Subgraph; TC-PDG-SUB-03)*
- **US-PLUGIN01.2.3** `[Done]` **As** an ARCH, **I want** to filter the graph by table, message, stage, mode
  or solution, **so that** I can narrow a large pipeline.
  - **AC:** Each filter live-updates the visible nodes/edges without re-querying Dataverse.
    *(PluginGraph.Filter; TC-PDG-FILTER-04, TC-PDG-FILTER-05)*

## FEAT-PLUGIN01-3 — Solution & custom API relationships `[Done]`

- **US-PLUGIN01.3.1** `[Done]` **As** an ALM lead, **I want** solution membership shown per assembly/step,
  **so that** I can see which solution owns a registration.
  - **AC:** Solution nodes link to their member components; unmanaged registrations are visually distinct.
    *(TC-PDG-BUILD-01)*
- **US-PLUGIN01.3.2** `[Done]` **As** a TOOLDEV, **I want** custom API relationships drawn where detectable,
  **so that** I can see which plugin type backs which custom API.
  - **AC:** `customapi.plugintypeid` produces an edge from the custom API node to the plugin type; absent
    links degrade silently. *(TC-PDG-BUILD-01)*
- **US-PLUGIN01.3.3** `[Done]` **As** a SEC reviewer, **I want** secure/unsecure configuration usage flagged
  without exposing secrets, **so that** I can audit config dependency safely.
  - **AC:** A "uses config" flag/edge appears; secure-config values are never displayed and unsecure values
    are secret-redacted. *(TC-PDG-SEC-11)*

## FEAT-PLUGIN01-4 — High-impact & risk detection `[Done]`

- **US-PLUGIN01.4.1** `[Done]` **As** an ARCH, **I want** high-impact assemblies (registered on many
  tables/messages) detected, **so that** I know what carries the most blast radius.
  - **AC:** Assemblies whose table+message fan-out exceeds a configurable threshold are flagged and ranked.
    *(PluginRiskRules; TC-PDG-RISK-06)*
- **US-PLUGIN01.4.2** `[Done]` **As** a DEVOPS, **I want** duplicate/overlapping steps detected, **so that**
  I can remove redundant registrations.
  - **AC:** Steps matching on message+entity+stage+mode are grouped and flagged Medium/High. *(TC-PDG-RISK-07)*
- **US-PLUGIN01.4.3** `[Done]` **As** an ALM lead, **I want** unmanaged plugin registrations detected,
  **so that** I can catch out-of-process changes.
  - **AC:** Unmanaged steps/assemblies are flagged High; the finding names the component and owning solution.
    *(TC-PDG-RISK-08)*

## FEAT-PLUGIN01-5 — Details, findings & export `[Done]`

- **US-PLUGIN01.5.1** `[Done]` **As** a TOOLDEV, **I want** a node details panel and a dependency grid,
  **so that** I can read exact registration values.
  - **AC:** Selecting a node populates a details panel and a dependency grid of its connected components.
    *(TC-PDG-DETAILS-04)*
- **US-PLUGIN01.5.2** `[Done]` **As** an ARCH, **I want** a risk findings panel with severities, **so that**
  high-impact and duplication risks are actionable.
  - **AC:** Findings carry Critical/High/Medium/Low/Info with a plain-language description each.
    *(TC-PDG-RISK-06..08)*
- **US-PLUGIN01.5.3** `[Done]` **As** a MGR, **I want** to export the graph to PNG, SVG, PDF, Excel, JSON,
  GraphML and HTML, **so that** I can share or archive it.
  - **AC:** All listed formats export; GraphML round-trips node/edge types. *(TC-PDG-EMIT-09, TC-PDG-EXPORT-05)*

## Definition of Done

- Follows suite conventions; read-only default (no registration changes); export formats as listed.
- All Dataverse access off the UI thread; secure-config secrets never exposed; unresolved edges degrade
  to informational notes.
- SDK-free graph model / builder / rules / emitters covered by `testing/UnitTests`; manual GUI/Dataverse
  cases documented under `testing/PluginDependencyGraph/`.
