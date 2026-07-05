# View Performance Analyzer - Test Plan

Traces to [`docs/user-stories/ViewPerformanceAnalyzer.md`](../../docs/user-stories/ViewPerformanceAnalyzer.md).

## Scope

The View Performance Analyzer batch-analyzes every system view (`savedquery`) and, optionally, personal
view (`userquery`) for a chosen table. Each view's FetchXML runs through the **shared** PERF3 FetchXML rule
engine (reused, not reimplemented) and its LayoutXML is parsed for displayed-column width; the two combine
into a labeled-heuristic 0–100 per-view cost score that ranks the slowest/riskiest views. It offers a
per-view FetchXML + layout-columns + findings detail view, optional read-only execution timing, and
Excel/PDF/JSON/HTML/Markdown/CSV export.

These tests verify:

- **Automated (SDK-free):** the `LayoutXmlParser` (column counting, tolerance of blank/malformed input), the
  `ViewScorer` (reuse of the shared engine's findings, the over-wide-layout rule, score/band computation,
  parse-failure degradation) and `ViewScorer.Rank` (ordering by score).
- **Manual (live):** the table picker, batch analysis with progress, the ranked grid + environment score
  cards, the detail panels, opt-in view timing, settings round-trip, and every export format — none of which
  can run headlessly (they need a Dataverse connection and the WinForms host).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (`LayoutXmlParser`, `ViewScorer`, `Rank`) | xUnit in `testing/UnitTests/ViewPerformanceAnalyzerTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse queries (collector), UI, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher, to read views).

## Entry / exit criteria

- **Entry:** the tool builds in Release with 0 warnings; export dependency DLLs land in `bin/Release/net48/`.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
