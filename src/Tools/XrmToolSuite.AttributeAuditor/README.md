# 🔍 Attribute Auditor

An **XrmToolBox** plugin that audits the custom columns (attributes) of a Dataverse environment for
usage, so unused ones can be found and safely retired. It reads every entity's custom attributes, gathers
usage evidence from four signal sources, and classifies unused custom columns as retirement candidates.
**Read-only** (reads metadata and component definitions; never modifies schema).

## Features

Each signal marks a column "used" with human-readable evidence; a custom, unmanaged column with **no**
signal is a retirement candidate.

| Usage signal | How it's detected |
|---|---|
| **Form** | `datafieldname` bindings extracted from formxml; evidence records the form name |
| **View / saved query** | Fetchxml attributes / conditions / orders plus layout cells, unioned |
| **Process** | Workflow, business-rule and cloud-flow definitions, matched by whole-token reference over `xaml` + `clientdata` (not substring) |
| **Field security** | Secured attributes (`IsSecured`), read directly from metadata |

- **Scope:** a "Custom only" toggle limits the audit to custom tables; intersect (N:N) tables are always excluded.
- **Results grid:** each column with its table, logical/display name, type, managed flag, used flag and a
  usage summary of which signals fired. Candidates are highlighted, and:
  - **Sortable** — click any column header to sort; click again to reverse.
  - **Responsive on large environments** — the grid is virtual (renders only visible rows), so a full audit
    of thousands of columns loads, scrolls, sorts and re-filters without freezing.
  - A "Candidates only" toggle focuses the grid on unused custom columns.
- **Exclusions:** an "Exclusions…" dialog excludes whole **tables** whose logical name starts with any of a
  comma-separated prefix list, and any **column** whose logical name starts with any of a second list — to
  drop ISV/managed noise (e.g. `adx_`, `msdyn_`). Exclusions apply as a **live view filter** (they re-filter
  the loaded grid instantly, no re-run) and are honoured by the exports; both lists persist in settings.
- **Companion columns hidden:** auto-generated shadow attributes are never listed — a picklist/boolean/status
  column's virtual `…name` label, and a lookup's `…name` (primary name) and `…type` (EntityName). These carry
  `AttributeOf` and are not independently retirable.
- **Status bar:** shows the active filters and counts, e.g.
  `Filters: custom tables only, exclusions • Tables: 512 total, 486 non-custom, 12 excluded, 34 shown • Columns: 1204 shown (89 used, 1115 candidate(s))`.
- **Classification:** a column is a retirement candidate only when it is custom, unmanaged and unused —
  managed and system columns are marked and never flagged.

## Exports

- **CSV** — the full column inventory (used and unused), RFC-4180 quoted with a UTF-8 BOM and
  formula-injection neutralisation
- **HTML dashboard** — gauge, metric strip and candidate findings, via the shared report projection

Both exports honour the current exclusions, and after a successful export you're offered to open the file in
its default application.

## Help & Support

A right-aligned **Help** button on the toolbar opens a Help & Support dialog (Documentation, Report an
issue, and a support link, each opened in the browser). The **Documentation** link opens this tool's own
guide (this README). The tool implements `IHelpPlugin` and `IGitHubPlugin` pointing at repository
[`kkora/XrmToolSuite`](https://github.com/kkora/XrmToolSuite).

## Build & install

This is a **single self-contained DLL** — it ships no export dependency chain (CSV and the HTML dashboard
are pure string / BCL). The one-step build copies the tool DLL into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.AttributeAuditor\XrmToolSuite.AttributeAuditor.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **Attribute Auditor**. For a manual copy to another machine, copy the single
output DLL into `%AppData%\MscrmTools\XrmToolBox\Plugins` (flat in the Plugins root) and unblock it. See
[`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Open **Attribute Auditor**; optionally enable "Custom only".
3. Optionally open **Exclusions…** to drop noisy table/column prefixes (e.g. `adx_`, `msdyn_`).
4. Run the audit — it runs on a background worker.
5. Review the grid; sort by any column header, toggle "Candidates only" to focus on unused custom columns
   (highlighted), and read each column's usage summary to see which signals fired. The status bar shows the
   active filters and table/column counts.
6. Export the full inventory to CSV, or produce the HTML dashboard — then optionally open the file.

## Notes & limitations

- **Read-only** — reads metadata and component definitions; never modifies schema. The collector degrades
  permission/query failures to empty (fail-soft) rather than throwing, and "used" vs "candidate" is always
  explainable from the listed evidence.
- Usage detection covers forms, views, processes and field security. Calculated/rollup fields and plugin
  steps are not separately scanned; charts, dashboards and field-security-profile membership beyond
  `IsSecured` are not yet covered.
- Planned (not yet built): a data-population check, multi-table/solution scoping, JSON/Excel export, and a
  guarded cleanup path (preview-only delete plan with a dependency check and confirmation dialog).
