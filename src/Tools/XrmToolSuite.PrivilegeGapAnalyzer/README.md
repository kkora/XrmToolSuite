# 🔑 Privilege Gap Analyzer

An **XrmToolBox** plugin that answers "why can't this user do X?" — it computes a principal's effective
privileges for a table + operation and shows the precise missing privilege, insufficient scope, or
append-pair reason, with a recommended fix. **Read-only**: it never edits roles, recommendations are
suggestions only, and sensitive principal names are masked in exports.

## Features

- **Pick the subject** — a user, team, or role, loaded via `RetrieveAll`; selection persists in settings.
- **Pick a table + operation** — Create, Read, Write, Delete, Append, Append To, Assign, Share. The
  operation maps to the concrete required privilege(s), including the **Append/AppendTo pair** (a common
  silent gap: append A→B needs Append on A *and* AppendTo on B), with a related-table selector for Append.
- **Effective-privilege calculation** — the union of direct-role and team-inherited privileges, resolved to
  the deepest access scope per privilege (Basic/Local/Deep/Global). Team-inherited entries are labelled with
  their source team/role and flagged `ViaTeam` only when there is no direct grant.
- **Summary grid** — privilege × scope × source, so you see the whole picture, not just the failing one.
- **Verdict & explanation** — each case is classified as Access allowed / Missing privilege / Insufficient
  scope / Team inheritance only / Append mismatch / Business unit boundary, with the exact privilege name and
  scope required vs held, plus a plain-language explanation you can paste into a support ticket.
- **Recommendation** — a single read-only suggested fix (which role to grant / which scope to raise).
- **Compare two principals** — diff two users/teams/roles side by side; highlights privileges present in one
  but not the other, and differing scopes.

The resolution engine (`Privileges/PrivilegeEngine.cs` + `PrivilegeModels.cs`) is UI-free and SDK-free, so
it is unit-tested without a live org and liftable into a console/CI check; the Dataverse collector
(`PrivilegeCollector.cs`) is kept separate.

## Exports

Excel, PDF, CSV, JSON, and HTML. Principal names are masked in every export. Excel (ClosedXML) and native
PDF (PdfSharp/MigraDoc-GDI) go through the shared `ReportModel`; JSON reuses the shared exporter; CSV/HTML
are BCL writers.

## Help & Support

A right-aligned **Help** button opens a Help & Support dialog with **Documentation**, **Report an issue**,
and a support link, each opened in the browser. The tool implements `IHelpPlugin` and `IGitHubPlugin`, so
XrmToolBox's own tool-menu links resolve to the same GitHub project (`kkora/XrmToolSuite`).

## Build & install

This tool is **not** a single-DLL tool — it ships the Excel/PDF export dependency chains (the
ClosedXML + PdfSharp/MigraDoc-GDI DLLs). The one-step build copies the whole chain into the XrmToolBox
Plugins root for you:

```powershell
dotnet build src\Tools\XrmToolSuite.PrivilegeGapAnalyzer\XrmToolSuite.PrivilegeGapAnalyzer.csproj -c Release -p:DeployToXTB=true
```

Then restart XrmToolBox and open **Privilege Gap Analyzer**. For a manual copy to another machine, copy
**every** DLL from the tool's `bin\Release\net48\` folder — flat in the Plugins root, never a subfolder — or
XrmToolBox silently drops the tool. Full details in [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your environment (System Customizer or higher recommended).
2. Pick the subject principal (user / team / role), a table, and an operation (add the related table for
   Append).
3. Review the effective-privilege summary grid, the verdict, and the plain-language explanation.
4. Read the recommended fix; optionally **compare** against a second principal.
5. **Export** the gap report in any of the supported formats.

## Notes & limitations

- Read-only — the tool never edits roles; every recommendation is a suggestion only.
- Sensitive principal names are masked in all exports.
- Append/AppendTo is modelled explicitly across the two related tables (append A→B = Append on A AND AppendTo
  on B).
- The SDK-free engine is covered by xUnit in `testing/UnitTests/`.
