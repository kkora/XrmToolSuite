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
  AnalyzerTests/                # executable xUnit project (net48) for UI-free analyzer logic
    AnalyzerTests.csproj        # uses the Dataverse SDK + an in-memory fake IOrganizationService
    Fakes/FakeOrganizationService.cs
    *Tests.cs
  ReportTests/                  # executable xUnit project (net48) for the shared report exporters
    ReportTests.csproj          # compiles src/Shared/Reporting/*.cs with ClosedXML + PdfSharp/MigraDoc
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

  Metadata-level branches that need constructed `EntityMetadata` (attribute type/length, option sets,
  relationships) stay in the manual suite.

- **Automated - report exporters (`ReportTests/`, net48):** compiles the shared exporters under
  `src/Shared/Reporting/` directly (driven through the `DeploymentReportModel` projection) with the
  tool's export dependencies (ClosedXML, PdfSharp/MigraDoc-GDI, Newtonsoft) and asserts each exporter
  produces a well-formed payload — including that the native PDF renders a valid `%PDF-` document. Kept
  separate from `AnalyzerTests/` so those deps don't bloat the analyzer suite.

  ```powershell
  dotnet test testing/ReportTests/ReportTests.csproj
  ```

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
| Deployment Risk Analyzer | [plan](DeploymentRiskAnalyzer/TEST_PLAN.md) | [cases](DeploymentRiskAnalyzer/TEST_CASES.md) | [summary](DeploymentRiskAnalyzer/TEST_SUMMARY.md) | 79/79 passed (24 scoring + 49 analyzer + 6 report) |
| Technical Debt Analyzer | [plan](TechnicalDebtAnalyzer/TEST_PLAN.md) | [cases](TechnicalDebtAnalyzer/TEST_CASES.md) | [summary](TechnicalDebtAnalyzer/TEST_SUMMARY.md) | 5/5 passed (debt scoring + metrics); analyzers/UI manual |
| Solution Complexity Score | [plan](SolutionComplexityScore/TEST_PLAN.md) | [cases](SolutionComplexityScore/TEST_CASES.md) | [summary](SolutionComplexityScore/TEST_SUMMARY.md) | 6/6 passed (metric/effort model + report); collector/UI manual |
| AI Solution Reviewer | [plan](AiSolutionReviewer/TEST_PLAN.md) | [cases](AiSolutionReviewer/TEST_CASES.md) | [summary](AiSolutionReviewer/TEST_SUMMARY.md) | 4/4 passed (report/concern score); collectors/AI/Word manual |
| Solution Knowledge Graph | [plan](SolutionKnowledgeGraph/TEST_PLAN.md) | [cases](SolutionKnowledgeGraph/TEST_CASES.md) | [summary](SolutionKnowledgeGraph/TEST_SUMMARY.md) | 9/9 passed (model/algorithms/GraphML/SVG/HTML); builder/PNG/UI manual |
| Attribute Auditor | [plan](AttributeAuditor/TEST_PLAN.md) | [cases](AttributeAuditor/TEST_CASES.md) | [summary](AttributeAuditor/TEST_SUMMARY.md) | n/a (WIP - no logic yet) |
