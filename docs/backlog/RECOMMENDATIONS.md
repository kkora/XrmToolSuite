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
| 1 | **PERF3 FetchXML Performance Analyzer** | FetchXML underpins views, dashboards, reports, plugins, flows, portals. Its parser/rule engine becomes a **shared `src/Shared/Core/` analyzer** reused by PERF4, PERF5, PP4. Pure static, fully SDK-feasible, unit-testable. Build this **first** as a building block. | M |
| 2 | **PA1 Flow Dependency Analyzer** | Top pre-deploy question ("what breaks if I change this table/connector?"). Fully static parse of `clientdata`; **reuses DRA's existing clientdata parser**. Also pack #4. | M |
| 3 | **SOLN1 Component Usage Explorer** | The universal "where is this used before I change/delete it" tool. Reuses the shipped Solution Knowledge Graph model; adds single-component drill-down + change-safety verdict. | M |
| 4 | **SEC1 Privilege Gap Analyzer** | Answers the daily "why can't this user do X?" support question the product can't. Anchors a shared **effective-privilege engine** reused by SEC2/SEC6/SEC10. Also pack #2. | M-H |
| 5 | **PERF8 JavaScript Performance Analyzer** | JS directly blocks form load, is highly fixable, static analysis works offline/CI. Also pack #9. | M |
| 6 | **ADMIN7 Environment Inventory** | Constant admin/audit ask **and** the data backbone the whole DOC + RPT tracks consume. Build early so documentation/reporting tools have a source model. | M-H |
| 7 | **PP1 Portal Health Analyzer** | Broad admin appeal, one-click; seeds the shared Power Pages data layer (adx_/mspp_) that PP2–PP7 reuse. | M-H |
| 8 | **RPT4 Technical Debt Trends** | Best ROI in Reporting: the scanning already ships (TDA). Incremental build is just a local snapshot store + diff. Consider shipping it as a **"Trends" tab on TDA** rather than a new tool. | S-M |
| 9 | **PERF10 Form Performance Analyzer** (pack-sourced) | Fills a real gap — the Performance track covers views/dashboards but **not** model-driven forms. Static from FormXML; clean unit-test target. | M |
| 10 | **SEC5 Audit Compliance Checker** | Proving audit coverage is a hard compliance requirement; gaps stay invisible until an incident. Also pack #18. | M-H |

---

## 2. Build-order / wave plan

Sequenced so shared building blocks land before their consumers.

**Wave 0 — shared building blocks (do first, they unlock the rest)**
- **PERF3 FetchXML analyzer** → consumed by PERF4, PERF5, PP4.
- **Effective-privilege engine** (ships inside **SEC1**) → consumed by SEC2, SEC6, SEC10, and the SEC9/RPT3 scorecards.
- **`clientdata` flow parser** — already exists in DRA; lift into shared core for PA1/PA2/PA4/PA7/PA5.
- **ADMIN7 Environment Inventory** — the shared metadata model behind DOC1–6, RPT1, RPT8.
- **Power Pages retrieval layer** (ships inside **PP1**) → consumed by PP2–PP7.
- **Cross-environment comparison engine** (ships inside **MIG1**) → shared with ADMIN8.

**Wave 1 — high-value standalone tools (mostly static, SDK-safe)**
PA1, SOLN1, SEC1, PERF8, PERF10, PLUGIN1, PP2, PP3, ALM4, SEC4.

**Wave 2 — consumers of Wave-0 blocks**
PERF4, PERF5, PP4, SEC2, SEC6, SEC10, PA2, PA4, PA7, ADMIN2, ADMIN6, SEC5, SEC7, DOC2, DOC3.

**Wave 3 — aggregators, dashboards, trends (need several analyzers to exist first)**
ADMIN1, RPT1, RPT2, RPT4, RPT8, SEC9/RPT3 (pick one — see consolidation), MIG1, DOC4/5/6.

**Wave 4 — AI layers (build after their deterministic tracks exist)**
AI1, AI3, AI4, AI6, AI7, AI9, AI2, AI5, AI8 — each reuses AI Solution Reviewer plumbing and sits on a completed deterministic track.

---

## 3. Shared building blocks / dependencies (build once, reuse)

| Building block | First delivered in | Reused by |
|---|---|---|
| FetchXML parser + performance rules | PERF3 | PERF4, PERF5, PP4 |
| Effective-privilege calculator | SEC1 | SEC2, SEC6, SEC10, SEC9, RPT3 |
| Flow `clientdata` parser | DRA (exists) → shared core | PA1, PA2, PA4, PA5, PA7 |
| Extracted metadata/inventory model | ADMIN7 | DOC1–DOC6, RPT1, RPT8, SOLN5 |
| Power Pages retrieval (adx_/mspp_) | PP1 | PP2, PP3, PP4, PP5, PP6, PP7 |
| Cross-environment comparison engine | MIG1 | ADMIN8 (Config Drift Monitor) |
| Dependency/graph model + rendering | Solution Knowledge Graph (exists) | SOLN1, SOLN2, PLUGIN1, DOC1, PA7, MIG7 |
| Plugin metadata retrieval | PLUGIN1 | PLUGIN2, PLUGIN3, PLUGIN4, PLUGIN5, PLUGIN6 (Custom API Explorer) |
| AI client (opt-in/redaction/consent/fallback) | AI Solution Reviewer (exists) | all AI1–AI9 |
| Export chains (ClosedXML/OpenXml, MigraDoc/PdfSharp-GDI) | DRA (exists) | DOC3/4/5, SOLN5, all report exports |

---

## 4. Consolidate — don't ship two tools that do the same thing

These candidates overlap each other or a **shipped** tool. Recommendation: merge or make one canonical.

- **SOLN4 Solution Quality Score ⟶ extend shipped Solution Complexity Score** (don't build a second scorer). Add governance/ALM/documentation/security categories to the existing tool.
- **RPT4 Technical Debt Trends ⟶ a "Trends" tab on shipped TDA** (snapshot + diff layer, not a new tool).
- **SEC9 Environment Governance Score vs RPT3 Governance Scorecard** — same scorecard from two tracks. **Pick one canonical governance scorecard.**
- **ADMIN1 Environment Health Dashboard vs RPT1 Executive Dashboard** — both are top-level roll-ups. Make one the aggregator; the other becomes a view/export of it.
- **ADMIN8 Configuration Drift Monitor vs MIG1 Environment Comparison Suite** — build the **comparison engine once**, expose two entry points (baseline-vs-now, env-vs-env).
- **MIG4 API Integration Explorer vs MIG7 Integration Dependency Map** — same discovery engine, grid lens vs graph lens.
- **ALM6 Connection Reference Validator vs PA5 Connection Usage Analyzer** — heavy overlap and both overlap DRA. Consolidate into one connection-reference tool.
- **SOLN5 Solution Documentation Generator vs the DOC track (DOC3/4/5)** — SOLN5 is the engine; DOC3/4/5 are output formats. Build SOLN5's extraction once, add format renderers.
- **DOC2 ERD Generator / DOC1 Architecture Diagram / SOLN2 Dependency Heatmap** — all lean on the shipped Knowledge Graph model; build as views/exporters over it.
- **PLUGIN2 Registration Auditor / PLUGIN3 Recursion Detector** overlap DRA's plugin checks — position them as **standing governance audits**, not deploy gates (DRA already gates deploys).
- **ADMIN2 Metadata Cleanup / ADMIN3 Duplicate Metadata** overlap shipped **Attribute Auditor** + TDA — extend those rather than start fresh.
- **PP1 Portal Health** overlaps DRA's Power Pages readiness checks — reuse them.

---

## 5. Feasibility caveats to validate before committing (SDK reality)

Several prompts assume data the Dataverse SDK doesn't cleanly expose. Flag these as risk before scheduling:

- **Flow run history / metrics** (PA3 Failure Investigator, PA6 Performance Dashboard) — run history lives in the Power Automate/Logic Apps runtime, **not** `IOrganizationService`. Static half is feasible now; live triage needs separate auth. Degrade to "not available", don't show blanks.
- **Plugin trace logs** (PLUGIN5 Exception Analyzer, parts of PERF1/PLUGIN3) — depend on `plugintracelog` being **enabled and retained**; often sparse. Handle empty state honestly.
- **Storage bytes / billing** (PERF9, ADMIN5, ADMIN4) — no per-table billed-bytes API; everything is **estimated**. Keep the estimation disclaimers.
- **Row counts** (ADMIN4 Growth Forecast) — `RetrieveTotalRecordCount` is approximate and needs **local snapshot history** to trend; low value on day one.
- **Licensing SKUs** (SEC8) — Dataverse can't see true license assignments; **estimates only**, label clearly.
- **Solution import history** (ALM3, RPT5) — limited native retention; several features are "where available" / inferred.
- **Change attribution "who changed it"** (RPT7) — best-effort from `modifiedby`/audit; not guaranteed.
- **Accessibility** (PP7) — static metadata analysis only; **cannot certify WCAG** without runtime testing (disclaimer already in the file).
- **Custom API test console** (PLUGIN6) — the invoke path is a **write/execute** surface: gate with a confirmation dialog stating scope, never expose secrets.

---

## 6. Quick-win cluster (small, static, high-confidence, CI-friendly)

If you want fast, low-risk wins that unit-test cleanly and need no live-org trickery:
**PERF8 (JavaScript)**, **PERF3 (FetchXML)**, **PERF10 (Form)**, **PA2 (Flow Complexity)**,
**DOC2 (ERD → Mermaid)**, **DOC3 (Markdown docs)**. All are pure static analysis / rendering over
metadata, so their cores are UI-free and land in `testing/UnitTests/` the way the shipped analyzers do.

---

*No tools were created and nothing was committed. Review, then tell me which candidates to promote
into real tools (I'd start each with `New-Tool.ps1`, which stamps the `testing/<Tool>/` + user-story
skeleton per the repo's tool lifecycle).*
