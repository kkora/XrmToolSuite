# Flow Documentation Generator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 7 (Power Automate), item 4. Not in pack file.
> **Suggested tag:** `PA4` · **Suggested project:** `XrmToolSuite.FlowDocumentationGenerator`
> **Overlaps:** Solution Documentation Generator (SOLN5) documents solutions broadly — this tool is a *cloud-flow-focused* doc generator with per-flow action trees, trigger/connector/connection-reference sections, and diagrams. Reuse DRA's `clientdata` parser and PA1's dependency model; align export/branding scaffolding with SOLN5 where it exists.
> **Value/priority (my read):** High — documentation is perennially missing, the data is fully SDK-feasible (static parse), and it directly reuses PA1/PA2 parsing, so incremental cost is low.

## Notes
- Core data: `workflow` (`category = 5`) + `clientdata` definition JSON; `connectionreference`, `environmentvariabledefinition`/`value`, `solutioncomponent`, entity/attribute metadata for readable table/column names, `systemuser` for owner. All static — no run history required.
- Documentation engine turns the parsed action tree into human-readable sections: summary, trigger, connectors, connection references, environment variables, action/logic structure, conditions/loops/scopes/branches/child flows, Dataverse tables/columns, HTTP/external calls (redacted), owners/status, run-after config, retry policies, error handling.
- Template/mode system: Executive summary, Technical specification, Support handoff, Audit documentation, Developer onboarding, Full flow reference, Release notes. Modes select which sections render.
- Diagrams: generate a dependency diagram and a flow action tree. Prefer a text/DSL diagram (e.g., Mermaid) rendered into HTML/Markdown; only invest in native PNG/SVG rendering (DRA already ships a PDF chain) if a picture export is required.
- Reuse the shared `clientdata` parser and PA1's dependency model; keep the parsing/generation logic UI-free so it is unit-testable and CI-liftable.
- Read-only; export only. **Never** render HTTP trigger URLs, connection secrets, tokens, or credentials into any document — redact consistently across every export format.

---

## EPIC-PA4 — Generate accurate, audience-appropriate flow documentation
> **As** an **ARCH**, **I want** complete, current documentation generated from the flow definition itself, **so that** audits, handoffs, onboarding, and release notes are accurate without manual authoring.

**Outcome:** per-flow (or per-solution) documents in multiple modes, each with a dependency diagram and action tree, exportable to Word/PDF/Markdown/HTML/Excel/JSON with secrets redacted.

---

## FEAT-PA4-1 — Scope selection and parsing `[Planned]`
- **US-PA4.1.1** `[Planned]` **As** an ARCH, **I want** to select flows by environment or solution, one or many, **so that** I can document a single flow or a whole solution.
  - **AC:** Flows load via `RetrieveAll` off the UI thread with progress/cancel; multi-select and solution scope both supported.
- **US-PA4.1.2** `[Planned]` **As** an ARCH, **I want** the definition parsed into a documentable model, **so that** every section has structured data to render.
  - **AC:** `clientdata` parses into the shared action-tree/dependency model; parse failures are noted in the document rather than crashing the run.

## FEAT-PA4-2 — Core flow documentation sections `[Planned]`
- **US-PA4.2.1** `[Planned]` **As** an ARCH, **I want** trigger, connectors, connection references, and environment variables documented, **so that** the flow's inputs and bindings are clear.
  - **AC:** Each section resolves human-readable names (connector, connection-reference logical name, env-var schema name) from metadata.
- **US-PA4.2.2** `[Planned]` **As** a support engineer, **I want** the action/logic structure documented — conditions, loops, scopes, branches, child flows, run-after, retry, error handling, and Dataverse tables/columns used, **so that** I can support the flow without opening the designer.
  - **AC:** Action tree, run-after config, retry policies, error-handling scopes, and table/column usage all render; HTTP/external calls appear with URLs redacted.

## FEAT-PA4-3 — Documentation modes and section control `[Planned]`
- **US-PA4.3.1** `[Planned]` **As** an ARCH, **I want** to pick a documentation mode (executive summary, technical spec, support handoff, audit, onboarding, full reference, release notes), **so that** each audience gets the right depth.
  - **AC:** Selecting a mode includes/excludes sections accordingly; a section checklist lets me override per document.
- **US-PA4.3.2** `[Planned]` **As** an ARCH, **I want** a branding/settings panel (title, org, logo, footer) that round-trips, **so that** documents look consistent and my choices persist.
  - **AC:** Branding settings load in `Load` and save in `ClosingPlugin` as a serializable POCO (no credentials); applied to every export.

## FEAT-PA4-4 — Diagrams and handoff artifacts `[Planned]`
- **US-PA4.4.1** `[Planned]` **As** an ARCH, **I want** a dependency diagram and a flow action tree generated, **so that** the structure is visual, not just prose.
  - **AC:** A diagram (text/DSL rendered to HTML/Markdown, with optional image export) and an action tree are produced per flow.
- **US-PA4.4.2** `[Planned]` **As** a release manager, **I want** support-handoff notes and release notes generated, **so that** deployment and support have ready-made artifacts.
  - **AC:** Handoff notes (owner, status, dependencies, known caveats) and release notes render in their respective modes.

## FEAT-PA4-5 — Preview and multi-format export `[Planned]`
- **US-PA4.5.1** `[Planned]` **As** an ARCH, **I want** a preview before export, **so that** I can confirm content and layout.
  - **AC:** Preview panel renders the selected mode/sections; regenerating is off the UI thread with progress.
- **US-PA4.5.2** `[Planned]` **As** an ARCH, **I want** export to Word, PDF, Markdown, HTML, Excel, and JSON, **so that** I can use the docs anywhere.
  - **AC:** Export runs off the UI thread (reuse DRA's ClosedXML/PdfSharp/MigraDoc chains where applicable); every format redacts secrets/URLs identically.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default; parsing/generation engine is UI-free and unit-tested; reuses shared `clientdata` parser and PA1 dependency model; secrets and HTTP trigger URLs never rendered in any document.
- Export formats: Word, PDF, Markdown, HTML, Excel, JSON.
- Testing skeleton under testing/FlowDocumentationGenerator/ when implementation starts.
