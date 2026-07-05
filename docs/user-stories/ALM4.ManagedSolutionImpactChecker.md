# Managed Solution Impact Checker — User Stories

> **Status:** Active — implemented (see status tags below). Area tag `ALM4`.
> **Source:** ported from `docs/backlog/01-ALM-DevOps/ALM4.ManagedSolutionImpactChecker.md`.
> **Engine:** SDK-free layering rules in `src/Tools/XrmToolSuite.ManagedSolutionImpactChecker/Analysis/LayerImpactRules.cs` over a `LayerAnalysisInput` (`ImpactModels.cs`); the live-connection `ImpactCollector.cs` builds that input read-only.
> **Overlap:** shares the deletion/data-loss and missing-dependency concepts with the Deployment Risk Analyzer; this tool goes deeper on *solution layering* (active-layer ownership, unmanaged layers above managed, and explicit Upgrade/Update/Patch/Holding delete-vs-overwrite semantics). Read-only — it never imports, upgrades, or deletes.

---

## EPIC-ALM4 — Analyze the impact of importing / upgrading / patching / deleting a managed solution

> **As** an ALM engineer, **I want** to understand how a managed-solution change interacts with existing layers before I apply it, **so that** I do not overwrite customizations, delete components with data, or hit publisher / managed-property restrictions in production.

**Outcome:** a layer-analysis dashboard, a banded impact score, a per-path (Upgrade / Update / Patch / Holding) risk breakdown, a pre-upgrade checklist, and rollback guidance in a CAB-ready report.

---

## FEAT-ALM4-1 — Select managed solution and analyze layers

- **US-ALM4.1.1** `[Done]` **As** an ALM engineer, **I want** to pick a managed solution and see its current solution layers, **so that** I know the layering context before changing it.
  - **AC:** Managed solutions load off the UI thread via `RetrieveAll` with progress; active-layer ownership per component is captured. *(`LoadSolutions` filters `solution` by `ismanaged=true`; `ImpactCollector` reads `solutioncomponent` and records `OwningSolution`/`IsManaged` per `ComponentLayer`.)*
- **US-ALM4.1.2** `[Done]` **As** an ALM engineer, **I want** unmanaged customizations sitting above managed layers detected, **so that** I know what an upgrade might reassert or an admin has overridden.
  - **AC:** Components with an active unmanaged layer over the managed one are flagged with the owning layer. *(`ImpactCollector` reads `msdyn_componentlayer`; `LayerImpactRules` raises "Unmanaged customization above managed layer" (Medium). Covered by `ManagedSolutionImpactCheckerTests`.)*

## FEAT-ALM4-2 — Overwrite and deletion impact

- **US-ALM4.2.1** `[Done]` **As** an ALM engineer, **I want** components that may be overwritten by the change flagged, **so that** I protect customer-specific customizations.
  - **AC:** On a delete-capable path (Upgrade / Holding) a component with an active unmanaged layer raises a High "Component would be overwritten"; Update / Patch do not. *(Path-aware escalation in `LayerImpactRules.EvaluateUnmanagedLayers`.)*
- **US-ALM4.2.2** `[Done]` **As** an ALM engineer, **I want** delete impact analyzed, **so that** I never remove a table/column (and its data) unknowingly.
  - **AC:** Removed tables → Critical (data loss), columns → High, other components → Medium, each noting data-loss risk. *(`EvaluateRemovedComponents` classifies the `RemovedComponents` "Type: Name" entries; covered by tests.)*

## FEAT-ALM4-3 — Upgrade, patch, and holding-solution impact

- **US-ALM4.3.1** `[Done]` **As** an ALM engineer, **I want** upgrade vs patch impact analyzed separately, **so that** I pick the safest deployment path.
  - **AC:** Only an Upgrade (or an applied Holding upgrade) deletes components missing from the incoming solution; Update / Patch surface a single informational note instead of deletion findings. *(`PathDeletes`; verified by the Update/Patch theory test.)*
- **US-ALM4.3.2** `[Done]` **As** an ALM engineer, **I want** holding-solution risks handled, **so that** a staged upgrade does not strand components.
  - **AC:** The Holding path is treated as eventually-deleting (configurable via `ImpactOptions.TreatHoldingAsDeleting`), so it surfaces the same deletion risk as an Upgrade. *(Holding test asserts the Critical data-loss finding.)*

## FEAT-ALM4-4 — Dependency, publisher, and managed-property restrictions

- **US-ALM4.4.1** `[Done]` **As** an ALM engineer, **I want** missing dependencies and publisher conflicts detected, **so that** the change is applicable.
  - **AC:** Missing dependencies (via `RetrieveMissingDependencies`) → High; a source/target publisher-prefix mismatch → Medium. *(`ImpactCollector` runs `RetrieveMissingDependenciesRequest`; `EvaluateMissingDependencies`/`EvaluatePublisherPrefix`.)*
- **US-ALM4.4.2** `[Done]` **As** a System Customizer, **I want** managed-property restrictions and the affected components surfaced, **so that** I know what I can and cannot change post-import.
  - **AC:** Components with restrictive managed properties are listed in a Medium finding. *(`ImpactCollector` reads entity `IsCustomizable`; `EvaluateRestrictiveManagedProperties` lists affected components.)*

## FEAT-ALM4-5 — Impact score, checklist, and CAB report

- **US-ALM4.5.1** `[Done]` **As** a Delivery Manager, **I want** an overall impact score across the risk areas, **so that** I get a single go/no-go signal for change approval.
  - **AC:** The score aggregates layering / deletion / overwrite / dependency / publisher / managed-property findings into a Low/Medium/High band (any Critical forces High). *(`ScoreCalculator.RiskDefault`; a clean input yields a single Info finding, score 0, Low band.)*
- **US-ALM4.5.2** `[Done]` **As** a release manager, **I want** a pre-upgrade checklist, rollback guidance, and a CAB-ready export, **so that** the change record is complete.
  - **AC:** Checklist and rollback guidance are generated from the findings + path; the report exports to Excel / PDF / JSON / HTML off the UI thread. *(`BuildChecklist`/`BuildRollbackGuidance`; export via shared `ExcelReportExporter`/`PdfReportExporter`/`JsonReportExporter` + BCL HTML, run inside `RunAsync`.)*

## FEAT-ALM4-0 — Scaffold & shared wiring `[Done]`

- **US-ALM4.0.1** `[Done]` **As** a TOOLDEV, **I want** the tool wired to `BaseToolControl` (connection, settings, background execution, Help button), **so that** feature work starts from a working shell.
  - **AC:** Loads in XTB, connects, runs queries via `RunAsync`/`RetrieveAll`, persists last solution + path via `ImpactSettings`, clears `MetadataCache` on `UpdateConnection`; no template leftovers.

---

## Definition of Done (tool-level)

- Every Dataverse call runs off the UI thread via `RunAsync` / `RetrieveAll`; the collector degrades permission/query failures to Info findings and never throws.
- Read-only by default — the tool never imports, upgrades, patches, or deletes.
- The layer-analysis algorithm is UI-free / deterministic and unit-tested (`testing/UnitTests/ManagedSolutionImpactCheckerTests.cs`).
- Settings round-trip (load on `Load`, save in `ClosingPlugin`).
- Export formats: Excel, PDF, JSON, HTML (CAB-ready); nuspec id/version/description/tags correct.
