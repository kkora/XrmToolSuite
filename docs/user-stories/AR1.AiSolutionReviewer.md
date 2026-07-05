# AI Solution Reviewer — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/09-AI-Assistants/AR1.AiSolutionReviewer.md`](../backlog/09-AI-Assistants/AR1.AiSolutionReviewer.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.AiSolutionReviewer` · **Area tag:** `— (pre-tagging; AI track)`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (live AI HTTPS call / GDI-MigraDoc runtime / WinForms host) — pending manual sign-off.

AI-assisted architecture review of a Dataverse solution. Four UI-free collectors (plugins, JavaScript,
automation, ALM/governance) run solution-scoped over `IOrganizationService` and gather structured facts;
the suite `ScoreCalculator` projects them onto a shared `ReportModel` with a 0–100 concern score and band.
The AI layer turns the anonymized facts into an executive summary, architecture recommendations,
modernization guidance, refactoring suggestions, a prioritized backlog and a sprint plan. AI is **opt-in**:
the tool defaults to a deterministic offline templated generator, and only calls a provider
(Anthropic / OpenAI / Google, via raw HTTPS `HttpClient`) when a session-only API key is supplied
(entered in the AI-settings dialog or read from `ANTHROPIC_API_KEY`) and the user approves a payload-preview
consent dialog. Only the anonymized `SummaryPayload` (finding metadata + headline metrics, no record data,
credentials or environment names) is sent; the key is never persisted. Read-only (no destructive ops).
Exports to Word (.docx), PDF, HTML, Markdown and JSON. The SDK-free report projection/score and the four
collectors are unit-tested; the live AI call, exports and UI are manual-tested.

---

## EPIC-AR — AI-assisted architecture review of a Dataverse solution `[Implemented]`
> **As** an **architect**, **I want** the tool to gather solution facts and have an AI produce architecture
> recommendations, a modernization plan, a prioritized backlog and a sprint plan, **so that** I get a
> professional review I can take to an architecture board — with an offline fallback when no AI is available.

**Outcome:** structured observations across plugins / JavaScript / automation / ALM / governance, a concern
score and band, and an AI-authored (or deterministic offline) review exported to Word / PDF / HTML /
Markdown / JSON.

---

## FEAT-AR-0 — Scaffold & shared wiring `[Implemented]`
- **US-AR-0.1** `[Implemented]` The tool loads in XrmToolBox with connection, settings and background
  execution via `BaseToolControl`, so feature work starts from a working shell.
  - **AC:** `AiSolutionReviewerControl : BaseToolControl, IGitHubPlugin, IHelpPlugin`; a right-aligned
    Help button via `CreateHelpButton`; MEF metadata incl. both `SmallImageBase64`/`BigImageBase64` keys;
    no "Template Tool" leftovers. Solutions load off the UI thread via `RunAsync`/`RetrieveAll`;
    `UpdateConnection` calls base then clears `MetadataCache` and the solution list. *(Manual — XrmToolBox host; load confirmed by `TC-AR-M-01` / UI smoke test.)*

## FEAT-AR-1 — Fact collection `[Implemented]`
- **US-AR-1.1** `[Implemented]` Collectors gather facts across plugins, JavaScript, automation and
  ALM/governance so the review is grounded in the real solution.
  - **AC:** Each collector implements the shared `IAnalyzer<ReviewContext>`, is solution-scoped
    (`ctx.QuerySolutionRows`), UI-free, and degrades query failures to informational findings
    (`ctx.SafeRetrieve`); `AnalyzerRunner` reports progress and honours cancellation.
  - **AC:** `PluginReviewCollector` flags synchronous steps with no filtering attributes (mode 0), a heavy
    step footprint (≥20), and no-steps as Info; `ScriptReviewCollector` flags deprecated client APIs
    (`Xrm.Page`, `crmForm`, `/2011/Organization.svc`, `getServerUrl`) in JScript web resources and heavy
    scripting (≥15); `AutomationReviewCollector` flags classic workflows (category 0) and sprawl (≥25);
    `AlmGovernanceReviewCollector` flags unmanaged solutions, the default `new_` publisher prefix, and
    records version as Info. **Automated** — `TC-AR-COL-01..09` (`CollectorTests` over a fake `IOrganizationService`).

## FEAT-AR-2 — Concern score & AI review `[Implemented]`
- **US-AR-2** `[Implemented]` A concern score and per-area metrics show at a glance where the risk sits.
  - **AC:** `ReviewReport.Build` scores findings with `ScoreCalculator.RiskDefault` into a 0–100 concern
    score and a band, adds an Observations total plus a per-category metric row, and the grid/headline
    labels colour by band. **Automated** — `TC-AR-REPORT-01..03`.
- **US-AR-3** `[Implemented*]` An AI-authored review (executive summary, architecture recommendations,
  modernization, refactoring, prioritized backlog, sprint plan) gives an actionable plan; a deterministic
  offline review is the no-key fallback.
  - **AC:** `ReviewReport.AiSystemPrompt` steers a principal-architect review covering all six sections in
    plain text ending in a `RECOMMENDATION:` line; the AI narrative lands in `ReportModel.AiSummary`. The
    prompt-section coverage is **Automated** — `TC-AR-REPORT-04`.
  - **AC:** AI is opt-in behind a session-only key (AI-settings dialog or `ANTHROPIC_API_KEY`) and a
    payload-preview consent dialog naming the provider/host/model; without a key/consent the tool uses
    `TemplatedSummaryGenerator` and labels the result offline; the key is held in `_sessionApiKey` only and
    never written to settings. `AiSummaryGenerator` posts only the anonymized `SummaryPayload` over HTTPS to
    Anthropic / OpenAI / Google and falls back to the offline template on any error. *(Live HTTPS call — manual: `TC-AR-M-03/04/05`.)*

## FEAT-AR-3 — Export `[Implemented]`
- **US-AR-4** `[Implemented]` Export the review to Word / PDF / HTML / Markdown / JSON to circulate it for
  sign-off.
  - **AC:** Word (.docx) via `WordReportExporter` (OpenXML), PDF via the sanctioned MigraDoc/PdfSharp-GDI
    chain (`PdfReportExporter`), HTML via `HtmlDashboardBuilder`, Markdown via `FixChecklistGenerator`, and
    JSON via `JsonReportExporter`, all embedding the AI/offline narrative (`ReportModel.AiSummary`). Export
    runs from a `SaveFileDialog` with an offer to open the file. JSON is **Automated** (shared exporter tests);
    **Word/PDF/HTML/Markdown `[Implemented*]`** — manual (`TC-AR-M-06/07`).

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress + cancellation, Help button, both image metadata keys).
- Read-only tool — no destructive ops; collectors and the report projection stay UI-free and SDK-free and degrade missing metadata to informational findings.
- AI is opt-in: session-only key (never persisted), a payload-preview consent dialog, and only the anonymized `SummaryPayload` sent; a deterministic offline generator is the default fallback.
- Export formats: Word, PDF, HTML, Markdown, JSON, each embedding the review narrative. — **Done.**
- Testing under `testing/AiSolutionReviewer/`; SDK-free report projection covered by `testing/UnitTests` (`ReviewReportTests`, `TC-AR-REPORT-01..04`) and the collectors by `testing/CollectorTests` (`TC-AR-COL-01..09`). — **Done** (live AI, Word/PDF/HTML/MD export, and the WinForms UI pending manual sign-off — `TC-AR-M-01..07`).
