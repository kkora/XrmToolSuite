# XrmToolSuite — Tool Backlog (by category)

Per-category home for the suite's tool user stories: **candidate** tools generated from the
**XrmToolBox Enterprise Prompt Pack** in [`prompt/`](../../prompt/), plus the **6 already-shipped**
tools' stories filed into their category and marked **Done**. No code was written and nothing is
committed by this pass.

## What this is

- **98 user-story files** across **12 category folders**:
  - **92 candidates** — **89** from the "all prompts" file (`prompt/3.XrmToolBox_ALL_PROMPTS.txt`,
    12 categories) + **3** pack-only ideas (from `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt`) with no
    equivalent in the 89, **merged into their matching category folder** (PERF10, PLUGIN6, SOLN7),
    each still marked `Source: pack`.
  - **6 shipped tools** (`✅ Done`) — filed by category (Deployment Risk Analyzer, Attribute Auditor,
    Solution Complexity Score, Solution Knowledge Graph, Technical Debt Analyzer, AI Solution Reviewer).
    They keep their original area tags and story IDs. (The portfolio/platform epics live in
    [`../README.md`](../README.md); `../_TEMPLATE.md` is what `New-Tool.ps1` stamps for new tools.)
- Each file follows the suite's existing convention (Epic → Feature → User Story with Acceptance
  Criteria, `[Planned]` status, `As a … I want … so that …`). See
  [`../README.md`](../README.md) for the personas and ID scheme they reuse.
- Every file's header records: **Source** (`all` / `pack`, with section + item number and any pack
  twin), **suggested tag & project name**, **overlaps** with shipped tools / sibling candidates, and
  a **Value/priority** read.
- **Prioritization, implement-first waves, shared building blocks, and consolidation advice** live in
  [RECOMMENDATIONS.md](RECOMMENDATIONS.md).

## File-naming format

`<TAG><n>.<PascalPluginName>.md` — e.g. `ALM1.SolutionMergeAssistant.md`. `<TAG>` is the category
code, `<n>` is the plugin's item number within that category's prompt section.

## Where each prompt was picked from

| Marker | Meaning |
|---|---|
| `all` | From `prompt/3.XrmToolBox_ALL_PROMPTS.txt` (the 89-plugin, 12-category file). |
| `all` + pack twin | Also appears in `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt` — noted in the file header. |
| `pack` | **Only** in `prompt/2...` (the 20-idea pack); no equivalent among the 89. Merged into its matching category folder (PERF10, PLUGIN6, SOLN7); header still marked `Source: pack`. |

The other 17 pack ideas all map onto an "all" candidate and are annotated on that candidate's header
rather than duplicated (see RECOMMENDATIONS.md → "Pack-file mapping").

## Folders & index

Value = my read (see each file's header for the reasoning). Legend: **H**igh · **M**edium-**H**igh · **M**edium · **L**ow · **✅ Done** = shipped tool (bold rows).

### 01 · ALM & DevOps  (`ALM`, source: all + shipped)
| File | Tool | Value | Note |
|---|---|---|---|
| **DeploymentRiskAnalyzer.md** | Deployment Risk Analyzer | ✅ Done | **shipped**; superset of ALM2/ALM5/ALM6 + MIG2 |
| ALM1.SolutionMergeAssistant.md | Solution Merge Assistant | H | also pack #6 |
| ALM2.SolutionDependencyValidator.md | Solution Dependency Validator | M | overlaps Deployment Risk Analyzer |
| ALM3.DeploymentTimelineVisualizer.md | Deployment Timeline Visualizer | M | gated by import-history retention |
| ALM4.ManagedSolutionImpactChecker.md | Managed Solution Impact Checker | H | layering incidents, poorly served natively |
| ALM5.EnvironmentVariableValidator.md | Environment Variable Validator | M | pack #12; overlaps DRA |
| ALM6.ConnectionReferenceValidator.md | Connection Reference Validator | M | overlaps DRA + PA5 |

### 02 · Security & Governance  (`SEC`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| SEC1.PrivilegeGapAnalyzer.md | Privilege Gap Analyzer | H | pack #2 (role simulator) |
| SEC2.TeamPermissionExplorer.md | Team Permission Explorer | H | |
| SEC3.FieldSecurityProfiler.md | Field Security Profiler | M-H | |
| SEC4.SharingAnalyzer.md | Sharing Analyzer | H | PrincipalObjectAccess |
| SEC5.AuditComplianceChecker.md | Audit Compliance Checker | H | pack #18 (log analyzer) |
| SEC6.SecurityMatrixGenerator.md | Security Matrix Generator | M-H | |
| SEC7.SensitiveDataScanner.md | Sensitive Data Scanner | H | metadata-first, masked samples |
| SEC8.LicensingUsageAnalyzer.md | Licensing Usage Analyzer | M | estimates only (no SKU API) |
| SEC9.EnvironmentGovernanceScore.md | Environment Governance Score | M-H | aggregator; overlaps RPT3 |
| SEC10.UserAccessHeatmap.md | User Access Heatmap | M-H | shares effective-privilege engine |

### 03 · Performance  (`PERF`, source: all + pack)
| File | Tool | Value | Note |
|---|---|---|---|
| PERF1.PluginPerformanceProfiler.md | Plugin Performance Profiler | H | pack #3 |
| PERF2.ApiLatencyAnalyzer.md | API Latency Analyzer | M | write tests opt-in |
| PERF3.FetchXmlPerformanceAnalyzer.md | FetchXML Performance Analyzer | H | **shared engine** for PERF4/PERF5/PP4 |
| PERF4.ViewPerformanceAnalyzer.md | View Performance Analyzer | H | reuses PERF3 |
| PERF5.DashboardPerformanceChecker.md | Dashboard Performance Checker | M | reuses PERF3 |
| PERF6.PcfPerformanceInspector.md | PCF Performance Inspector | M | pack #8 |
| PERF7.BusinessRulePerformanceAnalyzer.md | Business Rule Performance Analyzer | M | pack #16 |
| PERF8.JavaScriptPerformanceAnalyzer.md | JavaScript Performance Analyzer | H | pack #9; static, CI-friendly |
| PERF9.DataverseStorageOptimizer.md | Dataverse Storage Optimizer | H | pack #19; read-only |
| PERF10.FormPerformanceAnalyzer.md | Form Performance Analyzer | H | **pack #7** (merged); forms not in PERF1–9 |

### 04 · Dataverse Administration  (`ADMIN`, source: all + shipped)
| File | Tool | Value | Note |
|---|---|---|---|
| **AttributeAuditor.md** | Attribute Auditor | ✅ Done | **shipped**; twin of ADMIN2 / ADMIN3 |
| ADMIN1.EnvironmentHealthDashboard.md | Environment Health Dashboard | H | meta-tool; overlaps RPT1 |
| ADMIN2.MetadataCleanupAdvisor.md | Metadata Cleanup Advisor | M-H | overlaps Attribute Auditor + TDA |
| ADMIN3.DuplicateMetadataFinder.md | Duplicate Metadata Finder | M | pack #14 |
| ADMIN4.TableGrowthForecast.md | Table Growth Forecast | M | needs snapshot history |
| ADMIN5.StorageCostEstimator.md | Storage Cost Estimator | M | estimation disclaimers |
| ADMIN6.RelationshipValidator.md | Relationship Validator | M-H | pack #17 |
| ADMIN7.EnvironmentInventory.md | Environment Inventory | H | **data backbone** for DOC/RPT |
| ADMIN8.ConfigurationDriftMonitor.md | Configuration Drift Monitor | M-H | pack #1; share engine w/ MIG1 |
| ADMIN9.DataverseIndexAdvisor.md | Dataverse Index Advisor | M | pack #5; advisory only |

### 05 · Solution Management  (`SOLN`, source: all + pack + shipped)
| File | Tool | Value | Note |
|---|---|---|---|
| **SolutionComplexityScore.md** | Solution Complexity Score | ✅ Done | **shipped**; twin of SOLN4 |
| **SolutionKnowledgeGraph.md** | Solution Knowledge Graph | ✅ Done | **shipped**; twin of SOLN2 / PLUGIN1 |
| **TechnicalDebtAnalyzer.md** | Technical Debt Analyzer | ✅ Done | **shipped**; twin of SOLN6 / RPT4 / AI2 |
| SOLN1.ComponentUsageExplorer.md | Component Usage Explorer | H | "where used" before change |
| SOLN2.DependencyHeatmap.md | Dependency Heatmap | M | build on shipped Knowledge Graph |
| SOLN3.ComponentOwnershipTracker.md | Component Ownership Tracker | M | local tagging layer |
| SOLN4.SolutionQualityScore.md | Solution Quality Score | M-H | **extend** shipped Complexity Score |
| SOLN5.SolutionDocumentationGenerator.md | Solution Documentation Generator | H | pack #11; feeds DOC track |
| SOLN6.SolutionModernizationAdvisor.md | Solution Modernization Advisor | M | reuse TDA analyzers |
| SOLN7.SolutionSizeOptimizer.md | Solution Size Optimizer | M | **pack #15** (merged); solution footprint |

### 06 · Power Pages  (`PP`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| PP1.PortalHealthAnalyzer.md | Portal Health Analyzer | H | seeds shared PP data layer |
| PP2.WebTemplateDependencyAnalyzer.md | Web Template Dependency Analyzer | H | Liquid parser |
| PP3.PortalSecurityScanner.md | Portal Security Scanner | H | anonymous-exposure risk |
| PP4.PortalPerformanceAnalyzer.md | Portal Performance Analyzer | M | reuses PERF3 |
| PP5.PortalMetadataExplorer.md | Portal Metadata Explorer | M | shared PP retrieval layer |
| PP6.PortalContentAuditor.md | Portal Content Auditor | M | reuses PP2 index |
| PP7.PortalAccessibilityChecker.md | Portal Accessibility Checker | M | static-only, not WCAG cert |

### 07 · Power Automate  (`PA`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| PA1.FlowDependencyAnalyzer.md | Flow Dependency Analyzer | H | pack #4; reuse DRA clientdata parser |
| PA2.FlowComplexityAnalyzer.md | Flow Complexity Analyzer | H | static, no run-history risk |
| PA3.FlowFailureInvestigator.md | Flow Failure Investigator | M | run history not in SDK |
| PA4.FlowDocumentationGenerator.md | Flow Documentation Generator | H | reuses PA1/PA2 |
| PA5.ConnectionUsageAnalyzer.md | Connection Usage Analyzer | M | consolidate with ALM6 |
| PA6.FlowPerformanceDashboard.md | Flow Performance Dashboard | M | run metrics not in SDK |
| PA7.TriggerDependencyGraph.md | Trigger Dependency Graph | M-H | |

### 08 · Plugins & Custom APIs  (`PLUGIN`, source: all + pack)
| File | Tool | Value | Note |
|---|---|---|---|
| PLUGIN1.PluginDependencyGraph.md | Plugin Dependency Graph | H | pack #10 |
| PLUGIN2.PluginRegistrationAuditor.md | Plugin Registration Auditor | M | strong overlap w/ DRA |
| PLUGIN3.PluginRecursionDetector.md | Plugin Recursion Detector | M-H | pack #3 related |
| PLUGIN4.PluginImageAnalyzer.md | Plugin Image Analyzer | M | |
| PLUGIN5.PluginExceptionAnalyzer.md | Plugin Exception Analyzer | M | needs plugintracelog on |
| PLUGIN6.CustomApiExplorer.md | Custom API Explorer | H | **pack #13** (merged); adds live test console |

### 09 · AI Assistants  (`AI`, source: all + shipped)
| File | Tool | Value | Note |
|---|---|---|---|
| **AiSolutionReviewer.md** | AI Solution Reviewer | ✅ Done | **shipped**; the reused AI plumbing behind AI1–AI9 |
| AI1.AiNamingStandardReviewer.md | AI Naming Standard Reviewer | H | |
| AI2.AiTechnicalDebtAdvisor.md | AI Technical Debt Advisor | M-H | AI layer on shipped TDA |
| AI3.AiSecurityReviewer.md | AI Security Reviewer | H | rolls up SEC track |
| AI4.AiPerformanceAdvisor.md | AI Performance Advisor | H | rolls up PERF track |
| AI5.AiArchitectureReviewer.md | AI Architecture Reviewer | M-H | |
| AI6.AiSolutionDocumentationWriter.md | AI Solution Documentation Writer | H | |
| AI7.AiPluginReviewer.md | AI Plugin Reviewer | M-H | rolls up PLUGIN track |
| AI8.AiMigrationAdvisor.md | AI Migration Advisor | M-H | rolls up MIG track |
| AI9.AiGovernanceAssistant.md | AI Governance Assistant | H | |

> All AI tools reuse the shipped **AI Solution Reviewer** plumbing (opt-in, session-only key, consent
> preview, redaction, offline fallback) and each layers on a deterministic-analyzer track.

### 10 · Migration & Integration  (`MIG`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| MIG1.EnvironmentComparisonSuite.md | Environment Comparison Suite | H | pack #1; share engine w/ ADMIN8 |
| MIG2.MigrationReadinessChecker.md | Migration Readiness Checker | M | pack #20; overlaps DRA |
| MIG3.DataMappingVisualizer.md | Data Mapping Visualizer | M | imports mapping file |
| MIG4.ApiIntegrationExplorer.md | API Integration Explorer | M-H | share engine w/ MIG7 |
| MIG5.DataQualityScanner.md | Data Quality Scanner | M-H | read-only, masked |
| MIG6.DuplicateRecordAnalyzer.md | Duplicate Record Analyzer | M | recommend-only |
| MIG7.IntegrationDependencyMap.md | Integration Dependency Map | M-H | graph lens of MIG4 |

### 11 · Documentation  (`DOC`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| DOC1.ArchitectureDiagramGenerator.md | Architecture Diagram Generator | M | overlaps Knowledge Graph |
| DOC2.ErdGenerator.md | ERD Generator | H | |
| DOC3.MarkdownDocumentationGenerator.md | Markdown Documentation Generator | H | Git/wiki-friendly |
| DOC4.WordDocumentationGenerator.md | Word Documentation Generator | M-H | DOCX via OpenXml |
| DOC5.HtmlDocumentationPortal.md | HTML Documentation Portal | M-H | |
| DOC6.ApiDocumentationBuilder.md | API Documentation Builder | M-H | redact secrets |

> The DOC track are format-specific renderers over one shared extracted model (reuse ADMIN7
> Environment Inventory + SOLN5). Strong overlap with SOLN5 — see RECOMMENDATIONS.

### 12 · Reporting & Analytics  (`RPT`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| RPT1.ExecutiveDashboard.md | Executive Dashboard | M-H | overlaps ADMIN1 |
| RPT2.AlmKpiDashboard.md | ALM KPI Dashboard | M-H | |
| RPT3.GovernanceScorecard.md | Governance Scorecard | M | de-dupe vs SEC9 |
| RPT4.TechnicalDebtTrends.md | Technical Debt Trends | H | trends tab on shipped TDA |
| RPT5.DeploymentAnalytics.md | Deployment Analytics | M | limited native history |
| RPT6.UsageAnalytics.md | Usage Analytics | M | inferred telemetry |
| RPT7.ChangeHistoryDashboard.md | Change History Dashboard | M | best-effort attribution |
| RPT8.SolutionBenchmarkDashboard.md | Solution Benchmark Dashboard | M-H | reuse Complexity Score |

> **Pack-only ideas** (no ALL_PROMPTS twin) were merged into the categories above and keep `Source:
> pack` in their headers: **PERF10** Form Performance Analyzer (pack #7), **PLUGIN6** Custom API
> Explorer (pack #13), **SOLN7** Solution Size Optimizer (pack #15).

---

*Generated from the prompt pack as a planning artifact. No tools were created or committed.*
