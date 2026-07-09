# scripts/

Helper PowerShell scripts for developing, deploying, and packaging the suite. Run them from the
repo root (Windows PowerShell). Every script has full comment-based help — `Get-Help ./scripts/<name>.ps1 -Full`.

| Script | Purpose |
|---|---|
| [`New-Tool.ps1`](New-Tool.ps1) | Stamp out a new tool project from the template (renames everything, adds it to the solution, stamps docs + testing skeleton). |
| [`Deploy-Tool.ps1`](Deploy-Tool.ps1) | Build + deploy **one (or a few)** tools to the local XrmToolBox, leaving the rest untouched. |
| [`Pack-All.ps1`](Pack-All.ps1) | Build Release + pack **every** non-template `.nuspec` into `artifacts\`, and verify the per-tool subfolder layout. |
| [`Setup-TestConnection.ps1`](Setup-TestConnection.ps1) | Pre-flight for the Tier-3b/3c connected UI walkthroughs — validates the XrmToolBox connection and prints the env vars to set. |

## New-Tool.ps1

Clones `XrmToolSuite.TemplateTool`, renames it, and wires it into the solution. A tool's csproj is
~8 lines; all build config lives in `src/Tools/Directory.Build.props`.

```powershell
./scripts/New-Tool.ps1 -Name "SolutionCompare" `
    -DisplayName "Solution Compare" `
    -Description "Compares components between two solutions"
```

## Deploy-Tool.ps1

Builds a single project with `-p:DeployToXTB=true`, which copies the tool DLL into the Plugins root
and (for export tools) its dependency chain into the per-tool subfolder `Plugins\XrmToolSuite.<Tool>\`.
Use it when you've changed one tool and don't want to rebuild the whole suite. **Close XrmToolBox
first** — it locks loaded plugin DLLs.

```powershell
./scripts/Deploy-Tool.ps1 DeploymentRiskAnalyzer          # one tool
./scripts/Deploy-Tool.ps1 SharingAnalyzer, PrivilegeGapAnalyzer   # several
./scripts/Deploy-Tool.ps1 -List                           # list tool names
./scripts/Deploy-Tool.ps1 MyTool -Configuration Debug     # debug build
```

Accepts short (`SharingAnalyzer`) or full (`XrmToolSuite.SharingAnalyzer`) names, case-insensitive,
with a "did you mean" suggestion on typos.

## Pack-All.ps1

Builds Release, packs every tool `.nuspec` (skipping the scaffold `TemplateTool`) into `artifacts\`,
then verifies each package ships its tool DLL in the Plugins root and its deps in the per-tool subfolder.

```powershell
./scripts/Pack-All.ps1                                    # -> .\artifacts\*.nupkg
./scripts/Pack-All.ps1 -NuGet C:\tools\nuget.exe -SkipBuild
```

**Requires `nuget.exe` 5.10+** — the nuspecs use the `<readme>` element (embeds each tool's README);
older nuget rejects it with *"invalid child element 'readme'"*. The script checks the version and stops
with guidance if it's too old. Latest: <https://dist.nuget.org/win-x86-commandline/latest/nuget.exe>.

## Setup-TestConnection.ps1

Pre-flight for the connected UI walkthroughs (`testing/UiSmokeTests/`). Inspects
`%AppData%\MscrmTools\XrmToolBox\Connections\ConnectionsV2.xml`, reports whether the named connection
exists and can drive an automated run, and prints the env vars to set before `dotnet test`. **It never
stores or injects credentials** — the suite's `XTS-CI-*` connections are interactive; connect once by
hand to warm the token first.

```powershell
./scripts/Setup-TestConnection.ps1 -Connection XTS-CI-DEV
```
