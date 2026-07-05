# View Performance Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (SDK-free) — `testing/UnitTests/ViewPerformanceAnalyzerTests.cs`

| ID | Case | Traces to | Inputs | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | LayoutXML counts displayed cells | US-PERF4.3.1 | A `<grid>` with 4 named cells | `CountColumns` = 4; `Columns` = col0..col3 | Automated | Pass |
| TC-02 | LayoutXML ignores hidden cells | US-PERF4.3.1 | A cell with `ishidden='1'` | Hidden cell not counted / not listed | Automated | Pass |
| TC-03 | LayoutXML tolerates blank/malformed | US-PERF4.3.1 | null / "" / whitespace / unclosed cell | `CountColumns` = 0; `Columns` empty (no throw) | Automated | Pass |
| TC-04 | Heavy view scores High | US-PERF4.4.1 | all-attributes + no filter + 5 links + 20-col layout | Band = High, score ≥ 40; engine High finding + "Over-wide" View finding present | Automated | Pass |
| TC-05 | Lean view scores Low | US-PERF4.4.1 | filtered, bounded, 3-col layout | Band = Low, score 0, no Medium+ findings, an Info finding | Automated | Pass |
| TC-06 | Over-wide layout raises Medium | US-PERF4.3.1 | lean fetch + 25-col layout | A Medium "Over-wide view layout" View finding; score > 0 | Automated | Pass |
| TC-07 | Parse failure degrades to Info | US-PERF4.2.1 | malformed FetchXML + 2-col layout | Score 0, Band Low, single Info finding "could not be parsed"; layout still counted (2) | Automated | Pass |
| TC-08 | Rank orders by score desc | US-PERF4.4.2 | heavy + mid + lean views | Heavy first, lean last; scores non-increasing | Automated | Pass |

## Manual (live Dataverse + WinForms host) — capture a screenshot per case under `screenshots/`

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-09 | Tool loads in XrmToolBox | US-PERF4.1.1 | Open the tool; connect | Tool appears in Tools list, loads, Help button present | Manual | Pending |
| TC-10 | Refresh tables & pick a table | US-PERF4.1.1 | Click "Refresh tables"; pick e.g. Account | Table list populates; selection persists on reopen | Manual | Pending |
| TC-11 | Analyze system views | US-PERF4.1.1 / 4.4.2 | Click "Analyze views" | Ranked grid populates off-thread with progress; score cards show band counts | Manual | Pending |
| TC-12 | Include personal views | US-PERF4.1.2 | Toggle "Include personal views"; re-analyze | Personal (`userquery`) views appear with Type = Personal | Manual | Pending |
| TC-13 | Per-view detail panels | US-PERF4.2.2 / 4.3.2 | Select a ranked row | FetchXML, layout columns, and that view's findings show | Manual | Pending |
| TC-14 | Time selected view (opt-in) | US-PERF4.5.1 | Select a view; click "Time selected view" | Read-only, capped execution; elapsed ms + row count in status bar | Manual | Pending |
| TC-15 | Export Excel | US-PERF4.5.2 | Export ▸ Excel | `.xlsx` with Summary/Findings sheets opens | Manual | Pending |
| TC-16 | Export PDF | US-PERF4.5.2 | Export ▸ PDF | Native PDF with gauge + findings renders | Manual | Pending |
| TC-17 | Export JSON/HTML/Markdown/CSV | US-PERF4.5.2 | Export ▸ each format | Files contain the ranked views / aggregated findings | Manual | Pending |
| TC-18 | Settings round-trip | US-PERF4.1.2 | Set table + include-personal; close; reopen | Last table + toggle restored | Manual | Pending |

> The eight automated rows are executed by `dotnet test`. Execute the manual rows in a Windows + XrmToolBox
> session against a real org and save a screenshot per case under `screenshots/`.
