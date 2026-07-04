# Solution Dependency Validator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 1 (ALM & DevOps), item 2. Not in pack file.
> **Suggested tag:** `ALM2` · **Suggested project:** `XrmToolSuite.SolutionDependencyValidator`
> **Overlaps:** Deployment Risk Analyzer already runs `RetrieveMissingDependencies` and checks env-var/connection-ref/plugin dependencies as part of a broader pre-deploy risk score — this tool is a *focused, dependency-tree-first* validator (readiness checklist + full dependency graph), not a risk score. Note the overlap and reuse the DRA dependency analyzer rather than reimplementing.
> **Value/priority (my read):** Medium — high real value, but a meaningful slice already ships inside Deployment Risk Analyzer; justify as the deep-dive/standalone dependency view.

## Notes
- Core APIs: `RetrieveDependenciesForDeleteRequest`/`RetrieveMissingDependenciesRequest`, `dependency` table, `solutioncomponent`, `RetrieveRequiredComponentsRequest`.
- Dependency severity maps directly to the source's rules: Critical (import fails), High (feature breaks), Medium (manual review), Low (cleanup), Info.
- Read-only; output is a dependency tree + deployment-readiness checklist, not a fix action.
- Reuse shared-core `RetrieveAll` for component/dependency paging; lift the DRA missing-dependency analyzer where possible and keep the tree-builder UI-free.
- Dependency graph can be large — build incrementally with progress and support cancellation; render as a tree view, not a full graph canvas (that's Solution Knowledge Graph's remit).
- Cross-references shipped Solution Knowledge Graph (visual dependency graph) — link out rather than duplicate the visualization.

---

## EPIC-ALM2 — Validate a solution's dependencies before export/import/deploy
> **As** an **ALM** engineer, **I want** to confirm a solution carries or can resolve every component it depends on, **so that** an import does not fail or silently break a feature in the target.

**Outcome:** a categorized dependency inventory, a missing-dependency list with severity, a dependency tree, and a deployment-readiness checklist, exportable for humans and pipelines.

---

## FEAT-ALM2-1 — Scan a solution and its components `[Planned]`
- **US-ALM2.1.1** `[Planned]` **As** an ALM engineer, **I want** to pick one solution and enumerate all its components, **so that** dependency discovery has a complete starting set.
  - **AC:** Components load off the UI thread via `RetrieveAll` with progress and cancellation.
- **US-ALM2.1.2** `[Planned]` **As** an ALM engineer, **I want** required and dependent components retrieved for that solution, **so that** I see both what it needs and what needs it.
  - **AC:** Uses `RetrieveRequiredComponents` / `RetrieveMissingDependencies`; both directions are shown.

## FEAT-ALM2-2 — Detect missing and hidden dependencies `[Planned]`
- **US-ALM2.2.1** `[Planned]` **As** a release manager, **I want** missing dependencies detected, **so that** an import will not hard-fail on unmet prerequisites.
  - **AC:** Missing components are Critical ("import likely fails") and name the required component and its owning solution.
- **US-ALM2.2.2** `[Planned]` **As** an ALM engineer, **I want** hidden, unmanaged, and out-of-solution dependencies flagged, **so that** I catch dependencies the package does not obviously declare.
  - **AC:** Dependencies resolved outside the selected solution or on an unmanaged component are distinct findings with severity.

## FEAT-ALM2-3 — Detect component-type-specific dependencies `[Planned]`
- **US-ALM2.3.1** `[Planned]` **As** a System Customizer, **I want** plugin-step, web-resource, JavaScript-library, and form-event dependencies detected, **so that** client and server logic still resolves in the target.
  - **AC:** Each type is validated and reported with the owning component.
- **US-ALM2.3.2** `[Planned]` **As** an ALM engineer, **I want** flow, custom-API, env-variable, connection-reference, security-role, app/module, and Power Pages dependencies detected, **so that** automation, security, and portals deploy intact.
  - **AC:** Each category appears as a summary card; missing ones degrade to the appropriate severity, and a query failure degrades to Info.

## FEAT-ALM2-4 — Categorize severity and build the dependency tree `[Planned]`
- **US-ALM2.4.1** `[Planned]` **As** an ALM engineer, **I want** every dependency graded Critical/High/Medium/Low/Info, **so that** I know which to fix before deploy versus clean up later.
  - **AC:** Severity follows the source rules (Critical = import fails … Info = informational).
- **US-ALM2.4.2** `[Planned]` **As** an ALM engineer, **I want** a navigable dependency tree with a component-impact panel, **so that** I can trace why a component is required.
  - **AC:** Tree view expands required/dependent chains; selecting a node shows its impact and recommended action.

## FEAT-ALM2-5 — Readiness checklist and export `[Planned]`
- **US-ALM2.5.1** `[Planned]` **As** a release manager, **I want** a deployment-readiness checklist generated from the findings, **so that** I have a concrete pre-import task list.
  - **AC:** Checklist lists each unresolved dependency with its remediation and is exportable.
- **US-ALM2.5.2** `[Planned]` **As** a DEVOPS engineer, **I want** the dependency report exported to Excel/PDF/JSON/HTML with a machine-readable pass flag, **so that** I can gate a pipeline on unresolved dependencies.
  - **AC:** JSON carries a pass/fail and suggested exit code; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default; dependency analyzers stay UI-free and degrade query failures to Info; overlap with Deployment Risk Analyzer documented and its analyzer reused.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/SolutionDependencyValidator/ when implementation starts.
