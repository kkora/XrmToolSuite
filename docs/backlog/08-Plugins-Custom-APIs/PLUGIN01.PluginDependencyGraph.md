# Plugin Dependency Graph — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 8 (Plugins & Custom APIs), item 1. Also in pack file idea #10 'Plugin Dependency Graph' — same tool.
> **Suggested tag:** `PLUGIN01` · **Suggested project:** `XrmToolSuite.PluginDependencyGraph`
> **Overlaps:** Solution Knowledge Graph already renders a component graph across a solution — this tool is plugin-pipeline-specific (assembly → type → step → image → message/table → custom API → config → solution). Reuse the Knowledge Graph's node/edge model and layout/export code where possible; do not fork a second general graph engine. Deployment Risk Analyzer flags some of the same risks (duplicate steps, high-impact assemblies) — link, don't duplicate.
> **Value/priority (my read):** High — architects need to see everything a plugin assembly touches before refactoring, merging, or removing it, and no shipped tool draws the plugin pipeline as a graph.

## Notes
- Data sources: `pluginassembly`, `plugintype`, `sdkmessageprocessingstep` (stage/mode/rank/filteringattributes/impersonatinguserid/statecode/supporteddeployment), `sdkmessageprocessingstepimage` (pre/post images + attributes), `sdkmessage`, `sdkmessagefilter` (primaryobjecttypecode → table), `customapi`, `solutioncomponent`/`solution` for membership.
- Secure/unsecure configuration usage is shown as an edge/flag only — **never render secure-config values**; unsecure config is displayed but redact anything that looks like a key/secret.
- Custom API relationships are detectable via `customapi.plugintypeid`; surface them as graph edges where present, degrade gracefully when absent.
- Feasibility caveat: flow/web-resource references are only partially discoverable from metadata — mark those edges "where available" and never fail the graph when they can't be resolved.
- Read-only tool — inspects registration + solution metadata, never modifies steps or assemblies. No destructive ops.
- Shared-core reuse: `Service.RetrieveAll`, `BatchExecutor`, progress/cancellation, settings round-trip, the shared reporting/export module, and (ideally) the Solution Knowledge Graph node/edge/layout classes.

---

## EPIC-PLUGIN01 — Visualize the full dependency footprint of Dataverse plugin registrations
> **As** an ARCH, **I want** an interactive graph of what every plugin assembly, type, and step touches, **so that** I can safely refactor, merge, remove, or deploy plugins without missing hidden dependencies.

**Outcome:** an interactive dependency graph (assembly → type → step → image → message/table → custom API → solution → config), high-impact detection, risk findings with severities (Critical/High/Medium/Low/Info), and PNG/SVG/PDF/Excel/JSON/GraphML/HTML exports — from a live connection with no hand-written queries.

---

## FEAT-PLUGIN01-1 — Plugin metadata retrieval `[Planned]`
- **US-PLUGIN01.1.1** `[Planned]` **As** a TOOLDEV, **I want** to load all plugin assemblies, types, and SDK steps for the connected org, **so that** I have a complete pipeline inventory.
  - **AC:** Assemblies, types, and steps load via `Service.RetrieveAll` off the UI thread with progress and cancellation.
- **US-PLUGIN01.1.2** `[Planned]` **As** an ARCH, **I want** each step's table, message, stage, mode, rank, filtering attributes, impersonating user, deployment type, and status resolved, **so that** nodes carry accurate context.
  - **AC:** Step nodes expose stage/mode/rank/message/entity/filteringattributes/impersonation/supporteddeployment/statecode.
- **US-PLUGIN01.1.3** `[Planned]` **As** a TOOLDEV, **I want** pre/post images and their attributes attached to their steps, **so that** image dependencies appear in the graph.
  - **AC:** Each `sdkmessageprocessingstepimage` is linked to its step with imagetype and attribute list.

## FEAT-PLUGIN01-2 — Graph model & rendering `[Planned]`
- **US-PLUGIN01.2.1** `[Planned]` **As** an ARCH, **I want** an interactive dependency graph canvas with node types for assembly, type, step, table, message, stage, image, custom API, solution, and configuration, **so that** I can explore relationships visually.
  - **AC:** Nodes are typed and color-coded; edges connect assembly→type→step→image and step→table/message/config; pan/zoom supported.
- **US-PLUGIN01.2.2** `[Planned]` **As** a TOOLDEV, **I want** to select an assembly or plugin type and see its subgraph, **so that** I can focus on one component's footprint.
  - **AC:** Selecting a node highlights its connected nodes/edges and can isolate the subgraph; selection persists via settings.
- **US-PLUGIN01.2.3** `[Planned]` **As** an ARCH, **I want** to filter the graph by table, message, stage, mode, or solution, **so that** I can narrow a large pipeline.
  - **AC:** Each filter live-updates the visible nodes/edges without re-querying Dataverse.

## FEAT-PLUGIN01-3 — Solution & custom API relationships `[Planned]`
- **US-PLUGIN01.3.1** `[Planned]` **As** an ALM lead, **I want** solution membership shown for each assembly/step, **so that** I can see which solution owns a registration.
  - **AC:** Solution nodes link to their member components via `solutioncomponent`; unmanaged registrations are visually distinct.
- **US-PLUGIN01.3.2** `[Planned]` **As** a TOOLDEV, **I want** custom API relationships drawn where detectable, **so that** I can see which plugin type backs which custom API.
  - **AC:** `customapi.plugintypeid` produces an edge from the custom API node to the plugin type; absent links degrade silently.
- **US-PLUGIN01.3.3** `[Planned]` **As** a SEC reviewer, **I want** secure/unsecure configuration usage flagged on steps without exposing secrets, **so that** I can audit config dependency safely.
  - **AC:** A "uses config" flag/edge appears; secure-config values are never displayed and unsecure values are secret-redacted.

## FEAT-PLUGIN01-4 — High-impact & risk detection `[Planned]`
- **US-PLUGIN01.4.1** `[Planned]` **As** an ARCH, **I want** high-impact assemblies (registered on many tables/messages) detected, **so that** I know what carries the most blast radius.
  - **AC:** Assemblies whose table/message fan-out exceeds a configurable threshold are flagged and ranked.
- **US-PLUGIN01.4.2** `[Planned]` **As** a DEVOPS, **I want** duplicate or overlapping steps detected, **so that** I can remove redundant registrations.
  - **AC:** Steps matching on message+entity+stage+mode are grouped and flagged Medium/High.
- **US-PLUGIN01.4.3** `[Planned]` **As** an ALM lead, **I want** unmanaged plugin registrations in production detected, **so that** I can catch out-of-process changes.
  - **AC:** Unmanaged steps/assemblies are flagged High; finding names the component and (if known) owning solution.

## FEAT-PLUGIN01-5 — Details, findings & export `[Planned]`
- **US-PLUGIN01.5.1** `[Planned]` **As** a TOOLDEV, **I want** a step details panel and a dependency grid beside the canvas, **so that** I can read exact registration values.
  - **AC:** Selecting a node populates a details panel and a dependency grid with its connected components.
- **US-PLUGIN01.5.2** `[Planned]` **As** an ARCH, **I want** a risk findings panel with severities, **so that** high-impact and duplication risks are actionable.
  - **AC:** Findings list carries Critical/High/Medium/Low/Info with a plain-language description per item.
- **US-PLUGIN01.5.3** `[Planned]` **As** a MGR, **I want** to export the graph to PNG, SVG, PDF, Excel, JSON, GraphML, and HTML, **so that** I can share or archive it.
  - **AC:** All listed formats export from the shared reporting module; GraphML round-trips node/edge types.

## Definition of Done
- Follows suite conventions; read-only default (no registration changes); export formats as listed (PNG/SVG/PDF/Excel/JSON/GraphML/HTML).
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; secure-config secrets never exposed; unresolved flow/web-resource edges degrade to informational.
- Testing skeleton under `testing/Tools/PluginDependencyGraph/` when implementation starts; SDK-free graph-model/impact logic covered by `testing/UnitTests`.
