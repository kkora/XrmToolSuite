# JavaScript Performance Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 8. Related pack idea #9 'JavaScript Analyzer' (same tool).
> **Suggested tag:** `PERF8` · **Suggested project:** `XrmToolSuite.JavaScriptPerformanceAnalyzer`
> **Overlaps:** Technical Debt Analyzer's Deprecated-APIs analyzer already flags `Xrm.Page`/`crmForm`/2011-endpoint in JS web resources — this tool goes far deeper (sync XHR, blocking alerts, repeated retrieves, form/event mapping, per-script score). Reuse TDA's web-resource retrieval helper; do not duplicate its debt score.
> **Value/priority (my read):** High — JavaScript directly blocks form load and is a frequent, fixable performance sink; static analysis is fully feasible offline.
## Notes
- Data sources: `webresource` where `webresourcetype = 3` (JScript) for code content (base64 `content`), plus `systemform` FormXML for `<events>`/`<Handler>` to map scripts to forms and OnLoad/OnChange/OnSave handlers.
- Static analysis rules (regex/AST-lite over code): `Xrm.Page` usage, synchronous `XMLHttpRequest`, excessive `console` logging, hardcoded GUIDs, hardcoded URLs, blocking `alert()`, repeated retrieve calls, unsupported DOM manipulation; plus script size.
- Form mapping: too many event handlers on form load is a key latency signal — count OnLoad handlers per form.
- Feasibility: static analysis is fully offline (no runtime) — this is the most self-contained PERF tool; keep the code analyzer UI-free so it's CI-liftable.
- Feasibility caveat: regex heuristics can false-positive (e.g., `Xrm.Page` in comments); report with code line context and label confidence.
- Read-only tool — reads web resources and forms only.
- Shared-core reuse: `RunAsync`/`RetrieveAll`, progress/cancellation, settings round-trip, shared export module.

---

## EPIC-PERF8 — Statically analyze form JavaScript for performance and deprecation risks
> **As** a DEVOPS/developer, **I want** to scan every JS web resource and its form usage for slow or deprecated patterns, **so that** I can find and refactor risky scripts quickly.

**Outcome:** a per-script performance score, a findings grid with code context, form/event mapping, refactoring recommendations, and exports — from static analysis with no runtime.

---

## FEAT-PERF8-1 — Web resource inventory `[Planned]`
- **US-PERF8.1.1** `[Planned]` **As** a developer, **I want** JS web resources listed with size, **so that** I can spot bloated scripts.
  - **AC:** JScript `webresource` rows load via `RetrieveAll` off the UI thread; grid shows name and decoded size.
- **US-PERF8.1.2** `[Planned]` **As** a developer, **I want** to search across code content, **so that** I can find specific patterns.
  - **AC:** A search box filters scripts by code content match.

## FEAT-PERF8-2 — Static code analysis rules `[Planned]`
- **US-PERF8.2.1** `[Planned]` **As** a developer, **I want** deprecated `Xrm.Page` and synchronous `XMLHttpRequest` flagged, **so that** I can modernize and unblock the UI.
  - **AC:** `Xrm.Page` → Medium/High; synchronous XHR → High; each finding shows the code line for context.
- **US-PERF8.2.2** `[Planned]` **As** a developer, **I want** blocking alerts, excessive console logging, and repeated retrieve calls flagged, **so that** I catch runtime slowness.
  - **AC:** `alert()` in form logic → High; console over threshold → Low; repeated retrieves in a handler → Medium.
- **US-PERF8.2.3** `[Planned]` **As** a developer, **I want** hardcoded GUIDs/URLs and unsupported DOM manipulation flagged, **so that** I catch fragility and unsupported patterns.
  - **AC:** Hardcoded GUID/URL → Medium; direct DOM manipulation of the form → Medium; findings labeled with confidence.

## FEAT-PERF8-3 — Form & event mapping `[Planned]`
- **US-PERF8.3.1** `[Planned]` **As** a PERF engineer, **I want** scripts mapped to the forms and events that call them, **so that** I know where a risky script actually runs.
  - **AC:** FormXML `<events>` are parsed; a form-usage panel and event-handler panel show script→form→event links.
- **US-PERF8.3.2** `[Planned]` **As** a PERF engineer, **I want** forms with too many OnLoad handlers flagged, **so that** I catch slow form loads.
  - **AC:** OnLoad handler count over a configurable threshold → Medium/High.

## FEAT-PERF8-4 — Score, recommendations & export `[Planned]`
- **US-PERF8.4.1** `[Planned]` **As** a MGR, **I want** a per-script performance score and dashboard, **so that** I can prioritize refactoring.
  - **AC:** A 0–100 score per script combines rule findings and size, summarized on a dashboard.
- **US-PERF8.4.2** `[Planned]` **As** a developer, **I want** refactoring recommendations and Excel/PDF/JSON/HTML/CSV exports, **so that** I can act and share.
  - **AC:** Each finding carries a refactoring recommendation; all export formats come from the shared reporting module.

## Definition of Done
- Follows suite conventions; read-only default; export formats as listed.
- Code analyzer is UI-free (CI-liftable) and operates on decoded strings; heuristic findings labeled with confidence and code context.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; settings round-trip.
- Testing skeleton under `testing/JavaScriptPerformanceAnalyzer/` when implementation starts; the static rule engine is heavily covered by `testing/UnitTests` (SDK-free).
