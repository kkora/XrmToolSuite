# Configuration Drift Monitor — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 8. Related pack file (`prompt/2...`) idea #1 'Environment Drift Detector'.
> **Suggested tag:** `ADMIN8` · **Suggested project:** `XrmToolSuite.ConfigurationDriftMonitor`
> **Overlaps:** Overlaps a candidate Migration **'Environment Comparison Suite'** and pack idea #1 'Dataverse Environment Drift Detector' — same core (compare environments/baselines, score drift, recommend remediation); **NOTE** to consolidate rather than ship two comparison engines. Consumes candidate **ADMIN7 (Environment Inventory)** normalization as the comparable snapshot; complements ADMIN1 (health) baselines.
> **Value/priority (my read):** Medium-High — DEV/TEST/UAT/PROD drift is a real, recurring ALM pain, but it duplicates a Migration-track candidate; value is high only if the comparison engine is built once and shared.

## Notes
- Data sources: normalized inventory (reuse ADMIN7) for metadata/security/solution comparison; `solutioncomponent`/`solution`/`publisher` for component/version/publisher diff; `environmentvariabledefinition`/`connectionreference`, `workflow`, `sdkmessageprocessingstep`, `webresource`, `systemform`/`savedquery`, `role` for per-class comparison.
- Baseline storage: save a normalized snapshot of an environment locally (append-only, no deletes) and diff current-vs-baseline; optionally diff two connected environments using the dual-connection pattern (`RaiseRequestConnectionEvent` with a target action, handled in `UpdateConnection` without replacing the primary connection — see Deployment Risk Analyzer).
- Comparison/diff engine is UI-free and unit-testable: classify each component as Missing / Extra / Changed / Unmanaged-change; roll into a drift score with severities.
- Read-only — reports drift and a remediation checklist; never writes to either environment.
- Comparison over full inventories is large — cache both sides, report progress per category, support cancellation.

---

## EPIC-ADMIN8 — Detect configuration drift between environments or against a baseline
> **As** an **ALM** engineer, **I want** the current environment compared to a saved baseline or another connected environment, **so that** I can see exactly what drifted — missing, extra, changed, or unmanaged components — and whether environments are aligned.

**Outcome:** a per-category drift inventory (Missing/Extra/Changed/Unmanaged), a drift score with severities, a remediation checklist, and an exportable drift report — built on a comparison engine shared with the Migration comparison candidate.

---

## FEAT-ADMIN8-1 — Source/target selection and baselines `[Planned]`
- **US-ADMIN8.1.1** `[Planned]` **As** an ALM engineer, **I want** to save the current environment as a named baseline, **so that** I can compare future state against a known-good point.
  - **AC:** Baseline captures a normalized inventory snapshot locally (append-only, no deletes); a baseline manager lists saved baselines.
- **US-ADMIN8.1.2** `[Planned]` **As** an ALM engineer, **I want** to compare against a baseline or a second connected environment, **so that** I can validate DEV→PROD alignment.
  - **AC:** Second connection uses the dual-connection pattern (target action via `RaiseRequestConnectionEvent`, handled in `UpdateConnection`) without replacing the primary connection; both loads run off the UI thread with progress/cancellation.

## FEAT-ADMIN8-2 — Compare components `[Planned]`
- **US-ADMIN8.2.1** `[Planned]` **As** an ALM engineer, **I want** tables, columns, relationships, forms, views, charts, and dashboards compared, **so that** metadata drift surfaces.
  - **AC:** Each component is classified Missing/Extra/Changed; the diff engine is UI-free and unit-testable.
- **US-ADMIN8.2.2** `[Planned]` **As** a SEC, **I want** security roles, business rules, workflows, plugins, and web resources compared, **so that** logic and access drift surfaces.
  - **AC:** Per-class comparison lists changed properties; role diff is by privilege set.
- **US-ADMIN8.2.3** `[Planned]` **As** an ALM engineer, **I want** environment variables, connection references, and solution versions/publishers compared, **so that** binding and packaging drift surfaces.
  - **AC:** Env-var/connection-ref value and definition differences, and solution version/publisher mismatches, are findings.

## FEAT-ADMIN8-3 — Classify and score drift `[Planned]`
- **US-ADMIN8.3.1** `[Planned]` **As** an ALM engineer, **I want** unmanaged changes in the target, missing components, extra components, and changed properties detected, **so that** I know the nature of each drift.
  - **AC:** Unmanaged-change detection uses `ismanaged`/layering; each category has a count in the summary.
- **US-ADMIN8.3.2** `[Planned]` **As** an MGR, **I want** an overall drift score, **so that** I get an alignment signal without reading every row.
  - **AC:** Drift score is a UI-free weighted roll-up with severities Critical/High/Medium/Low/Info.

## FEAT-ADMIN8-4 — Review, remediate, and export `[Planned]`
- **US-ADMIN8.4.1** `[Planned]` **As** an ALM engineer, **I want** a component comparison grid and a difference viewer, **so that** I can inspect each drift in detail.
  - **AC:** Grid filters by category/classification/severity; the difference viewer shows changed-property before/after.
- **US-ADMIN8.4.2** `[Planned]` **As** an ALM engineer, **I want** a remediation checklist and an exported drift report, **so that** I can realign environments as a controlled change.
  - **AC:** Checklist orders fixes by severity; exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; read-only (no writes to either environment).

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel; dual-connection via `RaiseRequestConnectionEvent`/`UpdateConnection`).
- Read-only; baselines stored locally append-only; comparison engine UI-free, unit-testable, and shared with the Migration comparison candidate rather than duplicated.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/ConfigurationDriftMonitor/ when implementation starts.
