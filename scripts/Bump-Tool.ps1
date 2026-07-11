<#
.SYNOPSIS
    Bump ONE tool's version (per-tool versioning): updates the csproj <Version> and the nuspec
    <version> together, and optionally stamps the nuspec <releaseNotes>.

.DESCRIPTION
    The suite uses PER-TOOL versioning: each tool's csproj declares its own <Version>, which must
    equal its nuspec <version> (Tool Library rule: nupkg version == assembly version). Bump a tool
    only when that tool changes; unchanged tools keep their version, and `dotnet nuget push
    --skip-duplicate` in the publish workflow skips them (already on nuget.org), so ONLY changed
    tools are actually republished.

.PARAMETER Name
    Tool name without the XrmToolSuite. prefix (e.g. "AttributeAuditor"), or the full project name.

.PARAMETER Version
    The new version (e.g. 1.2026.7.9). If omitted, the LAST numeric part of the current version is
    incremented (1.2026.7.8 -> 1.2026.7.9).

.PARAMETER ReleaseNotes
    Optional text for the nuspec <releaseNotes>. If omitted, the existing notes are left as-is -
    remember to update them before publishing.

.EXAMPLE
    ./scripts/Bump-Tool.ps1 -Name AttributeAuditor
    Increments XrmToolSuite.AttributeAuditor from 1.2026.7.8 to 1.2026.7.9 (csproj + nuspec).

.EXAMPLE
    ./scripts/Bump-Tool.ps1 -Name SolutionKnowledgeGraph -Version 1.2026.8.1 -ReleaseNotes "1.2026.8.1: ..."
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Name,
    [string]$Version,
    [string]$ReleaseNotes
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = if ($Name -like 'XrmToolSuite.*') { $Name } else { "XrmToolSuite.$Name" }
$dir = Join-Path $repoRoot "src\Tools\$project"
$csproj = Join-Path $dir "$project.csproj"
$nuspec = Join-Path $dir "$project.nuspec"
if (-not (Test-Path $csproj)) { throw "No such tool: $csproj" }
if (-not (Test-Path $nuspec)) { throw "No nuspec for tool: $nuspec" }

$bom = New-Object System.Text.UTF8Encoding($true)

# --- current version from the csproj ---
$csText = [System.IO.File]::ReadAllText($csproj)
$m = [regex]::Match($csText, '<Version>([^<]+)</Version>')
if (-not $m.Success) { throw "$project.csproj has no <Version> - per-tool versioning expects one." }
$current = $m.Groups[1].Value.Trim()

if (-not $Version) {
    $parts = $current.Split('.')
    $parts[-1] = ([int]$parts[-1] + 1).ToString()
    $Version = $parts -join '.'
}

# --- csproj ---
$csText = $csText.Replace("<Version>$current</Version>", "<Version>$Version</Version>")
[System.IO.File]::WriteAllText($csproj, $csText, $bom)

# --- nuspec: version (+ optional release notes) ---
$nuText = [System.IO.File]::ReadAllText($nuspec)
$nuText = [regex]::Replace($nuText, '<version>[^<]+</version>', "<version>$Version</version>", 1)
if ($ReleaseNotes) {
    $nuText = [regex]::Replace($nuText, '(?s)<releaseNotes>.*?</releaseNotes>',
        "<releaseNotes>$([System.Security.SecurityElement]::Escape($ReleaseNotes))</releaseNotes>", 1)
}
[System.IO.File]::WriteAllText($nuspec, $nuText, $bom)

Write-Host "$project : $current -> $Version (csproj + nuspec)" -ForegroundColor Green
if (-not $ReleaseNotes) { Write-Host "Reminder: update the nuspec <releaseNotes> before publishing." -ForegroundColor Yellow }
