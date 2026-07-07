# AI Solution Reviewer - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org / AI key)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse/AI).

## Automated — report projection & concern score (US-AI10-2, US-AI10-3)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/ReviewReportTests.cs`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-AI10-REPORT-01 | Empty review | no observations | score 0, band Low, scoreWord "concern", tool name set | Automated | Pass |
| TC-AI10-REPORT-02 | Concern weighting | High + Medium | score 17, band Medium | Automated | Pass |
| TC-AI10-REPORT-03 | Per-area metrics | 2 Plugins + 1 ALM | Observations 3, Plugins 2 | Automated | Pass |
| TC-AI10-REPORT-04 | AI prompt sections | — | prompt covers backlog, sprint plan, architecture recommendations | Automated | Pass |

## Automated — review collectors against a fake connection (US-AI10-1)

Executed via `dotnet test testing/CollectorTests/CollectorTests.csproj`. Source: `testing/CollectorTests/ReviewCollectorTests.cs` (the four `ReviewCollectors` over a fake `IOrganizationService`).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-AI10-COL-01 | Sync step, no filtering | sdkmessageprocessingstep mode 0, no filteringattributes | Medium "Synchronous step without filtering attributes" | Automated | Pass |
| TC-AI10-COL-02 | Async / filtered step | async step + sync-with-filtering step | no sync finding | Automated | Pass |
| TC-AI10-COL-03 | Heavy plugin footprint | 20 steps | Low "Heavy plugin footprint" | Automated | Pass |
| TC-AI10-COL-04 | No plugin steps | none seeded | Info "No plugin steps in solution" | Automated | Pass |
| TC-AI10-COL-05 | Deprecated client API | JScript web resource with `Xrm.Page`; non-JScript ignored | Medium "Deprecated client API in use" (one finding) | Automated | Pass |
| TC-AI10-COL-06 | Heavy scripting | 15 clean JScript web resources | Low "Heavy client-side scripting"; no deprecated finding | Automated | Pass |
| TC-AI10-COL-07 | Classic workflows / sprawl | 25 classic workflows | Medium "Legacy classic workflows" + Low "Automation sprawl" | Automated | Pass |
| TC-AI10-COL-08 | Unmanaged + version | solution ismanaged=false | Medium "Unmanaged solution" + Info "Version" | Automated | Pass |
| TC-AI10-COL-09 | Default publisher prefix | uniquename "new_widgets" | Low "Default publisher prefix" | Automated | Pass |

## Manual — collectors, AI/offline review, export, UI (US-AI10-1, US-AI10-3, US-AI10-4)

Executed in XrmToolBox against a live org; screenshot per case into `screenshots/`.

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-AI10-M-01 | Tool loads & lists solutions | Open tool, Load solutions | Solutions listed | Manual | Pending |
| TC-AI10-M-02 | Collect facts | Select a solution, Collect facts | Off-thread scan; observations grid + concern score populate | Manual | Pending |
| TC-AI10-M-03 | AI review | Set key, Generate AI review, consent | Executive summary + recommendations + backlog + sprint plan returned | Manual | Pending |
| TC-AI10-M-04 | Offline fallback | Generate AI review with no key | Deterministic offline review produced, labelled offline | Manual | Pending |
| TC-AI10-M-05 | Consent payload | Trigger AI, inspect preview | Only anonymized observations shown; no record data/env names; key never saved | Manual | Pending |
| TC-AI10-M-06 | Word export | Export Word (.docx) | Valid .docx opens with title, metrics, summary, findings | Manual | Pending |
| TC-AI10-M-07 | PDF/HTML/MD/JSON export | Export each | Files open with the review embedded | Manual | Pending |
