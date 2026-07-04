# AI Performance Advisor — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 9 (AI Assistants), item 4. Not in pack file.
> **Suggested tag:** `AI4` · **Suggested project:** `XrmToolSuite.AiPerformanceAdvisor`
> **Overlaps:** **AI Solution Reviewer (shipped)** — reuse AI client, session-only key, consent-preview, redaction, offline-fallback. Consumes the **Performance-track** deterministic analyzers: Plugin Performance Profiler (PERF1), API Latency Analyzer (PERF2), FetchXML Performance Analyzer (PERF3), View Performance Analyzer (PERF4), Dashboard Performance Checker (PERF5), PCF Performance Inspector (PERF6), Business Rule Performance Analyzer (PERF7), JavaScript Performance Analyzer (PERF8), Dataverse Storage Optimizer (PERF9).
> **Value/priority (my read):** High — a unified advisor across the PERF track is the natural roll-up; deterministic detectors already exist, AI adds the optimisation sequence and backlog.

## Notes
- Deterministic first: consume/execute the PERF-track analyzers for form/view/dashboard complexity, view FetchXML, plugin registrations and trace logs, JavaScript web resources, PCF usage, business rules, flow complexity where available, and storage/large tables. Detectors: heavy forms/views/dashboards, broad FetchXML, plugins without filtering attributes, synchronous long-running plugin risk, deprecated JS APIs, excessive web resources.
- Produce a deterministic performance score and findings grid with severity — fully available with AI off.
- AI is opt-in and clearly labelled advisory: explain likely impact, recommend an optimisation sequence, suggest patterns, generate developer tasks and manager summary, recommend before/after validation metrics — it never changes any component.
- Reuse AI Solution Reviewer's AI client: session-only API key (never persisted), consent preview of the exact redacted payload before any send, GUIDs/URLs/user names/emails masked, audit trail, offline fallback to deterministic report on failure. Prompts/system-instructions configurable.
- Read-only tool. Reads via `Service.RetrieveAll` off the UI thread through `RunAsync`; progress + cancellation; settings round-trip. Keep the performance-rules aggregation UI-free and unit-testable.

---

## EPIC-AI4 — Turn multi-layer performance findings into a prioritised optimisation plan
> **As** a PERF lead / DEVOPS, **I want** deterministic performance findings with an optional AI layer that sequences optimisations and drafts tasks, **so that** I can improve responsiveness without manually correlating every layer.

**Outcome:** a performance score, findings across forms/views/dashboards/FetchXML/plugins/JS/PCF/storage, and (opt-in) an AI optimisation sequence, developer tasks and manager summary — deterministic findings stand alone with AI off.

---

## FEAT-AI4-1 — Deterministic rules-first findings `[Planned]`
- **US-AI4.1.1** `[Planned]` **As** a DEVOPS, **I want** the full performance analysis and score without any AI, **so that** the tool is useful and safe with AI disabled.
  - **AC:** Complete deterministic findings and score available offline; AI toggled off by default.
- **US-AI4.1.2** `[Planned]` **As** a PERF lead, **I want** detection of heavy forms/views/dashboards, broad FetchXML, plugins without filtering attributes, synchronous long-running plugin risk, deprecated JS APIs and excessive web resources, **so that** the real bottlenecks surface deterministically.
  - **AC:** Reads via `RetrieveAll` off the UI thread with progress and cancellation; each finding carries severity and evidence.

## FEAT-AI4-2 — Performance dashboard & scoring `[Planned]`
- **US-AI4.2.1** `[Planned]` **As** an MGR, **I want** a performance score with bottleneck-category cards and a findings grid, **so that** I can see where slowness concentrates.
  - **AC:** Score deterministic and reproducible with AI off.

## FEAT-AI4-3 — AI bottleneck summary & optimisation sequence (opt-in) `[Planned]`
- **US-AI4.3.1** `[Planned]` **As** a PERF lead, **I want** an AI summary of bottlenecks with likely impact and a recommended optimisation sequence, **so that** I know what to fix first.
  - **AC:** Consent preview of the exact redacted payload before send; session-only key; GUIDs/URLs/emails/names masked; labelled advisory; audit trail; offline fallback.
- **US-AI4.3.2** `[Planned]` **As** a DEVOPS, **I want** AI-suggested optimisation patterns and before/after validation metrics, **so that** I can verify improvements.

## FEAT-AI4-4 — AI backlog generation (opt-in) `[Planned]`
- **US-AI4.4.1** `[Planned]` **As** a DEVOPS, **I want** AI to generate a performance improvement backlog on an effort/value matrix with quick wins prioritised, **so that** I can schedule work.
  - **AC:** Backlog items trace to specific deterministic findings; advisory only; offline fallback yields the deterministic list.
- **US-AI4.4.2** `[Planned]` **As** an MGR, **I want** an AI manager summary, **so that** I can report expected gains.

## FEAT-AI4-5 — Configurable prompt templates `[Planned]`
- **US-AI4.5.1** `[Planned]` **As** a TOOLDEV, **I want** editable AI prompt/system-instruction templates, **so that** advice matches our platform standards.
  - **AC:** Templates round-trip via settings as POCOs; no key/connection details persisted.

## FEAT-AI4-6 — Export `[Planned]`
- **US-AI4.6.1** `[Planned]` **As** a PERF lead, **I want** to export the advisor report and backlog to Excel/PDF/Word/CSV/Markdown/HTML/JSON, **so that** I can share and track it.
  - **AC:** Export runs off the UI thread with progress and cancellation; sensitive values masked; AI sections labelled advisory.

## Definition of Done
- Follows suite conventions; deterministic analysis works with AI off; AI opt-in, redacted, consented, audited; export formats: Excel, PDF, Word, CSV, Markdown, HTML, JSON.
- Testing skeleton under testing/AiPerformanceAdvisor/ when implementation starts.
