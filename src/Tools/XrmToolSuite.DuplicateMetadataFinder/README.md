# 🧬 Duplicate Metadata Finder

An **XrmToolBox** plugin that finds duplicate and near-duplicate Dataverse metadata, groups it with
a similarity score, and recommends a primary component to keep — so teams can consolidate redundant
metadata and cut confusion and reporting inconsistency. **Read-only — it recommends only; it never
merges or deletes.**

## Features

| Capability | What you get |
|---|---|
| **Component types scanned** | Columns, option sets, tables, forms, views, business rules, web resources/JavaScript, plugin steps, and relationships. A checkable menu toggles each kind; a "Custom only" toggle drops managed/system components. |
| **Similarity scoring** | Each same-kind pair scored 0–100 from weighted signals — normalized display/schema name via Levenshtein edit-distance ratio + token Jaccard overlap, data-type agreement, option-value overlap — with each contributing factor shown. Comparisons are blocked by kind, so a form is never matched to a column. |
| **Exact content match** | Web-resource/JavaScript duplicates use an exact SHA-256 content hash and are flagged as exact (no false-positive disclaimer); plugin-step duplicates key on the message+entity+stage signature; relationships on schema name + type. |
| **Configurable threshold** | A similarity threshold (clamped 0–100, round-trips via settings) tunes false positives vs. recall; only pairs at/above it are grouped. |
| **Grouping & comparison** | Union-find clusters transitively-linked pairs into groups across containers; the detail pane lists members, per-pair scores, and contributing factors, worst-first. |
| **Recommended primary** | Per group, the most-referenced member is recommended to keep; ties break toward managed then the smaller key, with the reasoning shown. |

The similarity engine, scoring, clustering, and report projection are SDK-free and unit-tested; the
Dataverse collector runs off the UI thread via `RunAsync`/`RetrieveAll` with progress and per-kind
cancellation, and each kind degrades to a note on failure instead of aborting.

## Exports

The scan projects to the shared `ReportModel` (one finding per group, severity from the top pair,
recommended keep in the recommendation); export runs off the UI thread.

| Format | Notes |
|---|---|
| **Excel** | Via the shared reporters. |
| **PDF** | Native PDF via the shared reporters. |
| **JSON** | Machine-readable findings. |
| **HTML** | Self-contained dashboard. |

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
dotnet build src\Tools\XrmToolSuite.DuplicateMetadataFinder\XrmToolSuite.DuplicateMetadataFinder.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Duplicate Metadata Finder**. Full build/install/troubleshooting
details are in [`./DEPLOYMENT.md`](./DEPLOYMENT.md); the suite-wide guide (including why the export
DLLs must sit in the Plugins root) is in
[`../../../Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Choose the component types to scan and set the "Custom only" toggle and similarity threshold as needed.
3. Run the scan — retrieval runs off the UI thread with progress and per-kind cancellation.
4. Review the duplicate groups worst-first; open a group for the side-by-side comparison, per-pair scores, and the recommended primary to keep.
5. **Export** the duplicate report (Excel/PDF/JSON/HTML).

## Notes & limitations

- **Read-only** — recommends only; no merge or delete is ever performed.
- The similarity engine, scoring, clustering, and report projection stay UI-free/SDK-free and degrade query failures to notes.
- The threshold governs recall vs. false positives; exact web-resource/JS matches short-circuit to 100 via content hash and are marked exact.
