# XrmToolSuite

A multi-tool solution for building XrmToolBox tools fast. One shared core, one template, and a script that stamps out a new tool project in seconds — built for shipping many tools from a single repo.

## Layout

```
XrmToolSuite.sln
Directory.Build.props            # global version/author metadata (bump Version per release)
src/
  Shared/Core/                   # shared source, compiled INTO every tool (no extra DLL)
    BaseToolControl.cs           # PluginControlBase wrapper: RunAsync, status bar, settings, errors
    QueryExtensions.cs           # RetrieveAll with automatic paging + cancellation
    BatchExecutor.cs             # ExecuteMultiple chunking with progress + fault collection
    MetadataCache.cs             # cached RetrieveEntityRequest per connection
    PluginIcons.cs               # shared base64 tool icons (Small/Big) — REQUIRED ExportMetadata
  Tools/
    Directory.Build.props         # net48 + WinForms + XrmToolBoxPackage + shared-source glob
    XrmToolSuite.TemplateTool/     # the template every new tool is cloned from
    XrmToolSuite.DeploymentRiskAnalyzer/  # pre-deployment risk analysis tool (see its own README)
    XrmToolSuite.AttributeAuditor/ # audit unused custom columns (usage detection + CSV/HTML export)
scripts/
  New-Tool.ps1                   # stamp out a new tool project
```

## Create a new tool

```powershell
./scripts/New-Tool.ps1 -Name "SolutionCompare" `
    -DisplayName "Solution Compare" `
    -Description "Compares components between two solutions"
```

This clones the template, renames everything, and adds it to the solution. A tool's csproj is ~8 lines; all build config lives in `src/Tools/Directory.Build.props`.

## Develop & debug

1. Build with local deploy: `dotnet build -p:DeployToXTB=true` — copies the DLL to `%AppData%\MscrmTools\XrmToolBox\Plugins`.
2. In VS, set the project's debug launch to your `XrmToolBox.exe` and F5.
3. Key patterns already wired in `BaseToolControl`:
   - `ExecuteMethod(MyMethod)` — ensures a Dataverse connection before running
   - `RunAsync("msg", worker => ..., result => ...)` — background work + spinner
   - `LoadSettings<T>()` / `SaveSettings(T)` — per-tool persisted settings
   - `Service.RetrieveAll(query, count => worker.ReportProgress(0, $"{count} rows..."))`
   - `BatchExecutor.Execute(Service, requests, onProgress: (done, total) => ...)`

## Why shared *source* instead of a shared DLL

XrmToolBox loads every tool's DLLs into one process. If two of your tools shipped different versions of `XrmToolSuite.Core.dll`, whichever loaded first would win — a classic XTB versioning trap. Compiling the shared code into each tool assembly (via the `Compile` glob in `src/Tools/Directory.Build.props`) removes that entire failure mode and keeps each nupkg to a single DLL.

## Publish to the Tool Library

Each tool has a `.nuspec`. Rules enforced by the Tool Library (see xrmtoolbox.com developer docs):
- nupkg `version` must equal your DLL's assembly version (both flow from `Version` in the root `Directory.Build.props`)
- `tags` must start with `XrmToolBox` plus extra words
- dependency must be on **`XrmToolBox`** (not `XrmToolBoxPackage`)
- your DLL must sit under a `Plugins` folder in the package; ship only this suite's files (single DLL for most tools; the tool DLL **plus its 17-DLL ClosedXML/PdfSharp-MigraDoc-GDI chain** for the Excel/PDF/Word export tools)
- `iconUrl` is required and must be your own icon

Pack with: `nuget pack src/Tools/XrmToolSuite.MyTool/XrmToolSuite.MyTool.nuspec`

**Full publishing flow (package → push to nuget.org → Tool Library), manual and CI:** see
[`Publishing_Guide_XrmToolBox.md`](Publishing_Guide_XrmToolBox.md). Automated releases run via
[`.github/workflows/publish.yml`](.github/workflows/publish.yml) — tag `v<version>` (or a manual
dry run) builds, packs every tool except the template, and pushes via **NuGet Trusted Publishing**
(OIDC — no API-key secret).
For **local** install (build → copy DLL → unblock → launch), see
[`Deployment_Guide_XrmToolBox.md`](Deployment_Guide_XrmToolBox.md).

## Tools in this suite

| Tool | Purpose |
|---|---|
| **Deployment Risk Analyzer** | Analyzes a solution before deployment: dependencies, env vars/connection refs, flows/plugins, security coverage, schema conflicts vs a target env, Power Pages readiness — risk-scored report exportable as a native PDF, an HTML dashboard, Excel, CI-JSON, or Markdown |
| **Technical Debt Analyzer** | Scans a whole environment for a 0–100 technical-debt score with prioritized cleanup findings (unused metadata, duplicates, deprecated APIs, orphaned/dead components, performance, naming, security); exports PDF/HTML/Excel/JSON/Markdown |
| **Solution Complexity Score** | Inventories a solution and scores complexity + maintainability, with upgrade/migration/testing effort and annual support-cost estimates and an executive dashboard |
| **AI Solution Reviewer** | AI-assisted architecture review (recommendations, modernization, prioritized backlog, sprint plan) over collected solution facts, with an offline deterministic fallback; exports Word/PDF/HTML/Markdown/JSON |
| **Solution Knowledge Graph** | Interactive dependency graph with search, dependency tracing, deletion-impact analysis, and circular-dependency detection; exports interactive HTML, GraphML, SVG, and PNG |
| **Attribute Auditor** | Finds unused custom columns by detecting usage across forms, views, processes, and field security; classifies retirement candidates and exports CSV + an HTML dashboard (chart/data-population signals and guarded cleanup planned) |
| **FetchXML Performance Analyzer** | Parses any FetchXML (pasted or loaded from a system/user view) and flags performance risks (all-attributes, missing filters, excessive/outer joins, risky sorts, aggregation/paging) with a heuristic cost estimate, suggested fixes, and optional opt-in live timing; exports Excel/PDF/JSON/HTML/Markdown/CSV. Its UI-free parser + rule engine lives in `src/Shared/Core/FetchXml` for reuse by the View/Dashboard analyzers |
| **Environment Inventory** | Collects a normalized, searchable inventory of an environment (solutions, tables, security, automation, web/dev components, configuration) with per-component detail and Excel/Word/PDF/CSV/JSON/Markdown/HTML export (Excel carries the full item grid; Word/PDF a summary); never reads or exports secrets. The SDK-free normalization model is the data backbone for future ERD/Docs/Drift tools |
| **Privilege Gap Analyzer** | Answers "why can't this user do X?": computes a principal's effective privileges (direct + team-inherited, deepest scope) for a table + operation and returns a verdict (allowed / missing privilege / insufficient scope / append mismatch / team-inheritance) with a read-only recommended fix; compares two principals; exports Excel/PDF/JSON/CSV/HTML. UI-free engine is console/CI-liftable |
| **View Performance Analyzer** | Batch-analyzes every system and personal view's FetchXML (via the shared `src/Shared/Core/FetchXml` engine) plus its LayoutXML column count, scores and ranks the slowest/riskiest views, with per-view detail, optional opt-in execution timing, and Excel/PDF/JSON/HTML/Markdown/CSV export. First consumer of the Phase-A FetchXML engine |
| **Team Permission Explorer** | Shows what every team can access and which users inherit it — members, roles, an effective table-privilege matrix (via the shared `Core.Privileges` engine), owned-record counts — and flags empty, over-privileged, duplicate-role, and orphaned teams; compares two teams; exports Excel/PDF/CSV/HTML |
| **ERD Generator** | Generates Dataverse entity-relationship diagrams from live metadata (tables, keys, columns, relationships, cascade behavior, custom/managed status) and exports **Mermaid `erDiagram`**, PlantUML, SVG, PNG, PDF, HTML, Markdown, and structured JSON. UI-free ERD model + emitters |
| **JavaScript Performance Analyzer** | Statically scans JS web resources (offline) for performance/deprecation risks — `Xrm.Page`, synchronous XHR, blocking alerts, repeated retrieves, hardcoded GUIDs/URLs, DOM manipulation, size — with per-finding line context + confidence, maps scripts to forms/OnLoad events, scores each script, and exports Excel/PDF/JSON/HTML/Markdown/CSV |
| **Form Performance Analyzer** | Scores model-driven main forms by static FormXML "heaviness" (tabs, sections, fields, PCF/custom controls, subgrids, quick views, script libraries, handlers, business rules) into Light/Moderate/Heavy/Critical bands with optimization recommendations; compares two forms; exports CSV/HTML (single-DLL) |
| **Sharing Analyzer** | Scans `PrincipalObjectAccess` (record-level sharing) scoped by table, decodes access-rights masks, and flags excessive sharing, shares with inactive users / disabled teams, and inbound access sprawl — with a table×principal intensity view and preview-only cleanup recommendations; exports Excel/PDF/JSON/HTML/CSV |
| **Audit Compliance Checker** | Checks org/table/column audit settings (with sensitive-data heuristics), analyzes audit activity (deletes, security changes, after-hours) and an estimated storage-growth trend, and produces a 0–100 audit-compliance readiness score with prioritized, read-only remediation; exports Excel/PDF/JSON/HTML/CSV |
| **Managed Solution Impact Checker** | Analyzes a managed solution's layering and the impact of importing/upgrading/patching/deleting it — unmanaged-above-managed overrides, overwrite/deletion (data-loss) risk, path-aware Upgrade-deletes-vs-Patch-doesn't, missing dependencies, publisher and managed-property restrictions — into an impact score, pre-upgrade checklist, and rollback guidance; exports Excel/PDF/JSON/HTML (CAB-ready) |
| **Portal Health Analyzer** | Inventories a Power Pages website across the dual `adx_`/`mspp_` schemas and scores portal health — broken page/template relationships, dead web files, missing/duplicate site settings, broken data bindings, anonymous-access and over-broad-permission risks — with a categorized issue grid and recommendations; exports Excel/PDF/Word/JSON/HTML/CSV |
| **Solution Merge Assistant** | Compares two or more solutions from one environment and finds every conflict before a merge — duplicate/overlapping components, version/publisher mismatches, managed-vs-unmanaged, and env-var/connection-reference conflicts — rolling up to a verdict (Safe → Do-not-merge) with a recommended merge strategy and checklist; exports Excel/PDF/JSON/HTML |
| **Flow Dependency Analyzer** | Statically maps every cloud flow's dependencies from its `clientdata` — Dataverse trigger/tables/columns, connectors, connection references, environment variables, child flows, custom APIs, HTTP actions — with a reverse "which flows break if I change this component" view and a deployment-readiness checklist; secrets/URLs are always redacted; exports Excel/PDF/JSON/HTML |
| **Plugin Dependency Graph** | Builds a dependency graph of the plugin pipeline (assembly → type → step → image → message/table → custom API → solution/config) with high-impact-assembly, duplicate-step, and unmanaged-registration detection; secure config never exposed; exports PNG/SVG/PDF/Excel/JSON/GraphML/HTML |
| **Component Usage Explorer** | Pick one Dataverse component and see its full where-used footprint (required + dependent components, per-type usage) plus a change-safety verdict (Safe to change → Do not delete); read-only; exports Excel/PDF/JSON/HTML |
| **Environment Comparison Suite** | Compares a source and target environment (dual-connection) across every component class — solutions, schema, forms/views, security roles (by privilege set), plugins, workflows/flows, env vars, connection refs, web resources — classifying each Missing/Extra/Changed/Managed-vs-Unmanaged with a difference score, side-by-side before/after viewer, and Excel/PDF/JSON/HTML export; secrets masked |
| **Solution Documentation Generator** | Scans a solution into a multi-section technical/business document (inventory, schema, forms/views/apps, automation, plugins, web resources, custom APIs, config, roles, ERD diagram, release notes, architecture summary) at a chosen depth, exported to Word/PDF/Markdown/HTML/Excel/JSON |
| Template Tool | Clone source for new tools (not published) |

All twenty-four shipping tools follow the suite conventions (`XrmToolSuite.<Tool>` namespace, `BaseToolControl`, shared `RetrieveAll` paging, `Load/SaveSettings`). The scoring/report family shares one engine compiled in from **`src/Shared/`**: `Core/Analysis` (findings, `ScoreCalculator`, `ReportModel`), `Reporting` (JSON/Markdown/HTML/Excel/PDF/Word exporters), and `Summarization` (offline + AI executive summaries). Tools that export to Excel/PDF/Word ship the ClosedXML and PdfSharp/MigraDoc-GDI dependency chains in their nupkg (the Word exporter reuses the OpenXML assembly from the ClosedXML chain) — see any of their csproj/nuspec files for the pattern to follow when a tool needs third-party libraries. The Solution Knowledge Graph ships only its own DLL (its GraphML/SVG/HTML output is pure string; PNG uses the net48 System.Drawing GAC assembly).

## Backlog & user stories

The product backlog — Portfolio and Platform epics plus one Epic/Features/User-Stories file per tool — lives in [`docs/user-stories/`](docs/user-stories/README.md). Each tool folder also has a `DEPLOYMENT.md` build/install guide; `New-Tool.ps1` stamps both a deployment guide and a starter user-story file for every new tool.

## Notes

- Targets .NET Framework 4.8 (current XrmToolBox requirement) with SDK-style projects — builds with VS 2022 or `dotnet build` on Windows.
- Icons: every tool **must** export `SmallImageBase64`/`BigImageBase64` in its `*Plugin.cs` ExportMetadata — XrmToolBox marks both required and MEF silently drops any tool missing them. Reference the shared defaults (`PluginIcons.Small`/`PluginIcons.Big`) rather than inlining base64.
