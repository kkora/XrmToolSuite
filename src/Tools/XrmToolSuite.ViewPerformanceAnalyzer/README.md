# 📊 View Performance Analyzer

An **XrmToolBox** plugin that batch-analyzes every system and personal view's FetchXML plus its LayoutXML
column count, scores and ranks the slowest/riskiest views, and shows per-view detail — so you can
proactively fix the views most likely to be slow before users complain. **All analysis is read-only**;
per-view execution timing is opt-in only.

## Features

- **View inventory** — pick a table and list its system views (`savedquery`), loaded via `RetrieveAll` off
  the UI thread with progress; a toggle optionally includes personal views (`userquery`), and the selection
  persists via settings.
- **FetchXML analysis per view** — each view's FetchXML is run through the **shared PERF03 rule engine**
  (all-attributes, missing/broad filters, joins, sorts, aggregations), so results are consistent with the
  standalone FetchXML Performance Analyzer. A view whose FetchXML can't be parsed degrades to a single Info
  finding (score 0), never an exception. A detail panel shows a view's FetchXML (pretty-printed — views
  store it as a single line) and its findings.
- **Layout & column analysis** — displayed-column count (from LayoutXML) and selected-attribute count show
  per view; a layout over `MaxLayoutColumns` (default 15) → Medium ("Over-wide view layout"). A panel lists
  the layout columns driving the width.
- **Scoring & ranking** — a per-view 0–100 score (labeled **heuristic**) combines the shared FetchXML cost
  estimate with a transparent LayoutXML column penalty, banded at 15/40. A slow/risky-views grid sorts by
  score descending, with environment score cards (counts by band, worst views).
- **Optional timing** — opt-in, read-only, off the UI thread; caps an otherwise-unbounded query with a small
  top and shows elapsed time + row count per view.
- **Recommendations** — each flagged view carries a recommendation.

This tool is the first consumer of the shared FetchXML engine in `src/Shared/Core/FetchXml/` (PERF03) — it
reuses those query rules rather than reimplementing them, and adds LayoutXML column analysis, per-view
scoring, and batch ranking on top.

## Exports

Excel, PDF, JSON, HTML, Markdown, and CSV. Excel/PDF/JSON come from the shared reporting module
(`ExcelReportExporter`/`PdfReportExporter`/`JsonReportExporter` over a `ReportModel`); HTML/Markdown/CSV via
BCL writers.

## Help & Support

A right-aligned **Help** button opens a Help & Support dialog with **Documentation**, **Report an issue**,
and a support link, each opened in the browser. The tool implements `IHelpPlugin` and `IGitHubPlugin`, so
XrmToolBox's own tool-menu links resolve to the same GitHub project (`kkora/XrmToolSuite`).

## Build & install

This tool is **not** a single-DLL tool — it ships the Excel/PDF export dependency chains (the
ClosedXML + PdfSharp/MigraDoc-GDI DLLs). The one-step build copies the whole chain into the XrmToolBox
Plugins root for you:

```powershell
dotnet build src\Tools\XrmToolSuite.ViewPerformanceAnalyzer\XrmToolSuite.ViewPerformanceAnalyzer.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **View Performance Analyzer**. For a manual copy to another machine, copy
**every** DLL from the tool's `bin\Release\net48\` folder — flat in the Plugins root, never a subfolder — or
XrmToolBox silently drops the tool. Full details in [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your environment (System Customizer or higher recommended).
2. Click **Load tables**, pick a table in the dropdown (it shows `-- select table --` until you do);
   optionally toggle **include personal views**, then run the batch analysis (paged via `RetrieveAll`
   with progress/cancellation).
3. Review the ranked slow/risky-views grid and the environment score cards; select a view for its FetchXML
   (pretty-printed for readability), findings, and layout-column detail.
4. *(Optional)* Opt into **timing** to execute a view read-only and see elapsed time + row count.
5. **Export** in any of the supported formats.

## Notes & limitations

- Read-only by default; per-view execution timing is opt-in and runs a read capped with a small top.
- Per-view scores are **heuristic estimates** (no server query statistics) and are labeled as such — use the
  opt-in timing to validate them.
- SDK-free scoring is covered by `testing/UnitTests/ViewPerformanceAnalyzerTests.cs`.
