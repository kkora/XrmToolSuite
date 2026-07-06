# Environment Comparison Suite - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (SDK-free diff engine — `testing/UnitTests/EnvironmentComparisonSuiteTests.cs`)

| ID | Case | Traces to | Inputs | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | In source only → Missing | US-MIG01.4.1 | source has `account`, target empty | one diff, `Missing` | Automated | Pass |
| TC-02 | In target only → Extra | US-MIG01.4.1 | target has `contact`, source empty | one diff, `Extra` | Automated | Pass |
| TC-03 | Differing property → Changed + delta | US-MIG01.2.2 | `type` String vs Memo | `Changed`, delta lists `type: String → Memo` | Automated | Pass |
| TC-04 | Managed differs → ManagedVsUnmanaged | US-MIG01.4.1 | managed vs unmanaged | `ManagedVsUnmanaged`, source/target managed flags set | Automated | Pass |
| TC-05 | Same everything → Identical | US-MIG01.4.1 | same props, same managed | `Identical`, no delta | Automated | Pass |
| TC-06 | Version differs → Changed | US-MIG01.2.1 | solution v1.0.0.0 vs v1.0.0.1 | `Changed`, delta includes `version` | Automated | Pass |
| TC-07 | Structural Missing → High; managed/unmanaged → High | US-MIG01.4.2 | Tables.Missing, Roles.ManagedVsUnmanaged | both `High` | Automated | Pass |
| TC-08 | Soft Missing → Medium | US-MIG01.4.2 | Views.Missing | `Medium` | Automated | Pass |
| TC-09 | Identical → Info severity | US-MIG01.4.2 | any category, Identical | `Info` | Automated | Pass |
| TC-10 | Severity resolver override wins | US-MIG01.4.2 | resolver returns Critical | `Critical` | Automated | Pass |
| TC-11 | Roll → score, band, count matrix | US-MIG01.4.2 | 2 missing + 1 extra + 1 identical | score 29, `Medium`, matrix counts, 3 findings (identical excluded) | Automated | Pass |
| TC-12 | Roll is deterministic | US-MIG01.4.2 | same inputs twice | equal score + diff count | Automated | Pass |
| TC-13 | Secret property masked but change detected | US-MIG01.3.3 | secret `currentvalue` differs | `Changed`; both values are the mask placeholder | Automated | Pass |
| TC-14 | Secret via CompareOptions masked | US-MIG01.3.3 | `token` marked secret globally | delta values masked | Automated | Pass |

## Manual (live tool — Windows + XrmToolBox + two connections)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-M1 | Tool loads & connects source | US-MIG01.1.1 | Open the tool; connect the primary env | Loads; source connection set; no errors | Manual | Pending |
| TC-M2 | Connect target env (dual connection) | US-MIG01.1.1 | Click "Connect target env…"; pick a second env | Target label turns green with the target name; primary source unchanged | Manual | Pending |
| TC-M3 | Category scoping | US-MIG01.1.2 | Uncheck several categories; run Compare | Only checked categories appear; unchecked ones are not retrieved | Manual | Pending |
| TC-M4 | Compare runs off-thread with progress | US-MIG01.1.1 | Run Compare on two envs | Spinner shows per-category progress; UI stays responsive; cancellation works | Manual | Pending |
| TC-M5 | Schema drift surfaces | US-MIG01.2.2 | Compare envs with a known column change | Table/column diffs classified; detail viewer shows datatype/required before/after | Manual | Pending |
| TC-M6 | Security drift by privilege set | US-MIG01.3.1 | Compare envs with a modified role | Role shows `Changed` (privilegeset hash differs), not just by name | Manual | Pending |
| TC-M7 | Secret env-var value masked | US-MIG01.3.3 | Compare envs with a secret env var | Value shown/exported as the mask placeholder, never the real value | Manual | Pending |
| TC-M8 | Grid filters | US-MIG01.5.1 | Filter by category / classification / severity | Grid narrows to the selection | Manual | Pending |
| TC-M9 | Side-by-side detail viewer | US-MIG01.5.1 | Select a Changed row | Property/Source/Target grid + recommendation populate | Manual | Pending |
| TC-M10 | Export Excel/PDF/JSON/HTML | US-MIG01.5.2 | Export each format | File written off-thread; opens; masked values stay masked | Manual | Pending |
| TC-M11 | Read-only guarantee | DoD | Run a full comparison | No writes to either environment (verify via audit/no dialogs) | Manual | Pending |
| TC-M12 | Settings round-trip | US-MIG01.1.2 | Uncheck categories, close, reopen | The unchecked categories are still unchecked | Manual | Pending |

> Automated rows are executed by `dotnet test testing/UnitTests/UnitTests.csproj` (see TEST_SUMMARY).
> Save a screenshot under `screenshots/` for each manual case when executed against a live host.
