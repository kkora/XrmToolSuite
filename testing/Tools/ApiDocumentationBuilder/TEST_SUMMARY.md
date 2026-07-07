# API Documentation Builder - Test Summary

**Automated: PASS.** Manual/live and GUI cases: **PENDING** (require a Windows + XrmToolBox + Dataverse session).

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Suite:** `testing/UnitTests/ApiDocumentationBuilderTests.cs` (10 cases: TC-DOC06-RED-01/02/03,
  TC-DOC06-EX-04, TC-DOC06-MD-05, TC-DOC06-HTML-06, TC-DOC06-OAS-07/08, TC-DOC06-TYPE-09, TC-DOC06-JSON-10).
- **Result:** **10 passed, 0 failed** (full suite: **432 passed, 0 failed**).
- **Coverage:** the `Redactor` safety engine (secret-name masking, bearer-token + URL-query-secret stripping,
  operator-supplied terms), the field-type → OpenAPI-schema mapping, template example payloads (secrets masked),
  the Markdown reference, the self-contained theme-aware HTML (escaped), the raw JSON model (redacted), and the
  best-effort OpenAPI 3.0-style spec (path/operation/request/response schemas, secret-param annotations).
- **Build:** `dotnet build src/Tools/XrmToolSuite.ApiDocumentationBuilder -c Release` — succeeds, 0 warnings.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 10 | 10 | 10 | 0 | 0 |
| Manual | 7 | 0 | 0 | 0 | 7 |

The manual cases cover the SDK collector (`ApiCollector`) against a live environment's Custom APIs, preview
switching, live redaction-term entry, export round-trips, permission-gap/empty degradation, and the required
"tool loads in XrmToolBox" screenshot. They **cannot** run headlessly and are **not** claimed as passed.

## Verdict

The safety-critical redaction engine and all emitters (incl. the OpenAPI-style spec) are fully automated and
green. The tool builds clean and is registered in the `testing/UiSmokeTests` `ExpectedTools` list. Manual/live
verification remains pending a Windows + XrmToolBox + Dataverse session.
