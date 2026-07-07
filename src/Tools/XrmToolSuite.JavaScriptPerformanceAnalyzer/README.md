# ⚡ JavaScript Performance Analyzer

An **XrmToolBox** plugin that statically scans JS web resources — **fully offline, no runtime is
executed** — for performance and deprecation risks, maps scripts to the forms and events that
call them, scores each script, and exports the findings. **Read-only.** Findings are labeled
heuristics: every regex finding carries a 1-based line number, the trimmed source line as
context, and an explicit confidence note.

## Features

| Area | What it analyzes |
|---|---|
| **Web resource inventory** | JScript `webresource` rows (`webresourcetype = 3`) loaded via `RetrieveAll` off the UI thread; a ranked grid shows name, decoded size, band, score and finding count; a search box filters scripts by code content |
| **Static code rules** | Deprecated `Xrm.Page` → Medium; synchronous `XMLHttpRequest` (`open(...,false)` / `async:false`) → High; blocking `alert(` in form logic → High; excessive `console.*` (default > 10) → Low; repeated `retrieve`/`retrieveMultiple`/`Xrm.WebApi` (default > 3) → Medium; hardcoded GUIDs and absolute URLs → Medium; direct DOM access (`getElementById`/`querySelector`/`window.parent`) → Medium. Whole-line comments are skipped |
| **Form & event mapping** | FormXML `<events>/<Handlers>` parsed into library → form → event (OnLoad/OnChange/OnSave) links; selecting a script shows its form/event usage panel; forms with too many OnLoad handlers (default > 5) → Medium |
| **Score & dashboard** | A 0–100 performance score per script (capped) with a Low/Medium/High band (thresholds 15/40); a clean script yields a single Info note and score 0; a dashboard summarizes band counts and the worst scripts. Each finding carries a refactoring recommendation |

The static rule engine and FormXML event mapper are UI-free (CI-liftable) and covered by the
SDK-free unit tests; the live SDK collector is manual-tested.

## Exports

Excel, PDF, JSON, HTML, Markdown, and CSV. Excel/PDF/JSON come from the shared reporting
module; HTML/Markdown/CSV from small BCL writers. The `SaveFileDialog` runs off the analysis
thread.

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with Documentation,
Report an issue, and a support link, each opened via `Process.Start`. The control implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same
GitHub project (`kkora/XrmToolSuite`).

## Build & install

On the machine that runs XrmToolBox, build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.JavaScriptPerformanceAnalyzer\XrmToolSuite.JavaScriptPerformanceAnalyzer.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **JavaScript Performance Analyzer**.

This tool is **not** single-DLL: it ships the Excel/PDF export dependency chain — the tool DLL
plus its 17 ClosedXML/PdfSharp-MigraDoc-GDI dependency DLLs — flat in the Plugins root (never a
subfolder), or XrmToolBox silently drops it from the Tools list. The one-step build above copies
the whole chain for you.

See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) for manual-install steps and troubleshooting, and the
suite-wide [`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md) for the
full DLL list and export-tool guidance.

## Usage

1. Connect to your Dataverse environment (System Customizer or higher recommended).
2. Load the JS web resources — the ranked grid populates with size, band, score and finding count.
3. Select a script to see its findings (with line context and confidence) and its form/event usage.
4. Review the dashboard for band counts and the worst scripts; use the search box to find patterns.
5. Export to Excel, PDF, JSON, HTML, Markdown, or CSV.

## Notes & limitations

- **Read-only** and **fully offline** — no script is executed; all analysis operates on decoded
  strings.
- Findings are **labeled heuristics**: regex/line scans may match comments or string literals, so
  each finding carries a confidence note plus its 1-based line and trimmed source context.
- Thresholds (console count, repeated retrieves, OnLoad handlers) are configurable; the last
  search persists via settings.
