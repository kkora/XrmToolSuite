# FetchXML Performance Analyzer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj` (engine also verified standalone during development).
- **Result:** 10 passed, 0 failed, 0 skipped (`FetchXmlAnalyzerTests`).
- **Coverage:** parser shape/counts, nested-link counting, malformed + non-`<fetch>` errors, all-attributes→High,
  missing-filter→High, excessive-links→High, several-links→Medium, cost/band computation, clean query→Info/Low.
- **Wiring:** the FetchXml engine source is compiled into `testing/UnitTests/UnitTests.csproj` via
  `<Compile Include="..\..\src\Shared\Core\FetchXml\*.cs" />`; the case runs as part of the suite's 110-test run.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — launched real XrmToolBox v1.2025.10.74 (FlaUI) and confirmed **9/9** suite tools appear in
  the Tools list, including **FetchXML Performance Analyzer**. This proves MEF registration and that the shipped
  ClosedXML + PdfSharp/MigraDoc-GDI chains resolve at plugin-scan time (the "silently dropped tool" failure mode).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the XrmToolBox Tools tab filtered to
  **FetchXML Performance Analyzer** v1.2026.7.1 (Kanchan Kora), showing the tool loaded with a "NEW" badge.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 10 | 10 | 10 | 0 | 0 |
| Manual | 7 | 0 | 0 | 0 | 7 |

Manual cases (TC-11..TC-17) require a Windows + XrmToolBox session against a live Dataverse org and have
**not** been executed here. They cannot run headlessly. TC-16 (Excel) and TC-17 (PDF) additionally exercise
the shipped ClosedXML and PdfSharp/MigraDoc-GDI dependency chains loaded from the Plugins root.

## Verdict

Automated parser/rule-engine coverage passes (10/10). The tool builds in Release with zero warnings, and the
Excel (ClosedXML) + native-PDF (PdfSharp/MigraDoc-GDI) dependency chains land next to the tool DLL in the
build output. Manual GUI/Dataverse cases (analyze-without-connection, view load, timed execution, exports
incl. the new Excel + PDF cases, settings round-trip) remain **pending** a Windows + XrmToolBox session; do
not treat them as passed until executed with screenshots captured under `screenshots/`.
