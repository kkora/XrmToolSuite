<#
.SYNOPSIS
    Build (Release) and pack every XrmToolSuite tool into .nupkg files for the XrmToolBox Tool Library.

.DESCRIPTION
    Packs each src/Tools/XrmToolSuite.*/*.nuspec into the output directory, skipping the scaffold
    TemplateTool (never published). Each export tool's nupkg ships its dependency chain in a per-tool
    subfolder (lib\net48\Plugins\<AssemblyName>\) with the tool DLL in the Plugins root; the layout is
    verified after packing.

    REQUIREMENT: nuget.exe 5.10 or newer. The suite nuspecs use the <readme> element (embeds each tool's
    README in its package); nuget older than 5.10 rejects it with "invalid child element 'readme'". This
    script checks the version and stops with guidance if it is too old.

.PARAMETER OutputDirectory
    Where the .nupkg files are written. Default: <repo>\artifacts.

.PARAMETER NuGet
    Path to nuget.exe. Default: 'nuget' on PATH. Point this at a downloaded 5.10+ nuget.exe if your
    system one is older (get it from https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

.PARAMETER SkipBuild
    Skip the Release build (pack against whatever is already in bin\Release\net48).

.PARAMETER IncludeTemplate
    Also pack XrmToolSuite.TemplateTool (off by default; it is the scaffold, not a shipped tool).

.EXAMPLE
    ./scripts/Pack-All.ps1
    Build Release and pack all shipping tools into .\artifacts.

.EXAMPLE
    ./scripts/Pack-All.ps1 -NuGet C:\tools\nuget.exe -SkipBuild
    Pack using a specific nuget.exe without rebuilding.
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [string]$NuGet = 'nuget',
    [switch]$SkipBuild,
    [switch]$IncludeTemplate
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'XrmToolSuite.sln'
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'artifacts' }

# --- Resolve nuget.exe and enforce the 5.10+ requirement (for the <readme> element) ---
$nugetCmd = Get-Command $NuGet -ErrorAction SilentlyContinue
if (-not $nugetCmd) {
    throw "nuget.exe not found ('$NuGet'). Install it or pass -NuGet <path>. Latest: https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
}
$nugetPath = $nugetCmd.Source
$verLine = (& $nugetPath help) | Select-Object -First 1   # "NuGet Version: X.Y.Z.B"
$verMatch = [regex]::Match($verLine, '(\d+)\.(\d+)')
if ($verMatch.Success) {
    $major = [int]$verMatch.Groups[1].Value
    $minor = [int]$verMatch.Groups[2].Value
    if ($major -lt 5 -or ($major -eq 5 -and $minor -lt 10)) {
        $v = $verMatch.Value
        throw "nuget.exe is $v but the suite nuspecs use the <readme> element, which needs nuget 5.10+. Upgrade nuget.exe (https://dist.nuget.org/win-x86-commandline/latest/nuget.exe) or pass -NuGet <path-to-newer-nuget>."
    }
}
Write-Host "Using $nugetPath ($verLine)" -ForegroundColor Cyan

# --- Build Release so bin\Release\net48 is populated for every tool ---
if (-not $SkipBuild) {
    Write-Host "Building $solution (Release)..." -ForegroundColor Cyan
    & dotnet build $solution -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)." }
}

# --- Pack every tool nuspec (skip the scaffold TemplateTool unless -IncludeTemplate) ---
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
Get-ChildItem (Join-Path $OutputDirectory '*.nupkg') -ErrorAction SilentlyContinue | Remove-Item -Force

$nuspecs = Get-ChildItem (Join-Path $repoRoot 'src\Tools') -Recurse -Filter 'XrmToolSuite.*.nuspec'
if (-not $IncludeTemplate) {
    $nuspecs = $nuspecs | Where-Object { $_.Name -notlike '*TemplateTool*' }
}

$ok = 0; $failed = @()
foreach ($n in $nuspecs) {
    Write-Host "Packing $($n.Name)..." -ForegroundColor DarkGray
    & $nugetPath pack $n.FullName -OutputDirectory $OutputDirectory -NoDefaultExcludes
    if ($LASTEXITCODE -eq 0) { $ok++ } else { $failed += $n.Name }
}

# --- Verify the per-tool subfolder layout in each produced package ---
Add-Type -AssemblyName System.IO.Compression.FileSystem
$layoutProblems = @()
foreach ($pkg in Get-ChildItem (Join-Path $OutputDirectory '*.nupkg')) {
    $id = ($pkg.BaseName -replace '\.\d+\.\d+\.\d+\.\d+$', '')   # strip version -> package id
    $zip = [System.IO.Compression.ZipFile]::OpenRead($pkg.FullName)
    try {
        $entries = $zip.Entries.FullName
        $toolAtRoot = $entries -contains "lib/net48/Plugins/$id.dll"
        $depsInRoot = $entries | Where-Object { $_ -match '^lib/net48/Plugins/[^/]+\.dll$' -and $_ -ne "lib/net48/Plugins/$id.dll" }
        if (-not $toolAtRoot) { $layoutProblems += "${id}: tool DLL not at Plugins root" }
        if ($depsInRoot)      { $layoutProblems += ("{0}: {1} dep DLL(s) in Plugins root (should be in the {0} subfolder)" -f $id, $depsInRoot.Count) }
    } finally { $zip.Dispose() }
}

# --- Summary ---
Write-Host ""
Write-Host "Packed $ok package(s) into $OutputDirectory" -ForegroundColor Green
if ($failed.Count)         { Write-Host "FAILED: $($failed -join ', ')" -ForegroundColor Red }
if ($layoutProblems.Count) { $layoutProblems | ForEach-Object { Write-Host "LAYOUT: $_" -ForegroundColor Yellow } }
else                       { Write-Host "Layout OK: every package has its tool DLL in the Plugins root and deps in a per-tool subfolder." -ForegroundColor Green }

if ($failed.Count -or $layoutProblems.Count) { exit 1 }
