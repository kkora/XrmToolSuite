# 📋 Form Performance Analyzer

An **XrmToolBox** plugin that scores model-driven **main forms** by static FormXML "heaviness"
into Light/Moderate/Heavy/Critical bands with targeted optimization recommendations, and
compares two forms side by side. **Read-only** — scoring is fully offline; the only Dataverse
reads are retrieving the forms and business-rule counts.

## Features

| Area | What it analyzes |
|---|---|
| **Form inventory & ingestion** | All main forms (`systemform`, `type = 2`) retrieved via `RetrieveAll` off the UI thread, scoped by an optional table picker; FormXML parsed into a structured `FormModel` with no live-service dependency (malformed/blank XML degrades to a single warning, band Light) |
| **Component metrics** | Per-form counts of tabs and sections (visible vs hidden-by-default), fields (above-the-fold vs hidden), PCF/custom controls, subgrids, quick-view controls, JavaScript libraries, event handlers (onload/onchange/tabstatechange), and active form-scoped business rules (`workflow`, `category = 2`) |
| **Scoring & banding** | `FormScorer.Score` computes a deterministic, weighted, capped 0–100 score; Light/Moderate/Heavy/Critical bands with a colour cue in the grid. Weights/thresholds are editable in a settings dialog (reset-to-defaults) and persisted as a POCO |
| **Recommendations** | Targeted suggestions ("collapse/lazy-load N tabs", "reduce M above-the-fold fields", "defer subgrid load", "consolidate K script libraries"), each citing the metric that triggered it and tagged Quick win / Structural with an effort estimate |
| **Drill-down & compare** | A per-form metric breakdown showing each metric's contribution to the score; selecting exactly two forms opens a side-by-side comparison with per-metric deltas. A summary shows count per band and the top-10 heaviest forms |

The scoring and FormXML parsing engine is UI-free and covered by the SDK-free unit tests
(`testing/UnitTests/FormPerformanceAnalyzerTests.cs`).

## Exports

CSV and HTML only (hand-written — no ClosedXML/PdfSharp chain). Export covers forms, metrics,
scores, bands, and recommendations; the file write runs off the UI thread.

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with Documentation,
Report an issue, and a support link, each opened via `Process.Start`. The control implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same
GitHub project (`kkora/XrmToolSuite`).

## Build & install

On the machine that runs XrmToolBox, build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.FormPerformanceAnalyzer\XrmToolSuite.FormPerformanceAnalyzer.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **Form Performance Analyzer**.

This is a **single-DLL** tool — CSV/HTML export is hand-written, so it ships no ClosedXML or
PdfSharp/MigraDoc dependency chain. `XrmToolSuite.FormPerformanceAnalyzer.dll` (shared core
compiled in) is the only file you deploy.

See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) for manual-install steps and troubleshooting, and the
suite-wide [`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md) for more.

## Usage

1. Connect to your Dataverse environment (System Customizer or higher recommended).
2. Optionally scope the scan to a table set (an empty selection defaults to all main forms after
   a confirmation of intent), then run the analysis.
3. Review the ranked grid and the band-distribution summary.
4. Open a form's metric breakdown to see what drives its score, or select two forms to compare.
5. Export to CSV or HTML.

## Notes & limitations

- **Read-only:** no destructive operations; scoring is fully offline from parsed FormXML.
- `FormScorer.Score` is a pure, deterministic function — identical input always yields an
  identical score.
- Malformed or blank FormXML degrades to a single warning finding (band Light) rather than a
  crash.
