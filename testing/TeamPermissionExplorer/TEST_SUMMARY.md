# Team Permission Explorer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj --filter FullyQualifiedName~TeamPermissionExplorerTests`
- **Result:** 11 passed, 0 failed (executed 2026-07-04).
- **Scope:** SDK-free risk rules (`TeamRiskRules`) for every rule — no-members→Medium, AAD-group not
  flagged, no-roles→Medium, over-privileged→High (+ custom threshold), duplicate-role→Low (two shapes),
  orphaned→Medium, clean→Info — plus `TeamProfile.Effective()` resolving the deepest scope per privilege
  via the shared `PrivilegeEngine.ResolveEffective`.
- **Note:** the two source files under test (`Analysis/TeamModels.cs`, `Analysis/TeamRiskRules.cs`) must
  be added to `testing/UnitTests/UnitTests.csproj` for the suite `dotnet test` run to include them — see
  the Compile lines in the implementation notes. The shared `Privileges\*.cs` is already compiled there.

## Build

- **Command:** `dotnet build src/Tools/XrmToolSuite.TeamPermissionExplorer/XrmToolSuite.TeamPermissionExplorer.csproj -c Release`
- **Result:** Build succeeded — 0 warnings, 0 errors.
- **Dependency chain:** `ClosedXML.dll`, `PdfSharp-gdi.dll`, `MigraDoc.Rendering-gdi.dll`,
  `DocumentFormat.OpenXml.dll` confirmed present in `bin/Release/net48/`.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 11 | 11 | 11 | 0 | 0 |
| Manual | 11 | 0 | 0 | 0 | 11 |

Manual GUI + live-Dataverse cases (TC-20…TC-30) require a Windows + XrmToolBox session against a real
org and **have not been executed** in this headless environment. They are Pending, with screenshots to
be captured under `screenshots/`.

## Verdict

Automated logic: **Pass** (11/11). Build: **Pass** (0/0). Manual/live and export-dialog cases:
**Pending** — to be run in an interactive XrmToolBox session. No defects found so far.
