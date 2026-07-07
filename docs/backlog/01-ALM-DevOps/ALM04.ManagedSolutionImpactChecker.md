# Managed Solution Impact Checker — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 1 (ALM & DevOps), item 4. Not in pack file.
> **Suggested tag:** `ALM04` · **Suggested project:** `XrmToolSuite.ManagedSolutionImpactChecker`
> **Overlaps:** Deployment Risk Analyzer touches managed-upgrade deletion risk (removed tables/columns → data loss) and schema conflicts; this tool goes deeper on *solution layering* (active layer ownership, unmanaged layers above managed, upgrade/patch/delete/holding-solution impact). Note the overlap on deletion/schema and reuse those DRA analyzers.
> **Value/priority (my read):** High — layering surprises are the classic managed-solution production incident and are poorly served by native tooling.

## Notes
- Core APIs: `msdyn_componentlayer` / `RetrieveSolutionComponentsWithLayers`, `GetSolutionLayerRequest`, `solutioncomponent`, `RetrieveMissingDependencies`, `managedproperties` on components.
- Distinguishes Upgrade vs Update vs Patch vs Holding solution — the delete/overwrite semantics differ per path and must be modeled explicitly.
- Read-only analysis; produces an impact score, pre-upgrade checklist, rollback guidance, and a CAB-ready report — never performs the import/upgrade.
- Reuse shared-core `RetrieveAll`; lift Deployment Risk Analyzer's removed-component/data-loss and schema-conflict analyzers; keep the layer-analysis algorithm UI-free.
- Layer queries can be heavy — page and report progress, support cancellation, and degrade to Info on permission gaps.
- Overlaps candidate ALM02 (Dependency Validator) for the missing-dependency class — cross-reference, don't reimplement.

---

## EPIC-ALM04 — Analyze the impact of importing/upgrading/patching/deleting a managed solution
> **As** an **ALM** engineer, **I want** to understand how a managed-solution change interacts with existing layers before I apply it, **so that** I do not overwrite customizations, delete components with data, or hit publisher/managed-property restrictions in production.

**Outcome:** a layer-analysis dashboard, an impact score, per-path (upgrade/delete/patch) risk breakdown, a pre-upgrade checklist, and rollback guidance in a CAB-ready report.

---

## FEAT-ALM04-1 — Select managed solution and analyze layers `[Planned]`
- **US-ALM04.1.1** `[Planned]` **As** an ALM engineer, **I want** to pick a managed solution and see its current solution layers, **so that** I know the layering context before changing it.
  - **AC:** Layers load off the UI thread via `RetrieveAll` with progress; active layer ownership per component is shown.
- **US-ALM04.1.2** `[Planned]` **As** an ALM engineer, **I want** unmanaged customizations sitting above managed layers detected, **so that** I know what an upgrade might reassert or an admin has overridden.
  - **AC:** Components with an active unmanaged layer over the managed one are flagged with the owning layer.

## FEAT-ALM04-2 — Overwrite and deletion impact `[Planned]`
- **US-ALM04.2.1** `[Planned]` **As** an ALM engineer, **I want** components that may be overwritten by the change flagged, **so that** I protect customer-specific customizations.
  - **AC:** Components whose managed definition would win over an existing customization are listed with severity.
- **US-ALM04.2.2** `[Planned]` **As** an ALM engineer, **I want** delete impact analyzed, **so that** I never remove a table/column (and its data) unknowingly.
  - **AC:** Removed tables → Critical, columns → High, other components → Medium, each noting data-loss risk (reusing DRA's deletion analysis).

## FEAT-ALM04-3 — Upgrade, patch, and holding-solution impact `[Planned]`
- **US-ALM04.3.1** `[Planned]` **As** an ALM engineer, **I want** upgrade vs patch impact analyzed separately, **so that** I pick the safest deployment path.
  - **AC:** Each path lists components added/changed/removed and calls out where Upgrade deletes but Update/Patch does not.
- **US-ALM04.3.2** `[Planned]` **As** an ALM engineer, **I want** holding-solution risks detected, **so that** a staged upgrade does not strand components in a holding solution.
  - **AC:** Holding-solution scenarios are flagged with the components at risk.

## FEAT-ALM04-4 — Dependency, publisher, and managed-property restrictions `[Planned]`
- **US-ALM04.4.1** `[Planned]` **As** an ALM engineer, **I want** missing dependencies, component-ownership conflicts, and publisher conflicts detected, **so that** the change is applicable.
  - **AC:** Missing dependencies (via `RetrieveMissingDependencies`) and publisher/ownership conflicts are findings with severity.
- **US-ALM04.4.2** `[Planned]` **As** a System Customizer, **I want** managed-property restrictions and affected forms/views/scripts/plugins surfaced, **so that** I know what I can and cannot change post-import.
  - **AC:** Components with restrictive managed properties and the affected UI/logic components are listed.

## FEAT-ALM04-5 — Impact score, checklist, and CAB report `[Planned]`
- **US-ALM04.5.1** `[Planned]` **As** a Delivery Manager, **I want** an overall impact score across the risk areas, **so that** I get a single go/no-go signal for change approval.
  - **AC:** Score aggregates layering/dependency/deletion/upgrade/patch/publisher/overwrite/managed-property risks into a banded result.
- **US-ALM04.5.2** `[Planned]` **As** a release manager, **I want** a pre-upgrade checklist, rollback guidance, and a CAB-ready export, **so that** the change record is complete.
  - **AC:** Checklist and rollback guidance are generated from findings; report exports to Excel/PDF/JSON/HTML off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default (never imports/upgrades); layer-analysis algorithm is UI-free and degrades query failures to Info; DRA deletion/schema analyzers reused.
- Export formats: Excel, PDF, JSON, HTML (CAB-ready).
- Testing skeleton under testing/Tools/ManagedSolutionImpactChecker/ when implementation starts.
