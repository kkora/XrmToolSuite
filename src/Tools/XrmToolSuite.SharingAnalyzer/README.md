# 🔓 Sharing Analyzer

An **XrmToolBox** plugin that scans `PrincipalObjectAccess` (record-level sharing) scoped by
table, decodes access-rights masks, and flags excessive or stale sharing — with a
table × principal intensity view and preview-only cleanup recommendations. **Read-only** — the
tool performs no revoke or mutation.

## Features

| Area | What it analyzes |
|---|---|
| **Scoped sharing scan** | Scoped by table by default (a checked-list table picker, with an optional principal filter); a full-environment scan requires an explicit opt-in that warns first. Paging via `RetrieveAll` off the UI thread with progress and cancellation; per-table read failures degrade to an Info note |
| **Shared-records view** | A grid of shared records by table showing who shared with whom and the granted access rights — `accessrightsmask` decoded to named rights (Read/Write/Append/AppendTo/Create/Delete/Share/Assign) and summarized compactly (e.g. `R/W/D`); access-rights summary cards show the score, totals, and the mix of granted rights |
| **Risk detection** | Excessive sharing (> threshold principals per record) → High; sharing with inactive users → Medium; sharing with disabled/empty teams → Medium; statistical-outlier records → Medium; users with unusually high inbound shared access (default > 500 records) → Medium. Findings roll up to a composite score/band via `ScoreCalculator` |
| **Intensity visualization** | A ranked, heat-shaded table × principal grid of (table, principal, share-count) so hotspots are visible without reading every row (no external charting dependency) |
| **Cleanup recommendations** | A **preview-only** list of recommended cleanup actions — the tool never revokes or mutates shares |

The SDK-free logic (`AccessRights`, `SharingModels`, `SharingRiskRules`) is unit-tested; the
collector, UI, and exports are covered by manual cases.

## Exports

Excel, PDF, JSON, HTML, and CSV. Excel/PDF/JSON go through the shared reporters; HTML/CSV via
BCL writers. Export runs off the UI thread via a `SaveFileDialog`.

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with Documentation,
Report an issue, and a support link, each opened via `Process.Start`. The control implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same
GitHub project (`kkora/XrmToolSuite`).

## Build & install

On the machine that runs XrmToolBox, build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.SharingAnalyzer\XrmToolSuite.SharingAnalyzer.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **Sharing Analyzer**.

This tool is **not** single-DLL: it ships the Excel/PDF export dependency chain — the tool DLL
plus its 17 ClosedXML/PdfSharp-MigraDoc-GDI dependency DLLs — flat in the Plugins root (never a
subfolder), or XrmToolBox silently drops it from the Tools list. The one-step build above copies
the whole chain for you.

See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) for manual-install steps and troubleshooting, and the
suite-wide [`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md) for the
full DLL list and export-tool guidance.

## Usage

1. Connect to your Dataverse environment (System Customizer or higher recommended).
2. Pick the tables to scan (and optionally a principal filter); a full-environment scan needs an
   explicit opt-in.
3. Run the scan — progress and cancellation are supported.
4. Review the shared-records grid, summary cards, risk findings, and the table × principal
   intensity view.
5. Export to Excel, PDF, JSON, HTML, or CSV.

## Notes & limitations

- **Read-only:** cleanup recommendations are preview-only; the tool performs no revoke or
  mutation. If a revoke action were ever added it would require an explicit confirmation dialog
  stating scope and record count (suite rule 8).
- Exports carry findings plus aggregate metrics only — not a raw dump of every share — so full
  principal lists are not leaked.
- Scan scoping and thresholds keep the read within Dataverse service-protection limits.
