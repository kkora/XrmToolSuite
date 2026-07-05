# Custom API Explorer — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/08-Plugins-Custom-APIs/PLUGIN06.CustomApiExplorer.md`](../backlog/08-Plugins-Custom-APIs/PLUGIN06.CustomApiExplorer.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.CustomApiExplorer` · **Area tag:** `PLUGIN06`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (Dataverse read, WinForms grid, **live invocation**) — pending manual sign-off.

Discovers, documents and safely tests Custom APIs: a browsable, filterable catalog (unique/display name,
binding, is-function/private, request parameters, response properties, backing plugin type) plus a
**confirmation-gated test console** that builds a request from parameter metadata + typed inputs and invokes
the selected API, showing the typed response or the raw fault. **Inventory and documentation are read-only;
the invoke console is the only write path and always requires an explicit confirmation naming the API,
target and environment.** Never reads, stores or displays secrets. The value parsing, request-parameter
binding/validation, sample-snippet generation and the HTML/Markdown/CSV catalog exporters are SDK-free and
unit-tested; the Dataverse collector and the live invocation are manual-tested.

---

## EPIC-PLUGIN06 — Discover, document, and safely test Custom APIs `[Implemented]`
> **As** a **DEVOPS**, **I want** a complete inventory of Custom APIs with their parameters/responses/callers
> plus a guarded test console, **so that** I can understand and exercise them without spelunking through
> metadata or writing throwaway code.

**Outcome:** a browsable, exportable Custom API catalog and a confirmation-gated console that invokes a
selected API and shows the typed response or fault — with the execute path clearly separated from read-only browsing.

---

## FEAT-PLUGIN06-1 — Custom API inventory `[Implemented]`
- **US-PLUGIN06.1.1** `[Implemented]` List all Custom APIs in the org.
  - **AC:** APIs retrieved via `RetrieveAll` off the UI thread with progress + cancellation; the grid shows
    unique name, kind (function/action), private flag and binding. *(Collector: manual; grid: manual.)*
- **US-PLUGIN06.1.2** `[Implemented]` Filter the catalog.
  - **AC:** A live text filter narrows by unique/display name; the filter round-trips via Load/SaveSettings.
    *(Manual — WinForms.)*
- **US-PLUGIN06.1.3** `[Implemented]` Show each API's backing plugin type.
  - **AC:** `plugintypeid` resolved to the plugin type name; logic-less APIs are marked "(none)". *(Collector: manual.)*

## FEAT-PLUGIN06-2 — Parameters & responses `[Implemented]`
- **US-PLUGIN06.2.1** `[Implemented]` See each API's request parameters with type and optionality.
  - **AC:** Parameters listed with logical name, `CustomApiFieldType` and optional flag; an empty set is shown
    explicitly. **Automated** for the type model; collector *(manual)*.
- **US-PLUGIN06.2.2** `[Implemented]` See each API's response properties with types; functions vs. actions distinguished.
  - **AC:** Response properties listed with name and type; the detail pane labels Function vs. Action. *(Collector: manual.)*
- **US-PLUGIN06.2.3** `[Implemented]` Export parameter/response detail.
  - **AC:** The catalog (or the whole org) exports to Markdown/CSV including parameters and responses.
    **Automated** — `Markdown_ContainsApiParamsAndResponses`, `Csv_HasRowPerMember`.

## FEAT-PLUGIN06-3 — Callers & dependencies `[Implemented*]`
- **US-PLUGIN06.3.1** `[Implemented*]` See what references a Custom API.
  - **AC:** The model carries a caller list; a failed dependency query degrades to an informational note, not
    an error. *(Live dependency retrieval is manual; the note-not-throw contract is exercised by the collector's try/catch.)*
- **US-PLUGIN06.3.2** `[Implemented*]` See the SDK message wiring per API.
  - **AC:** The associated SDK message is surfaced when present. *(Manual — live metadata.)*

## FEAT-PLUGIN06-4 — Interactive test console (gated write/execute) `[Implemented]`
- **US-PLUGIN06.4.1** `[Implemented]` A form generated from the API's parameter metadata takes typed inputs.
  - **AC:** A parameter grid is generated per selected API; `RequestBuilder.Bind` validates required
    parameters and converts scalar values (invoke is blocked until valid). **Automated** —
    `Bind_MissingRequired_BlocksInvoke`, `Bind_ParsesScalars_AndCanInvoke`, `Bind_BadValue_IsAnError`,
    `Bind_OptionalOmitted_IsFine`, `Parse_*`.
- **US-PLUGIN06.4.2** `[Implemented*]` An explicit confirmation precedes any invocation.
  - **AC:** A dialog states the API name, the bound target/record and the environment, and invoke proceeds
    only on confirm (default button = Cancel); the call runs via `RunAsync` off the UI thread. *(Manual — GUI + live org.)*
- **US-PLUGIN06.4.3** `[Implemented*]` The response or fault is shown clearly.
  - **AC:** Typed response properties are rendered; on failure the raw `OrganizationServiceFault` is shown; no
    secrets are displayed or logged. *(Manual — live invocation.)*
- **US-PLUGIN06.4.4** `[Implemented]` The console never stores secrets.
  - **AC:** The settings POCO holds only the last filter — no parameter presets with secret values and no
    connection details. **Verified by inspection** of `CustomApiExplorerSettings`.

## FEAT-PLUGIN06-5 — Documentation export `[Implemented]`
- **US-PLUGIN06.5.1** `[Implemented]` Export a full Custom API catalog.
  - **AC:** HTML/Markdown export covers all APIs with parameters, responses, binding and backing plugin; the
    HTML is self-contained and theme-aware; export runs off the UI thread. **Automated** —
    `Html_IsSelfContainedAndEscapes`, `Markdown_ContainsApiParamsAndResponses`.
- **US-PLUGIN06.5.2** `[Implemented]` A sample invocation snippet per API.
  - **AC:** A Web API snippet is generated from parameter metadata, marked illustrative and containing no
    secrets. **Automated** — `Snippet_Action_IsPostWithBody_NoSecrets`, `Snippet_Function_IsGetWithQuery`.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress + cancellation). — **Done.**
- Inventory/documentation read-only; the test-console execute path is the only write and is
  confirmation-gated (scope named) and secret-safe. — **Done.**
- Request-building logic is UI-free and unit-tested; live invocation is a manual case (documented, not
  claimed as headless-passed). — **Done.**
- Testing under `testing/CustomApiExplorer/`; SDK-free logic covered by
  `testing/UnitTests/CustomApiExplorerTests.cs` (20 cases). — **Done** (collector/console/live-invoke pending manual).
