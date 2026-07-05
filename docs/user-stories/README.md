# User stories

One Epic / Features / User-Stories file per **implemented** tool. These are the "as-built" specs — each
traces to a source spec under [`docs/backlog/`](../backlog/) and to the tool's testing artifacts under
[`testing/<Tool>/`](../../testing/). Candidate tools that aren't built yet live only in `docs/backlog/`
(see [NEXT-20.md](../backlog/NEXT-20.md) for the build plan) and get a file here when they ship.

**Naming:** every user-story file is `<TAG>NN.<ToolName>.md`, where `<TAG>` is the category code and `NN`
is the two-digit item number within that category (zero-padded: `01`, `02`, … `10`) — e.g.
`PERF03.FetchXmlPerformanceAnalyzer.md`, `SEC04.SharingAnalyzer.md`. The **same** `<TAG>NN` names the tool's
backlog file and its EPIC/FEAT/US ids. Every tool follows this, including the six that shipped first (they
took the next number in their category: `ALM07`, `ADMIN10`, `SOLN08`, `SOLN09`, `SOLN10`, `AI10`). The
`TemplateTool.md` scaffold is exempt (it isn't a shipped tool).

## Shipped tools (built first)

| Tag | Tool | User stories | Backlog | Project |
|---|---|---|---|---|
| ALM07 | Deployment Risk Analyzer | [ALM07.DeploymentRiskAnalyzer.md](ALM07.DeploymentRiskAnalyzer.md) | [ALM](../backlog/01-ALM-DevOps/ALM07.DeploymentRiskAnalyzer.md) | `XrmToolSuite.DeploymentRiskAnalyzer` |
| ADMIN10 | Attribute Auditor | [ADMIN10.AttributeAuditor.md](ADMIN10.AttributeAuditor.md) | [ADMIN](../backlog/04-Dataverse-Administration/ADMIN10.AttributeAuditor.md) | `XrmToolSuite.AttributeAuditor` |
| SOLN10 | Technical Debt Analyzer | [SOLN10.TechnicalDebtAnalyzer.md](SOLN10.TechnicalDebtAnalyzer.md) | [SOLN](../backlog/05-Solution-Management/SOLN10.TechnicalDebtAnalyzer.md) | `XrmToolSuite.TechnicalDebtAnalyzer` |
| SOLN09 | Solution Knowledge Graph | [SOLN09.SolutionKnowledgeGraph.md](SOLN09.SolutionKnowledgeGraph.md) | [SOLN](../backlog/05-Solution-Management/SOLN09.SolutionKnowledgeGraph.md) | `XrmToolSuite.SolutionKnowledgeGraph` |
| SOLN08 | Solution Complexity Score | [SOLN08.SolutionComplexityScore.md](SOLN08.SolutionComplexityScore.md) | [SOLN](../backlog/05-Solution-Management/SOLN08.SolutionComplexityScore.md) | `XrmToolSuite.SolutionComplexityScore` |
| AI10 | AI Solution Reviewer | [AI10.AiSolutionReviewer.md](AI10.AiSolutionReviewer.md) | [AI](../backlog/09-AI-Assistants/AI10.AiSolutionReviewer.md) | `XrmToolSuite.AiSolutionReviewer` |

## Backlog build (NEXT-20, tagged)

| Tag | Tool | User stories | Project |
|---|---|---|---|
| PERF03 | FetchXML Performance Analyzer | [PERF03.FetchXmlPerformanceAnalyzer.md](PERF03.FetchXmlPerformanceAnalyzer.md) | `XrmToolSuite.FetchXmlPerformanceAnalyzer` |
| ADMIN07 | Environment Inventory | [ADMIN07.EnvironmentInventory.md](ADMIN07.EnvironmentInventory.md) | `XrmToolSuite.EnvironmentInventory` |
| SEC01 | Privilege Gap Analyzer | [SEC01.PrivilegeGapAnalyzer.md](SEC01.PrivilegeGapAnalyzer.md) | `XrmToolSuite.PrivilegeGapAnalyzer` |
| PA01 | Flow Dependency Analyzer | [PA01.FlowDependencyAnalyzer.md](PA01.FlowDependencyAnalyzer.md) | `XrmToolSuite.FlowDependencyAnalyzer` |
| SOLN01 | Component Usage Explorer | [SOLN01.ComponentUsageExplorer.md](SOLN01.ComponentUsageExplorer.md) | `XrmToolSuite.ComponentUsageExplorer` |
| PLUGIN01 | Plugin Dependency Graph | [PLUGIN01.PluginDependencyGraph.md](PLUGIN01.PluginDependencyGraph.md) | `XrmToolSuite.PluginDependencyGraph` |
| PERF08 | JavaScript Performance Analyzer | [PERF08.JavaScriptPerformanceAnalyzer.md](PERF08.JavaScriptPerformanceAnalyzer.md) | `XrmToolSuite.JavaScriptPerformanceAnalyzer` |
| PERF10 | Form Performance Analyzer | [PERF10.FormPerformanceAnalyzer.md](PERF10.FormPerformanceAnalyzer.md) | `XrmToolSuite.FormPerformanceAnalyzer` |
| ALM01 | Solution Merge Assistant | [ALM01.SolutionMergeAssistant.md](ALM01.SolutionMergeAssistant.md) | `XrmToolSuite.SolutionMergeAssistant` |
| ALM04 | Managed Solution Impact Checker | [ALM04.ManagedSolutionImpactChecker.md](ALM04.ManagedSolutionImpactChecker.md) | `XrmToolSuite.ManagedSolutionImpactChecker` |
| SEC05 | Audit Compliance Checker | [SEC05.AuditComplianceChecker.md](SEC05.AuditComplianceChecker.md) | `XrmToolSuite.AuditComplianceChecker` |
| SEC04 | Sharing Analyzer | [SEC04.SharingAnalyzer.md](SEC04.SharingAnalyzer.md) | `XrmToolSuite.SharingAnalyzer` |
| PP01 | Portal Health Analyzer | [PP01.PortalHealthAnalyzer.md](PP01.PortalHealthAnalyzer.md) | `XrmToolSuite.PortalHealthAnalyzer` |
| MIG01 | Environment Comparison Suite | [MIG01.EnvironmentComparisonSuite.md](MIG01.EnvironmentComparisonSuite.md) | `XrmToolSuite.EnvironmentComparisonSuite` |
| SOLN05 | Solution Documentation Generator | [SOLN05.SolutionDocumentationGenerator.md](SOLN05.SolutionDocumentationGenerator.md) | `XrmToolSuite.SolutionDocumentationGenerator` |
| PERF04 | View Performance Analyzer | [PERF04.ViewPerformanceAnalyzer.md](PERF04.ViewPerformanceAnalyzer.md) | `XrmToolSuite.ViewPerformanceAnalyzer` |
| SEC02 | Team Permission Explorer | [SEC02.TeamPermissionExplorer.md](SEC02.TeamPermissionExplorer.md) | `XrmToolSuite.TeamPermissionExplorer` |
| DOC02 | ERD Generator | [DOC02.ErdGenerator.md](DOC02.ErdGenerator.md) | `XrmToolSuite.ErdGenerator` |
| ADMIN03 | Duplicate Metadata Finder | [ADMIN03.DuplicateMetadataFinder.md](ADMIN03.DuplicateMetadataFinder.md) | `XrmToolSuite.DuplicateMetadataFinder` |
| PLUGIN06 | Custom API Explorer | [PLUGIN06.CustomApiExplorer.md](PLUGIN06.CustomApiExplorer.md) | `XrmToolSuite.CustomApiExplorer` |

All 20 NEXT-20 tools are now built.

## Scaffold

| Tool | User stories | Project |
|---|---|---|
| Template Tool | [TemplateTool.md](TemplateTool.md) | `XrmToolSuite.TemplateTool` |
