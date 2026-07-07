# Plugin Recursion Detector — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 8 (Plugins & Custom APIs), item 3. Related pack idea #3 'Plugin Execution Analyzer'.
> **Suggested tag:** `PLUGIN03` · **Suggested project:** `XrmToolSuite.PluginRecursionDetector`
> **Overlaps:** Deployment Risk Analyzer flags update steps without filtering attributes and execution-rank conflicts; Plugin Performance Profiler reads trace-log depth. This tool is the dedicated **recursion/loop** analyzer — self-update detection, circular table-update chains, sync-chain analysis, and a recursion risk score. Reuse the shared step-inventory queries and any trace-log depth helper; do not re-implement the general debt/perf score.
> **Value/priority (my read):** Medium/High — recursion causes infinite loops, transaction failures, and throttling that are hard to spot by eye, and no shipped tool models plugin execution chains for loops.

## Notes
- Data sources: `sdkmessageprocessingstep` (message/stage/mode/rank/filteringattributes), `sdkmessage`, `sdkmessagefilter` (primaryobjecttypecode → table), `plugintype`, `sdkmessageprocessingstepimage`, and `plugintracelog` (`depth`, `correlationid`) for observed depth where enabled.
- Feasibility caveat: `plugintracelog` only exists when trace logging is All/Exception and is retention-limited — depth analysis may be sparse or absent. Degrade to **static registration analysis** (self-updates, chains, broad filters) and say so in findings rather than throwing.
- Circular-chain detection is a **static graph** over "step triggers on table T and updates table U" edges; a cycle back to a triggering table is the recursion signal. Trace-log depth confirms it at runtime where available.
- Read-only tool — analyzes registration + logs, never modifies steps. No destructive ops; recommendations describe safe re-registration but don't apply it.
- Shared-core reuse: `Service.RetrieveAll`, `BatchExecutor`, progress/cancellation, settings round-trip, shared reporting/export module; analyzers stay UI-free for console/CI lift.

---

## EPIC-PLUGIN03 — Detect plugin recursion and circular execution risk before it causes loops or failures
> **As** a TOOLDEV, **I want** a tool that models plugin execution chains and reads trace-log depth, **so that** I can find recursion, self-update loops, and cascade risks before they hit production.

**Outcome:** a recursion risk score, plugin-chain visualization, categorized findings with severities (Critical/High/Medium/Low/Info), safe-registration recommendations, and Excel/PDF/JSON/CSV/HTML exports — from a live connection with no hand-written queries.

---

## FEAT-PLUGIN03-1 — Step & chain analysis `[Planned]`
- **US-PLUGIN03.1.1** `[Planned]` **As** a TOOLDEV, **I want** SDK steps loaded and grouped by table, message, stage, and mode, **so that** I can see what runs on each table/message combination.
  - **AC:** Steps load via `Service.RetrieveAll` off the UI thread with progress and cancellation, grouped by table/message/stage/mode.
- **US-PLUGIN03.1.2** `[Planned]` **As** a TOOLDEV, **I want** multiple plugins on the same table/message surfaced, **so that** I can spot contention and chaining.
  - **AC:** Table/message combinations with more than one active step are listed with step count and modes.
- **US-PLUGIN03.1.3** `[Planned]` **As** an ARCH, **I want** a plugin chain viewer with a table/message filter, **so that** I can trace how one operation triggers others.
  - **AC:** A chain view shows trigger→update edges between tables; filter narrows to a chosen table/message; selection persists via settings.

## FEAT-PLUGIN03-2 — Static recursion rules `[Planned]`
- **US-PLUGIN03.2.1** `[Planned]` **As** a TOOLDEV, **I want** plugins that update the same table they trigger on flagged High, **so that** self-update loops are caught.
  - **AC:** A step whose logic/registration targets its own triggering table (self-update pattern) → High finding.
- **US-PLUGIN03.2.2** `[Planned]` **As** a DEVOPS, **I want** `Update` steps without filtering attributes and broad filter/image patterns flagged, **so that** over-broad triggers that amplify recursion are found.
  - **AC:** Update + empty filteringattributes → High; very broad filtering/image attribute sets → Medium.
- **US-PLUGIN03.2.3** `[Planned]` **As** an ARCH, **I want** multiple synchronous plugins on the same table/message flagged, **so that** sync chains that compound depth are visible.
  - **AC:** More than one sync step on the same table/message/stage → Medium/High.

## FEAT-PLUGIN03-3 — Circular & cascade detection `[Planned]`
- **US-PLUGIN03.3.1** `[Planned]` **As** an ARCH, **I want** circular table-update patterns detected, **so that** A→B→A loops are caught before deployment.
  - **AC:** A cycle in the static trigger/update graph → Critical finding naming the tables in the cycle.
- **US-PLUGIN03.3.2** `[Planned]` **As** a TOOLDEV, **I want** parent/child cascade recursion risks flagged, **so that** cascading operations that re-enter plugins are visible.
  - **AC:** Steps on tables linked by cascade relationships that also update related records → Medium/High.

## FEAT-PLUGIN03-4 — Trace-log depth analysis `[Planned]`
- **US-PLUGIN03.4.1** `[Planned]` **As** a TOOLDEV, **I want** observed execution depth read from `plugintracelog` where available, **so that** static risk is confirmed with real runtime depth.
  - **AC:** Depth per correlation aggregates per step; when no trace logs exist the panel states telemetry is unavailable rather than showing zeros.
- **US-PLUGIN03.4.2** `[Planned]` **As** a PERF engineer, **I want** high-depth executions flagged by threshold, **so that** runaway recursion is graded.
  - **AC:** Depth > 2 → High; depth > 5 → Critical, each with the observed max depth.

## FEAT-PLUGIN03-5 — Score, remediation & export `[Planned]`
- **US-PLUGIN03.5.1** `[Planned]` **As** a MGR, **I want** a recursion risk score on a recursion dashboard, **so that** I can gauge loop risk at a glance.
  - **AC:** Weighted severities produce a 0–100 score with a Low/Medium/High band on score cards.
- **US-PLUGIN03.5.2** `[Planned]` **As** a TOOLDEV, **I want** a suggested-remediation panel per finding, **so that** I get safe re-registration guidance (add filters, split sync/async, guard depth, break the cycle).
  - **AC:** Each finding carries a plain-language remediation; nothing is applied automatically.
- **US-PLUGIN03.5.3** `[Planned]` **As** a MGR, **I want** to export the recursion report to Excel, PDF, JSON, CSV, and HTML, **so that** I can share it.
  - **AC:** All listed formats export from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; read-only default (no step changes); export formats as listed (Excel/PDF/JSON/CSV/HTML).
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; analyzers UI-free; trace-log absence degrades to static analysis, never throws.
- Testing skeleton under `testing/PluginRecursionDetector/` when implementation starts; SDK-free chain/cycle/scoring logic covered by `testing/UnitTests`.
