# 🧩 Template Tool

The canonical **scaffold** that every new XrmToolSuite tool is cloned from. It is a minimal, working
XrmToolBox plugin — it loads in XrmToolBox, connects to Dataverse, retrieves a sample list of accounts
off the UI thread, and persists a setting — wired up with the suite conventions so a new tool starts
from a correct baseline rather than a blank project.

> **Not published.** The Template Tool is the clone source only; it is never shipped to the XrmToolBox
> Tool Library. Keep it **generic** — never add feature-specific code here.

## Getting started

New tools are created with the script, which copies this project and rewrites the tokens for you:

```powershell
./scripts/New-Tool.ps1 -Name "MyTool" -DisplayName "My Tool" -Description "..."
```

`New-Tool.ps1` replaces the literal string `Template Tool` with the tool's display name and
`TemplateTool` with its PascalCase name across every copied file (project, namespace, controls,
docs) — so `XrmToolSuite.TemplateTool` becomes `XrmToolSuite.MyTool`, `TemplateToolControl.cs`
becomes `MyToolControl.cs`, and so on.

To build the scaffold (or a freshly cloned tool) straight into your local XrmToolBox in one step:

```powershell
dotnet build src\Tools\XrmToolSuite.TemplateTool\XrmToolSuite.TemplateTool.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Template Tool**. Full steps and troubleshooting are in
[`./DEPLOYMENT.md`](./DEPLOYMENT.md); the suite-wide guide is
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## What to customize

After cloning, work through this checklist for the new tool:

- **UI + logic** — rename and fill in `TemplateToolControl.cs` (and its `*.Designer.cs`); replace the
  sample account-retrieval logic with the tool's real behaviour. Keep layout in the Designer file and
  logic in the main partial class.
- **Repo / Help links** — set `UserName`, `RepositoryName`, and `HelpUrl` in the control. The scaffold
  ships placeholder `your-github-username` links; the suite's real links point at `kkora/XrmToolSuite`.
  Never ship the placeholders.
- **MEF metadata** — give the plugin a unique `ExportMetadata("Name", …)` and `("Description", …)` (no
  "Template Tool" leftovers).
- **Settings** — replace the sample `ToolSettings` POCO with the tool's own serializable settings
  (plain data only — no controls, services, or credentials).
- **Backlog + testing docs** — create/rename the tagged backlog and user-story files and fill in the
  `testing/Tools/<Tool>/` plan, cases, and summary, and add the tool to the UI smoke test's
  `ExpectedTools`.
- **This README** — replace it with a real README for the tool (overview, features, exports, help,
  build/install, usage, limitations), following the shipping tools' format.

## Suite conventions

The scaffold already follows the non-negotiable patterns — preserve them as you build:

- **`BaseToolControl`** — the control derives from the shared `XrmToolSuite.Core.BaseToolControl`.
- **`RunAsync` / `RetrieveAll`** — every Dataverse call runs off the UI thread via `RunAsync`
  (wraps `WorkAsync`); UI updates happen only in the completion callback. Use `Service.RetrieveAll`
  for paging. Event handlers call `ExecuteMethod(...)` so XrmToolBox prompts for a connection.
- **`Load` / `SaveSettings`** — settings load in the `Load` event via `LoadSettings<T>()` and save in
  `ClosingPlugin` via `SaveSettings(...)`.
- **`UpdateConnection`** — overridden to call `base` first and clear `MetadataCache`, since
  environments differ.
- **Required plugin icons** — the plugin must export `SmallImageBase64` and `BigImageBase64` metadata
  (from `XrmToolSuite.Core.PluginIcons`); MEF silently drops any plugin missing either key.
- **Help button** — the constructor adds the shared right-aligned Help button
  (`CreateHelpButton()`), and the control implements `IHelpPlugin` + `IGitHubPlugin`.
