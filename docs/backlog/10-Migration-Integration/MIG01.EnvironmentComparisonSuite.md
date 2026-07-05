# Environment Comparison Suite — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 10 (Migration & Integration), item 1. Related pack idea #1 'Environment Drift Detector'.
> **Suggested tag:** `MIG01` · **Suggested project:** `XrmToolSuite.EnvironmentComparisonSuite`
> **Overlaps:** **STRONG** overlap with the Admin candidate **'Configuration Drift Monitor' (ADMIN08)** and pack idea #1 'Environment Drift Detector' — both compare two environments/baselines, classify Missing/Extra/Changed/Unmanaged, and score divergence. **NOTE:** build the comparison/diff engine **ONCE** as UI-free shared Core and consume it from both tools; do not ship two comparison engines. Also touches Solution Complexity Score and Attribute Auditor (per-component normalization).
> **Value/priority (my read):** High — release validation and support triage constantly need a full "what differs between DEV/TEST/UAT/PROD" view; value is highest only if the shared engine is reused rather than duplicated with ADMIN08.

## Notes
- Data sources: `solution`/`publisher`/`solutioncomponent` (versions, publishers, layering); Entity/Attribute/Relationship metadata; `systemform`/`savedquery`/`savedqueryvisualization`; `role`/`systemuser`/`team`/`businessunit`; `pluginassembly`/`sdkmessageprocessingstep`/`sdkmessageprocessingstepimage`; `workflow` (workflows, business rules, flows via `clientdata`); `customapi`; `environmentvariabledefinition`/`environmentvariablevalue`; `connectionreference`; `webresource`; Power Pages metadata where available.
- Cross-environment is the core: source is the primary connection; the target uses the suite dual-connection pattern (`RaiseRequestConnectionEvent` with `actionName="TargetOrganization"`, handled in `UpdateConnection` without replacing the primary — reuse Deployment Risk Analyzer's implementation).
- Shared comparison-engine reuse: normalize each component class to a comparable snapshot, classify Missing/Extra/Changed/Managed-vs-Unmanaged, roll into a difference score; keep it UI-free and unit-testable so ADMIN08 (Configuration Drift Monitor) reuses the same engine.
- Retrieval is large: use `Service.RetrieveAll` for every list, cache both sides, run off the UI thread via `RunAsync`/`WorkAsync`, report progress per category, and support cancellation.
- Read-only — the suite reports differences and recommendations only; it never writes to either environment.

---

## EPIC-MIG01 — Compare two Dataverse environments across every component class
> **As** an **ALM** engineer, **I want** a full component-by-component comparison of a source and target environment, **so that** I can validate a release, explain support incidents, and see exactly what drifted.

**Outcome:** a per-category difference inventory (Missing/Extra/Changed/Managed-vs-Unmanaged), a difference score with severities, a recommendation panel, and an exportable comparison report — built on a comparison engine shared with the ADMIN08 drift candidate.

---

## FEAT-MIG01-1 — Connect source and target environments `[Planned]`
- **US-MIG01.1.1** `[Planned]` **As** an ALM engineer, **I want** to pick a source and a target environment, **so that** I can compare release stages (DEV→UAT→PROD).
  - **AC:** Target uses the dual-connection pattern (`RaiseRequestConnectionEvent` `actionName="TargetOrganization"`, handled in `UpdateConnection`) without replacing the primary; both loads run off the UI thread with progress/cancellation.
- **US-MIG01.1.2** `[Planned]` **As** an ALM engineer, **I want** a component category selector, **so that** I can scope a comparison to just the classes I care about.
  - **AC:** Categories can be toggled before a run; unselected classes are skipped and their retrieval is not issued.

## FEAT-MIG01-2 — Compare solutions, publishers, and metadata `[Planned]`
- **US-MIG01.2.1** `[Planned]` **As** an ALM engineer, **I want** solution versions and publishers compared, **so that** packaging and layering drift surfaces.
  - **AC:** Version mismatches, publisher/prefix differences, and managed/unmanaged layering differences are findings; comparison uses `Service.RetrieveAll`.
- **US-MIG01.2.2** `[Planned]` **As** a **CUST**, **I want** tables, columns, relationships, and alternate keys compared, **so that** schema drift surfaces.
  - **AC:** Each component is classified Missing/Extra/Changed; changed-property lists (datatype, required level, cascade) are shown; the diff engine is UI-free and unit-testable.
- **US-MIG01.2.3** `[Planned]` **As** a CUST, **I want** forms, views, charts, and dashboards compared, **so that** UI configuration drift surfaces.
  - **AC:** Presence and definition differences are classified per component; large FormXML/LayoutXML diffs are summarized to changed regions rather than raw XML.

## FEAT-MIG01-3 — Compare security, automation, and code `[Planned]`
- **US-MIG01.3.1** `[Planned]` **As** a **SEC**, **I want** security roles, teams, business units, and users compared, **so that** access drift surfaces.
  - **AC:** Role differences are compared by privilege set (not just name); missing/extra teams and business units are listed.
- **US-MIG01.3.2** `[Planned]` **As** an ALM engineer, **I want** plugin assemblies, steps, images, workflows, business rules, flows, and custom APIs compared, **so that** logic drift surfaces.
  - **AC:** Steps/images are matched by message+entity+stage; missing/extra/changed registrations are findings; flow comparison degrades gracefully where `clientdata` is unavailable.
- **US-MIG01.3.3** `[Planned]` **As** an ALM engineer, **I want** environment variables, connection references, and web resources/JavaScript compared, **so that** binding and code drift surfaces.
  - **AC:** Env-var definitions/values, connection-reference bindings, and web-resource content hashes are compared; value differences are flagged; secret-typed values are masked in output.

## FEAT-MIG01-4 — Detect, classify, and score differences `[Planned]`
- **US-MIG01.4.1** `[Planned]` **As** an ALM engineer, **I want** each component classified as Missing, Extra, Changed, or Managed-vs-Unmanaged, **so that** I understand the nature of every difference.
  - **AC:** Classification uses `ismanaged`/solution layering; every category shows a count in the summary; the classifier is shared UI-free Core (reused by ADMIN08).
- **US-MIG01.4.2** `[Planned]` **As** an **MGR**, **I want** an overall difference score with severities, **so that** I get a go/no-go signal without reading every row.
  - **AC:** Score is a UI-free weighted roll-up with severities Critical/High/Medium/Low/Info.

## FEAT-MIG01-5 — Review, recommend, and export `[Planned]`
- **US-MIG01.5.1** `[Planned]` **As** an ALM engineer, **I want** a difference grid and a side-by-side detail viewer, **so that** I can inspect each difference.
  - **AC:** Grid filters by category/classification/severity; the side-by-side viewer shows changed-property before/after for source vs target.
- **US-MIG01.5.2** `[Planned]` **As** an ALM engineer, **I want** recommendations and an exported comparison report, **so that** I can plan a controlled remediation.
  - **AC:** Recommendation panel orders fixes by severity; exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; masked values stay masked in exports; read-only (no writes to either environment).

## Definition of Done
- Follows suite conventions; read-only default; row sampling limited + sensitive/secret values masked; cross-env via `TargetOrganization`; export formats Excel, PDF, JSON, HTML.
- Comparison/diff engine is UI-free, unit-testable, and **shared with the ADMIN08 Configuration Drift Monitor candidate** rather than duplicated.
- Testing skeleton under testing/EnvironmentComparisonSuite/ when implementation starts.
