# User stories

One Epic / Features / User-Stories file per **implemented** tool. These are the "as-built" specs — each
traces to a source spec under [`docs/backlog/`](../backlog/) and to the tool's testing artifacts under
[`testing/<Tool>/`](../../testing/). Candidate tools that aren't built yet live only in `docs/backlog/`
(see [NEXT-20.md](../backlog/NEXT-20.md) for the build plan) and get a file here when they ship.

**Naming:** `<TAG>N.<ToolName>.md`, matching the backlog area tag (e.g. `PERF3.FetchXmlPerformanceAnalyzer.md`).
The six flagship tools shipped before the tagging convention keep untagged filenames, matching their
untagged backlog files.

## Shipped tools (pre-tagging convention)

| Tool | User stories | Backlog | Project |
|---|---|---|---|
| Deployment Risk Analyzer | [DeploymentRiskAnalyzer.md](DeploymentRiskAnalyzer.md) | [ALM](../backlog/01-ALM-DevOps/DeploymentRiskAnalyzer.md) | `XrmToolSuite.DeploymentRiskAnalyzer` |
| Attribute Auditor | [AttributeAuditor.md](AttributeAuditor.md) | [ADMIN](../backlog/04-Dataverse-Administration/AttributeAuditor.md) | `XrmToolSuite.AttributeAuditor` |
| Technical Debt Analyzer | [TechnicalDebtAnalyzer.md](TechnicalDebtAnalyzer.md) | [SOLN](../backlog/05-Solution-Management/TechnicalDebtAnalyzer.md) | `XrmToolSuite.TechnicalDebtAnalyzer` |
| Solution Knowledge Graph | [SolutionKnowledgeGraph.md](SolutionKnowledgeGraph.md) | [SOLN](../backlog/05-Solution-Management/SolutionKnowledgeGraph.md) | `XrmToolSuite.SolutionKnowledgeGraph` |
| Solution Complexity Score | [SolutionComplexityScore.md](SolutionComplexityScore.md) | [SOLN](../backlog/05-Solution-Management/SolutionComplexityScore.md) | `XrmToolSuite.SolutionComplexityScore` |
| AI Solution Reviewer | [AiSolutionReviewer.md](AiSolutionReviewer.md) | [AI](../backlog/09-AI-Assistants/AiSolutionReviewer.md) | `XrmToolSuite.AiSolutionReviewer` |

## Backlog build (NEXT-20, tagged)

| Tag | Tool | User stories | Project |
|---|---|---|---|
| PERF3 | FetchXML Performance Analyzer | [PERF3.FetchXmlPerformanceAnalyzer.md](PERF3.FetchXmlPerformanceAnalyzer.md) | `XrmToolSuite.FetchXmlPerformanceAnalyzer` |
| ADMIN7 | Environment Inventory | [ADMIN7.EnvironmentInventory.md](ADMIN7.EnvironmentInventory.md) | `XrmToolSuite.EnvironmentInventory` |
| SEC1 | Privilege Gap Analyzer | [SEC1.PrivilegeGapAnalyzer.md](SEC1.PrivilegeGapAnalyzer.md) | `XrmToolSuite.PrivilegeGapAnalyzer` |
| PA1 | Flow Dependency Analyzer | [PA1.FlowDependencyAnalyzer.md](PA1.FlowDependencyAnalyzer.md) | `XrmToolSuite.FlowDependencyAnalyzer` |
| SOLN1 | Component Usage Explorer | [SOLN1.ComponentUsageExplorer.md](SOLN1.ComponentUsageExplorer.md) | `XrmToolSuite.ComponentUsageExplorer` |
| PLUGIN1 | Plugin Dependency Graph | [PLUGIN1.PluginDependencyGraph.md](PLUGIN1.PluginDependencyGraph.md) | `XrmToolSuite.PluginDependencyGraph` |
| PERF8 | JavaScript Performance Analyzer | [PERF8.JavaScriptPerformanceAnalyzer.md](PERF8.JavaScriptPerformanceAnalyzer.md) | `XrmToolSuite.JavaScriptPerformanceAnalyzer` |
| PERF10 | Form Performance Analyzer | [PERF10.FormPerformanceAnalyzer.md](PERF10.FormPerformanceAnalyzer.md) | `XrmToolSuite.FormPerformanceAnalyzer` |
| ALM1 | Solution Merge Assistant | [ALM1.SolutionMergeAssistant.md](ALM1.SolutionMergeAssistant.md) | `XrmToolSuite.SolutionMergeAssistant` |
| ALM4 | Managed Solution Impact Checker | [ALM4.ManagedSolutionImpactChecker.md](ALM4.ManagedSolutionImpactChecker.md) | `XrmToolSuite.ManagedSolutionImpactChecker` |
| SEC5 | Audit Compliance Checker | [SEC5.AuditComplianceChecker.md](SEC5.AuditComplianceChecker.md) | `XrmToolSuite.AuditComplianceChecker` |
| SEC4 | Sharing Analyzer | [SEC4.SharingAnalyzer.md](SEC4.SharingAnalyzer.md) | `XrmToolSuite.SharingAnalyzer` |
| PP1 | Portal Health Analyzer | [PP1.PortalHealthAnalyzer.md](PP1.PortalHealthAnalyzer.md) | `XrmToolSuite.PortalHealthAnalyzer` |
| MIG1 | Environment Comparison Suite | [MIG1.EnvironmentComparisonSuite.md](MIG1.EnvironmentComparisonSuite.md) | `XrmToolSuite.EnvironmentComparisonSuite` |
| SOLN5 | Solution Documentation Generator | [SOLN5.SolutionDocumentationGenerator.md](SOLN5.SolutionDocumentationGenerator.md) | `XrmToolSuite.SolutionDocumentationGenerator` |
| PERF4 | View Performance Analyzer | [PERF4.ViewPerformanceAnalyzer.md](PERF4.ViewPerformanceAnalyzer.md) | `XrmToolSuite.ViewPerformanceAnalyzer` |
| SEC2 | Team Permission Explorer | [SEC2.TeamPermissionExplorer.md](SEC2.TeamPermissionExplorer.md) | `XrmToolSuite.TeamPermissionExplorer` |
| DOC2 | ERD Generator | [DOC2.ErdGenerator.md](DOC2.ErdGenerator.md) | `XrmToolSuite.ErdGenerator` |
| ADMIN3 | Duplicate Metadata Finder | [ADMIN3.DuplicateMetadataFinder.md](ADMIN3.DuplicateMetadataFinder.md) | `XrmToolSuite.DuplicateMetadataFinder` |
| PLUGIN6 | Custom API Explorer | [PLUGIN6.CustomApiExplorer.md](PLUGIN6.CustomApiExplorer.md) | `XrmToolSuite.CustomApiExplorer` |

All 20 NEXT-20 tools are now built.

## Scaffold

| Tool | User stories | Project |
|---|---|---|
| Template Tool | [TemplateTool.md](TemplateTool.md) | `XrmToolSuite.TemplateTool` |
