# ⚡ FetchXML Performance Analyzer

An **XrmToolBox** plugin that parses any FetchXML query — pasted ad-hoc or loaded from a system/personal
view — and flags performance risks in its filters, joins, sorts, and payload, with a heuristic cost
estimate, concrete optimization suggestions, and optional opt-in live timing. **All analysis is
read-only**; the query is only executed if you explicitly opt into timing.

## Features

The parser breaks a query into entity, attributes, filters, links, orders, aggregation, and paging, and a
shared rule engine ranks findings by severity:

| Area | What it flags |
|---|---|
| **Payload** | `<all-attributes/>` → High; selected-column count over `MaxAttributes` (30) → Medium |
| **Filters / scans** | No filter on the root entity → High; aggregate without a filter → Medium |
| **Joins** | Link count over `MaxLinkEntities` (4) → High, over `WarnLinkEntities` (2) → Medium; any outer join → Low (heuristic) |
| **Sorts** | Sort on a link-entity column → Medium (heuristic) |
| **Aggregation / paging** | Aggregate without filter → Medium; no paging + no top + no aggregate → Low; distinct over many joins → Info |

- **Query summary** — each element listed with counts (attributes across all entities, link-entities at
  every depth, orders, aggregate/distinct/no-lock, top/page size).
- **Estimated cost** — a composite 0–100 score derived from finding severities (via the shared
  `ScoreCalculator`), banded at 15/40, and **labeled an estimate** (no server statistics).
- **Optional live timing** — opt-in, read-only, off the UI thread; caps an otherwise-unbounded query with
  a small top and reports elapsed ms + row count to ground the estimate.
- **Recommendations** — a suggestions panel lists plain-text fixes (explicit columns, add filter, reduce
  joins, etc.) tied to the findings.

The UI-free parser + rule engine lives in `src/Shared/Core/FetchXml/` and is reused by the View and
Dashboard analyzers.

## Exports

Excel, PDF, JSON, HTML, Markdown, and CSV. Excel uses the shared `ExcelReportExporter` (ClosedXML) and PDF
uses `PdfReportExporter` (PdfSharp/MigraDoc-GDI), both over the shared `ReportModel`; JSON uses
`JsonReportExporter`; HTML/Markdown/CSV are BCL string builders.

## Help & Support

A right-aligned **Help** button opens a Help & Support dialog with **Documentation**, **Report an issue**,
and a support link, each opened in the browser. The tool implements `IHelpPlugin` and `IGitHubPlugin`, so
XrmToolBox's own tool-menu links resolve to the same GitHub project (`kkora/XrmToolSuite`).

## Build & install

This tool is **not** a single-DLL tool — it ships the Excel/PDF export dependency chains (the
ClosedXML + PdfSharp/MigraDoc-GDI DLLs). The one-step build copies the whole chain into the XrmToolBox
Plugins root for you:

```powershell
dotnet build src\Tools\XrmToolSuite.FetchXmlPerformanceAnalyzer\XrmToolSuite.FetchXmlPerformanceAnalyzer.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **FetchXML Performance Analyzer**. For a manual copy to another machine,
copy **every** DLL from the tool's `bin\Release\net48\` folder — flat in the Plugins root, never a
subfolder — or XrmToolBox silently drops the tool. Full details in [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and
the suite guide [`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your environment (System Customizer or higher recommended).
2. Paste FetchXML into the editor, **or** load it from a system (`savedquery`) / personal (`userquery`) view
   via the view picker. Invalid FetchXML shows a clear parse error and blocks analysis/export.
3. Review the query summary, severity-ranked findings, estimated cost + band, and suggestions.
4. *(Optional)* Opt into **live timing** to execute the query read-only and see elapsed ms + row count.
5. **Export** the analysis in any of the supported formats.

## Notes & limitations

- Read-only by default; the query is executed only when you explicitly opt into timing, and even then it is
  a read capped with a small top.
- "Index-friendly" / "query cost" are **heuristic estimates** without server statistics — the tool labels
  them as estimates; use the opt-in live timing to ground them.
- The parser depends only on a FetchXML string (`System.Xml.Linq`, no `IOrganizationService`), so it is
  unit-tested SDK-free in `testing/UnitTests/FetchXmlAnalyzerTests.cs`.
