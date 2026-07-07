# 👥 Team Permission Explorer

An **XrmToolBox** plugin that makes team access, membership, and inheritance visible and reviewable — a
per-team profile (members, roles, effective privileges, owned records, inheriting users) plus a
risk-findings list — so you can find over-privileged, empty, and orphaned teams. **Read-only**; only names
and counts are emitted, no secrets.

## Features

- **Browse & filter teams** — the full team list loads via `RetrieveAll`, filterable by team type
  (owner / access / AAD security / AAD office); the filter and last selection persist in settings. An
  in-memory search box filters the loaded grid by name or business unit instantly (no re-query).
- **Members & roles** — a members grid and an assigned-roles grid per team, with member and role counts in
  the team header; members are fetched lazily off the UI thread.
- **Effective table-privilege matrix** — resolved via the shared `PrivilegeEngine.ResolveEffective` over the
  team's grants (deepest scope per privilege), so you see resolved access, not just role names.
- **Inheritance** — the list of users inheriting the team's permissions (team members via
  teammembership → systemuser).
- **Owned records** — a summary of team-owned records by table, using aggregate/count FetchXML grouped by
  `owningteam` (no full retrieves); a failed table degrades to 0.
- **Risk findings** — teams flagged for cleanup, each with a severity and its evidence
  (rules in `TeamRiskRules`):
  - No members (non-AAD) → **Medium**
  - No roles → **Medium**
  - Over-privileged (Deep/Global on ≥10 privileges) → **High**
  - Duplicate role (same role via multiple teams / listed twice) → **Low**
  - Orphaned (0 members AND 0 owned records) → **Medium**
  - otherwise → Info "No team risks detected"
- **Compare two teams** — diff two teams' effective privileges (`PrivilegeEngine.Diff`) plus the roles
  unique to each side, to consolidate duplicates.

The tool shares the effective-privilege engine (`XrmToolSuite.Core.Privileges`) with the Privilege Gap
Analyzer (SEC01) rather than re-deriving privileges. The collector never throws — per-source failures become
progress notes / Info findings.

## Exports

Excel, PDF, CSV, and HTML. A `ReportModel` drives the shared Excel (ClosedXML) and native PDF
(PdfSharp/MigraDoc-GDI) exporters; CSV/HTML via BCL writers. Only names and counts are emitted (no secrets).

## Help & Support

A right-aligned **Help** button opens a Help & Support dialog with **Documentation**, **Report an issue**,
and a support link, each opened in the browser. The tool implements `IHelpPlugin` and `IGitHubPlugin`, so
XrmToolBox's own tool-menu links resolve to the same GitHub project (`kkora/XrmToolSuite`).

## Build & install

This tool is **not** a single-DLL tool — it ships the Excel/PDF export dependency chains (the
ClosedXML + PdfSharp/MigraDoc-GDI DLLs). The one-step build copies the whole chain into the XrmToolBox
Plugins root for you:

```powershell
dotnet build src\Tools\XrmToolSuite.TeamPermissionExplorer\XrmToolSuite.TeamPermissionExplorer.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Team Permission Explorer**. For a manual copy to another machine, copy
**every** DLL from the tool's `bin\Release\net48\` folder — flat in the Plugins root, never a subfolder — or
XrmToolBox silently drops the tool. Full details in [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your environment (System Customizer or higher recommended).
2. Load teams; filter by team type and/or search by name/business unit, then select a team.
3. Review its members, roles, effective privilege matrix, owned-record counts, and risk findings.
4. *(Optional)* **Compare** against a second team to spot duplicates.
5. **Export** the team security report in any of the supported formats.

## Notes & limitations

- Read-only; only names and counts are exported — no secrets.
- One blocked table does not abort the scan: the collector degrades a failed query to an informational
  finding / progress note.
- SDK-free risk rules are unit-tested in `testing/UnitTests/TeamPermissionExplorerTests.cs`.
