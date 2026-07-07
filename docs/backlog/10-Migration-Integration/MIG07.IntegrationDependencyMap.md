# Integration Dependency Map — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 10 (Migration & Integration), item 7. Not in pack file.
> **Suggested tag:** `MIG07` · **Suggested project:** `XrmToolSuite.IntegrationDependencyMap`
> **Overlaps:** **Overlaps MIG04 'API Integration Explorer'** — both discover the same integration touchpoints. **NOTE:** MIG04 is the *inventory/grid + risk-findings* lens; MIG07 is the *graph/visualization* lens (node-edge canvas, single-point-of-failure, inbound/outbound). Build **one shared integration-discovery engine** (UI-free, emitting a normalized node/edge model) and give each tool its own presentation. Also touches Solution Knowledge Graph (graph rendering approach — reuse patterns) and Deployment Risk Analyzer (connection-reference/`clientdata` parsing).
> **Value/priority (my read):** Medium-High — a visual "what breaks if this endpoint/connector dies" map answers questions a grid can't (single points of failure, blast radius); value is high only if it shares the discovery engine with MIG04 and reuses existing graph rendering.

## Notes
- Data sources (same as MIG04): `customapi`; `serviceendpoint`/`sdkmessageprocessingstep`; `connectionreference` + connectors; `environmentvariabledefinition`/`environmentvariablevalue` (URLs); `workflow` `clientdata` (flow connectors/HTTP where available); `webresource` (JS API calls); Power Pages external calls; external URLs/endpoints extracted from all of the above.
- Shared discovery-engine reuse: consume the **same UI-free integration-discovery engine as MIG04**, but here it emits a normalized **node/edge graph model** (nodes: table, plugin, custom API, flow, connector, connection reference, environment variable, web resource, Power Pages component, external endpoint, external system; edges: component→endpoint, inbound/outbound); do not re-implement discovery.
- Graph rendering: reuse Solution Knowledge Graph's rendering/interaction patterns where feasible rather than a new canvas stack.
- Static scanning only, read-only: parse metadata and content; never invoke a discovered endpoint. Retrieval via `Service.RetrieveAll` off the UI thread with progress + cancellation; degrade missing `clientdata`/Power Pages metadata to informational findings.
- Secrets masked: secret-typed env-var values and credentials embedded in URLs are masked in the graph, detail panel, and exports; graph layout/filter settings round-trip via Load/SaveSettings.

---

## EPIC-MIG07 — Visualize integration dependencies as a component-to-endpoint graph
> **As** an **ARCH**, **I want** a filterable dependency graph linking Dataverse components to endpoints and external systems, **so that** I can see what breaks when an endpoint changes, a connector expires, or an external system is retired.

**Outcome:** an interactive dependency graph (typed nodes/edges, inbound/outbound), a dependency detail panel, a risk-findings panel (single points of failure, non-prod endpoints in prod, stale/undocumented integrations), and exportable graph + integration report — built on the discovery engine shared with MIG04.

---

## FEAT-MIG07-1 — Discover and build the graph model `[Planned]`
- **US-MIG07.1.1** `[Planned]` **As** an ARCH, **I want** integration touchpoints discovered and turned into typed graph nodes, **so that** every integration component is represented.
  - **AC:** Nodes cover table, plugin, custom API, flow, connector, connection reference, environment variable, web resource, Power Pages component, external endpoint, and external system; discovery reuses the MIG04 shared engine and runs off the UI thread.
- **US-MIG07.1.2** `[Planned]` **As** an ARCH, **I want** component-to-endpoint relationships and inbound vs outbound direction mapped as edges, **so that** dependency direction is explicit.
  - **AC:** Edges link components to the endpoints/systems they use, labeled inbound or outbound; the graph model is UI-free and unit-testable.

## FEAT-MIG07-2 — Explore the graph interactively `[Planned]`
- **US-MIG07.2.1** `[Planned]` **As** an ARCH, **I want** a graph canvas with endpoint, component-type, and environment filters, **so that** I can focus on a slice of the map.
  - **AC:** Filters show/hide nodes/edges by type/endpoint/environment; rendering reuses Solution Knowledge Graph patterns where feasible.
- **US-MIG07.2.2** `[Planned]` **As** an ARCH, **I want** a dependency detail panel for a selected node, **so that** I can see everything connected to it.
  - **AC:** Selecting a node lists its upstream/downstream neighbors and endpoint details; secrets are masked.

## FEAT-MIG07-3 — Detect integration risks in the graph `[Planned]`
- **US-MIG07.3.1** `[Planned]` **As** an ARCH, **I want** single points of failure and duplicate integrations detected, **so that** I know where blast radius and redundancy concentrate.
  - **AC:** Endpoints/connectors with high fan-in are flagged as single points of failure; endpoints reached by multiple components are flagged as duplicate integrations.
- **US-MIG07.3.2** `[Planned]` **As** a **SEC**, **I want** non-production endpoints in production and stale/undocumented integrations detected, **so that** I can remediate risky or unknown links.
  - **AC:** Non-prod host patterns in a prod scope, endpoints with no recent-usage/documentation metadata are separate finding types shown in a risk panel.

## FEAT-MIG07-4 — Score and export `[Planned]`
- **US-MIG07.4.1** `[Planned]` **As** an **MGR**, **I want** an integration risk score, **so that** I get a signal on integration fragility.
  - **AC:** Score is a UI-free weighted roll-up (single-points-of-failure, insecure/non-prod endpoints, undocumented) with severities Critical/High/Medium/Low/Info.
- **US-MIG07.4.2** `[Planned]` **As** an ARCH, **I want** to export the graph and an integration report, **so that** I can share the dependency map.
  - **AC:** Graph exports as an image/JSON node-edge model and the report to Excel, PDF, and self-contained HTML run off the UI thread; secrets stay masked; read-only.

## Definition of Done
- Follows suite conventions; read-only default; static-scan only (never invokes endpoints); secrets masked in graph, detail panel, and exports; export formats Excel, PDF, JSON, HTML + graph image.
- Integration-discovery engine is UI-free and **shared with MIG04 (API Integration Explorer)**; graph rendering reuses Solution Knowledge Graph patterns rather than duplicated.
- Testing skeleton under testing/IntegrationDependencyMap/ when implementation starts.
