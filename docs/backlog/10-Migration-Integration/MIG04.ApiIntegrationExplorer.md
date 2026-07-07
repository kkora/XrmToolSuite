# API Integration Explorer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 10 (Migration & Integration), item 4. Not in pack file.
> **Suggested tag:** `MIG04` · **Suggested project:** `XrmToolSuite.ApiIntegrationExplorer`
> **Overlaps:** **Overlaps MIG07 'Integration Dependency Map'** — both discover the same integration touchpoints (custom APIs, service endpoints/webhooks, connection references, env-var URLs, JS/Power Pages external calls). **NOTE:** this tool is the *inventory + risk-findings* lens (grid of endpoints, insecure/hardcoded/duplicate findings); MIG07 is the *graph* lens (node-edge canvas, single-point-of-failure). Build **one shared integration-discovery engine** (UI-free) and give each tool its own presentation. Also touches Deployment Risk Analyzer (hardcoded-value/connection-reference logic — reuse).
> **Value/priority (my read):** Medium-High — "what integrations exist and where are the endpoints?" is a common enterprise support/security question and the discovery engine is reusable across MIG04/MIG07; value is high only if the engine is shared, not duplicated.

## Notes
- Data sources: `customapi`/`customapirequestparameter`/`customapiresponseproperty`; `serviceendpoint` (webhooks/Service Bus); `sdkmessageprocessingstep` linked to service endpoints; `environmentvariabledefinition`/`environmentvariablevalue` (URL/endpoint values); `connectionreference` + connector metadata; `workflow` `clientdata` for HTTP/custom-connector actions in flows where available; `webresource` (JavaScript static scan for API URLs); Power Pages templates with external URLs where available.
- Shared discovery engine reuse: a UI-free integration-discovery engine emits a normalized endpoint/touchpoint inventory consumed by both this tool and MIG07 (Integration Dependency Map); do not implement discovery twice.
- Reuse Deployment Risk Analyzer's hardcoded-value scanning and connection-reference parsing rather than re-implementing; degrade query failures (e.g., missing flow `clientdata`) to informational findings.
- Static scanning only, read-only: parse metadata and web-resource/flow content; never invoke a discovered endpoint. All retrieval via `Service.RetrieveAll`, off the UI thread with progress + cancellation.
- Data-touching output limits sampling and masks secrets: secret-typed env-var values and credentials embedded in URLs are masked in the grid and in exports; settings (scan options) round-trip via Load/SaveSettings.

---

## EPIC-MIG04 — Discover and document API/integration touchpoints in a solution or environment
> **As** an **ARCH**, **I want** a complete inventory of API and integration touchpoints with the components that use them and their endpoints, **so that** I have visibility into external dependencies for support, security, and migration.

**Outcome:** an endpoint inventory grid, a component-usage view, a risk-findings panel (hardcoded/insecure/duplicate/undocumented endpoints), an integration risk score, and an exportable integration report — built on a discovery engine shared with MIG07.

---

## FEAT-MIG04-1 — Discover integration components `[Planned]`
- **US-MIG04.1.1** `[Planned]` **As** an ARCH, **I want** custom APIs and their request/response contracts discovered, **so that** I know the environment's published API surface.
  - **AC:** Custom APIs are listed with parameters, response properties, and binding type; retrieval uses `Service.RetrieveAll` off the UI thread.
- **US-MIG04.1.2** `[Planned]` **As** an ARCH, **I want** service endpoints/webhooks and the plugin steps that invoke them discovered, **so that** outbound integration points are visible.
  - **AC:** `serviceendpoint` records and the `sdkmessageprocessingstep`s bound to them are listed with message/entity/stage.
- **US-MIG04.1.3** `[Planned]` **As** an ARCH, **I want** connection references, connectors, and flows using HTTP/custom connectors discovered, **so that** connector-based integrations are inventoried.
  - **AC:** Connection references and connector types are listed; flow HTTP/custom-connector actions are extracted from `clientdata` where available and degrade to informational findings otherwise.

## FEAT-MIG04-2 — Discover endpoints in configuration and code `[Planned]`
- **US-MIG04.2.1** `[Planned]` **As** an ARCH, **I want** environment variables containing URLs/endpoints discovered, **so that** externalized endpoints are inventoried.
  - **AC:** Env-var definitions/values matching URL/endpoint patterns are listed; secret-typed values are masked.
- **US-MIG04.2.2** `[Planned]` **As** an ARCH, **I want** JavaScript web resources and Power Pages templates scanned for API URLs, **so that** endpoints hidden in code are found.
  - **AC:** Static scan extracts external URLs from web-resource/Power Pages content; matches show the component and location; scanning is UI-free.

## FEAT-MIG04-3 — Detect integration risks `[Planned]`
- **US-MIG04.3.1** `[Planned]` **As** a **SEC**, **I want** hardcoded URLs and insecure HTTP endpoints detected, **so that** I can flag risky integrations.
  - **AC:** Hardcoded endpoints (not externalized to env vars) and `http://` endpoints are findings with severity; reuses DRA hardcoded-value scanning.
- **US-MIG04.3.2** `[Planned]` **As** an ARCH, **I want** environment-specific endpoints in production, duplicate endpoint usage, and undocumented endpoints detected, **so that** I can clean up and document integrations.
  - **AC:** Non-prod host patterns in a prod scope, endpoints reused by multiple components, and endpoints lacking documentation metadata are separate finding types.

## FEAT-MIG04-4 — Inventory, score, and export `[Planned]`
- **US-MIG04.4.1** `[Planned]` **As** an ARCH, **I want** an endpoint inventory grid with a component-usage panel, **so that** I can see every endpoint and what depends on it.
  - **AC:** Grid filters by component type/endpoint/risk; selecting an endpoint lists all using components.
- **US-MIG04.4.2** `[Planned]` **As** an **MGR**, **I want** an integration risk score, **so that** I get a signal on integration exposure.
  - **AC:** Score is a UI-free weighted roll-up of findings with severities Critical/High/Medium/Low/Info.
- **US-MIG04.4.3** `[Planned]` **As** an ARCH, **I want** an exported API integration report, **so that** I can share the inventory and risks.
  - **AC:** Exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; secrets/credentials stay masked; read-only.

## Definition of Done
- Follows suite conventions; read-only default; static-scan only (never invokes endpoints); secrets masked in grid and exports; export formats Excel, PDF, JSON, HTML.
- Integration-discovery engine is UI-free and **shared with MIG07 (Integration Dependency Map)** rather than duplicated; reuses DRA hardcoded-value/connection-reference logic.
- Testing skeleton under testing/ApiIntegrationExplorer/ when implementation starts.
