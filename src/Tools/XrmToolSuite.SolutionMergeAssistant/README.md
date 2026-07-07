# 🔀 Solution Merge Assistant

An **XrmToolBox** plugin that compares two or more solutions from one environment and surfaces every
conflict *before* you merge them — duplicate/overlapping components, version and publisher
mismatches, managed-vs-unmanaged clashes, and environment-variable/connection-reference conflicts —
rolled up into a single merge-risk verdict with a recommended strategy and a merged-component
checklist. **Read-only:** it *recommends* a merge and emits a checklist; it never imports or writes
solutions.

## Features

| Area | What it detects |
|---|---|
| **Solution selection** | Load two or more managed/unmanaged solutions (checked list) with their `ismanaged` flag and version; each solution's components are enumerated once and cached for fast, cancellable pairwise comparison |
| **Duplicate / overlapping components** | Components appearing in more than one selected solution, keyed by `(componenttype, objectid)`, listing every owning solution; web resources, plugin assemblies, plugin steps, forms, views and business rules each map to their own category and severity (plugin assemblies/types/steps High; web resources/forms/views/business rules Medium; base tables/columns Low) |
| **Version / publisher conflicts** | Differing publisher prefixes → Medium (with a recommended standard prefix); differing solution versions → Low, or Medium when the solutions actually overlap |
| **Managed-state conflicts** | A component managed in one solution and unmanaged in another → High `Managed State` finding |
| **Config conflicts** | The same env-var/connection-reference schema name packaged in multiple solutions with **different** definition/value → High; packaged **identically** in more than one → Medium duplicate-ownership; packaged in only one → not a conflict |
| **Merge verdict & strategy** | All conflicts roll into one verdict — **Safe to merge → Merge with warnings → Manual review required → High-risk merge → Do not merge** (any High ⇒ at least High-risk; ≥3 High ⇒ Do not merge) — plus a recommended import order (ascending version), a publisher to standardize on, per-conflict resolution, and a merged-component checklist |

## Exports

The merge report exports off the UI thread to four formats for a CAB / change record or pipeline gate:

- **Excel workbook**
- **PDF report**
- **JSON** (carries the verdict, solutions, metrics, checklist, and a machine-readable conflict list)
- **HTML** (self-contained, theme-aware)

## Help & Support

A **Help** button on the right of the toolbar opens a Help & Support dialog with **Documentation**,
**Report an issue**, and a support link, each opened in your browser. The tool implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same GitHub
project (`kkora/XrmToolSuite`).

## Build & install

Fastest path — build straight into your local XrmToolBox on the same machine:

```powershell
dotnet build src\Tools\XrmToolSuite.SolutionMergeAssistant\XrmToolSuite.SolutionMergeAssistant.csproj -c Release -p:DeployToXTB=true
```

This is **not** a single-DLL tool: it ships the Excel/PDF export dependency chain (ClosedXML +
PdfSharp/MigraDoc-GDI). The one-step build copies the tool DLL **and** its dependency DLLs into the
XrmToolBox Plugins **root** (never a subfolder, or XrmToolBox silently drops the tool). For manual
distribution and troubleshooting, see [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md). Restart XrmToolBox and
open **Solution Merge Assistant**.

## Usage

1. Connect to the environment holding the solutions (System Customizer or higher recommended).
2. Load solutions and check two or more to compare.
3. Run the comparison and review the conflict grid (grouped by category and severity).
4. Read the merge-risk verdict and the recommended merge strategy + checklist.
5. Export to Excel / PDF / JSON / HTML to attach to a change record or gate a pipeline.

## Notes & limitations

- **Read-only default** — the tool never imports or writes solutions; it only compares and recommends.
- The comparison engine is UI-free and SDK-free; the collector degrades query failures to progress
  notes rather than throwing.
- Managed state degrades to the owning solution's `ismanaged` when per-component layering is
  unavailable.
- Missing/required-component completeness (`RetrieveMissingDependencies`) is **out of scope** here —
  it is tracked separately under the Dependency Validator (ALM02).
- The SDK-free comparison is covered by `testing/UnitTests/SolutionMergeAssistantTests.cs`.
