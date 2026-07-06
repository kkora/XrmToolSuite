# Flow Dependency Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

Automated cases live in `testing/UnitTests/FlowDependencyAnalyzerTests.cs` and were executed with
`dotnet test` (17 tests passed). Manual cases require a Windows + XrmToolBox session against a live
Dataverse org and are **pending** (cannot run headlessly).

## Automated (xUnit)

| ID | Case | Traces to | Method | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | Dataverse trigger parsed | US-PA01.2.1 | `Parse_DataverseTrigger_ResolvesTypeEntityAndMessage` | Type "Dataverse", entity "account", message "Update" | Automated | Pass |
| TC-02 | Tables & columns extracted | US-PA01.2.2 | `Parse_ExtractsTablesAndColumns` | Tables account/contacts; columns contacts.firstname/lastname/emailaddress1 | Automated | Pass |
| TC-03 | Connectors & connection refs | US-PA01.3.1 | `Parse_ExtractsConnectorsAndConnectionReferences` | 3 connectors, conn-refs new_dataverseconn/new_office365conn | Automated | Pass |
| TC-04 | Env-var references | US-PA01.3.1 | `Parse_ExtractsEnvironmentVariableReferences` | `@parameters('new_recipientvar')` → new_recipientvar | Automated | Pass |
| TC-05 | Direct-connection detection | US-PA01.3.2 | `Parse_DetectsDirectConnection` | `UsesDirectConnection == true` | Automated | Pass |
| TC-06 | Child flows & custom APIs | US-PA01.4.1 | `Parse_ExtractsChildFlowsAndCustomApis` | Child-flow id + custom API new_RecalculatePremium | Automated | Pass |
| TC-07 | HTTP action + secret/URL redaction | US-PA01.4.2 | `Parse_HttpActionRecorded_ButUrlsAndSecretsRedacted` | HTTP action listed; no endpoint URL / SAS sig / bearer token stored | Automated | Pass |
| TC-08 | Hardcoded https URL redacted | US-PA01.5.2 | `Parse_HardcodedHttpsUrl_IsRedactedNotStored` | Literal flagged, URL redacted (not stored) | Automated | Pass |
| TC-09 | Malformed clientdata never throws | US-PA01.2.1 | `Parse_Malformed_DegradesToParseNote_NoThrow` | ParseNote set, no exception, empty deps | Automated | Pass |
| TC-10 | Direct connection → High | US-PA01.3.2 | `Rules_DirectConnection_IsHigh` | High finding "uses a direct connection" | Automated | Pass |
| TC-11 | Hardcoded literal → Medium | US-PA01.5.2 | `Rules_HardcodedLiteral_IsMedium` | Medium "Hardcoded literal…" finding | Automated | Pass |
| TC-12 | Missing table → Critical | US-PA01.5.1 | `Rules_MissingTable_IsCritical` | Critical missing-table finding for `contacts` | Automated | Pass |
| TC-13 | Missing conn-ref / env-var → High | US-PA01.5.1 | `Rules_MissingConnectionReference_And_EnvVar_AreHigh` | High findings for new_office365conn and new_recipientvar | Automated | Pass |
| TC-14 | Unavailable lookup → no false positives | US-PA01.5.1 | `Rules_ResolutionUnavailable_RaisesNoFalseMissingFindings` | Null sets → no missing findings, no throw | Automated | Pass |
| TC-15 | Reverse impact map | US-PA01.6.1 | `Impact_ReverseMap_ListsEveryFlowDependingOnAComponent` | ImpactedFlows/BuildImpactMap list all dependent flows | Automated | Pass |

## Manual (GUI + Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-20 | Tool loads & connects | US-PA01.0.1 | Open the tool, connect | Loads in XTB, connects, settings persist on close | Manual | Pending |
| TC-21 | Analyze flows (inventory) | US-PA01.1.1 | Click "Analyze flows" | Flows load off-thread with progress; owner/state per row | Manual | Pending |
| TC-22 | Filters | US-PA01.1.2 | Change Status/Owner/Connector/Trigger/Table/Solution | Grid filters to the matching subset | Manual | Pending |
| TC-23 | Dependency tree | US-PA01.2.x/1.4.x | Select a flow | Tree shows trigger, tables/columns, connectors, conn-refs, env-vars, child flows, custom APIs, HTTP (redacted) | Manual | Pending |
| TC-24 | Component impact picker | US-PA01.6.1 | Pick a kind + component | Impacted-flow list matches every flow using it | Manual | Pending |
| TC-25 | Readiness checklist | US-PA01.6.2 | Open "Deployment readiness" tab | PASS/REVIEW + per-check pass/fail rows | Manual | Pending |
| TC-26 | Export Excel/PDF/JSON/HTML | US-PA01.6.2 | Export each format | Files write off-thread; JSON carries impact map + pass/fail; secrets/URLs redacted everywhere | Manual | Pending |

> Save a screenshot under `screenshots/` for each manual case when executed.
