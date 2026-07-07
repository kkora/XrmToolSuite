# 📋 Environment Inventory

An **XrmToolBox** plugin that collects a normalized, searchable inventory of a Dataverse environment —
across metadata, solutions, security, automation, and web/dev components — with per-component detail and
multi-format export. **Read-only**, and **secret values are never read or exported**.

## Features

Each data source runs in its own fail-soft try/catch: a permission gap degrades to an "unavailable source"
note rather than a hard error.

| Category | What it inventories |
|---|---|
| **Metadata & solutions** | Solutions/publishers (managed state, version); tables via `RetrieveAllEntitiesRequest` with per-table column count and managed/custom state |
| **Security** | Security roles, users, teams, business units (key attributes only — no credentials/secrets) |
| **Automation** | Plugin assemblies + SDK steps; workflows/business rules/actions/BPFs/modern flows (by category, with state) |
| **Web / dev** | Web resources, PCF custom controls (degrades if absent), custom APIs (type + managed state) |
| **Configuration** | Environment-variable **definitions** (schema name, display name, declared type) and connection references (logical name, connector id) — never any value/secret column |

- **Search & filter** — a search box plus category and managed-state dropdowns drive the grid; filtering is
  client-side over the cached snapshot (`InventorySnapshot.Filter`), fast on large environments.
- **Detail panel** — selecting a row shows its normalized fields plus the source-specific `Details`
  dictionary.

The normalization model (`InventoryItem`/`InventorySnapshot`), exporters, and summary projection are
UI-free and SDK-free (unit-tested); the Dataverse collector is a separate file. Retrieval uses the shared
`RetrieveAll` (paging + cancellation) off the UI thread with progress.

## Exports

Excel, CSV, JSON, Markdown, HTML, Word, and PDF. CSV is RFC-4180 quoted; HTML is self-contained (inline
CSS); the text formats carry a summary counts table; **Excel** produces the full inventory grid (Summary +
Items worksheets, no secret column) via ClosedXML; **Word** and **PDF** produce a summary-level report from
the shared `ReportModel` exporters (Word reuses DocumentFormat.OpenXml from the ClosedXML chain; PDF uses
PdfSharp/MigraDoc-GDI). Export scope (selected sources) round-trips via settings.

## Help & Support

A right-aligned **Help** button opens a Help & Support dialog with **Documentation**, **Report an issue**,
and a support link, each opened in the browser. The tool implements `IHelpPlugin` and `IGitHubPlugin`, so
XrmToolBox's own tool-menu links resolve to the same GitHub project (`kkora/XrmToolSuite`).

## Build & install

This tool is **not** a single-DLL tool — it ships the Excel/PDF/Word export dependency chains (the
ClosedXML + PdfSharp/MigraDoc-GDI DLLs; the Word exporter reuses the OpenXML assembly from the ClosedXML
chain). The one-step build copies the whole chain into the XrmToolBox Plugins root for you:

```powershell
dotnet build src\Tools\XrmToolSuite.EnvironmentInventory\XrmToolSuite.EnvironmentInventory.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Environment Inventory**. For a manual copy to another machine, copy
**every** DLL from the tool's `bin\Release\net48\` folder — flat in the Plugins root, never a subfolder — or
XrmToolBox silently drops the tool. Full details in [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your environment (System Customizer or higher recommended).
2. Load the inventory — sources are collected via `RetrieveAll` off the UI thread with progress and
   cancellation.
3. Search and filter by category, name, and managed state; select a row for its detail panel.
4. **Export** in any of the supported formats.

## Notes & limitations

- Read-only; no secrets or credentials are ever persisted or exported — only environment-variable
  *definitions* and connection-reference metadata, never any value/secret column.
- Unavailable sources (e.g. a permission gap, or a table absent from the environment) degrade to a noted
  "unavailable source" rather than aborting the run.
- The normalization model is UI-free/SDK-free and unit-tested in
  `testing/UnitTests/EnvironmentInventoryTests.cs`.
