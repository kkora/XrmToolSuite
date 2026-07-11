# Attribute Auditor — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/04-Dataverse-Administration/ADMIN10.AttributeAuditor.md`](../backlog/04-Dataverse-Administration/ADMIN10.AttributeAuditor.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.AttributeAuditor` · **Area tag:** `ADMIN10`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (GUI/Dataverse runtime) — pending manual sign-off.

Audits the custom columns (attributes) of a Dataverse environment for usage, so unused ones can be found
and safely retired. It reads every entity's custom attributes, then gathers usage evidence from **forms**
(bound controls), **views / saved queries** (fetchxml attributes/conditions/orders and layout cells),
**processes** (workflow / business-rule / cloud-flow definitions, matched by whole-token reference), and
**field security** (secured attributes). A custom, unmanaged column with no evidence is classified a
retirement candidate; managed and system columns are never candidates. Results show in a filterable grid
(custom-only and candidates-only toggles, candidates highlighted), and export to CSV (full column inventory)
and a shared HTML dashboard. Read-only (reads metadata and component definitions; never modifies schema).
The SDK-free usage scanners, classification model, report projection and CSV exporter are unit-tested; the
Dataverse collector is tested headlessly against a fake connection; the WinForms UI and exports are
manual-tested.

---

## EPIC-ADMIN10 — Find and safely retire unused columns across a Dataverse environment `[Implemented]`
> **As** a **CUST** / **ADM**, **I want** to identify columns (attributes) that no longer appear to be used
> anywhere, **so that** I can reduce schema clutter, form/API payload size and maintenance cost — and retire
> them safely with an auditable trail.

**Outcome:** a per-table classification of each custom column by usage evidence, with a filterable grid,
CSV + HTML export, and evidence-based, human-confirmed "unused" calls. The guarded cleanup path (generate a
delete plan/solution) remains planned.

---

## FEAT-ADMIN10-0 — Scaffold & shared wiring `[Implemented]`
- **US-ADMIN10-0.1** `[Implemented]` The tool loads in XrmToolBox with connection, settings and background
  execution wired via `BaseToolControl`.
  - **AC:** Derives from `BaseToolControl`; the audit runs through `RunAsync`/`ExecuteMethod`; settings
    load in `_Load` and persist in `ClosingPlugin`; a right-aligned Help button is added via
    `CreateHelpButton`. *(Manual — live XTB host; MEF load asserted by the UI smoke test.)*
- **US-ADMIN10-0.2** `[Implemented]` No template leftovers ship — real identity and audit entry points replace
  the scaffold placeholders.
  - **AC:** `UserName`/`RepositoryName`/`HelpUrl` point at `kkora/XrmToolSuite`; no `your-github-username`
    and no "Load sample" command remain. **Automated** — MEF/name load asserted by `testing/UiSmokeTests`.
- **US-ADMIN10-0.3** `[Implemented*]` The Help dialog's **Documentation** link opens this tool's own guide
  (not the suite readme).
  - **AC:** `CreateHelpButton("Attribute Auditor", ToolDocsUrl)` passes a per-tool docs URL through the shared
    `ShowHelpDialog`, which uses it for the Documentation link; `HelpUrl` points at the same page. *(Manual —
    GUI, `TC-ADMIN10-M-11`.)*

## FEAT-ADMIN10-1 — Select audit scope `[Implemented*]`
- **US-ADMIN10-1.1** `[Implemented*]` A "Custom only" toggle limits the audit to custom tables; intersect (N:N) tables
  are always excluded.
  - **AC:** `AttributeUsageCollector.Collect(..., customEntitiesOnly, ...)` filters entities by
    `IsCustomEntity` when set and always drops `IsIntersect`; the toggle round-trips via `AuditSettings`.
    **Automated** for the scope filter (`TC-ADMIN10-COL-08`); the toggle/persistence is *manual* (GUI).
- **US-ADMIN10-1.2** `[Planned]` Multi-table select and solution scoping, and choosing which usage signals
  count, are not yet built — the audit runs across the whole environment (optionally custom-only).
- **US-ADMIN10-1.3** `[Implemented]` An "Exclusions…" dialog lets me exclude whole **tables** whose logical
  name starts with any of a comma-separated list of prefixes, and any **column** whose logical name starts
  with any of a second comma-separated list — to drop noise (e.g. ISV `adx_`/`msdyn_` prefixes) from the audit.
  Exclusions apply as a **live view filter**: clicking OK re-filters the loaded grid (and exports) immediately,
  no re-run required.
  - **AC:** The control filters the loaded `AuditResult` by the prefixes in `PopulateGrid`/`ApplyExclusions`
    (case-insensitive `StartsWith`, blank entries ignored); both lists round-trip via
    `AuditSettings.ExcludeTablePrefixes`/`ExcludeColumnPrefixes`. The `AttributeUsageCollector.Collect(...,
    excludeTablePrefixes, excludeColumnPrefixes, ...)` overload applies the same filter server-side and is
    **Automated** (`TC-ADMIN10-COL-09/10`); the dialog + live re-filter is *manual* (`TC-ADMIN10-M-09`).
- **US-ADMIN10-1.4** `[Implemented*]` The status bar reports table-level scope: total tables, non-custom
  (system) tables, excluded tables, and tables currently shown — plus the shown column counts.
  - **AC:** `AttributeUsageCollector` records `AuditResult.TotalTables`/`NonCustomTables` (env totals, minus
    N:N intersect tables); `UpdateStatusCounts` renders "Tables: … total, … non-custom, … excluded, … shown
    • Columns: …". **Automated** for the counts (`TC-ADMIN10-COL-11`); the status render is *manual*
    (`TC-ADMIN10-M-13`).

## FEAT-ADMIN10-2 — Detect column usage signals `[Implemented]`
Each signal marks a column "used" with human-readable evidence; a column with no signals is a candidate.
- **US-ADMIN10-2.1** `[Implemented]` Columns bound on any **form** are detected.
  - **AC:** `UsageScanners.FormColumns` extracts `datafieldname` bindings from formxml; the collector marks
    them `UsageSignal.Form` with the form name. **Automated** — `TC-ADMIN10-SCAN-01`, `TC-ADMIN10-COL-02`.
- **US-ADMIN10-2.2** `[Implemented]` Columns used by any **view / saved query** (fetch attributes/conditions/
  orders, or layout cells) are detected.
  - **AC:** `UsageScanners.FetchColumns` + `LayoutColumns` union the referenced names; marked
    `UsageSignal.View`. **Automated** — `TC-ADMIN10-SCAN-02/03`, `TC-ADMIN10-COL-03/04`.
- **US-ADMIN10-2.3** `[Implemented*]` Columns referenced by **processes** (workflows, business rules, cloud
  flows) are detected via whole-token match over `xaml` + `clientdata`.
  - **AC:** `UsageScanners.ReferencesToken` matches the logical name as a delimited token (not a substring),
    marked `UsageSignal.Process`. **Automated** for the token match + xaml case (`TC-ADMIN10-SCAN-04`,
    `TC-ADMIN10-COL-05`); calculated/rollup fields and plugin steps are not separately scanned.
- **US-ADMIN10-2.4** `[Implemented*]` Columns protected by **field security** are treated as used.
  - **AC:** A secured attribute (`IsSecured`) is marked `UsageSignal.FieldSecurity` directly from metadata,
    no query needed. **Automated** — `TC-ADMIN10-COL-06`. *(Charts, dashboards and field-security-profile
    membership beyond `IsSecured` remain `[Planned]`.)*
- **US-ADMIN10-2.5** `[Planned]` An optional **data-population** check (fraction of non-null values via a
  sampled/aggregate query) is not yet built.
- **US-ADMIN10-2.6** `[Implemented]` Auto-generated companion attributes are excluded so each real column
  appears once: a picklist/boolean/status column's virtual `…name` label, and a lookup's `…name` (primary
  name) and `…type` (EntityName) shadows.
  - **AC:** The collector skips any attribute with a non-empty `AttributeOf` (the parent it derives from),
    which marks exactly these system-generated shadows — they are never independently retirable. **Automated**
    — `TC-ADMIN10-COL-12`.

## FEAT-ADMIN10-3 — Results & classification `[Implemented]`
- **US-ADMIN10-3.1** `[Implemented]` A grid lists each column with table, logical/display name, type,
  managed flag, used flag and a usage summary (which signals fired).
  - **AC:** `PopulateGrid` fills `lvResults` from `ColumnAudit`; `UsageSummary()` joins the evidence
    details (e.g. "Form: …; View: …"). **Automated** for the classification/summary
    (`TC-ADMIN10-CLASS-05..09`); the grid render is *manual*.
- **US-ADMIN10-3.2** `[Implemented*]` A "Candidates only" toggle focuses the grid on unused custom columns;
  rows are ordered by table then logical name and candidates are highlighted.
  - **AC:** The toggle filters to `IsRetirementCandidate`; candidate rows render in Firebrick. *(Manual —
    GUI filter/colour.)*
- **US-ADMIN10-3.3** `[Implemented]` Managed and system columns are marked and never flagged as candidates.
  - **AC:** `IsRetirementCandidate => IsCustom && !IsManaged && !IsUsed`; the grid shows the managed flag.
    **Automated** — `TC-ADMIN10-CLASS-07`, `TC-ADMIN10-COL-07`.
- **US-ADMIN10-3.4** `[Implemented*]` The results grid is sortable — clicking a column header sorts by that
  column, clicking again reverses the direction.
  - **AC:** `ColumnClick` toggles `_sortColumn`/`_sortAscending` and re-sorts the virtual-mode backing list
    (`SortView`) with a table+column tie-break. *(Manual — GUI, `TC-ADMIN10-M-08`.)*
- **US-ADMIN10-3.5** `[Implemented*]` The grid stays responsive on large environments (thousands of custom
  columns, e.g. "Custom only" off) instead of freezing / "Not responding".
  - **AC:** `lvResults` runs in **virtual mode** (`VirtualMode = true`, `RetrieveVirtualItem`); `PopulateGrid`
    filters/sorts a backing `List<ColumnAudit>` and sets `VirtualListSize` rather than creating a
    `ListViewItem` per row, so filtering/sorting/exclusions are O(visible-rows). *(Manual — GUI,
    `TC-ADMIN10-M-12`.)*

## FEAT-ADMIN10-4 — Export `[Implemented*]`
- **US-ADMIN10-4.1** `[Implemented]` Export the full audit grid to **CSV** (every audited column, used and
  unused), RFC-4180 quoted with a UTF-8 BOM and formula-injection neutralisation.
  - **AC:** `AuditCsvExporter` writes Table/Column/DisplayName/Type/Custom/Managed/Used/Usage; leading
    `=+-@`/tab prefixed with an apostrophe. **Automated** — `TC-ADMIN10-CSV-12/13`; the Save dialog is *manual*.
- **US-ADMIN10-4.2** `[Implemented*]` Export a shared **HTML dashboard** (gauge, metric strip, candidate
  findings) via the suite `ReportModel` projection.
  - **AC:** `AttributeAuditReport.ToReportModel` maps each candidate to a Low finding and fills the
    metrics/verdict; `HtmlDashboardBuilder.Export` writes the page. **Automated** for the projection
    (`TC-ADMIN10-RPT-10/11`); the rendered HTML is *manual* (`TC-ADMIN10-M-06`). JSON/Excel export remain `[Planned]`
    (the `ReportModel` projection already makes them a small step).
- **US-ADMIN10-4.3** `[Implemented*]` After a successful CSV/HTML export I'm asked whether to open the file,
  and answering Yes opens it in its default application.
  - **AC:** Both export handlers call `BaseToolControl.PromptOpenExportedFile(path)` (Yes/No prompt →
    `Process.Start`, shell failures swallowed). *(Manual — GUI, `TC-ADMIN10-M-10`.)*

## FEAT-ADMIN10-5 — Guarded cleanup `[Planned]`
- **US-ADMIN10-5.1** `[Planned]` Selecting confirmed-unused columns and generating a preview-only cleanup
  solution / delete plan (with a mandatory dependency check and a scope+count confirmation dialog) is not
  yet built.
- **US-ADMIN10-5.2** `[Planned]` An audit log of what was proposed/removed and when is not yet built.

---

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll semantics, Load/SaveSettings, progress
  reporting, Help button). — **Done.**
- Read-only; the usage scanners, classification model, report projection and CSV exporter stay UI-free and
  SDK-free, and the collector degrades permission/query failures to empty (fail-soft) instead of throwing.
  — **Done.**
- Usage detection covers forms, views, processes and field security; "used" vs "candidate" is explainable
  from listed evidence. — **Done.**
- Export formats: CSV (full inventory) + HTML dashboard. — **Done** (JSON/Excel and guarded cleanup are
  `[Planned]`).
- Testing under `testing/Tools/AttributeAuditor/`; SDK-free logic covered by
  `testing/UnitTests/AttributeAuditTests.cs` (scanners/classification/report/CSV) and the collector by
  `testing/CollectorTests/AttributeAuditCollectorTests.cs` against a fake connection — **27 automated cases
  pass.** — **Done** (UI/run/filter/export GUI cases `TC-ADMIN10-M-01..11` pending manual sign-off).
