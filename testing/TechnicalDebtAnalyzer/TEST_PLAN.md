# Technical Debt Analyzer - Test Plan

Traces to [`docs/user-stories/SOLN10.TechnicalDebtAnalyzer.md`](../../docs/user-stories/SOLN10.TechnicalDebtAnalyzer.md).

## Scope

Validate the Technical Debt Analyzer end to end: the SDK-free debt scoring and report projection
(automated), and the eight Dataverse analyzers, the WinForms UI, the five exporters, and the
offline/AI executive summary (manual, against a live org in XrmToolBox).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | Debt score/band, cap, per-category metrics, report-model shape (US-SOLN10-3, US-SOLN10-4) | xUnit over `TechDebtReport` + shared `ScoreCalculator`/`ReportModel` | .NET 8 SDK (`testing/UnitTests`) |
| Manual | Analyzers over live metadata, UI, exports, summary (US-SOLN10-1, US-SOLN10-5..7) | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## What is NOT automatable here

The eight analyzers require live Dataverse metadata/rows (custom tables, web resources, plugin steps,
workflows, roles), and the Excel/PDF exporters + WinForms UI need the net48/WinForms host — none run
headlessly. These are documented manual cases executed in XrmToolBox with a screenshot per case.

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`) — no connection.
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher) with custom components.

## Entry / exit criteria

- **Entry:** tool builds in Release with zero warnings; automated tests green.
- **Exit:** all automated tests pass; all manual cases executed with Pass and a screenshot, or defects logged in the summary.

## Risks

- Row-count probing is capped (`MaxEntityProbes`) — verify the cap is reported (TC-SOLN10-M-02) rather than silently truncating.
- The AI summary must never send record data or persist the key — verify the consent payload (TC-SOLN10-M-09).
