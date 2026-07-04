# Technical Debt Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated — debt score & report projection (US-TD-3, US-TD-4)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/TechDebtScoreTests.cs`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-TD-SCORE-01 | Empty run scores zero | no findings | score 0, band Low, scoreWord "technical debt" | Automated | Pass |
| TC-TD-SCORE-02 | Weights sum | High+Medium+Low | score 19, band Medium | Automated | Pass |
| TC-TD-SCORE-03 | Critical does not force High | single Critical | score 25, band **Medium** (accumulates) | Automated | Pass |
| TC-TD-SCORE-04 | Accrual bands High + caps | 4×High / 10×Critical | band High; score capped at 100 | Automated | Pass |
| TC-TD-DASH-05 | Dashboard metrics | mixed findings | Metrics carry Total findings + per-category counts | Automated | Pass |

## Automated — analyzers against a fake connection (US-TD-1, US-TD-2)

Executed via `dotnet test testing/CollectorTests/CollectorTests.csproj`. Sources: `testing/CollectorTests/TechDebtAnalyzerTests.cs` (row-driven) and `TechDebtMetadataTests.cs` (metadata-driven, seeded via `MetaBuilder`), over a fake `IOrganizationService`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-TD-COL-01 | Plugin on RetrieveMultiple | active step, message RetrieveMultiple | High "Plugin on RetrieveMultiple" | Automated | Pass |
| TC-TD-COL-02 | Sync Update, no filtering | active step mode 0, message Update, blank filtering | Medium "Synchronous Update plugin without filtering attributes" | Automated | Pass |
| TC-TD-COL-03 | Dead plugin registration | disabled step + step-less type + step-less assembly | Low disabled step, Low step-less type, Medium step-less assembly | Automated | Pass |
| TC-TD-COL-04 | Draft process | workflow type 1 statecode 0 (+ an active one) | Low "Draft process never activated" (draft only) | Automated | Pass |
| TC-TD-COL-05 | Deprecated API | JScript web resource with `Xrm.Page` | Medium "Deprecated API: Xrm.Page" | Automated | Pass |
| TC-TD-COL-06 | Duplicate web resources | two share a display name | Low "Duplicate web-resource display name" | Automated | Pass |
| TC-TD-COL-07 | Copied security role | role "Copy of …" | Low "Ad-hoc copied security role" | Automated | Pass |
| TC-TD-COL-08 | Empty custom table | custom table, row count 0 (aggregate FetchXML) | Medium "Custom table has no data" | Automated | Pass |
| TC-TD-COL-09 | Very wide table | custom table with 200 custom columns | Low "Very wide custom table" | Automated | Pass |
| TC-TD-COL-10 | Default prefix / no description | custom table "new_widget", no description, column "new_field" | Low table prefix + Info no-description + Low column prefix | Automated | Pass |
| TC-TD-COL-11 | Secured columns | custom table with 2 secured columns | Info "Field-level security in use" | Automated | Pass |

## Manual — analyzers, UI, exports, summary (US-TD-1, US-TD-5..7)

Executed in XrmToolBox against a live org; capture a screenshot per case into `screenshots/` (e.g. `TC-TD-M-04-html.png`).

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-TD-M-01 | Tool loads & connects | Open tool, connect | Loads, connects, analyzers listed and checkable | Manual | Pending |
| TC-TD-M-02 | Analyze runs off-thread | Click Analyze | Spinner + progress messages; grid populates; probe cap reported if hit | Manual | Pending |
| TC-TD-M-03 | Unused metadata | Have an empty custom table | 0-row table flagged Medium | Manual | Pending |
| TC-TD-M-04 | Deprecated APIs | Have a JS web resource using `Xrm.Page` | Flagged Medium with the token named | Manual | Pending |
| TC-TD-M-05 | Dead plugins | Have a disabled SDK step | Disabled step flagged Low | Manual | Pending |
| TC-TD-M-06 | Performance | Have a plugin on RetrieveMultiple | Flagged High | Manual | Pending |
| TC-TD-M-07 | Selection persists | Uncheck an analyzer, reopen | Selection restored from settings | Manual | Pending |
| TC-TD-M-08 | Exports | Export PDF/HTML/XLSX/JSON/MD | Each file opens and reflects the score/findings | Manual | Pending |
| TC-TD-M-09 | AI consent & offline | Executive summary with/without key | Offline by default; AI shows the anonymized payload before sending; key never saved | Manual | Pending |
