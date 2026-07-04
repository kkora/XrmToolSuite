# AI Plugin Reviewer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 9 (AI Assistants), item 7. Not in pack file.
> **Suggested tag:** `AI7` · **Suggested project:** `XrmToolSuite.AiPluginReviewer`
> **Overlaps:** **AI Solution Reviewer (shipped)** — reuse AI client, session-only key, consent-preview, redaction, offline-fallback. Consumes the **Plugins-track** deterministic analyzers (e.g. Plugin Performance Profiler / PERF1 for trace-log and step analysis); overlaps AI Performance Advisor (AI4) on synchronous/recursion signals — share the plugin-metadata retrieval and trace-log parsing layer.
> **Value/priority (my read):** Medium-High — plugin registrations are hard to review at a glance; deterministic step/image/filtering analysis is concrete and valuable, AI adds refactoring and Custom API conversion advice.

## Notes
- Deterministic first: analyze plugin assemblies, plugin types/classes, SDK steps, stages/modes/ranks/filtering attributes, pre/post images, secure/unsecure configuration usage, and trace logs where available (`pluginassembly`, `plugintype`, `sdkmessageprocessingstep`, `sdkmessageprocessingstepimage`, `plugintracelog`). Detectors: duplicate steps, update steps without filtering attributes, recursion risks, high-depth trace logs, synchronous performance risks, excessive images, outdated assemblies.
- Produce a deterministic plugin quality score and findings grid with severity — fully available with AI off.
- AI is opt-in and clearly labelled advisory: summarise plugin health, explain recursion/performance, recommend refactoring and Custom API conversion, generate developer backlog and best-practice guidance — it never re-registers or edits steps.
- Redaction: unsecure configuration can contain secrets/connection strings — never send raw secure/unsecure config; mask GUIDs/URLs/user names/emails. Trace-log messages are redacted before any AI send.
- Reuse AI Solution Reviewer's AI client: session-only API key (never persisted), consent preview of the exact redacted payload before any send, audit trail, offline fallback to deterministic report on failure. Prompts/system-instructions configurable.
- Read-only tool. Reads via `Service.RetrieveAll` off the UI thread through `RunAsync`; progress + cancellation; settings round-trip. Keep the plugin-rules and trace-log analyzer UI-free and unit-testable.

---

## EPIC-AI7 — Review plugin registration quality and produce refactoring guidance
> **As** a TOOLDEV / DEVOPS, **I want** deterministic plugin findings with an optional AI layer that summarises health and recommends refactoring, **so that** I can assess plugin quality without inspecting each registration by hand.

**Outcome:** a plugin quality score, findings across assemblies/types/steps/images/traces, and (opt-in) AI health summary and refactoring backlog — deterministic findings stand alone with AI off.

---

## FEAT-AI7-1 — Deterministic rules-first findings `[Planned]`
- **US-AI7.1.1** `[Planned]` **As** a TOOLDEV, **I want** the full plugin analysis and score without any AI, **so that** the tool is useful and safe with AI disabled.
  - **AC:** Complete deterministic findings and score available offline; AI toggled off by default.
- **US-AI7.1.2** `[Planned]` **As** a TOOLDEV, **I want** detection of duplicate steps, update steps without filtering attributes, recursion risks, high-depth trace logs, synchronous performance risks, excessive images and outdated assemblies, **so that** the real issues surface deterministically.
  - **AC:** Reads via `RetrieveAll` off the UI thread with progress and cancellation; each finding carries severity and evidence.

## FEAT-AI7-2 — Plugin dashboard, filters & trace summary `[Planned]`
- **US-AI7.2.1** `[Planned]` **As** a TOOLDEV, **I want** a plugin review dashboard with assembly/type/step filters and a findings grid, **so that** I can focus on a specific assembly.
  - **AC:** Score deterministic and reproducible with AI off.
- **US-AI7.2.2** `[Planned]` **As** a DEVOPS, **I want** a trace-log summary panel, **so that** I can spot failing or high-depth executions.
  - **AC:** Trace messages shown with sensitive values masked; nothing leaves the tool unredacted.

## FEAT-AI7-3 — AI plugin health & refactoring advice (opt-in) `[Planned]`
- **US-AI7.3.1** `[Planned]` **As** a TOOLDEV, **I want** an AI plugin-health summary with recursion/performance explanation, **so that** I understand systemic risks.
  - **AC:** Consent preview of the exact redacted payload before send; session-only key; secure/unsecure config never sent; GUIDs/URLs/emails/names masked; labelled advisory; audit trail; offline fallback.
- **US-AI7.3.2** `[Planned]` **As** a TOOLDEV, **I want** AI refactoring recommendations and Custom API conversion suggestions, **so that** I can modernise plugin logic.
  - **AC:** Advisory only; the tool never edits registrations.

## FEAT-AI7-4 — AI developer backlog (opt-in) `[Planned]`
- **US-AI7.4.1** `[Planned]` **As** a DEVOPS, **I want** AI to generate a developer refactoring backlog with best-practice guidance, **so that** I can schedule remediation.
  - **AC:** Backlog items trace to specific deterministic findings; offline fallback yields the deterministic list.

## FEAT-AI7-5 — Configurable prompt templates `[Planned]`
- **US-AI7.5.1** `[Planned]` **As** a TOOLDEV, **I want** editable AI prompt/system-instruction templates, **so that** advice matches our plugin standards.
  - **AC:** Templates round-trip via settings as POCOs; no key/connection details persisted.

## FEAT-AI7-6 — Export `[Planned]`
- **US-AI7.6.1** `[Planned]` **As** a TOOLDEV, **I want** to export the plugin review and backlog to Excel/PDF/Word/CSV/Markdown/HTML/JSON, **so that** I can share findings.
  - **AC:** Export runs off the UI thread with progress and cancellation; sensitive/config values masked; AI sections labelled advisory.

## Definition of Done
- Follows suite conventions; deterministic analysis works with AI off; AI opt-in, redacted, consented, audited; export formats: Excel, PDF, Word, CSV, Markdown, HTML, JSON.
- Testing skeleton under testing/AiPluginReviewer/ when implementation starts.
