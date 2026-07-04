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
    XrmToolSuite.AttributeAuditor/ # scaffolded tool (audit unused attributes) — WIP
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
- your DLL must sit under a `Plugins` folder in the package; ship only your DLL
- `iconUrl` is required and must be your own icon

Pack with: `nuget pack src/Tools/XrmToolSuite.MyTool/XrmToolSuite.MyTool.nuspec`

## Tools in this suite

| Tool | Purpose |
|---|---|
| **Deployment Risk Analyzer** | Analyzes a solution before deployment: dependencies, env vars/connection refs, flows/plugins, security coverage, schema conflicts vs a target env, Power Pages readiness — risk-scored report exportable as a native PDF, an HTML dashboard, Excel, CI-JSON, or Markdown |
| Attribute Auditor | Audit unused attributes across entities — scaffolded from the template; logic not yet implemented (WIP) |
| Template Tool | Clone source for new tools (not published) |

Deployment Risk Analyzer fully follows suite conventions (`XrmToolSuite.DeploymentRiskAnalyzer` namespace, `BaseToolControl`, shared `RetrieveAll` paging, `Load/SaveSettings`). Its one specialty: it ships two extra dependency chains (the ClosedXML chain for Excel export and the PdfSharp/MigraDoc GDI chain for native PDF export) in its nupkg — see its csproj and nuspec for the pattern to follow when a tool needs third-party libraries.

## Backlog & user stories

The product backlog — Portfolio and Platform epics plus one Epic/Features/User-Stories file per tool — lives in [`docs/user-stories/`](docs/user-stories/README.md). Each tool folder also has a `DEPLOYMENT.md` build/install guide; `New-Tool.ps1` stamps both a deployment guide and a starter user-story file for every new tool.

## Notes

- Targets .NET Framework 4.8 (current XrmToolBox requirement) with SDK-style projects — builds with VS 2022 or `dotnet build` on Windows.
- Icons: every tool **must** export `SmallImageBase64`/`BigImageBase64` in its `*Plugin.cs` ExportMetadata — XrmToolBox marks both required and MEF silently drops any tool missing them. Reference the shared defaults (`PluginIcons.Small`/`PluginIcons.Big`) rather than inlining base64.
