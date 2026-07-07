# 🧪 Custom API Explorer

An **XrmToolBox** plugin that discovers, documents, and safely tests Custom APIs: a browsable,
filterable catalog (unique/display name, binding, is-function/private, request parameters, response
properties, backing plugin type) plus a **confirmation-gated test console** that builds a request
from parameter metadata and typed inputs and invokes the selected API, showing the typed response or
the raw fault. **Inventory and documentation are read-only; the invoke console is the only write
path, and it always requires an explicit confirmation naming the API, target, and environment.**

## Features

| Capability | What you get |
|---|---|
| **Inventory** | All Custom APIs in the org, retrieved via `RetrieveAll` off the UI thread with progress + cancellation; the grid shows unique name, kind (function/action), private flag, and binding. |
| **Filter** | A live text filter narrows by unique/display name; the filter round-trips via settings. |
| **Backing plugin type** | `plugintypeid` resolved to the plugin type name; logic-less APIs are marked "(none)". |
| **Parameters & responses** | Request parameters listed with logical name, `CustomApiFieldType`, and optional flag (empty sets shown explicitly); response properties with name and type; the detail pane labels Function vs. Action. |
| **Callers & wiring** | The model carries a caller list and surfaces the associated SDK message when present; a failed dependency query degrades to an informational note, not an error. |
| **Test console (gated)** | A parameter grid generated per selected API takes typed inputs; `RequestBuilder.Bind` validates required parameters and converts scalar values (invoke is blocked until valid). |
| **Sample snippet** | A Web API invocation snippet generated per API from parameter metadata — marked illustrative and containing no secrets (POST-with-body for actions, GET-with-query for functions). |

The value parsing, request-parameter binding/validation, sample-snippet generation, and the
HTML/Markdown/CSV exporters are SDK-free and unit-tested; the Dataverse collector and the live
invocation are manual-tested.

## Exports

Documentation export runs off the UI thread and covers all APIs with parameters, responses, binding,
and backing plugin. These are read-only, lightweight formats (no Excel/PDF dependency chain).

| Format | Notes |
|---|---|
| **HTML** | Self-contained and theme-aware; content is HTML-escaped. |
| **Markdown** | Full catalog including parameters and responses. |
| **CSV** | One row per member. |

### The invoke console (the only write path)

Distinct from the read-only catalog and exports, the test console **executes** the selected API. It
is always gated: a confirmation dialog states the API name, the bound target/record, and the
environment, and invoke proceeds only on confirm (the default button is **Cancel**). The call runs
via `RunAsync` off the UI thread; the typed response is rendered on success and the raw
`OrganizationServiceFault` on failure. No secrets are displayed or logged, and the settings POCO
stores only the last filter — never parameter presets with secret values or connection details.

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with **Documentation**,
**Report an issue**, and a support link, each opened via `Process.Start`. The tool implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same GitHub
project (`kkora/XrmToolSuite`).

## Build & install

This is a **single, self-contained DLL** — the shared core is compiled in, and the HTML/Markdown/CSV
exporters need no Excel/PDF dependency chain. Build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.CustomApiExplorer\XrmToolSuite.CustomApiExplorer.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Custom API Explorer**. Full build/install/troubleshooting details
(including the manual single-DLL copy + `Unblock-File` step) are in
[`./DEPLOYMENT.md`](./DEPLOYMENT.md); the suite-wide guide is in
[`../../../Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Browse or filter the Custom API catalog; select an API to see its parameters, responses, binding, and backing plugin type.
3. (Optional) **Export** the catalog to HTML, Markdown, or CSV, or copy the generated sample snippet.
4. To test: fill the generated parameter form with typed inputs (invoke is blocked until required parameters are valid).
5. Confirm the dialog — which names the API, target, and environment — to invoke; review the typed response or the raw fault.

## Notes & limitations

- **Inventory and documentation are read-only.** The invoke console is the **only** write path and is always confirmation-gated (default button = Cancel), naming the API, target, and environment before it runs.
- **Never reads, stores, or displays secrets** — the settings POCO holds only the last filter; generated snippets and exports contain no secrets.
- Live invocation and live dependency/message retrieval are manual (live-org) paths; a failed dependency query degrades to an informational note rather than throwing.
