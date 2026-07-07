# Deployment Analytics — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 12 (Reporting & Analytics), item 5. Not in pack file.
> **Suggested tag:** `RPT05` · **Suggested project:** `XrmToolSuite.DeploymentAnalytics`
> **Overlaps:** Overlaps the candidate ALM Deployment Timeline Visualizer (ALM track) — both build a deployment timeline from solution/component history. NOTE clearly: keep this the analytics/KPI + risk-pattern layer; if the ALM visualizer ships, reuse its timeline model rather than rebuild it. Also overlaps ALM KPI Dashboard (RPT02, readiness KPIs) and the shipped Deployment Risk Analyzer (risk rules) — reuse the risk engine; complements it with historical/frequency analysis.
> **Value/priority (my read):** Medium — release managers value deployment frequency/pattern insight, but Dataverse exposes limited native import history (no first-class import log table via SDK on all versions), so several features are "where available" and depend on solution/component modified dates as a proxy. Solid but constrained by data availability.

## Notes
- Analytics layer over solution history: derives a deployment timeline and KPIs from `solution` created/modified dates, version changes, `publisher`, and `solutioncomponent` modified dates; reuses the shipped Deployment Risk Analyzer's UI-free risk engine for risk indicators.
- Native import history is limited via SDK — treat "solution import history / failed-deployment indicators" as best-effort "where available"; fall back to version-change + modified-date inference and label inferred data as such.
- KPIs: deployment frequency, release volume, upgrade/patch/hotfix patterns, unmanaged changes after deployment, version deltas per solution; deterministic risk rules flag hotfix bursts and post-deploy unmanaged drift.
- Signals: `solution`, `solutioncomponent`, `publisher`, `msdyn_solutionhistory`/import job data where available — all via Service.RetrieveAll off the UI thread with targeted ColumnSets.
- Local snapshot storage (no credentials) so frequency/volume can be trended between runs; timeline + frequency charts as inline SVG/PNG for self-contained exports.
- Read-only; heavy history scans run off the UI thread via RunAsync/WorkAsync with progress + cancellation; unavailable data sources degrade to Info, never abort.

---

## EPIC-RPT05 — Give release managers insight into what deployed, when, and how risky the cadence is
> **As** an MGR / ALM, **I want** deployment history, frequency, and risk patterns visualized, **so that** I understand release cadence and spot ALM problems.

**Outcome:** a deployment timeline, a solution-version grid, a release-frequency chart, a deployment-risk panel, publisher/version filters, local trend snapshots, and an exportable deployment analytics report.

---

## FEAT-RPT05-1 — Deployment history collection `[Planned]`
- **US-RPT05.1.1** `[Planned]` **As** an ALM lead, **I want** solution versions, publishers, and created/modified dates collected, **so that** I have the deployment history.
  - **AC:** Retrieval uses Service.RetrieveAll off the UI thread with progress and cancellation; targeted ColumnSets only.
- **US-RPT05.1.2** `[Planned]` **As** an ARCH, **I want** import-history / failed-deployment indicators used where available and inferred otherwise, **so that** the analysis works across Dataverse versions.
  - **AC:** Unavailable native sources degrade to Info; inferred data is labeled "inferred", not presented as authoritative.

## FEAT-RPT05-2 — Deployment timeline `[Planned]`
- **US-RPT05.2.1** `[Planned]` **As** an MGR, **I want** a deployment timeline of solution imports/version changes, **so that** I can see release history at a glance.
  - **AC:** Timeline renders as inline SVG/PNG; reuses the ALM timeline model if that tool exists.
- **US-RPT05.2.2** `[Planned]` **As** an ALM lead, **I want** a solution-version grid with publisher/version filters, **so that** I can drill into specific solutions.

## FEAT-RPT05-3 — Deployment KPIs `[Planned]`
- **US-RPT05.3.1** `[Planned]` **As** an MGR, **I want** deployment-frequency and release-volume KPIs plus a frequency chart, **so that** I can judge cadence.
  - **AC:** KPI calc is deterministic from the collected history and explainable.
- **US-RPT05.3.2** `[Planned]` **As** an ALM lead, **I want** upgrade/patch/hotfix pattern detection, **so that** I can spot unstable release behavior.

## FEAT-RPT05-4 — Deployment risk panel `[Planned]`
- **US-RPT05.4.1** `[Planned]` **As** an ARCH, **I want** risk indicators (hotfix bursts, unmanaged changes after deployment), **so that** I can flag risky ALM patterns.
  - **AC:** Risk rules reuse the shipped Deployment Risk Analyzer engine; findings carry Critical/High/Medium/Low/Info severity.

## FEAT-RPT05-5 — Snapshots & trend `[Planned]`
- **US-RPT05.5.1** `[Planned]` **As** an MGR, **I want** frequency/volume saved locally and trended, **so that** I can show cadence changing over time.
  - **AC:** Snapshots persist locally with no credentials.

## FEAT-RPT05-6 — Export `[Planned]`
- **US-RPT05.6.1** `[Planned]` **As** an MGR, **I want** to export the deployment analytics report to Excel/PDF/CSV/HTML, **so that** I can share release insight.
  - **AC:** Export runs off the UI thread with progress; timeline/frequency charts embedded.

## Definition of Done
- Follows suite conventions; read-only default; inferred data clearly labeled; snapshots stored locally (no credentials); risk rules reuse the shipped analyzer; export formats: Excel, PDF, CSV, HTML, JSON, PNG, SVG.
- Testing skeleton under testing/DeploymentAnalytics/ when implementation starts.
