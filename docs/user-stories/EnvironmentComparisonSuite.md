# Environment Comparison Suite — User Stories

> **Status:** Implemented (v1). Read-only source↔target environment comparison across every component
> class, with a UI-free/SDK-free diff engine, a difference score, and Excel/PDF/JSON/HTML export.
> **Tag:** `MIG1` · **Project:** `XrmToolSuite.EnvironmentComparisonSuite`
> **Source backlog:** [`docs/backlog/10-Migration-Integration/MIG1.EnvironmentComparisonSuite.md`](../backlog/10-Migration-Integration/MIG1.EnvironmentComparisonSuite.md).
> **Testing:** [`testing/EnvironmentComparisonSuite/`](../../testing/EnvironmentComparisonSuite/) — automated SDK-free tests in `testing/UnitTests/EnvironmentComparisonSuiteTests.cs`; manual GUI/live cases in `TEST_CASES.md`.

## Notes
- **Source is the primary connection; the target uses the suite dual-connection pattern** (`RaiseRequestConnectionEvent` with `actionName="TargetOrganization"`, handled in `UpdateConnection` **without** replacing the primary) — copied from the Deployment Risk Analyzer.
- The diff engine (`Analysis/ComparisonModels.cs` + `Analysis/SnapshotComparer.cs`) is **UI-free and SDK-free**: it normalizes every component class to a `ComponentSnapshot`, classifies Missing/Extra/Changed/ManagedVsUnmanaged/Identical, assigns severity per category+class, and rolls a weighted difference score/band. It is unit-tested with hand-built snapshot lists and is designed to be reused by the ADMIN8 Configuration Drift Monitor candidate rather than duplicated.
- The Dataverse collector (`Analysis/ComparisonCollector.cs`) is the only SDK-touching piece; each category is fail-soft (a query/permission gap degrades to an informational finding), uses `RetrieveAll` (paging + cancellation), runs off the UI thread via `RunAsync`, and reports progress per category.
- **Read-only.** The tool never writes to either environment. **Secret-typed environment-variable values are masked** in the grid, the detail viewer, and every export.
- Large FormXML/LayoutXML/web-resource content is compared via a stable content hash (not raw XML), so a definition change is detected without storing or exporting the payload.

---

## EPIC-MIG1 — Compare two Dataverse environments across every component class
> **As** an **ALM** engineer, **I want** a full component-by-component comparison of a source and target environment, **so that** I can validate a release, explain support incidents, and see exactly what drifted.

**Outcome:** a per-category difference inventory (Missing/Extra/Changed/Managed-vs-Unmanaged), a difference score with severities, a recommendation panel, and an exportable comparison report — built on a comparison engine shared with the ADMIN8 drift candidate.

---

## FEAT-MIG1-1 — Connect source and target environments `[Done]`
- **US-MIG1.1.1** `[Done]` **As** an ALM engineer, **I want** to pick a source and a target environment, **so that** I can compare release stages (DEV→UAT→PROD).
  - **AC:** Target uses the dual-connection pattern (`actionName="TargetOrganization"`, handled in `UpdateConnection`) without replacing the primary; both loads run off the UI thread with progress/cancellation. **Met** — `AddAdditionalOrganization`/`UpdateConnection` in `EnvironmentComparisonSuiteControl`.
- **US-MIG1.1.2** `[Done]` **As** an ALM engineer, **I want** a component category selector, **so that** I can scope a comparison to just the classes I care about.
  - **AC:** Categories can be toggled before a run; unselected classes are skipped and their retrieval is not issued. **Met** — the checked-list drives the `enabledCategories` set; the collector never retrieves an unchecked category (and shares one metadata call only when a metadata category is checked).

## FEAT-MIG1-2 — Compare solutions, publishers, and metadata `[Done]`
- **US-MIG1.2.1** `[Done]` **As** an ALM engineer, **I want** solution versions and publishers compared, **so that** packaging and layering drift surfaces.
  - **AC:** Version mismatches, publisher/prefix differences, and managed/unmanaged layering differences are findings; comparison uses `RetrieveAll`. **Met.**
- **US-MIG1.2.2** `[Done]` **As** a **CUST**, **I want** tables, columns, relationships, and alternate keys compared, **so that** schema drift surfaces.
  - **AC:** Each component is classified Missing/Extra/Changed; changed-property lists (datatype, required level, cascade) are shown; the diff engine is UI-free and unit-testable. **Met** — one `RetrieveAllEntitiesRequest` per environment feeds table/column/relationship/key snapshots.
- **US-MIG1.2.3** `[Done]` **As** a CUST, **I want** forms, views, charts, and dashboards compared, **so that** UI configuration drift surfaces.
  - **AC:** Presence and definition differences are classified per component; large FormXML/LayoutXML diffs are summarized (a content hash), not raw XML. **Met.**

## FEAT-MIG1-3 — Compare security, automation, and code `[Done]`
- **US-MIG1.3.1** `[Done]` **As** a **SEC**, **I want** security roles, teams, business units, and users compared, **so that** access drift surfaces.
  - **AC:** Role differences are compared by privilege set (a stable hash of the sorted privilege+depth pairs), not just name; missing/extra teams and business units are listed. **Met.**
- **US-MIG1.3.2** `[Done]` **As** an ALM engineer, **I want** plugin assemblies, steps, images, workflows, business rules, flows, and custom APIs compared, **so that** logic drift surfaces.
  - **AC:** Steps/images are matched by message+entity+stage (step name + stage / image name + alias); missing/extra/changed registrations are findings; flow comparison degrades gracefully where `clientdata` is unavailable (presence + category/state). **Met.**
- **US-MIG1.3.3** `[Done]` **As** an ALM engineer, **I want** environment variables, connection references, and web resources/JavaScript compared, **so that** binding and code drift surfaces.
  - **AC:** Env-var definitions/values, connection-reference bindings, and web-resource content hashes are compared; value differences are flagged; **secret-typed values are masked** in output. **Met** — the collector marks the secret type; the comparer still detects a change but emits the masked placeholder.

## FEAT-MIG1-4 — Detect, classify, and score differences `[Done]`
- **US-MIG1.4.1** `[Done]` **As** an ALM engineer, **I want** each component classified as Missing, Extra, Changed, or Managed-vs-Unmanaged, **so that** I understand the nature of every difference.
  - **AC:** Classification uses `ismanaged`/layering; every category shows a count in the summary; the classifier is shared UI-free Core (reusable by ADMIN8). **Met** — `SnapshotComparer.Compare` + the `CountsByCategoryAndClass` matrix.
- **US-MIG1.4.2** `[Done]` **As** an **MGR**, **I want** an overall difference score with severities, **so that** I get a go/no-go signal without reading every row.
  - **AC:** Score is a UI-free weighted roll-up with severities Critical/High/Medium/Low/Info. **Met** — `SnapshotComparer.Roll` reuses `ScoreCalculator.RiskDefault`.

## FEAT-MIG1-5 — Review, recommend, and export `[Done]`
- **US-MIG1.5.1** `[Done]` **As** an ALM engineer, **I want** a difference grid and a side-by-side detail viewer, **so that** I can inspect each difference.
  - **AC:** Grid filters by category/classification/severity; the side-by-side viewer shows changed-property before/after for source vs target. **Met** — three filter combos + a Property/Source/Target detail grid.
- **US-MIG1.5.2** `[Done]` **As** an ALM engineer, **I want** recommendations and an exported comparison report, **so that** I can plan a controlled remediation.
  - **AC:** Recommendation panel orders fixes by severity; exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; masked values stay masked in exports; read-only (no writes to either environment). **Met** — `ComparisonReportModel` → the suite-shared exporters, run inside `RunAsync`.

## Definition of Done
- Follows suite conventions; read-only default; secret values masked; cross-env via `TargetOrganization`; export formats Excel, PDF, JSON, HTML.
- Comparison/diff engine is UI-free, unit-testable, and **shareable with the ADMIN8 Configuration Drift Monitor candidate** rather than duplicated.
- Testing skeleton under `testing/EnvironmentComparisonSuite/` with automated tests in `testing/UnitTests/EnvironmentComparisonSuiteTests.cs`.
