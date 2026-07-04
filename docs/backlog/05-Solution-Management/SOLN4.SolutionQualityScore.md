# Solution Quality Score — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 5 (Solution Management), item 4. Not in pack file.
> **Suggested tag:** `SOLN4` · **Suggested project:** `XrmToolSuite.SolutionQualityScore`
> **Overlaps:** **STRONGLY overlaps the shipped Solution Complexity Score** — that tool already selects a solution, inventories components, and computes a weighted score with category breakdown, findings, and export. It also overlaps Deployment Risk Analyzer (dependency/publisher/env-var/connection-ref checks), Technical Debt Analyzer (unused/duplicate/JS-quality signals), and AI Solution Reviewer (recommendations). **Recommendation: do NOT ship as a duplicate scorer — extend Solution Complexity Score with the governance/ALM-readiness categories it lacks (Documentation Completeness, ALM Readiness, Security Readiness, Configuration Readiness), or reuse its scoring engine and analyzer set.** Standalone value is low; extension value is real.
> **Value/priority (my read):** Low as a new tool (near-duplicate); Medium-High as an *extension* of Solution Complexity Score adding governance/documentation/security categories.

## Notes
- Reuse the Solution Complexity Score scoring engine, component inventory, and finding/severity model (Critical/High/Medium/Low/Info) rather than rebuilding. Reuse Deployment Risk Analyzer analyzers for dependency completeness, publisher consistency, env-var and connection-reference checks. Reuse Technical Debt Analyzer for duplicate/unused/JS-quality signals.
- Core data: `solution`, `solutioncomponent`, `msdyn_solutioncomponentsummary`, `publisher`; per-type metadata for tables/columns/forms/views/dashboards/business rules; `environmentvariabledefinition`, `connectionreference`, `pluginassembly`/step config, security roles included in the solution.
- Read-only — the tool evaluates and recommends; it never changes the solution.
- Retrieval off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress and honor cancellation; cache per connection and clear on `UpdateConnection`.
- Keep the rules engine and scoring UI-free (analyzer style) so they are unit-testable in `testing/UnitTests/` (banding/scoring is exactly the SDK-free logic that project targets); degrade unavailable checks to Info, not a crash.
- The differentiator vs Complexity Score is *governance/ALM readiness and documentation completeness*, not raw complexity — frame new categories there.

---

## EPIC-SOLN4 — Score a solution's deployment and governance readiness 0–100
> **As** an **ALM** engineer, **I want** an overall quality score with category breakdowns and a remediation backlog for a solution, **so that** I know if it is enterprise-ready before deployment, upgrade, or handoff.

**Outcome:** an overall 0–100 score with a band (Enterprise Ready / Good / Needs Improvement / High Risk / Not Deployment Ready), per-category scores, severity-ranked findings, a remediation backlog, and exportable executive + technical reports.

---

## FEAT-SOLN4-1 — Select solution and compute overall score `[Planned]`
- **US-SOLN4.1.1** `[Planned]` **As** an ALM engineer, **I want** to select one solution and get an overall quality score, **so that** I have a single readiness number.
  - **AC:** Score computes off the UI thread with progress/cancel; overall value maps to a band (90–100 Enterprise Ready … 0–39 Not Deployment Ready) shown on a dashboard.
- **US-SOLN4.1.2** `[Planned]` **As** an ALM engineer, **I want** the score reproducible and reusing the Complexity Score engine, **so that** results stay consistent across the suite.
  - **AC:** Scoring reuses the shared analyzer/scoring core; weighting lives in settings and round-trips via Load/SaveSettings.

## FEAT-SOLN4-2 — Governance and ALM-readiness categories `[Planned]`
- **US-SOLN4.2.1** `[Planned]` **As** an ALM engineer, **I want** ALM Readiness and Dependency Quality scored, **so that** I know the solution imports cleanly and is layered correctly.
  - **AC:** Managed/unmanaged status, publisher consistency, and dependency completeness (missing required components) feed these categories; each is a distinct category card.
- **US-SOLN4.2.2** `[Planned]` **As** an ALM engineer, **I want** Configuration Readiness and Security Readiness scored, **so that** I know env vars, connection refs, plugin steps, and role inclusion are sound.
  - **AC:** Environment-variable usage, connection-reference usage, plugin-step configuration, and included security roles each contribute; gaps become findings.

## FEAT-SOLN4-3 — Maintainability, naming, and documentation categories `[Planned]`
- **US-SOLN4.3.1** `[Planned]` **As** a CUST developer, **I want** Naming Standards and Documentation Completeness scored, **so that** the solution is maintainable and self-describing.
  - **AC:** Publisher-prefix/naming conformance and description/documentation coverage across components produce category scores and per-component findings.
- **US-SOLN4.3.2** `[Planned]` **As** a CUST developer, **I want** Maintainability, Performance Risk, and Component Hygiene scored, **so that** complexity and dead weight are visible.
  - **AC:** Form/view/dashboard complexity, business-rule complexity, JavaScript-quality signals, and duplicate/unused components feed these categories (reusing Technical Debt Analyzer signals).

## FEAT-SOLN4-4 — Findings, severity, and remediation backlog `[Planned]`
- **US-SOLN4.4.1** `[Planned]` **As** a Delivery Manager, **I want** findings ranked by severity, **so that** I address the biggest risks first.
  - **AC:** Findings carry Critical/High/Medium/Low/Info severity and link to the component and category that produced them.
- **US-SOLN4.4.2** `[Planned]` **As** a Delivery Manager, **I want** a remediation backlog generated from the findings, **so that** the score turns into actionable work.
  - **AC:** Backlog items describe the fix and its category impact; the backlog is exportable.

## FEAT-SOLN4-5 — Executive and technical export `[Planned]`
- **US-SOLN4.5.1** `[Planned]` **As** a release manager, **I want** executive and technical reports exported to Excel, PDF, JSON, and HTML, **so that** I can share the score with stakeholders and gate a release.
  - **AC:** Executive report leads with score/band/category summary; technical report includes all findings and the backlog; HTML is self-contained and theme-aware; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses Solution Complexity Score + Deployment Risk / Technical Debt analyzers instead of duplicating them.
- Read-only default; rules engine and scoring stay UI-free and unit-tested in testing/UnitTests/; unavailable checks degrade to Info.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/SolutionQualityScore/ when implementation starts.
