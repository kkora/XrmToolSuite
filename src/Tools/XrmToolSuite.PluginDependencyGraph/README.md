# 🕸 Plugin Dependency Graph

An **XrmToolBox** plugin that builds a dependency graph of the Dataverse plugin pipeline —
assembly → type → step → image → message/table → custom API → solution → config — so you can safely
refactor, merge, remove, or deploy plugins without missing hidden dependencies. It adds high-impact,
duplicate-step and unmanaged-registration detection with severities. **Read-only:** it inspects
plugin-registration and solution metadata; it never modifies steps or assemblies.

## Features

| Area | What it builds / detects |
|---|---|
| **Metadata retrieval** | Loads all plugin assemblies, types and SDK steps for the org; each step node resolves table, message, stage, mode, rank, filtering attributes, impersonating user, deployment type and status; pre/post images attach to their steps with imagetype + attribute list |
| **Typed graph model** | Colour-coded, typed nodes (assembly, type, step, table, message, image, custom API, solution, config) with edges assembly→type→step→image and step→table/message/config, rendered as Mermaid text plus SVG/PNG |
| **Focus & filtering** | Focus a single assembly or plugin type to isolate its subgraph (footprint + owning solution); live-filter by table, message, stage, mode or solution — all without re-querying Dataverse |
| **Solution & custom API links** | Solution membership shown per assembly/step (unmanaged registrations visually distinct); `customapi.plugintypeid` draws an edge from the custom API to its backing plugin type |
| **High-impact & risk detection** | High-impact assemblies (table+message fan-out over a configurable threshold) flagged and ranked; duplicate/overlapping steps (matching message+entity+stage+mode) grouped and flagged Medium/High; unmanaged steps/assemblies flagged High with the owning solution |
| **Details & findings** | Selecting a node populates a details panel and a dependency grid of its connected components; a findings panel carries Critical/High/Medium/Low/Info with a plain-language description each |

## Exports

The graph and findings export to seven formats:

- **PNG**
- **SVG**
- **PDF**
- **Excel workbook**
- **JSON** (written indented / pretty-printed)
- **GraphML** (round-trips node/edge types)
- **HTML** (self-contained)

## Help & Support

A **Help** button on the right of the toolbar opens a Help & Support dialog with **Documentation**,
**Report an issue**, and a support link, each opened in your browser. The tool implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same GitHub
project (`kkora/XrmToolSuite`).

## Build & install

Fastest path — build straight into your local XrmToolBox on the same machine:

```powershell
dotnet build src\Tools\XrmToolSuite.PluginDependencyGraph\XrmToolSuite.PluginDependencyGraph.csproj -c Release -p:DeployToXTB=true
```

This is **not** a single-DLL tool: it ships the Excel/PDF export dependency chain (ClosedXML +
PdfSharp/MigraDoc-GDI). The one-step build copies the tool DLL **and** its dependency DLLs into the
XrmToolBox Plugins **root** (never a subfolder, or XrmToolBox silently drops the tool from the Tools
list and Excel/PDF export fails). PNG export uses the GAC's `System.Drawing`, which is never shipped.
For manual distribution and troubleshooting, see [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite
guide [`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md). Restart XrmToolBox
and open **Plugin Dependency Graph**.

## Usage

1. Connect to your environment (System Customizer or higher recommended).
2. Load the plugin pipeline — assemblies, types and SDK steps load with progress and cancellation.
3. Explore the graph; Focus a node to isolate its subgraph, or filter by table/message/stage/mode/solution.
4. Review the risk findings panel (high-impact assemblies, duplicate steps, unmanaged registrations) and node details.
5. Export to PNG / SVG / PDF / Excel / JSON / GraphML / HTML to share or archive.

## Notes & limitations

- **Read-only** — inspects plugin-registration and solution metadata; it never modifies steps or
  assemblies.
- **Secure config is never exposed** — a step's secure/unsecure config usage appears as a flag/edge
  only; the secure-config *value* is never retrieved, and the unsecure config is shown with a
  redacted preview (secrets, GUIDs and long tokens masked).
- Custom-API relationships come from `customapi.plugintypeid` and degrade silently when absent.
- All Dataverse access runs off the UI thread via `RunAsync` / `RetrieveAll` with progress +
  cancellation; any query failure degrades to an informational note rather than failing the graph.
- The SDK-free graph model / builder / risk rules / emitters are covered by
  `testing/UnitTests/PluginDependencyGraphTests.cs`.
