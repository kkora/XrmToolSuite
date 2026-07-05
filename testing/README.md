# testing/ - Test artifacts for XrmToolSuite

Every tool that is created or changed gets its testing artifacts here. This folder is the single
home for test plans, test cases, execution summaries, and screenshots.

## Layout

```
testing/
  README.md                     # this file - the testing convention + index
  UnitTests/                    # executable xUnit project (net8.0) for SDK-free logic
    UnitTests.csproj
    *Tests.cs
  AnalyzerTests/                # executable xUnit project (net48) for the Deployment Risk analyzers
    AnalyzerTests.csproj        # uses the Dataverse SDK + an in-memory fake IOrganizationService
    Fakes/FakeOrganizationService.cs   # the shared fake (also linked by CollectorTests)
    Fakes/MetaBuilder.cs        # shared reflection EntityMetadata fixtures (also linked by CollectorTests)
    *Tests.cs
  CollectorTests/               # executable xUnit project (net48) for the OTHER tools' collectors
    CollectorTests.csproj       # Complexity / AI Reviewer / Knowledge Graph / Technical Debt collectors
    *Tests.cs                   # links the shared fake + MetaBuilder from AnalyzerTests
  ReportTests/                  # executable xUnit project (net48) for the shared report exporters
    ReportTests.csproj          # compiles src/Shared/Reporting/*.cs with ClosedXML + PdfSharp/MigraDoc
  UiSmokeTests/                 # Tier-3 FlaUI UI smoke tests (net48) — interactive, opt-in, NOT in CI
    UiSmokeTests.csproj         # drives the real XrmToolBox host; asserts the suite tools load
  _templates/                   # tokenized skeletons stamped into testing/<Tool>/ by New-Tool.ps1
    TEST_PLAN.md
    TEST_CASES.md
    TEST_SUMMARY.md
  <ToolName>/                   # one folder per tool
    TEST_PLAN.md                # scope, approach, environments, risks
    TEST_CASES.md               # numbered cases: steps, expected result, type, status
    TEST_SUMMARY.md             # execution results (automated + manual), date, verdict
    screenshots/                # captured evidence from manual/GUI runs
```

## The workflow (per tool, on create or update)

For each new or updated tool, produce - in this order:

1. **Plan** - the tool's user stories in [`docs/user-stories/`](../docs/user-stories/README.md) and a
   `TEST_PLAN.md` here.
2. **Tool** - implement/update the tool under `src/Tools/`.
3. **Test cases** - write/refresh `testing/<Tool>/TEST_CASES.md`, tracing each case to a user story.
4. **Execute** - run automated tests (`dotnet test`) and perform manual/GUI cases; drop screenshots
   into `testing/<Tool>/screenshots/`.
5. **Summary** - record results and verdict in `testing/<Tool>/TEST_SUMMARY.md`.

This convention is documented in the repo [`CLAUDE.md`](../CLAUDE.md); `New-Tool.ps1` stamps the
`testing/<Tool>/` skeleton automatically for every new tool.

## Automated vs manual - what runs where

These are XrmToolBox WinForms plugins that call Dataverse. Two tiers of tests:

- **Automated - SDK-free logic (`UnitTests/`, net8.0):** risk scoring, banding, and other pure helpers.
  Runs with the plain .NET SDK - no net48 pack, no Dataverse SDK, no WinForms.

  ```powershell
  dotnet test testing/UnitTests/UnitTests.csproj
  ```

- **Automated - analyzer logic against a fake connection (`AnalyzerTests/`, net48):** the real analyzer
  classes are UI-free (they depend only on `IOrganizationService`), so they can be driven headlessly
  against an in-memory `FakeOrganizationService` - no live org. This covers the query-driven branches
  of the analyzers (which findings fire for which data). It references the Dataverse SDK, so it targets
  net48 and is run on Windows.

  ```powershell
  dotnet test testing/AnalyzerTests/AnalyzerTests.csproj
  ```

  Metadata-level branches that need constructed `EntityMetadata` are also covered here — the schema-conflict
  comparisons (attribute type mismatch, string-length shrink, choice/option-set conflicts, relationship
  schema-name collisions) are seeded via a reflection `EntityMetadata` builder (`Fakes/MetaBuilder.cs`, shared
  with `CollectorTests`).

- **Automated - other tools' collectors against a fake connection (`CollectorTests/`, net48):** the same
  technique applied to the remaining tools' data-collection layers — Solution Complexity Score's
  `ComplexityCollector`, the AI Solution Reviewer's `ReviewCollectors`, the Solution Knowledge Graph's
  `GraphBuilder`, and the Technical Debt Analyzer's analyzers. All are UI-free (`IOrganizationService`
  only) and run against the shared `FakeOrganizationService` (linked from `AnalyzerTests`). Metadata-driven
  Technical Debt branches (row counts, wide tables, publisher prefixes, secured columns) are seeded with a
  reflection `EntityMetadata` builder (`MetaBuilder.cs`); the AI tool's summarization/HTTP path stays manual.

  ```powershell
  dotnet test testing/CollectorTests/CollectorTests.csproj
  ```

- **Automated - report exporters (`ReportTests/`, net48):** compiles the shared exporters under
  `src/Shared/Reporting/` directly (driven through the `DeploymentReportModel` projection) with the
  tool's export dependencies (ClosedXML, PdfSharp/MigraDoc-GDI, Newtonsoft) and asserts each exporter
  produces a well-formed payload — including that the native PDF renders a valid `%PDF-` document. Kept
  separate from `AnalyzerTests/` so those deps don't bloat the analyzer suite.

  ```powershell
  dotnet test testing/ReportTests/ReportTests.csproj
  ```

- **Tier-3 UI smoke (`UiSmokeTests/`, net48, FlaUI — interactive, opt-in, NOT in CI):** drives the real
  XrmToolBox host with UI Automation and asserts every suite plugin loaded into the Tools list — the
  "MEF silently dropped my tool" failure mode no headless test can see (needs no Dataverse connection).
  It requires a logged-in Windows desktop + XrmToolBox with the tools deployed, so it is not wired into
  `test`/`test-windows`; run it deliberately (see [`UiSmokeTests/README.md`](UiSmokeTests/README.md)).

- **Manual (needs a live Dataverse connection + XrmToolBox host):** anything that drives the UI
  (loading solutions, connecting, exporting reports) or depends on real metadata. These are documented
  as numbered cases with steps/expected results; evidence is captured as screenshots under the tool's
  `screenshots/` folder. They cannot be executed in a headless environment.

## Running the manual cases (GUI + screenshots)

The manual cases are the `TC-*-M-*` rows in each `TEST_CASES.md`. They need Windows + XrmToolBox + a
Dataverse sandbox (System Customizer or higher). The AI Solution Reviewer's AI path also needs an
Anthropic/OpenAI/Google API key (used session-only; never persisted).

1. **Build + deploy every tool into XrmToolBox.** This copies each tool DLL — and, for the tools that
   ship them, the ClosedXML/PDF/Word dependency chain — into `%AppData%\MscrmTools\XrmToolBox\Plugins`
   (the Plugins root, where XrmToolBox resolves them at scan time):

   ```powershell
   dotnet build XrmToolSuite.sln -c Release -p:DeployToXTB=true
   ```

   If the DLLs were flagged as downloaded, unblock them once:

   ```powershell
   Get-ChildItem "$env:AppData\MscrmTools\XrmToolBox\Plugins\XrmToolSuite.*.dll" | Unblock-File
   ```

2. **Launch XrmToolBox** (restart it if it was already open, so it re-scans plugins) and **connect** to
   your sandbox. Each tool appears in the Tools list by its display name.

3. **Walk each tool's manual cases in order.** Open `testing/<Tool>/TEST_CASES.md` next to XrmToolBox and
   follow the "Steps" column for every `TC-*-M-*` row.

4. **Capture one screenshot per case** (Win+Shift+S), saved into that tool's `screenshots/` folder named
   by the case ID, e.g.:

   ```
   testing/TechnicalDebtAnalyzer/screenshots/TC-TD-M-04-deprecated.png
   testing/SolutionComplexityScore/screenshots/TC-SC-M-04-html.png
   testing/AiSolutionReviewer/screenshots/TC-AR-M-03-review.png
   testing/SolutionKnowledgeGraph/screenshots/TC-KG-M-04-interactive.png
   ```

   Tip: for the score/report tools, running the analysis then **Export → HTML** gives one screenshot that
   evidences the gauge, metrics, and findings together.

5. **Record results.** Flip each executed row's Status in `TEST_CASES.md` from `Pending` to `Pass`/`Fail`,
   and update the manual rollup + verdict in `TEST_SUMMARY.md`. Log any failure as a defect in the summary
   rather than marking it `Pass`.

## Index

| Tool | Plan | Cases | Summary | Automated status |
|---|---|---|---|---|
| Deployment Risk Analyzer | [plan](DeploymentRiskAnalyzer/TEST_PLAN.md) | [cases](DeploymentRiskAnalyzer/TEST_CASES.md) | [summary](DeploymentRiskAnalyzer/TEST_SUMMARY.md) | 84/84 passed (24 scoring + 54 analyzer + 6 report) |
| Technical Debt Analyzer | [plan](TechnicalDebtAnalyzer/TEST_PLAN.md) | [cases](TechnicalDebtAnalyzer/TEST_CASES.md) | [summary](TechnicalDebtAnalyzer/TEST_SUMMARY.md) | 24/24 passed (5 scoring/metrics + 8 trends store/analytics + 11 analyzer); UI/export/trends manual |
| Solution Complexity Score | [plan](SolutionComplexityScore/TEST_PLAN.md) | [cases](SolutionComplexityScore/TEST_CASES.md) | [summary](SolutionComplexityScore/TEST_SUMMARY.md) | 22/22 passed (14 metric/effort/quality model + 8 collector); UI manual |
| AI Solution Reviewer | [plan](AiSolutionReviewer/TEST_PLAN.md) | [cases](AiSolutionReviewer/TEST_CASES.md) | [summary](AiSolutionReviewer/TEST_SUMMARY.md) | 13/13 passed (4 report/concern score + 9 collectors); AI/Word/UI manual |
| Solution Knowledge Graph | [plan](SolutionKnowledgeGraph/TEST_PLAN.md) | [cases](SolutionKnowledgeGraph/TEST_CASES.md) | [summary](SolutionKnowledgeGraph/TEST_SUMMARY.md) | 18/18 passed (9 model/exporters + 9 builder); PNG/UI manual |
| Attribute Auditor | [plan](AttributeAuditor/TEST_PLAN.md) | [cases](AttributeAuditor/TEST_CASES.md) | [summary](AttributeAuditor/TEST_SUMMARY.md) | 25/25 passed (13 engine + 8 collector); UI/export manual |
| FetchXML Performance Analyzer | [plan](FetchXmlPerformanceAnalyzer/TEST_PLAN.md) | [cases](FetchXmlPerformanceAnalyzer/TEST_CASES.md) | [summary](FetchXmlPerformanceAnalyzer/TEST_SUMMARY.md) | 10/10 passed (parser + rule engine); UI/view-load/timing/export manual |
| Environment Inventory | [plan](EnvironmentInventory/TEST_PLAN.md) | [cases](EnvironmentInventory/TEST_CASES.md) | [summary](EnvironmentInventory/TEST_SUMMARY.md) | 16/16 passed (normalization model + exporters); collector/UI/export manual |
| Privilege Gap Analyzer | [plan](PrivilegeGapAnalyzer/TEST_PLAN.md) | [cases](PrivilegeGapAnalyzer/TEST_CASES.md) | [summary](PrivilegeGapAnalyzer/TEST_SUMMARY.md) | 10/10 passed (effective-privilege engine); collector/UI/export manual |
| View Performance Analyzer | [plan](ViewPerformanceAnalyzer/TEST_PLAN.md) | [cases](ViewPerformanceAnalyzer/TEST_CASES.md) | [summary](ViewPerformanceAnalyzer/TEST_SUMMARY.md) | 11/11 passed (LayoutXML parser + per-view scorer, reuses FetchXML engine); collector/UI/export/timing manual |
| Team Permission Explorer | [plan](TeamPermissionExplorer/TEST_PLAN.md) | [cases](TeamPermissionExplorer/TEST_CASES.md) | [summary](TeamPermissionExplorer/TEST_SUMMARY.md) | 11/11 passed (team risk rules, reuses shared Privilege engine); collector/UI/export manual |
| ERD Generator | [plan](ErdGenerator/TEST_PLAN.md) | [cases](ErdGenerator/TEST_CASES.md) | [summary](ErdGenerator/TEST_SUMMARY.md) | 9/9 passed (ERD model + Mermaid/PlantUML/SVG/JSON emitters); collector/PNG/PDF/UI manual |
| JavaScript Performance Analyzer | [plan](JavaScriptPerformanceAnalyzer/TEST_PLAN.md) | [cases](JavaScriptPerformanceAnalyzer/TEST_CASES.md) | [summary](JavaScriptPerformanceAnalyzer/TEST_SUMMARY.md) | 20/20 passed (static JS rule engine + FormXML event mapper); collector/UI/export manual |
| Form Performance Analyzer | [plan](FormPerformanceAnalyzer/TEST_PLAN.md) | [cases](FormPerformanceAnalyzer/TEST_CASES.md) | [summary](FormPerformanceAnalyzer/TEST_SUMMARY.md) | 14/14 passed (FormXML parser + heaviness scorer); collector/UI/export manual |
| Sharing Analyzer | [plan](SharingAnalyzer/TEST_PLAN.md) | [cases](SharingAnalyzer/TEST_CASES.md) | [summary](SharingAnalyzer/TEST_SUMMARY.md) | 15/15 passed (access-rights decode + sharing risk rules); collector/UI/export manual |
| Audit Compliance Checker | [plan](AuditComplianceChecker/TEST_PLAN.md) | [cases](AuditComplianceChecker/TEST_CASES.md) | [summary](AuditComplianceChecker/TEST_SUMMARY.md) | 19/19 passed (sensitivity heuristics + compliance scoring); collector/UI/export manual |
| Managed Solution Impact Checker | [plan](ManagedSolutionImpactChecker/TEST_PLAN.md) | [cases](ManagedSolutionImpactChecker/TEST_CASES.md) | [summary](ManagedSolutionImpactChecker/TEST_SUMMARY.md) | 15/15 passed (path-aware layering impact rules); collector/UI/export manual |
| Portal Health Analyzer | [plan](PortalHealthAnalyzer/TEST_PLAN.md) | [cases](PortalHealthAnalyzer/TEST_CASES.md) | [summary](PortalHealthAnalyzer/TEST_SUMMARY.md) | 14/14 passed (adx_/mspp_ normalized model + health rules); collector/UI/export manual |
| Solution Merge Assistant | [plan](SolutionMergeAssistant/TEST_PLAN.md) | [cases](SolutionMergeAssistant/TEST_CASES.md) | [summary](SolutionMergeAssistant/TEST_SUMMARY.md) | 14/14 passed (multi-solution conflict detection + verdict); collector/UI/export manual |
| Flow Dependency Analyzer | [plan](FlowDependencyAnalyzer/TEST_PLAN.md) | [cases](FlowDependencyAnalyzer/TEST_CASES.md) | [summary](FlowDependencyAnalyzer/TEST_SUMMARY.md) | 17/17 passed (clientdata parser + redaction + risk rules); collector/UI/export manual |
| Plugin Dependency Graph | [plan](PluginDependencyGraph/TEST_PLAN.md) | [cases](PluginDependencyGraph/TEST_CASES.md) | [summary](PluginDependencyGraph/TEST_SUMMARY.md) | 11/11 passed (graph model/builder/rules/emitters); collector/PNG/UI/export manual |
| Component Usage Explorer | [plan](ComponentUsageExplorer/TEST_PLAN.md) | [cases](ComponentUsageExplorer/TEST_CASES.md) | [summary](ComponentUsageExplorer/TEST_SUMMARY.md) | 13/13 passed (usage footprint + change-safety verdict); collector/UI/export manual |
| Environment Comparison Suite | [plan](EnvironmentComparisonSuite/TEST_PLAN.md) | [cases](EnvironmentComparisonSuite/TEST_CASES.md) | [summary](EnvironmentComparisonSuite/TEST_SUMMARY.md) | 14/14 passed (snapshot diff engine + roll-up); dual-connection/collector/UI/export manual |
| Solution Documentation Generator | [plan](SolutionDocumentationGenerator/TEST_PLAN.md) | [cases](SolutionDocumentationGenerator/TEST_CASES.md) | [summary](SolutionDocumentationGenerator/TEST_SUMMARY.md) | 10/10 passed (document model + template engine + renderers); collector/Word/PDF/Excel/UI manual |
| Duplicate Metadata Finder | [plan](DuplicateMetadataFinder/TEST_PLAN.md) | [cases](DuplicateMetadataFinder/TEST_CASES.md) | [summary](DuplicateMetadataFinder/TEST_SUMMARY.md) | 22/22 passed (similarity primitives + scoring/grouping + report projection); tool-load smoke Pass; collector/UI/export manual |
| Custom API Explorer | [plan](CustomApiExplorer/TEST_PLAN.md) | [cases](CustomApiExplorer/TEST_CASES.md) | [summary](CustomApiExplorer/TEST_SUMMARY.md) | 20/20 passed (value parsing + request binding + snippet + catalog exporters); tool-load smoke Pass; collector/gated-invoke/UI manual |
