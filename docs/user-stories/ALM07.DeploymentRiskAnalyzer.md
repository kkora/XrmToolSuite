# Deployment Risk Analyzer — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/01-ALM-DevOps/ALM07.DeploymentRiskAnalyzer.md`](../backlog/01-ALM-DevOps/ALM07.DeploymentRiskAnalyzer.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.DeploymentRiskAnalyzer` · **Area tag:** `ALM07`
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

## EPIC-ALM07 — Pre-deployment risk analysis for Dataverse solutions `[Implemented]`
> **As** an **ALM** engineer, **I want** to analyze a solution *before* I import it into a target
> environment, **so that** I catch dependency, configuration, security, and schema problems while they
> are cheap to fix instead of discovering them as a failed or damaging import.

**Outcome:** a Low/Medium/High risk score with actionable, severity-ranked findings and rollback
guidance, exportable for humans (HTML/PDF/Excel/Markdown) and for pipelines (JSON with a CI gate).

---

## FEAT-ALM07-1 — Connect to source and target environments `[Implemented*]`
- **US-ALM07-1.1** `[Implemented*]` Load the solutions in the source (dev) environment and pick one to scope the analysis.
  - **AC:** Solutions load off the UI thread with progress via `RunAsync`; managed and unmanaged solutions are selectable. *(Manual — live Dataverse.)*
- **US-ALM07-1.2** `[Implemented*]` Optionally connect a target (test/prod) environment as a second connection for cross-environment checks.
  - **AC:** The second connection is requested with `RaiseRequestConnectionEvent` (`actionName="TargetOrganization"`) and handled in `UpdateConnection` without replacing the primary source connection.
  - **AC:** Target-only analyzers are skipped/disabled when no target is connected. *(Manual — needs two live connections.)*

## FEAT-ALM07-2 — Solution dependency analysis `[Implemented]`
- **US-ALM07-2.1** `[Implemented]` Detect missing required components so an import will not fail on unmet dependencies.
  - **AC:** `DependencyAnalyzer` uses `RetrieveMissingDependencies` and reports prerequisite managed solutions and missing components. **Automated (fake conn)** — `TC-ALM07-DEP-03/04`.
- **US-ALM07-2.2** `[Implemented*]` Flag publisher/option-value prefix collisions and components duplicated across unmanaged solutions.
  - **AC:** Prefix collisions and cross-solution duplicate components appear as severity-tagged findings. *(Manual — needs richer metadata / duplicate layers; unmanaged-state path automated `TC-ALM07-DEP-01/02`.)*
- **US-ALM07-2.3** `[Implemented]` Flag components installed in the target but missing from the source so a managed upgrade does not silently delete tables/columns and their data.
  - **AC:** `DeletedComponentAnalyzer` diffs target vs source by (type, objectid): on a managed target, removed tables → Critical, columns → High, other components → Medium, each noting data loss and the Upgrade-vs-Update distinction; unmanaged target → single Info; no target/no prior version → Info; degrades to Info on query failure. **Automated (fake conn)** — `TC-ALM07-DC-01..07`.

## FEAT-ALM07-3 — Environment variables & connection references `[Implemented]`
- **US-ALM07-3.1** `[Implemented]` Flag environment variables with no default/current value and secret (Key Vault) variables.
  - **AC:** `EnvironmentVariableAnalyzer` reports definitions lacking a usable value; secrets are called out as per-environment config. **Automated (fake conn)** — `TC-ALM07-EV-01/03/04/05/06`.
- **US-ALM07-3.2** `[Implemented]` Detect values accidentally packaged into the solution and unbound/missing connection references.
  - **AC:** Packaged values and unbound/missing connection references (verified against target when connected) are findings. **Automated (fake conn)** — `TC-ALM07-EV-02/07/08/09`.

## FEAT-ALM07-4 — Flow & plugin readiness `[Implemented]`
- **US-ALM07-4.1** `[Implemented]` Detect draft (OFF) flows/processes and flows referencing non-existent connection references.
  - **AC:** `FlowPluginAnalyzer` parses `clientdata` to resolve referenced connection references; draft cloud flows → Medium, draft classic processes → Low, missing refs → High. **Automated (fake conn)** — `TC-ALM07-FP-01/02/03/04`.
- **US-ALM07-4.2** `[Implemented*]` Flag plugin steps with missing types/assemblies, disabled steps, and steps targeting tables absent from the target.
  - **AC:** Each condition is reported with the owning step/assembly. *(Manual — plugin-step *health* needs aliased LEFT-OUTER joins / richer metadata.)*
- **US-ALM07-4.3** `[Implemented]` Detect duplicate SDK step registrations and execution-rank conflicts.
  - **AC:** For steps on the same event (message + filter + stage): the same plugin type registered twice with overlapping filtering attributes and the same mode → High "Duplicate SDK step registration"; enabled steps of different types sharing a rank → Medium "share an execution rank"; disabled steps excluded. **Automated (fake conn)** — `TC-ALM07-FP-05..10`.

## FEAT-ALM07-5 — Security impact `[Implemented*]`
- **US-ALM07-5.1** `[Implemented*]` Flag new custom tables with no role coverage and secured columns without field security profiles.
  - **AC:** `SecurityAnalyzer` reports tables lacking role privileges and secured columns lacking FLS profiles. *(Manual — needs security role/field metadata.)*
- **US-ALM07-5.2** `[Implemented*]` Flag roles assigned to no user/team in the target.
  - **AC:** Roles with no user/team assignment surface as findings. *(Manual — target-side privilege data.)*

## FEAT-ALM07-6 — Data model / schema conflict analysis (target required) `[Implemented]`
- **US-ALM07-6.1** `[Implemented]` Detect import-breaking schema conflicts against the target.
  - **AC:** `SchemaConflictAnalyzer` reports attribute type mismatches (Critical), string max-length reductions (High), choice value/label conflicts and removed values (Medium/High), and relationship schema-name collisions (High). **Automated (fake conn, `MetaBuilder`-seeded)** — `TC-ALM07-SC-06..10`.
- **US-ALM07-6.2** `[Implemented]` Detect solution-version-not-incremented and managed/unmanaged mismatch.
  - **AC:** Source version ≤ target → High "not incremented"; managed source vs unmanaged target → Critical "mismatch"; solution absent from target → no version finding; no target → single Info. **Automated (fake conn)** — `TC-ALM07-SC-01..05`.

## FEAT-ALM07-7 — Power Pages readiness `[Implemented]`
- **US-ALM07-7.1** `[Implemented]` Flag Power Pages security/readiness gaps (web role defaults, tables surfaced without table permissions, forms bypassing permissions, empty web files/snippets, baseline site settings).
  - **AC:** `PowerPagesAnalyzer` supports both `adx_` and `mspp_` (enhanced data model) schemas; no site → single Info; missing web roles/table permissions → High; empty web file → Medium; empty snippet → Low; findings include a cache-refresh checklist. **Automated (fake conn)** — `TC-ALM07-PP-01..07`.

## FEAT-ALM07-8 — Risk scoring & deployment summary `[Implemented]`
- **US-ALM07-8.1** `[Implemented]` Roll findings into a weighted Low/Medium/High score for a single go/no-go signal.
  - **AC:** `Scoring/RiskScoreCalculator.cs`: Critical=25, High=12, Medium=5, Low=2, Info=0, capped at 100; ≥40 or any Critical → High, ≥15 → Medium, else Low; weights/bands tunable in that file. **Automated (SDK-free)** — `TC-ALM07-SCORE-01..05`, `TC-ALM07-BAND-06..11`, `TC-ALM07-EXPLAIN-12`, `TC-ALM07-SUMMARY-13`.
- **US-ALM07-8.2** `[Implemented*]` Provide an executive deployment summary with a go/no-go recommendation.
  - **AC:** An offline, deterministic summary (NO-GO / GO WITH CAUTION / GO per band) is the default, needs no network, and is always available; the anonymized payload builder maps score/risk/target flag, supports component redaction (Mode C) and top-N truncation. **Automated (SDK-free)** — `TC-ALM07-SUM-01..05`.
  - **AC:** AI generation (Anthropic) is opt-in and consent-gated — a dialog previews the exact JSON/endpoint/model before sending; the payload carries finding metadata only (no record data, credentials, or environment names) with an optional component-name redaction toggle; the API key is session-only and never persisted; on any AI failure the tool falls back to the offline summary; the summary embeds in the PDF/HTML/JSON exports. *(`[Implemented*]` — live Anthropic HTTP path is manual, `TC-ALM07-M-16`.)*

## FEAT-ALM07-9 — Report export `[Implemented]`
- **US-ALM07-9.1** `[Implemented]` Export a styled, self-contained HTML dashboard report.
  - **AC:** No external CSS/JS/fonts and theme-aware (light/dark): radial score gauge, severity KPI cards, risk categories, top issues, recommendations, next steps, findings detail; `@media print` makes browser Print → Save as PDF pixel-identical; output is HTML-encoded. **Automated (SDK-free)** — `TC-ALM07-HTML-01..08`, `TC-ALM07-RPT-02`.
- **US-ALM07-9.2** `[Implemented]` Export an Excel workbook (Summary / Findings / Fix Checklist).
  - **AC:** ClosedXML workbook (Plugins-root export chain) produces a valid OOXML/ZIP package. **Automated** — `TC-ALM07-RPT-05`; GUI SaveFileDialog flow `[Implemented*]` (`TC-ALM07-M-12`).
- **US-ALM07-9.3** `[Implemented]` Export JSON with `ci.pass` and `suggestedExitCode` to gate a pipeline.
  - **AC:** A non-passing (High-gate) result yields `ci.pass=false` and a non-zero `suggestedExitCode`. **Automated** — `TC-ALM07-RPT-03`.
- **US-ALM07-9.4** `[Implemented]` Export a Markdown fix checklist that always ends with rollback guidance.
  - **AC:** Output contains a "Fix Checklist" plus a "## Guidance" section with rollback steps. **Automated** — `TC-ALM07-RPT-04`.
- **US-ALM07-9.5** `[Implemented]` Export a native executive PDF rendered directly (not via browser print).
  - **AC:** Rendered with MigraDoc/PdfSharp (GDI, net48) mirroring the HTML dashboard — a radial score gauge drawn as a PdfSharp `XGraphics` overlay (decorative-only; drawing failures degrade to a gauge-less PDF, never fail the export), colour-coded risk banner, risk-categories/recommendations tables, next-steps block, severity summary, findings grouped by category; the `-gdi` PDF assemblies ship in the Plugins root. **Automated (exporter)** — `TC-ALM07-RPT-01/06`; GUI export dialog + font/chain resolution `[Implemented*]` (`TC-ALM07-M-15`).

## FEAT-ALM07-10 — Resilience & extensibility `[Implemented]`
- **US-ALM07-10.1** `[Implemented]` An analyzer whose query fails degrades to an informational finding so one permission gap never aborts the whole run.
  - **AC:** Each analyzer catches query failures and emits an Info finding instead of throwing (e.g. the "check skipped" Info paths). **Automated (fake conn)** — `TC-ALM07-SC-01`, `TC-ALM07-DC-01`, `TC-ALM07-PP-01`; live permission-gap case `[Implemented*]` (`TC-ALM07-M-10`).
- **US-ALM07-10.2** `[Implemented]` Add an analyzer by implementing `IAnalyzer` and registering one line.
  - **AC:** Analyzers depend only on `IOrganizationService` (UI-free) and register in `_allAnalyzers` in `DeploymentRiskAnalyzerControl.cs`; the fake-connection test suite proves they are liftable into a console/CI wrapper.

## FEAT-ALM07-11 — Form changes `[Implemented]`
- **US-ALM07-11** `[Implemented]` Flag forms whose script/control references a web resource missing from the source.
  - **AC:** `FormAnalyzer` checks `formLibraries`, event-handler `libraryName`, and `$webresource:` references against the source web resources; a missing reference → High; source-only; degrades to Info on query failure. **Automated (fake conn)** — `TC-ALM07-FM-01..04`.

## FEAT-ALM07-12 — Ribbon changes `[Implemented]`
- **US-ALM07-12** `[Implemented]` Flag ribbon (command bar) commands referencing a web resource missing from the source.
  - **AC:** `RibbonAnalyzer` checks `$webresource:` references in `ribbondiffxml` against the source web resources; a missing reference → High; source-only; degrades to Info on query failure. **Automated (fake conn)** — `TC-ALM07-RB-01..03`.

## FEAT-ALM07-14 — Help & Support `[Implemented*]`
- **US-ALM07-14** `[Implemented*]` An in-tool Help & Support page for documentation, issue reporting, and support.
  - **AC:** A **Help** button (right of the toolbar) opens a dialog with Documentation (repo README), Report an issue (GitHub `issues/new`), and a Buy-me-a-coffee support link, each opened in the default browser; the control implements `IGitHubPlugin` (`kkora/XrmToolSuite`) and `IHelpPlugin`. *(Manual GUI — `TC-ALM07-M-17`.)*

---

## Planned / future
- **US-ALM07-13.1** `[Planned]` A headless CLI runner for the analyzers (no XrmToolBox) for direct CI use.
- **US-ALM07-13.2** `[Planned]` Save/load selected-analyzer and target-connection profiles for one-click repeat analyses.
- **US-ALM07-13.4** `[Planned]` A baseline/diff mode comparing this run to a previous report to show whether risk improved or regressed.

## Definition of Done
- Follows suite conventions (`BaseToolControl`, `RunAsync`/`RetrieveAll` semantics, Load/SaveSettings, progress + cancellation, dual-connection via `TargetOrganization`). — **Done.**
- Read-only; the nine analyzers stay UI-free and depend only on `IOrganizationService`, degrading query failures to informational findings. — **Done.**
- Weighted Low/Medium/High scoring with tunable weights/bands in `Scoring/RiskScoreCalculator.cs`. — **Done** (unit-tested SDK-free).
- Export formats: HTML, JSON (CI gate), Markdown (rollback guidance), Excel, native PDF — plus an opt-in, consent-gated AI executive summary with an always-available offline fallback. — **Done** (exporters unit-tested; GUI dialogs and live AI path pending manual sign-off).
- Testing under `testing/DeploymentRiskAnalyzer/`; SDK-free scoring/summary logic in `testing/UnitTests`, analyzer logic in `testing/AnalyzerTests` (fake connection), exporters in `testing/ReportTests`. — **Done** (80 automated cases green; connections, security/plugin-health metadata, and the GUI/export flow pending manual sign-off in a Windows + XrmToolBox session).
