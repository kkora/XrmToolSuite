# 🕸 Solution Knowledge Graph

An **XrmToolBox** plugin that builds a directed dependency graph of a Dataverse solution's components,
then traces dependencies, computes deletion impact, and detects circular dependencies. It renders a
self-contained offline interactive HTML view and exports to GraphML, SVG and PNG. **Read-only** (reads
components, dependencies and metadata; never modifies the solution).

## Features

| Capability | What it does |
|---|---|
| **Graph construction** | One node per solution component with a friendly type (`Table`, `Column`, `Form`, `View`, `Relationship`, `Option Set`, `Plugin Step`, `Web Resource`, `Workflow / Flow`, `Security Role`, `Model-driven App`, `Environment Variable`, `Site Map`, …) and a resolved display name. |
| **Readable names** | Tables/columns/relationships/option sets from a bulk metadata read (columns show as `table.Display Name`, relationships as their schema name), environment variables and site maps by name query, org-custom component types (codes ≥ 10000) labelled from `solutioncomponentdefinition`. Falls back to `type + short id` only when no name source exists. Environment Variable *Values* are deliberately left as short ids — their only readable column is the value itself, which can carry secrets. |
| **Edges** | Come from the platform `dependency` table (dependent → required, kind `requires`), only when the dependent is in the solution. Required components *outside* the solution are typed + named from the dependency row's `requiredcomponenttype` instead of showing as `Unknown` GUIDs. Construction is off-thread and fail-soft. |
| **Dependency trace** | Forward transitive reachability (BFS over out-edges) — everything a node depends on. |
| **Deletion impact** | Reverse transitive reachability (BFS over in-edges) — everything that would be impacted by deleting a node. |
| **Circular-dependency detection** | Strongly-connected components of size > 1 via an iterative, stack-safe Tarjan; an acyclic graph reports none. |
| **Search & filters** | Case-insensitive label search plus node-type filters over the graph's distinct types. **Exports and the interactive view honour the type filter** — uncheck a type and it disappears from every output (edges are kept only when both endpoints survive). |
| **Interactive view** | A self-contained offline HTML page — a vanilla-JS force-directed canvas (cooling layout that settles instead of exploding) with search, type filters, always-on node labels, scroll-zoom, drag-to-pan, a **Fit** button (double-click also re-fits), node drag, and click-to-highlight (green = depends-on, red = impacted); no external CSS/JS/fonts/CDN. |
| **Layout** | Three resizable panels (type filters \| node grid \| detail pane) split into equal thirds by default, adjustable via draggable splitters. |

## Exports

All exports respect the current node-type filter (unchecked types are excluded, with their edges).

- **Interactive HTML** (self-contained, offline — zoom/pan/fit, always-labelled nodes)
- **GraphML** (for graph editors — see *Viewing GraphML files* below)
- **SVG** (deterministic circular layout, per-type node colours, **colour legend** top-left)
- **PNG** (rasterised circular layout via GDI+, same colour legend)

### Viewing GraphML files

GraphML is an XML graph format — Windows has no default app for it, so use a graph editor:

| App | Get it | Notes |
|---|---|---|
| **yEd** (recommended) | [yworks.com/products/yed](https://www.yworks.com/products/yed) (free desktop) or [yEd Live](https://www.yworks.com/yed-live/) (browser, no install) | Opens `.graphml` natively. First map the data to node text once (*Edit ▸ Properties Mapper* → map `label`), then run *Layout ▸ Organic* or *Hierarchical* — far more readable than any fixed layout, and you can rearrange/group nodes by hand. |
| **Gephi** | [gephi.org](https://gephi.org) (free) | Better for large-graph *analysis* (centrality, clustering) than diagrams. |
| **Cytoscape** | [cytoscape.org](https://cytoscape.org) (free) | Network-analysis oriented; opens GraphML fine. |
| Any text editor / VS Code | — | It's plain XML: each `<node>` carries `label` + `type`, each `<edge>` carries `kind="requires"` — easy to post-process with a script. |

That's why the tool ships GraphML alongside SVG/PNG: the images are fixed snapshots; GraphML is for
exploring or restyling the graph in a real graph editor.

## Help & Support

A right-aligned **Help** button on the toolbar opens a Help & Support dialog (Documentation, Report an
issue, and a support link, each opened in the browser). The **Documentation** link opens this tool's own
guide (this README). The tool implements `IHelpPlugin` and `IGitHubPlugin` pointing at repository
[`kkora/XrmToolSuite`](https://github.com/kkora/XrmToolSuite).

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
5. Open the interactive HTML view (scroll = zoom, drag background = pan, **Fit** re-frames, click a node
   to highlight its dependencies/impact), or export to GraphML / SVG / PNG — exports match the current
   type filter.

## Notes & limitations

- **Read-only** — the graph model, algorithms (trace/impact/cycles) and GraphML/SVG/HTML exporters stay
  UI-free and SDK-free; the builder degrades query failures instead of throwing.
- The interactive HTML embeds node/edge data as hand-built JSON with `<`/`>` and control-char escaping so
  a component label can't break out of the `<script>` block; it opens offline with no external resources.
