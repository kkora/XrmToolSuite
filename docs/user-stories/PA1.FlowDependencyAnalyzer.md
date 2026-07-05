# Flow Dependency Analyzer - User Stories

Static, read-only dependency mapper for Power Automate cloud flows. Parses each flow's `clientdata`
JSON into a dependency footprint (trigger, Dataverse tables/columns, connectors, connection references,
environment variables, child flows, custom APIs, HTTP actions), builds a reverse "which flows break if I
change this component?" impact view, raises deployment-risk findings, and exports the result. Every HTTP
endpoint URL, SAS/trigger URL and secret is **redacted** — never stored or exported.

Area tag: **PA1**. See the [index](../README.md) for personas, ID scheme, and status legend. Traces to
`testing/FlowDependencyAnalyzer/TEST_CASES.md`.

---

## EPIC-PA1 - Map cloud-flow dependencies and component impact

> **As** an **ALM** engineer, **I want** to see every Dataverse, connector, connection-reference,
> environment-variable, custom-API and child-flow dependency of each cloud flow, **so that** I know which
> flows a proposed change will impact before I deploy.

**Outcome:** a per-flow dependency tree plus a reverse "impacted flows for this component" view,
missing-dependency warnings, and a deployment-readiness checklist, exportable for humans and pipelines.

---

## FEAT-PA1-0 - Scaffold & shared wiring `[Done]`

- **US-PA1.0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings and background execution via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Tool appears in XTB, connects, runs the collector via `RunAsync`, persists settings on close;
    no `your-github-username` / "Load sample" template leftovers remain.

## FEAT-PA1-1 - Flow inventory and filtering `[Done]`

- **US-PA1.1.1** `[Done]` **As** an ALM engineer, **I want** all cloud flows listed with owner and status,
  **so that** I have the full set to analyze.
  - **AC:** Flows load off the UI thread via `Service.RetrieveAll` (`workflow`, `category=5`, `type=1`) with
    progress and cancellation; owner and state (Activated/Draft) show per row.
- **US-PA1.1.2** `[Done]` **As** a maker, **I want** to filter flows by solution, owner, status, connector,
  trigger type and referenced table, **so that** I can focus on a relevant subset.
  - **AC:** Filters apply against parsed dependency data; the solution filter uses `solutioncomponent`
    membership (type 29).

## FEAT-PA1-2 - Trigger and Dataverse dependency analysis `[Done]`

- **US-PA1.2.1** `[Done]` **As** an ALM engineer, **I want** each flow's trigger type and (for Dataverse
  triggers) table/message parsed, **so that** I know what starts it.
  - **AC:** Trigger type and Dataverse table + message (Create/Update/Delete/…) are resolved from
    `clientdata`; a missing table degrades to a warning finding.
- **US-PA1.2.2** `[Done]` **As** a maker, **I want** the Dataverse tables and columns each flow reads/writes
  listed, **so that** I know what a schema change affects.
  - **AC:** Table (`entityName`) and column (`$select`) references are extracted from action bodies;
    unresolved logical names are flagged as possible missing metadata.

## FEAT-PA1-3 - Connector, connection-reference and environment-variable dependencies `[Done]`

- **US-PA1.3.1** `[Done]` **As** an ALM engineer, **I want** connectors, connection references and
  environment variables each flow depends on listed, **so that** I can confirm they exist in the target.
  - **AC:** Connector ids, `connectionreference` logical names (resolved via `properties.connectionReferences`)
    and environment-variable references (`@parameters('…')` / `environmentVariables`) are shown.
- **US-PA1.3.2** `[Done]` **As** a release manager, **I want** flows using a direct connection instead of a
  connection reference detected, **so that** the package is portable across environments.
  - **AC:** Direct-connection usage (a `connectionName` GUID that maps to no connection reference) is a
    distinct **High** finding with remediation.

## FEAT-PA1-4 - Child-flow, custom-API and HTTP dependencies `[Done]`

- **US-PA1.4.1** `[Done]` **As** an Enterprise Architect, **I want** child-flow relationships and custom-API
  invocations discovered, **so that** I understand cross-flow and pro-code coupling.
  - **AC:** `Workflow`/RunFlow child-flow references and unbound/bound custom-API action names are extracted
    and linked into the dependency tree.
- **US-PA1.4.2** `[Done]` **As** an ALM engineer, **I want** HTTP/action dependencies surfaced with any URLs
  redacted, **so that** external coupling is visible without leaking secrets.
  - **AC:** HTTP actions are listed by name; endpoint URLs, SAS/trigger URLs and auth values are **redacted**,
    never shown or exported (stored as `[redacted]`).

## FEAT-PA1-5 - Missing/hardcoded metadata risk findings `[Done]`

- **US-PA1.5.1** `[Done]` **As** a release manager, **I want** flows referencing deleted/missing metadata
  detected, **so that** I catch import breakers early.
  - **AC:** Missing table = **Critical**; missing column / connection-reference / environment-variable =
    **High**, each with a remediation note; a lookup that fails degrades to **Info** rather than throwing.
- **US-PA1.5.2** `[Done]` **As** a Technical-Debt owner, **I want** hardcoded environment URLs, GUIDs and
  table names in flow definitions flagged, **so that** flows are environment-agnostic.
  - **AC:** Hardcoded-value findings (**Medium**) list the action and the offending literal, redacting
    anything secret.

## FEAT-PA1-6 - Component impact, dependency tree and export `[Done]`

- **US-PA1.6.1** `[Done]` **As** an ALM engineer, **I want** to pick a component (table, column, connector,
  connection reference, environment variable, child flow, custom API) and get the impacted flows, **so that**
  I can scope a change.
  - **AC:** The reverse-lookup panel returns every flow depending on the selected component.
- **US-PA1.6.2** `[Done]` **As** a DEVOPS engineer, **I want** the dependency tree and findings exported to
  Excel/PDF/JSON/HTML with a deployment-readiness checklist, **so that** I can attach it to a release and gate
  a pipeline.
  - **AC:** Export runs off the UI thread; JSON carries the impacted-flow map and a pass/fail readiness flag;
    secrets/URLs are redacted in every format.

## Definition of Done

- Follows suite conventions (`BaseToolControl`, `RunAsync`/`RetrieveAll`, Load/SaveSettings, progress+cancel).
- Read-only; the dependency-discovery engine (`FlowClientDataParser`, `FlowRiskRules`, `FlowModels`) is UI-free
  and unit-tested on `clientdata` fixtures; overlap with the Deployment Risk Analyzer's `clientdata`/
  connection-reference logic is documented and reused in spirit.
- Secrets and HTTP trigger URLs are never exposed in any surface or export.
- Export formats: Excel, PDF, JSON, HTML.
