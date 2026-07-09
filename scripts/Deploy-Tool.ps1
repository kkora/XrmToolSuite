<#
.SYNOPSIS
    Build and deploy ONE (or a few) XrmToolSuite tools to the local XrmToolBox Plugins folder.

.DESCRIPTION
    Use this when you've changed a single tool and don't want to rebuild/redeploy the whole suite.
    For each named tool it builds just that project (Release) with -p:DeployToXTB=true, which copies the
    tool DLL into the Plugins root and (for export tools) its dependency chain into the per-tool subfolder
    Plugins\XrmToolSuite.<Name>\. Other tools already in the Plugins folder are left untouched.

    Close XrmToolBox before deploying: it locks loaded plugin DLLs, so a copy over a running tool fails.

.PARAMETER Name
    One or more tool names. Accepts the short form ("DeploymentRiskAnalyzer") or the full assembly id
    ("XrmToolSuite.DeploymentRiskAnalyzer"), case-insensitive.

.PARAMETER Configuration
    Build configuration. Default: Release.

.PARAMETER List
    List the available tool names and exit (no build).

.EXAMPLE
    ./scripts/Deploy-Tool.ps1 DeploymentRiskAnalyzer
    Build + deploy just the Deployment Risk Analyzer.

.EXAMPLE
    ./scripts/Deploy-Tool.ps1 SharingAnalyzer, PrivilegeGapAnalyzer
    Deploy two tools.

.EXAMPLE
    ./scripts/Deploy-Tool.ps1 -List
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$Name,
    [string]$Configuration = 'Release',
    [switch]$List
)

$ErrorActionPreference = 'Stop'
$repoRoot  = Split-Path -Parent $PSScriptRoot
$toolsRoot = Join-Path $repoRoot 'src\Tools'
$pluginsDir = Join-Path $env:APPDATA 'MscrmTools\XrmToolBox\Plugins'

# All tool project dirs except the scaffold template.
$allTools = Get-ChildItem $toolsRoot -Directory |
    Where-Object { $_.Name -like 'XrmToolSuite.*' -and $_.Name -ne 'XrmToolSuite.TemplateTool' } |
    Select-Object -ExpandProperty Name |
    Sort-Object

if ($List) {
    Write-Host "Available tools ($($allTools.Count)):" -ForegroundColor Cyan
    $allTools | ForEach-Object { Write-Host "  $($_ -replace '^XrmToolSuite\.', '')" }
    return
}

if (-not $Name -or $Name.Count -eq 0) {
    throw "Specify at least one tool name (or -List to see them). Example: ./scripts/Deploy-Tool.ps1 DeploymentRiskAnalyzer"
}

# Resolve each requested name to a project, failing early on typos with a suggestion.
$projects = @()
foreach ($n in $Name) {
    $id = if ($n -like 'XrmToolSuite.*') { $n } else { "XrmToolSuite.$n" }
    $match = $allTools | Where-Object { $_ -ieq $id }
    if (-not $match) {
        $near = $allTools | Where-Object { $_ -ilike "*$($n)*" } | ForEach-Object { $_ -replace '^XrmToolSuite\.', '' }
        $hint = if ($near) { " Did you mean: $($near -join ', ')?" } else { " Use -List to see all tools." }
        throw "Unknown tool '$n'.$hint"
    }
    $csproj = Join-Path (Join-Path $toolsRoot $match) "$match.csproj"
    if (-not (Test-Path $csproj)) { throw "Project file not found: $csproj" }
    $projects += [pscustomobject]@{ Id = $match; Csproj = $csproj }
}

Write-Host "Deploying $($projects.Count) tool(s) to $pluginsDir" -ForegroundColor Cyan

$deployed = @()
foreach ($p in $projects) {
    Write-Host "`nBuilding $($p.Id) ($Configuration)..." -ForegroundColor Cyan
    & dotnet build $p.Csproj -c $Configuration -p:DeployToXTB=true
    if ($LASTEXITCODE -ne 0) { throw "Build/deploy failed for $($p.Id) (exit $LASTEXITCODE)." }

    $dll = Join-Path $pluginsDir "$($p.Id).dll"
    $sub = Join-Path $pluginsDir $p.Id
    $depCount = if (Test-Path $sub) { (Get-ChildItem "$sub\*.dll" -ErrorAction SilentlyContinue).Count } else { 0 }
    $deployed += [pscustomobject]@{
        Tool     = $p.Id -replace '^XrmToolSuite\.', ''
        ToolDll  = if (Test-Path $dll) { 'root OK' } else { 'MISSING' }
        Deps     = if ($depCount) { "$depCount in subfolder" } else { '(none)' }
    }
}

Write-Host ""
$deployed | Format-Table -AutoSize
if ($deployed.ToolDll -contains 'MISSING') {
    Write-Host "One or more tool DLLs did not land in the Plugins root - is XrmToolBox still open (files locked)?" -ForegroundColor Yellow
    exit 1
}
Write-Host "Done. Restart XrmToolBox to pick up the change." -ForegroundColor Green
