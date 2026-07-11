# Attribute Auditor - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated — usage scanners, classification, report & CSV (US-ADMIN10-2/3/4)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/AttributeAuditTests.cs` (SDK-free engine — no connection).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ADMIN10-SCAN-01 | Form column scan | formxml with two `datafieldname` controls + a subgrid | the two bound columns; subgrid ignored | Automated | Pass |
| TC-ADMIN10-SCAN-02 | Fetch column scan | fetchxml attribute + condition + order | all three column names | Automated | Pass |
| TC-ADMIN10-SCAN-03 | Layout column scan | layoutxml with two `<cell name>` | both column names | Automated | Pass |
| TC-ADMIN10-SCAN-04 | Whole-token match | body vs logical name (5 cases) | matches only as a delimited token, not a substring | Automated | Pass |
| TC-ADMIN10-CLASS-05 | Candidate | custom, unmanaged, no evidence | IsRetirementCandidate = true, "No usage signals" | Automated | Pass |
| TC-ADMIN10-CLASS-06 | Used | column with form evidence | IsUsed = true, not a candidate | Automated | Pass |
| TC-ADMIN10-CLASS-07 | Never candidate | managed column / system column | IsRetirementCandidate = false | Automated | Pass |
| TC-ADMIN10-CLASS-08 | Evidence de-dupe | same evidence added twice | one Evidence entry | Automated | Pass |
| TC-ADMIN10-CLASS-09 | Roll-up counts | 1 used + 2 candidates across 2 tables | Total 3, Used 1, Candidates 2, 2 table groups | Automated | Pass |
| TC-ADMIN10-RPT-10 | Report projection | 1 used + 1 candidate | ToolName/scoreWord set, Score 1, band Medium, 1 finding, metrics present | Automated | Pass |
| TC-ADMIN10-RPT-11 | No candidates | all used | Score 0, band Low, no findings | Automated | Pass |
| TC-ADMIN10-CSV-12 | CSV shape | one candidate | header row + escaped data row | Automated | Pass |
| TC-ADMIN10-CSV-13 | CSV quoting | evidence containing a comma | field quoted per RFC-4180 | Automated | Pass |

## Automated — Dataverse collector against a fake connection (US-ADMIN10-2/3)

Executed via `dotnet test testing/CollectorTests/CollectorTests.csproj`. Source: `testing/CollectorTests/AttributeAuditCollectorTests.cs` (`AttributeUsageCollector` over a fake `IOrganizationService`, metadata via `MetaBuilder`).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ADMIN10-COL-01 | Unused custom column | custom column, no references | retirement candidate | Automated | Pass |
| TC-ADMIN10-COL-02 | On a form | form binds the column | used (Form evidence) | Automated | Pass |
| TC-ADMIN10-COL-03 | In a view (fetch) | savedquery fetchxml references it | used (View) | Automated | Pass |
| TC-ADMIN10-COL-04 | In a view (layout) | savedquery layoutxml cell | used (View) | Automated | Pass |
| TC-ADMIN10-COL-05 | In a process | workflow xaml references it | used (Process) | Automated | Pass |
| TC-ADMIN10-COL-06 | Field secured | attribute IsSecured | used (FieldSecurity), no query | Automated | Pass |
| TC-ADMIN10-COL-07 | Managed column | managed custom column, unused | never a candidate | Automated | Pass |
| TC-ADMIN10-COL-08 | System-column ref + scope | form binds a system column; custom col on a system table | candidate untouched; excluded under custom-only, included otherwise | Automated | Pass |
| TC-ADMIN10-COL-09 | Column-prefix exclusion | two columns, exclude prefix `sys_` | only the non-matching column audited | Automated | Pass |
| TC-ADMIN10-COL-10 | Table-prefix exclusion | table, exclude prefix matching its name | no columns audited (table dropped) | Automated | Pass |
| TC-ADMIN10-COL-11 | Environment table counts | 1 custom + 1 system table | TotalTables 2, NonCustomTables 1, AuditedTables 2 | Automated | Pass |
| TC-ADMIN10-COL-12 | Companion shadow attrs skipped | real column + `…name`/`…type` shadows (AttributeOf set) | only the real column audited | Automated | Pass |

## Manual — UI, run, filter, export (US-ADMIN10-0/1/3/4)

Executed in XrmToolBox against a live org; screenshot per case into `screenshots/`.

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ADMIN10-M-01 | Tool loads & connects | Open Attribute Auditor, connect | Loads, connects; toolbar shows Run/toggles/export/close | Manual | Pending |
| TC-ADMIN10-M-02 | Run audit off-thread | Click Run audit | Spinner + progress messages; grid populates; status shows total/used/candidate counts | Manual | Pending |
| TC-ADMIN10-M-03 | Custom-only scope | Toggle "Custom only", re-run | Custom columns on system tables appear/disappear accordingly | Manual | Pending |
| TC-ADMIN10-M-04 | Candidates-only filter | Toggle "Candidates only" | Grid shows only unused custom columns; candidates shown in red | Manual | Pending |
| TC-ADMIN10-M-05 | CSV export | Export CSV, open in Excel | All audited columns with Used/Usage columns; UTF-8, commas quoted | Manual | Pending |
| TC-ADMIN10-M-06 | HTML report | Export report (HTML), open in browser | Dashboard: gauge, metric strip (audited/used/candidates), candidate findings; light/dark aware | Manual | Pending |
| TC-ADMIN10-M-07 | Settings persist | Change toggles, close, reopen | "Custom only" / "Candidates only" / exclusion prefixes restored from settings | Manual | Pending |
| TC-ADMIN10-M-08 | Sortable grid | Click each column header, click again | Rows sort by that column ascending, then descending on second click (virtual mode) | Manual | Pending |
| TC-ADMIN10-M-09 | Exclusion dialog (live) | Click "Exclusions…", enter table/column prefixes, OK | Matching tables/columns leave the grid immediately (no re-run) and are absent from exports | Manual | Pending |
| TC-ADMIN10-M-12 | Large audit responsive | Run with "Custom only" off (thousands of columns) | Grid loads without "Not responding"; scrolling/sorting/exclusions stay responsive (virtual mode) | Manual | Pending |
| TC-ADMIN10-M-13 | Status-bar counts | Run audit; then apply an exclusion | Status shows "Tables: N total, M non-custom, X excluded, S shown • Columns: …"; counts update on exclusion | Manual | Pending |
| TC-ADMIN10-M-10 | Open after export | Export CSV/HTML, answer Yes on the prompt | Exported file opens in its default app | Manual | Pending |
| TC-ADMIN10-M-11 | Help → Documentation | Click Help, click Documentation | Opens this tool's guide (ADMIN10.AttributeAuditor), not the suite readme | Manual | Pending |

> Deferred (still planned, no case executed): chart/dashboard signals & data population (US-ADMIN10-2.4/2.5),
> table multi-select / solution scoping (US-ADMIN10-1.1/1.2), JSON/Excel export (US-ADMIN10-4), guarded cleanup (US-ADMIN10-5).
