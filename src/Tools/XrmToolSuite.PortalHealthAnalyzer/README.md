# 🌐 Portal Health Analyzer

An **XrmToolBox** plugin that inventories a Power Pages website's configuration and scores its
health from metadata alone — broken page/template relationships, dead web files, missing or
duplicate site settings, broken data bindings, and anonymous-access/over-broad-permission risks —
across both the `adx_` and `mspp_` (enhanced data model) schemas. **Read-only:** it never creates,
updates, or deletes.

## Features

| Area | What it checks |
|---|---|
| **Website discovery** | Lists every Power Pages website in the environment (`adx_website` + `mspp_website`) with a schema badge (`[adx]` / `[mspp]`); remembers your last selection |
| **Configuration summary** | Retrieves settings, roles, permissions, pages, templates, snippets, forms, lists, web files and redirects in one pass; shows counts per record type with the active schema labelled |
| **Structural integrity** | Web pages with a missing parent, no page template, or a dangling template reference → High; inactive pages/templates → Medium; web files referenced but absent → High |
| **Data bindings** | Basic/entity forms and entity lists bound to a non-existent or disabled Dataverse table → High (existence resolved via metadata) |
| **Site settings** | Missing required (baseline) site settings → High; duplicate/conflicting settings within a website → Medium (showing the conflicting values) |
| **Security surface** | Anonymous read/write/delete grants → per-permission High plus a Critical roll-up (pointing to the Portal Security Scanner for deep analysis); over-broad Global-scope permissions → Medium |
| **Health score** | Weighted severities produce a 0–100 score with a Low/Medium/High band (any Critical forces High); every actionable finding carries a plain-language recommendation |

Findings describe **configuration risk**, not live availability — scoring is metadata/static
analysis only, with no runtime page hits.

## Exports

The health report exports off the UI thread to:

- **Excel workbook**
- **PDF report**
- **Word document**
- **JSON**
- **CSV**
- **HTML** (self-contained)

## Help & Support

A **Help** button on the right of the toolbar opens a Help & Support dialog with **Documentation**,
**Report an issue**, and a support link, each opened in your browser. The tool implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same GitHub
project (`kkora/XrmToolSuite`).

## Build & install

Fastest path — build straight into your local XrmToolBox on the same machine:

```powershell
dotnet build src\Tools\XrmToolSuite.PortalHealthAnalyzer\XrmToolSuite.PortalHealthAnalyzer.csproj -c Release -p:DeployToXTB=true
```

This is **not** a single-DLL tool: it ships the Excel/PDF/Word export dependency chain (ClosedXML +
PdfSharp/MigraDoc-GDI). The one-step build copies the tool DLL **and** its dependency DLLs into the
XrmToolBox Plugins **root** (never a subfolder, or XrmToolBox silently drops the tool). For manual
distribution and troubleshooting, see [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md). Restart XrmToolBox and
open **Portal Health Analyzer**.

## Usage

1. Connect to the environment hosting the portal (System Customizer or higher recommended).
2. Load the website list and pick the site to analyze (its schema is detected automatically).
3. Run the analysis to retrieve the configuration inventory and compute the health score.
4. Review the summary cards and the categorized issue grid (severities + recommendations).
5. Export to Excel / PDF / Word / JSON / CSV / HTML to share.

## Notes & limitations

- **Read-only by default** — the tool never creates, updates, or deletes portal configuration.
- **Static analysis only** — findings describe configuration risk from metadata; there are no
  runtime page hits, so results are not a live-availability check.
- Supports both `adx_` and `mspp_` schemas; a table that isn't provisioned degrades to an
  informational finding and never throws.
- All Dataverse access runs off the UI thread via `RunAsync` / `RetrieveAll`; the SDK-free scoring
  model is deterministic and unit-tested (`testing/UnitTests/PortalHealthAnalyzerTests.cs`).
- The security checks are a **health-level summary**; for deep portal security analysis use the
  Portal Security Scanner.
