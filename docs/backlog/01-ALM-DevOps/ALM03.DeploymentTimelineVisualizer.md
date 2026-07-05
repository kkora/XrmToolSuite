# Deployment Timeline Visualizer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 1 (ALM & DevOps), item 3. Not in pack file.
> **Suggested tag:** `ALM03` · **Suggested project:** `XrmToolSuite.DeploymentTimelineVisualizer`
> **Overlaps:** None material — read-only historical/analytics view of deployments; complements (does not duplicate) Deployment Risk Analyzer's pre-deploy focus.
> **Value/priority (my read):** Medium — strong stakeholder/manager reporting value, but delivery depends on how much import history a given environment actually retains.

## Notes
- Core data: `msdyn_solutionhistory` (import history, start/end, result, operation type) where available, `solution` (version, ismanaged, publisher, modifiedon), `importjob` for older orgs, `publisher`.
- Feasibility caveat: `msdyn_solutionhistory` retention and completeness vary by environment/age; degrade gracefully and label gaps rather than inventing data.
- Read-only reporting tool; no writes. Emphasis is visualization + release-notes generation.
- Reuse shared-core `RetrieveAll` for history paging; keep the timeline/model builder UI-free so it can be unit-tested and lifted into a report generator.
- Timeline/charts should be self-contained (drawn in WinForms or emitted into the HTML export) — no external chart libraries beyond what the suite already ships.
- Cross-environment comparison implies an optional second connection (mirror Deployment Risk Analyzer's `TargetOrganization` dual-connection pattern) — optional, not required for single-env timelines.

---

## EPIC-ALM03 — Visualize solution deployment history across environments over time
> **As** an **ADM**, **I want** to see what was deployed, when, by whom, and what changed across DEV/TEST/UAT/PROD, **so that** I understand release activity and can produce release notes and audit answers without guesswork.

**Outcome:** an interactive deployment timeline, version-history and release-frequency charts, per-environment comparison, and generated release notes, exportable to PDF/PNG/Excel/JSON/HTML.

---

## FEAT-ALM03-1 — Read deployment and version history `[Planned]`
- **US-ALM03.1.1** `[Planned]` **As** an ADM, **I want** solution import history and version history loaded, **so that** the timeline reflects real deployment events.
  - **AC:** History loads off the UI thread via `RetrieveAll` with progress; missing/limited history is labeled, not faked.
- **US-ALM03.1.2** `[Planned]` **As** an ADM, **I want** publisher, solution metadata, and component modified-dates read, **so that** I can attribute and date changes.
  - **AC:** Each event records solution, version, publisher, operation type, and result where available.

## FEAT-ALM03-2 — Render the deployment timeline `[Planned]`
- **US-ALM03.2.1** `[Planned]` **As** an ADM, **I want** a timeline canvas of deployment events, **so that** I can see the deployment cadence at a glance.
  - **AC:** Events include created/exported/imported/upgraded/patched and component added/removed/modified/publisher-changed/version-changed.
- **US-ALM03.2.2** `[Planned]` **As** a Delivery Manager, **I want** major releases, hotfixes/patches, and risky patterns highlighted, **so that** anomalies stand out.
  - **AC:** Patch/upgrade vs major release are visually distinct; a configurable "risky pattern" (e.g. rapid successive prod imports) is highlighted.

## FEAT-ALM03-3 — Component change timeline `[Planned]`
- **US-ALM03.3.1** `[Planned]` **As** a System Customizer, **I want** a component-change grid tied to the timeline, **so that** I can see exactly what components moved in a given release.
  - **AC:** Grid lists component, change type (added/removed/modified), date, and owning solution/version.

## FEAT-ALM03-4 — Cross-environment comparison `[Planned]`
- **US-ALM03.4.1** `[Planned]` **As** an Enterprise Architect, **I want** an environment-by-environment comparison, **so that** I can see how DEV/TEST/UAT/PROD diverge.
  - **AC:** With an optional second connection (`TargetOrganization` pattern), solution versions and last-deploy dates are compared side by side; managed/unmanaged movement is shown.
- **US-ALM03.4.2** `[Planned]` **As** a Delivery Manager, **I want** release-frequency and version-history charts, **so that** I can report on delivery velocity.
  - **AC:** Frequency and version charts render from the loaded history and are included in the HTML/PDF export.

## FEAT-ALM03-5 — Release notes and export `[Planned]`
- **US-ALM03.5.1** `[Planned]` **As** a Delivery Manager, **I want** release notes generated from the timeline, **so that** I can publish what changed without hand-writing it.
  - **AC:** Notes group component changes by release/version with dates and are editable before export.
- **US-ALM03.5.2** `[Planned]` **As** an ADM, **I want** the timeline exported to PDF, PNG, Excel, JSON, and HTML, **so that** I can share it with stakeholders.
  - **AC:** HTML is self-contained and theme-aware; PNG captures the timeline canvas; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only; timeline/model builder is UI-free and unit-tested; history gaps are labeled, never fabricated.
- Export formats: PDF, PNG, Excel, JSON, HTML.
- Testing skeleton under testing/DeploymentTimelineVisualizer/ when implementation starts.
