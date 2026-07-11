# 🗺 ERD Generator

An **XrmToolBox** plugin that generates Dataverse entity-relationship diagrams from live
metadata. Scope by all tables / solution / publisher, pick tables, choose a column-display
level, filter (custom-only / managed-only), preview the Mermaid output, and export to eight
formats. **Read-only** — it reads metadata and never modifies schema.

## Features

| Area | What it produces |
|---|---|
| **Scope & table selection** | Scope the ERD by all tables, one solution, or a publisher; a checked-list table selector (with text filter) adds/removes tables. Relationship targets outside the selected set are listed on lookup columns (marked) but not drawn as edges (both-ends rule) |
| **Schema, keys & columns** | Each table shows display + schema name, primary id/name, and alternate keys (`EntityKeyMetadata`); a column-display setting (Keys+lookups / Important / All) controls which columns appear, each with type and required level |
| **Relationships & cascade** | 1:N, N:1 and N:N relationships plus lookup columns drawn with correct cardinality; cascade behavior (assign/delete/merge/reparent/share/unshare) and required level shown per relationship |
| **Filters & status** | Custom-only, managed-only and relationship-type filters apply to the model before layout without re-querying; custom vs standard tables are visually distinguished; a live text preview reflects the current column-display / filter choices before export |

The SDK-free ERD model, emitters, and the `ErdModel.Apply` filter are UI-free and unit-tested;
the live metadata collector, PNG (GDI+) and PDF (MigraDoc-GDI) renderers are manual-tested.

## Exports

Eight formats, all run off the UI thread:

- **Mermaid** `erDiagram`
- **PlantUML**
- **SVG** (hand-written; colours custom table headers distinctly)
- **PNG** (GDI+ raster)
- **PDF** (native, via the MigraDoc/PdfSharp-GDI chain)
- **HTML** (self-contained; embeds the SVG)
- **Markdown** (embeds a `mermaid` erDiagram block)
- **JSON** (the structured ERD model, written indented / pretty-printed)

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with Documentation,
Report an issue, and a support link, each opened via `Process.Start`. The control implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same
GitHub project (`kkora/XrmToolSuite`).

## Build & install

On the machine that runs XrmToolBox, build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.ErdGenerator\XrmToolSuite.ErdGenerator.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **ERD Generator**.

This tool is **not** single-DLL: it ships the native-PDF (PdfSharp/MigraDoc-GDI) export
dependency chain — the tool DLL plus its five `-gdi` PdfSharp/MigraDoc DLLs — flat in the
Plugins root (never a subfolder), or XrmToolBox silently drops it from the Tools list. The
one-step build above copies the whole chain for you.

See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) for manual-install steps and troubleshooting, and the
suite-wide [`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md) for the
full DLL list and export-tool guidance.

## Usage

1. Connect to your Dataverse environment (System Customizer or higher recommended).
2. Choose a scope — all tables, a solution, or a publisher — and load the table list.
3. Check the tables to include, pick a column-display level, and set any custom/managed or
   relationship-type filters.
4. Review the live Mermaid preview.
5. Export to any of the eight formats.

## Notes & limitations

- **Read-only:** the tool reads metadata only and never modifies schema.
- The ERD model, emitters and filter stay UI-free and SDK-free, and degrade missing metadata to
  notes rather than failing.
- Lookup columns to tables outside the selected set are listed and marked but not drawn as edges.
- PNG (GDI+) and PDF (MigraDoc-GDI) rendering is only verifiable in a live Windows/XrmToolBox
  session and is pending manual sign-off.
