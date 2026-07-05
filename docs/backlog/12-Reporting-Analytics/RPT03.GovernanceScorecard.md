# Governance Scorecard — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 12 (Reporting & Analytics), item 3. Not in pack file.
> **Suggested tag:** `RPT03` · **Suggested project:** `XrmToolSuite.GovernanceScorecard`
> **Overlaps:** STRONG overlap with the candidate Security Environment Governance Score (SEC09) — both compute an overall governance score with category breakdowns, control-gap list, and remediation roadmap. NOTE clearly: pick ONE to build, or make this the Reporting-track presentation/export front-end over SEC09's UI-free rules engine. Also aggregates shipped Deployment Risk Analyzer, Technical Debt Analyzer, and (for security categories) the SEC-track analyzers; feeds the Executive Dashboard (RPT01) governance card.
> **Value/priority (my read):** Medium — genuinely useful repeatable governance assessment for MGR/SEC/ARCH, but its heavy overlap with SEC09 makes it a de-dupe decision, not a straight build. Value is High only if it becomes the single canonical governance scorecard for the suite.

## Notes
- Aggregator/rules-engine tool: rolls category rules over signals collected by existing analyzers rather than re-querying; reuse SEC09's engine if that ships first (avoid two governance scoring implementations).
- Categories to score: Security, Audit, ALM, Data Protection, Configuration, Ownership, Documentation, Operational Readiness, plus Power Pages security and Power Automate connector risk where available. Each is a weighted category roll-up.
- Signals: `role`/`roleprivileges`, `audit` settings, `environmentvariabledefinition`, `connectionreference`, `solution`/`solutioncomponent`, `principalobjectaccess` (sharing), `systemuser`/`team` (inactive/ownership), plus sensitive-data and connector metadata where available.
- Deterministic overall + category scores (0-100) with a control-gap list and prioritized remediation roadmap; keep the governance rules/scoring engine UI-free and unit-testable.
- Local snapshot storage (no credentials) for trend comparison; charts (risk-severity, category) as inline SVG/PNG for self-contained exports.
- Read-only; run categories sequentially via RunAsync/WorkAsync with progress + cancellation; degrade any failed/unavailable category to Info, never abort the scorecard.

---

## EPIC-RPT03 — Produce a repeatable, measurable governance scorecard for an environment
> **As** an MGR / SEC / ARCH, **I want** an overall governance score with category breakdowns, control gaps, and a remediation roadmap, **so that** I can assess and communicate governance maturity repeatably.

**Outcome:** an overall governance score, per-category scores, a control-gap grid, a risk-severity chart, a prioritized remediation roadmap, local trend comparison, and an exportable scorecard report.

---

## FEAT-RPT03-1 — Governance data collection `[Planned]`
- **US-RPT03.1.1** `[Planned]` **As** an ARCH, **I want** each category's signals collected from existing analyzers, **so that** the scorecard reflects real state without duplicate queries.
  - **AC:** Collection runs off the UI thread with progress and cancellation; a failed/unavailable category degrades to Info without aborting.
- **US-RPT03.1.2** `[Planned]` **As** a TOOLDEV, **I want** the governance rules engine kept UI-free (ideally SEC09's engine), **so that** rules stay in one place and unit-testable.

## FEAT-RPT03-2 — Category scoring `[Planned]`
- **US-RPT03.2.1** `[Planned]` **As** an MGR, **I want** a score per category (Security, Audit, ALM, Data Protection, Configuration, Ownership, Documentation, Operational Readiness), **so that** I see where governance is weak.
  - **AC:** Each category score is deterministic and explainable from its contributing rules.
- **US-RPT03.2.2** `[Planned]` **As** an ARCH, **I want** category weights configurable, **so that** the score reflects our governance priorities.
  - **AC:** Weight settings round-trip via settings load/save.

## FEAT-RPT03-3 — Overall score & control gaps `[Planned]`
- **US-RPT03.3.1** `[Planned]` **As** an MGR, **I want** an overall 0-100 score with category score cards and a control-gap grid, **so that** I get a governance dashboard.
  - **AC:** Each control gap carries a Critical/High/Medium/Low/Info severity and links to its evidence.

## FEAT-RPT03-4 — Remediation roadmap `[Planned]`
- **US-RPT03.4.1** `[Planned]` **As** an ARCH, **I want** a prioritized remediation roadmap (highest-impact gaps first), **so that** teams know what to fix next.
  - **AC:** Roadmap items link back to the control gap and its source analyzer; all read-only.

## FEAT-RPT03-5 — Trend comparison `[Planned]`
- **US-RPT03.5.1** `[Planned]` **As** an MGR, **I want** prior scorecards saved locally and compared, **so that** I can show governance improving over time.
  - **AC:** History persists locally with no credentials or sensitive values stored.

## FEAT-RPT03-6 — Export `[Planned]`
- **US-RPT03.6.1** `[Planned]` **As** an MGR, **I want** to export the governance scorecard to Excel/PDF/CSV/HTML, **so that** I can present it to leadership and auditors.
  - **AC:** Export runs off the UI thread with progress; sensitive values masked; charts embedded.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; snapshots stored locally (no credentials); scores explainable from listed rules; export formats: Excel, PDF, CSV, HTML, JSON, PNG, SVG.
- Testing skeleton under testing/GovernanceScorecard/ when implementation starts.
