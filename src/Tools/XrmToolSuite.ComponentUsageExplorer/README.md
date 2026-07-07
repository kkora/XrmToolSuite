# 🔍 Component Usage Explorer

An **XrmToolBox** plugin that takes one Dataverse component and shows its full where-used
footprint — everything it *requires*, everything that *depends on* it, a per-type usage count —
plus a single change-safety verdict so you can modify, replace, or delete it without breaking
something unseen. **Read-only:** it inspects and reports; it never writes or deletes.

## Features

| Capability | What you get |
|---|---|
| **Find & select a component** | Search by display name, schema name, GUID, or type; results show component type, owning solution(s), and managed/unmanaged state, narrowable by a component-type filter. Search runs off the UI thread with progress. |
| **Solution membership** | Every owning solution's unique name is listed with its managed/unmanaged state (Active/Default layers excluded), so you know which packages ship the component before you touch it. |
| **Required & dependent inventory** | Both directions of the dependency chain, in separate grids grouped by component type, each row carrying its type and owning solution(s). Sourced from the platform dependency APIs (`RetrieveRequiredComponents`, `RetrieveDependentComponents`, `RetrieveDependenciesForDelete`). |
| **Usage detection** | Dependent object ids resolved to a friendly type + name + owning solution via their base tables (`systemform`, `workflow`, `savedquery`, `plugintype`, …); unresolved ids fall back to a GUID label instead of failing. Managed and cross-solution dependents are highlighted. |
| **Usage-by-type summary** | A grid tallying each dependent type with its count, so you can gauge blast radius at a glance. |
| **Change-safety verdict** | A single deterministic verdict — Safe to change / Change with caution / High impact / Do not delete / Requires dependency review / Requires ALM review — with a banded 0–100 impact score, driven by dependent count, high-value dependent types (forms/flows/plugins/apps), managed/cross-solution state, and whether the component is a table. |
| **Recommendation panel** | A plain-language explanation that names the highest-impact dependents and the required review steps, carried into every export. |

Where the platform dependency APIs can't fully answer (e.g. some Power Pages usage), the footprint
is flagged **DependencyDataIncomplete** and yields a `RequiresDependencyReview` verdict rather than
being read as "safe".

## Exports

Available once an analysis has run; export runs off the UI thread.

| Format | Notes |
|---|---|
| **Excel** | Via the shared reporters. |
| **PDF** | Native PDF via the shared reporters. |
| **JSON** | Carries the verdict, score, and machine-readable findings. |
| **HTML** | A self-contained document (BCL-built), opens offline. |

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
dotnet build src\Tools\XrmToolSuite.ComponentUsageExplorer\XrmToolSuite.ComponentUsageExplorer.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Component Usage Explorer**. Full build/install/troubleshooting
details are in [`./DEPLOYMENT.md`](./DEPLOYMENT.md); the suite-wide guide (including why the export
DLLs must sit in the Plugins root) is in
[`../../../Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Search for the component you're about to change (name, schema name, GUID, or type); filter by type if needed.
3. Select the component — review its owning solution(s), required and dependent grids, and the usage-by-type summary.
4. Read the verdict banner and recommendation panel to understand the blast radius and required review steps.
5. **Export** the usage + impact report (Excel/PDF/JSON/HTML) to attach to a change record.

## Notes & limitations

- **Read-only** — no write or delete path; usage-detection and verdict rules are UI-free and degrade query failures to informational findings rather than aborting.
- An incomplete platform dependency answer is surfaced as `RequiresDependencyReview`, never silently treated as safe.
- Requires System Customizer or higher for full results.
