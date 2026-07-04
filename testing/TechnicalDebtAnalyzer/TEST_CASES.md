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
