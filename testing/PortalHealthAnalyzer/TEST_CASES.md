# Portal Health Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

Automated cases live in `testing/UnitTests/PortalHealthAnalyzerTests.cs` and were executed on the SDK-free
engine (see TEST_SUMMARY.md). Manual cases require a live Dataverse connection with a Power Pages website and
the XrmToolBox host, so they are Pending until run in a Windows session.

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-01 | Clean site scores Low | US-PP01.6.1 | Evaluate a fully-healthy fixture | Band Low, score 0, no ≥Medium findings | Automated | Pass |
| TC-02 | Missing page template → High | US-PP01.3.1 | Page with no `TemplateId` | High "page template" finding + recommendation | Automated | Pass |
| TC-03 | Dangling page-template ref → High | US-PP01.3.1 | Page `TemplateId` not in inventory | High "missing page template" | Automated | Pass |
| TC-04 | Missing parent page → High | US-PP01.3.1 | Page `ParentId` not present | High "missing parent" | Automated | Pass |
| TC-05 | Inactive page → Medium | US-PP01.3.2 | Page `Active = false` | Medium "Inactive" structural finding | Automated | Pass |
| TC-06 | Web file referenced but absent → High | US-PP01.3.2 | Referenced web-file id with no record | High "Web file referenced but absent" | Automated | Pass |
| TC-07 | Form bound to missing table → High | US-PP01.3.3 | Form `EntityExists = false` | High "missing/disabled table" | Automated | Pass |
| TC-08 | Missing required setting → High | US-PP01.4.1 | Drop a required site setting | High "Missing required site setting" | Automated | Pass |
| TC-09 | Duplicate setting → Medium | US-PP01.4.2 | Same setting name twice | Medium "Duplicate" with conflicting values | Automated | Pass |
| TC-10 | Anonymous permission → Critical + High | US-PP01.5.1 | Permission `AnonymousReadWriteOrDelete = true` | Critical roll-up + per-permission High; band High | Automated | Pass |
| TC-11 | Global-scope permission → Medium | US-PP01.5.2 | Permission `Scope = Global` | Medium "Global-scope" | Automated | Pass |
| TC-12 | Deterministic score/band | US-PP01.6.1 | Evaluate same fixture twice | Equal score, band, finding count | Automated | Pass |
| TC-13 | Both schemas normalize identically | US-PP01.2.1 | Evaluate adx + mspp clean fixtures | Equal score/band/finding count, both Low | Automated | Pass |
| TC-14 | Unavailable table degrades to Info | US-PP01.2.1 | Inventory with an unavailable table | Info "not available" finding; band stays Low | Automated | Pass |
| TC-15 | Website discovery lists both schemas | US-PP01.1.1 | Connect, click "Load websites" | Combo lists `adx_`/`mspp_` sites with a schema badge; off-thread with progress | Manual | Pending |
| TC-16 | Last website remembered | US-PP01.1.2 | Select a site, close, reopen | Selection restored from settings (id + schema only) | Manual | Pending |
| TC-17 | Analyze populates dashboard | US-PP01.2.2 / 1.6.1 | Pick a site, click "Analyze" | Score+band header, config summary counts, categorized issue grid | Manual | Pending |
| TC-18 | Missing table doesn't throw (live) | US-PP01.2.1 | Analyze a site whose schema is partly provisioned | Skipped table listed as Unavailable; no exception | Manual | Pending |
| TC-19 | Export all formats | US-PP01.6.3 | Export Excel/PDF/Word/JSON/HTML/CSV | Each file opens and contains the score + findings | Manual | Pending |

> Each automated row is an xUnit `[Fact]` in `PortalHealthAnalyzerTests.cs`. Save a screenshot under
> `screenshots/` for each manual case when executed in a live session.
