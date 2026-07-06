# Component Usage Explorer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (xUnit — `testing/UnitTests/ComponentUsageExplorerTests.cs`, executed)

| ID | Case | Traces to | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-01 | No dependents → SafeToChange | US-SOLN01.4.1 | Verdict SafeToChange, band Low, "No dependent components found" finding | Automated | Pass |
| TC-02 | A few plain dependents → ChangeWithCaution | US-SOLN01.4.1 | Verdict ChangeWithCaution, band Medium | Automated | Pass |
| TC-03 | High-value dependents (form/flow/plugin) → HighImpact | US-SOLN01.4.1 | Verdict HighImpact, band High | Automated | Pass |
| TC-04 | Many plain dependents (≥ threshold) → HighImpact | US-SOLN01.4.1 | Verdict HighImpact | Automated | Pass |
| TC-05 | Managed dependents → RequiresAlmReview | US-SOLN01.3.2/4.1 | Verdict RequiresAlmReview, managed-dependent finding present | Automated | Pass |
| TC-06 | Cross-solution dependents → RequiresAlmReview | US-SOLN01.3.2/4.1 | Verdict RequiresAlmReview, cross-solution finding present | Automated | Pass |
| TC-07 | Table with many dependents → DoNotDelete | US-SOLN01.4.1 | Verdict DoNotDelete, band High | Automated | Pass |
| TC-08 | Incomplete dependency data → RequiresDependencyReview | US-SOLN01.2.2 | Verdict RequiresDependencyReview, "Dependency data incomplete" finding | Automated | Pass |
| TC-09 | Incomplete data does not downgrade a higher verdict | US-SOLN01.2.2/4.1 | High-value deps + incomplete → still HighImpact | Automated | Pass |
| TC-10 | Score increases with verdict severity, stays 0–100 | US-SOLN01.4.1 | safe < caution < doNotDelete, all in range | Automated | Pass |
| TC-11 | UsageByType tallies dependents by type name | US-SOLN01.3.3 | 2 Saved Query, 1 System Form | Automated | Pass |
| TC-12 | Evaluate populates dependent-count metric | US-SOLN01.3.3 | "Dependent components" metric = "2" | Automated | Pass |
| TC-13 | Explanation names verdict + next steps | US-SOLN01.4.2 | Non-empty, contains "High impact" and "Next:" | Automated | Pass |

## Manual (GUI + Dataverse — pending execution in a Windows + XrmToolBox session)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-M1 | Tool loads + connects | US-SOLN01.1.1 | Open the tool, connect to an env | Loads, connects, status prompts to search; settings persist on close | Manual | Pending |
| TC-M2 | Search by name / GUID / type | US-SOLN01.1.1/1.2 | Enter a name, then a GUID, then pick a type + Find | Results grid shows type/name/schema/owning solutions/managed, off-thread with progress | Manual | Pending |
| TC-M3 | Analyze usage builds footprint | US-SOLN01.2.1 | Select a component, click "Analyze usage" | Required + dependent grids populate; usage-by-type grid populates | Manual | Pending |
| TC-M4 | Verdict banner + explanation | US-SOLN01.4.1/4.2 | Observe the banner after analyze | Coloured banner shows verdict + score/band; explanation text names dependents + next steps | Manual | Pending |
| TC-M5 | Incomplete dependency degradation | US-SOLN01.2.2 | Analyze a component the APIs cannot fully resolve | "Dependency data incomplete" finding; verdict not reported as Safe | Manual | Pending |
| TC-M6 | Export Excel/PDF/JSON/HTML | US-SOLN01.5.1 | Export each format | Files written off-thread; JSON carries verdict/score/findings; HTML self-contained | Manual | Pending |
