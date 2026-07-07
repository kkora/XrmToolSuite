# 🧹 Technical Debt Analyzer

An **XrmToolBox** plugin that scans a connected Dataverse environment with eight UI-free analyzers,
scores the accumulated technical debt on a **0–100 scale** with a Low/Medium/High band, and lists
prioritized cleanup findings in a severity-coloured grid. **Read-only** — it queries metadata, plugin
registration, web resources, processes and roles, and never modifies anything.

## Features

The eight analyzers cover the main debt sources; each degrades a failed query or permission gap to an
informational finding rather than aborting the scan.

| Analyzer | What it flags |
|---|---|
| **Unused metadata** | Custom tables with 0 rows (Medium); very wide custom tables (≥ 200 custom columns, Low) |
| **Duplicate artifacts** | Web resources that share a display name (Low) |
| **Deprecated API** | JS web resources referencing `Xrm.Page`, `crmForm`, the 2011 `/Organization.svc` endpoint, `getServerUrl` or `XMLHttpRequest` (Medium) |
| **Orphaned components** | Draft processes never activated (Low) |
| **Dead plugins** | Disabled steps (Low), plugin types with no steps (Low), assemblies with no stepped type (Medium) |
| **Performance** | Active steps on `RetrieveMultiple` (High); synchronous `Update` steps with no filtering attributes (Medium) |
| **Naming violations** | Default `new_` publisher prefixes on tables/columns (Low); undocumented custom tables (Info) |
| **Security** | "Copy of …" roles (Low); secured-column sprawl (Info) |

- **Debt score:** severities are weighted (Critical=25 / High=12 / Medium=5 / Low=2 / Info=0), summed and
  capped at 100, and banded at 15 (Medium) / 40 (High). Debt accumulates — a single Critical does not
  force the High band.
- **Dashboard:** a score + band header, a headline metric strip (total findings plus a per-category
  breakdown), and a detail pane per finding (component, description, recommendation, docs link).
- **Trends tab:** charts the debt score run-over-run per environment from local per-machine JSON snapshots
  (a dependency-free GDI line chart, a "since last run" delta banner, and a confirmation-gated **Clear
  history** action). Trend history is local only — the tool never writes to the org.

## Exports

- **Excel** workbook
- **PDF** report (native, MigraDoc/PdfSharp-GDI)
- **HTML** dashboard
- **JSON**
- **Markdown** cleanup checklist
- An **executive summary** — offline-templated by default, with an auditable, opt-in AI path
- The Trends tab additionally exports the snapshot series to **CSV/JSON**

## Help & Support

A right-aligned **Help** button on the toolbar opens a Help & Support dialog (Documentation, Report an
issue, and a support link, each opened in the browser). The tool implements `IHelpPlugin` and
`IGitHubPlugin` pointing at repository [`kkora/XrmToolSuite`](https://github.com/kkora/XrmToolSuite), so
XrmToolBox's own tool-menu links resolve to the same project.

## Build & install

This tool is **not** single-DLL — it ships the Excel/PDF export dependency chain (ClosedXML +
PdfSharp/MigraDoc-GDI) into the Plugins root next to the tool DLL. The one-step build copies the whole
chain automatically:

```powershell
dotnet build src\Tools\XrmToolSuite.TechnicalDebtAnalyzer\XrmToolSuite.TechnicalDebtAnalyzer.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **Technical Debt Analyzer**. For a manual copy to another machine, copy
**every** DLL from the tool's `bin\Release\net48\` folder — flat in the Plugins root, never a subfolder,
or XrmToolBox silently drops the tool. See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Open **Technical Debt Analyzer**; tick which analyzers to run (the unchecked set persists).
3. Run the scan — analysis runs on a background worker with progress and cancellation.
4. Review the severity-coloured findings grid and the dashboard; click a finding for its detail pane.
5. Export to Excel / PDF / HTML / JSON / Markdown, and open the **Trends** tab to see the score over time.

## Notes & limitations

- **Read-only** — never modifies the environment. Trend history is stored as local per-machine JSON under
  the XrmToolBox Settings folder (capped to the most recent 100 runs per environment); **Clear history** is
  confirmation-gated.
- Per-entity row probing is capped (400 by default); the cap is reported as an Info finding when hit.
- The optional AI executive summary is opt-in behind a **session-only** API key that is never persisted,
  and a payload-preview consent dialog shows the anonymized JSON before anything is sent. Component names
  in the payload are toggleable.
- System Customizer or higher is recommended for full results.
