# CLAUDE.md — XrmToolSuite

Guidance for Claude Code when working in this repository. This repo produces **XrmToolBox tools** (Windows Forms plugins for Dynamics 365 / Dataverse admin tooling), all sharing one core.

## What this repo is

- `src/Shared/Core/` — shared source compiled **into every tool assembly** (deliberate: XrmToolBox loads all tools into one process, so a shared DLL would cause version conflicts). Never turn this into a separate class library project.
- `src/Tools/XrmToolSuite.<Name>/` — one project per tool. Keep each csproj minimal (~8 lines); all build config belongs in `src/Tools/Directory.Build.props`.
- `src/Tools/XrmToolSuite.TemplateTool/` — the canonical template. Never add feature-specific code here; improvements to the template must stay generic.
- Root `Directory.Build.props` — single `Version` for every tool. Bump it per release; the Tool Library requires assembly version == nupkg version.

## Commands

```powershell
# New tool (always use the script; never hand-copy a project)
./scripts/New-Tool.ps1 -Name "MyTool" -DisplayName "My Tool" -Description "..."

# Build (Windows only — net48 + WinForms)
dotnet build XrmToolSuite.sln -c Release

# Build + deploy DLL to local XrmToolBox for manual testing
dotnet build -p:DeployToXTB=true

# Package one tool for the Tool Library
nuget pack src/Tools/XrmToolSuite.MyTool/XrmToolSuite.MyTool.nuspec

# Run automated tests (SDK-free logic; net8, no Dataverse/XrmToolBox needed)
dotnet test testing/UnitTests/UnitTests.csproj
```

## Tool lifecycle & testing (MANDATORY for every new/updated tool)

Whenever a tool is **created or changed**, produce all of the following, in order — no tool is "done" until they exist:

1. **Plan** — user stories in `docs/user-stories/<TAG>NN.<Tool>.md` and a `testing/Tools/<Tool>/TEST_PLAN.md`.
   **Backlog & user-story file naming (keep the two in lock-step):** a tool's **backlog** file
   (`docs/backlog/<category>/`) and its **user-story** file (`docs/user-stories/`) share the **same**
   `<TAG>NN.<Tool>.md` name, where `<TAG>` is the category code and `NN` is the **two-digit, zero-padded**
   item number within that category (`01`, `02`, … `10`) — e.g. `SEC04.SharingAnalyzer.md`,
   `ADMIN03.DuplicateMetadataFinder.md`. The tool's EPIC/FEAT/US ids use the **same** `<TAG>NN` as their area
   (`EPIC-SEC04`, `FEAT-SEC04-1`, `US-SEC04-…`). **Every** tool follows this — the six that shipped first take
   the next number in their category (`ALM07` Deployment Risk Analyzer, `ADMIN10` Attribute Auditor, `SOLN08`
   Solution Complexity Score, `SOLN09` Solution Knowledge Graph, `SOLN10` Technical Debt Analyzer, `AI10` AI
   Solution Reviewer). The `TemplateTool.md` scaffold is exempt. `New-Tool.ps1` stamps an **untagged**
   `docs/backlog/<Name>.md` at the backlog root — rename it to `<TAG>NN.<Name>.md`, move it under the correct
   `docs/backlog/<category>/` folder, and give the user-story file the matching name. When you add or rename
   either file, keep in sync: the `docs/backlog/README.md` category index, the `docs/user-stories/README.md`
   index, the user-story "Source spec" backlink, the `testing/Tools/<Tool>/TEST_PLAN.md` "Traces to …" backlink,
   and the tool's EPIC/FEAT/US ids.
2. **Tool** — the implementation under `src/Tools/`.
3. **Test cases** — `testing/Tools/<Tool>/TEST_CASES.md`, each case traced to a user story.
4. **Execute + screenshots** — run `dotnet test`, perform the manual/GUI cases, and save evidence (screenshots) under `testing/Tools/<Tool>/screenshots/`. **A "tool loads in XrmToolBox" screenshot is REQUIRED for every tool**, saved as `testing/Tools/<Tool>/screenshots/xrmtoolbox-tools-list.png` — the XrmToolBox **Tools** tab filtered to that tool, showing its name/version/description (proof of MEF registration; note the tool lists even before its export deps resolve, since dep types are method-body locals only). The `testing/UiSmokeTests` FlaUI harness generates all of these automatically: deploy with `dotnet build XrmToolSuite.sln -c Release -p:DeployToXTB=true`, then run `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set — it asserts every tool in its `ExpectedTools` list appears and captures a per-tool `tool_<slug>.png` under its `screenshots/`. When adding a tool, add its exact `ExportMetadata("Name", …)` to `ExpectedTools`, run the harness, and copy its shot into `testing/Tools/<Tool>/screenshots/xrmtoolbox-tools-list.png`.
5. **Summary** — results and verdict in `testing/Tools/<Tool>/TEST_SUMMARY.md`.

All of this lives in the root **`testing/`** folder (see `testing/README.md`). `New-Tool.ps1` stamps the `testing/Tools/<Tool>/` skeleton (plan/cases/summary + `screenshots/`) and the user-story file automatically; keep them updated as the tool changes.

- **Automated tests** cover **SDK-free logic only** (risk scoring, banding, pure helpers) and live in `testing/UnitTests/` (net8.0). This project is **deliberately not in `XrmToolSuite.sln`** (the tools are net48/WinForms) — it compiles the specific source files under test directly, so it runs with the plain .NET SDK and needs no Dataverse SDK, net48 pack, or connection. Run it with `dotnet test testing/UnitTests/UnitTests.csproj`.
- **Manual tests** cover anything needing a live Dataverse connection or the WinForms host (analyzers, UI, exports). They are documented cases executed in a Windows + XrmToolBox session against a real org; capture a screenshot per case. They **cannot** run headlessly — say so in the summary rather than claiming a pass.

## Non-negotiable XrmToolBox patterns

1. **Never call Dataverse on the UI thread.** All `IOrganizationService` calls go through `RunAsync(...)` (wraps `WorkAsync`). UI updates happen only in the `onCompleted` callback, which runs on the UI thread.
2. **Never assume a connection exists.** Any button handler that needs Dataverse must call `ExecuteMethod(MyMethod)`; XrmToolBox will prompt for a connection if needed. Don't call `Service` directly from event handlers.
3. **MEF registration is required per tool.** Each tool has exactly one class deriving from `PluginBase` with `[Export(typeof(IXrmToolBoxPlugin))]` and `ExportMetadata("Name", ...)` / `("Description", ...)`. Name must be unique across the suite.
4. **Handle `UpdateConnection`.** When overriding it, call `base.UpdateConnection(...)` first and clear caches (`MetadataCache.Clear()`), since environments differ.
5. **Settings** persist via `LoadSettings<T>()` in the `Load` event and `SaveSettings(...)` in `ClosingPlugin`. Settings classes must be plain serializable POCOs — no controls, services, or connection details in them (never persist credentials).
6. **Paging and batching are already solved.** Use `Service.RetrieveAll(query, ...)` instead of writing paging loops, and `BatchExecutor.Execute(...)` instead of raw `ExecuteMultipleRequest`. Support cancellation by passing the `BackgroundWorker` through.
7. **Long operations must report progress** — `worker.ReportProgress(0, "message")` updates the spinner; `SetStatusProgress(pct, msg)` updates the status bar.
8. **Destructive operations (delete, bulk update, publish) require an explicit confirmation dialog** stating scope and record count before executing.
9. **Every tool must surface a Help button**, like the Deployment Risk Analyzer. Each control implements `IHelpPlugin` (+ `IGitHubPlugin`) and puts a right-aligned `Help` button on its toolbar that opens a Help & Support dialog (Documentation, Report an issue, and a support link, each opened via `Process.Start`). Use the shared `BaseToolControl.CreateHelpButton()` / `ShowHelpDialog(...)` helper (suite links point at `kkora/XrmToolSuite`) rather than hand-rolling a dialog per tool — the DRA's bespoke dialog predates the helper and is the visual reference. Never ship the scaffold's placeholder `your-github-username` links.

## Coding conventions

- Target `net48`, C# latest via `LangVersion`. No `Span<T>`/NET-Core-only APIs.
- WinForms: keep generated layout in `*.Designer.cs`, logic in the main partial class. Control naming: `tsb*` (toolstrip buttons), `lv*` (list views), `txt*`, `cbo*`, `grd*`.
- Namespaces: `XrmToolSuite.<ToolName>`; shared code stays in `XrmToolSuite.Core`.
- New reusable logic (used by 2+ tools) goes in `src/Shared/Core/` as plain static helpers or extension methods with no UI dependencies where possible.
- Do not add NuGet dependencies to individual tools without strong reason. If unavoidable: the DLL must be shipped inside the nupkg as a file under `Plugins` (never as a NuGet dependency), and check it doesn't collide with a DLL XrmToolBox already ships (Newtonsoft.Json, ScintillaNET, DockPanelSuite, etc. are already present — reference, don't ship).
- **Backlog-required Excel/PDF/Word export is a sanctioned reason to add dependencies.** When a tool's user stories call for Excel, PDF, or Word export, implement it — do not defer it — by wiring through the shared `src/Shared/Reporting` exporters (`ExcelReportExporter`/`PdfReportExporter`/`WordReportExporter`, all keyed to `ReportModel`) and shipping the ClosedXML + PdfSharp/MigraDoc-GDI chains exactly as the Deployment Risk Analyzer does (see the **Excel/PDF/Word export pattern** note under Tool-specific notes). Lightweight formats (JSON/HTML/Markdown/CSV) stay BCL/Newtonsoft-only and never need these chains.
- Never ship Microsoft SDK assemblies (`Microsoft.Xrm.*`, `Microsoft.Crm.*`) in a nupkg.

## Packaging rules (Tool Library will reject violations)

- nupkg `version` == assembly version (both derive from root `Directory.Build.props`)
- `tags` starts with `XrmToolBox` plus additional words
- dependency is on **`XrmToolBox`** — never `XrmToolBoxPackage`
- tool DLL sits under `lib\net48\Plugins`; package contains only this suite's files
- `iconUrl` set to our own hosted icon

## Verification checklist before declaring work done

Builds cannot run in non-Windows environments; when a build isn't possible, say so explicitly. Otherwise:

1. `dotnet build XrmToolSuite.sln -c Release` succeeds with zero warnings introduced.
2. Every Dataverse call is inside `RunAsync`/`WorkAsync` — grep for `Service.` usages in event handlers.
3. New tool appears via `New-Tool.ps1` output and is present in `XrmToolSuite.sln`.
4. ExportMetadata Name/Description updated (no "Template Tool" leftovers): `grep -ri "template" src/Tools/XrmToolSuite.<Name>/`.
5. Settings round-trip: load in `*_Load`, save in `ClosingPlugin`.
6. Nuspec id/version/description match the tool.
7. Testing artifacts exist and are current under `testing/Tools/<Tool>/` (TEST_PLAN, TEST_CASES, TEST_SUMMARY, screenshots — **including the required `xrmtoolbox-tools-list.png` load screenshot**), the tool is listed in `testing/UiSmokeTests` `ExpectedTools`, user stories under `docs/user-stories/<TAG>N.<Tool>.md` (tagged naming — see the Tool lifecycle section), and `dotnet test testing/UnitTests/UnitTests.csproj` passes. State plainly which cases were executed vs. pending-manual — never report a manual GUI case as passed if it wasn't run.

## Tool-specific notes

- **Every plugin MUST export `SmallImageBase64` and `BigImageBase64` metadata.** XrmToolBox's `IPluginMetadata` marks both as required (no default); MEF silently drops any plugin missing a required metadata key from the Tools list — no error, no "Tools not loaded" entry, the tool just never appears even though the assembly loads. Suite defaults live in `src/Shared/Core/PluginIcons.cs` (compile-time base64 PNG constants); reference them as `[ExportMetadata("SmallImageBase64", PluginIcons.Small)]` / `PluginIcons.Big`. Never remove the keys.
- **Excel/PDF/Word export pattern (reusable — not a one-off).** The Deployment Risk Analyzer is the **reference implementation** of the suite's Office/PDF export-dependency pattern; **any tool whose backlog requires Excel, PDF, or Word ships the same two chains the same way.** Most of the suite's export tools now follow it — the Phase-A batch (`FetchXmlPerformanceAnalyzer`, `EnvironmentInventory`, `PrivilegeGapAnalyzer`) through the Phase-B/C export tools and `DuplicateMetadataFinder` (see the dependency-category table in `Deployment_Guide_XrmToolBox.md` for the current list; `ErdGenerator` ships the PDF-only subset). To add the pattern to a tool: (1) csproj sets `CopyLocalLockFileAssemblies=true`, compiles `..\..\Shared\Reporting\**\*.cs`, references `ClosedXML` 0.102.3 + `PDFsharp-MigraDoc-GDI` 1.50.5147 + `Newtonsoft.Json` 13.0.3, and adds the `DeployGuardDependencies` target; (2) the `*Plugin.cs` static ctor registers the scoped `AppDomain.AssemblyResolve` handler (whitelist + `[ThreadStatic] _resolving` guard, probing the per-tool subfolder then root) copied from `DeploymentRiskAnalyzerPlugin`; (3) the nuspec `<files>` ships the tool DLL in the Plugins root and all its dep DLLs into a **per-tool subfolder** `lib\net48\Plugins\<AssemblyName>` (the XrmToolBox store convention — cf. `Ryr.ExcelExport`, `Maverick.PCF.Builder`; isolates each tool's dep versions), and the csproj `DeployGuardDependencies` target deploys them to that same `Plugins\$(AssemblyName)` subfolder for local `DeployToXTB` testing; (4) export calls go through the shared `ReportModel` exporters (or, for tabular data like the inventory, a tool-local ClosedXML exporter with ClosedXML types as **method-body locals only** — never in a signature). Keep the nuspec `<files>`, the csproj `DeployGuardDependencies` list, and the handler's `OwnedDependencies` set **in sync within each tool** (the three must agree on that tool's dep set + subfolder). Per-tool subfolders now isolate versions, so tools no longer need byte-identical dep versions across the suite. All the load-bearing details (per-tool-subfolder layout, `-gdi` suffixes, SixLabors.Fonts facade mismatch, re-entrancy guard) are spelled out in the DRA note below.
- **XrmToolSuite.DeploymentRiskAnalyzer** follows all suite conventions (namespace, `BaseToolControl`, `RunAsync`, `Load/SaveSettings`, shared `RetrieveAll`). It is the reference implementation of the export-dependency pattern above and ships two export dependency chains XrmToolBox's built-in libraries don't cover — (1) the ClosedXML Excel chain (ClosedXML + DocumentFormat.OpenXml + ExcelNumberFormat + XLParser + Irony + SixLabors.Fonts + `System.IO.Packaging` + the facades `System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe`, `System.Buffers`, `System.Memory`, `System.Threading.Tasks.Extensions`) and (2) the native-PDF chain (PdfSharp/MigraDoc **GDI build**, whose assemblies carry a `-gdi` suffix: `PdfSharp-gdi`, `PdfSharp.Charting-gdi`, `MigraDoc.DocumentObjectModel-gdi`, `MigraDoc.Rendering-gdi`, `MigraDoc.RtfRendering-gdi` — GDI, not WPF/SkiaSharp, so it stays pure-managed on net48 and resolves fonts via GDI+). Its csproj sets `CopyLocalLockFileAssemblies=true` — do not "clean up" either. **These deps ship in a per-tool subfolder `lib\net48\Plugins\XrmToolSuite.DeploymentRiskAnalyzer\`; the tool DLL itself stays in the Plugins root.** This is the XrmToolBox store convention (cf. `Ryr.ExcelExport`, `Maverick.PCF.Builder`) and isolates this tool's dep versions from every other tool in the shared AppDomain. It is safe for the Tools-list scan because the tool keeps `ClosedXML`/`PdfSharp`/`MigraDoc` types out of all signatures (method-body locals only), so MEF composition never loads them while reading the plugin metadata — the tool lists whether or not the deps are on the probe path (verified 2026-07-09: hiding the deps drops nothing from the Tools list). The deps are needed only at runtime (export), which the `AssemblyResolve` handler satisfies by probing the per-tool subfolder first, then the Plugins root as a fallback (runtime export from the subfolder is verified working). *Historical note: an earlier revision shipped these in the Plugins ROOT and this doc claimed a subfolder would silently drop the tool at scan time — that was tested false; both the list scan and runtime export work from the subfolder, matching how store tools ship.* SixLabors.Fonts 1.0.0 is mispackaged (compiled against `System.Numerics.Vectors` 4.1.3.0 / `System.Memory` 4.0.1.1 but its NuGet deps demand higher = asm 4.1.4.0 / 4.0.1.2); current XrmToolBox ships those facades in its app dir with binding redirects covering the range, so the mismatch resolves at runtime. `DeploymentRiskAnalyzerPlugin`'s static ctor still registers an `AppDomain.AssemblyResolve` handler as a fallback for hosts whose redirects don't cover it — it is **scoped to an `OwnedDependencies` whitelist** (never touches other tools' assemblies) and **must** keep the re-entrancy guard (`[ThreadStatic] _resolving`) or a dependency cycle StackOverflows. The tool keeps ClosedXML and MigraDoc/PdfSharp types out of all type signatures (method-body locals only). Keep the nuspec `<files>` targets, the csproj `DeployGuardDependencies` target, and the handler's `OwnedDependencies` set in sync — for **both** chains (the PDF assemblies' `OwnedDependencies` / file names use the `-gdi` suffix). Newtonsoft.Json is compiled against but never shipped (XrmToolBox provides it). It also demonstrates the dual-connection pattern: `RaiseRequestConnectionEvent` with `actionName="TargetOrganization"`, handled in `UpdateConnection` without replacing the primary connection.
- Its analyzers (`Analyzers/*.cs`) depend only on `IOrganizationService` and must stay UI-free so they remain liftable into a console/CI wrapper. New analyzers: implement `IAnalyzer`, register in `_allAnalyzers` in `DeploymentRiskAnalyzerControl.cs`, and degrade query failures to informational findings instead of throwing.

## Domain reminders

- "Plugin" is ambiguous in this ecosystem: XrmToolBox tools (this repo) vs. Dataverse server-side plugins (`IPlugin`, registered in the sandbox). This repo is the former. If asked for a Dataverse `IPlugin`, that code does NOT belong in this solution.
- Entity/attribute names in queries are logical names (lowercase, e.g. `account`, `createdon`).
- Respect Dataverse service protection limits: batch writes (`BatchExecutor` default 200/batch), avoid parallel `ExecuteMultiple`, and prefer targeted `ColumnSet`s over `ColumnSet(true)`.
