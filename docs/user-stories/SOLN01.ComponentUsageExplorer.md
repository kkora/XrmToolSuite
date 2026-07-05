# Component Usage Explorer — User Stories

> **Status:** Active — implemented (see status tags below). Area tag `SOLN01`.
> **Source:** ported from `docs/backlog/05-Solution-Management/SOLN01.ComponentUsageExplorer.md`.
> **Design:** SDK-free usage model + change-safety rules in `src/Tools/XrmToolSuite.ComponentUsageExplorer/Analysis/` (`UsageModels.cs`, `UsageVerdictRules.cs`) with an SDK collector (`UsageCollector.cs`) on top; the WinForms host is `ComponentUsageExplorerControl`. Read-only.
> **Feasibility note:** the required/dependent inventory comes from the platform dependency APIs (`RetrieveRequiredComponentsRequest`, `RetrieveDependentComponentsRequest`, `RetrieveDependenciesForDeleteRequest`). Those APIs do not cover every component class (e.g. some Power Pages usage), so an incomplete answer is surfaced as a `RequiresDependencyReview` verdict rather than read as "safe". All analysis is read-only.

---

## EPIC-SOLN01 — Show the full usage footprint and change-safety of any solution component

> **As** a MAKER, **I want** to pick one Dataverse component and see everywhere it is used plus a change-safety verdict, **so that** I can modify, replace, or delete it without breaking something unseen.

**Outcome:** for a selected component, a required/dependent inventory, a per-type usage count, and a verdict (Safe to change / Change with caution / High impact / Do not delete / Requires dependency review / Requires ALM review), exportable to Excel/PDF/JSON/HTML.

---

## FEAT-SOLN01-1 — Find and select a component

- **US-SOLN01.1.1** `[Done]` **As** a MAKER, **I want** to search by display name, schema name, GUID, or type, **so that** I can locate the exact component I am about to change.
  - **AC:** Search runs off the UI thread with progress; results show component type, owning solution(s), and managed/unmanaged state; a component-type filter narrows results. *(`UsageCollector.Search` — metadata for tables/columns, `solutioncomponent` for the rest; `tstSearch` + `tscbType` + `tsbFind` → `ExecuteMethod` → `RunAsync`.)*
- **US-SOLN01.1.2** `[Done]` **As** a DEVOPS engineer, **I want** the component's solution membership listed, **so that** I know which packages ship it before I touch it.
  - **AC:** Every owning `solution` unique-name is listed with managed/unmanaged state; sourced from `solutioncomponent` joined to `solution` (Active/Default layers excluded). *(`grdResults` "Owning solution(s)" + "Managed" columns.)*

## FEAT-SOLN01-2 — Enumerate required and dependent components

- **US-SOLN01.2.1** `[Done]` **As** a CUST developer, **I want** the components the selected one *requires* and the components that *depend on* it, **so that** I see both directions of the dependency chain.
  - **AC:** Required and dependent lists come from the platform dependency APIs and are shown in separate grids grouped by component type, each row carrying its type + owning solution(s). *(`UsageCollector.BuildFootprint`; `grdRequired` / `grdDependents`.)*
- **US-SOLN01.2.2** `[Done]` **As** a CUST developer, **I want** unsupported or empty dependency results surfaced as informational, **so that** an incomplete platform answer never reads as "safe".
  - **AC:** A dependency API that throws flags the footprint `DependencyDataIncomplete`, which produces an Info/Medium "Dependency data incomplete" finding and a `RequiresDependencyReview` verdict when nothing more severe applies. *(`UsageCollector.TryDependencyCall`; `UsageVerdictRules.Evaluate`; covered by `ComponentUsageExplorerTests.IncompleteDependencyData_YieldsRequiresDependencyReview`.)*

## FEAT-SOLN01-3 — Usage detection and per-type summary

- **US-SOLN01.3.1** `[Done]` **As** a MAKER, **I want** each dependent resolved to its friendly type + name + owning solution, **so that** I can tell what would break at a glance.
  - **AC:** Dependent object ids are resolved to names via their base tables (`systemform`, `workflow`, `savedquery`, `plugintype`, …) and to owning solutions via `solutioncomponent`; unresolved ids fall back to a GUID label instead of failing. *(`UsageCollector.ResolveRefs` / `ResolveNames` / `OwningSolutions`.)*
- **US-SOLN01.3.2** `[Done]` **As** a MAKER, **I want** managed and cross-solution dependents highlighted, **so that** I know when a change escapes my own solution.
  - **AC:** Managed dependents are shaded in the grid and drive dedicated findings; cross-solution dependents (sharing no solution with the component) drive their own finding. *(`grdDependents` shading; `UsageVerdictRules` managed / cross-solution findings.)*
- **US-SOLN01.3.3** `[Done]` **As** an ADM, **I want** a usage-count-by-component-type summary, **so that** I can gauge blast radius at a glance.
  - **AC:** A usage-by-type grid lists each dependent type with its count. *(`UsageFootprint.BuildUsageByType`; `grdUsage`; covered by `ComponentUsageExplorerTests.UsageByType_TalliesDependentsByTypeName`.)*

## FEAT-SOLN01-4 — Impact scoring and change-safety verdict

- **US-SOLN01.4.1** `[Done]` **As** an ALM engineer, **I want** a change-safety verdict for the selected component, **so that** I get a single go/caution/stop signal.
  - **AC:** Verdict is one of Safe to change / Change with caution / High impact / Do not delete / Requires dependency review / Requires ALM review, driven deterministically by dependent count, high-value dependent types (forms/flows/plugins/apps), managed/cross-solution state, and whether the component is a table. A banded 0–100 impact score accompanies it. *(`UsageVerdictRules.Evaluate`; a coloured verdict banner `lblVerdict`; every verdict covered by `ComponentUsageExplorerTests`.)*
- **US-SOLN01.4.2** `[Done]` **As** an ALM engineer, **I want** a recommendation panel explaining the verdict, **so that** I know *why* and what to check next.
  - **AC:** A plain-language explanation names the highest-impact dependents and the required review steps (dependency review / ALM sign-off), shown beside the findings grid and carried into every export. *(`UsageReport.Explanation`; `txtExplanation`; `ComponentUsageExplorerTests.Explanation_NamesVerdictAndNextSteps`.)*

## FEAT-SOLN01-5 — Export usage report

- **US-SOLN01.5.1** `[Done]` **As** a release manager, **I want** the usage + impact report exported to Excel, PDF, JSON, and HTML, **so that** I can attach it to a change record.
  - **AC:** Excel/PDF/JSON go through the shared reporters and HTML is a self-contained BCL-built document; JSON carries the verdict, score, and machine-readable findings; export runs off the UI thread. *(`BuildReportModel` → `ExcelReportExporter` / `PdfReportExporter` / `JsonReportExporter`; `BuildHtml`; `tsbExport` dropdown.)*

---

## Definition of Done

- Follows suite conventions (`BaseToolControl`, `RunAsync`/`RetrieveAll`, `Load`/`SaveSettings`, progress + cancel); settings round-trip in `Load`/`ClosingPlugin`; `UpdateConnection` clears `MetadataCache`.
- Read-only (no write/delete); usage-detection and verdict rules stay UI-free and degrade query failures to Info findings.
- Export formats: Excel, PDF, JSON, HTML.
- Testing artifacts under `testing/ComponentUsageExplorer/`; SDK-free rules covered by `testing/UnitTests/ComponentUsageExplorerTests.cs`.
