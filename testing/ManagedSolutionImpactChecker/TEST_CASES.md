# Managed Solution Impact Checker - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

Automated cases live in `testing/UnitTests/ManagedSolutionImpactCheckerTests.cs`.

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | Clean input → single Info, Low band | US-ALM4.5.1 | Evaluate an empty `LayerAnalysisInput` (Upgrade) | 1 Info finding, score 0, Low band; checklist + rollback + metrics still generated | Automated | Pass |
| TC-02 | Removed table on Upgrade → Critical (data loss) | US-ALM4.2.2 | Evaluate `RemovedComponents=["Entity: new_widget"]` on Upgrade | Critical "Table would be deleted (data loss)"; band High | Automated | Pass |
| TC-03 | Removed column on Upgrade → High | US-ALM4.2.2 | Evaluate `["Attribute: new_field"]` on Upgrade | High "Column would be deleted (data loss)" | Automated | Pass |
| TC-04 | Removed other component on Upgrade → Medium | US-ALM4.2.2 | Evaluate `["Web Resource: new_script.js"]` on Upgrade | Medium "Component would be deleted" | Automated | Pass |
| TC-05 | Removed components on Update/Patch do NOT delete | US-ALM4.3.1 | Evaluate removed set on Update and Patch | No High+ findings; one Info "not deleted on this path"; Low band | Automated | Pass |
| TC-06 | Removed table on Holding → Critical | US-ALM4.3.2 | Evaluate `["Entity: new_widget"]` on Holding | Critical data-loss finding (staged upgrade eventually deletes) | Automated | Pass |
| TC-07 | Unmanaged layer above managed on Upgrade | US-ALM4.1.2 / US-ALM4.2.1 | Evaluate a layer with `HasUnmanagedLayerAbove=true` on Upgrade | Medium "Unmanaged customization above managed layer" + High "Component would be overwritten" | Automated | Pass |
| TC-08 | Unmanaged layer above managed on Update → no overwrite | US-ALM4.2.1 | Same layer on Update | Medium detect only; NO overwrite finding | Automated | Pass |
| TC-09 | Missing dependency → High | US-ALM4.4.1 | Evaluate `MissingDependencies=[("Entity","new_prereq")]` | High "Missing dependency" | Automated | Pass |
| TC-10 | Publisher prefix mismatch → Medium | US-ALM4.4.1 | Evaluate src `abc` / tgt `xyz` | Medium "Publisher prefix mismatch" | Automated | Pass |
| TC-11 | Publisher prefix match → no finding | US-ALM4.4.1 | Evaluate src `abc` / tgt `ABC` | No mismatch finding | Automated | Pass |
| TC-12 | Restrictive managed properties → Medium, lists components | US-ALM4.4.2 | Two layers with `RestrictiveManagedProperties=true` | Medium finding listing both component names | Automated | Pass |
| TC-13 | Aggregate → High band + checklist + rollback | US-ALM4.5.1 / US-ALM4.5.2 | Evaluate combined risky input on Upgrade | High band, score ≥ 40, checklist has backup + dependency steps, rollback notes "cannot be rolled back", findings ordered worst-first | Automated | Pass |
| TC-14 | Null input throws | — | Evaluate `null` | `ArgumentNullException` | Automated | Pass |
| TC-15 | Tool loads and connects | US-ALM4.0.1 | Open the tool in XTB, connect, click "Refresh solutions" | Loads, connects, managed solutions list populates off-thread; settings persist on close | Manual | Pending |
| TC-16 | Analyze a managed solution (Upgrade) | US-ALM4.1.1 / US-ALM4.1.2 | Pick a managed solution + Upgrade, click "Analyze impact" | Summary shows score/band + path-deletes note; findings grid + checklist + rollback populate | Manual | Pending |
| TC-17 | Switch path Upgrade→Update re-runs semantics | US-ALM4.3.1 | Re-analyze same solution on Update | Deletion findings replaced by the informational "not deleted" note | Manual | Pending |
| TC-18 | Degraded query → Info note (no crash) | DoD | Connect as a low-privilege user / env without `msdyn_componentlayer` | Layer detection degrades to an Info finding; tool does not throw | Manual | Pending |
| TC-19 | Export CAB report | US-ALM4.5.2 | Export Excel / PDF / JSON / HTML | Each file writes off-thread and contains the score, findings, checklist + rollback | Manual | Pending |

> Automated rows are executed via `dotnet test`. Save a screenshot under `screenshots/` for each manual case when run against a live org.
