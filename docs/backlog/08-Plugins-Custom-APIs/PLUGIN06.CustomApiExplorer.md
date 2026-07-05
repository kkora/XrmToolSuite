# Custom API Explorer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `pack` — `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt`, idea #13. No direct equivalent in the ALL_PROMPTS (Doc 3) set. Merged into the Plugins & Custom APIs category (08-Plugins-Custom-APIs) as item 6; kept pack-sourced.
> **Suggested tag:** `PLUGIN06` · **Suggested project:** `XrmToolSuite.CustomApiExplorer`
> **Overlaps:** The Documentation track's "API Documentation Builder" candidate (ALL_PROMPTS) documents Custom APIs on paper; this tool adds an **interactive test console** to actually invoke them — that live-execution capability is the differentiator and is not in the Documentation candidate. Inventory/dependency plumbing could be shared with Solution Knowledge Graph (shipped).
> **Value/priority (my read):** High — Custom APIs are hard to inspect in-product, and a safe test console fills a real gap. The console is a write/execute surface, so it needs careful gating.

## Notes
- Primary data sources: `customapi`, `customapirequestparameter`, `customapiresponseproperty` metadata entities; binding type + bound entity from `customapi.bindingtype`/`boundentitylogicalname`; the backing plugin type via `customapi.plugintypeid`.
- Callers/dependencies: resolve `sdkmessage`/`sdkmessageprocessingstep` links and use `RetrieveDependenciesForDeleteRequest`/dependency queries to find components that reference the API. Degrade any failed dependency query to an informational finding, not an exception (analyzer convention).
- **The test console is a write/execute action.** Invoking a Custom API can mutate data or trigger side effects. It MUST: (a) be off by default / clearly separated from read-only browsing; (b) require an explicit confirmation dialog stating the API name, bound target, and that it will execute against the connected org; (c) never persist or display secrets/credentials; (d) surface the raw fault on failure.
- All reads and the invoke call go through `RunAsync`/`WorkAsync` off the UI thread; inventory paged via `Service.RetrieveAll`; progress + cancellation on the inventory scan.
- Request-body building (OrganizationRequest construction from parameter metadata + typed values) is UI-free and unit-testable in `testing/UnitTests/`; the actual execution is manual (needs a live org) — say so in the test summary.
- Settings (last-used API, saved parameter presets **excluding any secret values**) round-trip via load/save POCO.
- Read-only for inventory/documentation; execute path is the only write and is gated.

---

## EPIC-PLUGIN06 — Discover, document, and safely test Custom APIs
> **As** a DEVOPS, **I want** a complete inventory of Custom APIs with their parameters/responses/callers plus a guarded test console, **so that** I can understand and exercise them without spelunking through metadata or writing throwaway code.

**Outcome:** A browsable, exportable Custom API catalog (parameters, responses, binding, backing plugin, callers) and a confirmation-gated console that invokes a selected API and shows the typed response or fault.

---

## FEAT-PLUGIN06-1 — Custom API inventory `[Planned]`
- **US-PLUGIN06.1.1** `[Planned]` **As** a DEVOPS, **I want** to list all Custom APIs in the org, **so that** I have a single catalog.
  - **AC:** APIs retrieved via `Service.RetrieveAll` off the UI thread; grid shows unique name, display name, binding type, bound entity, is-function, is-private; progress + cancellation.
- **US-PLUGIN06.1.2** `[Planned]` **As** a DEVOPS, **I want** to filter by solution, publisher prefix, or binding type, **so that** I can focus on relevant APIs.
  - **AC:** Filters applied client-side or via query; count reflects active filter.
- **US-PLUGIN06.1.3** `[Planned]` **As** an ARCH, **I want** each API's backing plugin type shown, **so that** I know where the logic lives.
  - **AC:** `plugintypeid` resolved to plugin type name/assembly; unresolved (e.g., logic-less) APIs marked clearly.

## FEAT-PLUGIN06-2 — Parameters & responses `[Planned]`
- **US-PLUGIN06.2.1** `[Planned]` **As** a DEVOPS, **I want** to see each API's request parameters with type and optionality, **so that** I know how to call it.
  - **AC:** Request parameters listed with logical name, type, is-optional; loaded off the UI thread; empty set shown explicitly.
- **US-PLUGIN06.2.2** `[Planned]` **As** a DEVOPS, **I want** to see each API's response properties with types, **so that** I know what it returns.
  - **AC:** Response properties listed with name and type; functions vs. actions distinguished.
- **US-PLUGIN06.2.3** `[Planned]` **As** a CUST, **I want** parameter/response detail exportable, **so that** I can hand it to integrators.
  - **AC:** Selected API (or all) exported to Markdown/CSV including parameters and responses.

## FEAT-PLUGIN06-3 — Callers & dependencies `[Planned]`
- **US-PLUGIN06.3.1** `[Planned]` **As** an ARCH, **I want** to see what references a Custom API, **so that** I can assess impact before changing it.
  - **AC:** Dependency query lists referencing components (steps, flows, other solutions) where discoverable; a failed query yields an informational finding, not an error.
- **US-PLUGIN06.3.2** `[Planned]` **As** a DEVOPS, **I want** to see the SDK message/processing step wiring, **so that** I understand the execution registration.
  - **AC:** Associated `sdkmessage`/step shown per API when present.

## FEAT-PLUGIN06-4 — Interactive test console (gated write/execute) `[Planned]`
- **US-PLUGIN06.4.1** `[Planned]` **As** a DEVOPS, **I want** a form generated from an API's parameter metadata, **so that** I can supply typed inputs without hand-coding a request.
  - **AC:** Input controls generated per parameter type; required parameters validated before invoke is enabled; request building is a UI-free, unit-tested function.
- **US-PLUGIN06.4.2** `[Planned]` **As** a DEVOPS, **I want** an explicit confirmation before any invocation, **so that** I never execute against an org by accident.
  - **AC:** Confirmation dialog states the API name, bound target/record, and that it executes against the connected environment; invoke proceeds only on confirm; runs via `RunAsync` off the UI thread.
- **US-PLUGIN06.4.3** `[Planned]` **As** a DEVOPS, **I want** the response or fault shown clearly, **so that** I can debug the API.
  - **AC:** Typed response properties rendered; on failure the raw `OrganizationServiceFault` is shown; no secrets/credentials are displayed or logged.
- **US-PLUGIN06.4.4** `[Planned]` **As** a SEC, **I want** assurance the console never stores secrets, **so that** testing does not leak credentials.
  - **AC:** Parameter presets may be saved but any secret-typed/sensitive value is excluded from persisted settings; settings POCO holds no connection details.

## FEAT-PLUGIN06-5 — Documentation export `[Planned]`
- **US-PLUGIN06.5.1** `[Planned]` **As** a CUST, **I want** to export a full Custom API catalog, **so that** I can publish reference docs.
  - **AC:** HTML/Markdown export covering all APIs with parameters, responses, binding, backing plugin, and callers; export off the UI thread.
- **US-PLUGIN06.5.2** `[Planned]` **As** a DEVOPS, **I want** a sample invocation snippet per API, **so that** integrators have a starting point.
  - **AC:** Snippet generated from parameter metadata (e.g., Web API / SDK shape); marked as illustrative, contains no live secret values.

## Definition of Done
- Follows suite conventions; inventory/documentation read-only; the test-console execute path is the only write and is confirmation-gated (scope named) and secret-safe.
- Request-building logic is UI-free and unit-tested; live invocation is a manual case (documented, not claimed as headless-passed).
- Testing skeleton under testing/CustomApiExplorer/ when implementation starts.
