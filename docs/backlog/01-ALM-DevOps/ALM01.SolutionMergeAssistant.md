# Solution Merge Assistant — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 1 (ALM & DevOps), item 1. Also in pack file (`prompt/2...`) idea #6 'Solution Merge Assistant' — same tool.
> **Suggested tag:** `ALM01` · **Suggested project:** `XrmToolSuite.SolutionMergeAssistant`
> **Overlaps:** Deployment Risk Analyzer (dependency, publisher-prefix, cross-solution duplicate, env-var and connection-ref checks) — reuse those analyzers; this tool is multi-solution *comparison/merge*, not single-solution pre-import risk.
> **Value/priority (my read):** High — merging vendor/feature-team solutions is a recurring, error-prone ALM pain with no first-class Microsoft tooling.

## Notes
- Core data: `solution`, `solutioncomponent` (componenttype + objectid), `publisher`, `webresource`, `pluginassembly`, `sdkmessageprocessingstep`, `systemform`, `savedquery`, plus `environmentvariabledefinition`/`connectionreference` for those conflict classes.
- Layering/ownership reads from `msdyn_componentlayer` / `RetrieveSolutionComponents`; managed vs unmanaged from `ismanaged`.
- Read-only by default — the tool *recommends* a merge strategy and emits a checklist; it does not write or import solutions.
- Reuse shared-core `RetrieveAll` for component paging and the DeploymentRiskAnalyzer duplicate/publisher-prefix logic where liftable; keep comparison analyzers UI-free.
- Two-or-more-solution selection means comparison is O(n²) across component sets — cache metadata and report progress per pair.
- Cross-references candidate ALM02 (Dependency Validator) for the "missing required components" class.

---

## EPIC-ALM01 — Compare and safely merge multiple Dataverse solutions before deployment
> **As** an **ALM** engineer, **I want** to compare two or more solutions from one environment and see every conflict before I merge them, **so that** I avoid duplicate components, version/publisher conflicts, and accidental overwrites at import time.

**Outcome:** a per-pair conflict inventory, a pre-merge risk verdict (Safe / Merge-with-warnings / Manual-review / High-risk / Do-not-merge), and a merged-component checklist, exportable to Excel/PDF/JSON/HTML.

---

## FEAT-ALM01-1 — Select and load solutions to compare `[Planned]`
- **US-ALM01.1.1** `[Planned]` **As** an ALM engineer, **I want** to pick two or more solutions from the connected environment, **so that** I can scope the comparison to the packages I intend to merge.
  - **AC:** Solutions load off the UI thread via `RunAsync`/`RetrieveAll` with progress; both managed and unmanaged are selectable and their `ismanaged`/publisher shown.
- **US-ALM01.1.2** `[Planned]` **As** an ALM engineer, **I want** each solution's components enumerated once and cached, **so that** repeated pairwise comparisons stay fast and cancellable.
  - **AC:** Component retrieval reports progress and honors the `BackgroundWorker` cancellation token.

## FEAT-ALM01-2 — Detect duplicate and overlapping components `[Planned]`
- **US-ALM01.2.1** `[Planned]` **As** a Solution Architect, **I want** components that appear in more than one selected solution flagged, **so that** I know what will collide or double-register on merge.
  - **AC:** Duplicates keyed by (componenttype, objectid) list every owning solution; grid is groupable by component type.
- **US-ALM01.2.2** `[Planned]` **As** a Solution Architect, **I want** duplicate web resources, plugin assemblies, plugin steps, and forms/views/business rules called out specifically, **so that** I catch the highest-churn overlap classes.
  - **AC:** Each of these types is a distinct conflict category with its own count in the summary.

## FEAT-ALM01-3 — Detect version, publisher, and managed-state conflicts `[Planned]`
- **US-ALM01.3.1** `[Planned]` **As** an ALM engineer, **I want** version conflicts and publisher mismatches between the solutions flagged, **so that** I do not create layering or prefix collisions.
  - **AC:** Differing publisher prefixes and solution versions on shared components are reported with severity.
- **US-ALM01.3.2** `[Planned]` **As** an ALM engineer, **I want** managed/unmanaged and component-ownership/layering conflicts detected, **so that** I understand which layer wins after merge.
  - **AC:** A component managed in one solution and unmanaged in another is High; layering/ownership conflicts note the active layer.

## FEAT-ALM01-4 — Detect config and completeness conflicts `[Planned]`
- **US-ALM01.4.1** `[Planned]` **As** an ALM engineer, **I want** environment-variable and connection-reference conflicts across the selected solutions flagged, **so that** merged automation still binds correctly.
  - **AC:** Same schema name with different definition/value, and references included in one solution but not another, are findings.
- **US-ALM01.4.2** `[Planned]` **As** an ALM engineer, **I want** missing required components and deleted/deprecated components detected, **so that** the merged set is importable and not silently removing things.
  - **AC:** Missing required components (via `RetrieveMissingDependencies`) and components present in an older-version solution but absent in another are reported.

## FEAT-ALM01-5 — Merge risk score and recommendation `[Planned]`
- **US-ALM01.5.1** `[Planned]` **As** a Delivery Manager, **I want** all conflicts rolled into a single verdict, **so that** I get a go/no-go signal without reading every row.
  - **AC:** Verdict is one of Safe to merge / Merge with warnings / Manual review required / High-risk merge / Do not merge, driven by weighted severity.
- **US-ALM01.5.2** `[Planned]` **As** a Solution Architect, **I want** a recommended merge strategy and a merged-component checklist, **so that** I have step-by-step guidance for the actual merge.
  - **AC:** Recommendation names order of import, publisher to standardize on, and per-conflict resolution; checklist is exportable.

## FEAT-ALM01-6 — Export merge report `[Planned]`
- **US-ALM01.6.1** `[Planned]` **As** a release manager, **I want** the merge report exported to Excel, PDF, JSON, and HTML, **so that** I can attach it to a CAB/change record and gate a pipeline.
  - **AC:** HTML is self-contained and theme-aware; JSON carries the verdict and a machine-readable conflict list; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default (no import/write); comparison analyzers stay UI-free and degrade query failures to Info findings.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/Tools/SolutionMergeAssistant/ when implementation starts.
