# FetchXML Performance Analyzer - Test Plan

Traces to [`docs/user-stories/PERF3.FetchXmlPerformanceAnalyzer.md`](../../docs/user-stories/PERF3.FetchXmlPerformanceAnalyzer.md).

## Scope

The tool parses a FetchXML string (pasted or loaded from a view) into a structural model, runs a
heuristic performance rule engine, and reports severity-ranked findings, an estimated cost + band, and
optimization suggestions. It offers opt-in read-only execution timing and JSON/HTML/Markdown/CSV export.

These tests verify:

- **Parser** (`FetchXmlParser`): correct shape/counts for valid queries; clear errors for malformed or
  non-`<fetch>` input; nested link-entities counted at every depth.
- **Rule engine** (`FetchXmlRules`): each rule fires at the right severity; cost/band derive from findings;
  a clean query yields a single Info finding in the Low band.
- **UI + Dataverse** (manual): analyze pastes with no connection; load from `savedquery`/`userquery`;
  execute-with-timing runs read-only off the UI thread; exports write the expected shape; settings round-trip.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free parser + rule engine | xUnit in `testing/UnitTests/FetchXmlAnalyzerTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | View load, timed execution, UI, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher).

## Entry / exit criteria

- **Entry:** tool builds in Release; unit tests compile in the UnitTests project.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
