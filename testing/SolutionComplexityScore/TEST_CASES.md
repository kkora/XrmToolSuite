# Solution Complexity Score - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated — metric/effort model & report projection (US-SC-3..5)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/ComplexityScoreTests.cs`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SC-METRIC-01 | Empty solution | no components | score 0, maintainability 100, 0 effort | Automated | Pass |
| TC-SC-METRIC-02 | Exact model | 10 tables, 50 cols, 8 steps, 4 forms | 66 pts → score 11, maint 89, test 8d, upgrade 3d, migration 10d, $17,600/yr | Automated | Pass |
| TC-SC-METRIC-03 | Score caps | 1000 tables | score 100, maintainability 0 | Automated | Pass |
| TC-SC-METRIC-04 | Dimension breakdown | 5 tables, 2 custom APIs | Tables 15 pts, Custom APIs 5 pts | Automated | Pass |
| TC-SC-REPORT-05 | Report projection | 200 tables | scoreWord "complexity", score 100, band High, effort metrics present | Automated | Pass |
| TC-SC-REPORT-06 | Hotspots | wide form / plain solution | wide form flagged Medium; else "No structural hotspots" | Automated | Pass |

## Automated — component collector against a fake connection (US-SC-2)

Executed via `dotnet test testing/CollectorTests/CollectorTests.csproj`. Source: `testing/CollectorTests/ComplexityCollectorTests.cs` (`ComplexityCollector` over a fake `IOrganizationService`).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SC-COL-01 | Component-type tally | solutioncomponent types 1/1/2/10/92/66 | Tables 2, Columns 1, Relationships 1, PluginSteps 1, Pcfs 1 | Automated | Pass |
| TC-SC-COL-02 | JScript web resources | webresourcetype 3/3/1 | JavaScriptWebResources 2 | Automated | Pass |
| TC-SC-COL-03 | Dashboards vs forms | systemform type 0 + type 2 | Dashboards 1, Forms 1 | Automated | Pass |
| TC-SC-COL-04 | Widest form | forms with 2 vs 5 controls | WidestForm 5, WidestFormName "Wide" | Automated | Pass |
| TC-SC-COL-05 | Workflow categories | workflow category 2/5/0/3 | BusinessRules 1, Flows 1, Workflows 2 | Automated | Pass |
| TC-SC-COL-06 | Apps sum | 2 appmodule + 1 canvasapp | Apps 3 | Automated | Pass |
| TC-SC-COL-07 | Views & charts | 2 savedquery + 1 savedqueryvisualization | Views 2, Charts 1 | Automated | Pass |
| TC-SC-COL-08 | Null formxml | form with no formxml | Forms 1, WidestForm 0 (no crash) | Automated | Pass |

## Manual — collector, dashboard, exports, summary (US-SC-1, US-SC-6..7)

Executed in XrmToolBox against a live org; screenshot per case into `screenshots/`.

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SC-M-01 | Tool loads & lists solutions | Open tool, Load solutions | Visible solutions listed in the combo | Manual | Pending |
| TC-SC-M-02 | Score a solution | Select a solution, Score complexity | Off-thread scan; gauge + metrics + hotspots populate | Manual | Pending |
| TC-SC-M-03 | Inventory correctness | Compare counts to the maker portal | Tables/forms/flows/plugin steps match within expected splits | Manual | Pending |
| TC-SC-M-04 | Dashboard export (HTML/PDF) | Export HTML and PDF | Gauge + metric strip + hotspots render; opens in browser/PDF | Manual | Pending |
| TC-SC-M-05 | Effort labeling | Inspect the report metrics | Effort/cost shown as estimates with units (days / $/yr) | Manual | Pending |
| TC-SC-M-06 | Excel / JSON / MD export | Export each | Files open with the metrics and hotspots | Manual | Pending |
| TC-SC-M-07 | AI consent & offline | Executive summary with/without key | Offline by default; AI shows anonymized payload first; key never saved | Manual | Pending |
