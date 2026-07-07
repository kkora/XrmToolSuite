# 📘 Solution Documentation Generator

An **XrmToolBox** plugin that scans a Dataverse solution and generates a complete, current,
multi-section document — technical and business — in one run, previews it, and exports to several
formats. Use it so support, audit, onboarding, and client-handoff teams always have accurate,
formatted docs. **Read-only:** it never modifies the solution and never reads environment-variable
values or secrets.

## Features

| Section | What is documented |
|---|---|
| **Component inventory** | Counts by component type, rolled up across the scanned data. |
| **Schema** | Tables with a column/relationship summary; Full Solution Reference adds a per-table column-detail table; global choices/option sets. |
| **Forms, views, charts, dashboards, apps** | Component-summary rows (name, type, table, managed) drive readable sections; unavailable detail degrades to a note. |
| **Automation & logic** | Business rules, workflows, flows, plug-ins (assembly/type/step/image), web resources, custom APIs, environment variables, connection references, and included security roles — each a distinct section. |
| **Diagrams** | A deterministic Mermaid `erDiagram` of the documented tables plus a relationships table (Full Solution Reference), embedded into Markdown/HTML/Word/PDF. |
| **Summaries** | Release-notes and architecture-summary sections generated from the scanned data. |
| **Branding** | Header line, logo URL, and publisher override round-trip in settings and appear in the rendered document header (no credentials stored). |

Three **documentation modes** — Executive Summary, Standard Reference, Full Solution Reference —
plus a per-section checklist control the depth and scope of a run. Generation runs off the UI thread
with per-section progress and cancellation, and the preview pane shows the rendered Markdown or HTML
source before final export. The SDK-free document pipeline (`SolutionScanData`, the `DocBuilder`
template engine, and the Markdown/HTML/JSON `DocRenderers`) is unit-tested; unavailable component
types degrade to documented "not available" notes.

## Exports

Export runs off the UI thread.

| Format | Notes |
|---|---|
| **Word** | Via OpenXML. |
| **PDF** | Native PDF via MigraDoc-GDI. |
| **Markdown** | SDK-free renderer. |
| **HTML** | Self-contained + theme-aware. |
| **Searchable HTML portal** | A single self-contained file with a sticky sidebar table-of-contents, offline client-side search that filters sections and table rows, collapsible sections, and a light/dark toggle — all CSS/JS inlined, browses from `file://` with no server or CDN. Folds in the retired Markdown/Word/HTML-Portal doc-format tools. |
| **Excel** | Via ClosedXML. |
| **JSON** | Carries the structured inventory. |

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with **Documentation**,
**Report an issue**, and a support link, each opened via `Process.Start`. The tool implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same GitHub
project (`kkora/XrmToolSuite`).

## Build & install

This tool is **not** a single-DLL tool — it ships the Excel/PDF/Word export dependency chain (the
tool DLL plus its ClosedXML + PdfSharp/MigraDoc-GDI dependency DLLs, flat in the Plugins root, never
a subfolder). The one-step build copies the whole chain for you:

```powershell
dotnet build src\Tools\XrmToolSuite.SolutionDocumentationGenerator\XrmToolSuite.SolutionDocumentationGenerator.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Solution Documentation Generator**. Full
build/install/troubleshooting details are in [`./DEPLOYMENT.md`](./DEPLOYMENT.md); the suite-wide
guide (including why the export DLLs must sit in the Plugins root) is in
[`../../../Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Pick the solution to document, a documentation mode, an output format, and which sections to include.
3. (Optional) Set the branding fields (header line, logo URL, publisher override).
4. Generate — watch per-section progress, and review the rendered Markdown/HTML in the preview pane.
5. **Export** to Word, PDF, Markdown, HTML, the searchable HTML portal, Excel, or JSON.

## Notes & limitations

- **Read-only** — never modifies the solution; environment-variable values and secrets are never read.
- The `DocBuilder` template engine stays UI-free/SDK-free and degrades unavailable component types to documented notes rather than failing.
- Branding settings and section selections round-trip via Load/SaveSettings; no credentials are stored.
