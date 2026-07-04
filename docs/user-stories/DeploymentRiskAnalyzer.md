# Deployment Risk Analyzer - User Stories

Tool Epic under the XrmToolSuite portfolio. See the [index](README.md) for personas, ID scheme, and
status legend. Area tag: `DG`.

Most of this tool is shipped; items are marked `[Done] / [WIP] / [Planned]` to reflect reality.

---

## EPIC-DG - Pre-deployment risk analysis for Dataverse solutions

> **As** an **ALM** engineer, **I want** to analyze a solution *before* I import it into a target
> environment, **so that** I catch dependency, configuration, security, and schema problems while
> they are cheap to fix instead of discovering them as a failed or damaging import.

**Outcome:** a Low/Medium/High risk score with actionable findings and rollback guidance, exportable
for humans and for pipelines.

---

## FEAT-DG-1 - Connect to source and target environments `[Done]`

- **US-DG-1.1** `[Done]` **As** an ALM engineer, **I want** to load the solutions in my source (dev)
  environment and pick one, **so that** I can scope the analysis to what I am about to ship.
  - **AC:** Solutions load off the UI thread with progress; unmanaged and managed solutions are selectable.
- **US-DG-1.2** `[Done]` **As** an ALM engineer, **I want** to *optionally* connect a target
  (test/prod) environment as a second connection, **so that** cross-environment checks (schema,
  version, target config) become available without replacing my primary connection.
  - **AC:** A second connection is requested via `TargetOrganization` and handled in `UpdateConnection` without dropping the source connection.
  - **AC:** Target-only analyzers are clearly disabled/skipped when no target is connected.

## FEAT-DG-2 - Solution dependency analysis `[Done]`

- **US-DG-2.1** `[Done]` **As** an ALM engineer, **I want** missing required components detected,
  **so that** an import will not fail on unmet dependencies.
  - **AC:** Uses `RetrieveMissingDependencies`; reports prerequisite managed solutions and missing components.
- **US-DG-2.2** `[Done]` **As** an ALM engineer, **I want** publisher prefix / option-value-prefix
  collisions and components duplicated across unmanaged solutions flagged, **so that** I avoid layering conflicts.
  - **AC:** Prefix collisions and cross-solution duplicate components appear as findings with severity.
- **US-DG-2.3** `[Done]` **As** a release manager, **I want** components in the target's installed
  solution but missing from the source flagged, **so that** a managed upgrade does not silently delete tables/columns and their data.
  - **AC:** Diffs target vs source solution components by (type, objectid). On a managed target: removed
    tables → Critical, columns → High, other components → Medium, each noting data loss and the
    Upgrade-vs-Update distinction. Unmanaged target → single Info (drift, not deletion); no target or
    no prior version → Info. Degrades to Info on query failure.

## FEAT-DG-3 - Environment variables & connection references `[Done]`

- **US-DG-3.1** `[Done]` **As** an ALM engineer, **I want** environment variables with no
  default/current value and secret (Key Vault) variables flagged, **so that** the target is configured before go-live.
  - **AC:** Definitions lacking a usable value are reported; secrets are called out as per-environment config.
- **US-DG-3.2** `[Done]` **As** an ALM engineer, **I want** values accidentally packaged into the
  solution and unbound/missing connection references detected, **so that** I do not leak env-specific values or ship broken references.
  - **AC:** Packaged values and unbound/missing connection references (verified against target when connected) are findings.

## FEAT-DG-4 - Flow & plugin readiness `[Done]`

- **US-DG-4.1** `[Done]` **As** an ALM engineer, **I want** draft (OFF) flows/processes and flows
  referencing non-existent connection references detected, **so that** automation actually runs after deployment.
  - **AC:** `clientdata` is parsed to resolve referenced connection references; missing ones are findings.
- **US-DG-4.2** `[Done]` **As** an ALM engineer, **I want** plugin steps with missing types/assemblies,
  disabled steps, and steps targeting tables absent from the target flagged, **so that** server-side logic is deployable.
  - **AC:** Each condition is reported with the owning step/assembly.
- **US-DG-4.3** `[Done]` **As** an ALM engineer, **I want** duplicate SDK step registrations and
  plugin execution conflicts detected, **so that** logic does not fire twice or run in a non-deterministic order.
  - **AC:** For steps on the same event (message + filter + stage): the same plugin type registered
    twice with overlapping filtering attributes and the same mode is flagged High "Duplicate SDK step
    registration"; enabled steps of different types sharing an execution rank are flagged Medium
    "Plugin steps share an execution rank". Disabled steps are excluded.

## FEAT-DG-5 - Security impact `[Done]`

- **US-DG-5.1** `[Done]` **As** an ADM, **I want** new custom tables with no role coverage and
  secured columns without field security profiles flagged, **so that** users are neither locked out nor over-exposed.
  - **AC:** Tables lacking role privileges and secured columns lacking FLS profiles are findings.
- **US-DG-5.2** `[Done]` **As** an ADM, **I want** roles assigned to no user/team in the target
  flagged, **so that** I notice ineffective security before relying on it.

## FEAT-DG-6 - Data model / schema conflict analysis (target required) `[Done]`

- **US-DG-6.1** `[Done]` **As** an ALM engineer, **I want** import-breaking schema conflicts
  detected against the target, **so that** I never attempt an import that will hard-fail or corrupt data.
  - **AC:** Attribute type mismatches, string max-length reductions, choice value/label conflicts and removed values, and 1:N/N:N schema-name collisions are reported.
- **US-DG-6.2** `[Done]` **As** an ALM engineer, **I want** solution-version-not-incremented and
  managed/unmanaged mismatch detected, **so that** I avoid downgrade and layering surprises.

## FEAT-DG-7 - Power Pages readiness `[Done]`

- **US-DG-7.1** `[Done]` **As** an ADM, **I want** Power Pages security/readiness gaps flagged
  (web role defaults, tables surfaced without table permissions, forms bypassing permissions, empty
  web files/snippets, baseline site settings), **so that** a portal deployment is safe and functional.
  - **AC:** Supports both `adx_` and `mspp_` (enhanced data model) schemas; findings include a cache-refresh checklist.

## FEAT-DG-8 - Risk scoring `[Done]`

- **US-DG-8.1** `[Done]` **As** an ALM engineer, **I want** findings rolled into a weighted
  Low/Medium/High score, **so that** I get a single go/no-go signal.
  - **AC:** Critical=25, High=12, Medium=5, Low=2, capped at 100; >=40 or any Critical -> High, >=15 -> Medium, else Low.
  - **AC:** Weights/bands are tunable in `Scoring/RiskScoreCalculator.cs`.
- **US-DG-8.2** `[Done]` **As** a release manager, **I want** an executive deployment summary with a
  go/no-go recommendation, **so that** I can brief stakeholders without reading every finding.
  - **AC:** An offline, deterministic summary is the default (no network) and is always available.
  - **AC:** AI generation (Anthropic) is opt-in and auditable — a consent dialog previews the exact
    JSON before sending; the payload carries finding metadata only (no record data, credentials, or
    environment names) with an optional component-name redaction toggle.
  - **AC:** API key is session-only (env var or masked prompt), never persisted; on any AI failure the
    tool falls back to the offline summary.
  - **AC:** "AI summary" button + an "Auto" toggle (auto-run after analysis); Haiku default with an
    Opus toggle; the summary is embedded in the PDF/HTML/JSON exports.

## FEAT-DG-9 - Report export `[Done]`

- **US-DG-9.1** `[Done]` **As** an ADM, **I want** to export a styled HTML **dashboard** report, **so that** I can
  share it or print to PDF.
  - **AC:** Self-contained (no external CSS/JS/fonts) and theme-aware (light/dark): radial score gauge,
    severity KPI cards, risk categories, top issues, recommendations, next steps, and findings detail.
    `@media print` makes browser Print → Save as PDF pixel-identical. SDK-free, so it is unit-tested directly.
- **US-DG-9.2** `[Done]` **As** an ADM, **I want** an Excel workbook (Summary / Findings / Fix
  Checklist), **so that** I can track remediation.
- **US-DG-9.3** `[Done]` **As** a DEVOPS engineer, **I want** a JSON export with `ci.pass` and
  `suggestedExitCode`, **so that** I can gate a pipeline on deployment risk.
  - **AC:** A non-passing result yields a non-zero suggested exit code usable in a release stage.
- **US-DG-9.4** `[Done]` **As** an ADM, **I want** a Markdown fix checklist that always ends with
  rollback guidance, **so that** I have a recovery plan if an import goes wrong.
- **US-DG-9.5** `[Done]` **As** an ADM, **I want** a native executive PDF (rendered directly, not via
  browser print), **so that** report generation is fully unattended.
  - **AC:** Rendered with MigraDoc/PdfSharp (GDI, net48), styled to mirror the HTML dashboard — a radial
    score gauge drawn as a PdfSharp `XGraphics` overlay, colour-coded risk banner, risk-categories and
    recommendations tables, next-steps block, severity summary, and findings grouped by category.
    The gauge is decorative-only (drawing failures degrade to a gauge-less PDF, never fail the export).
    Ships in the Plugins root like the ClosedXML chain (the `-gdi` PDF assemblies).

## FEAT-DG-10 - Resilience & extensibility `[Done]`

- **US-DG-10.1** `[Done]` **As** an ALM engineer, **I want** an analyzer whose query fails to degrade
  to an informational finding, **so that** one permission gap never aborts the whole run.
- **US-DG-10.2** `[Done]` **As** a TOOLDEV, **I want** to add an analyzer by implementing `IAnalyzer`
  and registering one line, **so that** the tool is easy to extend.
  - **AC:** Analyzers depend only on `IOrganizationService` (UI-free) so they can be lifted into a console/CI wrapper.

## FEAT-DG-11 - Form changes `[Done]`

- **US-DG-11** `[Done]` **As** an ALM engineer, **I want** forms flagged when a script/control
  references a web resource missing from the source, **so that** I catch broken form scripts before they ship.
  - **AC:** `formLibraries`, event-handler `libraryName`, and `$webresource:` references are checked
    against the source web resources; a missing reference is High. Source-only; degrades to Info on query failure.

## FEAT-DG-12 - Ribbon changes `[Done]`

- **US-DG-12** `[Done]` **As** an ALM engineer, **I want** ribbon (command bar) commands flagged when
  they reference a web resource missing from the source, **so that** I catch broken buttons before they ship.
  - **AC:** `$webresource:` references in `ribbondiffxml` are checked against the source web resources;
    a missing reference is High. Source-only; degrades to Info on query failure.

## FEAT-DG-14 - Help & Support `[Done]`

- **US-DG-14** `[Done]` **As** a user, **I want** an in-tool Help & Support page, **so that** I can find
  documentation, report issues, and support the plugin without leaving XrmToolBox.
  - **AC:** A **Help** button (right of the toolbar) opens a dialog with three sections —
    **Documentation** (repo README), **Report an issue** (GitHub `issues/new`), and a
    **Buy me a coffee** support link — each opening in the default browser.
  - **AC:** The tool implements `IGitHubPlugin` (`kkora/XrmToolSuite`) and `IHelpPlugin`, so XrmToolBox's
    own tool-menu repository/help links resolve to the same GitHub project.

---

## Planned / future

- **US-DG-13.1** `[Planned]` **As** a DEVOPS engineer, **I want** a headless CLI runner for the
  analyzers (no XrmToolBox), **so that** I can run Deployment Risk Analyzer directly in CI.
- **US-DG-13.2** `[Planned]` **As** an ALM engineer, **I want** to save/load selected-analyzer and
  target-connection profiles, **so that** repeat analyses are one click.
- **US-DG-13.3** `[Done]` Native PDF export — delivered as US-DG-9.5.
- **US-DG-13.4** `[Planned]` **As** an ALM engineer, **I want** a baseline/diff mode comparing this
  run to a previous report, **so that** I see whether risk improved or regressed.
- **US-DG-13.5** `[Done]` AI/offline deployment summary — delivered as US-DG-8.2.
