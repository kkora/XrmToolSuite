# Migration Readiness Checker — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 10 (Migration & Integration), item 2. Related pack idea #20 'Release Readiness Score'.
> **Suggested tag:** `MIG02` · **Suggested project:** `XrmToolSuite.MigrationReadinessChecker`
> **Overlaps:** Overlaps the **shipped Deployment Risk Analyzer** (dependency/layering/unmanaged/hardcoded-value analysis and release scoring) and pack idea #20 'Release Readiness Score'. **NOTE:** this tool is the migration-specific readiness lens (tenant move, consolidation, managed conversion, cutover) — reuse DRA's analyzers (`Analyzers/*.cs`, all `IOrganizationService`-only and UI-free) and scoring rather than re-implementing; add migration-only categories (data volume, cutover checklist, migration blockers). Also touches Technical Debt Analyzer and Solution Complexity Score.
> **Value/priority (my read):** Medium — genuinely useful pre-migration gate, but a large share of the analysis already exists in Deployment Risk Analyzer; value depends on reusing DRA rather than forking it, and on the migration-only additions (blockers, data-volume, cutover checklist).

## Notes
- Data sources: `solution`/`solutioncomponent`/`dependency` (dependencies, layering, unmanaged customizations); `workflow` (workflows, business rules, flows via `clientdata`); `pluginassembly`/`sdkmessageprocessingstep`/`customapi`; `webresource` (JavaScript scanning); `environmentvariabledefinition`/`connectionreference`; `role`/`systemuser`/`team`; Power Pages metadata where available; `RetrieveTotalRecordCount`/aggregate queries for data-volume indicators.
- Reuse first: lift Deployment Risk Analyzer's UI-free analyzers and scoring engine; each new readiness rule implements the same `IAnalyzer`-style contract and degrades query failures to informational findings instead of throwing.
- Scope selector supports whole-environment or a single solution; the target of a move is the primary connection (a second environment is optional and, if used, follows the dual-connection `TargetOrganization` pattern from Deployment Risk Analyzer).
- Data-touching checks (volume, duplicate-metadata, data readiness) limit row sampling by default and mask sensitive values in any sample shown or exported.
- Read-only; all retrieval via `Service.RetrieveAll`, off the UI thread with progress + cancellation; settings (scope, thresholds, category weights) round-trip via Load/SaveSettings.

---

## EPIC-MIG02 — Score whether an environment or solution is ready to migrate
> **As** an **ALM** specialist, **I want** a readiness assessment across ALM, dependency, security, data, integration, automation, Power Pages, performance, and documentation categories, **so that** I know the blockers and risks before a migration, tenant move, or cutover starts.

**Outcome:** a migration readiness score by category, a prioritized migration blockers grid, a risk register, a data-volume panel, and a cutover checklist — exportable — built by reusing the shipped Deployment Risk Analyzer engine.

---

## FEAT-MIG02-1 — Scope selection `[Planned]`
- **US-MIG02.1.1** `[Planned]` **As** an ALM specialist, **I want** to scope the assessment to an environment or a specific solution, **so that** I can check readiness at the granularity of the planned move.
  - **AC:** Scope selector offers environment-wide or single-solution; the chosen scope constrains every analyzer's queries; runs off the UI thread with progress/cancellation.

## FEAT-MIG02-2 — ALM, dependency, and automation readiness `[Planned]`
- **US-MIG02.2.1** `[Planned]` **As** an ALM specialist, **I want** solution dependencies, layering, and unmanaged customizations analyzed, **so that** I know what will block a managed conversion or import.
  - **AC:** Missing dependencies, unmanaged layers, and unresolved references become findings; reuses DRA dependency/layering analyzers rather than new code.
- **US-MIG02.2.2** `[Planned]` **As** an ALM specialist, **I want** plugins, custom APIs, workflows, business rules, and flows analyzed, **so that** obsolete or fragile automation is flagged before the move.
  - **AC:** Deprecated/legacy workflow patterns, unbound flows, and orphaned steps are findings; flow analysis degrades gracefully where `clientdata` is unavailable.

## FEAT-MIG02-3 — Security, integration, and code readiness `[Planned]`
- **US-MIG02.3.1** `[Planned]` **As** a **SEC**, **I want** security roles, users, and teams analyzed for undocumented or environment-specific access, **so that** access is portable across the move.
  - **AC:** Custom roles, direct-user privilege grants, and BU-bound assignments are surfaced as readiness findings.
- **US-MIG02.3.2** `[Planned]` **As** an **ARCH**, **I want** JavaScript/web resources, environment variables, and connection references scanned for hardcoded URLs, GUIDs, and environment values, **so that** environment-specific bindings are found before cutover.
  - **AC:** Hardcoded URL/GUID/endpoint detection and unbound connection references are findings; reuses DRA's hardcoded-value scanning; integration endpoints are inventoried.

## FEAT-MIG02-4 — Data readiness and volume `[Planned]`
- **US-MIG02.4.1** `[Planned]` **As** an ALM specialist, **I want** data-volume indicators and large-table detection, **so that** I can estimate migration effort and risk.
  - **AC:** Row-count indicators use aggregate/total-count queries (not full retrieves); large tables above a configurable threshold are flagged.
- **US-MIG02.4.2** `[Planned]` **As** an ALM specialist, **I want** duplicate metadata and basic data-quality indicators flagged, **so that** poor data doesn't fail the migration.
  - **AC:** Duplicate-metadata detection and any data sampling limit rows by default and mask sensitive values in output.

## FEAT-MIG02-5 — Score, register, checklist, and export `[Planned]`
- **US-MIG02.5.1** `[Planned]` **As** an **MGR**, **I want** a readiness score per category and overall, **so that** I get a go/no-go migration signal.
  - **AC:** Scoring is a UI-free weighted roll-up across the nine readiness categories with severities Critical/High/Medium/Low/Info; category weights round-trip via settings.
- **US-MIG02.5.2** `[Planned]` **As** an ALM specialist, **I want** a migration blockers grid, a risk register, and a cutover checklist, **so that** I can plan and execute the migration.
  - **AC:** Blockers are ordered by severity; the checklist is generated from findings; the grid filters by category/severity.
- **US-MIG02.5.3** `[Planned]` **As** an MGR, **I want** an exported readiness report, **so that** I can share the gate decision with stakeholders.
  - **AC:** Exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; masked values stay masked; read-only.

## Definition of Done
- Follows suite conventions; read-only default; row sampling limited + sensitive values masked; cross-env via `TargetOrganization` if a second connection is used; export formats Excel, PDF, JSON, HTML.
- Readiness analyzers are UI-free and **reuse the shipped Deployment Risk Analyzer engine** (`IAnalyzer` contract, degrade-on-failure) rather than duplicating it.
- Testing skeleton under testing/MigrationReadinessChecker/ when implementation starts.
