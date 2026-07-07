# 🛡 Audit Compliance Checker

An **XrmToolBox** plugin that checks org/table/column audit settings (with sensitive-data
heuristics), analyzes audit activity, estimates a storage-growth trend, and produces a 0–100
audit-compliance readiness score with prioritized remediation. **Read-only** — it never changes
audit settings; changed/sample field values are never collected.

## Features

| Area | What it analyzes |
|---|---|
| **Audit settings coverage** | Org, table, and column audit settings in one grid — `organization.isauditenabled`, `EntityMetadata.IsAuditEnabled`, per-`AttributeMetadata.IsAuditEnabled` — with managed/custom shown per row and org status as a banner. `SensitivityHeuristics` flag sensitive tables/columns: a sensitive table without audit → High; a sensitive column without audit on an audited table → Medium |
| **Activity analysis** | `audit` queries date-scoped and paged via `RetrieveAll` off the UI thread (progress + cancellation), pivoted by table/user/date. High delete volume → Medium; security-role/privilege/team-membership changes → Medium; after-hours edits (outside configurable business hours / weekends) → Low |
| **Storage growth trend** | A labelled **estimate** of audit storage growth (records × ~2 KB/record, cumulative by date) — explicitly not billed Dataverse storage |
| **Compliance score** | A deterministic 0–100 readiness score (HIGHER = MORE compliant), banded Low/Medium/High via `ScoreCalculator.BandFor` — a weighted blend of org (25%) / table coverage (30%) / column coverage (25%) / activity (20%), explainable from the listed findings |
| **Recommendations** | Prioritized, read-only remediation text (enable audit on these tables/fields; review these changes) — the tool never changes audit settings |

The SDK-free compliance engine (`Analysis/AuditModels.cs`, `SensitivityHeuristics.cs`,
`AuditComplianceRules.cs`) is deterministic and unit-tested.

## Exports

Excel, PDF, JSON, HTML, and CSV. Excel/PDF via the shared reporters (`ReportModel`); JSON via
`JsonReportExporter`; HTML/CSV via BCL writers. No changed/sample field values appear in
exports; export runs off the UI thread via a `SaveFileDialog`.

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with Documentation,
Report an issue, and a support link, each opened via `Process.Start`. The control implements
`IHelpPlugin` and `IGitHubPlugin`, so XrmToolBox's own tool-menu links resolve to the same
GitHub project (`kkora/XrmToolSuite`).

## Build & install

On the machine that runs XrmToolBox, build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.AuditComplianceChecker\XrmToolSuite.AuditComplianceChecker.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **Audit Compliance Checker**.

This tool is **not** single-DLL: it ships the Excel/PDF export dependency chain — the tool DLL
plus its 17 ClosedXML/PdfSharp-MigraDoc-GDI dependency DLLs — flat in the Plugins root (never a
subfolder), or XrmToolBox silently drops it from the Tools list. The one-step build above copies
the whole chain for you.

See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) for manual-install steps and troubleshooting, and the
suite-wide [`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md) for the
full DLL list and export-tool guidance.

## Usage

1. Connect to your Dataverse environment (System Customizer or higher recommended).
2. Load audit settings coverage — the org/table/column grid and org banner populate.
3. Run the activity analysis (date-scoped) to see change volume by table/user/date and
   high-risk changes.
4. Review the storage-growth estimate, the compliance readiness score with its category
   breakdown, and the prioritized recommendations.
5. Export to Excel, PDF, JSON, HTML, or CSV.

## Notes & limitations

- **Read-only:** the tool never mutates audit settings; there are no destructive operations.
- Sensitive/sample and changed field values are never collected or exported.
- Storage figures are a clearly-labelled estimate from record volume, not billed Dataverse
  storage.
- The compliance score is deterministic — same input yields the same output — and higher means
  more compliant.
