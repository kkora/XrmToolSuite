# Solution Complexity Score - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated — metric/effort model & report projection (US-SOLN08-3..5)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/ComplexityScoreTests.cs`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN08-METRIC-01 | Empty solution | no components | score 0, maintainability 100, 0 effort | Automated | Pass |
| TC-SOLN08-METRIC-02 | Exact model | 10 tables, 50 cols, 8 steps, 4 forms | 66 pts → score 11, maint 89, test 8d, upgrade 3d, migration 10d, $17,600/yr | Automated | Pass |
| TC-SOLN08-METRIC-03 | Score caps | 1000 tables | score 100, maintainability 0 | Automated | Pass |
| TC-SOLN08-METRIC-04 | Dimension breakdown | 5 tables, 2 custom APIs | Tables 15 pts, Custom APIs 5 pts | Automated | Pass |
| TC-SOLN08-REPORT-05 | Report projection | 200 tables | scoreWord "complexity", score 100, band High, effort metrics present | Automated | Pass |
| TC-SOLN08-REPORT-06 | Hotspots | wide form / plain solution | wide form flagged Medium; else "No structural hotspots" | Automated | Pass |

## Automated — build-quality score (US-SOLN08-8)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/ComplexityScoreTests.cs`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN08-QUALITY-07 | Empty = perfect | no components | quality 100, High band, no deductions | Automated | Pass |
| TC-SOLN08-QUALITY-08 | Band cutoffs | 80 / 79 / 60 / 59 | High / Medium / Medium / Low | Automated | Pass |
| TC-SOLN08-QUALITY-09 | Exact multi-violation | 10 tbl, 500 col, 40 steps, 30 JS, 12 wf, 2 flows, form 160 | deductions 15+12+6+8+4+5=50 → quality 50, Low band | Automated | Pass |
| TC-SOLN08-QUALITY-10 | Projection metric + clean note | 2 tables | "Quality score" metric present; "Well-structured solution" finding | Automated | Pass |
| TC-SOLN08-QUALITY-11 | Violations become findings | 10 tbl, 40 steps, form 160 | a `Solution Quality` finding "Oversized form" present | Automated | Pass |

## Automated — component collector against a fake connection (US-SOLN08-2)

Executed via `dotnet test testing/CollectorTests/CollectorTests.csproj`. Source: `testing/CollectorTests/ComplexityCollectorTests.cs` (`ComplexityCollector` over a fake `IOrganizationService`).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN08-COL-01 | Component-type tally | solutioncomponent types 1/1/2/10/92/66 | Tables 2, Columns 1, Relationships 1, PluginSteps 1, Pcfs 1 | Automated | Pass |
| TC-SOLN08-COL-02 | JScript web resources | webresourcetype 3/3/1 | JavaScriptWebResources 2 | Automated | Pass |
| TC-SOLN08-COL-03 | Dashboards vs forms | systemform type 0 + type 2 | Dashboards 1, Forms 1 | Automated | Pass |
| TC-SOLN08-COL-04 | Widest form | forms with 2 vs 5 controls | WidestForm 5, WidestFormName "Wide" | Automated | Pass |
| TC-SOLN08-COL-05 | Workflow categories | workflow category 2/5/0/3 | BusinessRules 1, Flows 1, Workflows 2 | Automated | Pass |
| TC-SOLN08-COL-06 | Apps sum | 2 appmodule + 1 canvasapp | Apps 3 | Automated | Pass |
| TC-SOLN08-COL-07 | Views & charts | 2 savedquery + 1 savedqueryvisualization | Views 2, Charts 1 | Automated | Pass |
| TC-SOLN08-COL-08 | Null formxml | form with no formxml | Forms 1, WidestForm 0 (no crash) | Automated | Pass |

## Manual — collector, dashboard, exports, summary (US-SOLN08-1, US-SOLN08-6..7)

Executed in XrmToolBox against a live org; screenshot per case into `screenshots/`.

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN08-M-01 | Tool loads & lists solutions | Open tool, Load solutions | Visible solutions listed in the combo | Manual | Pending |
| TC-SOLN08-M-02 | Score a solution | Select a solution, Score complexity | Off-thread scan; gauge + metrics + hotspots populate | Manual | Pending |
| TC-SOLN08-M-03 | Inventory correctness | Compare counts to the maker portal | Tables/forms/flows/plugin steps match within expected splits | Manual | Pending |
| TC-SOLN08-M-04 | Dashboard export (HTML/PDF) | Export HTML and PDF | Gauge + metric strip + hotspots render; opens in browser/PDF | Manual | Pending |
| TC-SOLN08-M-05 | Effort labeling | Inspect the report metrics | Effort/cost shown as estimates with units (days / $/yr) | Manual | Pending |
| TC-SOLN08-M-06 | Excel / JSON / MD export | Export each | Files open with the metrics and hotspots | Manual | Pending |
| TC-SOLN08-M-07 | AI consent & offline | Executive summary with/without key | Offline by default; AI shows anonymized payload first; key never saved | Manual | Pending |
