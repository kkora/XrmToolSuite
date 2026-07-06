<#
.SYNOPSIS
    Pre-flight for the Tier-3b connected UI walkthrough (testing/UiSmokeTests/ConnectedWalkthroughTest.cs).
    Validates that the target XrmToolBox connection exists, reports whether it can drive an automated run,
    and prints the environment variables to set before `dotnet test`.

.DESCRIPTION
    The connected walkthrough opens a plugin in the REAL XrmToolBox host and asserts it comes up connected.
    That relies on XrmToolBox's Option-1 behaviour: the connection selected/last-used in the host is handed
    to the opened plugin. This script inspects %AppData%\MscrmTools\XrmToolBox\Connections\ConnectionsV2.xml
    and tells you whether the named connection is present and automatable.

    IT DOES NOT STORE OR INJECT CREDENTIALS. The suite's XTS-CI-* connections are interactive
    (OnlineFederation / AD, SavePassword=false): they reconnect only while an MSAL token is warm in THIS
    Windows profile. So the workflow is: connect the target once by hand in XrmToolBox (to warm the token),
    then run this script, then run the test - all in the same desktop session. There is deliberately no
    unattended-CI path here, because an interactive connection cannot authenticate headlessly.

.PARAMETER Connection
    Connection name to validate (default XTS-CI-TEST). NEVER point this at production.

.PARAMETER ConnectionsFile
    Override the ConnectionsV2.xml path (default: the current profile's XrmToolBox connections file).

.EXAMPLE
    ./scripts/Setup-TestConnection.ps1
    ./scripts/Setup-TestConnection.ps1 -Connection XTS-CI-TEST
#>
[CmdletBinding()]
param(
    [string]$Connection = 'XTS-CI-TEST',
    [string]$ConnectionsFile = (Join-Path $env:APPDATA 'MscrmTools\XrmToolBox\Connections\ConnectionsV2.xml')
)

$ErrorActionPreference = 'Stop'

function Write-Ok  ($m) { Write-Host "  [OK]   $m" -ForegroundColor Green }
function Write-Warn2($m) { Write-Host "  [WARN] $m" -ForegroundColor Yellow }
function Write-Err ($m) { Write-Host "  [FAIL] $m" -ForegroundColor Red }

Write-Host "Tier-3b connected-walkthrough pre-flight" -ForegroundColor Cyan
Write-Host "----------------------------------------"

# Guardrail: never run the connected walkthrough against production.
if ($Connection -match '(?i)prod') {
    Write-Err "Connection name '$Connection' looks like production. Point -Connection at a dev/test org."
    exit 1
}

if (-not (Test-Path $ConnectionsFile)) {
    Write-Err "ConnectionsV2.xml not found at: $ConnectionsFile"
    Write-Host  "        Open XrmToolBox and create a connection named '$Connection' first."
    exit 1
}
Write-Ok "Found connections file: $ConnectionsFile"

[xml]$xml = Get-Content -Path $ConnectionsFile -Raw
$detail = $xml.CrmConnections.Connections.ConnectionDetail |
    Where-Object { $_.ConnectionName -eq $Connection } | Select-Object -First 1

if (-not $detail) {
    $names = @($xml.CrmConnections.Connections.ConnectionDetail | ForEach-Object { $_.ConnectionName })
    Write-Err "No connection named '$Connection' in the file."
    Write-Host  "        Available: $($names -join ', ')"
    exit 1
}
Write-Ok "Connection '$Connection' is present."
Write-Host  "         Org : $($detail.OrganizationFriendlyName)  ($($detail.WebApplicationUrl))"
Write-Host  "         Auth: $($detail.AuthType) / $($detail.NewAuthType)   MFA=$($detail.UseMfa)   SavePassword=$($detail.SavePassword)"

# Automatability check. Interactive federated/AD auth with no saved password can only reconnect from a warm
# token - usable for a LOCAL, same-session run, but never for unattended CI.
$interactive = (($detail.SavePassword -eq 'false') -and ($detail.NewAuthType -in @('AD', 'OAuth'))) -or
               ($detail.AuthType -eq 'OnlineFederation')
if ($interactive) {
    Write-Warn2 "This is an INTERACTIVE connection (no stored secret)."
    Write-Warn2 "  - LOCAL run: OK, but connect '$Connection' once in XrmToolBox first to warm the token."
    Write-Warn2 "  - CI (unattended): NOT usable - it would block on an auth prompt. Use a service-principal"
    Write-Warn2 "    (ClientSecret/Certificate) connection for that, if your tenant permits app registrations."
}
else {
    Write-Ok "Connection appears non-interactive (service principal) - usable unattended."
}

Write-Host ""
Write-Host "Next steps (run in THIS PowerShell session):" -ForegroundColor Cyan
Write-Host "  dotnet build XrmToolSuite.sln -c Release -p:DeployToXTB=true"
Write-Host "  `$env:XTB_EXE = 'C:\devtools\XrmToolbox\XrmToolBox.exe'   # your local install"
Write-Host "  `$env:XTB_CONNECTED_TEST = '1'"
Write-Host "  `$env:XTB_TEST_CONNECTION = '$Connection'"
Write-Host "  `$env:XTB_WALKTHROUGH_TOOL = 'Deployment Risk Analyzer'"
Write-Host "  dotnet test testing/UiSmokeTests/UiSmokeTests.csproj"
Write-Host ""
Write-Host "Pre-flight complete." -ForegroundColor Green
