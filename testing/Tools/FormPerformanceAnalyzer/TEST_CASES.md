# Form Performance Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (SDK-free) — `testing/UnitTests/FormPerformanceAnalyzerTests.cs`

| ID | Case | Traces to | Inputs | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | Parser counts all components | US-PERF10.2.1/2.2/2.3 | A form with 3 tabs (1 hidden), 4 sections, 6 visible + 2 hidden fields, 2 subgrids, 1 quick view, 3 PCF, 2 libs, 2/3/1 handlers | Every count matches (Tabs 3/Hidden 1, Sections 4, Fields 8/Hidden 2, Subgrids 2, QuickViews 1, Custom 3, JsLibraries 2, handlers 2/3/1) | Automated | Pass |
| TC-02 | Handler-only library counted | US-PERF10.2.3 | onload handler referencing a library not in `<formLibraries>` | `JsLibraries` = 1, `OnLoadHandlers` = 1 | Automated | Pass |
| TC-03 | Blank/malformed → ParseFailed (no throw) | US-PERF10.1.2 | null / "" / whitespace / unclosed tag / non-XML | `ParseFailed` = true; counts 0; never throws | Automated | Pass |
| TC-04 | Score is deterministic | US-PERF10.3.1 | Same model + business-rule count scored twice | Identical score, band, finding + recommendation counts | Automated | Pass |
| TC-05 | Light form bands Light | US-PERF10.3.2 | 1 tab, 2 sections, 8 fields, nothing else | Band = Light, score < 25, no recommendations | Automated | Pass |
| TC-06 | Heavy form bands Heavy/Critical | US-PERF10.3.2 | 8 tabs, 14 sections, 60+ fields, 8 subgrids, 6 PCF, 6 libs, many handlers, 6 rules | Band = Heavy or Critical, score ≥ 50 | Automated | Pass |
| TC-07 | Many tabs → collapse recommendation | US-PERF10.4.1/4.2 | 8 tabs (budget 5) | A `TriggeredBy = Tabs`, Impact = Quick win recommendation; a "Many tabs" Medium+ finding | Automated | Pass |
| TC-08 | Fields/subgrids/scripts → structural recs | US-PERF10.4.1/4.2 | 45 visible fields, 6 subgrids, 5 libs | Structural recommendations triggered by Visible fields, Subgrids, Script libraries; each names its trigger | Automated | Pass |
| TC-09 | Parse-failed model scores Light + warning | US-PERF10.1.2 | A `ParseFailed` model | Score 0, band Light, single "could not be parsed" finding, no recommendations | Automated | Pass |
| TC-10 | Rank orders by score descending | US-PERF10.5.2 | Light + mid + heavy forms | Heaviest first, lightest last; scores non-increasing | Automated | Pass |

## Manual (live Dataverse + WinForms host) — capture a screenshot per case under `screenshots/`

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-11 | Tool loads in XrmToolBox | US-PERF10.1.1 | Open the tool; connect | Tool appears in the Tools list, loads, Help button present | Manual | Pending |
| TC-12 | Analyze all forms (scope confirm) | US-PERF10.1.1/1.3 | Click "Analyze forms" with no scope | Confirmation dialog appears; on OK the ranked grid populates off-thread with progress | Manual | Pending |
| TC-13 | Scope by table set | US-PERF10.1.3 | "Select tables…", check a few, re-analyze | Only forms for the chosen tables are scored; scope label updates; persists on reopen | Manual | Pending |
| TC-14 | Ranked grid + band colors | US-PERF10.3.2/5.2 | Analyze | Rows ranked by score, band color cue applied; summary shows band counts + top-10 heaviest | Manual | Pending |
| TC-15 | Metric breakdown pane | US-PERF10.6.1 | Select a form row | Every metric + its contribution shows in the breakdown grid | Manual | Pending |
| TC-16 | Recommendations pane | US-PERF10.4.1/4.2 | Select a heavy form | Recommendations show with Impact/Effort/Trigger, sorted by impact | Manual | Pending |
| TC-17 | Compare two forms | US-PERF10.6.2 | Ctrl+click two rows; "Compare…" | Side-by-side metric deltas; no writes | Manual | Pending |
| TC-18 | Score settings round-trip | US-PERF10.3.3 | "Score settings…", change a weight/threshold, reset, OK; close + reopen | Edits apply on re-score; reset restores defaults; settings persist | Manual | Pending |
| TC-19 | Export CSV | US-PERF10.5.1 | Export ▸ CSV | `.csv` with forms + metrics + score + band + recommendations columns | Manual | Pending |
| TC-20 | Export HTML | US-PERF10.5.1 | Export ▸ HTML | Self-contained themed report with band summary, ranked table, recommendations | Manual | Pending |

> The ten automated rows are executed by `dotnet test`. Execute the manual rows in a Windows + XrmToolBox
> session against a real org and save a screenshot per case under `screenshots/` (plus the required
> `xrmtoolbox-tools-list.png`).
