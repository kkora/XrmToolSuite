# Team Permission Explorer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (xUnit — `testing/UnitTests/TeamPermissionExplorerTests.cs`)

| ID | Case | Traces to | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-01 | No members, non-AAD team | US-SEC2.4.1 | "Team has no members" finding at Medium | Automated | Pass |
| TC-02 | No members, AAD group team | US-SEC2.4.1 | No empty/orphaned finding (membership syncs from AAD) | Automated | Pass |
| TC-03 | No roles | US-SEC2.4.1 | "Team has no security roles" at Medium | Automated | Pass |
| TC-04 | Over-privileged (≥10 Deep/Global) | US-SEC2.4.1 | "Team is over-privileged" at High | Automated | Pass |
| TC-05 | Over-privileged custom threshold | US-SEC2.4.1 | Trips at custom `OverPrivilegeTableThreshold` | Automated | Pass |
| TC-06 | Duplicate role via multiple teams | US-SEC2.4.1 | "Duplicate role assignment" at Low | Automated | Pass |
| TC-07 | Duplicate role name listed twice | US-SEC2.4.1 | "Duplicate role assignment" at Low | Automated | Pass |
| TC-08 | Orphaned (0 members + 0 owned) | US-SEC2.4.1 | "Team is inactive / orphaned" at Medium | Automated | Pass |
| TC-09 | Clean team | US-SEC2.4.1 | Single Info "No team risks detected" | Automated | Pass |
| TC-10 | Effective resolves deepest scope | US-SEC2.2.2 | `Effective()` keeps Global over Basic/Local via shared engine | Automated | Pass |
| TC-11 | Effective with empty grants | US-SEC2.2.2 | Empty dictionary | Automated | Pass |

## Manual (GUI + live Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-20 | Tool loads & lists teams | US-SEC2.0.1, US-SEC2.1.1 | Open the tool, connect, click **Load teams** | Teams grid fills off-thread with name/type/BU/members/roles/top-risk | Manual | Pending |
| TC-21 | Type filter + search | US-SEC2.1.1, US-SEC2.1.2 | Change the Type combo; type in Search | Grid filters instantly by type and by name/BU; filter persists on reopen | Manual | Pending |
| TC-22 | Members & roles on selection | US-SEC2.2.1 | Select a team | Members/Inheriting-users and Roles tabs populate; header shows counts | Manual | Pending |
| TC-23 | Effective privilege matrix | US-SEC2.2.2 | Select a team, open Effective privileges | Privilege × scope shows deepest scope per privilege | Manual | Pending |
| TC-24 | Owned-record summary | US-SEC2.3.2 | Open Owned records tab | Per-table owned counts (aggregate query); missing table shows 0 | Manual | Pending |
| TC-25 | Risk findings | US-SEC2.4.1 | Open Findings tab | Findings with severity + evidence; colour-coded by severity | Manual | Pending |
| TC-26 | Degrade on blocked query | US-SEC2.4.2 | Load against an org where one owned table is blocked | Scan completes; blocked source degrades (0 / Info), no crash | Manual | Pending |
| TC-27 | Compare two teams | US-SEC2.5.1 | Select a team, **Compare…**, pick a second | Diff dialog shows privilege scope diff + roles unique to each side | Manual | Pending |
| TC-28 | Export Excel | US-SEC2.6.1 | Export ▸ Excel, choose a path | .xlsx written with findings across teams; opens | Manual | Pending |
| TC-29 | Export PDF | US-SEC2.6.1 | Export ▸ PDF | .pdf written (native PdfSharp/MigraDoc chain) | Manual | Pending |
| TC-30 | Export CSV / HTML | US-SEC2.6.1 | Export ▸ CSV, then HTML | Files contain team/severity/finding/evidence columns; names & counts only | Manual | Pending |

> Execute each manual case in a Windows + XrmToolBox session against a real org and save a screenshot
> under `screenshots/`.
