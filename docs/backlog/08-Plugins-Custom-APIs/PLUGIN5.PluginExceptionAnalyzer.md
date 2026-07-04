# Plugin Exception Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 8 (Plugins & Custom APIs), item 5. Not in pack file.
> **Suggested tag:** `PLUGIN5` · **Suggested project:** `XrmToolSuite.PluginExceptionAnalyzer`
> **Overlaps:** Plugin Performance Profiler reads `plugintracelog` for durations and surfaces trace-log exceptions as a reliability signal; this tool is the dedicated **exception** analyzer — parsing, grouping, root-cause classification, baseline/post-deployment comparison, and a remediation checklist. Reuse the Profiler's trace-log retrieval/paging helper; do not duplicate its performance score. Deployment Risk Analyzer is registration-focused, not log-focused — no overlap.
> **Value/priority (my read):** Medium — high value when trace logging is enabled, but **entirely dependent on `plugintracelog` being turned on and retained**, which many orgs disable; the tool must be honest when there's nothing to analyze.

## Notes
- Primary data source: `plugintracelog` (messagename, primaryentity, exceptiondetails, messageblock, performanceexecutionduration, depth, correlationid, createdon, typename, pluginstepid, operationtype), joined to `plugintype`/`pluginassembly`/`sdkmessageprocessingstep` for context.
- **Hard feasibility caveat:** `plugintracelog` only populates when the org's plugin trace log setting is All (or Exception) and rows are retention-limited (purged over time). If logging is off or empty, the tool must state that clearly and offer to analyze whatever window exists — never imply zero exceptions means zero failures.
- Exception parsing is heuristic (regex/keyword over `exceptiondetails`/`messageblock`) to extract exception type, error code, and stack-trace signature; classification maps signatures to categories (Configuration, Security/permission, Data validation, Null reference, Timeout/performance, Recursion/depth, External dependency, Missing metadata, Serialization, Unknown).
- Secret hygiene: trace text can contain connection strings, tokens, or PII — **redact secrets/PII before display or export**; never surface secure-config values.
- Read-only tool — reads trace logs only, never writes or deletes. No destructive ops (does not purge logs).
- Shared-core reuse: `Service.RetrieveAll` (paged trace-log retrieval), `BatchExecutor`, progress/cancellation, settings round-trip (filters + baseline), shared reporting/export module; the parser/classifier stays UI-free for console/CI lift.

---

## EPIC-PLUGIN5 — Consolidate and classify plugin exceptions from trace logs into actionable root causes
> **As** an ADM, **I want** a single view of plugin exceptions with frequency, impacted components, and root-cause classification, **so that** I can stop investigating failures one trace log at a time.

**Outcome:** a filterable exception inventory, grouped error summaries, top-failing-plugins and baseline-delta views, root-cause categories with a remediation checklist, and Excel/PDF/JSON/CSV/HTML exports — from a live connection with no hand-written queries, degrading cleanly when trace logging is off.

---

## FEAT-PLUGIN5-1 — Trace-log retrieval & filtering `[Planned]`
- **US-PLUGIN5.1.1** `[Planned]` **As** an ADM, **I want** plugin trace logs retrieved where enabled and permitted, **so that** I have the exception data to analyze.
  - **AC:** Logs load via paged `Service.RetrieveAll` off the UI thread with progress and cancellation; when logging is off/empty the tool states so instead of showing an empty pass.
- **US-PLUGIN5.1.2** `[Planned]` **As** an ADM, **I want** to filter by date range, assembly, plugin type, table, message, operation, correlation ID, exception type, user, and severity, **so that** I can scope an investigation.
  - **AC:** All listed filters apply to the exception grid; filter state persists via settings.
- **US-PLUGIN5.1.3** `[Planned]` **As** a SEC reviewer, **I want** secrets and PII in trace text redacted before display, **so that** the analyzer never leaks sensitive data.
  - **AC:** Connection strings/tokens/known-PII patterns are masked in grid, details panel, and every export.

## FEAT-PLUGIN5-2 — Parsing & grouping `[Planned]`
- **US-PLUGIN5.2.1** `[Planned]` **As** a TOOLDEV, **I want** exception details parsed into type, error code, and stack-trace signature, **so that** raw trace text becomes structured.
  - **AC:** Each log yields exception type + code + normalized stack signature; unparseable entries fall back to Unknown without failing.
- **US-PLUGIN5.2.2** `[Planned]` **As** an ADM, **I want** exceptions grouped by plugin, table, message, error code, exception type, and stack-trace pattern, **so that** I see recurring failures, not noise.
  - **AC:** A grouped error summary shows each group with count, first/last seen, and impacted components.
- **US-PLUGIN5.2.3** `[Planned]` **As** a MGR, **I want** the top failing plugins and most frequent exceptions ranked, **so that** I can prioritize fixes.
  - **AC:** A top-failing panel ranks by occurrence count with drill-down to the group.

## FEAT-PLUGIN5-3 — Baseline & deployment comparison `[Planned]`
- **US-PLUGIN5.3.1** `[Planned]` **As** a DEVOPS, **I want** new exceptions since a saved baseline highlighted, **so that** I can tell regressions from known issues.
  - **AC:** Groups absent from the baseline are marked "new"; baseline persists via settings.
- **US-PLUGIN5.3.2** `[Planned]` **As** a DEVOPS, **I want** exceptions after a chosen deployment date surfaced, **so that** I can see what a release broke.
  - **AC:** Selecting a cutoff date filters to post-deployment exceptions and shows counts before/after.

## FEAT-PLUGIN5-4 — Root-cause classification `[Planned]`
- **US-PLUGIN5.4.1** `[Planned]` **As** an ADM, **I want** each exception classified into a root-cause category, **so that** I know what kind of problem it is.
  - **AC:** Each group maps to one of Configuration/Security/Data validation/Null reference/Timeout/Recursion/External dependency/Missing metadata/Serialization/Unknown.
- **US-PLUGIN5.4.2** `[Planned]` **As** a TOOLDEV, **I want** timeout, permission, null-reference, recursion/depth, missing-config, and external-API failures specifically detected, **so that** common causes are called out by name.
  - **AC:** Signature rules tag these categories explicitly with the matched indicator shown in the details panel.

## FEAT-PLUGIN5-5 — Remediation & export `[Planned]`
- **US-PLUGIN5.5.1** `[Planned]` **As** an ADM, **I want** a stack-trace/details panel and a root-cause panel per group, **so that** I can read the full context and likely cause together.
  - **AC:** Selecting a group shows redacted stack/details plus its category and matched indicators.
- **US-PLUGIN5.5.2** `[Planned]` **As** a TOOLDEV, **I want** a remediation checklist per category, **so that** I get concrete next actions (add env var, fix privilege, guard depth, handle null, add retry).
  - **AC:** Each category renders a checklist of category-specific remediation steps.
- **US-PLUGIN5.5.3** `[Planned]` **As** a MGR, **I want** to export the exception analysis to Excel, PDF, JSON, CSV, and HTML, **so that** I can share the findings.
  - **AC:** All listed formats export from the shared reporting module with secrets/PII redacted.

## Definition of Done
- Follows suite conventions; read-only default (reads trace logs only, never purges); export formats as listed (Excel/PDF/JSON/CSV/HTML).
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; parser/classifier UI-free; secrets/PII redacted everywhere; secure-config values never exposed.
- Trace-logging-off / empty-window state is reported plainly — never a false "no exceptions" pass.
- Testing skeleton under `testing/PluginExceptionAnalyzer/` when implementation starts; SDK-free parser/classifier/grouping logic covered by `testing/UnitTests`.
