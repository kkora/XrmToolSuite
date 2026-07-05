# API Documentation Builder — User Stories (Candidate / Backlog)

> **Status:** ✅ **SHIPPED** as `XrmToolSuite.ApiDocumentationBuilder`. Built as the OpenAPI + redaction
> specialist (distinct from the already-shipped **Custom API Explorer** / PLUGIN06, which browses & test-invokes
> APIs). SDK-free `ApiModels` + `Redactor` + `ApiCollector` read `customapi` / `customapirequestparameter` /
> `customapiresponseproperty` (+ `plugintype`), then emit a **Markdown** reference, a **self-contained
> theme-aware HTML** reference, a **raw JSON** model, and a **best-effort OpenAPI 3.0-style JSON** spec.
> Secret-named parameters are masked in samples/spec, free-text bearer tokens + URL query strings (SAS/trigger
> secrets) are stripped, and the operator can add redaction terms. As-built user stories:
> `docs/user-stories/DOC06.ApiDocumentationBuilder.md`. Deferred to future extensions: Word/PDF export (the
> sanctioned OpenXML/MigraDoc chains), legacy custom-action (`workflow`) coverage, and per-API solution scoping.
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 11 (Documentation), item 6. Not in pack file (except ERD/Doc generators relate to pack #11).
> **Suggested tag:** `DOC06` · **Suggested project:** `XrmToolSuite.ApiDocumentationBuilder`
> **Overlaps:** **Solution Documentation Generator** (SOLN05 candidate) documents Custom APIs as one section — this tool is the deep, API-focused specialist (parameters, responses, OpenAPI-style, examples, redaction). **Markdown / Word / HTML Documentation Generators** (DOC03/DOC04/DOC05) are format renderers whose API section can consume this tool's model. Custom API + plugin metadata overlaps **Plugin Dependency Graph** (PLUGIN01) extraction. Export patterns reuse **Deployment Risk Analyzer** (SHIPPED) PDF/Excel and the self-contained-HTML pattern.
> **Value/priority (my read):** Medium-High — Custom APIs and integration endpoints are frequently undocumented and painful for integration teams; an accurate, redaction-safe reference (with OpenAPI-style JSON) fills a real gap, though scope is narrower than the whole-solution generators.

## Notes
- Shared extracted-metadata model: build an API model (Custom API → parameters → response properties → binding → plugin/step → solution) reusing the Solution Documentation Generator (SOLN05) / Plugin Dependency Graph extraction where it already reads these tables.
- Core data: `customapi`, `customapirequestparameter`, `customapiresponseproperty`, `sdkmessage`/`sdkmessagerequest` (bound table, binding type, allowed custom processing step type), `plugintype`/`pluginassembly`/`sdkmessageprocessingstep` (plugin relationships), legacy custom actions via `workflow` (category = Action) where available, `environmentvariabledefinition`/`connectionreference` related to APIs, and `solution`/`solutioncomponent` for scoping. Web API endpoint *patterns* for tables are derived from metadata (schema/entity set names), not called live.
- **Safety / redaction (from Safety Requirements):** never expose secrets, tokens, keys, or full HTTP-trigger URLs; mask environment-specific values; example request/response payloads are **clearly labeled as templates**; provide **user-controlled redaction** so the operator can suppress additional values before export. Power Automate HTTP-trigger metadata is documented **only where available and without exposing the trigger secret/URL**.
- OpenAPI-style output: generate an **OpenAPI-style JSON** (paths/parameters/schemas) where feasible for Custom APIs — a best-effort spec, not a guaranteed-valid live endpoint contract.
- Export chain reuse: Markdown/HTML natively; **PDF/Word via the sanctioned MigraDoc/PdfSharp-GDI and DocumentFormat.OpenXml chains** already shipped in Deployment Risk Analyzer (DOCX needs OpenXml from the ClosedXML chain); JSON for the OpenAPI-style spec + raw model. Reuse the self-contained-HTML pattern.
- Read-only — reads metadata and produces docs; never invokes the APIs or modifies anything. Off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress and honor cancellation; cache per connection, clear on `UpdateConnection`. Keep the API-doc model + redaction/OpenAPI engine UI-free so it is unit-testable; degrade unavailable metadata to a documented note.

---

## EPIC-DOC06 — Document Dataverse Custom APIs and integration endpoints safely
> **As** a **TOOLDEV**, **I want** to discover and document Custom APIs, actions, and integration touchpoints with request/response detail and safe examples, **so that** integration and developer teams have clear, redaction-safe API references instead of tribal knowledge.

**Outcome:** for discovered Custom APIs (and legacy actions where available), a reference covering name/unique/display name, binding type, bound table, parameters, response properties, allowed step type, plugin type, dependencies, and safe template examples — exportable to Markdown, HTML, Word, PDF, and OpenAPI-style JSON with secrets masked.

---

## FEAT-DOC06-1 — Discovery and selection `[Planned]`
- **US-DOC06.1.1** `[Planned]` **As** a TOOLDEV, **I want** an API discovery dashboard listing Custom APIs in scope, **so that** I can see what exists before documenting.
  - **AC:** Custom APIs are discovered off the UI thread via `RunAsync`; the dashboard shows count and key attributes; scope persists via Load/SaveSettings.
- **US-DOC06.1.2** `[Planned]` **As** a TOOLDEV, **I want** to select one or more Custom APIs (and legacy actions where available), **so that** I document only what I need.
  - **AC:** Selection drives the API model; legacy custom actions are included when present and labeled as legacy.

## FEAT-DOC06-2 — API detail and parameters `[Planned]`
- **US-DOC06.2.1** `[Planned]` **As** a TOOLDEV, **I want** each API's name, unique name, display name, binding type, bound table, allowed custom processing step type, plugin type, and solution documented, **so that** its contract and ownership are clear.
  - **AC:** A detail viewer shows these fields sourced from `customapi`/`sdkmessage` metadata; missing fields degrade to a note.
- **US-DOC06.2.2** `[Planned]` **As** a TOOLDEV, **I want** a parameter/response grid, **so that** I see request parameters and response properties with types and requirement.
  - **AC:** `customapirequestparameter`/`customapiresponseproperty` render in a grid with name, type, required, and description.

## FEAT-DOC06-3 — Dependencies and related configuration `[Planned]`
- **US-DOC06.3.1** `[Planned]` **As** a TOOLDEV, **I want** plugin/custom-API relationships and API dependencies documented, **so that** I know what implements and consumes each API.
  - **AC:** A dependency panel links each API to its plugin type/step and dependent components (reusing Plugin Dependency Graph data where available).
- **US-DOC06.3.2** `[Planned]` **As** an ARCH, **I want** related environment variables, connection references, and table Web API endpoint patterns documented, **so that** integration context is complete.
  - **AC:** Related env vars/connection refs are listed; table endpoint patterns are derived from metadata (entity set names), not live calls.

## FEAT-DOC06-4 — Safe examples, OpenAPI-style spec, and redaction `[Planned]`
- **US-DOC06.4.1** `[Planned]` **As** an integration DEVOPS, **I want** generated example request/response payloads and an OpenAPI-style JSON spec, **so that** I can bootstrap client code.
  - **AC:** Examples are clearly labeled as templates; OpenAPI-style JSON is generated best-effort where feasible from parameter/response metadata.
- **US-DOC06.4.2** `[Planned]` **As** a SEC, **I want** secrets, tokens, keys, full HTTP-trigger URLs, and environment-specific values masked, with user-controlled redaction, **so that** documentation never leaks sensitive endpoint data.
  - **AC:** Sensitive values are masked by default; Power Automate HTTP-trigger metadata is shown only where available without the trigger secret/URL; the operator can suppress additional values before export.

## FEAT-DOC06-5 — Multi-format export `[Planned]`
- **US-DOC06.5.1** `[Planned]` **As** a TOOLDEV, **I want** to export API reference pages to Markdown, HTML, and JSON, **so that** docs feed repos, portals, and tooling.
  - **AC:** Markdown/HTML render natively (self-contained HTML); JSON carries the OpenAPI-style spec + raw model; export runs off the UI thread with redaction applied.
- **US-DOC06.5.2** `[Planned]` **As** an MGR, **I want** to export to Word and PDF, **so that** I can hand a polished API reference to clients/auditors.
  - **AC:** DOCX via the DocumentFormat.OpenXml dependency in the DRA ClosedXML chain; PDF via the sanctioned MigraDoc/PdfSharp-GDI chain; generation reports progress and is cancellable.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses the shared extracted model / Plugin Dependency Graph data and the sanctioned OpenXml (DOCX) + MigraDoc/PdfSharp (PDF) chains.
- Read-only default; API-doc model + redaction/OpenAPI engine stays UI-free where possible and degrades unavailable metadata to documented notes; secrets/tokens/keys/full trigger URLs never exposed and redaction is user-controlled.
- Export formats: Markdown, HTML, Word (DOCX), PDF, JSON (OpenAPI-style).
- Testing skeleton under testing/ApiDocumentationBuilder/ when implementation starts.
