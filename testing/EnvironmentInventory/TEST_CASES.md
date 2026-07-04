# Environment Inventory - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | Tool loads | US-TT-0.1 | Open the tool, connect, run the primary action | Loads, connects, runs off-thread; settings persist on close | Manual | Pending |
| TC-02 | <TODO: core capability> | US-TT-1.1 | <steps> | <expected> | Manual | Pending |
| TC-03 | <TODO: pure-logic case> | US-TT-1.x | <inputs> | <expected> | Automated | Pending |
| TC-04 | Export results | US-TT-2.2 | Export to CSV/Excel/JSON | File contains the expected columns/shape | Manual | Pending |

> Add an xUnit case in `testing/UnitTests/` for each pure-logic row, execute it, set the status, and
> save a screenshot under `screenshots/` for each manual case.
