# Deployment Risk Analyzer - Test Cases

Status: `Pass` / `Fail` / `Pending (manual - needs live org)`.
Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated - Risk scoring & banding (US-ALM07-8.1)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj`. Evidence: [`test-run.txt`](test-run.txt).

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ALM07-SCORE-01 | No findings | (empty) | Score 0, Risk Low | Automated | Pass |
| TC-ALM07-SCORE-02 | Per-severity weights | one finding of each severity | Low=2, Medium=5, High=12, Critical=25, Info=0 | Automated | Pass |
| TC-ALM07-SCORE-03 | Weights sum | High+Medium+Low | Score 19 | Automated | Pass |
| TC-ALM07-SCORE-04 | Cap at 100 | 10x Critical (raw 250) | Score 100 | Automated | Pass |
| TC-ALM07-SCORE-05 | Info is weightless | 3x Info | Score 0, Low | Automated | Pass |
| TC-ALM07-BAND-06 | Below Medium threshold | 2x Medium + Low = 12 | Risk Low | Automated | Pass |
| TC-ALM07-BAND-07 | Medium boundary | 3x Medium = 15 | Risk Medium | Automated | Pass |
| TC-ALM07-BAND-08 | Just below High threshold | 3x High + Low = 38 | Risk Medium | Automated | Pass |
| TC-ALM07-BAND-09 | High boundary | 8x Medium = 40 | Risk High | Automated | Pass |
| TC-ALM07-BAND-10 | Critical forces High | 1x Critical (score 25) | Risk High (not Medium) | Automated | Pass |
| TC-ALM07-BAND-11 | Critical cannot be downgraded | 1x Critical | Risk High | Automated | Pass |
| TC-ALM07-EXPLAIN-12 | Explain text | one of each severity | Contains counts, "score 44/100", "High risk" | Automated | Pass |
| TC-ALM07-SUMMARY-13 | Severity tallies | 2x High + Low | CountBySeverity + SeveritySummary correct | Automated | Pass |

## Automated - Analyzer logic via fake connection (US-ALM07-2..7)

Executed via `dotnet test testing/AnalyzerTests/AnalyzerTests.csproj`. These drive the real analyzer
classes against an in-memory `FakeOrganizationService` (no live org) - proving the analyzers stay
UI-free and liftable into a console/CI wrapper. Schema attribute/relationship comparisons that need
constructed `EntityMetadata` are seeded via a reflection builder (`Fakes/MetaBuilder.cs`) and run here too
(TC-ALM07-SC-06..10). Paths needing aliased LEFT-OUTER joins or richer metadata (plugin-step health,
security role/field metadata, duplicate unmanaged layers, publisher `Retrieve`) remain manual - see TC-ALM07-M-*.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ALM07-EV-01 | Secret variable | env var of type Secret | High "requires manual setup" | Automated | Pass |
| TC-ALM07-EV-02 | Value packaged in solution | env var with a value row in-solution | Medium "VALUE packaged in solution" | Automated | Pass |
| TC-ALM07-EV-03 | No default, no value, no target | bare env var, no target | Medium "no default value" | Automated | Pass |
| TC-ALM07-EV-04 | Has default, no target | env var with default | No non-Info EV finding | Automated | Pass |
| TC-ALM07-EV-05 | New to target, no default | source-only env var, target connected | High "new to target" | Automated | Pass |
| TC-ALM07-EV-06 | Exists in target but unset | env var present, no value/default in target | High "unset in target" | Automated | Pass |
| TC-ALM07-EV-07 | Data-source var unbound | data-source var, no target binding | Medium "unbound in target" | Automated | Pass |
| TC-ALM07-EV-08 | Connection ref, no target | connection reference, no target | Info "must be bound at import" | Automated | Pass |
| TC-ALM07-EV-09 | Connection ref unbound in target | conn ref exists in target, no connection | High "unbound in target" | Automated | Pass |
| TC-ALM07-SC-01 | Schema check, no target | no target connection | Single Info "check skipped" | Automated | Pass |
| TC-ALM07-SC-02 | Version not incremented | source version <= target | High "not incremented" | Automated | Pass |
| TC-ALM07-SC-03 | Version incremented | source version > target | Info "managed upgrade will run" | Automated | Pass |
| TC-ALM07-SC-04 | Managed/unmanaged mismatch | source managed, target unmanaged | Critical "mismatch" | Automated | Pass |
| TC-ALM07-SC-05 | Solution absent from target | target has no such solution | No version/managed finding | Automated | Pass |
| TC-ALM07-SC-06 | Attribute type mismatch | source String vs target Integer (same column) | Critical "Attribute type mismatch" | Automated | Pass |
| TC-ALM07-SC-07 | String length shrink | source maxlength 100 vs target 200 | High "Column max length reduced" | Automated | Pass |
| TC-ALM07-SC-08 | Choice label conflict | value 1 labelled "Alpha" vs "Beta" | Medium "Choice value label conflict" | Automated | Pass |
| TC-ALM07-SC-09 | Choice value removed | target has value 2, source does not | High "Choice value removed" | Automated | Pass |
| TC-ALM07-SC-10 | Relationship shape collision | same schema name, different referencing entity | High "Relationship schema name collision" | Automated | Pass |
| TC-ALM07-FP-01 | Draft cloud flow | modern flow, statecode Draft | Medium "Cloud flow is OFF (draft)" | Automated | Pass |
| TC-ALM07-FP-02 | Draft classic process | classic workflow, statecode Draft | Low "Process is in Draft state" | Automated | Pass |
| TC-ALM07-FP-03 | Activated flow, known conn ref | active flow, conn ref exists | No finding | Automated | Pass |
| TC-ALM07-FP-04 | Flow -> missing conn ref | flow clientdata names an absent conn ref | High "references missing connection reference" | Automated | Pass |
| TC-ALM07-FP-05 | Duplicate step, no filters | same plugin type twice on one event, no filtering attrs | High "Duplicate SDK step registration" | Automated | Pass |
| TC-ALM07-FP-06 | Same type, disjoint filters | same type, filtering attrs "name" vs "telephone1" | No duplicate finding | Automated | Pass |
| TC-ALM07-FP-07 | Same type, overlapping filters | same type, one all-attrs vs one "name" | High "Duplicate SDK step registration" | Automated | Pass |
| TC-ALM07-FP-08 | Duplicate, one disabled | same type twice, one step statecode Disabled | No duplicate finding | Automated | Pass |
| TC-ALM07-FP-09 | Different types, same rank | two types on one event sharing rank 1 | Medium "share an execution rank" | Automated | Pass |
| TC-ALM07-FP-10 | Different types, distinct ranks | two types on one event at ranks 1 and 2 | No rank-conflict finding | Automated | Pass |
| TC-ALM07-PP-01 | No portal | no adx_/mspp_ tables | Single Info "No Power Pages site detected" | Automated | Pass |
| TC-ALM07-PP-02 | Site, no web roles | adx_website, no web roles | High "No web roles found" | Automated | Pass |
| TC-ALM07-PP-03 | Table on form, no permission | form surfaces a table, no permission record | High "used on portal without table permission" | Automated | Pass |
| TC-ALM07-PP-04 | Form permissions OFF | basic form with table permissions disabled | High "bypasses table permissions" | Automated | Pass |
| TC-ALM07-PP-05 | Web file no content | web file with no document annotation | Medium "Web file has no content" | Automated | Pass |
| TC-ALM07-PP-06 | Empty content snippet | snippet with blank value | Low "Empty content snippet" | Automated | Pass |
| TC-ALM07-PP-07 | Enhanced mspp_ schema | mspp_website present | Detected (no "no site" Info) | Automated | Pass |
| TC-ALM07-DEP-01 | Unmanaged solution | ismanaged = false | Medium "Solution is unmanaged" | Automated | Pass |
| TC-ALM07-DEP-02 | Managed solution | ismanaged = true | No unmanaged finding | Automated | Pass |
| TC-ALM07-DEP-03 | No missing dependencies | empty missing-dependency set | Info "No missing dependencies" | Automated | Pass |
| TC-ALM07-DEP-04 | Missing dependency | one required Entity component | High "Missing dependency" | Automated | Pass |
| TC-ALM07-DC-01 | Deleted-components, no target | no target connection | Info "check skipped" | Automated | Pass |
| TC-ALM07-DC-02 | Solution absent from target | target has no such solution | Info "No prior version in target" | Automated | Pass |
| TC-ALM07-DC-03 | Managed target, table removed | table in target not in source | Critical "Entity deleted on managed upgrade" | Automated | Pass |
| TC-ALM07-DC-04 | Managed target, column removed | attribute in target not in source | High "Attribute deleted on managed upgrade" | Automated | Pass |
| TC-ALM07-DC-05 | Managed target, workflow removed | workflow in target not in source | Medium "deleted on managed upgrade" | Automated | Pass |
| TC-ALM07-DC-06 | Component in both sides | same objectid in source & target | No finding | Automated | Pass |
| TC-ALM07-DC-07 | Unmanaged target, removal | removed component, target unmanaged | Info "unmanaged", no Critical | Automated | Pass |
| TC-ALM07-FM-01 | Form → missing web resource | form library not in source | High "Form references missing web resource" | Automated | Pass |
| TC-ALM07-FM-02 | Form → known web resource | form library present in source | No finding | Automated | Pass |
| TC-ALM07-FM-03 | Form handler libraryName missing | event handler library absent | High "Form references missing web resource" | Automated | Pass |
| TC-ALM07-FM-04 | No forms in solution | empty systemform set | Info "No forms in solution" | Automated | Pass |
| TC-ALM07-RB-01 | Ribbon → missing web resource | `$webresource:` command lib not in source | High "Ribbon command references missing web resource" | Automated | Pass |
| TC-ALM07-RB-02 | Ribbon → known web resource | command lib present in source | No finding | Automated | Pass |
| TC-ALM07-RB-03 | No ribbon customizations | empty ribboncustomization set | Info "No ribbon customizations in solution" | Automated | Pass |
| TC-ALM07-RPT-01 | PDF render | export result to PDF | Valid `%PDF-` file > 1 KB (MigraDoc/PdfSharp GDI); dashboard layout (radial gauge overlay, risk categories, recommendations, next steps) | Automated | Pass |
| TC-ALM07-RPT-02 | HTML render | export result to HTML | Self-contained dashboard doc containing the findings | Automated | Pass |
| TC-ALM07-HTML-01 | HTML self-contained | `Build()` a result | Has `<title>`/`<style>`, no external/CDN refs | Automated | Pass |
| TC-ALM07-HTML-02 | HTML theme-aware | `Build()` a result | Contains `prefers-color-scheme:dark` + `[data-theme="dark"]`/`"light"` hooks | Automated | Pass |
| TC-ALM07-HTML-03 | HTML gauge | `Build()` score 78 / High | Renders `78`, `HIGH RISK`, arc `stroke-dasharray="78 100"` | Automated | Pass |
| TC-ALM07-HTML-04 | HTML KPIs & categories | `Build()` a result | Severity labels + friendly category names (e.g. "Environment Variables") | Automated | Pass |
| TC-ALM07-HTML-05 | HTML findings/recs | `Build()` with findings | Finding title, recommendation, and HelpUrl surface | Automated | Pass |
| TC-ALM07-HTML-06 | HTML encoding | finding title `Bad <script>` | Encoded to `&lt;script&gt;`; no raw `<script>` | Automated | Pass |
| TC-ALM07-HTML-07 | HTML exec summary | result with `AiSummary` | "Executive Summary" heading + summary text present | Automated | Pass |
| TC-ALM07-HTML-08 | HTML clear state | no findings, Low risk | Shows `LOW RISK` + "Clear for deployment" (not blank) | Automated | Pass |
| TC-ALM07-RPT-03 | JSON CI payload | export result to JSON at High gate | `score`, `ci.pass=false`, `suggestedExitCode=1` | Automated | Pass |
| TC-ALM07-RPT-04 | Markdown checklist | export result to Markdown | Contains "Fix Checklist" + a "## Guidance" section with rollback steps | Automated | Pass |
| TC-ALM07-RPT-05 | Excel package | export result to XLSX | Valid ZIP/OOXML (`PK` header) > 1 KB | Automated | Pass |
| TC-ALM07-RPT-06 | Summary embedded in exports | result with `AiSummary` → PDF/HTML/JSON | PDF renders; HTML has "Executive Summary"; JSON `aiSummary` set | Automated | Pass |

## Automated - Deployment summary (US-ALM07-8)

Executed via `dotnet test testing/UnitTests/UnitTests.csproj` — SDK-free payload builder + offline
template. The live Anthropic HTTP path (`ClaudeSummaryGenerator`) needs a key + network and is manual.

| ID | Case | Input | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ALM07-SUM-01 | Payload maps score/risk + target flag | result with / without target | score & risk mapped; `hasTarget` reflects target presence | Automated | Pass |
| TC-ALM07-SUM-02 | Component redaction (Mode C) | includeComponents false vs true | component null when redacted; present when enabled | Automated | Pass |
| TC-ALM07-SUM-03 | Top-N truncation + ordering | 4 findings, topN=2 | 2 kept, `truncated=true`, severity-descending order | Automated | Pass |
| TC-ALM07-SUM-04 | Offline verdict per band | High / Medium / Low | NO-GO / GO WITH CAUTION / GO; not AI; score shown | Automated | Pass |
| TC-ALM07-SUM-05 | Offline top-risks list | Critical + Low findings | lists Medium+ risks; omits Low | Automated | Pass |

## Manual - Connections (US-ALM07-1)

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ALM07-M-01 | Load solutions | Open tool, connect to source, click Load solutions | Solutions list populates off-thread with progress; managed + unmanaged selectable | Manual | Pending |
| TC-ALM07-M-02 | Connect target env | Click "Connect target env...", pick a second connection | Target connects without dropping the source; target-only analyzers become enabled | Manual | Pending |
| TC-ALM07-M-03 | No target | Do not connect a target | Schema/version/target-config analyzers show as skipped/disabled | Manual | Pending |

## Manual - Analyzers (US-ALM07-2..7)

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ALM07-M-04 | Missing dependencies | Analyze a solution with a known missing component | Dependency finding lists the missing component / prerequisite solution | Manual | Pending |
| TC-ALM07-M-05 | Env var without value | Solution with an env variable lacking default/current value | Finding flags the unset variable; secrets flagged as per-env config | Manual | Pending |
| TC-ALM07-M-06 | Draft flow | Solution containing an OFF cloud flow | Finding flags the draft flow; missing connection references detected from clientdata | Manual | Pending |
| TC-ALM07-M-07 | Security coverage | New custom table with no role privileges | Finding flags the table with no role coverage | Manual | Pending |
| TC-ALM07-M-08 | Schema conflict (target) | Target has an attribute of a different type | Import-breaking type-mismatch finding (Critical/High) | Manual | Pending |
| TC-ALM07-M-09 | Power Pages readiness | Solution with a table on a form but no table permission | Finding flags the table surfaced without permission (adx_ and mspp_) | Manual | Pending |
| TC-ALM07-M-10 | Analyzer resilience | Run with a user lacking a privilege one analyzer needs | That analyzer degrades to an informational finding; run completes (US-ALM07-10.1) | Manual | Pending |

## Manual - Exports (US-ALM07-9)

| ID | Case | Steps | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-ALM07-M-15 | PDF export (GUI) | Export → "PDF report (executive)", choose a path | SaveFileDialog defaults to `.pdf`; a native PDF is written and opens; page 1 shows the radial score gauge (top-right), risk-categories/recommendations/next-steps sections; PdfSharp/MigraDoc chain resolves (tool not dropped from list) | Manual | Pending |
| TC-ALM07-M-11 | HTML export | Export findings as HTML | Dashboard report opens (gauge, KPI cards, categories, recommendations); follows browser light/dark; Print → Save as PDF is pixel-identical | Manual | Pending |
| TC-ALM07-M-12 | Excel export | Export as Excel | Workbook with Summary / Findings / Fix Checklist sheets; no crash (ClosedXML chain present) | Manual | Pending |
| TC-ALM07-M-13 | JSON CI gating | Export JSON; inspect `ci.pass` + `suggestedExitCode` | High-risk result -> `ci.pass=false` and non-zero suggested exit code | Manual | Pending |
| TC-ALM07-M-14 | Markdown checklist | Export Markdown | Fix checklist ends with rollback guidance | Manual | Pending |
| TC-ALM07-M-16 | AI summary (GUI) | After analysis, click "AI summary" (first run) | Consent dialog shows the exact JSON + endpoint + model; Send → AI summary (needs key); Cancel or no key → offline summary; "Auto" auto-runs after analysis; summary appears in PDF/HTML/JSON exports | Manual | Pending |
| TC-ALM07-M-17 | Help & Support dialog | Click "Help" (right of toolbar) | Dialog opens with Documentation + Report-an-issue links and a Buy-me-a-coffee button; "Open documentation" → repo README, "Report an issue on GitHub" → issues/new, "Buy me a coffee" → buymeacoffee.com/kkora, each in the default browser | Manual | Pending |

> Capture a screenshot for each executed manual case into `screenshots/` (e.g. `TC-ALM07-M-11-html.png`).
