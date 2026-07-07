# Metadata Cleanup Advisor — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 2. Not in pack file.
> **Suggested tag:** `ADMIN02` · **Suggested project:** `XrmToolSuite.MetadataCleanupAdvisor`
> **Overlaps:** **Strong** overlap with the shipped **Attribute Auditor** (attribute-usage detection is the core of "unused custom columns") — reuse its usage-scan logic, do not reimplement it. Also overlaps the shipped **Technical Debt Analyzer** (deprecated JS, unmanaged-in-prod, stale components) and candidate **ADMIN03 Duplicate Metadata Finder** (duplicate fields/views/forms) — this tool should *consume* those signals and add cleanup scoring + safe-action recommendations.
> **Value/priority (my read):** Medium-High — clear governance value, but much of the detection already exists in Attribute Auditor / Technical Debt Analyzer; the differentiator is the cleanup checklist, dependency-validated safety, and Keep/Review/Cleanup/Ignore triage.

## Notes
- Data sources: `RetrieveMetadataChanges` (custom attributes/entities, `IsCustomAttribute`, `IsManaged`, modifiedon), `systemform`/`savedquery`/`savedqueryvisualization` (inactive/statuscode), `webresource`, `workflow` (category=BusinessRule, statecode), `GlobalOptionSetMetadata`, `RelationshipMetadataBase`, `solutioncomponent`.
- Usage evidence for "unused column" reuses Attribute Auditor's approach (form/view/business-rule/plugin/flow references) — lift into shared core rather than duplicating.
- **Safety is the point:** read-only by default; never delete automatically unless an explicit opt-in cleanup mode is enabled; validate dependencies via `RetrieveDependenciesForDelete` before any component is even *recommended* for cleanup; provide rollback guidance.
- Any actual delete (only in opt-in mode) requires a confirmation dialog stating scope and exact component count.
- Cleanup findings are advisory triage states: Keep / Review / Cleanup Candidate / Ignore, persisted in settings (ignore list round-trips).
- Component/attribute names are logical names (lowercase); prefer targeted `ColumnSet`s over `ColumnSet(true)`.

---

## EPIC-ADMIN02 — Find safely-removable Dataverse metadata and recommend validated cleanup actions
> **As** an **ADM**, **I want** unused, obsolete, duplicated, poorly-named, or risky metadata identified with a dependency-validated cleanup recommendation, **so that** I can reduce environment complexity and maintenance risk without breaking anything.

**Outcome:** a categorized findings inventory with usage/dependency evidence, a cleanup score, per-finding triage (Keep/Review/Cleanup/Ignore), a safe cleanup checklist with rollback guidance, and an export.

---

## FEAT-ADMIN02-1 — Scan scope and metadata retrieval `[Planned]`
- **US-ADMIN02.1.1** `[Planned]` **As** an ADM, **I want** to scan a selected solution or the whole environment, **so that** I can scope cleanup to what I own.
  - **AC:** Scope selector loads solutions via `RetrieveAll`; scan runs off the UI thread with progress and cancellation.
- **US-ADMIN02.1.2** `[Planned]` **As** an ADM, **I want** metadata loaded once and cached per scan, **so that** re-triage and filtering stay responsive.
  - **AC:** Metadata retrieval uses `RetrieveMetadataChanges` with targeted properties; results cached for the session and cleared on `UpdateConnection`.

## FEAT-ADMIN02-2 — Detect unused components `[Planned]`
- **US-ADMIN02.2.1** `[Planned]` **As** an ADM, **I want** unused custom columns and unused custom tables detected, **so that** I can target the biggest bloat.
  - **AC:** "Unused" reuses Attribute Auditor usage evidence (no form/view/rule/plugin/flow reference); each finding lists the evidence checked.
- **US-ADMIN02.2.2** `[Planned]` **As** an ADM, **I want** inactive forms, views, charts, dashboards, unused web resources, and inactive business rules detected, **so that** stale UI/config assets surface.
  - **AC:** Inactive detection uses statecode/statuscode and last-modified; unreferenced web resources are flagged with a confidence note.
- **US-ADMIN02.2.3** `[Planned]` **As** a TOOLDEV, **I want** deprecated JavaScript libraries and unused relationships/option sets detected, **so that** technical debt is included in cleanup.
  - **AC:** Deprecated-JS reuses the shared analyzer; unused relationships and obsolete choices are separate finding categories.

## FEAT-ADMIN02-3 — Detect quality and hygiene issues `[Planned]`
- **US-ADMIN02.3.1** `[Planned]` **As** an ARCH, **I want** duplicate/similar fields, duplicate views/forms, unmanaged metadata in production, and old solution layers detected, **so that** structural debt is captured.
  - **AC:** Duplicate detection consumes ADMIN03 logic where available; unmanaged-in-prod uses `ismanaged`; layering uses solution-component layers.
- **US-ADMIN02.3.2** `[Planned]` **As** an ARCH, **I want** components not modified in years and components without descriptions flagged, **so that** stale and undocumented metadata is visible.
  - **AC:** Staleness threshold is configurable and round-trips; missing-description is a Low/Info finding.

## FEAT-ADMIN02-4 — Dependency validation and safety `[Planned]`
- **US-ADMIN02.4.1** `[Planned]` **As** an ADM, **I want** each cleanup candidate validated against its dependencies before it is recommended, **so that** I never remove something still in use.
  - **AC:** `RetrieveDependenciesForDelete` runs per candidate; any dependency downgrades the recommendation and shows an impact panel.
- **US-ADMIN02.4.2** `[Planned]` **As** an ADM, **I want** the tool to be read-only unless I explicitly enable cleanup mode, **so that** I cannot cause accidental damage.
  - **AC:** No write API is called in default mode; opt-in cleanup mode requires a confirmation dialog stating component type and exact count, plus rollback guidance.

## FEAT-ADMIN02-5 — Cleanup scoring, triage, and export `[Planned]`
- **US-ADMIN02.5.1** `[Planned]` **As** an ADM, **I want** a cleanup score and per-finding triage states, **so that** I can prioritize and track decisions.
  - **AC:** Cleanup score is a UI-free unit-testable roll-up; each finding is markable Keep/Review/Cleanup Candidate/Ignore and the ignore list round-trips via settings.
- **US-ADMIN02.5.2** `[Planned]` **As** an MGR, **I want** a safe cleanup checklist and report exported, **so that** cleanup can be reviewed and executed as a change.
  - **AC:** Exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; the checklist includes dependency status and rollback notes.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default; any delete gated behind opt-in mode + confirmation dialog with scope/count; dependencies validated before recommendation; analyzers UI-free.
- Reuses Attribute Auditor / Technical Debt / ADMIN03 detection instead of duplicating it.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/MetadataCleanupAdvisor/ when implementation starts.
