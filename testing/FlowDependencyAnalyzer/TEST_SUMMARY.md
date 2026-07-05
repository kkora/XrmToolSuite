# Flow Dependency Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj` (SDK-free logic: `FlowClientDataParser`,
  `FlowRiskRules`, `FlowModels`).
- **Result:** 17 passed, 0 failed. The `FlowDependencyAnalyzerTests` suite was executed against the parser,
  rules and reverse-impact map using a representative `clientdata` fixture; all cases pass.
- **Note:** the three engine files must be referenced by `testing/UnitTests/UnitTests.csproj` via
  `<Compile Include=… />` (SDK-free files only) — see the exact lines in the implementation notes / repo README.
  During implementation the suite was executed in an isolated net8 project with those files and all 17 passed.

## Manual run

Manual GUI cases require Windows + XrmToolBox + a live Dataverse org and **were not executed** in this
environment (they cannot run headlessly). They are documented in `TEST_CASES.md` (TC-20…TC-26) as Pending.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 15 | 15 | 15 | 0 | 0 |
| Manual | 7 | 0 | 0 | 0 | 7 |

## Verdict

**Engine: PASS.** The SDK-free parser, risk rules and reverse-impact map are fully unit-tested (15 automated
cases, all passing), including verification that HTTP endpoint URLs, SAS/trigger URLs and secrets are redacted
and never stored. The tool builds in Release with 0 warnings / 0 errors and ships its Excel + native-PDF
dependency chains in the Plugins root.

**Manual GUI/Dataverse cases (TC-20…TC-26): PENDING** — to be executed in a Windows + XrmToolBox session
against a real org, with a screenshot captured per case under `screenshots/`.
