# Custom API Explorer - Test Plan

Traces to [`docs/user-stories/PLUGIN6.CustomApiExplorer.md`](../../docs/user-stories/PLUGIN6.CustomApiExplorer.md).

## Scope

The Custom API Explorer catalogs Custom APIs (parameters, responses, binding, backing plugin) read-only, and
offers a confirmation-gated console that invokes a selected API. These tests verify:

- **Automated (SDK-free):** scalar value parsing per `CustomApiFieldType`, request-parameter binding and
  required-validation, the illustrative sample-snippet generator, and the HTML/Markdown/CSV catalog
  exporters (incl. HTML escaping).
- **Manual (live):** the Dataverse collector (customapi + parameters + responses + plugin type), the
  inventory grid + filter, the generated parameter form, the **gated invocation** (confirmation dialog,
  off-thread execute, response/fault display), settings round-trip, and that the tool loads in XrmToolBox.

**Safety focus:** the invoke path is the only write. Manual cases explicitly verify the confirmation dialog
names the API/target/environment, defaults to Cancel, and that no secret values are persisted or displayed.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free parsing/binding/snippet/exporters | xUnit in `testing/UnitTests/CustomApiExplorerTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Collector, inventory UI, gated invoke, exports, XTB load | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher). Live invocation
  requires an org where executing the chosen Custom API is safe (prefer a sandbox / read-only function).

## Entry / exit criteria

- **Entry:** tool builds in Release (`dotnet build XrmToolSuite.sln -c Release`).
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
