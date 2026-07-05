# Deployment Risk Analyzer - Test Plan

Traces to [`docs/user-stories/ALM07.DeploymentRiskAnalyzer.md`](../../docs/user-stories/ALM07.DeploymentRiskAnalyzer.md).

## Scope

Verify pre-deployment risk analysis: connection handling, the six analyzers, risk scoring/banding,
and the four export formats.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | Risk scoring & banding (US-ALM07-8.1) and other SDK-free logic | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK only |
| Manual | Analyzers, connections, exports (US-ALM07-1..7, 9, 10) | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a source (and target) Dataverse env with System Customizer+ |

## What is NOT automatable here

Analyzer logic depends on `IOrganizationService` and real solution metadata; the exporters and UI
require the WinForms host. These need a live org and are covered by manual cases. The automated tier
is intentionally limited to pure logic so it runs anywhere with no connection.

## Environments

- **Automated:** .NET 8 SDK (`dotnet test`). No Dataverse, no XrmToolBox.
- **Manual:** XrmToolBox (>= ~1.2025.10), a dev/source environment, optionally a test/prod target
  environment for the schema/version/target-config cases.

## Entry / exit criteria

- **Entry:** tool builds in Release; unit project restores and builds.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.

## Risks

- Analyzers degrade query failures to informational findings; manual cases must confirm that a
  permission gap produces an info finding rather than aborting the run (US-ALM07-10.1).
- Excel export depends on the shipped ClosedXML chain; the "no output on export" case guards the
  packaging regression documented in the tool's DEPLOYMENT.md.
