# Privilege Gap Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (xUnit — `testing/UnitTests/PrivilegeEngineTests.cs`)

| ID | Case | Traces to | Inputs | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-SEC1-RESOLVE-01 | Deepest scope wins across duplicate grants | US-SEC1.2.1 | Read granted Basic/Global/Local by 3 roles | Effective = Global, source VP, not ViaTeam | Automated | Pass |
| TC-SEC1-RESOLVE-02 | Team-only privilege flagged ViaTeam | US-SEC1.2.1 | Write granted only via two teams | Resolved entry ViaTeam=true, scope=Local | Automated | Pass |
| TC-SEC1-EVAL-01 | AccessAllowed when scope sufficient | US-SEC1.3.1 | Read Global vs required Local | Allowed, Type=AccessAllowed | Automated | Pass |
| TC-SEC1-EVAL-02 | MissingPrivilege when absent | US-SEC1.3.1 | Create required, not granted | Denied, Type=MissingPrivilege, HeldScope=None | Automated | Pass |
| TC-SEC1-EVAL-03 | InsufficientScope when held too shallow | US-SEC1.3.1 | Write Basic vs required Deep | Denied, Type=InsufficientScope, held vs required reported | Automated | Pass |
| TC-SEC1-EVAL-04 | TeamInheritanceOnly when sole grant via team | US-SEC1.3.1 | Read Global via team only | Allowed, Type=TeamInheritanceOnly, team named in explanation | Automated | Pass |
| TC-SEC1-APPEND-01 | AppendMismatch when AppendTo missing | US-SEC1.1.2 | Append on account, no AppendTo on contact | Denied, Type=AppendMismatch, requires prvAppendTocontact | Automated | Pass |
| TC-SEC1-APPEND-02 | Append allowed when both sides present | US-SEC1.1.2 | Append + AppendTo both granted | Allowed, Type=AccessAllowed | Automated | Pass |
| TC-SEC1-DIFF-01 | Diff highlights scope + present-in-one-only | US-SEC1.5.1 | A vs B with differing/only-in-one privileges | 3 diff rows with correct a/b scopes | Automated | Pass |
| TC-SEC1-MAX-01 | Max returns the deeper scope | US-SEC1.2.1 | (Basic,Deep),(Global,Local) | Deep, Global | Automated | Pass |

## Manual (GUI + Dataverse — capture a screenshot per case in `screenshots/`)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-M-01 | Tool loads & connects | US-SEC1.1.1 | Open the tool in XrmToolBox, connect to an org | Loads without error; status shows connection | Manual | Pending |
| TC-M-02 | Load principals + tables | US-SEC1.1.1 | Pick a principal type, click **Load principals** | Principal list + table list populate off the UI thread | Manual | Pending |
| TC-M-03 | Analyze — denied (missing) | US-SEC1.3.1/.3.2 | Pick a user lacking Create on a table, op=Create, **Analyze** | Red DENIED panel, Type=Missing Privilege, explanation + recommendation | Manual | Pending |
| TC-M-04 | Analyze — insufficient scope | US-SEC1.3.1 | Pick a user with Basic on a table, required scope=Deep | Red DENIED, Type=Insufficient Scope, held vs required shown | Manual | Pending |
| TC-M-05 | Analyze — team inheritance only | US-SEC1.2.1 | Pick a user who gets a privilege only via a team | Amber ALLOWED, Type=Team Inheritance Only, team named | Manual | Pending |
| TC-M-06 | Analyze — Append pair | US-SEC1.1.2 | op=Append, choose related table where AppendTo is missing | Red DENIED, Type=Append Mismatch, missing privilege named | Manual | Pending |
| TC-M-07 | Effective grid | US-SEC1.2.2 | After Analyze | Grid lists privilege × scope × source role/team + Via team | Manual | Pending |
| TC-M-08 | Compare two principals | US-SEC1.5.1 | Select principal A, **Compare…**, pick B | Diff grid shows differing scopes + present-in-one-only | Manual | Pending |
| TC-M-09 | Export CSV/JSON/HTML | US-SEC1.6.1 | **Export ▸** each format, save | Files contain verdict + effective grid; principal name masked | Manual | Pending |
| TC-M-09a | Export Excel (*.xlsx) | US-SEC1.6.1 | **Export ▸ Excel**, save, open in Excel | Valid .xlsx opens; contains verdict/metrics + the gap Finding; principal name masked | Manual | Pending |
| TC-M-09b | Export PDF (*.pdf) | US-SEC1.6.1 | **Export ▸ PDF**, save, open in a reader | Valid native PDF (no HTML round-trip); contains verdict/metrics + the gap Finding; principal name masked | Manual | Pending |
| TC-M-10 | Settings round-trip | US-SEC1.1.1 | Set selections, close & reopen the tool | Last type/table/op/scope/principal restored | Manual | Pending |
| TC-M-11 | Read-only guarantee | US-SEC1.4.1 | Run several analyses | No writes issued; recommendations are text only | Manual | Pending |

> Each automated row above is an executed xUnit fact. Each manual row needs a screenshot in `screenshots/`
> and a status update once run on Windows + XrmToolBox.
