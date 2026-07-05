# Form Performance Analyzer - Test Plan

Traces to [`docs/user-stories/PERF10.FormPerformanceAnalyzer.md`](../../docs/user-stories/PERF10.FormPerformanceAnalyzer.md).

## Scope

The Form Performance Analyzer statically scores model-driven **main forms** (`systemform`, `type = 2`) by
parsing their FormXML into structural counts (tabs/sections, visible vs. hidden fields, PCF/custom controls,
subgrids, quick views, script libraries, onload/onchange/tabstatechange handlers) and combining them with the
entity's active form-scoped business-rule count into a deterministic, weighted 0–100 "heaviness" score banded
Light/Moderate/Heavy/Critical. It ranks forms by score, offers a per-form metric breakdown and targeted
Quick-win/Structural recommendations, a two-form side-by-side comparison, a configurable weights/thresholds
settings dialog, and CSV/HTML export. **Scoring is fully offline** — the only Dataverse reads are retrieving
the forms and the business-rule counts.

These tests verify:

- **Automated (SDK-free):** `FormXmlParser` (structural counts; blank/malformed tolerance) and `FormScorer`
  (deterministic composite score, band thresholds, findings, recommendations with the right triggers, ranking,
  and parse-failure degradation).
- **Manual (live):** the table-scope picker, batch form collection (`systemform` + `workflow` business rules),
  the ranked grid + band summary, the metric-breakdown and recommendations detail panes, the two-form compare
  dialog, the scoring-settings round-trip, and the CSV/HTML export dialogs — none of which can run headlessly
  (they need a Dataverse connection and the WinForms host).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (`FormXmlParser`, `FormScorer`) | xUnit in `testing/UnitTests/FormPerformanceAnalyzerTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse queries (collector), UI, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher, to read forms).

## Entry / exit criteria

- **Entry:** the tool builds in Release with 0 warnings; only `XrmToolSuite.FormPerformanceAnalyzer.dll` lands
  in `bin/Release/net48/` (single-DLL — no dependency chain).
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
