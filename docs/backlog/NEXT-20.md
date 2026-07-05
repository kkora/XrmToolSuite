# Next 20 tools to implement

Ranked build plan picked from the [backlog](README.md), ordered so **shared engines land before the
tools that consume them**, weighted toward high value + SDK-feasibility + the strongest prompt-pack
ideas. See [RECOMMENDATIONS.md](RECOMMENDATIONS.md) for the full reasoning and
[README.md](README.md) for the per-tool user stories.

> **Build status (2026-07-05): 19 of 20 implemented.** Every tool in Phases A, B, and C except #20 is built —
> shipped as a project under `src/Tools/`, present in `XrmToolSuite.sln`, UI-free engine + WinForms host,
> with user stories under `docs/user-stories/<TAG>.<Tool>.md`, SDK-free unit tests in `testing/UnitTests`,
> and testing artifacts under `testing/<Tool>/`. **Only #20 Custom API Explorer (PLUGIN6) remains un-built**
> (no `src/Tools/` project yet). Live-connection and WinForms GUI cases (including Office/PDF export dialogs)
> remain pending a Windows + XrmToolBox session across all built tools — documented, not claimed as passed.

Legend: **H** High · **M** Medium · Pack# = matching idea in `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt`.
Status: ✅ built · ⬜ not started.

## Phase A — foundational engines (build first; everything else reuses them)

| # | Tool | Status | Backlog file | Value | Pack# | Why first |
|---|---|---|---|---|---|---|
| 1 | FetchXML Performance Analyzer | ✅ | [PERF3](03-Performance/PERF3.FetchXmlPerformanceAnalyzer.md) | H | — | Shared FetchXML parser/rules engine reused by #16 View + Dashboard + Portal Perf. Pure static. |
| 2 | Environment Inventory | ✅ | [ADMIN7](04-Dataverse-Administration/ADMIN7.EnvironmentInventory.md) | H | — | The normalized metadata model that feeds ERD / Docs / Reporting / Drift tools. |
| 3 | Privilege Gap Analyzer | ✅ | [SEC1](02-Security-Governance/SEC1.PrivilegeGapAnalyzer.md) | H | #2 | Ships the effective-privilege engine reused by #17 Team Explorer, Matrix, Heatmap. |

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
| 4 | Flow Dependency Analyzer | ✅ | [PA1](07-Power-Automate/PA1.FlowDependencyAnalyzer.md) | H | #4 | Reuses DRA's `clientdata` parser. |
| 5 | Component Usage Explorer | ✅ | [SOLN1](05-Solution-Management/SOLN1.ComponentUsageExplorer.md) | H | — | "Where is this used before I change it"; reuses Knowledge Graph. |
| 6 | Plugin Dependency Graph | ✅ | [PLUGIN1](08-Plugins-Custom-APIs/PLUGIN1.PluginDependencyGraph.md) | H | #10 | Ships shared plugin-metadata retrieval for the PLUGIN track. |
| 7 | JavaScript Performance Analyzer | ✅ | [PERF8](03-Performance/PERF8.JavaScriptPerformanceAnalyzer.md) | H | #9 | Static, CI-friendly, highly fixable findings. |
| 8 | Form Performance Analyzer | ✅ | [PERF10](03-Performance/PERF10.FormPerformanceAnalyzer.md) | H | #7 | Fills the gap views/dashboards don't cover; clean unit-test target. |
| 9 | Solution Merge Assistant | ✅ | [ALM1](01-ALM-DevOps/ALM1.SolutionMergeAssistant.md) | H | #6 | No first-class MS tooling for multi-solution merge. |
| 10 | Managed Solution Impact Checker | ✅ | [ALM4](01-ALM-DevOps/ALM4.ManagedSolutionImpactChecker.md) | H | — | Layering surprises are the classic prod incident. |
| 11 | Audit Compliance Checker | ✅ | [SEC5](02-Security-Governance/SEC5.AuditComplianceChecker.md) | H | #18 | Compliance proof that stays invisible until an incident. |
| 12 | Sharing Analyzer | ✅ | [SEC4](02-Security-Governance/SEC4.SharingAnalyzer.md) | H | — | `PrincipalObjectAccess` debt — invisible security/perf risk. |
| 13 | Portal Health Analyzer | ✅ | [PP1](06-Power-Pages/PP1.PortalHealthAnalyzer.md) | H | — | Seeds the shared adx_/mspp_ Power Pages layer for the PP track. |
| 14 | Environment Comparison Suite | ✅ | [MIG1](10-Migration-Integration/MIG1.EnvironmentComparisonSuite.md) | H | #1 | Build the diff engine once, share with ADMIN8 Drift Monitor. |
| 15 | Solution Documentation Generator | ✅ | [SOLN5](05-Solution-Management/SOLN5.SolutionDocumentationGenerator.md) | H | #11 | Reuses #2 inventory + shipped export chains. |

## Phase C — consumers & quick wins (cheap once A/B exist)

> **Status:** #16–#19 ✅ built; **#20 is the only remaining work** — no `src/Tools/` project yet.

| # | Tool | Status | Backlog file | Value | Pack# | Depends on |
|---|---|---|---|---|---|---|
| 16 | View Performance Analyzer | ✅ | [PERF4](03-Performance/PERF4.ViewPerformanceAnalyzer.md) | H | — | #1 FetchXML engine |
| 17 | Team Permission Explorer | ✅ | [SEC2](02-Security-Governance/SEC2.TeamPermissionExplorer.md) | H | — | #3 privilege engine |
| 18 | ERD Generator | ✅ | [DOC2](11-Documentation/DOC2.ErdGenerator.md) | H | — | #2 inventory → Mermaid/PNG; quick win |
| 19 | Duplicate Metadata Finder | ✅ | [ADMIN3](04-Dataverse-Administration/ADMIN3.DuplicateMetadataFinder.md) | M | #14 | extends Attribute Auditor plumbing |
| 20 | Custom API Explorer | ⬜ | [PLUGIN6](08-Plugins-Custom-APIs/PLUGIN6.CustomApiExplorer.md) | H | #13 | #6 plugin retrieval; gated test console |

## Pack coverage

Delivers **10 of the 20 pack ideas** directly: #1, #4, #6, #7, #9, #10, #11, #13, #14, #18 — including
all the highest-value "same-tool" ones.

## Deliberately deferred (and why)

- **Flow Failure Investigator / Flow Performance Dashboard** — run history/metrics aren't in the
  Dataverse SDK; low feasibility until a separate Power Automate API is wired.
- **Solution Quality Score, Technical Debt Trends** — better built as **extensions of shipped tools**
  (Solution Complexity Score; a "Trends" tab on Technical Debt Analyzer) than as new tools.
- **The 9 AI Assistants** — build after their deterministic tracks exist, then layer on the shipped
  AI Solution Reviewer plumbing.
- **Dashboards/scorecards (Executive, Governance, Health)** — aggregators; they need several of the
  above to exist first.
