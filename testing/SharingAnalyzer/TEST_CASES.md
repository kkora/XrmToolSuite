# Sharing Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (xUnit — `testing/UnitTests/SharingAnalyzerTests.cs`)

| ID | Case | Traces to | Inputs | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | Decode Read | US-SEC04.2.1 | mask = 1 | `["Read"]`, summary `R` | Automated | Pass |
| TC-02 | Decode Read+Write | US-SEC04.2.1 | mask = 3 | `["Read","Write"]`, summary `R/W` | Automated | Pass |
| TC-03 | Decode full mask | US-SEC04.2.1 | all 8 bits set | all 8 named rights, summary `R/W/A/AT/C/D/S/AS` | Automated | Pass |
| TC-04 | Decode none | US-SEC04.2.1 | mask = 0 | empty list, summary `None` | Automated | Pass |
| TC-05 | RecordStats aggregation | US-SEC04.2.1 | 3 rows, 2 principals, 1 record | DistinctPrincipals=2, ShareCount=3, CombinedMask=OR | Automated | Pass |
| TC-06 | PrincipalStats aggregation | US-SEC04.2.1 | user shared 2 records | InboundRecords=2, InboundShares=2 | Automated | Pass |
| TC-07 | Excessive sharing → High | US-SEC04.3.1 | record shared with 30 principals | High "Excessive record sharing" | Automated | Pass |
| TC-08 | Excessive custom threshold | US-SEC04.3.1 | 6 principals, threshold 5 | High finding present | Automated | Pass |
| TC-09 | Inactive user → Medium | US-SEC04.3.1 | disabled-user share | Medium "Shared with an inactive user" | Automated | Pass |
| TC-10 | Disabled/empty team → Medium | US-SEC04.3.1 | empty-team share | Medium "Shared with a disabled or empty team" | Automated | Pass |
| TC-11 | High inbound → Medium | US-SEC04.3.2 | user with 12 inbound, threshold 10 | Medium "User with high inbound shared access" | Automated | Pass |
| TC-12 | Outlier record → Medium | US-SEC04.3.1 | 8 records @5 principals + 1 @24 | Medium outlier finding; no Excessive finding | Automated | Pass |
| TC-13 | Clean sharing → Info | US-SEC04.3.1 | one benign share | single Info "No sharing risks detected" | Automated | Pass |
| TC-14 | Score/band clean | US-SEC04.3.1 | clean findings | score 0, band Low | Automated | Pass |
| TC-15 | Score/band many High | US-SEC04.3.1 | 4 excessive records | 4 High findings, score 48, band High | Automated | Pass |

## Manual (GUI + Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-M1 | Tool loads & connects | US-SEC04.0.1 | Open the tool, connect | Loads in XTB, connects; settings persist on close | Manual | Pending |
| TC-M2 | Table picker | US-SEC04.1.1 | Click Tables…, filter, check `account` | Selection persists; button shows count | Manual | Pending |
| TC-M3 | Scoped scan off-thread | US-SEC04.1.2 | Scan `account` | Spinner + progress; grid fills; UI stays responsive; cancel works | Manual | Pending |
| TC-M4 | Full-scan opt-in warning | US-SEC04.1.1 | Toggle "Full-environment scan" | Warning dialog; declining un-checks the toggle | Manual | Pending |
| TC-M5 | Shares grid + rights decode | US-SEC04.2.1 | Inspect grid | Table/Record/Principal/Type/Active/Rights populated; rights decoded | Manual | Pending |
| TC-M6 | Summary cards | US-SEC04.2.2 | Inspect cards | Band/score + totals + rights mix shown | Manual | Pending |
| TC-M7 | Findings + inactive highlight | US-SEC04.3.1 | Share to a disabled user, rescan | Medium finding; inactive row shaded | Manual | Pending |
| TC-M8 | Intensity view | US-SEC04.4.1 | Open Intensity tab | Ranked, heat-shaded table × principal rows | Manual | Pending |
| TC-M9 | Recommendations preview | US-SEC04.5.1 | Open Recommendations tab | Preview-only actions; no revoke button; no writes | Manual | Pending |
| TC-M10 | Export Excel/PDF | US-SEC04.6.1 | Export xlsx and pdf | Files open; findings + metrics present; no raw full share dump | Manual | Pending |
| TC-M11 | Export JSON/HTML/CSV | US-SEC04.6.1 | Export each | Well-formed output with findings + aggregate metrics | Manual | Pending |

> Each automated row maps to a `[Fact]` in `SharingAnalyzerTests.cs`. Save a screenshot under
> `screenshots/` for each manual case when executed.
