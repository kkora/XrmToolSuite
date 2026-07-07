# Backlog Recommendations — what to build first, and in what order

My read on the 92 candidates in this folder: **highest value**, **implement-first sequencing**,
**shared building blocks (dependencies)**, and **consolidation** where a candidate overlaps a tool
you have already shipped. This is advice only — nothing is built or committed.

> Context: you have **6 tools already shipped** — Deployment Risk Analyzer (DRA), Technical Debt
> Analyzer (TDA), Solution Complexity Score, AI Solution Reviewer, Solution Knowledge Graph,
> Attribute Auditor. Several prompt-pack ideas overlap these; the win is to **extend/reuse**, not
> re-implement. Overlaps are flagged in each candidate's header.

---

## 1. The shortlist — highest value, build these first

Ranked by (value ÷ effort), SDK feasibility, and how much they unlock other tools.

| # | Candidate | Why it's top | Rough effort |
|---|---|---|---|
| 1 | **PERF03 FetchXML Performance Analyzer** | FetchXML underpins views, dashboards, reports, plugins, flows, portals. Its parser/rule engine becomes a **shared `src/Shared/Core/` analyzer** reused by PERF04, PERF05, PP04. Pure static, fully SDK-feasible, unit-testable. Build this **first** as a building block. | M |
| 2 | **PA01 Flow Dependency Analyzer** | Top pre-deploy question ("what breaks if I change this table/connector?"). Fully static parse of `clientdata`; **reuses DRA's existing clientdata parser**. Also pack #4. | M |
| 3 | **SOLN01 Component Usage Explorer** | The universal "where is this used before I change/delete it" tool. Reuses the shipped Solution Knowledge Graph model; adds single-component drill-down + change-safety verdict. | M |
| 4 | **SEC01 Privilege Gap Analyzer** | Answers the daily "why can't this user do X?" support question the product can't. Anchors a shared **effective-privilege engine** reused by SEC02/SEC06/SEC10. Also pack #2. | M-H |
| 5 | **PERF08 JavaScript Performance Analyzer** | JS directly blocks form load, is highly fixable, static analysis works offline/CI. Also pack #9. | M |
| 6 | **ADMIN07 Environment Inventory** | Constant admin/audit ask **and** the data backbone the whole DOC + RPT tracks consume. Build early so documentation/reporting tools have a source model. | M-H |
| 7 | **PP01 Portal Health Analyzer** | Broad admin appeal, one-click; seeds the shared Power Pages data layer (adx_/mspp_) that PP02–PP07 reuse. | M-H |
| 8 | **RPT04 Technical Debt Trends** | Best ROI in Reporting: the scanning already ships (TDA). Incremental build is just a local snapshot store + diff. Consider shipping it as a **"Trends" tab on TDA** rather than a new tool. | S-M |
| 9 | **PERF10 Form Performance Analyzer** (pack-sourced) | Fills a real gap — the Performance track covers views/dashboards but **not** model-driven forms. Static from FormXML; clean unit-test target. | M |
| 10 | **SEC05 Audit Compliance Checker** | Proving audit coverage is a hard compliance requirement; gaps stay invisible until an incident. Also pack #18. | M-H |

---

## 2. Build-order / wave plan

Sequenced so shared building blocks land before their consumers.

**Wave 0 — shared building blocks (do first, they unlock the rest)**
- **PERF03 FetchXML analyzer** → consumed by PERF04, PERF05, PP04.
- **Effective-privilege engine** (ships inside **SEC01**) → consumed by SEC02, SEC06, SEC10, and the SEC09/RPT03 scorecards.
- **`clientdata` flow parser** — already exists in DRA; lift into shared core for PA01/PA02/PA04/PA07/PA05.
- **ADMIN07 Environment Inventory** — the shared metadata model behind DOC01–6, RPT01, RPT08.
- **Power Pages retrieval layer** (ships inside **PP01**) → consumed by PP02–PP07.
- **Cross-environment comparison engine** (ships inside **MIG01**) → shared with ADMIN08.

**Wave 1 — high-value standalone tools (mostly static, SDK-safe)**
PA01, SOLN01, SEC01, PERF08, PERF10, PLUGIN01, PP02, PP03, ALM04, SEC04.

**Wave 2 — consumers of Wave-0 blocks**
PERF04, PERF05, PP04, SEC02, SEC06, SEC10, PA02, PA04, PA07, ADMIN02, ADMIN06, SEC05, SEC07, DOC02, DOC03.

**Wave 3 — aggregators, dashboards, trends (need several analyzers to exist first)**
ADMIN01, RPT01, RPT02, RPT04, RPT08, SEC09/RPT03 (pick one — see consolidation), MIG01, DOC04/5/6.

**Wave 4 — AI layers (build after their deterministic tracks exist)**
AI01, AI03, AI04, AI06, AI07, AI09, AI02, AI05, AI08 — each reuses AI Solution Reviewer plumbing and sits on a completed deterministic track.

---

## 3. Shared building blocks / dependencies (build once, reuse)

| Building block | First delivered in | Reused by |
|---|---|---|
| FetchXML parser + performance rules | PERF03 | PERF04, PERF05, PP04 |
| Effective-privilege calculator | SEC01 | SEC02, SEC06, SEC10, SEC09, RPT03 |
| Flow `clientdata` parser | DRA (exists) → shared core | PA01, PA02, PA04, PA05, PA07 |
| Extracted metadata/inventory model | ADMIN07 | DOC01–DOC06, RPT01, RPT08, SOLN05 |
| Power Pages retrieval (adx_/mspp_) | PP01 | PP02, PP03, PP04, PP05, PP06, PP07 |
| Cross-environment comparison engine | MIG01 | ADMIN08 (Config Drift Monitor) |
| Dependency/graph model + rendering | Solution Knowledge Graph (exists) | SOLN01, SOLN02, PLUGIN01, DOC01, PA07, MIG07 |
| Plugin metadata retrieval | PLUGIN01 | PLUGIN02, PLUGIN03, PLUGIN04, PLUGIN05, PLUGIN06 (Custom API Explorer) |
| AI client (opt-in/redaction/consent/fallback) | AI Solution Reviewer (exists) | all AI01–AI09 |
| Export chains (ClosedXML/OpenXml, MigraDoc/PdfSharp-GDI) | DRA (exists) | DOC03/4/5, SOLN05, all report exports |

---

## 4. Consolidate — don't ship two tools that do the same thing

These candidates overlap each other or a **shipped** tool. Recommendation: merge or make one canonical.

- **SOLN04 Solution Quality Score ⟶ extend shipped Solution Complexity Score** (don't build a second scorer). Add governance/ALM/documentation/security categories to the existing tool.
- **RPT04 Technical Debt Trends ⟶ a "Trends" tab on shipped TDA** (snapshot + diff layer, not a new tool).
- **SEC09 Environment Governance Score vs RPT03 Governance Scorecard** — same scorecard from two tracks. **Pick one canonical governance scorecard.**
- **ADMIN01 Environment Health Dashboard vs RPT01 Executive Dashboard** — both are top-level roll-ups. Make one the aggregator; the other becomes a view/export of it.
- **ADMIN08 Configuration Drift Monitor vs MIG01 Environment Comparison Suite** — build the **comparison engine once**, expose two entry points (baseline-vs-now, env-vs-env).
- **MIG04 API Integration Explorer vs MIG07 Integration Dependency Map** — same discovery engine, grid lens vs graph lens.
- **ALM06 Connection Reference Validator vs PA05 Connection Usage Analyzer** — heavy overlap and both overlap DRA. Consolidate into one connection-reference tool.
- **SOLN05 Solution Documentation Generator vs the DOC track (DOC03/4/5)** — SOLN05 is the engine; DOC03/4/5 are output formats. Build SOLN05's extraction once, add format renderers.
- **DOC02 ERD Generator / DOC01 Architecture Diagram / SOLN02 Dependency Heatmap** — all lean on the shipped Knowledge Graph model; build as views/exporters over it.
- **PLUGIN02 Registration Auditor / PLUGIN03 Recursion Detector** overlap DRA's plugin checks — position them as **standing governance audits**, not deploy gates (DRA already gates deploys).
- **ADMIN02 Metadata Cleanup / ADMIN03 Duplicate Metadata** overlap shipped **Attribute Auditor** + TDA — extend those rather than start fresh.
- **PP01 Portal Health** overlaps DRA's Power Pages readiness checks — reuse them.

---

## 5. Feasibility caveats to validate before committing (SDK reality)

Several prompts assume data the Dataverse SDK doesn't cleanly expose. Flag these as risk before scheduling:

- **Flow run history / metrics** (PA03 Failure Investigator, PA06 Performance Dashboard) — run history lives in the Power Automate/Logic Apps runtime, **not** `IOrganizationService`. Static half is feasible now; live triage needs separate auth. Degrade to "not available", don't show blanks.
- **Plugin trace logs** (PLUGIN05 Exception Analyzer, parts of PERF01/PLUGIN03) — depend on `plugintracelog` being **enabled and retained**; often sparse. Handle empty state honestly.
- **Storage bytes / billing** (PERF09, ADMIN05, ADMIN04) — no per-table billed-bytes API; everything is **estimated**. Keep the estimation disclaimers.
- **Row counts** (ADMIN04 Growth Forecast) — `RetrieveTotalRecordCount` is approximate and needs **local snapshot history** to trend; low value on day one.
- **Licensing SKUs** (SEC08) — Dataverse can't see true license assignments; **estimates only**, label clearly.
- **Solution import history** (ALM03, RPT05) — limited native retention; several features are "where available" / inferred.
- **Change attribution "who changed it"** (RPT07) — best-effort from `modifiedby`/audit; not guaranteed.
- **Accessibility** (PP07) — static metadata analysis only; **cannot certify WCAG** without runtime testing (disclaimer already in the file).
- **Custom API test console** (PLUGIN06) — the invoke path is a **write/execute** surface: gate with a confirmation dialog stating scope, never expose secrets.

---

## 6. Quick-win cluster (small, static, high-confidence, CI-friendly)

If you want fast, low-risk wins that unit-test cleanly and need no live-org trickery:
**PERF08 (JavaScript)**, **PERF03 (FetchXML)**, **PERF10 (Form)**, **PA02 (Flow Complexity)**,
**DOC02 (ERD → Mermaid)**, **DOC03 (Markdown docs)**. All are pure static analysis / rendering over
metadata, so their cores are UI-free and land in `testing/UnitTests/` the way the shipped analyzers do.

---

*No tools were created and nothing was committed. Review, then tell me which candidates to promote
into real tools (I'd start each with `New-Tool.ps1`, which stamps the `testing/<Tool>/` + user-story
skeleton per the repo's tool lifecycle).*
