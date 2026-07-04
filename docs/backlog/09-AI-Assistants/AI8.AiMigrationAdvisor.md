# AI Migration Advisor — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 9 (AI Assistants), item 8. Not in pack file.
> **Suggested tag:** `AI8` · **Suggested project:** `XrmToolSuite.AiMigrationAdvisor`
> **Overlaps:** **AI Solution Reviewer (shipped)** — reuse AI client, session-only key, consent-preview, redaction, offline-fallback. Consumes the **Migration/ALM-track** deterministic analyzers: Solution Dependency Validator (ALM2), Managed Solution Impact Checker (ALM4), Environment Variable Validator (ALM5), Connection Reference Validator (ALM6), plus Solution Complexity Score and Deployment Risk Analyzer for readiness signals; Solution Knowledge Graph for dependency mapping.
> **Value/priority (my read):** Medium-High — migration projects need a structured readiness assessment; deterministic dependency/blocker detection is concrete, and AI adds wave planning and the risk register.

## Notes
- Deterministic first: analyze component inventory, dependencies, managed/unmanaged status, solution layering, data-volume indicators, plugins/custom APIs, flows/connection references, environment variables, Power Pages metadata where available, security roles/users/teams, and detectable integrations/external endpoints. Detectors: migration blockers, unsupported/deprecated patterns, hardcoded environment values.
- Produce a deterministic migration readiness score and blockers grid with severity — fully available with AI off.
- AI is opt-in and clearly labelled advisory: create a migration roadmap, wave plan, risk register, cutover checklist, testing strategy and rollback guidance — it never moves solutions or changes anything.
- Reuse AI Solution Reviewer's AI client: session-only API key (never persisted), consent preview of the exact redacted payload before any send, GUIDs/URLs/user names/emails/endpoint hosts masked, audit trail, offline fallback to deterministic report on failure. Prompts/system-instructions configurable.
- Read-only tool. Reads via `Service.RetrieveAll` off the UI thread through `RunAsync`; progress + cancellation; settings round-trip. Keep the migration-rules and dependency-analysis engine UI-free and unit-testable.

---

## EPIC-AI8 — Assess migration readiness and generate a wave-based migration plan
> **As** an ALM lead / ARCH, **I want** deterministic migration readiness findings with an optional AI layer that builds a roadmap and risk register, **so that** I can plan a move without manually correlating dependencies and blockers.

**Outcome:** a readiness score, a blockers grid with dependency mapping, and (opt-in) an AI migration roadmap, wave plan and risk register — deterministic findings stand alone with AI off.

---

## FEAT-AI8-1 — Deterministic rules-first findings `[Planned]`
- **US-AI8.1.1** `[Planned]` **As** an ALM lead, **I want** the full readiness assessment and score without any AI, **so that** the tool is useful and safe with AI disabled.
  - **AC:** Complete deterministic findings and readiness score available offline; AI toggled off by default.
- **US-AI8.1.2** `[Planned]` **As** an ALM lead, **I want** detection of migration blockers, unsupported/deprecated patterns and hardcoded environment values across a selected solution or environment, **so that** blockers surface deterministically.
  - **AC:** Reads via `RetrieveAll` off the UI thread with progress and cancellation; each finding carries severity and evidence.

## FEAT-AI8-2 — Migration dashboard, scope & dependencies `[Planned]`
- **US-AI8.2.1** `[Planned]` **As** an ARCH, **I want** a migration dashboard with a scope selector, readiness score cards, a blockers grid and a dependency panel, **so that** I can gauge readiness and impact.
  - **AC:** Score deterministic and reproducible with AI off; dependency mapping reuses deterministic dependency analysis.

## FEAT-AI8-3 — AI migration roadmap & wave plan (opt-in) `[Planned]`
- **US-AI8.3.1** `[Planned]` **As** an ALM lead, **I want** an AI migration executive summary, roadmap and wave plan, **so that** I can sequence the migration.
  - **AC:** Consent preview of the exact redacted payload before send; session-only key; GUIDs/URLs/emails/names/endpoints masked; labelled advisory; audit trail; offline fallback.
- **US-AI8.3.2** `[Planned]` **As** an ARCH, **I want** an AI-generated cutover checklist, testing strategy and rollback plan guidance, **so that** execution is de-risked.
  - **AC:** Advisory only; the tool never performs any migration action.

## FEAT-AI8-4 — AI risk register (opt-in) `[Planned]`
- **US-AI8.4.1** `[Planned]` **As** an MGR, **I want** AI to generate a risk register from blockers and dependencies, **so that** I can track and mitigate migration risk.
  - **AC:** Register entries trace to specific deterministic findings; offline fallback yields the deterministic blockers list.

## FEAT-AI8-5 — Configurable prompt templates `[Planned]`
- **US-AI8.5.1** `[Planned]` **As** a TOOLDEV, **I want** editable AI prompt/system-instruction templates, **so that** the plan matches our migration methodology.
  - **AC:** Templates round-trip via settings as POCOs; no key/connection details persisted.

## FEAT-AI8-6 — Export `[Planned]`
- **US-AI8.6.1** `[Planned]` **As** an ALM lead, **I want** to export the migration advisor report (readiness, blockers, roadmap, risk register) to Excel/PDF/Word/CSV/Markdown/HTML/JSON, **so that** I can share the plan.
  - **AC:** Export runs off the UI thread with progress and cancellation; sensitive values masked; AI sections labelled advisory.

## Definition of Done
- Follows suite conventions; deterministic analysis works with AI off; AI opt-in, redacted, consented, audited; export formats: Excel, PDF, Word, CSV, Markdown, HTML, JSON.
- Testing skeleton under testing/AiMigrationAdvisor/ when implementation starts.
