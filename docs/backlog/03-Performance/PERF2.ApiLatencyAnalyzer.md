# API Latency Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 2. Not in pack file.
> **Suggested tag:** `PERF2` · **Suggested project:** `XrmToolSuite.ApiLatencyAnalyzer`
> **Overlaps:** None — this is the only suite tool that actively measures live Dataverse round-trip latency. FetchXML/View analyzers estimate cost statically; this one times real requests.
> **Value/priority (my read):** Medium — high diagnostic value for "is it the API, the query, or the network?", but write-mode testing adds safety burden and results vary by environment load.

## Notes
- This is an active measurement tool, not a metadata reader: it issues timed `Retrieve`, `RetrieveMultiple`, `Create`, `Update`, `Delete`, `Execute`, FetchXML, and QueryExpression requests and records wall-clock latency.
- **Safety is central:** read-only mode is the default. Create/Update/Delete latency tests require an explicit opt-in confirmation dialog stating scope and record count, must run against a designated test table, and must auto-clean every record they create (tracked IDs deleted in a `finally`).
- Measure min/max/average/median/P95 across repeated iterations to smooth out jitter; a single call is not a measurement.
- Baseline persistence: save a baseline locally (settings/JSON) and compare a later scan to it to spot regressions — never persist credentials.
- Comparison dimensions: selected columns vs `ColumnSet(true)`, with vs without related entities, per-table.
- Shared-core reuse: `RunAsync`/`WorkAsync`, progress + cancellation (long test batches must be cancellable), settings round-trip, shared export module.
- Feasibility caveat: this measures client-observed latency (network + server), not server-side execution time in isolation; findings must say so and distinguish patterns heuristically, not claim precise server timings.

---

## EPIC-PERF2 — Measure Dataverse API latency to localize performance problems
> **As** a PERF engineer, **I want** to run safe, repeatable latency tests across operations and tables, **so that** I can tell whether slowness comes from APIs, query design, payload size, or the network.

**Outcome:** min/max/avg/median/P95 latency per operation, a baseline-vs-current comparison, slow-operation findings with recommendations, and exportable reports — with writes off by default and test records auto-cleaned.

---

## FEAT-PERF2-1 — Test configuration `[Planned]`
- **US-PERF2.1.1** `[Planned]` **As** a PERF engineer, **I want** to configure which operations, tables, and iteration counts to test, **so that** I control scope and cost.
  - **AC:** A config panel drives operation/table/iteration selection; configuration persists via settings.
- **US-PERF2.1.2** `[Planned]` **As** a PERF engineer, **I want** read-only mode selected by default, **so that** I can never accidentally write to production.
  - **AC:** On load, only read operations (Retrieve/RetrieveMultiple/FetchXML/QueryExpression/Execute) are enabled; write ops are disabled until explicitly opted in.

## FEAT-PERF2-2 — Read-operation latency tests `[Planned]`
- **US-PERF2.2.1** `[Planned]` **As** a PERF engineer, **I want** timed Retrieve and RetrieveMultiple tests, **so that** I can baseline core read latency.
  - **AC:** Each test runs N iterations off the UI thread with progress/cancellation and records per-call latency.
- **US-PERF2.2.2** `[Planned]` **As** a PERF engineer, **I want** FetchXML and QueryExpression latency tested, **so that** I can compare query styles.
  - **AC:** Both query paths are timed against the same table for a like-for-like comparison.
- **US-PERF2.2.3** `[Planned]` **As** a PERF engineer, **I want** selected-columns vs all-columns and with vs without related entities compared, **so that** I can quantify payload cost.
  - **AC:** Results grid shows paired latencies for each comparison dimension.

## FEAT-PERF2-3 — Safe write-operation tests `[Planned]`
- **US-PERF2.3.1** `[Planned]` **As** a PERF engineer, **I want** Create/Update/Delete latency tested only after an explicit confirmation dialog, **so that** writes never happen by surprise.
  - **AC:** Enabling write tests shows a confirmation dialog stating the target test table and record count before any write; declining aborts.
- **US-PERF2.3.2** `[Planned]` **As** a PERF engineer, **I want** every test record auto-cleaned, **so that** the environment is left as found.
  - **AC:** Created record IDs are tracked and deleted in a `finally`; the summary reports created vs cleaned counts.

## FEAT-PERF2-4 — Latency metrics & slow-op detection `[Planned]`
- **US-PERF2.4.1** `[Planned]` **As** a PERF engineer, **I want** min/max/average/median/P95 per operation, **so that** I see distribution, not just an average.
  - **AC:** The dashboard shows all five statistics per operation/table.
- **US-PERF2.4.2** `[Planned]` **As** a PERF engineer, **I want** slow operations detected and network-vs-server patterns hinted, **so that** I can localize the bottleneck.
  - **AC:** Operations over a configurable threshold are flagged; findings label likely network vs server-side patterns heuristically and disclaim precision.

## FEAT-PERF2-5 — Baseline, comparison & export `[Planned]`
- **US-PERF2.5.1** `[Planned]` **As** a PERF engineer, **I want** to save a baseline and compare the current scan to it, **so that** I can detect regressions over time.
  - **AC:** Baseline saves locally (no credentials); a comparison view shows deltas per operation.
- **US-PERF2.5.2** `[Planned]` **As** a MGR, **I want** Excel/PDF/JSON/HTML/CSV latency reports, **so that** I can share findings.
  - **AC:** All export formats come from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; read-only default; any write-mode test gated behind explicit opt-in confirmation and auto-cleaned; export formats as listed.
- All Dataverse calls off the UI thread via `RunAsync`; long test runs cancellable with progress; settings round-trip (baseline persisted, no credentials).
- Testing skeleton under `testing/ApiLatencyAnalyzer/` when implementation starts; SDK-free statistics (percentiles/median) covered by `testing/UnitTests`.
