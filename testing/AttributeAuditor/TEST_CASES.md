# Attribute Auditor - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated — usage scanners, classification, report & CSV (US-AA-2/3/4)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/AttributeAuditTests.cs` (SDK-free engine — no connection).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-AA-SCAN-01 | Form column scan | formxml with two `datafieldname` controls + a subgrid | the two bound columns; subgrid ignored | Automated | Pass |
| TC-AA-SCAN-02 | Fetch column scan | fetchxml attribute + condition + order | all three column names | Automated | Pass |
| TC-AA-SCAN-03 | Layout column scan | layoutxml with two `<cell name>` | both column names | Automated | Pass |
| TC-AA-SCAN-04 | Whole-token match | body vs logical name (5 cases) | matches only as a delimited token, not a substring | Automated | Pass |
| TC-AA-CLASS-05 | Candidate | custom, unmanaged, no evidence | IsRetirementCandidate = true, "No usage signals" | Automated | Pass |
| TC-AA-CLASS-06 | Used | column with form evidence | IsUsed = true, not a candidate | Automated | Pass |
| TC-AA-CLASS-07 | Never candidate | managed column / system column | IsRetirementCandidate = false | Automated | Pass |
| TC-AA-CLASS-08 | Evidence de-dupe | same evidence added twice | one Evidence entry | Automated | Pass |
| TC-AA-CLASS-09 | Roll-up counts | 1 used + 2 candidates across 2 tables | Total 3, Used 1, Candidates 2, 2 table groups | Automated | Pass |
| TC-AA-RPT-10 | Report projection | 1 used + 1 candidate | ToolName/scoreWord set, Score 1, band Medium, 1 finding, metrics present | Automated | Pass |
| TC-AA-RPT-11 | No candidates | all used | Score 0, band Low, no findings | Automated | Pass |
| TC-AA-CSV-12 | CSV shape | one candidate | header row + escaped data row | Automated | Pass |
| TC-AA-CSV-13 | CSV quoting | evidence containing a comma | field quoted per RFC-4180 | Automated | Pass |

## Automated — Dataverse collector against a fake connection (US-AA-2/3)

Executed via `dotnet test testing/CollectorTests/CollectorTests.csproj`. Source: `testing/CollectorTests/AttributeAuditCollectorTests.cs` (`AttributeUsageCollector` over a fake `IOrganizationService`, metadata via `MetaBuilder`).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-AA-COL-01 | Unused custom column | custom column, no references | retirement candidate | Automated | Pass |
| TC-AA-COL-02 | On a form | form binds the column | used (Form evidence) | Automated | Pass |
| TC-AA-COL-03 | In a view (fetch) | savedquery fetchxml references it | used (View) | Automated | Pass |
| TC-AA-COL-04 | In a view (layout) | savedquery layoutxml cell | used (View) | Automated | Pass |
| TC-AA-COL-05 | In a process | workflow xaml references it | used (Process) | Automated | Pass |
| TC-AA-COL-06 | Field secured | attribute IsSecured | used (FieldSecurity), no query | Automated | Pass |
| TC-AA-COL-07 | Managed column | managed custom column, unused | never a candidate | Automated | Pass |
| TC-AA-COL-08 | System-column ref + scope | form binds a system column; custom col on a system table | candidate untouched; excluded under custom-only, included otherwise | Automated | Pass |

## Manual — UI, run, filter, export (US-AA-0/1/3/4)

Executed in XrmToolBox against a live org; screenshot per case into `screenshots/`.

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-AA-M-01 | Tool loads & connects | Open Attribute Auditor, connect | Loads, connects; toolbar shows Run/toggles/export/close | Manual | Pending |
| TC-AA-M-02 | Run audit off-thread | Click Run audit | Spinner + progress messages; grid populates; status shows total/used/candidate counts | Manual | Pending |
| TC-AA-M-03 | Custom-only scope | Toggle "Custom tables only", re-run | Custom columns on system tables appear/disappear accordingly | Manual | Pending |
| TC-AA-M-04 | Candidates-only filter | Toggle "Candidates only" | Grid shows only unused custom columns; candidates shown in red | Manual | Pending |
| TC-AA-M-05 | CSV export | Export CSV, open in Excel | All audited columns with Used/Usage columns; UTF-8, commas quoted | Manual | Pending |
| TC-AA-M-06 | HTML report | Export report (HTML), open in browser | Dashboard: gauge, metric strip (audited/used/candidates), candidate findings; light/dark aware | Manual | Pending |
| TC-AA-M-07 | Settings persist | Change toggles, close, reopen | "Custom tables only" / "Candidates only" restored from settings | Manual | Pending |

> Deferred (still planned, no case executed): chart/dashboard signals & data population (US-AA-2.4/2.5),
> table multi-select / solution scoping (US-AA-1.1/1.2), JSON/Excel export (US-AA-4), guarded cleanup (US-AA-5).
