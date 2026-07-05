# AI Solution Reviewer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit (net8.0)
- **Result:** 4 Reviewer cases passed, 0 failed, 0 skipped (48 total across the suite).
- **Coverage:** TC-AR-REPORT-01..04 (concern score/band, per-area metrics, tool identity, and the architecture-review AI prompt covering executive summary / recommendations / modernization / refactoring / backlog / sprint plan), tracing to US-AR-2 / US-AR-3.
- **Collectors (headless):** `dotnet test testing/CollectorTests/CollectorTests.csproj` — TC-AR-COL-01..09 drive the four `ReviewCollectors` (plugin, script, automation, ALM/governance) against the shared fake `IOrganizationService`: sync-plugin-without-filter, deprecated `Xrm.Page` in Base64 web resources, legacy workflows / sprawl, unmanaged + `new_`-prefix governance. 9 passed. The AI/HTTP summarization path stays manual.

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
| Automated (collectors, headless) | 9 | 9 | 9 | 0 | 0 |
| Manual (AI/export/UI) | 7 | 0 | 0 | 0 | 7 |

## Verdict

The SDK-free report projection and concern scoring pass and the tool builds `Release` with zero warnings
across the solution. The Dataverse review collectors are now covered headlessly (TC-AR-COL-01..09,
`CollectorTests`); only the AI/HTTP review, exports, and UI remain manual. Manual GUI/Dataverse/AI cases
(TC-AR-M-01..07) are **pending a live org and an AI key** and must be run before release — no manual case
is claimed as passed here.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **AI Solution Reviewer** loads and appears in the Tools list.
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **AI Solution Reviewer** v1.2026.7.1 (Kanchan Kora).
