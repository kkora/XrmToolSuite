# 🕸 Solution Knowledge Graph

An **XrmToolBox** plugin that builds a directed dependency graph of a Dataverse solution's components,
then traces dependencies, computes deletion impact, and detects circular dependencies. It renders a
self-contained offline interactive HTML view and exports to GraphML, SVG and PNG. **Read-only** (reads
components, dependencies and metadata; never modifies the solution).

## Features

| Capability | What it does |
|---|---|
| **Graph construction** | One node per solution component with a friendly type (`Table`, `Form`, `View`, `Plugin Step`, `Web Resource`, `Workflow / Flow`, `Security Role`, `Model-driven App`, …) and a resolved display name — tables from entity metadata, the rest by name query, falling back to `type + short id` when a name is missing. |
| **Edges** | Come from the platform `dependency` table (dependent → required, kind `requires`), only when the dependent is in the solution. Construction is off-thread and fail-soft. |
| **Dependency trace** | Forward transitive reachability (BFS over out-edges) — everything a node depends on. |
| **Deletion impact** | Reverse transitive reachability (BFS over in-edges) — everything that would be impacted by deleting a node. |
| **Circular-dependency detection** | Strongly-connected components of size > 1 via an iterative, stack-safe Tarjan; an acyclic graph reports none. |
| **Search & filters** | Case-insensitive label search plus node-type filters over the graph's distinct types. |
| **Interactive view** | A self-contained offline HTML page — a vanilla-JS force-directed canvas with search, type filters, node drag and click-to-highlight (green = depends-on, red = impacted); no external CSS/JS/fonts/CDN. |

## Exports

- **Interactive HTML** (self-contained, offline)
- **GraphML** (readable in yEd / Gephi / Cytoscape)
- **SVG** (deterministic circular layout, per-type node colours)
- **PNG** (rasterised circular layout via GDI+)

## Help & Support

A right-aligned **Help** button on the toolbar opens a Help & Support dialog (Documentation, Report an
issue, and a support link, each opened in the browser). The tool implements `IHelpPlugin` and
`IGitHubPlugin` pointing at repository [`kkora/XrmToolSuite`](https://github.com/kkora/XrmToolSuite).

## Build & install

This is a **single self-contained DLL** — it ships no export dependency chain (GraphML/SVG/HTML output is
pure string; PNG uses the net48 `System.Drawing` GAC assembly). The one-step build copies the tool DLL
into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.SolutionKnowledgeGraph\XrmToolSuite.SolutionKnowledgeGraph.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **Solution Knowledge Graph**. For a manual copy to another machine, copy the
single output DLL into `%AppData%\MscrmTools\XrmToolBox\Plugins` (flat in the Plugins root) and unblock
it. See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Open **Solution Knowledge Graph** and pick a solution; build the graph (runs off the UI thread).
3. Explore the node grid — each row shows direct depends-on / impact counts; select a node for the
   transitive sets in the detail pane.
4. Use **Detect cycles** to list circular-dependency groups, and search / type filters to focus.
5. Open the interactive HTML view, or export to GraphML / SVG / PNG.

## Notes & limitations

- **Read-only** — the graph model, algorithms (trace/impact/cycles) and GraphML/SVG/HTML exporters stay
  UI-free and SDK-free; the builder degrades query failures instead of throwing.
- The interactive HTML embeds node/edge data as hand-built JSON with `<`/`>` and control-char escaping so
  a component label can't break out of the `<script>` block; it opens offline with no external resources.
