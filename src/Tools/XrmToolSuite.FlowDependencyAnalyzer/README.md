# 🔗 Flow Dependency Analyzer

A static, **read-only** dependency mapper for Power Automate cloud flows. It parses each flow's
`clientdata` JSON into a dependency footprint — trigger, Dataverse tables/columns, connectors,
connection references, environment variables, child flows, custom APIs and HTTP actions — builds a
reverse "which flows break if I change this component?" impact view, raises deployment-risk
findings, and exports the result. Every HTTP endpoint URL, SAS/trigger URL and secret is
**redacted** — never stored or exported.

## Features

| Area | What it maps / detects |
|---|---|
| **Flow inventory & filtering** | Lists all cloud flows (`workflow`, `category=5`, `type=1`) with owner and state (Activated/Draft); filter by solution, owner, status, connector, trigger type and referenced table |
| **Trigger & Dataverse deps** | Resolves each flow's trigger type and (for Dataverse triggers) table + message (Create/Update/Delete/…); extracts the Dataverse tables (`entityName`) and columns (`$select`) each flow reads/writes |
| **Connectors, connection refs & env vars** | Lists connector ids, `connectionreference` logical names, and environment-variable references each flow depends on, so you can confirm they exist in the target |
| **Direct-connection detection** | A `connectionName` GUID that maps to no connection reference → **High** finding with remediation (breaks environment portability) |
| **Child flows, custom APIs & HTTP** | Discovers child-flow (RunFlow) relationships and bound/unbound custom-API invocations; lists HTTP actions by name with all URLs/auth values redacted |
| **Missing / hardcoded metadata** | Missing table → **Critical**; missing column / connection reference / environment variable → **High**; hardcoded environment URLs, GUIDs and table names → **Medium** (a failed lookup degrades to **Info**, never throws) |
| **Component impact & tree** | Pick a component (table, column, connector, connection reference, environment variable, child flow, custom API) and get every flow that depends on it, plus a per-flow dependency tree and a deployment-readiness checklist |

## Exports

The dependency tree and findings export off the UI thread to four formats — secrets/URLs stay
redacted in **every** format:

- **Excel workbook**
- **PDF report**
- **JSON** (carries the impacted-flow map and a pass/fail readiness flag)
- **HTML** (self-contained)

## Help & Support

A **Help** button on the right of the toolbar opens a Help & Support dialog with **Documentation**,
**Report an issue**, and a support link, each opened in your browser. The tool implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same GitHub
project (`kkora/XrmToolSuite`).

## Build & install

Fastest path — build straight into your local XrmToolBox on the same machine:

```powershell
dotnet build src\Tools\XrmToolSuite.FlowDependencyAnalyzer\XrmToolSuite.FlowDependencyAnalyzer.csproj -c Release -p:DeployToXTB=true
```

This is **not** a single-DLL tool: it ships the Excel/PDF export dependency chain (ClosedXML +
PdfSharp/MigraDoc-GDI). The one-step build copies the tool DLL **and** its dependency DLLs into the
XrmToolBox Plugins **root** (never a subfolder, or XrmToolBox silently drops the tool). For manual
distribution and troubleshooting, see [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md). Restart XrmToolBox and
open **Flow Dependency Analyzer**.

## Usage

1. Connect to your environment (System Customizer or higher recommended).
2. Load the cloud-flow inventory and, if needed, filter by solution/owner/status/connector/table.
3. Run the analysis to parse each flow's `clientdata` into a dependency footprint.
4. Review a flow's dependency tree, or use the reverse-lookup panel to see which flows a component change impacts; check the risk findings.
5. Export the tree + findings + readiness checklist to Excel / PDF / JSON / HTML.

## Notes & limitations

- **Read-only** — the tool only parses and reports; it never modifies flows.
- **Secrets are never exposed** — HTTP endpoint URLs, SAS/trigger URLs and auth values are stored and
  exported as `[redacted]` in every surface and format.
- The dependency-discovery engine (`FlowClientDataParser`, `FlowRiskRules`, `FlowModels`) is UI-free
  and unit-tested on `clientdata` fixtures.
- All Dataverse access runs off the UI thread via `RunAsync` / `RetrieveAll`; unresolved logical
  names are flagged as possible missing metadata rather than aborting the run.
