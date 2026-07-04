# AI Solution Reviewer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 4 Reviewer cases passed, 0 failed, 0 skipped (48 total across the suite).
- **Coverage:** TC-AR-REPORT-01..04 (concern score/band, per-area metrics, tool identity, and the architecture-review AI prompt covering executive summary / recommendations / modernization / refactoring / backlog / sprint plan), tracing to US-AR-2 / US-AR-3.

```
Passed! - Failed: 0, Passed: 48, Skipped: 0, Total: 48 (whole suite)
```

## Manual run

Not executed in this environment (headless — no XrmToolBox host, Dataverse connection, or AI key). The
fact collectors, the AI review (live HTTPS), the offline fallback, the Word/PDF/HTML/Markdown/JSON
exports, and the WinForms UI must be exercised in a Windows + XrmToolBox session; capture a screenshot per case.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated (report/score) | 4 | 4 | 4 | 0 | 0 |
| Manual (collectors/AI/export/UI) | 7 | 0 | 0 | 0 | 7 |

## Verdict

The SDK-free report projection and concern scoring pass and the tool builds `Release` with zero warnings
across the solution. Manual GUI/Dataverse/AI cases (TC-AR-M-01..07) are **pending a live org and an AI
key** and must be run before release — no manual case is claimed as passed here.
