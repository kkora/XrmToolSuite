# Audit Compliance Checker - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

Automated cases live in `testing/UnitTests/AuditComplianceCheckerTests.cs` (19 xUnit tests).

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | Sensitive table classification | US-SEC5.1.2 | `IsSensitiveTable` over ssn/salary/bank/passport vs. account/color | Sensitive names true, benign names false | Automated | Pass |
| TC-02 | Sensitive column classification | US-SEC5.1.2 | `IsSensitiveColumn` over email/phone/dob names + Money type | Name-pattern and Money → true; benign → false | Automated | Pass |
| TC-03 | Org auditing disabled → Critical | US-SEC5.1.1 | Evaluate coverage with `OrgAuditEnabled=false` | Critical finding; score capped Low band | Automated | Pass |
| TC-04 | Sensitive table without audit → High | US-SEC5.1.2 | Evaluate coverage with a sensitive, non-audited table | High finding on that table, with remediation | Automated | Pass |
| TC-05 | Sensitive column without audit → Medium | US-SEC5.1.2 | Audited sensitive table, sensitive column audit off | Medium finding on `table.column` | Automated | Pass |
| TC-06 | All covered → high score + category breakdown | US-SEC5.4.1 | Evaluate a fully-covered environment | High band, score ≥ threshold, no Medium+ findings, 4 category metrics present | Automated | Pass |
| TC-07 | Deterministic scoring | US-SEC5.4.1 | Evaluate same input twice | Equal score, band, and finding sequence | Automated | Pass |
| TC-08 | Activity rules fire | US-SEC5.2.2 | Evaluate with deletes/security/after-hours activity | Medium/Medium/Low findings respectively | Automated | Pass |
| TC-09 | Delete threshold configurable | US-SEC5.2.2 | Evaluate with a custom `HighDeleteVolumeThreshold` | High-delete finding fires at the custom threshold | Automated | Pass |
| TC-10 | No activity → no activity findings | US-SEC5.2.1 | Evaluate coverage with `activity=null` | No `Activity`-component findings | Automated | Pass |
| TC-11 | Tool loads & connects | US-SEC5.0.1 | Open the tool in XTB, connect, click "Check audit settings" | Loads, connects, runs off-thread; org banner + coverage grid populate | Manual | Pending |
| TC-12 | Coverage grid | US-SEC5.1.1 | After "Check audit settings" | Grid shows table/managed/sensitive/audit on-off; sensitive-without-audit rows highlighted | Manual | Pending |
| TC-13 | Analyze activity | US-SEC5.2.1 | Set a date range (+ optional table scope), click "Analyze activity" | By-table/user/date pivots populate; highlights show delete/security/after-hours counts | Manual | Pending |
| TC-14 | Storage estimate | US-SEC5.3.1 | Open the Storage tab after analyzing activity | Cumulative records + estimated MB by date, clearly labelled an estimate | Manual | Pending |
| TC-15 | Compliance dashboard | US-SEC5.4.1 | Open the Compliance score tab | Score, band color, and category breakdown shown | Manual | Pending |
| TC-16 | Recommendations | US-SEC5.5.1 | Open the Recommendations tab | Findings sorted by severity with evidence + remediation; read-only | Manual | Pending |
| TC-17 | Export Excel/PDF | US-SEC5.6.1 | Export ▸ Excel, then PDF | Files open; contain score, metrics, findings; no changed/sample values | Manual | Pending |
| TC-18 | Export JSON/HTML/CSV | US-SEC5.6.1 | Export ▸ JSON, HTML, CSV | Files contain the expected shape/columns; masked values | Manual | Pending |
| TC-19 | Settings round-trip | US-SEC5.0.1 | Change range/scope, close & reopen the tool | Range length and table scope restored | Manual | Pending |

> Add a screenshot under `screenshots/` for each manual case as it is executed, then set its status.
</content>
