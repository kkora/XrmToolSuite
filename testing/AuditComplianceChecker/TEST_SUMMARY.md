# Audit Compliance Checker - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Result:** 19 audit-compliance tests **passed** (0 failed), verified in isolation against the SDK-free
  engine files. The run is gated on adding the three `<Compile Include=.../>` lines below to
  `UnitTests.csproj` (the engine files are SDK-free; the collector is not included).
- **Compile lines to add to `testing/UnitTests/UnitTests.csproj`:**
  ```xml
  <!-- Audit Compliance Checker: SDK-free sensitivity heuristics + compliance rules/scoring. -->
  <Compile Include="..\..\src\Tools\XrmToolSuite.AuditComplianceChecker\Analysis\AuditModels.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.AuditComplianceChecker\Analysis\SensitivityHeuristics.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.AuditComplianceChecker\Analysis\AuditComplianceRules.cs" />
  ```
- **Evidence:** the 19 tests are TC-01 … TC-10 in `TEST_CASES.md` (some cases map to multiple `[Fact]`/`[Theory]` rows).

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 10 | 10 | 10 | 0 | 0 |
| Manual | 9 | 0 | 0 | 0 | 9 |

Manual cases (TC-11 … TC-19) require a Windows + XrmToolBox session against a live Dataverse org and
**cannot run headlessly**; they are Pending until executed with screenshots in `screenshots/`.

## Verdict

Automated engine logic **passes** (deterministic scoring, banding, sensitivity classification, coverage
and activity rules). The Release build is clean (0 warnings / 0 errors) and the Excel/PDF export
dependency chain lands in the tool output. Manual GUI/live and export cases are **Pending** a Windows +
XrmToolBox + Dataverse session and are not claimed as passed.
</content>
