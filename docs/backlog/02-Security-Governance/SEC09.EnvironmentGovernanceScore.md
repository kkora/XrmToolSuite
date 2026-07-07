# Environment Governance Score — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 9. Not in pack file.
> **Suggested tag:** `SEC09` · **Suggested project:** `XrmToolSuite.EnvironmentGovernanceScore`
> **Overlaps:** Aggregates signals from many suite tools — Sharing Analyzer (SEC04), Audit Compliance Checker (SEC05), Sensitive Data Scanner (SEC07), Licensing Usage Analyzer (SEC08), User Access Heatmap (SEC10), plus shipped Deployment Risk Analyzer, Technical Debt Analyzer, Solution Complexity Score. Also overlaps the candidate Reporting Governance Scorecard. This is the executive roll-up above them.
> **Value/priority (my read):** Medium-High — a single executive score is compelling to MGRs, but it is only as good as the analyzers under it; best built after several SEC tools exist so it can reuse their engines.

## Notes
- Governance categories to score: Security, Audit, Compliance, ALM, Data Protection, Ownership, Configuration, Access Management, Maintainability. Each is a weighted roll-up of category rules.
- Reuse existing analyzers' UI-free engines rather than re-querying: role complexity/privileged users (SEC01/SEC06/SEC10), audit config (SEC05), sharing risk (SEC04), sensitive-data protection (SEC07), inactive users/teams (SEC08/SEC02), unmanaged customizations/solution layering/env variables/connection references (shipped Deployment Risk + Technical Debt analyzers).
- Signals: `role`/`roleprivileges`, `audit` settings, `principalobjectaccess`, `solution`/`solutioncomponent`, `environmentvariabledefinition`, `connectionreference`, `systemuser`/`team`.
- Read-only. Full scan is heavy — run analyzers sequentially off the UI thread with progress + cancellation; degrade any failed category to Info, never abort the whole score.
- Local history: persist prior scores in settings/local files (no credentials) for a trend view. Keep the scoring/rules engine UI-free and unit-testable.

---

## EPIC-SEC09 — Give leadership one explainable governance health score for an environment
> **As** an MGR / ARCH, **I want** a 0-100 governance score with category breakdowns and a remediation roadmap, **so that** I can judge and communicate an environment's health at a glance.

**Outcome:** an overall score (0-100) with nine category scores, a critical-findings list, a prioritized remediation roadmap, optional local trend history, and an executive export.

---

## FEAT-SEC09-1 — Data collection & orchestration `[Planned]`
- **US-SEC09.1.1** `[Planned]` **As** an ARCH, **I want** the tool to run each category analyzer and collect its findings, **so that** the score reflects real environment state.
  - **AC:** Collection runs sequentially off the UI thread with progress and cancellation; a failed category degrades to Info without aborting.
- **US-SEC09.1.2** `[Planned]` **As** a TOOLDEV, **I want** category engines reused from existing analyzers, **so that** rules live in one place and stay unit-testable.

## FEAT-SEC09-2 — Category scoring `[Planned]`
- **US-SEC09.2.1** `[Planned]` **As** an MGR, **I want** a score per category (Security, Audit, Compliance, ALM, Data Protection, Ownership, Configuration, Access Management, Maintainability), **so that** I see where the environment is weak.
  - **AC:** Each category score is deterministic and explainable from its contributing findings.
- **US-SEC09.2.2** `[Planned]` **As** an ARCH, **I want** category weights configurable, **so that** the score reflects our governance priorities.
  - **AC:** Weight settings round-trip via settings load/save.

## FEAT-SEC09-3 — Overall score & findings `[Planned]`
- **US-SEC09.3.1** `[Planned]` **As** an MGR, **I want** an overall 0-100 score with category score cards and a critical-findings list, **so that** I get an executive dashboard.

## FEAT-SEC09-4 — Remediation roadmap `[Planned]`
- **US-SEC09.4.1** `[Planned]` **As** an ARCH, **I want** a prioritized remediation roadmap (highest-impact fixes first), **so that** teams know what to tackle next.
  - **AC:** Roadmap items link back to the finding and its source analyzer; all read-only.

## FEAT-SEC09-5 — Trend history `[Planned]`
- **US-SEC09.5.1** `[Planned]` **As** an MGR, **I want** past scores saved locally and shown as a trend, **so that** I can prove governance is improving.
  - **AC:** History persists locally with no credentials or sensitive values stored.

## FEAT-SEC09-6 — Export `[Planned]`
- **US-SEC09.6.1** `[Planned]` **As** an MGR, **I want** to export an executive governance report to Excel/PDF/CSV/HTML, **so that** I can present it to leadership.
  - **AC:** Export runs off the UI thread with progress; sensitive values masked.

## Definition of Done
- Follows suite conventions; read-only default; sensitive values masked in exports; scores explainable from listed evidence; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/EnvironmentGovernanceScore/ when implementation starts.
