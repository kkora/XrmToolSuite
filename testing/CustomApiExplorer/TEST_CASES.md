# Custom API Explorer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

Automated cases are the xUnit facts in `testing/UnitTests/CustomApiExplorerTests.cs`
(`dotnet test testing/UnitTests/UnitTests.csproj`). Manual cases need a live org + XrmToolBox.

## Automated (SDK-free)

| ID | Case | Traces to | xUnit fact | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-CAE-01 | Scalar parsing per type | US-PLUGIN06.4.1 | `Parse_ScalarTypes` (theory) | ok/fail per input | Automated | Pass |
| TC-CAE-02 | StringArray splits on comma | US-PLUGIN06.4.1 | `Parse_StringArray_SplitsOnComma` | `[a,b,c]` | Automated | Pass |
| TC-CAE-03 | Guid + DateTime parse | US-PLUGIN06.4.1 | `Parse_GuidAndDateTime` | both ok | Automated | Pass |
| TC-CAE-04 | Complex types are non-scalar | US-PLUGIN06.4.1 | `IsScalar_ComplexTypesAreNotScalar` | Entity/EntityReference false | Automated | Pass |
| TC-CAE-05 | Missing required blocks invoke | US-PLUGIN06.4.1 | `Bind_MissingRequired_BlocksInvoke` | CanInvoke false | Automated | Pass |
| TC-CAE-06 | Scalars parsed; can invoke | US-PLUGIN06.4.1 | `Bind_ParsesScalars_AndCanInvoke` | typed values, CanInvoke | Automated | Pass |
| TC-CAE-07 | Bad value is an error | US-PLUGIN06.4.1 | `Bind_BadValue_IsAnError` | error listed | Automated | Pass |
| TC-CAE-08 | Optional omitted is fine | US-PLUGIN06.4.1 | `Bind_OptionalOmitted_IsFine` | CanInvoke, no value | Automated | Pass |
| TC-CAE-09 | Complex input routed | US-PLUGIN06.4.1 | `Bind_ComplexInput_GoesToComplexInputs` | in ComplexInputs | Automated | Pass |
| TC-CAE-10 | Action snippet POST, no secrets | US-PLUGIN06.5.2 | `Snippet_Action_IsPostWithBody_NoSecrets` | POST + name + disclaimer | Automated | Pass |
| TC-CAE-11 | Function snippet GET query | US-PLUGIN06.5.2 | `Snippet_Function_IsGetWithQuery` | GET + `Id=<Guid>` | Automated | Pass |
| TC-CAE-12 | Markdown has params/responses | US-PLUGIN06.2.3 | `Markdown_ContainsApiParamsAndResponses` | tables present | Automated | Pass |
| TC-CAE-13 | CSV row per member | US-PLUGIN06.2.3 | `Csv_HasRowPerMember` | request+response rows | Automated | Pass |
| TC-CAE-14 | HTML self-contained + escapes | US-PLUGIN06.5.1 | `Html_IsSelfContainedAndEscapes` | doctype + escaped markup | Automated | Pass |

## Manual (live Dataverse + XrmToolBox)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-CAE-M-01 | Tool loads in XrmToolBox | EPIC-PLUGIN06 | Open XrmToolBox → Tools → "Custom API Explorer" | Appears with name/version/description/icon (MEF registration resolved at scan time); shot `screenshots/xrmtoolbox-tools-list.png` | Manual | **Pass** (FlaUI `UiSmokeTests`, 2026-07-05) |
| TC-CAE-M-02 | Load inventory off-thread | US-PLUGIN06.1.1 | Connect, "Load Custom APIs" | Progress shown; grid fills with unique name/kind/binding/plugin | Manual | Pending |
| TC-CAE-M-03 | Filter + backing plugin | US-PLUGIN06.1.2/1.3 | Type in filter | Grid narrows; plugin type shown, logic-less marked "(none)" | Manual | Pending |
| TC-CAE-M-04 | Params/responses detail | US-PLUGIN06.2 | Select an API | Detail lists parameters (type/optional), responses, and a sample snippet | Manual | Pending |
| TC-CAE-M-05 | Parameter form generated | US-PLUGIN06.4.1 | Select an API with parameters | A row per parameter with an editable Value cell | Manual | Pending |
| TC-CAE-M-06 | Validation blocks invalid invoke | US-PLUGIN06.4.1 | Leave a required param empty / bad value, Invoke | Warning lists the problem; no call made | Manual | Pending |
| TC-CAE-M-07 | Confirmation gate | US-PLUGIN06.4.2 | Invoke a valid call | Dialog names API/target/environment, defaults to Cancel; cancel = no execute | Manual | Pending |
| TC-CAE-M-08 | Response/fault display | US-PLUGIN06.4.3 | Confirm invoke of a safe function; then a failing one | Typed results shown on success; raw fault on failure; no secrets | Manual | Pending |
| TC-CAE-M-09 | Bound API needs target | US-PLUGIN06.4.2 | Invoke an entity-bound API without a target | Prompted for `entity:guid` target | Manual | Pending |
| TC-CAE-M-10 | Catalog export HTML/MD/CSV | US-PLUGIN06.5.1 | Export each format | Files open; contain params/responses/binding/plugin | Manual | Pending |
| TC-CAE-M-11 | Settings round-trip | US-PLUGIN06.4.4 | Set filter, close, reopen | Filter restored; no secrets in settings file | Manual | Pending |

> Save a screenshot under `screenshots/` for each manual case; the load shot MUST be
> `screenshots/xrmtoolbox-tools-list.png`.
