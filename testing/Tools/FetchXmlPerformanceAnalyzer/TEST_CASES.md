# FetchXML Performance Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

Automated cases live in `testing/UnitTests/FetchXmlAnalyzerTests.cs` and were executed with `dotnet test`.

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | Parse valid query populates counts | US-PERF03.2.1 | Parse a fetch with attrs, filter, order, outer link | RootEntity/attr/link/order/flags counts correct | Automated | Pass |
| TC-02 | Nested links counted at all depths | US-PERF03.2.1 | Parse fetch with a link inside a link | `LinkCount == 2` | Automated | Pass |
| TC-03 | Malformed XML → clear error | US-PERF03.1.1 | Parse unterminated element | `Success=false`, non-empty `Error` | Automated | Pass |
| TC-04 | Non-`<fetch>` root → error | US-PERF03.1.1 | Parse `<root>...</root>` | `Success=false`, error mentions fetch | Automated | Pass |
| TC-05 | `<all-attributes/>` → High | US-PERF03.3.1 | Analyze fetch with all-attributes | High finding + all-attributes suggestion | Automated | Pass |
| TC-06 | Missing root filter → High | US-PERF03.3.2 | Analyze fetch with no filter | High "No filter on the root entity" | Automated | Pass |
| TC-07 | Excessive links (>4) → High | US-PERF03.3.3 | Analyze fetch with 5 links | High "Excessive link-entity joins" | Automated | Pass |
| TC-08 | Several links (>2) → Medium | US-PERF03.3.3 | Analyze fetch with 3 links | Medium "Several link-entity joins" | Automated | Pass |
| TC-09 | Cost + band from findings | US-PERF03.4.1 | Analyze a high-risk fetch | `CostEstimate` in 1..100, Band High, matches `BandFor(_,15,40)` | Automated | Pass |
| TC-10 | Clean query → Info + Low | US-PERF03.4.1 | Analyze a bounded, filtered fetch | Single Info finding, Band Low, cost 0 | Automated | Pass |
| TC-11 | Analyze without connection | US-PERF03.1.1 | Paste FetchXML, click Analyze (not connected) | Grid + summary + suggestions populate; no connection prompt | Manual | Pending |
| TC-12 | Load from view | US-PERF03.1.2 | Click "Load from view...", pick a view | Picker lists system/personal views; chosen FetchXML loads into editor | Manual | Pending |
| TC-13 | Execute with timing (read-only) | US-PERF03.4.2 | Click "Execute with timing" on a valid query | Status bar shows elapsed ms + row count; no writes | Manual | Pending |
| TC-14 | Export JSON/HTML/Markdown/CSV | US-PERF03.5.2 | Analyze, then Export ▼ each format | Files written with findings/summary; JSON matches ReportModel shape | Manual | Pending |
| TC-15 | Settings round-trip | US-TT-0.1 | Enter FetchXML, close + reopen tool | Last FetchXML restored on load | Manual | Pending |
| TC-16 | Export Excel (*.xlsx) | US-PERF03.5.2 | Analyze, then Export ▼ → Excel (*.xlsx) | Valid .xlsx written with Summary/Findings/Checklist sheets; opens in Excel | Manual | Pending |
| TC-17 | Export PDF (*.pdf) | US-PERF03.5.2 | Analyze, then Export ▼ → PDF (*.pdf) | Native PDF written (score gauge, band, findings by category); opens in a viewer | Manual | Pending |

> Manual cases require a Windows + XrmToolBox session against a Dataverse org; save a screenshot per case under `screenshots/`.
