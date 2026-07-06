# API Documentation Builder - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (SDK-free — `testing/UnitTests/ApiDocumentationBuilderTests.cs`)

| ID | Case | Traces to | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-DOC06-RED-01 | Secret-named param masked | US-DOC06.4.1 | `ApiKey`/`clientSecret` sensitive; secret sample = mask; normal sample unmasked | Automated | Pass |
| TC-DOC06-RED-02 | Bearer/URL secrets stripped | US-DOC06.4.1 | `Bearer <tok>`→masked; URL query `?sig=SECRET`→`?***REDACTED***`; no `SECRET` | Automated | Pass |
| TC-DOC06-RED-03 | Operator redaction terms | US-DOC06.4.1 | Extra term `ssn` makes `CustomerSSN` sensitive | Automated | Pass |
| TC-DOC06-EX-04 | Example request template | US-DOC06.3.1 | Template payload; `AsOfDate` typed sample; `ApiKey` masked | Automated | Pass |
| TC-DOC06-MD-05 | Markdown reference | US-DOC06.2.1/2.2/5.1 | Title, `## Recalculate`, param/response tables, `Bound to account`, "template" label, no URL secret | Automated | Pass |
| TC-DOC06-HTML-06 | HTML self-contained + escaped | US-DOC06.5.1 | `<!DOCTYPE html>`, `prefers-color-scheme:dark`, `&lt;script&gt;`, no live `<script>` | Automated | Pass |
| TC-DOC06-OAS-07 | OpenAPI path + schemas | US-DOC06.3.1/5.1 | `openapi:3.0.3`, `/contoso_Recalculate` POST, requestBody + responses, `required` list | Automated | Pass |
| TC-DOC06-OAS-08 | OpenAPI redacts secret params | US-DOC06.4.1 | Secret param annotated `format:password` + `x-redacted:true` | Automated | Pass |
| TC-DOC06-TYPE-09 | Field-type → OpenAPI schema | US-DOC06.2.2 | Boolean/DateTime/Guid/StringArray map to correct schema fragments | Automated | Pass |
| TC-DOC06-JSON-10 | Raw JSON model + redaction | US-DOC06.5.1 | `uniqueName`, `pluginType`; 4 `name` fields (3 params + 1 response); no URL secret | Automated | Pass |

## Manual (GUI + live Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-DOC06-M01 | Tool loads in XrmToolBox | US-DOC06.1.1 | Open the tool in XrmToolBox | Appears in Tools list; loads; settings restore (see `screenshots/xrmtoolbox-tools-list.png`) | Manual | Pending |
| TC-DOC06-M02 | Load Custom APIs | US-DOC06.1.1 | Click Load Custom APIs | Off-thread scan; status shows count; preview shows the Markdown reference | Manual | Pending |
| TC-DOC06-M03 | Parameter/response detail | US-DOC06.2.2 | Inspect a documented API | Request params + response properties with types and requirement | Manual | Pending |
| TC-DOC06-M04 | Preview switch | US-DOC06.5.1 | Switch Preview Markdown/HTML/OpenAPI | Preview re-renders in each format | Manual | Pending |
| TC-DOC06-M05 | Redaction terms | US-DOC06.4.1 | Enter a term in Redact terms; observe preview | New term masks matching params/values live | Manual | Pending |
| TC-DOC06-M06 | Export each format | US-DOC06.5.1 | Export Markdown/HTML/JSON/OpenAPI; open | Files valid; HTML opens offline; OpenAPI parses in a Swagger viewer | Manual | Pending |
| TC-DOC06-M07 | Degradation note | US-DOC06.1.1 | Run against an env with no Custom APIs / a permission gap | Documented note, no crash | Manual | Pending |

> Automated rows are executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Save a screenshot under
> `screenshots/` for each manual case; the required load shot is `screenshots/xrmtoolbox-tools-list.png`.
