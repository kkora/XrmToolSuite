# XrmToolSuite — Tool Backlog (by category)

Per-category home for the suite's tool user stories: **candidate** tools generated from the
**XrmToolBox Enterprise Prompt Pack** in [`prompt/`](../../prompt/), plus the **6 already-shipped**
tools' stories filed into their category and marked **Done**.

> **Build status (living):** this file is the original planning snapshot (the `✅ Done` marks below are
> the 6 tools shipped when it was generated). Since then **all 20 tools in [NEXT-20.md](NEXT-20.md) have
> shipped**, plus **DOC01 Architecture Diagram Generator** and **DOC06 API Documentation Builder** beyond the
NEXT-20 set — so **28 tools total** are now built. NEXT-20.md is the authoritative build tracker; the
> as-built user stories live in [`../user-stories/`](../user-stories/README.md). The category tables
> below still reflect the candidate/priority planning and are not re-marked per build.

## What this is

- **98 user-story files** across **12 category folders**:
  - **92 candidates** — **89** from the "all prompts" file (`prompt/3.XrmToolBox_ALL_PROMPTS.txt`,
    12 categories) + **3** pack-only ideas (from `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt`) with no
    equivalent in the 89, **merged into their matching category folder** (PERF10, PLUGIN06, SOLN07),
    each still marked `Source: pack`.
  - **6 shipped tools** (`✅ Done`) — filed by category (Deployment Risk Analyzer, Attribute Auditor,
    Solution Complexity Score, Solution Knowledge Graph, Technical Debt Analyzer, AI Solution Reviewer).
    They use the category tag + next number like every other tool (`ALM07`, `ADMIN10`, `SOLN08`, `SOLN09`,
    `SOLN10`, `AI10`), with their EPIC/FEAT/US ids renumbered to match. (The portfolio/platform epics live in
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

`<TAG><n>.<PascalPluginName>.md` — e.g. `ALM01.SolutionMergeAssistant.md`. `<TAG>` is the category
code, `<n>` is the plugin's item number within that category. **Every** tool follows this, including the
six originally-shipped tools, which take the next number in their category: `ALM07` (Deployment Risk
Analyzer), `ADMIN10` (Attribute Auditor), `SOLN08` (Solution Complexity Score), `SOLN09` (Solution Knowledge
Graph), `SOLN10` (Technical Debt Analyzer), `AI10` (AI Solution Reviewer). The **user-story file** under
`docs/user-stories/` uses the **same** `<TAG><n>.<Name>.md` name, and the tool's EPIC/FEAT/US ids use the
same `<TAG><n>` as their area (`EPIC-SOLN08`, `US-SOLN08-…`).

## Where each prompt was picked from

| Marker | Meaning |
|---|---|
| `all` | From `prompt/3.XrmToolBox_ALL_PROMPTS.txt` (the 89-plugin, 12-category file). |
| `all` + pack twin | Also appears in `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt` — noted in the file header. |
| `pack` | **Only** in `prompt/2...` (the 20-idea pack); no equivalent among the 89. Merged into its matching category folder (PERF10, PLUGIN06, SOLN07); header still marked `Source: pack`. |

The other 17 pack ideas all map onto an "all" candidate and are annotated on that candidate's header
rather than duplicated (see RECOMMENDATIONS.md → "Pack-file mapping").

## Folders & index

Value = my read (see each file's header for the reasoning). Legend: **H**igh · **M**edium-**H**igh · **M**edium · **L**ow · **✅ Done** = shipped tool (bold rows).

### 01 · ALM & DevOps  (`ALM`, source: all + shipped)
| File | Tool | Value | Note |
|---|---|---|---|
| **ALM07.DeploymentRiskAnalyzer.md** | Deployment Risk Analyzer | ✅ Done | **shipped**; superset of ALM02/ALM05/ALM06 + MIG02 |
| ALM01.SolutionMergeAssistant.md | Solution Merge Assistant | H | also pack #6 |
| ALM02.SolutionDependencyValidator.md | Solution Dependency Validator | M | overlaps Deployment Risk Analyzer |
| ALM03.DeploymentTimelineVisualizer.md | Deployment Timeline Visualizer | M | gated by import-history retention |
| ALM04.ManagedSolutionImpactChecker.md | Managed Solution Impact Checker | H | layering incidents, poorly served natively |
| ALM05.EnvironmentVariableValidator.md | Environment Variable Validator | M | pack #12; overlaps DRA |
| ALM06.ConnectionReferenceValidator.md | Connection Reference Validator | M | overlaps DRA + PA05 |

### 02 · Security & Governance  (`SEC`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| SEC01.PrivilegeGapAnalyzer.md | Privilege Gap Analyzer | H | pack #2 (role simulator) |
| SEC02.TeamPermissionExplorer.md | Team Permission Explorer | H | |
| SEC03.FieldSecurityProfiler.md | Field Security Profiler | M-H | |
| SEC04.SharingAnalyzer.md | Sharing Analyzer | H | PrincipalObjectAccess |
| SEC05.AuditComplianceChecker.md | Audit Compliance Checker | H | pack #18 (log analyzer) |
| SEC06.SecurityMatrixGenerator.md | Security Matrix Generator | M-H | |
| SEC07.SensitiveDataScanner.md | Sensitive Data Scanner | H | metadata-first, masked samples |
| SEC08.LicensingUsageAnalyzer.md | Licensing Usage Analyzer | M | estimates only (no SKU API) |
| SEC09.EnvironmentGovernanceScore.md | Environment Governance Score | M-H | aggregator; overlaps RPT03 |
| SEC10.UserAccessHeatmap.md | User Access Heatmap | M-H | shares effective-privilege engine |

### 03 · Performance  (`PERF`, source: all + pack)
| File | Tool | Value | Note |
|---|---|---|---|
| PERF01.PluginPerformanceProfiler.md | Plugin Performance Profiler | H | pack #3 |
| PERF02.ApiLatencyAnalyzer.md | API Latency Analyzer | M | write tests opt-in |
| PERF03.FetchXmlPerformanceAnalyzer.md | FetchXML Performance Analyzer | H | **shared engine** for PERF04/PERF05/PP04 |
| PERF04.ViewPerformanceAnalyzer.md | View Performance Analyzer | H | reuses PERF03 |
| PERF05.DashboardPerformanceChecker.md | Dashboard Performance Checker | M | reuses PERF03 |
| PERF06.PcfPerformanceInspector.md | PCF Performance Inspector | M | pack #8 |
| PERF07.BusinessRulePerformanceAnalyzer.md | Business Rule Performance Analyzer | M | pack #16 |
| PERF08.JavaScriptPerformanceAnalyzer.md | JavaScript Performance Analyzer | H | pack #9; static, CI-friendly |
| PERF09.DataverseStorageOptimizer.md | Dataverse Storage Optimizer | H | pack #19; read-only |
| PERF10.FormPerformanceAnalyzer.md | Form Performance Analyzer | H | **pack #7** (merged); forms not in PERF01–9 |

### 04 · Dataverse Administration  (`ADMIN`, source: all + shipped)
| File | Tool | Value | Note |
|---|---|---|---|
| **ADMIN10.AttributeAuditor.md** | Attribute Auditor | ✅ Done | **shipped**; twin of ADMIN02 / ADMIN03 |
| ADMIN01.EnvironmentHealthDashboard.md | Environment Health Dashboard | H | meta-tool; overlaps RPT01 |
| ADMIN02.MetadataCleanupAdvisor.md | Metadata Cleanup Advisor | M-H | overlaps Attribute Auditor + TDA |
| ADMIN03.DuplicateMetadataFinder.md | Duplicate Metadata Finder | M | pack #14 |
| ADMIN04.TableGrowthForecast.md | Table Growth Forecast | M | needs snapshot history |
| ADMIN05.StorageCostEstimator.md | Storage Cost Estimator | M | estimation disclaimers |
| ADMIN06.RelationshipValidator.md | Relationship Validator | M-H | pack #17 |
| ADMIN07.EnvironmentInventory.md | Environment Inventory | H | **data backbone** for DOC/RPT |
| ADMIN08.ConfigurationDriftMonitor.md | Configuration Drift Monitor | M-H | pack #1; share engine w/ MIG01 |
| ADMIN09.DataverseIndexAdvisor.md | Dataverse Index Advisor | M | pack #5; advisory only |

### 05 · Solution Management  (`SOLN`, source: all + pack + shipped)
| File | Tool | Value | Note |
|---|---|---|---|
| **SOLN08.SolutionComplexityScore.md** | Solution Complexity Score | ✅ Done | **shipped**; twin of SOLN04 |
| **SOLN09.SolutionKnowledgeGraph.md** | Solution Knowledge Graph | ✅ Done | **shipped**; twin of SOLN02 / PLUGIN01 |
| **SOLN10.TechnicalDebtAnalyzer.md** | Technical Debt Analyzer | ✅ Done | **shipped**; twin of SOLN06 / RPT04 / AI02 |
| SOLN01.ComponentUsageExplorer.md | Component Usage Explorer | H | "where used" before change |
| SOLN02.DependencyHeatmap.md | Dependency Heatmap | M | build on shipped Knowledge Graph |
| SOLN03.ComponentOwnershipTracker.md | Component Ownership Tracker | M | local tagging layer |
| SOLN04.SolutionQualityScore.md | Solution Quality Score | M-H | **extend** shipped Complexity Score |
| SOLN05.SolutionDocumentationGenerator.md | Solution Documentation Generator | H | pack #11; feeds DOC track |
| SOLN06.SolutionModernizationAdvisor.md | Solution Modernization Advisor | M | reuse TDA analyzers |
| SOLN07.SolutionSizeOptimizer.md | Solution Size Optimizer | M | **pack #15** (merged); solution footprint |

### 06 · Power Pages  (`PP`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| PP01.PortalHealthAnalyzer.md | Portal Health Analyzer | H | seeds shared PP data layer |
| PP02.WebTemplateDependencyAnalyzer.md | Web Template Dependency Analyzer | H | Liquid parser |
| PP03.PortalSecurityScanner.md | Portal Security Scanner | H | anonymous-exposure risk |
| PP04.PortalPerformanceAnalyzer.md | Portal Performance Analyzer | M | reuses PERF03 |
| PP05.PortalMetadataExplorer.md | Portal Metadata Explorer | M | shared PP retrieval layer |
| PP06.PortalContentAuditor.md | Portal Content Auditor | M | reuses PP02 index |
| PP07.PortalAccessibilityChecker.md | Portal Accessibility Checker | M | static-only, not WCAG cert |

### 07 · Power Automate  (`PA`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| PA01.FlowDependencyAnalyzer.md | Flow Dependency Analyzer | H | pack #4; reuse DRA clientdata parser |
| PA02.FlowComplexityAnalyzer.md | Flow Complexity Analyzer | H | static, no run-history risk |
| PA03.FlowFailureInvestigator.md | Flow Failure Investigator | M | run history not in SDK |
| PA04.FlowDocumentationGenerator.md | Flow Documentation Generator | H | reuses PA01/PA02 |
| PA05.ConnectionUsageAnalyzer.md | Connection Usage Analyzer | M | consolidate with ALM06 |
| PA06.FlowPerformanceDashboard.md | Flow Performance Dashboard | M | run metrics not in SDK |
| PA07.TriggerDependencyGraph.md | Trigger Dependency Graph | M-H | |

### 08 · Plugins & Custom APIs  (`PLUGIN`, source: all + pack)
| File | Tool | Value | Note |
|---|---|---|---|
| PLUGIN01.PluginDependencyGraph.md | Plugin Dependency Graph | H | pack #10 |
| PLUGIN02.PluginRegistrationAuditor.md | Plugin Registration Auditor | M | strong overlap w/ DRA |
| PLUGIN03.PluginRecursionDetector.md | Plugin Recursion Detector | M-H | pack #3 related |
| PLUGIN04.PluginImageAnalyzer.md | Plugin Image Analyzer | M | |
| PLUGIN05.PluginExceptionAnalyzer.md | Plugin Exception Analyzer | M | needs plugintracelog on |
| PLUGIN06.CustomApiExplorer.md | Custom API Explorer | H | **pack #13** (merged); adds live test console |

### 09 · AI Assistants  (`AI`, source: all + shipped)
| File | Tool | Value | Note |
|---|---|---|---|
| **AI10.AiSolutionReviewer.md** | AI Solution Reviewer | ✅ Done | **shipped**; the reused AI plumbing behind AI01–AI09 |
| AI01.AiNamingStandardReviewer.md | AI Naming Standard Reviewer | H | |
| AI02.AiTechnicalDebtAdvisor.md | AI Technical Debt Advisor | M-H | AI layer on shipped TDA |
| AI03.AiSecurityReviewer.md | AI Security Reviewer | H | rolls up SEC track |
| AI04.AiPerformanceAdvisor.md | AI Performance Advisor | H | rolls up PERF track |
| AI05.AiArchitectureReviewer.md | AI Architecture Reviewer | M-H | |
| AI06.AiSolutionDocumentationWriter.md | AI Solution Documentation Writer | H | |
| AI07.AiPluginReviewer.md | AI Plugin Reviewer | M-H | rolls up PLUGIN track |
| AI08.AiMigrationAdvisor.md | AI Migration Advisor | M-H | rolls up MIG track |
| AI09.AiGovernanceAssistant.md | AI Governance Assistant | H | |

> All AI tools reuse the shipped **AI Solution Reviewer** plumbing (opt-in, session-only key, consent
> preview, redaction, offline fallback) and each layers on a deterministic-analyzer track.

### 10 · Migration & Integration  (`MIG`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| MIG01.EnvironmentComparisonSuite.md | Environment Comparison Suite | H | pack #1; share engine w/ ADMIN08 |
| MIG02.MigrationReadinessChecker.md | Migration Readiness Checker | M | pack #20; overlaps DRA |
| MIG03.DataMappingVisualizer.md | Data Mapping Visualizer | M | imports mapping file |
| MIG04.ApiIntegrationExplorer.md | API Integration Explorer | M-H | share engine w/ MIG07 |
| MIG05.DataQualityScanner.md | Data Quality Scanner | M-H | read-only, masked |
| MIG06.DuplicateRecordAnalyzer.md | Duplicate Record Analyzer | M | recommend-only |
| MIG07.IntegrationDependencyMap.md | Integration Dependency Map | M-H | graph lens of MIG04 |

### 11 · Documentation  (`DOC`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| DOC01.ArchitectureDiagramGenerator.md | Architecture Diagram Generator | M | ✅ SHIPPED (`XrmToolSuite.ArchitectureDiagramGenerator`) |
| DOC02.ErdGenerator.md | ERD Generator | H | |
| DOC03.MarkdownDocumentationGenerator.md | Markdown Documentation Generator | H | ✅ folded into SOLN05 (Markdown export) |
| DOC04.WordDocumentationGenerator.md | Word Documentation Generator | M-H | ✅ folded into SOLN05 (Word/PDF export) |
| DOC05.HtmlDocumentationPortal.md | HTML Documentation Portal | M-H | ✅ folded into SOLN05 (searchable HTML portal) |
| DOC06.ApiDocumentationBuilder.md | API Documentation Builder | M-H | ✅ SHIPPED (`XrmToolSuite.ApiDocumentationBuilder`) — OpenAPI + redaction specialist |

> The DOC track are format-specific renderers over one shared extracted model (reuse ADMIN07
> Environment Inventory + SOLN05). Strong overlap with SOLN05 — see RECOMMENDATIONS.

### 12 · Reporting & Analytics  (`RPT`, source: all)
| File | Tool | Value | Note |
|---|---|---|---|
| RPT01.ExecutiveDashboard.md | Executive Dashboard | M-H | overlaps ADMIN01 |
| RPT02.AlmKpiDashboard.md | ALM KPI Dashboard | M-H | |
| RPT03.GovernanceScorecard.md | Governance Scorecard | M | de-dupe vs SEC09 |
| RPT04.TechnicalDebtTrends.md | Technical Debt Trends | H | trends tab on shipped TDA |
| RPT05.DeploymentAnalytics.md | Deployment Analytics | M | limited native history |
| RPT06.UsageAnalytics.md | Usage Analytics | M | inferred telemetry |
| RPT07.ChangeHistoryDashboard.md | Change History Dashboard | M | best-effort attribution |
| RPT08.SolutionBenchmarkDashboard.md | Solution Benchmark Dashboard | M-H | reuse Complexity Score |

> **Pack-only ideas** (no ALL_PROMPTS twin) were merged into the categories above and keep `Source:
> pack` in their headers: **PERF10** Form Performance Analyzer (pack #7), **PLUGIN06** Custom API
> Explorer (pack #13), **SOLN07** Solution Size Optimizer (pack #15).

---

*Generated from the prompt pack as a planning artifact. No tools were created or committed.*
