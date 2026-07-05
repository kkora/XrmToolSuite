# Relationship Validator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 6. Related pack file (`prompt/2...`) idea #17 'Relationship Impact Explorer'.
> **Suggested tag:** `ADMIN06` · **Suggested project:** `XrmToolSuite.RelationshipValidator`
> **Overlaps:** Shares the relationship-map/cascade-impact idea with pack #17 'Relationship Impact Explorer' (visual dependency chains + delete impacts); this tool leads with *validation/risk scoring*, the explorer leads with *interactive visualization* — the graph rendering is common and could be shared. Cascade/dependency signals also feed candidate ADMIN02 (unused relationships).
> **Value/priority (my read):** Medium-High — risky cascade-delete config causes real data-integrity incidents and there is no first-class Microsoft validator; the graph is nice-to-have, the cascade risk rules are the core value.

## Notes
- Data sources: `RetrieveMetadataChanges` / `RetrieveRelationship` for `OneToManyRelationshipMetadata`, `ManyToManyRelationshipMetadata`, and lookup attributes; `CascadeConfiguration` (Delete/Assign/Share/Unshare/Reparent/Merge/RollupView) for behavior analysis; `RetrieveTotalRecordCount` and targeted queries for orphan-lookup sampling.
- Cascade risk rules are UI-free and unit-testable: flag `CascadeType.Cascade` on delete for high-volume/parent tables, restricted/parental combinations, and referential mismatches → severity Critical/High/Medium/Low/Info.
- Orphan-lookup detection is best-effort sampling (lookups pointing to inactive/obsolete or non-existent targets) — disclaim it is a sample, not exhaustive, to respect service-protection limits.
- Read-only — analyzes and scores; recommends changes but never alters relationships or cascade config.
- Graph/map rendering reuses any shared visualization helper (see Solution Knowledge Graph if liftable); keep the risk analyzers UI-free so they stay console-liftable.
- Report progress per table/relationship and support cancellation.

---

## EPIC-ADMIN06 — Validate Dataverse relationships and cascade behavior to prevent data-integrity incidents
> **As** an **ADM**, **I want** relationships, lookups, cascade rules, and referential behavior analyzed for risk, **so that** I catch dangerous cascade-delete config, orphaned lookups, and broken dependencies before they cause unexpected deletes or import failures.

**Outcome:** a relationship inventory with cascade behavior, risk findings by severity, a relationship risk score, a relationship map, remediation recommendations, and an exportable validation report.

---

## FEAT-ADMIN06-1 — Inventory relationships and lookups `[Planned]`
- **US-ADMIN06.1.1** `[Planned]` **As** an ADM, **I want** all 1:N, N:1, and N:N relationships listed with referenced tables, **so that** I can see the full relationship surface.
  - **AC:** Relationship metadata loads via `RetrieveMetadataChanges` off the UI thread with progress/cancellation; a table selector scopes the view.
- **US-ADMIN06.1.2** `[Planned]` **As** an ADM, **I want** lookup columns and their target tables shown, **so that** I understand every reference.
  - **AC:** Each lookup lists its owning table, target(s), and required/optional state.

## FEAT-ADMIN06-2 — Analyze cascade and referential behavior `[Planned]`
- **US-ADMIN06.2.1** `[Planned]` **As** an ARCH, **I want** cascade delete/assign/share/unshare/reparent/merge/rollup behavior shown per relationship, **so that** I understand the runtime consequences.
  - **AC:** A cascade-behavior panel renders each `CascadeConfiguration` value per relationship.
- **US-ADMIN06.2.2** `[Planned]` **As** an ARCH, **I want** risky cascade-delete configurations flagged, **so that** I catch relationships that can trigger unexpected mass deletes.
  - **AC:** Cascade-delete on high-volume/parent tables and restricted/parental mismatches raise High/Critical findings; the rules are UI-free and unit-testable.

## FEAT-ADMIN06-3 — Detect relationship risks and orphans `[Planned]`
- **US-ADMIN06.3.1** `[Planned]` **As** an ADM, **I want** relationships referencing inactive/obsolete tables, duplicate relationship patterns, and relationships without clear naming/descriptions detected, **so that** structural debt surfaces.
  - **AC:** Each is a distinct finding category with severity; naming/description gaps are Low/Info.
- **US-ADMIN06.3.2** `[Planned]` **As** an ADM, **I want** orphan lookup values and required lookups with data-quality issues detected where possible, **so that** integrity gaps are visible.
  - **AC:** Orphan detection samples records (respecting service-protection limits) and disclaims it is a sample, not exhaustive.

## FEAT-ADMIN06-4 — Risk score and relationship map `[Planned]`
- **US-ADMIN06.4.1** `[Planned]` **As** an MGR, **I want** an overall relationship risk score, **so that** I can gauge integrity risk at a glance.
  - **AC:** Score is a UI-free weighted roll-up of findings; complexity/depth contributes.
- **US-ADMIN06.4.2** `[Planned]` **As** an ARCH, **I want** a relationship map/graph of tables and cascade chains, **so that** I can see delete-impact paths visually.
  - **AC:** The map renders tables and relationships with cascade-delete edges highlighted; rendering reuses shared visualization where liftable.

## FEAT-ADMIN06-5 — Recommendations and export `[Planned]`
- **US-ADMIN06.5.1** `[Planned]` **As** an ADM, **I want** remediation recommendations and an exported validation report, **so that** I can plan safe fixes.
  - **AC:** Recommendations reference their finding and severity; exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; read-only (no relationship changes made).

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only; cascade/orphan analyzers UI-free and unit-testable; orphan detection disclaimed as sampled; degrades query failures to Info.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/RelationshipValidator/ when implementation starts.
