# Duplicate Metadata Finder - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

Automated cases are the xUnit facts in `testing/UnitTests/DuplicateMetadataFinderTests.cs` (run with
`dotnet test testing/UnitTests/UnitTests.csproj`). Manual cases need a live org + XrmToolBox.

## Automated (SDK-free)

| ID | Case | Traces to | xUnit fact | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-DMF-01 | Name normalization | US-ADMIN3.2.1 | `Normalize_StripsCaseAndSeparators` | Case/separators stripped; null → "" | Automated | Pass |
| TC-DMF-02 | Levenshtein distance | US-ADMIN3.2.1 | `Levenshtein_KnownDistance` | 0 / 1 / 3 for the sample inputs | Automated | Pass |
| TC-DMF-03 | Name ratio identical after normalize | US-ADMIN3.2.1 | `NameRatio_IdenticalAfterNormalize_IsOne` | 1.0 | Automated | Pass |
| TC-DMF-04 | Two blank names are not a match | US-ADMIN3.2.1 | `NameRatio_TwoBlanks_IsZero_NotOne` | 0.0 | Automated | Pass |
| TC-DMF-05 | Tokenize camelCase/separators | US-ADMIN3.2.1 | `Tokenize_SplitsCamelCaseAndSeparators` | `[new, phone, number]` | Automated | Pass |
| TC-DMF-06 | Jaccard set overlap | US-ADMIN3.2.2 | `Jaccard_OverlapRatio` | 1/3, 1.0, 0.0 | Automated | Pass |
| TC-DMF-07 | Content hash stable + sensitive | US-ADMIN3.3.2 | `ContentHash_StableAndContentSensitive` | Equal for same, differs for different | Automated | Pass |
| TC-DMF-08 | Identical columns score 100 | US-ADMIN3.2.1 | `Score_IdenticalColumns_Is100` | 100 | Automated | Pass |
| TC-DMF-09 | Type mismatch lowers score | US-ADMIN3.2.1 | `Score_DifferentType_LowersScore` | diff < same | Automated | Pass |
| TC-DMF-10 | Cross-kind pair scores 0 | US-ADMIN3.3.1 | `Score_CrossKind_IsZeroAndNeverGroups` | 0, never grouped | Automated | Pass |
| TC-DMF-11 | Exact content hash → 100 (exact) | US-ADMIN3.3.2 | `Score_IdenticalContentHash_ShortCircuitsTo100_Exact` | 100, IsExactContentMatch | Automated | Pass |
| TC-DMF-12 | Option-set overlap contributes | US-ADMIN3.2.2 | `Score_OptionSetOverlap_Contributes` | Higher when values overlap | Automated | Pass |
| TC-DMF-13 | Transitive clustering across containers | US-ADMIN3.4.1 | `Group_ClustersTransitiveDuplicates_AcrossContainers` | One 3-member group | Automated | Pass |
| TC-DMF-14 | Threshold filters weak pairs | US-ADMIN3.1.2 | `Group_ThresholdFiltersWeakPairs` | No groups | Automated | Pass |
| TC-DMF-15 | Recommend most-referenced primary | US-ADMIN3.4.2 | `Group_RecommendsMostReferencedPrimary` | Higher-usage member; reason cites usage | Automated | Pass |
| TC-DMF-16 | Tie-break toward managed | US-ADMIN3.4.2 | `Group_TieBreaksTowardManagedDeterministically` | Managed member kept | Automated | Pass |
| TC-DMF-17 | Groups ranked worst-first | US-ADMIN3.4.1 | `Group_RanksWorstFirst` | Top group has highest score | Automated | Pass |
| TC-DMF-18 | Empty/single → no groups | US-ADMIN3.4.1 | `Group_EmptyOrSingle_NoGroups` | Empty | Automated | Pass |
| TC-DMF-19 | Report projects metrics + one finding/group | US-ADMIN3.5.1 | `Report_Projects_MetricsAndOneFindingPerGroup` | Findings == groups; metrics present | Automated | Pass |
| TC-DMF-20 | Empty scan → zero score, no findings | US-ADMIN3.5.1 | `Report_EmptyScan_ZeroScoreNoFindings` | Score 0, no findings | Automated | Pass |
| TC-DMF-21 | Exact content match → High severity | US-ADMIN3.5.1 | `Report_ExactContentMatch_IsHighSeverity` | High | Automated | Pass |
| TC-DMF-22 | Overall score caps at 100 | US-ADMIN3.5.1 | `Report_ScoreCapsAt100` | 100 | Automated | Pass |

## Manual (live Dataverse + XrmToolBox)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-DMF-M-01 | Tool loads in XrmToolBox | EPIC-ADMIN3 | Open XrmToolBox → Tools → "Duplicate Metadata Finder" | Appears with name/version/description/icon (MEF + dep chain resolved at scan time); shot `screenshots/xrmtoolbox-tools-list.png` | Manual | **Pass** (FlaUI `UiSmokeTests`, 2026-07-05) |
| TC-DMF-M-02 | Scan runs off-thread with progress | US-ADMIN3.1.1 | Connect, click "Scan for duplicates" | Spinner + progress messages; UI responsive; groups fill the grid | Manual | Pending |
| TC-DMF-M-03 | Component-type + custom-only filters | US-ADMIN3.1.1 | Uncheck some types / toggle "Custom only", rescan | Only selected kinds scanned; managed/system excluded when custom-only | Manual | Pending |
| TC-DMF-M-04 | Threshold tuning | US-ADMIN3.1.2 | Change "Similarity ≥" and rescan | Fewer/more groups as threshold rises/falls | Manual | Pending |
| TC-DMF-M-05 | Group detail + recommendation | US-ADMIN3.4 | Select a group row | Detail pane shows members, scored pairs/factors, recommended keep + reason | Manual | Pending |
| TC-DMF-M-06 | Export Excel/PDF | US-ADMIN3.5.1 | Export → Excel, then PDF | Files open; contain groups, scores, recommended keeps | Manual | Pending |
| TC-DMF-M-07 | Export JSON/HTML | US-ADMIN3.5.1 | Export → JSON, then HTML | JSON carries groups/scores/keeps; HTML self-contained + themed | Manual | Pending |
| TC-DMF-M-08 | Settings round-trip | US-ADMIN3.1.2 | Set options/threshold, close, reopen | Options and threshold restored | Manual | Pending |
| TC-DMF-M-09 | Degraded scan note | US-ADMIN3.1.1 | Scan with a restricted role | Failing kinds recorded as notes; scan still completes | Manual | Pending |

> Save a screenshot under `screenshots/` for each manual case; the load shot MUST be
> `screenshots/xrmtoolbox-tools-list.png`.
