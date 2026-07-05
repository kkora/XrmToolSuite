# JavaScript Performance Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (SDK-free rule engine + FormXML mapper) — `testing/UnitTests/JavaScriptPerformanceAnalyzerTests.cs`

| ID | Case | Traces to | Inputs | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-A01 | `Xrm.Page` flagged Medium with line context | US-PERF8.2.1 | Snippet using `Xrm.Page` on line 2 | Medium finding; `Line == 2`; context contains `Xrm.Page`; confidence "heuristic…" | Automated | Pass |
| TC-A02 | Synchronous XHR `open(...,false)` → High | US-PERF8.2.1 | `x.open('GET', url, false)` on line 2 | High finding; `Line == 2` | Automated | Pass |
| TC-A03 | `async:false` → High | US-PERF8.2.1 | `$.ajax({ async: false })` | High "Synchronous" finding | Automated | Pass |
| TC-A04 | Blocking `alert(` → High | US-PERF8.2.2 | `alert('saved')` on line 2 | High finding; `Line == 2` | Automated | Pass |
| TC-A05 | `console.*` over threshold → Low | US-PERF8.2.2 | 12 `console.log` (threshold 10) | Low "console" finding | Automated | Pass |
| TC-A06 | `console.*` under threshold not flagged | US-PERF8.2.2 | 3 `console.log` | No console finding | Automated | Pass |
| TC-A07 | Hardcoded GUID → Medium | US-PERF8.2.3 | GUID literal | Medium finding; confidence "heuristic…" | Automated | Pass |
| TC-A08 | Hardcoded URL → Medium | US-PERF8.2.3 | `https://…` literal | Medium "URL" finding | Automated | Pass |
| TC-A09 | Repeated retrieves → Medium | US-PERF8.2.2 | 4 retrieve/`Xrm.WebApi` calls (threshold 3) | Medium "retrieval" finding | Automated | Pass |
| TC-A10 | DOM manipulation → Medium | US-PERF8.2.3 | `document.getElementById(...)` | Medium "DOM" finding | Automated | Pass |
| TC-A11 | Size bands (High/Low) | US-PERF8.4.1 | 150 B / 300 B with 100/200 thresholds | Low "Large script" / Medium "Very large script" | Automated | Pass |
| TC-A12 | Whole-line comment skipped | US-PERF8.2.3 | `// Xrm.Page …` comment line | No `Xrm.Page` finding | Automated | Pass |
| TC-A13 | Clean script → single Info, score 0, Low | US-PERF8.4.1 | Modern executionContext snippet | One Info finding; score 0; band Low | Automated | Pass |
| TC-A14 | Risky script scores High band | US-PERF8.4.1 | Xrm.Page + alert + 2 sync XHR | Score ≥ 40; band High | Automated | Pass |
| TC-A15 | `Rank` orders by score desc | US-PERF8.4.1 | Clean + risky scripts | Riskiest first | Automated | Pass |
| TC-A16 | `FormEventMap.Map` links library→form→event | US-PERF8.3.1 | FormXML with onload/onchange/onsave handlers | 4 usages incl. `OnChange(telephone1)`, `OnSave` | Automated | Pass |
| TC-A17 | `OnLoadHandlerCount` counts only OnLoad | US-PERF8.3.2 | FormXML with 2 onload handlers | Returns 2 | Automated | Pass |
| TC-A18 | Blank/malformed FormXML → 0 / empty | US-PERF8.3.1 | null / "" / malformed | Count 0; empty map | Automated | Pass |

## Manual (live Dataverse + WinForms host)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-M01 | Tool loads and connects | US-PERF8.1.1 | Open the tool, connect | Loads; Help button present; status prompts to analyze | Manual | Pending |
| TC-M02 | Analyze web resources | US-PERF8.1.1 | Click "Analyze web resources" | Off-thread retrieval; grid ranked by score (Score, Band, Script, Size, #Findings) | Manual | Pending |
| TC-M03 | Search filters by code content | US-PERF8.1.2 | Type a token in "Search code" | Grid filters to scripts whose code contains the token; status shows N of M | Manual | Pending |
| TC-M04 | Findings detail with line/context/confidence | US-PERF8.2.x | Select a flagged script | Findings grid shows Severity, Title, Line, Context, Confidence, Recommendation; code panel shows the source | Manual | Pending |
| TC-M05 | Form/event usage panel | US-PERF8.3.1 | Select a script used on a form | Usage panel lists entity — form · event → function | Manual | Pending |
| TC-M06 | Heavy OnLoad form flagged | US-PERF8.3.2 | Analyze an env with a form over the OnLoad threshold | Dashboard reports "Forms with heavy OnLoad" count | Manual | Pending |
| TC-M07 | Settings round-trip | US-PERF8.1.2 | Set a search, close, reopen | Last search restored | Manual | Pending |
| TC-M08 | Excel/PDF export | US-PERF8.4.2 | Export ▼ → Excel, then PDF | Files open; contain metrics + findings with script as Component | Manual | Pending |
| TC-M09 | JSON/HTML/Markdown/CSV export | US-PERF8.4.2 | Export each format | Well-formed files with the expected shape/columns | Manual | Pending |

> Automated rows are executed via `dotnet test`; manual rows require a Windows + XrmToolBox session against a
> real org — capture a screenshot per manual case under `screenshots/`.
