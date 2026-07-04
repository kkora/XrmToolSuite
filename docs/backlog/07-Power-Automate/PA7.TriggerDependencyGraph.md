# Trigger Dependency Graph — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 7 (Power Automate), item 7. Not in pack file.
> **Suggested tag:** `PA7` · **Suggested project:** `XrmToolSuite.TriggerDependencyGraph`
> **Overlaps:** PA1 Flow Dependency Analyzer (parses the same `clientdata`; PA1 covers full dependency trees while PA7 focuses on *triggers* and cross-flow cascade). Solution Knowledge Graph (general component graph/visualization). Reuse DRA/PA1 `clientdata` parser and, if built, PA1's dependency model; PA7 adds the trigger taxonomy, duplicate/cascade detection, and graph rendering.
> **Value/priority (my read):** Medium-High — "what starts this flow and what does it cascade into?" is a real troubleshooting gap and fully SDK-feasible (static parse); the graph visualization is the main build cost.

## Notes
- Core data: `workflow` (`category = 5`) + `clientdata` trigger definition; entity/attribute metadata (readable Dataverse trigger table/columns); `connectionreference`, `environmentvariabledefinition`; child-flow (`RunFlow`/`Workflow`) references for parent/child edges. All static — no run history required.
- Trigger taxonomy from `clientdata`: Dataverse (with table + message create/update/delete/create-or-update/row-selected + filter conditions + filtering attributes), Recurrence (schedule), Manual, HTTP request, SharePoint, Outlook, Teams, custom connector, child flow, other.
- Graph model nodes: Flow, Trigger, Table, Event, Connector, Child flow, External system, Environment variable, Connection reference. Edges connect triggers to their sources and parent flows to child flows. Keep the graph-building logic UI-free/unit-testable; rendering is a separate layer.
- Detection rules: duplicate triggers on same table/event, broad Dataverse triggers without filter conditions, disabled flows that still have active downstream dependencies, multiple flows on the same Dataverse event, and cascading-flow risk (chains of triggers/child flows).
- Rendering: a graph canvas in-tool plus export. For PNG/SVG, prefer a text/DSL (e.g., Mermaid/GraphViz-style) rendered to HTML/SVG; reuse DRA's PDF chain for PDF. Keep image generation optional if it becomes heavy.
- Read-only; output is the graph, trigger details, duplicate/cascade findings, and export. **HTTP trigger endpoint metadata must be shown without exposing secrets or full callback URLs** (the source is explicit) — redact tokens/signatures.

---

## EPIC-PA7 — Visualize flow triggers and their cross-flow dependencies
> **As** an **ADM**, **I want** to see what starts each flow and how triggers relate across tables, schedules, connectors, and child flows, **so that** I can troubleshoot and assess impact quickly.

**Outcome:** a trigger inventory, a dependency graph over the standard node types, duplicate/broad-trigger and cascade-risk findings, and multi-format export.

---

## FEAT-PA7-1 — Trigger inventory and categorization `[Planned]`
- **US-PA7.1.1** `[Planned]` **As** an ADM, **I want** all flow triggers listed and categorized by type (Dataverse, Recurrence, Manual, HTTP, SharePoint, Outlook, Teams, custom connector, child flow, other), **so that** I can see how the estate is triggered.
  - **AC:** Triggers load via `RetrieveAll` off the UI thread with progress/cancel; each trigger's type is parsed from `clientdata`; a trigger-type filter is available.
- **US-PA7.1.2** `[Planned]` **As** an ADM, **I want** a table/event filter, **so that** I can focus on triggers for a specific Dataverse table or event.
  - **AC:** Filtering by table and message narrows the inventory and the graph.

## FEAT-PA7-2 — Trigger detail extraction `[Planned]`
- **US-PA7.2.1** `[Planned]` **As** an ADM, **I want** Dataverse trigger details — table, operation, filter conditions, and filtering attributes where available — shown per flow, **so that** I know exactly what fires it.
  - **AC:** Table, message, filter expression, and column filters are resolved from `clientdata`; unresolved names degrade to a warning, not an exception.
- **US-PA7.2.2** `[Planned]` **As** an ADM, **I want** recurrence schedules and HTTP trigger endpoint metadata shown with secrets redacted, **so that** I understand timing and inbound triggers without leaking URLs.
  - **AC:** Recurrence schedule renders; HTTP trigger shows method/shape but redacts callback URL, SAS tokens, and signatures.

## FEAT-PA7-3 — Dependency graph `[Planned]`
- **US-PA7.3.1** `[Planned]` **As** an ADM, **I want** a trigger dependency graph over the standard node types (flow, trigger, table, event, connector, child flow, external system, environment variable, connection reference), **so that** I can see relationships visually.
  - **AC:** The UI-free graph builder produces nodes/edges from parsed data; the graph canvas renders them and supports selecting a node to focus.
- **US-PA7.3.2** `[Planned]` **As** an ADM, **I want** parent/child flow relationships shown in the graph, **so that** I can trace flow chains.
  - **AC:** Parent→child edges are built from child-flow references and rendered as a distinct edge type.

## FEAT-PA7-4 — Duplicate and cascade risk detection `[Planned]`
- **US-PA7.4.1** `[Planned]` **As** an ADM, **I want** duplicate triggers on the same table/event and broad Dataverse triggers without filters detected, **so that** I can reduce redundant or over-firing triggers.
  - **AC:** Duplicate-trigger and no-filter findings list the affected flows with severity; unit-tested against fixtures.
- **US-PA7.4.2** `[Planned]` **As** an ADM, **I want** cascading-flow risk, multiple flows on one Dataverse event, and disabled flows with active downstream dependencies detected, **so that** I can foresee chain reactions and orphaned dependencies.
  - **AC:** Cascade chains, shared-event fan-out, and disabled-flow-with-active-dependency each produce a distinct finding.

## FEAT-PA7-5 — Export `[Planned]`
- **US-PA7.5.1** `[Planned]` **As** an ADM, **I want** the graph exported to PNG, SVG, PDF, Excel, JSON, and HTML, **so that** I can share it in reviews and docs.
  - **AC:** Export runs off the UI thread; JSON carries the node/edge model and findings; image/PDF formats render the graph; every format redacts HTTP endpoint secrets/URLs.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default; graph-building and detection logic is UI-free and unit-tested; reuses DRA/PA1 `clientdata` parser and dependency model; HTTP trigger URLs/secrets never exposed in the UI or any export.
- Export formats: PNG, SVG, PDF, Excel, JSON, HTML.
- Testing skeleton under testing/TriggerDependencyGraph/ when implementation starts.
