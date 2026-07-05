# Solution Merge Assistant - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (SDK-free engine — `testing/UnitTests/SolutionMergeAssistantTests.cs`)

| ID | Case | Traces to | Inputs | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-A1 | Duplicate web resource across 2 solutions | US-ALM01.2.1/2.2 | Same `(WebResource, objectid)` in solA + solB | One `Web Resources` finding, Medium, names both owners | Automated | Pass |
| TC-A2 | Plugin assembly overlap is its own High category | US-ALM01.2.2 | Same `(PluginAssembly, objectid)` in both | One `Plugin Assemblies` finding, High | Automated | Pass |
| TC-A3 | Non-overlapping components ⇒ no duplicate | US-ALM01.2.1 | Distinct object ids | No "Duplicate ..." finding | Automated | Pass |
| TC-A4 | Managed-in-one / unmanaged-in-another | US-ALM01.3.2 | Shared component, managed=true vs false | `Managed State` finding High; verdict HighRisk | Automated | Pass |
| TC-A5 | Publisher prefix mismatch + standard pick | US-ALM01.3.1 | Prefixes `contoso` (2 comps) vs `new` (1) | `Publisher` finding Medium; strategy standardizes on `contoso` | Automated | Pass |
| TC-A6 | Same publisher ⇒ no publisher finding | US-ALM01.3.1 | Both `new` | No `Publisher` finding | Automated | Pass |
| TC-A7 | Version difference with overlap ⇒ Medium | US-ALM01.3.1 | v1 vs v2, shared component | `Version` finding Medium | Automated | Pass |
| TC-A8 | Env-var conflict (different values) ⇒ High | US-ALM01.4.1 | `new_ApiUrl` = a vs b | `Environment Variables` finding High | Automated | Pass |
| TC-A9 | Connection ref identical in both ⇒ Medium | US-ALM01.4.1 | Same conn ref value in both | `Connection References` finding Medium | Automated | Pass |
| TC-A10 | Config item in one solution only ⇒ not a conflict | US-ALM01.4.1 | `new_only` in solA only | No env-var finding | Automated | Pass |
| TC-A11 | Clean merge ⇒ SafeToMerge | US-ALM01.5.1 | Distinct comps, same prefix/version | Verdict SafeToMerge, score 0, band Low | Automated | Pass |
| TC-A12 | Many High conflicts ⇒ DoNotMerge | US-ALM01.5.1 | 3 shared plugin assemblies | Verdict DoNotMerge | Automated | Pass |
| TC-A13 | Single solution ⇒ Info + SafeToMerge | US-ALM01.5.1 | One solution | Info finding; verdict SafeToMerge | Automated | Pass |
| TC-A14 | Comparison is deterministic | Definition of Done | Same inputs run twice (unordered) | Identical verdict/score/findings/strategy | Automated | Pass |

## Manual (live Dataverse + WinForms host)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-M1 | Load solutions | US-ALM01.1.1 | Connect, click **Load solutions** | Checked list fills with visible solutions (name/version, `[managed]` marked); runs off-thread | Manual | Pending |
| TC-M2 | Compare 2 solutions | US-ALM01.1.2 / 1.5.1 | Check two solutions, click **Compare** | Progress spinner; verdict banner + conflict grid + strategy + checklist populate | Manual | Pending |
| TC-M3 | Overlap surfaced from a real org | US-ALM01.2.1 | Compare two solutions that share a component | Duplicate finding lists both owning solutions with a resolved component name | Manual | Pending |
| TC-M4 | Env-var / conn-ref conflict from real org | US-ALM01.4.1 | Compare solutions sharing an env var/conn ref with different values | High config-conflict finding | Manual | Pending |
| TC-M5 | Export Excel | US-ALM01.6.1 | Compare, Export ▸ Excel | `.xlsx` with Summary/Findings/Checklist sheets | Manual | Pending |
| TC-M6 | Export PDF | US-ALM01.6.1 | Compare, Export ▸ PDF | Native PDF renders verdict/score + findings | Manual | Pending |
| TC-M7 | Export JSON | US-ALM01.6.1 | Compare, Export ▸ JSON | JSON carries `verdict`, `solutions`, `metrics`, `checklist`, `conflicts[]` | Manual | Pending |
| TC-M8 | Export HTML | US-ALM01.6.1 | Compare, Export ▸ HTML | Self-contained, theme-aware dashboard opens offline | Manual | Pending |
| TC-M9 | Settings round-trip | US-ALM01.1.1 | Check solutions, close + reopen | Previously checked solutions are re-checked after reload | Manual | Pending |
| TC-M10 | Cancellation | US-ALM01.1.2 | Compare, cancel mid-run | Operation stops cleanly; UI stays responsive | Manual | Pending |

> Automated rows are executed by `SolutionMergeAssistantTests.cs`. Manual rows require Windows + XrmToolBox +
> a Dataverse connection and a screenshot per case under `screenshots/`.
