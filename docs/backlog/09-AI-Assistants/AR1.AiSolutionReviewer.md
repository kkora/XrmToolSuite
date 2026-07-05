# AI Solution Reviewer - User Stories

> **Status:** DONE (shipped tool) — filed under AI Assistants. Kept its own area tag and story IDs.

Area tag: `AR`. See the [index](../../README.md) for personas, ID scheme, and status legend.

---

## EPIC-AR - AI-assisted architecture review of a Dataverse solution

> **As** an architect, **I want** the tool to gather solution facts and have an AI produce architecture
> recommendations, a modernization plan, a prioritized backlog, and a sprint plan, **so that** I get a
> professional review I can take to an architecture board — with an offline fallback when no AI is available.

**Outcome:** structured observations across plugins/JavaScript/automation/ALM/governance, a concern
score, and an AI-authored (or deterministic offline) review exported to Word/PDF/HTML/Markdown/JSON.

---

## FEAT-AR-0 - Scaffold & shared wiring `[Done]`

- **US-AR-0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background execution via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Tool appears in XTB, connects, runs off-thread; no template leftovers; MEF metadata incl. both image keys.

## FEAT-AR-1 - Fact collection `[Done]`

- **US-AR-1.1** `[Done]` **As** an architect, **I want** collectors that gather facts across plugins,
  JavaScript, automation, ALM, and governance, **so that** the review is grounded in the real solution.
  - **AC:** Collectors implement the shared `IAnalyzer<ReviewContext>`, are solution-scoped, UI-free, and degrade failures to informational findings.
  - **AC:** Plugin (sync/no-filter, footprint), Script (deprecated APIs, volume), Automation (classic workflows, sprawl), ALM/Governance (unmanaged, default prefix, versioning).

## FEAT-AR-2 - Concern score & AI review `[Done]`

- **US-AR-2** `[Done]` **As** an architect, **I want** a concern score and per-area metrics, **so that**
  I can see at a glance where the risk sits.
  - **AC:** Weighted severities produce a 0–100 concern score and a band. *(TC-AR-REPORT-01..02)*
- **US-AR-3** `[Done]` **As** an architect, **I want** an AI-authored review (executive summary,
  recommendations, modernization, refactoring, prioritized backlog, sprint plan), **so that** I get an
  actionable plan.
  - **AC:** The reviewer supplies an architecture-review system prompt covering all six sections. *(TC-AR-REPORT-04)*
  - **AC:** AI is opt-in behind a session-only key and a payload-preview consent dialog; the offline templated generator is the no-key fallback; the key is never persisted.

## FEAT-AR-3 - Export `[Done]`

- **US-AR-4** `[Done]` **As** an architect, **I want** to export the review to Word/PDF/HTML/Markdown/JSON,
  **so that** I can circulate it for sign-off.
  - **AC:** A Word (.docx) exporter (OpenXML, no extra dependency) plus the shared PDF/HTML/Markdown/JSON exporters, all embedding the AI narrative.

---

## Definition of Done (tool-level)

- Every Dataverse call runs off the UI thread via `RunAsync` (read-only tool — no destructive ops).
- Settings round-trip; the API key is never persisted; only anonymized observations are sent to the AI.
- nuspec id/version/description/tags correct; the Word/Excel/PDF dependency chain ships in the Plugins root.
- SDK-free report projection is covered by `testing/UnitTests` (`ReviewReportTests`); collectors, Word export, and live AI are manual-tested.
