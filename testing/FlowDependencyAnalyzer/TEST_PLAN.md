# Flow Dependency Analyzer - Test Plan

Traces to [`docs/user-stories/PA01.FlowDependencyAnalyzer.md`](../../docs/user-stories/PA01.FlowDependencyAnalyzer.md).

## Scope

The Flow Dependency Analyzer is a read-only tool that parses every cloud flow's (`workflow`, `category=5`)
`clientdata` JSON into a dependency footprint — trigger, Dataverse tables/columns, connectors, connection
references, environment variables, child flows, custom APIs and HTTP actions — builds a reverse
component→flows impact map, raises deployment-risk findings, and exports Excel/PDF/JSON/HTML.

These tests verify:

- **Parsing** (`FlowClientDataParser`): correct extraction of trigger/tables/columns/connectors/
  connection-references/env-vars/child-flows/custom-APIs/HTTP actions from representative `clientdata`,
  and **redaction** of every HTTP endpoint URL, SAS/trigger URL and secret.
- **Risk rules** (`FlowRiskRules`): direct-connection → High, hardcoded literal → Medium, missing
  table → Critical, missing column/connection-reference/environment-variable → High, and graceful
  degradation (unavailable lookups → no false positives, no throws).
- **Reverse impact** (`FlowAnalysis`): the impacted-flows map for a selected component.
- **UI + live queries** (`FlowCollector`, control): flow inventory, filtering, the dependency tree, the
  component-impact picker, the readiness checklist, and off-thread export with redaction preserved.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (parser, rules, impact map) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse queries and UI | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher) with cloud flows.

## Entry / exit criteria

- **Entry:** tool builds in Release.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
