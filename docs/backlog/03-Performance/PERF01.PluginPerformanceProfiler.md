# Plugin Performance Profiler — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 1. Related pack idea #3 'Plugin Execution Analyzer'.
> **Suggested tag:** `PERF01` · **Suggested project:** `XrmToolSuite.PluginPerformanceProfiler`
> **Overlaps:** Deployment Risk Analyzer and Technical Debt Analyzer already flag sync `Update` steps without filtering attributes and dead/disabled steps — this tool goes deeper on execution timing, trace-log analysis, and a dedicated plugin performance score. Reuse those step-inventory queries; do not duplicate the debt score.
> **Value/priority (my read):** High — plugin bottlenecks are a top cause of slow forms, timeouts, and deployment failures, and no shipped tool profiles execution duration.

## Notes
- Data sources: `pluginassembly`, `plugintype`, `sdkmessageprocessingstep` (stage/mode/rank/filteringattributes/impersonatinguserid), `sdkmessageprocessingstepimage` (pre/post images), `sdkmessage`, and `plugintracelog` for durations/exceptions.
- Feasibility caveat: `plugintracelog` only exists when trace logging is set to All/Exception and is retention-limited — durations may be sparse or absent. Degrade to configuration-only analysis and say so in findings rather than throwing.
- `performanceexecutionduration` / `depth` on trace logs give avg/max duration and recursion depth; treat missing telemetry as an informational finding.
- Read-only tool — inspects registration + logs, never modifies steps. No destructive ops.
- Shared-core reuse: `Service.RetrieveAll`, `BatchExecutor`, progress/cancellation, settings round-trip, and the shared reporting/export module.
- Analyzers stay UI-free (operate on `IOrganizationService`) so they can be lifted into a console/CI wrapper later.

---

## EPIC-PERF01 — Profile Dataverse plugin behavior to expose performance bottlenecks and reliability risks
> **As** a PERF engineer, **I want** a single tool that inventories plugin registration and analyzes trace logs, **so that** I can find slow, misconfigured, or failing plugins before they degrade the app.

**Outcome:** a plugin performance score, a ranked slow-plugin list, categorized risk findings with severities (Critical/High/Medium/Low/Info), and Excel/PDF/HTML/JSON exports — from a live connection with no hand-written queries.

---

## FEAT-PERF01-1 — Plugin inventory `[Planned]`
- **US-PERF01.1.1** `[Planned]` **As** a PERF engineer, **I want** to list plugin assemblies, types, and SDK message processing steps, **so that** I have a full registration inventory.
  - **AC:** Assemblies, types, and steps load via `Service.RetrieveAll` off the UI thread with progress and cancellation.
- **US-PERF01.1.2** `[Planned]` **As** a DEVOPS, **I want** each step's stage, mode, rank, filtering attributes, images, and impersonation shown, **so that** I can audit configuration at a glance.
  - **AC:** A step grid shows stage/mode/rank/message/entity/filteringattributes/image-count/impersonation columns.
- **US-PERF01.1.3** `[Planned]` **As** an ADM, **I want** an assembly selector that filters the step grid, **so that** I can focus on one assembly.
  - **AC:** Selecting an assembly filters types and steps; selection persists via settings.

## FEAT-PERF01-2 — Trace log analysis `[Planned]`
- **US-PERF01.2.1** `[Planned]` **As** a PERF engineer, **I want** the tool to read `plugintracelog` where available and compute average/max execution duration per step, **so that** I can quantify runtime cost.
  - **AC:** Durations aggregate per plugin type/step; when no trace logs exist the panel states telemetry is unavailable rather than showing zeros.
- **US-PERF01.2.2** `[Planned]` **As** a PERF engineer, **I want** long-running plugins ranked in a slow-plugin list, **so that** I can target the worst offenders first.
  - **AC:** A slow-plugin panel sorts by average duration descending with configurable threshold.
- **US-PERF01.2.3** `[Planned]` **As** an ADM, **I want** plugin failures surfaced from trace-log exceptions, **so that** I can see reliability hot spots.
  - **AC:** Steps with recorded exceptions are flagged High with exception message and count.

## FEAT-PERF01-3 — Configuration risk rules `[Planned]`
- **US-PERF01.3.1** `[Planned]` **As** a PERF engineer, **I want** sync steps that run too long flagged High/Critical, **so that** I know which to move to async.
  - **AC:** Synchronous mode + duration over threshold → High; well over threshold → Critical.
- **US-PERF01.3.2** `[Planned]` **As** a DEVOPS, **I want** `Update` steps with no filtering attributes flagged High, **so that** I stop plugins firing on every field change.
  - **AC:** Update message + empty filteringattributes → High finding with the affected entity.
- **US-PERF01.3.3** `[Planned]` **As** a DEVOPS, **I want** excessive pre/post image usage and assemblies with many steps flagged, **so that** I can spot heavy registrations.
  - **AC:** Large image usage → Medium/High; assemblies whose step count exceeds a threshold → Medium.

## FEAT-PERF01-4 — Recursion & duplication risks `[Planned]`
- **US-PERF01.4.1** `[Planned]` **As** a PERF engineer, **I want** high-depth executions flagged Critical, **so that** runaway recursion is caught.
  - **AC:** Trace-log depth greater than a configurable expected value → Critical finding.
- **US-PERF01.4.2** `[Planned]` **As** a DEVOPS, **I want** duplicate steps (same message/entity/stage) detected, **so that** I can remove redundant registrations.
  - **AC:** Steps matching on message+entity+stage+mode are grouped and flagged Medium/High.

## FEAT-PERF01-5 — Score, dashboard & export `[Planned]`
- **US-PERF01.5.1** `[Planned]` **As** a MGR, **I want** a plugin performance score and a performance dashboard, **so that** I can communicate risk at a glance.
  - **AC:** Weighted severities produce a 0–100 score with a Low/Medium/High band shown on score cards.
- **US-PERF01.5.2** `[Planned]` **As** a PERF engineer, **I want** a recommendation panel per finding, **so that** I get concrete next actions (make async, add filters, reduce images).
  - **AC:** Each finding carries a plain-language recommendation.
- **US-PERF01.5.3** `[Planned]` **As** a MGR, **I want** Excel/PDF/JSON/HTML/CSV exports, **so that** I can share the profile.
  - **AC:** All export formats come from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; read-only default (no step changes); export formats as listed (Excel/PDF/JSON/HTML/CSV).
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; analyzers UI-free; trace-log absence degrades to informational, never throws.
- Testing skeleton under `testing/PluginPerformanceProfiler/` when implementation starts; SDK-free scoring covered by `testing/UnitTests`.
