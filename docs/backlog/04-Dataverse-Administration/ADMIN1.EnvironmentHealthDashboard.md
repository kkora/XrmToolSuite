# Environment Health Dashboard — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 1. Not in pack file.
> **Suggested tag:** `ADMIN1` · **Suggested project:** `XrmToolSuite.EnvironmentHealthDashboard`
> **Overlaps:** This is a **meta-tool** — it aggregates category scans that overlap several shipped and candidate tools: Deployment Risk Analyzer (ALM/plugin/env-var/connection-ref checks), Technical Debt Analyzer (deprecated JS, unmanaged-in-prod), Solution Complexity Score, and candidates ADMIN2 (Metadata Cleanup), ADMIN4/ADMIN5 (storage/growth), ADMIN9 (index advisory). Should *host and roll up* those analyzers, not reimplement them.
> **Value/priority (my read):** High — a single 0–100 environment health score with actionable findings is the headline governance artifact admins currently assemble by hand across PPAC, Maker Portal, and multiple plugins.

## Notes
- Data sources: `RetrieveMetadataChanges` (entity/attribute/relationship inventory), `Organization`/`WhoAmI`/`RetrieveVersion` for org overview, `solution`/`solutioncomponent`/`publisher`, `sdkmessageprocessingstep` + `plugintracelog`, `systemuser`/`team`, `environmentvariabledefinition`/`connectionreference`, `RetrieveTotalRecordCount` for large-table signals, `audit`/`RetrieveAuditPartitionList` for audit-config gaps.
- Architecture: reuse existing suite analyzers as UI-free `IAnalyzer` inputs; each returns findings + a category subscore. Do **not** duplicate their logic — lift into shared core where liftable.
- Category subscores (Security, Performance, Storage, ALM, Configuration, Metadata Quality, Governance, Technical Debt) roll into one weighted 0–100 score with severities Critical/High/Medium/Low/Info.
- Read-only by default. Baseline snapshots stored locally (no deletes) so future scans compare and trend.
- Row counts and exact storage bytes are not fully exposed via SDK — flag large tables heuristically and disclaim estimates.
- Cross-references: ADMIN7 (Environment Inventory) supplies the overview cards; ADMIN8 (Drift Monitor) supplies baseline diffing.

---

## EPIC-ADMIN1 — Give admins one operational health score and prioritized findings for a Dataverse environment
> **As** an **ADM**, **I want** a one-click environment health scan that produces an overall 0–100 score, category subscores, and ranked findings, **so that** I can judge environment health and act on the biggest risks without touching five different portals.

**Outcome:** an overall health score, eight category subscores, a ranked findings grid with severities, prioritized recommendations, a savable baseline with trend, and executive + technical exports.

---

## FEAT-ADMIN1-1 — One-click health scan and scoring engine `[Planned]`
- **US-ADMIN1.1.1** `[Planned]` **As** an ADM, **I want** to run a full health scan from one button, **so that** I get a complete picture in a single pass.
  - **AC:** Scan runs off the UI thread via `RunAsync`/`WorkAsync`, reports per-category progress, and is cancellable via the `BackgroundWorker`.
- **US-ADMIN1.1.2** `[Planned]` **As** an ADM, **I want** an overall 0–100 health score with a rating band, **so that** I can communicate status at a glance.
  - **AC:** Score is a documented weighted roll-up of the eight category subscores; the weighting model is UI-free and unit-testable.
- **US-ADMIN1.1.3** `[Planned]` **As** an ARCH, **I want** each analyzer failure to degrade to an Info finding, **so that** one broken query never aborts the whole scan.
  - **AC:** A category whose query throws is scored as "insufficient data" and surfaces an Info finding, not an exception.

## FEAT-ADMIN1-2 — Category subscores `[Planned]`
- **US-ADMIN1.2.1** `[Planned]` **As** an ADM, **I want** separate subscores for Security, Performance, Storage, ALM, Configuration, Metadata Quality, Governance, and Technical Debt, **so that** I know which dimension is dragging the score down.
  - **AC:** Eight category cards each show a 0–100 subscore, band, and finding count.
- **US-ADMIN1.2.2** `[Planned]` **As** a SEC, **I want** the Security subscore to reflect audit gaps, inactive users, and unused teams, **so that** governance risk is visible in the score.
  - **AC:** Audit-configuration gaps, inactive `systemuser` records, and empty/unused `team` records feed the Security/Governance subscores.

## FEAT-ADMIN1-3 — Environment overview inventory `[Planned]`
- **US-ADMIN1.3.1** `[Planned]` **As** an ADM, **I want** org name, URL, version, region, type, base language, and currency shown, **so that** I can confirm which environment I am judging.
  - **AC:** Overview reads `Organization`/`RetrieveVersion`/`WhoAmI` and renders as cards without blocking the UI.
- **US-ADMIN1.3.2** `[Planned]` **As** an ADM, **I want** counts of users, teams, solutions, tables, columns, relationships, forms, views, and dashboards, **so that** I have a size baseline for the environment.
  - **AC:** Counts derive from `RetrieveMetadataChanges` and record queries via `RetrieveAll`, cached for the session.

## FEAT-ADMIN1-4 — Cross-category risk detection `[Planned]`
- **US-ADMIN1.4.1** `[Planned]` **As** an ADM, **I want** plugin issues detected — duplicate steps, missing filtering attributes, high-depth executions, and trace failures — **so that** operational reliability risks surface.
  - **AC:** Findings derive from `sdkmessageprocessingstep` and `plugintracelog`; each is categorized under Performance or Technical Debt with a severity.
- **US-ADMIN1.4.2** `[Planned]` **As** an ALM, **I want** missing environment variables, missing connection references, unmanaged customizations in production, and solution-layering risks detected, **so that** ALM hygiene is scored.
  - **AC:** Env-var/connection-ref gaps and unmanaged-in-prod components (via `ismanaged`) roll into the ALM subscore.
- **US-ADMIN1.4.3** `[Planned]` **As** a PERF, **I want** large tables, large attachments, storage-growth risk, deprecated JavaScript APIs, and oversized forms/dashboards detected, **so that** performance and storage debt is quantified.
  - **AC:** Large-table detection uses `RetrieveTotalRecordCount` with an estimation disclaimer; deprecated-JS and oversized-form checks reuse shared analyzers.

## FEAT-ADMIN1-5 — Recommendations and findings grid `[Planned]`
- **US-ADMIN1.5.1** `[Planned]` **As** an ADM, **I want** a critical-issues panel and a full findings grid, **so that** I can triage from headline down to detail.
  - **AC:** Grid is filterable/groupable by category and severity (Critical/High/Medium/Low/Info); the critical panel lists only Critical/High.
- **US-ADMIN1.5.2** `[Planned]` **As** an ADM, **I want** prioritized, actionable recommendations, **so that** I know what to fix first.
  - **AC:** Each recommendation links to its finding, states impact and effort, and is ordered by weighted severity.

## FEAT-ADMIN1-6 — Baseline, trend, and export `[Planned]`
- **US-ADMIN1.6.1** `[Planned]` **As** an ADM, **I want** to save a baseline scan and compare future scans against it, **so that** I can prove the environment improved or regressed.
  - **AC:** Baselines persist to local snapshot storage (no deletes); a trend chart plots overall and category scores over saved scans.
- **US-ADMIN1.6.2** `[Planned]` **As** an MGR, **I want** executive and technical health reports exported, **so that** I can brief leadership and hand engineers the detail.
  - **AC:** Exports to Excel, PDF, JSON, and self-contained theme-aware HTML run off the UI thread; settings (weights, ignore list, pricing assumptions) round-trip via Load/SaveSettings.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); destructive ops N/A (read-only meta-tool).
- Hosts existing analyzers as UI-free inputs; degrades query failures to Info; baselines stored locally.
- Export formats: Excel, PDF, JSON, HTML (executive + technical).
- Testing skeleton under testing/EnvironmentHealthDashboard/ when implementation starts.
