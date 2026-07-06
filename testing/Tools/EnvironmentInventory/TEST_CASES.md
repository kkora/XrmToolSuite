# Environment Inventory - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

Automated cases live in `testing/UnitTests/EnvironmentInventoryTests.cs` and were executed with
`dotnet test` (16 passed). Manual cases require Windows + XrmToolBox + a live Dataverse org and are pending.

## Automated (executed)

| ID | Case | Traces to | Inputs | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-A1 | Filter by text matches Name/Schema/Type, case-insensitive | US-ADMIN07.4.1 | Hand-built snapshot; `Filter("widget"/"CUSTOM TABLE"/"account")` | Each returns exactly the matching row(s) | Automated | Pass |
| TC-A2 | Filter by category pins category | US-ADMIN07.4.1 | `Filter(null,"Tables",null)` | Only the 2 Tables rows | Automated | Pass |
| TC-A3 | Filter by managed state | US-ADMIN07.4.1 | `Filter(null,null,true/false)` | Managed=2, Unmanaged=2 | Automated | Pass |
| TC-A4 | Combined text+category+managed filter | US-ADMIN07.4.1 | `Filter("a","Tables",true)` | Single row (Account) | Automated | Pass |
| TC-A5 | CountByCategory / Categories / Total | US-ADMIN07.4.1 | Sample snapshot | Solutions=1, Tables=2, Configuration=1; 3 categories; Total=4 | Automated | Pass |
| TC-A6 | CSV header + rows | US-ADMIN07.5.1 | `ToCsv(sample)` | Correct header; managed row present | Automated | Pass |
| TC-A7 | CSV RFC-4180 escaping | US-ADMIN07.5.1 | Name with comma/quotes/newline | Quoted, embedded quotes doubled | Automated | Pass |
| TC-A8 | JSON well-formed + round-trippable | US-ADMIN07.5.1 | `ToJson(sample)` parsed with JsonDocument | total=4, environmentName=DEV, 4 items, Tables count=2 | Automated | Pass |
| TC-A9 | JSON escapes quotes/control chars | US-ADMIN07.5.1 | Name `a"b\tc` | Parses back to original string | Automated | Pass |
| TC-A10 | Markdown has summary + category headers | US-ADMIN07.5.1 | `ToMarkdown(sample)` | `## Summary`, `## Solutions/Tables/Configuration`, unavailable-sources note | Automated | Pass |
| TC-A11 | HTML self-contained + category headers | US-ADMIN07.5.1 | `ToHtml(sample)` | `<!DOCTYPE html>`, inline `<style>`, `<h2>` per category, no `http://` | Automated | Pass |
| TC-A12 | HTML escapes markup | US-ADMIN07.5.1 | Name `<script>x</script>` | Rendered as `&lt;script&gt;`, no raw tag | Automated | Pass |
| TC-A13 | No secret/value column in any export | US-ADMIN07.3.2 | All four formats of sample | No `Secret`/`Value` header; no `secretvalue`/`environmentvariablevalue` text | Automated | Pass |
| TC-A14 | Report model projection (zero score, metrics) | US-ADMIN07.5.1 | `ToReportModel(sample)` | ToolName/ScoreWord set, Score=0, Band=Low, no findings, Total/Tables metrics | Automated | Pass |
| TC-A15 | Metrics include total + unavailable sources | US-ADMIN07.5.1 | `ToMetrics(sample)` | Total=4; "Unavailable sources" contains PCF | Automated | Pass |

## Manual (pending — Windows + XrmToolBox + Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-M1 | Tool loads and connects | US-TT-0.1 | Open tool, connect to an org | Tool appears in list, loads, Sources dropdown defaults to all checked | Manual | Pending |
| TC-M2 | Collect inventory off-thread with progress | US-ADMIN07.1.1 / .2.2 | Click "Collect inventory" | Spinner shows progress messages; UI stays responsive; grid fills; status shows total count | Manual | Pending |
| TC-M3 | Solutions & tables captured | US-ADMIN07.1.1 / .1.2 | Filter category to Solutions, then Tables | Solutions+publishers listed with version/managed; tables listed with column count in detail | Manual | Pending |
| TC-M4 | Security / automation / web-dev / config captured | US-ADMIN07.2.x / .3.x | Cycle category filter | Roles/users/teams/BUs, plugins/steps/workflows, web resources/PCF/custom APIs, env vars/conn refs all present | Manual | Pending |
| TC-M5 | Unavailable source degrades gracefully | US-ADMIN07.2.2 | Collect against an org lacking a table / with restricted privileges | Missing source noted in status bar; collection still completes | Manual | Pending |
| TC-M6 | Search + managed filter (client-side) | US-ADMIN07.4.1 | Type in Search, change Managed dropdown | Grid filters instantly without re-querying Dataverse | Manual | Pending |
| TC-M7 | Detail panel | US-ADMIN07.4.2 | Select a row | Detail panel shows normalized fields + Details dictionary | Manual | Pending |
| TC-M8 | Env-var secrets never shown/exported | US-ADMIN07.3.2 | Inspect an environment variable of type Secret; export | Only the definition/type shown; no value anywhere in UI or export files | Manual | Pending |
| TC-M9 | Export CSV/JSON/Markdown/HTML | US-ADMIN07.5.1 | Export ▼ each format | Files write; CSV opens in Excel (BOM); HTML renders standalone; MD/JSON well-formed | Manual | Pending |
| TC-M10 | Settings round-trip | US-TT-0.1 | Uncheck a source, set a filter, close + reopen | Selected sources and last filter restored | Manual | Pending |
| TC-M11 | Export Excel (full grid) | US-ADMIN07.5.1 | Export ▼ Excel (*.xlsx) | .xlsx opens in Excel; "Summary" sheet has env/collected/total + category→count; "Items" sheet has header (Category/Type/Name/Schema/Managed/Modified) + one row per component; NO secret/value column | Manual | Pending |
| TC-M12 | Export Word (summary report) | US-ADMIN07.5.1 | Export ▼ Word (*.docx) | .docx opens in Word; shows the Environment Inventory summary report (title, environment, metrics/counts) from the shared ReportModel; no secrets | Manual | Pending |
| TC-M13 | Export PDF (summary report) | US-ADMIN07.5.1 | Export ▼ PDF (*.pdf) | .pdf opens in a viewer; renders the Environment Inventory summary report (native PdfSharp/MigraDoc, no HTML round-trip); no secrets | Manual | Pending |
| TC-M14 | Excel/Word/PDF deps load in-process | US-ADMIN07.5.1 | With ~70 tools installed, run all three exports in one session | ClosedXML + PdfSharp/MigraDoc-GDI resolve from the Plugins root; no assembly-load error; tool stays in the Tools list | Manual | Pending |
