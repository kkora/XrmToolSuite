# API Documentation Builder — User Stories (As-Built)

> **Tool:** `XrmToolSuite.ApiDocumentationBuilder` · **Tag:** `DOC06`
> **Source spec:** [docs/backlog/11-Documentation/DOC06.ApiDocumentationBuilder.md](../backlog/11-Documentation/DOC06.ApiDocumentationBuilder.md)
> **Testing:** [testing/ApiDocumentationBuilder/](../../testing/ApiDocumentationBuilder/)

Read-only tool that documents a Dataverse environment's **Custom APIs** (parameters, response properties,
binding, backing plugin) as a redaction-safe reference and a **best-effort OpenAPI 3.0-style JSON spec**, and
exports to Markdown, self-contained theme-aware HTML, raw JSON, and OpenAPI JSON. It is the OpenAPI + redaction
specialist — complementary to the shipped **Custom API Explorer** (PLUGIN06), which browses and confirmation-gated
*invokes* APIs. It never invokes an API. Secret-named parameters are masked in example payloads and the spec,
free-text bearer tokens + URL query strings (SAS / HTTP-trigger secrets) are stripped, and the operator can add
redaction terms. Follows the suite patterns (BaseToolControl, RunAsync, Load/SaveSettings); BCL-only.

The SDK-free model + redaction + emitters (`Api/ApiModels.cs`, `Api/Redactor.cs`, `Api/ApiDocEmitters.cs`,
`Api/OpenApiEmitter.cs`) are unit-tested in `testing/UnitTests/ApiDocumentationBuilderTests.cs`. The SDK
collector (`Api/ApiCollector.cs`) and the WinForms host are manual-tested (need a live connection).

---

## EPIC-DOC06 — Document Dataverse Custom APIs and integration endpoints safely
> **As** a **TOOLDEV**, **I want** to discover and document Custom APIs with request/response detail, safe
> examples, and an OpenAPI-style spec, **so that** integration and developer teams have clear, redaction-safe
> references instead of tribal knowledge.

**Outcome:** for the environment's Custom APIs, a reference covering unique/display name, binding, bound table,
plugin type, request parameters, and response properties, plus template examples and an OpenAPI-style JSON spec —
exportable to Markdown, HTML, raw JSON, and OpenAPI JSON with secrets masked.

---

## FEAT-DOC06-1 — Discovery `[Implemented]`
- **US-DOC06.1.1** `[Implemented]` **As** a TOOLDEV, **I want** to load and document the Custom APIs in the
  connected environment, **so that** I can see and reference what exists.
  - **AC:** Custom APIs load off the UI thread via `RunAsync`; the status shows the documented count; the
    catalog carries a documented note when the tables can't be read or none are present. **Manual** (live).

## FEAT-DOC06-2 — API detail and parameters `[Implemented]`
- **US-DOC06.2.1** `[Implemented]` **As** a TOOLDEV, **I want** each API's unique/display name, operation kind
  (Action/Function), private flag, binding type, bound table, and backing plugin type documented, **so that**
  its contract and ownership are clear.
  - **AC:** `ApiCollector` sources these from `customapi` (+ `plugintype` for the plugin name); the emitters
    render them; missing fields degrade gracefully. **Automated** — `TC-DOC06-MD-05` (binding/plugin), plus the
    HTML/OpenAPI cases; **Manual** for the live collector.
- **US-DOC06.2.2** `[Implemented]` **As** a TOOLDEV, **I want** a request-parameter and response-property grid
  with name, type, and requirement, **so that** I see the full request/response shape.
  - **AC:** Parameters/response properties render with name, type, and required flag; field types map to friendly
    labels and OpenAPI schema fragments. **Automated** — `TC-DOC06-MD-05`, `TC-DOC06-TYPE-09`.

## FEAT-DOC06-3 — Safe examples and OpenAPI-style spec `[Implemented]`
- **US-DOC06.3.1** `[Implemented]` **As** an integration DEVOPS, **I want** generated template request/response
  payloads and an OpenAPI-style JSON spec, **so that** I can bootstrap client code.
  - **AC:** Examples are labelled as templates; the OpenAPI 3.0-style spec emits one POST path per API with
    request-body + 200-response schemas built from parameter/response metadata (best-effort, not a guaranteed
    live contract — the spec description says so). **Automated** — `TC-DOC06-EX-04`, `TC-DOC06-OAS-07`.

## FEAT-DOC06-4 — Redaction safety `[Implemented]`
- **US-DOC06.4.1** `[Implemented]` **As** a SEC, **I want** secrets, tokens, keys, and full HTTP-trigger URLs
  masked with user-controlled redaction, **so that** documentation never leaks sensitive endpoint data.
  - **AC:** Secret-named parameters (built-in heuristics + operator-supplied terms) get a masked sample and are
    annotated (`format: password`, `x-redacted`) in the spec; free-text bearer tokens and URL query strings are
    stripped from descriptions. **Automated** — `TC-DOC06-RED-01/02/03`, `TC-DOC06-OAS-08`.

## FEAT-DOC06-5 — Multi-format export `[Implemented]`
- **US-DOC06.5.1** `[Implemented]` **As** a TOOLDEV, **I want** to export to Markdown, self-contained HTML, raw
  JSON, and OpenAPI JSON, **so that** docs feed repos, portals, and codegen tooling.
  - **AC:** Markdown/HTML render natively (HTML self-contained + theme-aware, content escaped); JSON carries the
    raw model; OpenAPI JSON carries the spec; export runs off the UI thread with redaction applied. **Automated**
    — `TC-DOC06-MD-05`, `TC-DOC06-HTML-06`, `TC-DOC06-JSON-10`, `TC-DOC06-OAS-07`.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress, Help button,
  required plugin icons); read-only, never invokes an API.
- API-doc model + redaction/OpenAPI engine stays UI-free / SDK-free and degrades unavailable metadata to notes;
  secrets/tokens/keys/full trigger URLs never exposed and redaction is user-controlled. BCL-only.
- Export formats: Markdown, self-contained HTML, JSON (raw model), OpenAPI-style JSON.
  *(Word/PDF via the sanctioned chains are a documented future extension.)*
- Testing artifacts under `testing/ApiDocumentationBuilder/`; SDK-free tests in `testing/UnitTests`.
