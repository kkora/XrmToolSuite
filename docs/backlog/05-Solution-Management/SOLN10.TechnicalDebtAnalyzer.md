# Technical Debt Analyzer - User Stories

> **Status:** DONE (shipped tool) — filed under Solution Management. Kept its own area tag and story IDs.

Area tag: `TD`. See the [index](../../README.md) for personas, ID scheme, and status legend.

---

## EPIC-SOLN10 - Quantify and prioritize a Dataverse environment's technical debt

> **As** an ADM/ALM lead, **I want** a single tool that scans a whole environment, scores its technical
> debt, and lists prioritized cleanup work, **so that** I can plan remediation and track debt down over time.

**Outcome:** a 0–100 technical debt score, a categorized findings list, and shareable Excel/PDF/HTML/JSON
reports plus an executive summary — produced from a live connection without writing any queries by hand.

---

## FEAT-SOLN10-0 - Scaffold & shared wiring `[Done]`

- **US-SOLN10-0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background execution via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Tool appears in XTB, connects, runs analysis on a `BackgroundWorker`, persists settings on close.
  - **AC:** No template leftovers (`your-github-username`, "Load sample") remain; MEF metadata incl. both image keys is set.

## FEAT-SOLN10-1 - Environment scan & analyzers `[Done]`

- **US-SOLN10-1.1** `[Done]` **As** an ADM, **I want** to pick which analyzers run and scan the connected
  environment, **so that** I control scope and cost.
  - **AC:** A checked list drives the run loop; selection persists via settings.
  - **AC:** All Dataverse access runs through `RunAsync`/`RetrieveAll`/fetch aggregates with progress and cancellation.
  - **AC:** Any analyzer that throws degrades to an informational finding and is listed as skipped (`AnalyzerRunner`).
- **US-SOLN10-1.2** `[Done]` **As** an ADM, **I want** unused-metadata, duplicate, deprecated-API, orphaned,
  dead-plugin, performance, naming, and security analyzers, **so that** the main debt sources are covered.
  - **AC:** Unused Metadata flags custom tables with 0 rows and very wide tables (probing is capped and reported).
  - **AC:** Deprecated APIs flags JS web resources using `Xrm.Page`, `crmForm`, the 2011 endpoint, etc.
  - **AC:** Dead Plugins flags disabled steps, step-less plugin types, and step-less assemblies.
  - **AC:** Performance flags plugins on `RetrieveMultiple` and sync `Update` steps without filtering attributes.
  - **AC:** Naming flags default `new_` publisher prefixes and undocumented tables; Security flags copied roles and secured-column sprawl.

## FEAT-SOLN10-2 - Debt score & dashboard `[Done]`

- **US-SOLN10-3** `[Done]` **As** an ALM lead, **I want** a 0–100 debt score and a Low/Medium/High band,
  **so that** I can communicate debt at a glance and track it across scans.
  - **AC:** Weighted severities (Critical=25/High=12/Medium=5/Low=2) sum and cap at 100 (`TechDebtReport`).
  - **AC:** Debt does NOT force High on a single Critical — it accumulates (bands at 15/40). *(TC-SOLN10-SCORE-03)*
- **US-SOLN10-4** `[Done]` **As** an ADM, **I want** headline metrics (total findings + per-category counts),
  **so that** the dashboard shows where the debt concentrates.
  - **AC:** `ReportModel.Metrics` carries the total and a per-category breakdown. *(TC-SOLN10-DASH-05)*

## FEAT-SOLN10-3 - Results, export & summary `[Done]`

- **US-SOLN10-5** `[Done]` **As** an ADM, **I want** to review findings in a color-coded grid with detail,
  **so that** I can act on them.
  - **AC:** Grid groups by severity/category with a detail pane; a clean environment reads as low debt, not blank.
- **US-SOLN10-6** `[Done]` **As** an ADM, **I want** Excel/PDF/HTML/JSON/Markdown exports, **so that** I can
  share results or gate CI.
  - **AC:** All five exporters come from the shared reporting module and open on demand.
- **US-SOLN10-7** `[Done]` **As** an ALM lead, **I want** an executive summary (offline by default, AI opt-in),
  **so that** I get a narrative for stakeholders.
  - **AC:** Offline templated summary is the default; AI is opt-in behind a session-only key and a payload-preview consent dialog; the key is never persisted.

---

## Definition of Done (tool-level)

- Every Dataverse call runs off the UI thread via `RunAsync`/`WorkAsync`. *(read-only tool — no destructive ops)*
- Settings round-trip (load on `Load`, save in `ClosingPlugin`); the API key is never persisted.
- nuspec id/version/description/tags are correct and the dependency chain ships in the Plugins root.
- SDK-free scoring/report logic is covered by `testing/UnitTests` (`TechDebtScoreTests`); analyzers/UI are manual-tested against a live org.
