# Solution Modernization Advisor — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 5 (Solution Management), item 6. Not in pack file.
> **Suggested tag:** `SOLN6` · **Suggested project:** `XrmToolSuite.SolutionModernizationAdvisor`
> **Overlaps:** Overlaps the shipped **Technical Debt Analyzer** (deprecated Xrm.Page JS, old plugin patterns, unused/duplicate components, unmanaged customizations) — reuse its detection analyzers as the modernization rule base. Overlaps the **AI Architecture Reviewer** candidate and the shipped **AI Solution Reviewer** for the recommendation/roadmap narrative. This tool's distinct value is the *modernization roadmap*: effort-vs-value prioritization into quick wins / medium / strategic, not just debt detection.
> **Value/priority (my read):** Medium — strong architect value, but detection heavily overlaps Technical Debt Analyzer; the incremental scope is the roadmap/prioritization layer, so consider building it on top of that analyzer set.

## Notes
- Core data: `solution`, `solutioncomponent`, `msdyn_solutioncomponentsummary`, `publisher`; `workflow` (classic vs modern, business rules); `webresource` (JavaScript body scan for deprecated `Xrm.Page` / sync XHR / blocking patterns); `pluginassembly`/`plugintype`/`sdkmessageprocessingstep` (old patterns, plugins better as custom APIs); `customapi`; `environmentvariabledefinition`/`connectionreference` (missing env vars, hardcoded URLs/GUIDs); FormXML (field/subgrid/script counts, old UI patterns); security roles; audit/governance settings where readable.
- Reuse Technical Debt Analyzer detection analyzers as the rule base; add modernization-specific rules (classic→Power Automate candidates, plugin→custom-API candidates, hardcoded env-specific values) on top. Keep every rule as an `IAnalyzer`-style UI-free unit.
- Read-only — the tool detects and recommends; it never refactors or changes the solution.
- Retrieval off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress and honor cancellation; cache per connection and clear on `UpdateConnection`.
- Keep the rules engine, modernization scoring, and effort/value classification UI-free so scoring/banding is unit-testable in `testing/UnitTests/`; degrade unreadable sources (e.g. flows lacking definition access) to Info, not a crash.
- Roadmap prioritization (quick win / medium effort / strategic) is the differentiator — model effort and value as explicit inputs, not a single severity.

---

## EPIC-SOLN6 — Turn legacy customizations into a prioritized modernization roadmap
> **As** an **ARCH**, **I want** modernization opportunities across a solution detected, scored, and grouped by effort and value, **so that** I can plan a practical upgrade backlog instead of guessing what to modernize first.

**Outcome:** a modernization score, a legacy-component inventory, an effort-vs-value matrix, recommendations grouped into quick wins / medium effort / strategic initiatives, a migration/refactoring backlog, and exportable reports.

---

## FEAT-SOLN6-1 — Scope and inventory legacy components `[Planned]`
- **US-SOLN6.1.1** `[Planned]` **As** an ARCH, **I want** to scan a selected solution or the whole environment for legacy components, **so that** I can scope the modernization review.
  - **AC:** Scan runs off the UI thread with progress/cancel; a legacy-component inventory lists each item with its modernization category.
- **US-SOLN6.1.2** `[Planned]` **As** an ARCH, **I want** the inventory reuse Technical Debt Analyzer detections, **so that** results stay consistent across the suite.
  - **AC:** Shared detection analyzers feed the inventory; modernization-specific rules layer on top without duplicating debt detection.

## FEAT-SOLN6-2 — Automation and plugin modernization detection `[Planned]`
- **US-SOLN6.2.1** `[Planned]` **As** a DEVOPS engineer, **I want** classic workflows that could move to Power Automate or plugins/custom APIs flagged, **so that** I can plan automation modernization.
  - **AC:** Classic `workflow` records are detected and tagged with a suggested target (Power Automate / plugin / custom API); category = Automation Modernization.
- **US-SOLN6.2.2** `[Planned]` **As** a CUST developer, **I want** old plugin patterns and plugins better suited as custom APIs flagged, **so that** I can modernize the plugin layer.
  - **AC:** Plugin step/message patterns are analyzed; candidates for custom-API conversion are listed with rationale; category = Plugin Modernization.

## FEAT-SOLN6-3 — JavaScript, form, and config modernization detection `[Planned]`
- **US-SOLN6.3.1** `[Planned]` **As** a CUST developer, **I want** deprecated `Xrm.Page` usage and synchronous/blocking scripts detected, **so that** I can modernize client scripting.
  - **AC:** Web-resource JavaScript is scanned for deprecated APIs and sync/blocking calls; findings name the web resource and category = JavaScript Modernization.
- **US-SOLN6.3.2** `[Planned]` **As** an ARCH, **I want** hardcoded URLs/GUIDs/env-specific values, missing environment variables, and overloaded forms detected, **so that** I can modernize configuration and UI.
  - **AC:** Hardcoded env-specific values and missing env vars (Configuration Modernization), and forms with excessive fields/subgrids/scripts (UI/Form Modernization) are distinct categories.
- **US-SOLN6.3.3** `[Planned]` **As** a SEC reviewer, **I want** unmanaged-should-be-managed components, duplicate tables/columns/views/forms, security-role complexity, and audit/governance gaps flagged, **so that** ALM, security, and governance modernization are covered.
  - **AC:** ALM Modernization, Security Modernization, and Governance Modernization categories are populated; duplicates and missing descriptions/documentation are included.

## FEAT-SOLN6-4 — Modernization score and roadmap `[Planned]`
- **US-SOLN6.4.1** `[Planned]` **As** an ARCH, **I want** a modernization score, **so that** I can track a solution's modernization posture over time.
  - **AC:** Score aggregates category findings with configurable weights that round-trip via Load/SaveSettings; displayed on a dashboard.
- **US-SOLN6.4.2** `[Planned]` **As** an ARCH, **I want** recommendations grouped into quick wins, medium effort, and strategic initiatives on an effort-vs-value matrix, **so that** I can prioritize the roadmap.
  - **AC:** Each recommendation carries an effort and a value rating and lands in a quadrant/bucket; the matrix is viewable and drillable.

## FEAT-SOLN6-5 — Backlog and export `[Planned]`
- **US-SOLN6.5.1** `[Planned]` **As** a Delivery Manager, **I want** a migration/refactoring backlog and the modernization report exported to Excel, PDF, JSON, and HTML, **so that** I can drive the modernization program.
  - **AC:** Backlog items describe the change, category, effort, and value; HTML is self-contained and theme-aware; JSON carries the score and machine-readable recommendations; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); reuses Technical Debt Analyzer detection analyzers instead of duplicating them.
- Read-only default (no refactor/write); rules engine, scoring, and effort/value classification stay UI-free and unit-tested in testing/UnitTests/; unreadable sources degrade to Info.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/SolutionModernizationAdvisor/ when implementation starts.
