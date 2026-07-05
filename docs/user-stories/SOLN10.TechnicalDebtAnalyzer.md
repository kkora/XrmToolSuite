# Technical Debt Analyzer — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/05-Solution-Management/SOLN10.TechnicalDebtAnalyzer.md`](../backlog/05-Solution-Management/SOLN10.TechnicalDebtAnalyzer.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.TechnicalDebtAnalyzer` · **Area tag:** `SOLN10`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (GDI/MigraDoc runtime) — pending manual sign-off.

Scans a connected Dataverse environment with eight UI-free analyzers, scores the accumulated technical
debt on a 0–100 scale with a Low/Medium/High band, and lists prioritized cleanup findings in a
severity-coloured grid. Read-only (queries metadata, plugin registration, web resources, processes and
roles; never modifies anything). Findings project to the suite-shared `ReportModel` and export to Excel,
PDF, HTML, JSON and a Markdown cleanup checklist, plus an executive summary that is offline-templated by
default with an auditable, opt-in AI path. A **Trends** tab (FEAT-SOLN10-4) charts the debt score run-over-run
from per-machine JSON snapshots. The SDK-free scoring/report projection, the trend store/analytics and every
analyzer are covered headlessly against a fake `IOrganizationService`; the WinForms UI, the five exporters,
the Trends tab/chart and the AI/offline summary are manual-tested against a live org.

---

## EPIC-SOLN10 — Quantify and prioritize a Dataverse environment's technical debt `[Implemented]`
> **As** an **ADM/ALM lead**, **I want** a single tool that scans a whole environment, scores its technical
> debt, and lists prioritized cleanup work, **so that** I can plan remediation and track debt down over time.

**Outcome:** a 0–100 technical debt score with a Low/Medium/High band, a categorized findings list, and
shareable Excel/PDF/HTML/JSON/Markdown reports plus an executive summary — produced from a live connection
without writing any queries by hand.

---

## FEAT-SOLN10-0 — Scaffold & shared wiring `[Implemented]`
- **US-SOLN10-0.1** `[Implemented]` The tool loads in XrmToolBox with connection, settings, and background
  execution via `BaseToolControl`.
  - **AC:** `TechnicalDebtAnalyzerControl : BaseToolControl` implements `IGitHubPlugin`/`IHelpPlugin`
    (`kkora/XrmToolSuite`), runs analysis on a `BackgroundWorker` through `RunAsync`, and persists
    `TechDebtSettings` (a plain POCO) on close. MEF metadata incl. both image keys is set on the plugin.
    *(UI load: manual — `TC-SOLN10-M-01`, `xrmtoolbox-tools-list.png`.)*
  - **AC:** No template leftovers (`your-github-username`, "Load sample") remain; a right-aligned Help button
    is added via `CreateHelpButton("Technical Debt Analyzer")`.

## FEAT-SOLN10-1 — Environment scan & analyzers `[Implemented]`
- **US-SOLN10-1.1** `[Implemented]` Pick which analyzers run and scan the connected environment.
  - **AC:** A `CheckedListBox` over `_allAnalyzers` drives the run loop; the unchecked set persists via
    `TechDebtSettings.UncheckedAnalyzers`. All Dataverse access is fail-soft through `TechDebtContext`
    (`SafeRetrieveAll`/`RetrieveAll`, aggregate FetchXML for row counts, `RetrieveAllEntitiesRequest`),
    off the UI thread with progress and cancellation. *(Selection persist: manual — `TC-SOLN10-M-07`.)*
  - **AC:** Any analyzer that throws degrades to an informational finding and is listed as skipped
    (`AnalyzerRunner.Run`); a permission gap returns empty rather than aborting the scan. Per-entity row
    probing is capped (`TechDebtContext.MaxEntityProbes` = 400) and the cap is reported as an Info finding
    when hit. **Automated** — `TC-SOLN10-COL-08`.
- **US-SOLN10-1.2** `[Implemented]` Unused-metadata, duplicate, deprecated-API, orphaned, dead-plugin,
  performance, naming, and security analyzers cover the main debt sources.
  - **AC:** `UnusedMetadataAnalyzer` flags custom tables with 0 rows (Medium) and very wide custom tables
    (≥ 200 custom columns, Low). **Automated** — `TC-SOLN10-COL-08`, `TC-SOLN10-COL-09`.
  - **AC:** `DuplicateArtifactsAnalyzer` flags web resources that share a display name (Low). **Automated**
    — `TC-SOLN10-COL-06`.
  - **AC:** `DeprecatedApiAnalyzer` flags JS web resources referencing `Xrm.Page`, `crmForm`, the 2011
    `/Organization.svc` endpoint, `getServerUrl` or `XMLHttpRequest` (Medium, base64 content decoded).
    **Automated** — `TC-SOLN10-COL-05`.
  - **AC:** `OrphanedComponentsAnalyzer` flags draft processes (`workflow` type 1 / statecode 0) never
    activated (Low). **Automated** — `TC-SOLN10-COL-04`.
  - **AC:** `DeadPluginsAnalyzer` flags disabled steps (Low), non-workflow-activity plugin types with no
    steps (Low), and assemblies with no stepped type (Medium). **Automated** — `TC-SOLN10-COL-03`.
  - **AC:** `PerformanceAnalyzer` flags active steps on `RetrieveMultiple` (High) and synchronous `Update`
    steps with no filtering attributes (Medium). **Automated** — `TC-SOLN10-COL-01`, `TC-SOLN10-COL-02`.
  - **AC:** `NamingViolationsAnalyzer` flags default `new_` publisher prefixes on tables/columns (Low) and
    undocumented custom tables (Info); `SecurityAnalyzer` flags "Copy of …" roles (Low) and secured-column
    sprawl (Info). **Automated** — `TC-SOLN10-COL-07`, `TC-SOLN10-COL-10`, `TC-SOLN10-COL-11`.

## FEAT-SOLN10-2 — Debt score & dashboard `[Implemented]`
- **US-SOLN10-3** `[Implemented]` A 0–100 debt score and a Low/Medium/High band.
  - **AC:** `TechDebtReport.Scorer` weights severities (Critical=25 / High=12 / Medium=5 / Low=2 / Info=0),
    sums and caps at 100, and bands at 15 (Medium) / 40 (High). **Automated** — `TC-SOLN10-SCORE-01`,
    `TC-SOLN10-SCORE-02`, `TC-SOLN10-SCORE-04`.
  - **AC:** Debt does NOT force High on a single Critical (`criticalForcesHigh:false`) — it accumulates.
    **Automated** — `TC-SOLN10-SCORE-03`.
- **US-SOLN10-4** `[Implemented]` Headline metrics (total findings + per-category counts).
  - **AC:** `TechDebtReport.Build` fills `ReportModel.Metrics` with the total plus a per-category breakdown
    (descending by count) that drives the dashboard metric strip. **Automated** — `TC-SOLN10-DASH-05`.

## FEAT-SOLN10-3 — Results, export & summary `[Implemented*]`
- **US-SOLN10-5** `[Implemented*]` Review findings in a colour-coded grid with a detail pane.
  - **AC:** The grid binds findings ordered by descending severity then category, colours the Severity cell
    (Critical/High/Medium/Low), and the selection populates a detail pane (title, component, description,
    recommendation, docs link); a clean environment reads as low debt. *(Manual — `TC-SOLN10-M-02`.)*
- **US-SOLN10-6** `[Implemented*]` Excel, PDF, HTML, JSON and Markdown exports.
  - **AC:** Export routes through the shared reporting module — `ExcelReportExporter` / `PdfReportExporter`
    (MigraDoc/PdfSharp-GDI chain shipped in the Plugins root) / `HtmlDashboardBuilder` /
    `JsonReportExporter` / `FixChecklistGenerator` — each keyed to `ReportModel` and opened on demand.
    *(Manual — `TC-SOLN10-M-08`; JSON/HTML/MD emitters share automated `ReportModel` coverage.)*
- **US-SOLN10-7** `[Implemented*]` An executive summary (offline by default, AI opt-in).
  - **AC:** `TemplatedSummaryGenerator` is the default; `AiSummaryGenerator` is opt-in behind a session-only
    key (`_sessionApiKey`, never persisted; `TechDebtSettings` stores no key) and a payload-preview consent
    dialog (`ShowConsentDialog`) that shows the anonymized JSON before sending; component names in the
    payload are toggleable. *(Manual — `TC-SOLN10-M-09`.)*

## FEAT-SOLN10-4 — Debt trends over time `[Implemented]`
> Formerly the standalone **RPT04 Technical Debt Trends** candidate; built as a Trends tab here (trends need
> the tool's own run-over-run history, which this tool already produces on each scan).
- **US-SOLN10-8** `[Implemented]` A **Trends** tab that charts the debt score run-over-run per environment, so I
  can see whether debt is falling over successive cleanup sprints.
  - **AC:** Each completed scan records a `DebtSnapshot` (timestamp, environment, score, band, total +
    per-category counts) via `TrendStore.Append` — capped to the most recent 100 per environment, with a
    same-run dedupe guard — persisted as **local per-machine JSON** (`TrendHistoryFile`, under the XrmToolBox
    Settings folder). **No Dataverse writes** — the tool stays read-only against the org. The store/analytics
    logic is SDK-free. **Automated** — `TC-SOLN10-TREND-01..08` (append / per-env cap / same-run dedupe / per-env
    isolation; run-over-run delta, improving/worsening direction, series, best/worst).
  - **AC:** The Trends tab (a `TabControl` alongside the Dashboard) shows a runs grid, a **dependency-free
    GDI** score-over-time line chart, a "since last run" delta banner (▼ improving / ▲ worsening), a
    confirmation-gated **Clear history** action, and CSV/JSON export of the snapshot series. *(Tab, chart and
    live capture: manual — `TC-SOLN10-M-10..12`.)*

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll semantics, Load/SaveSettings, progress + cancellation). Read-only — no destructive ops (trend history is local-only; Clear history is confirmation-gated).
- The eight analyzers and `TechDebtContext` stay UI-free and SDK-liftable, and degrade query/permission failures to empty results or informational findings instead of throwing.
- Export formats: Excel, PDF, HTML, JSON, Markdown; executive summary offline-templated by default with an auditable AI opt-in whose key is never persisted. — **Done.**
- Testing under `testing/TechnicalDebtAnalyzer/`; SDK-free scoring/report logic covered by `testing/UnitTests` (`TechDebtScoreTests`), the trend store/analytics by `testing/UnitTests/TechDebtTrendsTests.cs` (`TC-SOLN10-TREND-01..08`), and every analyzer by `testing/CollectorTests` (`TC-SOLN10-COL-01..11`) against a fake `IOrganizationService`. — **Done** (WinForms UI, five exporters, Trends tab/chart, and offline/AI summary pending manual sign-off: `TC-SOLN10-M-01..12`).
