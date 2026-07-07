# 🧱 Managed Solution Impact Checker

An **XrmToolBox** plugin that analyzes how a managed-solution change interacts with existing
solution layers *before* you apply it — active-layer ownership, unmanaged customizations sitting
above managed layers, overwrite and deletion (data-loss) risk, and path-aware Upgrade / Update /
Patch / Holding semantics — rolled into an impact score, a pre-upgrade checklist, and rollback
guidance in a CAB-ready report. **Read-only:** it never imports, upgrades, patches, or deletes.

## Features

| Area | What it checks |
|---|---|
| **Layer analysis** | Loads managed solutions and captures active-layer ownership per component; flags unmanaged customizations sitting above a managed layer (an admin override an upgrade might reassert) |
| **Overwrite impact** | On a delete-capable path (Upgrade / Holding) a component with an active unmanaged layer raises a High "component would be overwritten"; Update / Patch do not escalate |
| **Deletion / data-loss** | Removed tables → Critical (data loss), removed columns → High, other removed components → Medium, each noting the data-loss risk |
| **Path-aware semantics** | Only Upgrade (or an applied Holding upgrade) deletes components missing from the incoming solution; Update / Patch surface a single informational note instead of deletion findings. Holding is treated as eventually-deleting |
| **Dependencies & publisher** | Missing dependencies (via `RetrieveMissingDependencies`) → High; a source/target publisher-prefix mismatch → Medium |
| **Managed properties** | Components with restrictive managed properties (non-customizable post-import) are listed in a Medium finding |
| **Impact score & report** | Aggregates layering / deletion / overwrite / dependency / publisher / managed-property findings into a Low/Medium/High band (any Critical forces High), with a pre-upgrade checklist and rollback guidance |

## Exports

The analysis exports off the UI thread to four formats for a CAB / change record:

- **Excel workbook**
- **PDF report**
- **JSON**
- **HTML** (self-contained)

## Help & Support

A **Help** button on the right of the toolbar opens a Help & Support dialog with **Documentation**,
**Report an issue**, and a support link, each opened in your browser. The tool implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same GitHub
project (`kkora/XrmToolSuite`).

## Build & install

Fastest path — build straight into your local XrmToolBox on the same machine:

```powershell
dotnet build src\Tools\XrmToolSuite.ManagedSolutionImpactChecker\XrmToolSuite.ManagedSolutionImpactChecker.csproj -c Release -p:DeployToXTB=true
```

This is **not** a single-DLL tool: it ships the Excel/PDF export dependency chain (ClosedXML +
PdfSharp/MigraDoc-GDI). The one-step build copies the tool DLL **and** its dependency DLLs into the
XrmToolBox Plugins **root** (never a subfolder, or XrmToolBox silently drops the tool). For manual
distribution and troubleshooting, see [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md). Restart XrmToolBox and
open **Managed Solution Impact Checker**.

## Usage

1. Connect to your environment (System Customizer or higher recommended).
2. Click **Load solutions** and pick the managed solution to analyze.
3. Choose the deployment path (Upgrade / Update / Patch / Holding).
4. Run the analysis and review the layer-analysis dashboard, impact score, and per-path findings grid.
5. Export the checklist + rollback guidance to Excel / PDF / JSON / HTML for your change record.

## Notes & limitations

- **Read-only by default** — the tool never imports, upgrades, patches, or deletes; it only analyzes.
- Every Dataverse call runs off the UI thread via `RunAsync` / `RetrieveAll`; the collector degrades
  permission/query failures to informational findings and never throws.
- The layer-analysis algorithm is UI-free, deterministic, and unit-tested
  (`testing/UnitTests/ManagedSolutionImpactCheckerTests.cs`).
- Overlaps with the Deployment Risk Analyzer on deletion/data-loss and missing-dependency concepts,
  but goes deeper on solution *layering* (active-layer ownership, unmanaged-above-managed, and
  explicit Upgrade/Update/Patch/Holding delete-vs-overwrite semantics).
