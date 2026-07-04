# Executive Dashboard — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 12 (Reporting & Analytics), item 1. Not in pack file.
> **Suggested tag:** `RPT1` · **Suggested project:** `XrmToolSuite.ExecutiveDashboard`
> **Overlaps:** Top-level aggregator that rolls up scores from many suite tools — shipped Deployment Risk Analyzer, Technical Debt Analyzer, Solution Complexity Score, plus candidate Governance Scorecard (RPT3), ALM KPI Dashboard (RPT2), Usage Analytics (RPT6), Technical Debt Trends (RPT4). Also overlaps the candidate Admin Environment Health Dashboard — that is component/admin-facing; this is the executive/board-facing single-pane roll-up above it. NOTE the duplication before both are built.
> **Value/priority (my read):** Medium-High — a one-glance executive view is highly sellable to MGRs, but it is a thin aggregation layer whose value depends entirely on the analyzers beneath it; build it after several scoring tools ship so it can reuse their engines rather than re-implement scoring.

## Notes
- Pure aggregator: consumes the deterministic, UI-free analyzer engines from other tracks (Deployment Risk, Technical Debt, Solution Complexity, Governance Scorecard, ALM KPI, Usage) rather than re-querying Dataverse for each score. Adds only the KPI roll-up, heatmap, trend, and executive-report layers.
- Score domains to surface: overall environment health, governance, ALM readiness, security risk, performance, technical debt, storage risk, solution quality, deployment risk — each a 0-100 card with a Critical/High/Medium/Low/Info band.
- Inventory counts (active users, teams, solutions, apps, plugins, flows, Power Pages sites, tables, custom components) via `Service.RetrieveAll` over `systemuser`/`team`/`solution`/`solutioncomponent` and `RetrieveMetadataChanges`; sample cheaply, never `ColumnSet(true)`.
- Local snapshot storage (no credentials) per environment for trend cards; keep the KPI aggregation + banding engine UI-free and unit-testable.
- Chart rendering as inline SVG/PNG so exports (PDF / PowerPoint-style deck / Excel / HTML) are self-contained; render off the UI thread.
- Full roll-up is heavy — run contributing analyzers sequentially via RunAsync/WorkAsync with progress + cancellation; degrade any failed domain to Info, never abort the whole dashboard.

---

## EPIC-RPT1 — Give leadership one executive pane for environment health, risk, and trend
> **As** an MGR, **I want** a single business-friendly dashboard of environment scores, top issues, and trends, **so that** I can judge and communicate platform health without reading technical metadata reports.

**Outcome:** an overall health score with per-domain score cards, a risk heatmap, inventory KPIs, a top-issues panel, a recommendation roadmap, local trend snapshots, and an executive-ready export.

---

## FEAT-RPT1-1 — Score aggregation & orchestration `[Planned]`
- **US-RPT1.1.1** `[Planned]` **As** an ARCH, **I want** the tool to run each contributing analyzer and collect its score and findings, **so that** the dashboard reflects real environment state.
  - **AC:** Aggregation runs off the UI thread (RunAsync/WorkAsync) with progress and cancellation; a failed domain degrades to Info without aborting the roll-up.
- **US-RPT1.1.2** `[Planned]` **As** a TOOLDEV, **I want** domain scores computed by reusing existing analyzer engines, **so that** scoring rules live in one place and stay unit-testable.
  - **AC:** The aggregation/banding engine is UI-free with no Dataverse calls in its signatures.

## FEAT-RPT1-2 — Executive KPI dashboard & score cards `[Planned]`
- **US-RPT1.2.1** `[Planned]` **As** an MGR, **I want** an overall health score plus a card per domain (governance, ALM, security, performance, tech debt, storage, solution quality, deployment risk), **so that** I see strengths and weaknesses at a glance.
  - **AC:** Each card shows a 0-100 score and a Critical/High/Medium/Low/Info band, explainable from its contributing findings.
- **US-RPT1.2.2** `[Planned]` **As** an MGR, **I want** inventory KPI tiles (active users, teams, solutions, apps, plugins, flows, Power Pages sites, tables, custom components), **so that** I understand the environment's size.
  - **AC:** Counts retrieved via Service.RetrieveAll / metadata queries off the UI thread; missing sources shown as "unavailable", not zero.

## FEAT-RPT1-3 — Risk heatmap & top issues `[Planned]`
- **US-RPT1.3.1** `[Planned]` **As** an ARCH, **I want** a risk heatmap across domains and severities, **so that** I can spot concentrations of risk quickly.
  - **AC:** Heatmap renders as inline SVG/PNG and is embeddable in exports.
- **US-RPT1.3.2** `[Planned]` **As** an MGR, **I want** a top-critical-issues panel, **so that** I know the few things that most need attention.

## FEAT-RPT1-4 — Recommendation roadmap `[Planned]`
- **US-RPT1.4.1** `[Planned]` **As** an ARCH, **I want** a prioritized recommendation roadmap (highest-impact first), **so that** teams know what to tackle next.
  - **AC:** Each item links back to its finding and source analyzer; all read-only.

## FEAT-RPT1-5 — Trend snapshots `[Planned]`
- **US-RPT1.5.1** `[Planned]` **As** an MGR, **I want** prior scores saved locally and shown as trend charts, **so that** I can prove health is improving over releases.
  - **AC:** Snapshots persist locally per environment with no credentials or sensitive values stored; trend settings round-trip via settings load/save.

## FEAT-RPT1-6 — Executive export `[Planned]`
- **US-RPT1.6.1** `[Planned]` **As** an MGR, **I want** to export an executive report (PDF / PowerPoint-style deck / Excel / HTML) with charts and KPI data, **so that** I can present to leadership.
  - **AC:** Export runs off the UI thread with progress; charts embedded as SVG/PNG; sensitive values masked.

## Definition of Done
- Follows suite conventions; read-only default; snapshots stored locally (no credentials); scores explainable from listed evidence; export formats: PDF, PowerPoint-style deck, Excel, CSV, HTML, PNG, SVG.
- Testing skeleton under testing/ExecutiveDashboard/ when implementation starts.
