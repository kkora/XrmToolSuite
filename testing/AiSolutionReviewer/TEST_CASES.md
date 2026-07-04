# AI Solution Reviewer - Test Cases

Status: `Pass` / `Fail` / `Pending (manual — needs live org / AI key)`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse/AI).

## Automated — report projection & concern score (US-AR-2, US-AR-3)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Source: `testing/UnitTests/ReviewReportTests.cs`.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-AR-REPORT-01 | Empty review | no observations | score 0, band Low, scoreWord "concern", tool name set | Automated | Pass |
| TC-AR-REPORT-02 | Concern weighting | High + Medium | score 17, band Medium | Automated | Pass |
| TC-AR-REPORT-03 | Per-area metrics | 2 Plugins + 1 ALM | Observations 3, Plugins 2 | Automated | Pass |
| TC-AR-REPORT-04 | AI prompt sections | — | prompt covers backlog, sprint plan, architecture recommendations | Automated | Pass |

## Manual — collectors, AI/offline review, export, UI (US-AR-1, US-AR-3, US-AR-4)

Executed in XrmToolBox against a live org; screenshot per case into `screenshots/`.

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-AR-M-01 | Tool loads & lists solutions | Open tool, Load solutions | Solutions listed | Manual | Pending |
| TC-AR-M-02 | Collect facts | Select a solution, Collect facts | Off-thread scan; observations grid + concern score populate | Manual | Pending |
| TC-AR-M-03 | AI review | Set key, Generate AI review, consent | Executive summary + recommendations + backlog + sprint plan returned | Manual | Pending |
| TC-AR-M-04 | Offline fallback | Generate AI review with no key | Deterministic offline review produced, labelled offline | Manual | Pending |
| TC-AR-M-05 | Consent payload | Trigger AI, inspect preview | Only anonymized observations shown; no record data/env names; key never saved | Manual | Pending |
| TC-AR-M-06 | Word export | Export Word (.docx) | Valid .docx opens with title, metrics, summary, findings | Manual | Pending |
| TC-AR-M-07 | PDF/HTML/MD/JSON export | Export each | Files open with the review embedded | Manual | Pending |
