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
  ReportTests/                  # executable xUnit project (net48) for the report exporters
    ReportTests.csproj          # compiles ReportExporters.cs with ClosedXML + PdfSharp/MigraDoc
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

- **Automated - report exporters (`ReportTests/`, net48):** compiles `ReportExporters.cs` directly with
  the tool's export dependencies (ClosedXML, PdfSharp/MigraDoc-GDI, Newtonsoft) and asserts each exporter
  produces a well-formed payload — including that the native PDF renders a valid `%PDF-` document. Kept
  separate from `AnalyzerTests/` so those deps don't bloat the analyzer suite.

  ```powershell
  dotnet test testing/ReportTests/ReportTests.csproj
  ```

- **Manual (needs a live Dataverse connection + XrmToolBox host):** anything that drives the UI
  (loading solutions, connecting, exporting reports) or depends on real metadata. These are documented
  as numbered cases with steps/expected results; evidence is captured as screenshots under the tool's
  `screenshots/` folder. They cannot be executed in a headless environment.

## Index

| Tool | Plan | Cases | Summary | Automated status |
|---|---|---|---|---|
| Deployment Risk Analyzer | [plan](DeploymentRiskAnalyzer/TEST_PLAN.md) | [cases](DeploymentRiskAnalyzer/TEST_CASES.md) | [summary](DeploymentRiskAnalyzer/TEST_SUMMARY.md) | 46/46 passed (17 scoring + 29 analyzer) |
| Attribute Auditor | [plan](AttributeAuditor/TEST_PLAN.md) | [cases](AttributeAuditor/TEST_CASES.md) | [summary](AttributeAuditor/TEST_SUMMARY.md) | n/a (WIP - no logic yet) |
