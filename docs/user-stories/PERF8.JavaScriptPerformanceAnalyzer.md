# JavaScript Performance Analyzer — User Stories

> **Status:** Active — implemented (see status tags below). Area tag `PERF8`.
> **Source:** ported from `docs/backlog/03-Performance/PERF8.JavaScriptPerformanceAnalyzer.md`.
> **Engine:** SDK-free static rule engine + FormXML event mapper in `src/Tools/XrmToolSuite.JavaScriptPerformanceAnalyzer/Analysis/` (`JsRules`, `JsModels`, `FormEventMap`); the live SDK collector (`JsCollector`) is manual-tested.
> **Feasibility note:** analysis is **fully offline** — no runtime is executed. Findings are **labeled heuristics** (regex/line scans may match comments or string literals); every regex finding carries a 1-based line number, the trimmed source line as context, and an explicit confidence note. Read-only tool.

---

## EPIC-PERF8 — Statically analyze form JavaScript for performance and deprecation risks

> **As** a DEVOPS/developer, **I want** to scan every JS web resource and its form usage for slow or deprecated patterns, **so that** I can find and refactor risky scripts quickly.

**Outcome:** a per-script performance score, a findings grid with code context, form/event mapping, refactoring recommendations, and exports — from static analysis with no runtime.

---

## FEAT-PERF8-1 — Web resource inventory

- **US-PERF8.1.1** `[Done]` **As** a developer, **I want** JS web resources listed with size, **so that** I can spot bloated scripts.
  - **AC:** JScript `webresource` rows (`webresourcetype = 3`) load via `RetrieveAll` off the UI thread; the grid shows name, decoded size, band, score and finding count. *(`JsCollector.Collect`; ranked grid `grdScripts`.)*
- **US-PERF8.1.2** `[Done]` **As** a developer, **I want** to search across code content, **so that** I can find specific patterns.
  - **AC:** A search box filters scripts by code-content match (case-insensitive); the last search persists via settings. *(`txtSearch` → `FilteredScripts`; `ToolSettings.LastSearch`.)*

## FEAT-PERF8-2 — Static code analysis rules

- **US-PERF8.2.1** `[Done]` **As** a developer, **I want** deprecated `Xrm.Page` and synchronous `XMLHttpRequest` flagged, **so that** I can modernize and unblock the UI.
  - **AC:** `Xrm.Page` → Medium (recommend `executionContext.getFormContext()`); synchronous XHR (`open(...,false)` or `async:false`) → High; each finding shows the 1-based line and trimmed context. *(`JsRules.Analyze`; covered by `JavaScriptPerformanceAnalyzerTests`.)*
- **US-PERF8.2.2** `[Done]` **As** a developer, **I want** blocking alerts, excessive console logging, and repeated retrieve calls flagged, **so that** I catch runtime slowness.
  - **AC:** `alert(` in form logic → High; `console.*` over the threshold (default 10) → Low; repeated `retrieve`/`retrieveMultiple`/`Xrm.WebApi` over the threshold (default 3) → Medium.
- **US-PERF8.2.3** `[Done]` **As** a developer, **I want** hardcoded GUIDs/URLs and unsupported DOM manipulation flagged, **so that** I catch fragility and unsupported patterns.
  - **AC:** Hardcoded GUID → Medium; hardcoded absolute URL (`https?://…`) → Medium; direct DOM access (`document.getElementById`/`querySelector`/`window.parent`) → Medium; every finding is labeled with its confidence note. Whole-line comments (`//…`) are skipped.

## FEAT-PERF8-3 — Form & event mapping

- **US-PERF8.3.1** `[Done]` **As** a PERF engineer, **I want** scripts mapped to the forms and events that call them, **so that** I know where a risky script actually runs.
  - **AC:** FormXML `<events>/<event>/<Handlers>/<Handler>` are parsed (`System.Xml.Linq`) into library → form → event (OnLoad/OnChange/OnSave) links; selecting a script shows its form/event usage panel. *(`FormEventMap.Map`; `JsCollector.CollectFormUsage`; `lstUsage`.)*
- **US-PERF8.3.2** `[Done]` **As** a PERF engineer, **I want** forms with too many OnLoad handlers flagged, **so that** I catch slow form loads.
  - **AC:** OnLoad handler count per form over a configurable threshold (default 5) → Medium finding, surfaced in the collector and the dashboard. *(`FormEventMap.OnLoadHandlerCount`; `JsCollector.LastFormFindings`.)*

## FEAT-PERF8-4 — Score, recommendations & export

- **US-PERF8.4.1** `[Done]` **As** a MGR, **I want** a per-script performance score and dashboard, **so that** I can prioritize refactoring.
  - **AC:** A 0–100 score per script (`ScoreCalculator.RiskDefault`, capped at 100) with a Low/Medium/High band (thresholds 15/40); a clean script yields a single Info note and score 0; a dashboard summarizes band counts and worst scripts. *(`JsScriptAnalysis.Score/Band`; `BuildDashboardText`.)*
- **US-PERF8.4.2** `[Done]` **As** a developer, **I want** refactoring recommendations and Excel/PDF/JSON/HTML/Markdown/CSV exports, **so that** I can act and share.
  - **AC:** Each finding carries a refactoring recommendation; Excel/PDF/JSON come from the shared reporting module; HTML/Markdown/CSV from small BCL writers; SaveFileDialog is used off the analysis thread. *(`BuildReportModel` → shared exporters; `BuildHtml`/`BuildMarkdown`/`BuildCsv`.)*

## Definition of Done

- Follows suite conventions; read-only default; export formats as listed.
- Code analyzer is UI-free (CI-liftable) and operates on decoded strings; heuristic findings labeled with confidence and code context.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; settings round-trip.
- Testing skeleton under `testing/JavaScriptPerformanceAnalyzer/`; the static rule engine is covered by `testing/UnitTests` (SDK-free).
