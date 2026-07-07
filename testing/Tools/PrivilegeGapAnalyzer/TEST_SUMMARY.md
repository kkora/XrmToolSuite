# Privilege Gap Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj` (after adding the two `<Compile>` lines below).
- **Result:** 10 passed, 0 failed, 0 skipped — the `PrivilegeEngineTests` suite (verified in isolation against
  `PrivilegeModels.cs` + `PrivilegeEngine.cs`; the same source files the tool compiles).
- **Scope:** SDK-free effective-privilege engine only — deepest-scope resolution, team-only flagging, the five
  exercised verdict types (AccessAllowed, MissingPrivilege, InsufficientScope, TeamInheritanceOnly, AppendMismatch),
  the Append pair, principal Diff, and `Max`.
- **Required csproj change (not yet applied — do not edit `UnitTests.csproj` without sign-off):**

  ```xml
  <!-- Privilege Gap Analyzer: SDK-free effective-privilege engine (no Dataverse/WinForms). -->
  <Compile Include="..\..\src\Tools\XrmToolSuite.PrivilegeGapAnalyzer\Privileges\PrivilegeModels.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.PrivilegeGapAnalyzer\Privileges\PrivilegeEngine.cs" />
  ```

  (Do **not** add `PrivilegeCollector.cs` — it references the Dataverse SDK and is manual-tested.)

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — launched real XrmToolBox v1.2025.10.74 (FlaUI) and confirmed **9/9** suite tools appear in
  the Tools list, including **Privilege Gap Analyzer**. This proves MEF registration and that the shipped
  ClosedXML + PdfSharp/MigraDoc-GDI chains resolve at plugin-scan time (the "silently dropped tool" failure mode).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the XrmToolBox Tools tab filtered to
  **Privilege Gap Analyzer** v1.2026.7.1 (Kanchan Kora), showing the tool loaded with a "NEW" badge.

## Manual run

Not executed — the collector queries and WinForms UI require Windows + XrmToolBox + a live Dataverse org, which
is not available in this environment. TC-M-01 … TC-M-11 (including the new Excel/PDF export cases TC-M-09a and
TC-M-09b) remain **Pending**; each needs execution and a screenshot in `screenshots/`. No manual case is claimed
as passed.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 10 | 10 | 10 | 0 | 0 |
| Manual | 13 | 0 | 0 | 0 | 13 |

## Build

- `dotnet build src/Tools/XrmToolSuite.PrivilegeGapAnalyzer/XrmToolSuite.PrivilegeGapAnalyzer.csproj -c Release`
  succeeds with **0 warnings, 0 errors**.
- The Excel (ClosedXML) and native-PDF (PdfSharp/MigraDoc-GDI) export dependency chains copy into
  `bin/Release/net48/` (17 DLLs incl. the five `-gdi` PDF assemblies) and are declared in the nuspec/`DeployGuardDependencies`
  target, matching Deployment Risk Analyzer's shipping model.

## Verdict

Automated engine tests pass and the tool builds clean. Manual GUI/Dataverse verification is pending a
Windows + XrmToolBox session against a real org.
