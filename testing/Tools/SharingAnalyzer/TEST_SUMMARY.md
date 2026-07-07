# Sharing Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Result:** 15 Sharing Analyzer tests pass (executed against the three SDK-free files via a standalone
  equivalent test project; they pass once the `<Compile Include>` lines for `AccessRights.cs`,
  `SharingModels.cs`, and `SharingRiskRules.cs` are added to `UnitTests.csproj`). `Passed! - Failed: 0,
  Passed: 15, Skipped: 0, Total: 15`.
- **Coverage:** `AccessRights.Decode`/`Summary` (Read, Read+Write, full, none), `SharingSummary`
  aggregations (RecordStats, PrincipalStats), and every `SharingRiskRules` rule (excessive → High,
  inactive-user → Medium, disabled-team → Medium, high-inbound → Medium, outlier → Medium, clean → Info)
  plus composite score/band.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 15 | 15 | 15 | 0 | 0 |
| Manual | 11 | 0 | 0 | 0 | 11 |

## Verdict

- **Automated logic:** PASS — the SDK-free rights decoding, aggregations, and risk rules are verified and
  deterministic.
- **Manual (live Dataverse + WinForms):** PENDING — the POA collector, principal resolution, all UI views,
  the full-scan opt-in, and Excel/PDF/JSON/HTML/CSV export require a Windows + XrmToolBox session against a
  real org and have not been executed headlessly. They must be run and screenshotted before the tool is
  declared release-verified.
- **Build:** `dotnet build src/Tools/XrmToolSuite.SharingAnalyzer/XrmToolSuite.SharingAnalyzer.csproj -c
  Release` succeeds with 0 warnings / 0 errors; ClosedXML + the `-gdi` PDF assemblies land in
  `bin/Release/net48`.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Sharing Analyzer** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Sharing Analyzer** v1.2026.7.2 (Kanchan Kora).
