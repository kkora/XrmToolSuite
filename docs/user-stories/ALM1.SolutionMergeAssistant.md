# Solution Merge Assistant — User Stories

> **Status:** Implemented (Phase A). SDK-free comparison engine + collector + WinForms tool + exports.
> **Source:** `docs/backlog/01-ALM-DevOps/ALM1.SolutionMergeAssistant.md` (EPIC-ALM1).
> **Area tag:** `ALM1` · **Project:** `XrmToolSuite.SolutionMergeAssistant`
> **Read-only:** the tool *recommends* a merge strategy and emits a checklist — it never imports or writes solutions.

See the [index](../README.md) for personas, ID scheme, and status legend.

---

## EPIC-ALM1 — Compare and safely merge multiple Dataverse solutions before deployment

> **As** an **ALM** engineer, **I want** to compare two or more solutions from one environment and see every
> conflict before I merge them, **so that** I avoid duplicate components, version/publisher conflicts, and
> accidental overwrites at import time.

**Outcome:** a conflict inventory, a pre-merge risk verdict (Safe / Merge-with-warnings / Manual-review /
High-risk / Do-not-merge), a recommended merge strategy, and a merged-component checklist, exportable to
Excel/PDF/JSON/HTML.

---

## FEAT-ALM1-1 — Select and load solutions to compare `[Done]`

- **US-ALM1.1.1** `[Done]` **As** an ALM engineer, **I want** to pick two or more solutions from the connected
  environment, **so that** I can scope the comparison to the packages I intend to merge.
  - **AC:** Solutions load off the UI thread via `RunAsync`/`RetrieveAll` with progress; both managed and
    unmanaged are selectable (a checked list) and their `ismanaged`/version are shown.
- **US-ALM1.1.2** `[Done]` **As** an ALM engineer, **I want** each solution's components enumerated once and
  cached, **so that** repeated pairwise comparisons stay fast and cancellable.
  - **AC:** `MergeCollector` enumerates `solutioncomponent` once per solution via `RetrieveAll`, reports
    progress, and honors the `BackgroundWorker` cancellation token.

## FEAT-ALM1-2 — Detect duplicate and overlapping components `[Done]`

- **US-ALM1.2.1** `[Done]` **As** a Solution Architect, **I want** components that appear in more than one
  selected solution flagged, **so that** I know what will collide or double-register on merge.
  - **AC:** Duplicates are keyed by `(componenttype, objectid)`, list every owning solution, and the grid is
    grouped by category and severity.
- **US-ALM1.2.2** `[Done]` **As** a Solution Architect, **I want** duplicate web resources, plugin assemblies,
  plugin steps, and forms/views/business rules called out specifically, **so that** I catch the highest-churn
  overlap classes.
  - **AC:** Each of these types maps to its own category (`Web Resources`, `Plugin Assemblies`, `Plugin Steps`,
    `Forms`, `Views`, `Business Rules`) with its own severity (plugin assemblies/types/steps High; web
    resources/forms/views/business rules Medium; base tables/columns Low).

## FEAT-ALM1-3 — Detect version, publisher, and managed-state conflicts `[Done]`

- **US-ALM1.3.1** `[Done]` **As** an ALM engineer, **I want** version conflicts and publisher mismatches
  between the solutions flagged, **so that** I do not create layering or prefix collisions.
  - **AC:** Differing publisher prefixes are Medium (with a recommended standard prefix); differing solution
    versions are Low, or Medium when the solutions actually overlap.
- **US-ALM1.3.2** `[Done]` **As** an ALM engineer, **I want** managed/unmanaged conflicts detected, **so that**
  I understand which layer wins after merge.
  - **AC:** A component managed in one solution and unmanaged in another is a High `Managed State` finding.
    (Managed state degrades to the owning solution's `ismanaged` when per-component layering is unavailable.)

## FEAT-ALM1-4 — Detect config and completeness conflicts `[Done]`

- **US-ALM1.4.1** `[Done]` **As** an ALM engineer, **I want** environment-variable and connection-reference
  conflicts across the selected solutions flagged, **so that** merged automation still binds correctly.
  - **AC:** The same schema name packaged in more than one solution with **different** definition/value is a
    High finding; packaged **identically** in more than one solution is a Medium duplicate-ownership finding;
    packaged in only one solution is not a conflict (it merges cleanly).
- **US-ALM1.4.2** `[Deferred]` Missing/required-component completeness (`RetrieveMissingDependencies`) is
  tracked under ALM2 (Dependency Validator) and is out of scope for this comparison tool.

## FEAT-ALM1-5 — Merge risk verdict and recommendation `[Done]`

- **US-ALM1.5.1** `[Done]` **As** a Delivery Manager, **I want** all conflicts rolled into a single verdict,
  **so that** I get a go/no-go signal without reading every row.
  - **AC:** Verdict is one of Safe to merge / Merge with warnings / Manual review required / High-risk merge /
    Do not merge, driven by the severity mix (any High ⇒ at least High-risk; ≥3 High ⇒ Do not merge). A shared
    0–100 score/band is also computed.
- **US-ALM1.5.2** `[Done]` **As** a Solution Architect, **I want** a recommended merge strategy and a
  merged-component checklist, **so that** I have step-by-step guidance for the actual merge.
  - **AC:** The strategy names import order (ascending version), the publisher to standardize on, and
    per-conflict resolution; the checklist groups every actionable item by category and is exportable.

## FEAT-ALM1-6 — Export merge report `[Done]`

- **US-ALM1.6.1** `[Done]` **As** a release manager, **I want** the merge report exported to Excel, PDF, JSON,
  and HTML, **so that** I can attach it to a CAB/change record and gate a pipeline.
  - **AC:** HTML is self-contained and theme-aware (shared `HtmlDashboardBuilder`); JSON carries the verdict,
    solutions, metrics, checklist and a machine-readable conflict list; export runs off the UI thread.

## Definition of Done

- Follows suite conventions (`BaseToolControl`, `RunAsync`/`RetrieveAll`, `Load`/`SaveSettings`, progress+cancel).
- Read-only default (no import/write); the comparison engine is UI-free and SDK-free, and the collector
  degrades query failures to progress notes rather than throwing.
- Export formats: Excel, PDF, JSON, HTML.
- SDK-free comparison covered by `testing/UnitTests/SolutionMergeAssistantTests.cs`.
