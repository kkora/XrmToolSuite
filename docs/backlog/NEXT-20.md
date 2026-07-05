# Next 20 tools to implement

Ranked build plan picked from the [backlog](README.md), ordered so **shared engines land before the
tools that consume them**, weighted toward high value + SDK-feasibility + the strongest prompt-pack
ideas. See [RECOMMENDATIONS.md](RECOMMENDATIONS.md) for the full reasoning and
[README.md](README.md) for the per-tool user stories.

> **Build status (2026-07-05): 20 of 20 implemented. ✅ Complete.** Every tool in Phases A, B, and C is built —
> shipped as a project under `src/Tools/`, present in `XrmToolSuite.sln`, UI-free engine + WinForms host,
> with user stories under `docs/user-stories/<TAG>.<Tool>.md`, SDK-free unit tests in `testing/UnitTests`,
> and testing artifacts under `testing/<Tool>/`. The whole solution builds Release with zero warnings and
> `dotnet test` is green. Live-connection and WinForms GUI cases (including Office/PDF export dialogs and the
> Custom API Explorer's gated invoke console) remain pending a Windows + XrmToolBox session across all tools —
> documented, not claimed as passed.

Legend: **H** High · **M** Medium · Pack# = matching idea in `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt`.
Status: ✅ built · ⬜ not started.

## Phase A — foundational engines (build first; everything else reuses them)

| # | Tool | Status | Backlog file | Value | Pack# | Why first |
|---|---|---|---|---|---|---|
| 1 | FetchXML Performance Analyzer | ✅ | [PERF03](03-Performance/PERF03.FetchXmlPerformanceAnalyzer.md) | H | — | Shared FetchXML parser/rules engine reused by #16 View + Dashboard + Portal Perf. Pure static. |
| 2 | Environment Inventory | ✅ | [ADMIN07](04-Dataverse-Administration/ADMIN07.EnvironmentInventory.md) | H | — | The normalized metadata model that feeds ERD / Docs / Reporting / Drift tools. |
| 3 | Privilege Gap Analyzer | ✅ | [SEC01](02-Security-Governance/SEC01.PrivilegeGapAnalyzer.md) | H | #2 | Ships the effective-privilege engine reused by #17 Team Explorer, Matrix, Heatmap. |

> **Status:** ✅ **implemented.** All three tools are built (Release, 0 warnings) with UI-free engines,
> WinForms hosts, SDK-free unit tests (10 FetchXML + 16 Environment Inventory + 10 Privilege Gap, all
> passing in `testing/UnitTests`), active user stories under `docs/user-stories/`, and testing artifacts
> under `testing/<Tool>/`. The shared FetchXML parser/rule engine lives in `src/Shared/Core/FetchXml/`;
> the Environment Inventory normalization model and the Privilege Gap effective-privilege engine are
> UI-free and console/CI-liftable. Full export is implemented per the backlog: **Excel + PDF** (and
> **Word** for Environment Inventory) via the shared `ReportModel` exporters, plus JSON/HTML/Markdown/CSV —
> each tool ships the ClosedXML + PdfSharp/MigraDoc-GDI dependency chains the same way the Deployment Risk
> Analyzer does (the pattern is now documented in CLAUDE.md as reusable, not a one-off). Live-connection and
> WinForms GUI cases (including the Office/PDF export dialogs) remain pending a Windows + XrmToolBox session
> (documented, not claimed as passed).

## Phase B — flagship standalone tools (high value, mostly static, cover the best pack ideas)

> **Status:** ✅ all 12 (#4–#15) implemented — projects under `src/Tools/`, in the sln, unit-tested where
> SDK-free logic exists; live/GUI cases pending a Windows + XrmToolBox session.

| # | Tool | Status | Backlog file | Value | Pack# | Note |
|---|---|---|---|---|---|---|
| 4 | Flow Dependency Analyzer | ✅ | [PA01](07-Power-Automate/PA01.FlowDependencyAnalyzer.md) | H | #4 | Reuses DRA's `clientdata` parser. |
| 5 | Component Usage Explorer | ✅ | [SOLN01](05-Solution-Management/SOLN01.ComponentUsageExplorer.md) | H | — | "Where is this used before I change it"; reuses Knowledge Graph. |
| 6 | Plugin Dependency Graph | ✅ | [PLUGIN01](08-Plugins-Custom-APIs/PLUGIN01.PluginDependencyGraph.md) | H | #10 | Ships shared plugin-metadata retrieval for the PLUGIN track. |
| 7 | JavaScript Performance Analyzer | ✅ | [PERF08](03-Performance/PERF08.JavaScriptPerformanceAnalyzer.md) | H | #9 | Static, CI-friendly, highly fixable findings. |
| 8 | Form Performance Analyzer | ✅ | [PERF10](03-Performance/PERF10.FormPerformanceAnalyzer.md) | H | #7 | Fills the gap views/dashboards don't cover; clean unit-test target. |
| 9 | Solution Merge Assistant | ✅ | [ALM01](01-ALM-DevOps/ALM01.SolutionMergeAssistant.md) | H | #6 | No first-class MS tooling for multi-solution merge. |
| 10 | Managed Solution Impact Checker | ✅ | [ALM04](01-ALM-DevOps/ALM04.ManagedSolutionImpactChecker.md) | H | — | Layering surprises are the classic prod incident. |
| 11 | Audit Compliance Checker | ✅ | [SEC05](02-Security-Governance/SEC05.AuditComplianceChecker.md) | H | #18 | Compliance proof that stays invisible until an incident. |
| 12 | Sharing Analyzer | ✅ | [SEC04](02-Security-Governance/SEC04.SharingAnalyzer.md) | H | — | `PrincipalObjectAccess` debt — invisible security/perf risk. |
| 13 | Portal Health Analyzer | ✅ | [PP01](06-Power-Pages/PP01.PortalHealthAnalyzer.md) | H | — | Seeds the shared adx_/mspp_ Power Pages layer for the PP track. |
| 14 | Environment Comparison Suite | ✅ | [MIG01](10-Migration-Integration/MIG01.EnvironmentComparisonSuite.md) | H | #1 | Build the diff engine once, share with ADMIN08 Drift Monitor. |
| 15 | Solution Documentation Generator | ✅ | [SOLN05](05-Solution-Management/SOLN05.SolutionDocumentationGenerator.md) | H | #11 | Reuses #2 inventory + shipped export chains. |

## Phase C — consumers & quick wins (cheap once A/B exist)

> **Status:** ✅ all of #16–#20 built.

| # | Tool | Status | Backlog file | Value | Pack# | Depends on |
|---|---|---|---|---|---|---|
| 16 | View Performance Analyzer | ✅ | [PERF04](03-Performance/PERF04.ViewPerformanceAnalyzer.md) | H | — | #1 FetchXML engine |
| 17 | Team Permission Explorer | ✅ | [SEC02](02-Security-Governance/SEC02.TeamPermissionExplorer.md) | H | — | #3 privilege engine |
| 18 | ERD Generator | ✅ | [DOC02](11-Documentation/DOC02.ErdGenerator.md) | H | — | #2 inventory → Mermaid/PNG; quick win |
| 19 | Duplicate Metadata Finder | ✅ | [ADMIN03](04-Dataverse-Administration/ADMIN03.DuplicateMetadataFinder.md) | M | #14 | extends Attribute Auditor plumbing |
| 20 | Custom API Explorer | ✅ | [PLUGIN06](08-Plugins-Custom-APIs/PLUGIN06.CustomApiExplorer.md) | H | #13 | #6 plugin retrieval; gated test console |

## Pack coverage

Delivers **10 of the 20 pack ideas** directly: #1, #4, #6, #7, #9, #10, #11, #13, #14, #18 — including
all the highest-value "same-tool" ones.

## Deliberately deferred (and why)

- **Flow Failure Investigator / Flow Performance Dashboard** — run history/metrics aren't in the
  Dataverse SDK; low feasibility until a separate Power Automate API is wired.
- **Solution Quality Score, Technical Debt Trends** — better built as **extensions of shipped tools**
  (Solution Complexity Score; a "Trends" tab on Technical Debt Analyzer) than as new tools.
  **Both have now shipped as extensions:** **SOLN04** as Solution Complexity Score's **FEAT-SOLN08-4**
  (build-quality score over the same `ComponentCounts`), and **RPT04** as Technical Debt Analyzer's
  **FEAT-SOLN10-4** (a Trends tab charting the debt score run-over-run from per-machine JSON snapshots). See
  [../design/SOLN04-RPT04-extensions.md](../design/SOLN04-RPT04-extensions.md).
- **The 9 AI Assistants** — build after their deterministic tracks exist, then layer on the shipped
  AI Solution Reviewer plumbing.
- **Dashboards/scorecards (Executive, Governance, Health)** — aggregators; they need several of the
  above to exist first.
