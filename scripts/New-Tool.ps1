<#
.SYNOPSIS
    Stamps out a new XrmToolBox tool from the template project.
.EXAMPLE
    ./scripts/New-Tool.ps1 -Name "AttributeAuditor" -DisplayName "Attribute Auditor" -Description "Audits unused attributes across entities"
#>
param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z][A-Za-z0-9]*$')]
    [string]$Name,                       # PascalCase, used in namespaces/filenames
    [string]$DisplayName = $Name,        # Shown in the XrmToolBox tool list
    [string]$Description = "TODO: describe $DisplayName"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$src  = Join-Path $root 'src/Tools/XrmToolSuite.TemplateTool'
$dst  = Join-Path $root "src/Tools/XrmToolSuite.$Name"

if (Test-Path $dst) { throw "Project already exists: $dst" }

Copy-Item $src $dst -Recurse
Get-ChildItem $dst -Recurse -Include 'bin','obj' -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# Rename files first, then rewrite content
Get-ChildItem $dst -Recurse -File | Where-Object { $_.Name -match 'TemplateTool' } | ForEach-Object {
    Rename-Item $_.FullName ($_.Name -replace 'TemplateTool', $Name)
}

Get-ChildItem $dst -Recurse -File | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    # Stamp the caller's description into the nuspec (both description and summary).
    # Match on the XML elements so this survives future edits to the template's wording.
    # Escape '$' as '$$' so a description containing '$' can't inject a regex group reference.
    $descRepl = $Description -replace '\$', '$$$$'
    $content = $content -replace '<description>.*?</description>', "<description>$descRepl</description>"
    $content = $content -replace '<summary>.*?</summary>', "<summary>$descRepl</summary>"
    $content = $content -replace 'Template Tool', $DisplayName
    $content = $content -replace 'TemplateTool', $Name
    Set-Content $_.FullName $content -NoNewline -Encoding UTF8
}

# Generate this tool's user-story backlog from the shared template in docs/user-stories.
$usDir = Join-Path $root 'docs/user-stories'
$usTemplate = Join-Path $usDir '_TEMPLATE.md'
if (Test-Path $usTemplate) {
    $us = (Get-Content $usTemplate -Raw) -replace 'Template Tool', $DisplayName -replace 'TemplateTool', $Name
    Set-Content (Join-Path $usDir "$Name.md") $us -NoNewline -Encoding UTF8
}

# Scaffold this tool's testing folder (plan / cases / summary + screenshots) from testing/_templates.
$testTemplates = Join-Path $root 'testing/_templates'
if (Test-Path $testTemplates) {
    $toolTestDir = Join-Path $root "testing/$Name"
    New-Item -ItemType Directory -Path (Join-Path $toolTestDir 'screenshots') -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $toolTestDir 'screenshots/.gitkeep') -Force | Out-Null
    Get-ChildItem $testTemplates -File -Filter '*.md' | ForEach-Object {
        $t = (Get-Content $_.FullName -Raw) -replace 'Template Tool', $DisplayName -replace 'TemplateTool', $Name
        Set-Content (Join-Path $toolTestDir $_.Name) $t -NoNewline -Encoding UTF8
    }
}

# Add to the solution
Push-Location $root
try {
    dotnet sln XrmToolSuite.sln add "src/Tools/XrmToolSuite.$Name/XrmToolSuite.$Name.csproj"
}
finally { Pop-Location }

Write-Host ""
Write-Host "Created XrmToolSuite.$Name" -ForegroundColor Green
Write-Host "Next steps:"
Write-Host "  1. Open the solution and rename UI/logic in ${Name}Control.cs"
Write-Host "  2. Update RepositoryName/UserName/HelpUrl in the control"
Write-Host "  3. Review the generated DEPLOYMENT.md (single-DLL build/install guide for this tool)"
Write-Host "  4. Fill in docs/user-stories/$Name.md (Epic / Features / User Stories)"
Write-Host "  5. Fill in testing/$Name/ (TEST_PLAN / TEST_CASES / TEST_SUMMARY); add xUnit cases to testing/UnitTests"
Write-Host "  6. Build with: dotnet build -p:DeployToXTB=true  (auto-copies to XrmToolBox Plugins folder)"
