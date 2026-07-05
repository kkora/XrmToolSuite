# API Documentation Builder - Test Plan

Traces to [`docs/user-stories/DOC06.ApiDocumentationBuilder.md`](../../docs/user-stories/DOC06.ApiDocumentationBuilder.md).

## Scope

The API Documentation Builder documents a Dataverse environment's Custom APIs (parameters, responses, binding,
backing plugin) as a redaction-safe reference and a best-effort OpenAPI 3.0-style JSON spec, exported to Markdown,
self-contained HTML, raw JSON, and OpenAPI JSON. These tests verify:

- **Automated (SDK-free):** the `Redactor` safety engine (secret-name masking, bearer/URL-secret stripping,
  user-supplied terms), the field-type → OpenAPI-schema mapping, the example-payload generation, and the
  Markdown/HTML/JSON/OpenAPI emitters (shape, escaping, redaction applied).
- **Manual (live):** the SDK collector (`ApiCollector`) against a real environment's Custom APIs, preview
  switching, redaction-term entry, export round-trips, and that the tool loads in XrmToolBox.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free model + redaction + emitters | xUnit in `testing/UnitTests/ApiDocumentationBuilderTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse queries and UI | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection with Custom APIs.

## Entry / exit criteria

- **Entry:** tool builds in Release with zero warnings.
- **Exit:** all automated tests pass; all manual cases executed with Pass (incl. the required
  `screenshots/xrmtoolbox-tools-list.png` load shot), or defects logged in the summary.
