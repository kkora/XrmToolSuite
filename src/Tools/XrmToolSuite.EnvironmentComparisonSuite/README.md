# ⚖️ Environment Comparison Suite

An **XrmToolBox** plugin that compares a **source** and a **target** Dataverse environment across
every component class, classifies each difference (Missing / Extra / Changed / Managed-vs-Unmanaged),
rolls up a weighted difference score with severities, and exports the comparison report. Use it to
validate a release, explain a support incident, and see exactly what drifted. **Read-only:** it never
writes to either environment, and secret-typed values are masked everywhere.

## Features

| Area | What is compared |
|---|---|
| **Solutions & publishers** | Solution versions, publishers/prefixes, and managed/unmanaged layering drift. |
| **Schema** | Tables, columns, relationships, and alternate keys — classified Missing/Extra/Changed, with changed-property lists (datatype, required level, cascade). |
| **UI configuration** | Forms, views, charts, and dashboards; large FormXML/LayoutXML is compared by stable content hash, not raw XML. |
| **Security** | Roles compared by **privilege set** (a stable hash of sorted privilege+depth pairs, not just name); teams, business units, and users. |
| **Automation & code** | Plugin assemblies, steps (matched by message+entity+stage), images, workflows, business rules, flows, and custom APIs. |
| **Bindings & content** | Environment-variable definitions/values, connection-reference bindings, and web-resource/JavaScript content hashes. |
| **Classification & score** | Every component classified via `ismanaged`/layering; an overall weighted difference score with severities Critical/High/Medium/Low/Info. |
| **Review** | A difference grid filterable by category/classification/severity and a side-by-side before/after detail viewer for source vs target, with a severity-ordered recommendation panel. |

A component-category selector scopes a run: unselected classes are skipped and never retrieved. The
diff engine is UI-free/SDK-free and unit-tested; the Dataverse collector is fail-soft per category
(a query or permission gap degrades to an informational finding), pages via `RetrieveAll`, runs off
the UI thread, and reports progress per category.

## Exports

Run off the UI thread; masked values stay masked in every export.

| Format | Notes |
|---|---|
| **Excel** | Via the shared reporters. |
| **PDF** | Native PDF via the shared reporters. |
| **JSON** | Machine-readable comparison payload. |
| **HTML** | Self-contained, theme-aware (light/dark), opens offline. |

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with **Documentation**,
**Report an issue**, and a support link, each opened via `Process.Start`. The tool implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same GitHub
project (`kkora/XrmToolSuite`).

## Build & install

This tool is **not** a single-DLL tool — it ships the Excel/PDF export dependency chain (the tool
DLL plus its ClosedXML + PdfSharp/MigraDoc-GDI dependency DLLs, flat in the Plugins root, never a
subfolder). The one-step build copies the whole chain for you:

```powershell
dotnet build src\Tools\XrmToolSuite.EnvironmentComparisonSuite\XrmToolSuite.EnvironmentComparisonSuite.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Environment Comparison Suite**. Full build/install/troubleshooting
details are in [`./DEPLOYMENT.md`](./DEPLOYMENT.md); the suite-wide guide (including why the export
DLLs must sit in the Plugins root) is in
[`../../../Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your **source** environment as the primary connection.
2. Add the **target** environment via the dual-connection prompt (`TargetOrganization`) — the primary connection is not replaced.
3. Toggle the component categories you want to compare.
4. Run the comparison — both loads run off the UI thread with progress and cancellation.
5. Filter the difference grid, inspect changes in the side-by-side viewer, review the recommendations, then **Export** (Excel/PDF/JSON/HTML).

## Notes & limitations

- **Read-only** — the tool never writes to either environment.
- **Secret-typed environment-variable values are masked** in the grid, the detail viewer, and every export; a value change is still detected via the masked placeholder.
- The target uses the suite dual-connection pattern (`RaiseRequestConnectionEvent` with `actionName="TargetOrganization"`, handled in `UpdateConnection` without replacing the primary).
- Large FormXML/LayoutXML/web-resource content is compared by content hash, so a definition change is detected without storing or exporting the payload.
