# JavaScript Performance Analyzer - Test Plan

Traces to [`docs/user-stories/PERF08.JavaScriptPerformanceAnalyzer.md`](../../../docs/user-stories/PERF08.JavaScriptPerformanceAnalyzer.md).

## Scope

The JavaScript Performance Analyzer statically scans every JScript web resource (`webresourcetype = 3`) and
the FormXML event handlers that reference them, entirely offline (no runtime is executed). It flags
deprecated `Xrm.Page`, synchronous `XMLHttpRequest`, blocking `alert()`, excessive `console` logging,
repeated retrieve/`Xrm.WebApi` calls, hardcoded GUIDs/URLs, unsupported DOM manipulation, and oversized
scripts; maps scripts to forms/events; flags forms with too many OnLoad handlers; scores each script 0–100
with a Low/Medium/High band; and exports Excel/PDF/JSON/HTML/Markdown/CSV.

These tests verify (a) the SDK-free rule engine and FormXML mapper produce the correct severities, line
context, confidence labels, scores/bands and event links, and (b) the live collector + WinForms UI + exports
behave correctly against a real environment.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic: `JsRules`, `JsModels`, `FormEventMap` (rules, score/band, event mapping) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Live collection (`JsCollector`), grid/search/detail UI, form-usage panel, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`). No Dataverse, no WinForms.
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher, read-only usage).

## Entry / exit criteria

- **Entry:** tool builds in Release.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.

## Out of scope

- No runtime execution of JavaScript; the tool measures pattern/structural risk, not measured execution time.
- The collector (`JsCollector`) needs a live connection and is therefore manual-only (not unit-tested).
