# 📘 API Documentation Builder

A read-only **XrmToolBox** plugin that documents a Dataverse environment's **Custom APIs**
(parameters, response properties, binding, and backing plugin) as a redaction-safe reference and a
**best-effort OpenAPI 3.0-style JSON spec**. It is the OpenAPI + redaction specialist of the suite —
complementary to the **Custom API Explorer**, which browses and (confirmation-gated) *invokes* APIs.
This tool **never invokes an API**: secret-named parameters are masked in examples and the spec,
free-text bearer tokens and URL query strings (SAS / HTTP-trigger secrets) are stripped, and the
operator can add their own redaction terms.

## Features

| Area | What it does |
|---|---|
| **Discovery** | Loads and documents the Custom APIs in the connected environment off the UI thread; the status shows the documented count, and the catalog carries a documented note when the tables can't be read or none are present. |
| **Select which APIs** | A left panel lists every Custom API with a checkbox (all checked by default), a **Select all (n/total)** toggle, and a search box that filters by unique/display name (checked state is preserved while filtering). **Preview and every export include only the checked APIs** — the count in the output reflects the selection. |
| **Real HTML preview** | An **Open in browser** button (enabled in the HTML source preview) renders the actual themed HTML page in your default browser; the source pane also pretty-prints HTML and OpenAPI JSON for readability (exports write the original bytes). |
| **API detail** | For each API, documents unique/display name, operation kind (Action/Function), private flag, binding type, bound table, and backing plugin type (sourced from `customapi` + `plugintype`); missing fields degrade gracefully. |
| **Parameters & responses** | Renders a request-parameter and response-property grid with name, type, and requirement; field types map to friendly labels and OpenAPI schema fragments. |
| **Safe examples & spec** | Generates template request/response payloads (labelled as templates) and an OpenAPI 3.0-style spec — one POST path per API with request-body + 200-response schemas built from parameter/response metadata (best-effort, not a guaranteed live contract; the spec description says so). |
| **Redaction safety** | Secret-named parameters (built-in heuristics + operator-supplied terms) get a masked sample and are annotated (`format: password`, `x-redacted`) in the spec; free-text bearer tokens and URL query strings are stripped from descriptions. |

## Exports

BCL-only — this tool has **no export dependency chain** (no ClosedXML/PdfSharp/MigraDoc). All formats
are produced as plain strings using the base class library and Newtonsoft.Json, so the tool ships as a
single DLL. Redaction is applied before anything is written.

| Format | What you get |
|---|---|
| **Markdown** | A rendered reference with the API detail and parameter/response grids, for repos and wikis. |
| **HTML** | A self-contained, theme-aware (light/dark) page with content escaped; opens offline. |
| **JSON (raw)** | The raw documentation model, for programmatic use. |
| **OpenAPI JSON** | The best-effort OpenAPI 3.0-style spec, for portals and codegen tooling. |

> Word/PDF via the sanctioned ClosedXML/PdfSharp chains are a documented **future** extension — this
> tool does not ship them today.

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
dotnet build src\Tools\XrmToolSuite.ApiDocumentationBuilder\XrmToolSuite.ApiDocumentationBuilder.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **API Documentation Builder**. For manual/other-machine installs copy
only `XrmToolSuite.ApiDocumentationBuilder.dll` into `%AppData%\MscrmTools\XrmToolBox\Plugins` (flat in
the Plugins root, not a subfolder), `Unblock-File` it, and restart. Do **not** copy `Newtonsoft.Json.dll`
or any `Microsoft.Xrm.*` / `Microsoft.Crm.*` DLLs — XrmToolBox already ships them.

Full steps and troubleshooting: [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite-wide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to the environment whose Custom APIs you want to document.
2. *(Optional)* Add any extra **redaction terms** — comma/semicolon-separated name fragments for parameters
   your org treats as secret (added to the built-in `secret`/`token`/`apikey`/… heuristics); matching
   parameter values are masked as `***REDACTED***` in every output.
3. **Load Custom APIs** — read and documented off the UI thread; they appear in the left list, all checked.
4. In the left panel, **search** and **check/uncheck** the APIs you want (or use **Select all**). The
   preview and exports include only the checked APIs.
5. Preview the Markdown / HTML source / OpenAPI JSON; click **Open in browser** to see the rendered HTML.
6. **Export** to Markdown, HTML, raw JSON, or OpenAPI JSON — redaction is applied to every output.

## Notes & limitations

- **Read-only** — the tool documents metadata and **never invokes** an API. To test-invoke a Custom
  API, use the Custom API Explorer.
- **BCL-only** — no third-party export dependencies; the tool ships as one DLL.
- The OpenAPI spec is **best-effort** from Custom API metadata, not a guaranteed live contract — the
  spec's own description states this.
- Redaction masks secret-named parameters and strips bearer tokens / URL query strings, and is
  user-controllable, but operators should still review exports before sharing widely.
- The SDK-free model + redaction + emitters (`Api/ApiModels.cs`, `Api/Redactor.cs`,
  `Api/ApiDocEmitters.cs`, `Api/OpenApiEmitter.cs`) are unit-tested
  (`testing/UnitTests/ApiDocumentationBuilderTests.cs`); the SDK collector (`Api/ApiCollector.cs`) and
  the WinForms host are manual-tested against a live connection.
- Unavailable metadata degrades to documented notes rather than a crash; System Customizer or higher is
  recommended for complete results.
