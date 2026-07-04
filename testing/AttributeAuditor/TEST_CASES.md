# Attribute Auditor - Test Cases

> **Tool is WIP.** All cases are `Pending` (feature not yet implemented). Type is the intended tier.

| ID | Case | Traces to | Steps (planned) | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-AA-01 | Scaffold loads | US-AA-0.1 | Open tool, connect, click the sample command | Tool loads, connects, returns rows off-thread; settings persist on close | Manual | Pending |
| TC-AA-02 | Select scope | US-AA-1.1 | Pick tables; toggle custom-only; apply solution filter | Only selected/custom tables are audited; selection persists | Manual | Pending |
| TC-AA-03 | Form usage signal | US-AA-2.1 | Audit a table with a column on a form | Column marked "used - on form"; not a candidate | Manual | Pending |
| TC-AA-04 | View usage signal | US-AA-2.2 | Column used in a view column/filter | Column marked "used - in view" | Manual | Pending |
| TC-AA-05 | Logic usage signal | US-AA-2.3 | Column referenced by a business rule/flow/plugin step | Column marked "used - logic" | Manual | Pending |
| TC-AA-06 | Data population | US-AA-2.5 | Column with 0 populated values (sampled/aggregate) | Reported as 0% populated; sampling respects service-protection limits | Automated + Manual | Pending |
| TC-AA-07 | Candidate classification | US-AA-3.1 | Column with no signals and 0% populated | Listed as a retirement candidate with evidence "no signals; 0% populated" | Automated | Pending |
| TC-AA-08 | Managed column guard | US-AA-3.3 | Include a managed column | Marked non-deletable | Manual | Pending |
| TC-AA-09 | Export | US-AA-4.1 | Export audit to CSV/Excel | File contains table, column, type, usage summary | Manual | Pending |
| TC-AA-10 | Guarded cleanup - confirm | US-AA-5.1 | Select candidates, request cleanup | Confirmation dialog states exact columns + count before any change | Manual | Pending |
| TC-AA-11 | Guarded cleanup - dependency block | US-AA-5.1 | Attempt to delete a column that has a dependency | Deletion blocked; dependency reason shown | Manual | Pending |

> When a feature ships, move its case to `Automated` where possible, execute it, set the status, and
> add a screenshot under `screenshots/`.
