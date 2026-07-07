# 🏛 Architecture Diagram Generator

A read-only **XrmToolBox** plugin that turns a Dataverse solution's components and platform
dependencies into an architecture diagram. Components are classified into architectural **layers**
(Apps, UI, Automation, Code, Data, Security, Configuration, Other) and connected by edges read from
the platform `dependency` table, so the diagram reflects the live environment instead of a hand-drawn
Visio/Lucidchart that has drifted. It reuses the Solution Knowledge Graph's extraction approach and
keeps its model + emitters UI-free and SDK-free so they stay unit-tested and portable.

## Features

| Area | What it does |
|---|---|
| **Scope selection** | Load and select a solution as the diagram scope; system solutions are excluded and the picker shows friendly name / unique name / version / managed flag. Layout, direction, and hide-orphans preferences round-trip via Load/SaveSettings. |
| **Layer classification** | `ComponentCatalog` maps each `solutioncomponent.componenttype` to a friendly label and an architectural layer; unknown types degrade to a generic label in the *Other* layer. |
| **Dependency edges** | `ArchCollector` reads `solutioncomponent` + the platform `dependency` table fail-soft — a query gap degrades to a documented note rather than crashing; self-loops and duplicate edges are removed. |
| **Layout styles** | Choose **Layered** (grouped by layer) or flat **Dependency graph** layout and **LR/TD** flow direction; re-layout happens from the in-memory model without re-querying Dataverse. Layers return in canonical Apps→…→Other order. |
| **Orphan filtering** | Optionally hide unconnected (orphan) nodes to declutter large diagrams down to just the dependency structure; emitters and JSON honour the filter and all node text is escaped. |
| **Preview** | A preview combo re-renders the current model/layout in the chosen format (Mermaid / PlantUML / DOT / HTML) before export; generation reports progress and node/edge counts. |

## Exports

BCL-only — this tool has **no export dependency chain** (no ClosedXML/PdfSharp/MigraDoc). All formats
are produced as plain strings using the base class library, so the tool ships as a single DLL.

| Format | What you get |
|---|---|
| **Mermaid** | Layered `subgraph`s plus edges, ready to drop into wikis and Markdown that render Mermaid. |
| **PlantUML** | A well-formed `@startuml`/`@enduml` document with `package`s per layer. |
| **DOT / Graphviz** | A clustered `digraph` for Graphviz and diagram tooling. |
| **Markdown** | A fenced Mermaid block plus a per-layer legend, for formatted docs. |
| **HTML** | A self-contained, theme-aware (light/dark) page that renders a hand-laid-out **inline SVG** offline — no external engine or CDN — with the Mermaid source embedded for re-rendering. |
| **JSON** | Nodes + edges honouring the orphan filter, for programmatic use. |

## Help & Support

A right-aligned **Help** button on the toolbar opens the shared suite Help & Support dialog
(Documentation, Report an issue, and a support link, each opened via `Process.Start`). The control
implements `IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same
GitHub project (`kkora/XrmToolSuite`).

## Build & install

This tool is a **single DLL** — the shared core is compiled in, so there is no ClosedXML/PdfSharp
chain to ship alongside it.

Build and deploy straight into your local XrmToolBox in one step:

```powershell
dotnet build src\Tools\XrmToolSuite.ArchitectureDiagramGenerator\XrmToolSuite.ArchitectureDiagramGenerator.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Architecture Diagram Generator**. For manual/other-machine installs
copy only `XrmToolSuite.ArchitectureDiagramGenerator.dll` into `%AppData%\MscrmTools\XrmToolBox\Plugins`
(flat in the Plugins root, not a subfolder), `Unblock-File` it, and restart. Do **not** copy
`Newtonsoft.Json.dll` or any `Microsoft.Xrm.*` / `Microsoft.Crm.*` DLLs — XrmToolBox already ships them.

Full steps and troubleshooting: [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite-wide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your environment as usual.
2. Click **Load solutions** and pick the solution to diagram.
3. Choose a **layout** (Layered or Dependency graph) and **direction** (LR/TD); optionally tick
   **Hide orphans**.
4. **Generate** — components are classified into layers and dependency edges are drawn; progress and
   node/edge counts are reported.
5. Use the preview combo to check the source, then **Export** to any of the six formats.

Because re-layout runs from the in-memory model, changing layout/direction/orphan settings re-renders
instantly without re-querying Dataverse.

## Notes & limitations

- **Read-only** — the tool only reads solution and dependency metadata; it never writes to the org.
- **BCL-only** — no third-party export dependencies; the tool ships as one DLL.
- Edges come from the platform `dependency` table, which records the dependencies Dataverse itself
  tracks; components with no recorded dependency appear as orphans unless hidden.
- The SDK-free model (`Diagram/ArchModel.cs`) and emitters (`Diagram/DiagramEmitters.cs`) are
  unit-tested (`testing/UnitTests/ArchitectureDiagramGeneratorTests.cs`); the SDK collector
  (`Diagram/ArchCollector.cs`) and the WinForms host are manual-tested against a live connection.
- Unavailable data degrades to a documented note rather than a crash; System Customizer or higher is
  recommended for complete results.
