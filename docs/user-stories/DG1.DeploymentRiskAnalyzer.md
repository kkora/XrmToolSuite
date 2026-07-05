# Deployment Risk Analyzer — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/01-ALM-DevOps/DG1.DeploymentRiskAnalyzer.md`](../backlog/01-ALM-DevOps/DG1.DeploymentRiskAnalyzer.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.DeploymentRiskAnalyzer` · **Area tag:** `DG` — (pre-tagging; ALM track)
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free / fake-connection, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (live Dataverse connection, GUI export dialogs, GDI/MigraDoc PDF, Anthropic HTTP) — pending manual sign-off.

Read-only pre-import risk analysis for Dataverse solutions. Nine `IAnalyzer` engines
(`DependencyAnalyzer`, `EnvironmentVariableAnalyzer`, `FlowPluginAnalyzer`, `SecurityAnalyzer`,
`SchemaConflictAnalyzer`, `DeletedComponentAnalyzer`, `FormAnalyzer`, `RibbonAnalyzer`,
`PowerPagesAnalyzer`) run off the UI thread via `RunAsync` and emit severity-ranked findings, which
`Scoring/RiskScoreCalculator.cs` rolls into a weighted Low/Medium/High score. An optional second
(target) connection — requested via `RaiseRequestConnectionEvent` with `actionName="TargetOrganization"`
and handled in `UpdateConnection` without dropping the source — enables cross-environment checks
(schema, version, deleted components, target config). Results export to HTML, JSON, Markdown, Excel and
a native PDF, with an optional (opt-in, consent-gated) AI executive summary. The analyzers stay UI-free
and depend only on `IOrganizationService`, so they are liftable into a console/CI wrapper; the scoring,
summary-payload and report exporters are unit-tested SDK-free/fake-connection, while the live-connection
and GUI/export paths are manual-tested.

---

## EPIC-DG — Pre-deployment risk analysis for Dataverse solutions `[Implemented]`
> **As** an **ALM** engineer, **I want** to analyze a solution *before* I import it into a target
> environment, **so that** I catch dependency, configuration, security, and schema problems while they
> are cheap to fix instead of discovering them as a failed or damaging import.

**Outcome:** a Low/Medium/High risk score with actionable, severity-ranked findings and rollback
guidance, exportable for humans (HTML/PDF/Excel/Markdown) and for pipelines (JSON with a CI gate).

---

## FEAT-DG-1 — Connect to source and target environments `[Implemented*]`
- **US-DG-1.1** `[Implemented*]` Load the solutions in the source (dev) environment and pick one to scope the analysis.
  - **AC:** Solutions load off the UI thread with progress via `RunAsync`; managed and unmanaged solutions are selectable. *(Manual — live Dataverse.)*
- **US-DG-1.2** `[Implemented*]` Optionally connect a target (test/prod) environment as a second connection for cross-environment checks.
  - **AC:** The second connection is requested with `RaiseRequestConnectionEvent` (`actionName="TargetOrganization"`) and handled in `UpdateConnection` without replacing the primary source connection.
  - **AC:** Target-only analyzers are skipped/disabled when no target is connected. *(Manual — needs two live connections.)*

## FEAT-DG-2 — Solution dependency analysis `[Implemented]`
- **US-DG-2.1** `[Implemented]` Detect missing required components so an import will not fail on unmet dependencies.
  - **AC:** `DependencyAnalyzer` uses `RetrieveMissingDependencies` and reports prerequisite managed solutions and missing components. **Automated (fake conn)** — `TC-DG-DEP-03/04`.
- **US-DG-2.2** `[Implemented*]` Flag publisher/option-value prefix collisions and components duplicated across unmanaged solutions.
  - **AC:** Prefix collisions and cross-solution duplicate components appear as severity-tagged findings. *(Manual — needs richer metadata / duplicate layers; unmanaged-state path automated `TC-DG-DEP-01/02`.)*
- **US-DG-2.3** `[Implemented]` Flag components installed in the target but missing from the source so a managed upgrade does not silently delete tables/columns and their data.
  - **AC:** `DeletedComponentAnalyzer` diffs target vs source by (type, objectid): on a managed target, removed tables → Critical, columns → High, other components → Medium, each noting data loss and the Upgrade-vs-Update distinction; unmanaged target → single Info; no target/no prior version → Info; degrades to Info on query failure. **Automated (fake conn)** — `TC-DG-DC-01..07`.

## FEAT-DG-3 — Environment variables & connection references `[Implemented]`
- **US-DG-3.1** `[Implemented]` Flag environment variables with no default/current value and secret (Key Vault) variables.
  - **AC:** `EnvironmentVariableAnalyzer` reports definitions lacking a usable value; secrets are called out as per-environment config. **Automated (fake conn)** — `TC-DG-EV-01/03/04/05/06`.
- **US-DG-3.2** `[Implemented]` Detect values accidentally packaged into the solution and unbound/missing connection references.
  - **AC:** Packaged values and unbound/missing connection references (verified against target when connected) are findings. **Automated (fake conn)** — `TC-DG-EV-02/07/08/09`.

## FEAT-DG-4 — Flow & plugin readiness `[Implemented]`
- **US-DG-4.1** `[Implemented]` Detect draft (OFF) flows/processes and flows referencing non-existent connection references.
  - **AC:** `FlowPluginAnalyzer` parses `clientdata` to resolve referenced connection references; draft cloud flows → Medium, draft classic processes → Low, missing refs → High. **Automated (fake conn)** — `TC-DG-FP-01/02/03/04`.
- **US-DG-4.2** `[Implemented*]` Flag plugin steps with missing types/assemblies, disabled steps, and steps targeting tables absent from the target.
  - **AC:** Each condition is reported with the owning step/assembly. *(Manual — plugin-step *health* needs aliased LEFT-OUTER joins / richer metadata.)*
- **US-DG-4.3** `[Implemented]` Detect duplicate SDK step registrations and execution-rank conflicts.
  - **AC:** For steps on the same event (message + filter + stage): the same plugin type registered twice with overlapping filtering attributes and the same mode → High "Duplicate SDK step registration"; enabled steps of different types sharing a rank → Medium "share an execution rank"; disabled steps excluded. **Automated (fake conn)** — `TC-DG-FP-05..10`.

## FEAT-DG-5 — Security impact `[Implemented*]`
- **US-DG-5.1** `[Implemented*]` Flag new custom tables with no role coverage and secured columns without field security profiles.
  - **AC:** `SecurityAnalyzer` reports tables lacking role privileges and secured columns lacking FLS profiles. *(Manual — needs security role/field metadata.)*
- **US-DG-5.2** `[Implemented*]` Flag roles assigned to no user/team in the target.
  - **AC:** Roles with no user/team assignment surface as findings. *(Manual — target-side privilege data.)*

## FEAT-DG-6 — Data model / schema conflict analysis (target required) `[Implemented]`
- **US-DG-6.1** `[Implemented]` Detect import-breaking schema conflicts against the target.
  - **AC:** `SchemaConflictAnalyzer` reports attribute type mismatches (Critical), string max-length reductions (High), choice value/label conflicts and removed values (Medium/High), and relationship schema-name collisions (High). **Automated (fake conn, `MetaBuilder`-seeded)** — `TC-DG-SC-06..10`.
- **US-DG-6.2** `[Implemented]` Detect solution-version-not-incremented and managed/unmanaged mismatch.
  - **AC:** Source version ≤ target → High "not incremented"; managed source vs unmanaged target → Critical "mismatch"; solution absent from target → no version finding; no target → single Info. **Automated (fake conn)** — `TC-DG-SC-01..05`.

## FEAT-DG-7 — Power Pages readiness `[Implemented]`
- **US-DG-7.1** `[Implemented]` Flag Power Pages security/readiness gaps (web role defaults, tables surfaced without table permissions, forms bypassing permissions, empty web files/snippets, baseline site settings).
  - **AC:** `PowerPagesAnalyzer` supports both `adx_` and `mspp_` (enhanced data model) schemas; no site → single Info; missing web roles/table permissions → High; empty web file → Medium; empty snippet → Low; findings include a cache-refresh checklist. **Automated (fake conn)** — `TC-DG-PP-01..07`.

## FEAT-DG-8 — Risk scoring & deployment summary `[Implemented]`
- **US-DG-8.1** `[Implemented]` Roll findings into a weighted Low/Medium/High score for a single go/no-go signal.
  - **AC:** `Scoring/RiskScoreCalculator.cs`: Critical=25, High=12, Medium=5, Low=2, Info=0, capped at 100; ≥40 or any Critical → High, ≥15 → Medium, else Low; weights/bands tunable in that file. **Automated (SDK-free)** — `TC-DG-SCORE-01..05`, `TC-DG-BAND-06..11`, `TC-DG-EXPLAIN-12`, `TC-DG-SUMMARY-13`.
- **US-DG-8.2** `[Implemented*]` Provide an executive deployment summary with a go/no-go recommendation.
  - **AC:** An offline, deterministic summary (NO-GO / GO WITH CAUTION / GO per band) is the default, needs no network, and is always available; the anonymized payload builder maps score/risk/target flag, supports component redaction (Mode C) and top-N truncation. **Automated (SDK-free)** — `TC-DG-SUM-01..05`.
  - **AC:** AI generation (Anthropic) is opt-in and consent-gated — a dialog previews the exact JSON/endpoint/model before sending; the payload carries finding metadata only (no record data, credentials, or environment names) with an optional component-name redaction toggle; the API key is session-only and never persisted; on any AI failure the tool falls back to the offline summary; the summary embeds in the PDF/HTML/JSON exports. *(`[Implemented*]` — live Anthropic HTTP path is manual, `TC-DG-M-16`.)*

## FEAT-DG-9 — Report export `[Implemented]`
- **US-DG-9.1** `[Implemented]` Export a styled, self-contained HTML dashboard report.
  - **AC:** No external CSS/JS/fonts and theme-aware (light/dark): radial score gauge, severity KPI cards, risk categories, top issues, recommendations, next steps, findings detail; `@media print` makes browser Print → Save as PDF pixel-identical; output is HTML-encoded. **Automated (SDK-free)** — `TC-DG-HTML-01..08`, `TC-DG-RPT-02`.
- **US-DG-9.2** `[Implemented]` Export an Excel workbook (Summary / Findings / Fix Checklist).
  - **AC:** ClosedXML workbook (Plugins-root export chain) produces a valid OOXML/ZIP package. **Automated** — `TC-DG-RPT-05`; GUI SaveFileDialog flow `[Implemented*]` (`TC-DG-M-12`).
- **US-DG-9.3** `[Implemented]` Export JSON with `ci.pass` and `suggestedExitCode` to gate a pipeline.
  - **AC:** A non-passing (High-gate) result yields `ci.pass=false` and a non-zero `suggestedExitCode`. **Automated** — `TC-DG-RPT-03`.
- **US-DG-9.4** `[Implemented]` Export a Markdown fix checklist that always ends with rollback guidance.
  - **AC:** Output contains a "Fix Checklist" plus a "## Guidance" section with rollback steps. **Automated** — `TC-DG-RPT-04`.
- **US-DG-9.5** `[Implemented]` Export a native executive PDF rendered directly (not via browser print).
  - **AC:** Rendered with MigraDoc/PdfSharp (GDI, net48) mirroring the HTML dashboard — a radial score gauge drawn as a PdfSharp `XGraphics` overlay (decorative-only; drawing failures degrade to a gauge-less PDF, never fail the export), colour-coded risk banner, risk-categories/recommendations tables, next-steps block, severity summary, findings grouped by category; the `-gdi` PDF assemblies ship in the Plugins root. **Automated (exporter)** — `TC-DG-RPT-01/06`; GUI export dialog + font/chain resolution `[Implemented*]` (`TC-DG-M-15`).

## FEAT-DG-10 — Resilience & extensibility `[Implemented]`
- **US-DG-10.1** `[Implemented]` An analyzer whose query fails degrades to an informational finding so one permission gap never aborts the whole run.
  - **AC:** Each analyzer catches query failures and emits an Info finding instead of throwing (e.g. the "check skipped" Info paths). **Automated (fake conn)** — `TC-DG-SC-01`, `TC-DG-DC-01`, `TC-DG-PP-01`; live permission-gap case `[Implemented*]` (`TC-DG-M-10`).
- **US-DG-10.2** `[Implemented]` Add an analyzer by implementing `IAnalyzer` and registering one line.
  - **AC:** Analyzers depend only on `IOrganizationService` (UI-free) and register in `_allAnalyzers` in `DeploymentRiskAnalyzerControl.cs`; the fake-connection test suite proves they are liftable into a console/CI wrapper.

## FEAT-DG-11 — Form changes `[Implemented]`
- **US-DG-11** `[Implemented]` Flag forms whose script/control references a web resource missing from the source.
  - **AC:** `FormAnalyzer` checks `formLibraries`, event-handler `libraryName`, and `$webresource:` references against the source web resources; a missing reference → High; source-only; degrades to Info on query failure. **Automated (fake conn)** — `TC-DG-FM-01..04`.

## FEAT-DG-12 — Ribbon changes `[Implemented]`
- **US-DG-12** `[Implemented]` Flag ribbon (command bar) commands referencing a web resource missing from the source.
  - **AC:** `RibbonAnalyzer` checks `$webresource:` references in `ribbondiffxml` against the source web resources; a missing reference → High; source-only; degrades to Info on query failure. **Automated (fake conn)** — `TC-DG-RB-01..03`.

## FEAT-DG-14 — Help & Support `[Implemented*]`
- **US-DG-14** `[Implemented*]` An in-tool Help & Support page for documentation, issue reporting, and support.
  - **AC:** A **Help** button (right of the toolbar) opens a dialog with Documentation (repo README), Report an issue (GitHub `issues/new`), and a Buy-me-a-coffee support link, each opened in the default browser; the control implements `IGitHubPlugin` (`kkora/XrmToolSuite`) and `IHelpPlugin`. *(Manual GUI — `TC-DG-M-17`.)*

---

## Planned / future
- **US-DG-13.1** `[Planned]` A headless CLI runner for the analyzers (no XrmToolBox) for direct CI use.
- **US-DG-13.2** `[Planned]` Save/load selected-analyzer and target-connection profiles for one-click repeat analyses.
- **US-DG-13.4** `[Planned]` A baseline/diff mode comparing this run to a previous report to show whether risk improved or regressed.

## Definition of Done
- Follows suite conventions (`BaseToolControl`, `RunAsync`/`RetrieveAll` semantics, Load/SaveSettings, progress + cancellation, dual-connection via `TargetOrganization`). — **Done.**
- Read-only; the nine analyzers stay UI-free and depend only on `IOrganizationService`, degrading query failures to informational findings. — **Done.**
- Weighted Low/Medium/High scoring with tunable weights/bands in `Scoring/RiskScoreCalculator.cs`. — **Done** (unit-tested SDK-free).
- Export formats: HTML, JSON (CI gate), Markdown (rollback guidance), Excel, native PDF — plus an opt-in, consent-gated AI executive summary with an always-available offline fallback. — **Done** (exporters unit-tested; GUI dialogs and live AI path pending manual sign-off).
- Testing under `testing/DeploymentRiskAnalyzer/`; SDK-free scoring/summary logic in `testing/UnitTests`, analyzer logic in `testing/AnalyzerTests` (fake connection), exporters in `testing/ReportTests`. — **Done** (80 automated cases green; connections, security/plugin-health metadata, and the GUI/export flow pending manual sign-off in a Windows + XrmToolBox session).
