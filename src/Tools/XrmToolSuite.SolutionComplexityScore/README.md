# 📊 Solution Complexity Score

An **XrmToolBox** plugin that inventories a single solution's components and computes a weighted
**0–100 complexity score** — with a maintainability score, upgrade/migration/testing effort in
person-days, a rough annual support-cost estimate, and a sibling build-quality score — from the
component tallies via a UI-free, SDK-free scoring engine. **Read-only** (counts components; never
modifies the solution).

## Features

| Area | What it produces |
|---|---|
| **Component inventory** | Tallies tables, columns, relationships, plugin steps, PCF controls, views, charts, JavaScript web resources, forms, dashboards, workflows, flows, business rules, custom APIs and apps (model-driven + canvas) into a plain `ComponentCounts` POCO — every query solution-scoped and fail-soft. The widest form (by control count) is captured as an outlier signal. |
| **Complexity score** | Each dimension contributes `count × weight` points (e.g. Table 3.0, Plugin step 2.5, PCF 3.0, Column 0.2); the total maps to a 0–100 score that saturates at 100 when points reach the max. Per-dimension contributions are exposed as rows. |
| **Maintainability** | `MaintainabilityScore = 100 − ComplexityScore`. |
| **Effort & cost** | Transparent linear estimates from the tallies — testing / upgrade / migration effort in person-days and a rough annual support-cost figure. |
| **Build-quality score** | A separate 0–100 score (higher = better, Low/Med/High band split at 80/60) that starts at 100 and deducts for best-practice violations over the same tallies (oversized forms, plugin-step density, automation sprawl, client-script weight, legacy-workflow reliance, schema sprawl, low maintainability). |
| **Hotspots** | Graded-severity findings — wide form (≥100 controls), high plugin-step count (≥30), large automation surface (≥40), heavy scripting (≥25 JS), large data model (≥50 tables). An unremarkable solution reads as "No structural hotspots". |
| **Dashboard** | A score + band header with a metric strip and a hotspot grid. |

## Exports

- **PDF** (native, MigraDoc/PdfSharp-GDI)
- **HTML** dashboard
- **Excel** workbook
- **JSON**
- **Markdown**
- An **executive summary** — offline-templated by default, with an opt-in AI path

## Help & Support

A right-aligned **Help** button on the toolbar opens a Help & Support dialog (Documentation, Report an
issue, and a support link, each opened in the browser). The tool implements `IHelpPlugin` and
`IGitHubPlugin` pointing at repository [`kkora/XrmToolSuite`](https://github.com/kkora/XrmToolSuite).

## Build & install

This tool is **not** single-DLL — it ships the Excel/PDF export dependency chain (ClosedXML +
PdfSharp/MigraDoc-GDI) into the Plugins root next to the tool DLL. The one-step build copies the whole
chain automatically:

```powershell
dotnet build src\Tools\XrmToolSuite.SolutionComplexityScore\XrmToolSuite.SolutionComplexityScore.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **Solution Complexity Score**. For a manual copy to another machine, copy
**every** DLL from the tool's `bin\Release\net48\` folder — flat in the Plugins root, never a subfolder,
or XrmToolBox silently drops the tool. See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Open **Solution Complexity Score** and pick a solution (visible, non-system solutions load off-thread).
3. Run the inventory — collection runs on a background worker with progress.
4. Review the dashboard: complexity + maintainability + build-quality scores, effort/cost estimates, the
   metric strip and the hotspot grid.
5. Export to PDF / HTML / Excel / JSON / Markdown.

## Notes & limitations

- **Read-only** — no destructive operations, so no confirmation dialog is required.
- The complexity/effort model, the `ComponentCounts` POCO and the report projection are UI-free and
  SDK-free; the collector degrades query failures to zero counts rather than throwing.
- The optional AI executive summary is opt-in behind a **session-only** API key (env var or the
  AI-settings dialog) that is never persisted, and a payload-preview consent dialog shows the anonymized
  JSON (no record data, credentials or environment names) before anything is sent. The chosen
  provider/model-id persist; the key does not.
- Deferred quality signals (naming-prefix consistency, description coverage, managed/unmanaged layering)
  need a collector change and are a phase-2 item.
