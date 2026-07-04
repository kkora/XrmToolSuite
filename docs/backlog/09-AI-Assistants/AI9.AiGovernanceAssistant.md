# AI Governance Assistant — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 9 (AI Assistants), item 9. Not in pack file.
> **Suggested tag:** `AI9` · **Suggested project:** `XrmToolSuite.AiGovernanceAssistant`
> **Overlaps:** **AI Solution Reviewer (shipped)** — reuse AI client, session-only key, consent-preview, redaction, offline-fallback. Consumes the **Security/Governance-track** deterministic analyzers: Environment Governance Score (SEC9), Audit Compliance Checker (SEC5), Sensitive Data Scanner (SEC7), Licensing Usage Analyzer (SEC8), Privilege Gap Analyzer (SEC1), plus ALM-track (ALM readiness) and Technical Debt Analyzer (documentation completeness). Overlaps AI Security Reviewer (AI3) — governance is the broader roll-up; share security signals rather than duplicate them.
> **Value/priority (my read):** High — governance is a top manager concern spanning many tracks; deterministic scorecards already exist, and AI turns them into policy and an operating-model roadmap.

## Notes
- Deterministic first: analyze environment governance, solution governance, ALM readiness, security posture, audit configuration, data-protection risks, environment variables/connection references, unmanaged customizations, privileged users, inactive users/teams, Power Pages security where available, flow/connector governance where available, and documentation completeness — reusing SEC/ALM-track analyzers. Produce a deterministic governance score and gaps list with severity — fully available with AI off.
- AI is opt-in and clearly labelled advisory: write governance summary, recommend policies, generate a risk-based remediation roadmap, create an executive scorecard, explain control gaps and suggest operating-model changes — it never changes environment or tenant settings.
- Reuse AI Solution Reviewer's AI client: session-only API key (never persisted), consent preview of the exact redacted payload before any send, GUIDs/URLs/user names/emails/sensitive values masked, audit trail, offline fallback to deterministic report on failure. Prompts/system-instructions configurable.
- Read-only tool. Reads via `Service.RetrieveAll` off the UI thread through `RunAsync`; progress + cancellation; settings round-trip. Keep the governance-rules aggregation UI-free and unit-testable.

---

## EPIC-AI9 — Turn governance findings into policy, scorecards and a remediation roadmap
> **As** an MGR / SEC lead, **I want** deterministic governance findings with an optional AI layer that recommends policy and builds an executive scorecard, **so that** I can operationalise governance without manually translating technical gaps.

**Outcome:** a governance score, a control-gap list across environments/solutions/security/ALM/audit/documentation, and (opt-in) an AI governance summary, policy recommendations and remediation roadmap — deterministic findings stand alone with AI off.

---

## FEAT-AI9-1 — Deterministic rules-first findings `[Planned]`
- **US-AI9.1.1** `[Planned]` **As** an MGR, **I want** the full governance assessment and score without any AI, **so that** the tool is useful and safe with AI disabled.
  - **AC:** Complete deterministic findings, gaps and governance score available offline; AI toggled off by default.
- **US-AI9.1.2** `[Planned]` **As** a SEC lead, **I want** governance gaps detected across ALM readiness, security posture, audit configuration, data protection, privileged/inactive users, unmanaged customizations and documentation completeness, **so that** control gaps surface deterministically.
  - **AC:** Reads via `RetrieveAll` off the UI thread with progress and cancellation; each gap carries severity and evidence.

## FEAT-AI9-2 — Governance dashboard & scorecards `[Planned]`
- **US-AI9.2.1** `[Planned]` **As** an MGR, **I want** a governance dashboard with score cards, a policy-gap panel and a findings grid, **so that** I can see governance health across domains.
  - **AC:** Scores deterministic and reproducible with AI off.

## FEAT-AI9-3 — AI governance summary & policy recommendations (opt-in) `[Planned]`
- **US-AI9.3.1** `[Planned]` **As** an MGR, **I want** an AI executive governance scorecard and audit-ready summary with control-gap explanations, **so that** I can brief leadership and auditors.
  - **AC:** Consent preview of the exact redacted payload before send; session-only key; GUIDs/URLs/emails/user names/sensitive values masked; labelled advisory; audit trail; offline fallback.
- **US-AI9.3.2** `[Planned]` **As** a SEC lead, **I want** AI policy recommendations and operating-model suggestions from recurring gaps, **so that** I can codify governance standards.
  - **AC:** Advisory only; the tool never changes tenant/environment settings.

## FEAT-AI9-4 — AI remediation roadmap (opt-in) `[Planned]`
- **US-AI9.4.1** `[Planned]` **As** an MGR, **I want** AI to generate a risk-based remediation roadmap, **so that** I can prioritise governance work.
  - **AC:** Roadmap items trace to specific deterministic gaps; offline fallback yields the deterministic gaps list.

## FEAT-AI9-5 — Configurable prompt templates `[Planned]`
- **US-AI9.5.1** `[Planned]` **As** a TOOLDEV, **I want** editable AI prompt/system-instruction templates, **so that** summaries match our governance framework.
  - **AC:** Templates round-trip via settings as POCOs; no key/connection details persisted.

## FEAT-AI9-6 — Export `[Planned]`
- **US-AI9.6.1** `[Planned]` **As** an MGR, **I want** to export the governance report (score, gaps, scorecard, roadmap) to Excel/PDF/Word/CSV/Markdown/HTML/JSON, **so that** I can share it with stakeholders and auditors.
  - **AC:** Export runs off the UI thread with progress and cancellation; sensitive values masked; AI sections labelled advisory.

## Definition of Done
- Follows suite conventions; deterministic analysis works with AI off; AI opt-in, redacted, consented, audited; export formats: Excel, PDF, Word, CSV, Markdown, HTML, JSON.
- Testing skeleton under testing/AiGovernanceAssistant/ when implementation starts.
