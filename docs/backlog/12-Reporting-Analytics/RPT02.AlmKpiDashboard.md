# ALM KPI Dashboard — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 12 (Reporting & Analytics), item 2. Not in pack file.
> **Suggested tag:** `RPT02` · **Suggested project:** `XrmToolSuite.AlmKpiDashboard`
> **Overlaps:** Aggregates ALM signals from the shipped Deployment Risk Analyzer (deployment readiness, env variables, connection references, unmanaged customizations, layering) and Solution Complexity Score (component counts). Sibling to candidate Deployment Analytics (RPT05, deployment history/timeline) and Executive Dashboard (RPT01, which surfaces the ALM readiness score). NOTE overlap with RPT05 — keep this KPI/scorecard-focused, leave deployment timeline to RPT05.
> **Value/priority (my read):** Medium-High — release managers actively want ALM maturity/readiness KPIs; strong MGR/ALM audience, but most underlying signals already come from Deployment Risk Analyzer, so this is largely a KPI + trend presentation layer.

## Notes
- Aggregator over the Deployment Risk Analyzer's UI-free deployment-readiness engine plus Solution Complexity component counts; adds ALM KPI calc, release-readiness/ALM-maturity scoring, and snapshot/trend.
- KPIs: solutions by managed/unmanaged, solution versions, publishers, unmanaged customizations, layering risks, missing dependencies, environment variables + connection references readiness, component counts, deployment readiness findings, drift indicators.
- Signals: `solution`, `solutioncomponent`, `publisher`, `environmentvariabledefinition`/`environmentvariablevalue`, `connectionreference`, `RetrieveMissingDependencies`, dependency messages — all via Service.RetrieveAll off the UI thread; no `ColumnSet(true)`.
- Release-readiness and ALM-maturity scores are deterministic 0-100 weighted roll-ups from the KPIs; keep the scoring/rules engine UI-free and unit-testable.
- Local snapshot storage (no credentials) per environment/solution for KPI-over-time comparison; charts as inline SVG/PNG for self-contained exports.
- Read-only; heavy scans run sequentially via RunAsync/WorkAsync with progress + cancellation; a failed KPI degrades to Info, never aborts the dashboard.

---

## EPIC-RPT02 — Give release managers measurable ALM KPIs and a release-readiness score
> **As** an MGR / ALM, **I want** ALM metrics, a release-readiness score, and trend comparison, **so that** I can judge solution quality, deployment readiness, and process maturity.

**Outcome:** an ALM KPI dashboard with score cards, a solution-version grid, layering/drift panels, release-readiness and ALM-maturity scores, local trend snapshots, and an exportable ALM KPI report.

---

## FEAT-RPT02-1 — ALM KPI collection `[Planned]`
- **US-RPT02.1.1** `[Planned]` **As** an ALM lead, **I want** solution/publisher/version/component KPIs collected, **so that** I have the raw ALM metrics.
  - **AC:** Retrieval uses Service.RetrieveAll off the UI thread with progress and cancellation; targeted ColumnSets only.
- **US-RPT02.1.2** `[Planned]` **As** a TOOLDEV, **I want** readiness/layering/drift signals reused from the Deployment Risk Analyzer engine, **so that** ALM rules aren't duplicated.
  - **AC:** The KPI engine is UI-free with no Dataverse types in its signatures.

## FEAT-RPT02-2 — KPI cards & solution version grid `[Planned]`
- **US-RPT02.2.1** `[Planned]` **As** an MGR, **I want** KPI cards (managed/unmanaged split, publishers, unmanaged customizations, missing dependencies, env variables + connection references), **so that** I see ALM health at a glance.
  - **AC:** Each card value is explainable from the underlying retrieved records.
- **US-RPT02.2.2** `[Planned]` **As** an ALM lead, **I want** a solution version grid with a solution/environment selector, **so that** I can inspect versions per solution.

## FEAT-RPT02-3 — Layering & drift panels `[Planned]`
- **US-RPT02.3.1** `[Planned]` **As** an ARCH, **I want** a solution-layering risk panel, **so that** I can spot risky layering before deployment.
  - **AC:** Layering findings degrade to Info when the layering query is unavailable, without aborting the scan.
- **US-RPT02.3.2** `[Planned]` **As** an ALM lead, **I want** an environment drift indicator panel, **so that** I know where environments diverge.

## FEAT-RPT02-4 — Readiness & maturity scoring `[Planned]`
- **US-RPT02.4.1** `[Planned]` **As** an MGR, **I want** a release-readiness score and an ALM-maturity score, **so that** I can gate releases on a number.
  - **AC:** Both scores are deterministic 0-100 with Critical/High/Medium/Low/Info bands, explainable from the KPIs; weights round-trip via settings.

## FEAT-RPT02-5 — Snapshots & trend comparison `[Planned]`
- **US-RPT02.5.1** `[Planned]` **As** an MGR, **I want** scan snapshots saved locally and KPIs compared over time, **so that** I can show ALM improving across releases.
  - **AC:** Snapshots persist locally with no credentials; trend chart renders as SVG/PNG.

## FEAT-RPT02-6 — Export `[Planned]`
- **US-RPT02.6.1** `[Planned]` **As** an MGR, **I want** to export the ALM KPI report to Excel/PDF/CSV/HTML, **so that** I can share release readiness with stakeholders.
  - **AC:** Export runs off the UI thread with progress; charts embedded.

## Definition of Done
- Follows suite conventions; read-only default; snapshots stored locally (no credentials); scores explainable from listed KPIs; export formats: Excel, PDF, CSV, HTML, JSON, PNG, SVG.
- Testing skeleton under testing/AlmKpiDashboard/ when implementation starts.
