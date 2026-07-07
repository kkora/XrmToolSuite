# Environment Inventory - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Result:** 16 passed, 0 failed (the Environment Inventory SDK-free suite).
  Verified in isolation against the SDK-free source files (`InventoryModels.cs`, `InventoryExporter.cs`,
  `InventorySummary.cs`) plus the shared analysis core; all 16 `EnvironmentInventoryTests` cases pass.
- **Note:** the three SDK-free files must be added to `testing/UnitTests/UnitTests.csproj` for the suite run to
  include them (see the implementation report for the exact `<Compile Include>` lines). The tests themselves take
  **no** dependency on Newtonsoft — JSON validity is checked with the net8 built-in `System.Text.Json`.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — launched real XrmToolBox v1.2025.10.74 (FlaUI) and confirmed **9/9** suite tools appear in
  the Tools list, including **Environment Inventory**. This proves MEF registration and that the shipped
  ClosedXML + PdfSharp/MigraDoc-GDI chains resolve at plugin-scan time (the "silently dropped tool" failure mode).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the XrmToolBox Tools tab filtered to
  **Environment Inventory** v1.2026.7.1 (Kanchan Kora), showing the tool loaded with a "NEW" badge.

## Manual run

Manual GUI/live-Dataverse cases (TC-M1..TC-M14) require Windows + XrmToolBox + a Dataverse connection and were
**not executed** in this environment — they remain Pending. They cannot run headlessly. TC-M11..TC-M14 cover the
new Excel/Word/PDF export paths (ClosedXML + PdfSharp/MigraDoc-GDI chains) and are pending manual execution;
the Excel exporter references ClosedXML and is therefore excluded from the SDK-free unit-test project.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 15 | 15 | 15 | 0 | 0 |
| Manual | 14 | 0 | 0 | 0 | 14 |

## Verdict

Automated SDK-free logic (normalization/filter/counts, CSV/JSON/Markdown/HTML export, no-secrets guarantee,
report projection) is **fully passing**. The tool — including the newly added Excel/Word/PDF export chains —
builds in Release with zero warnings. Live Dataverse collection, the WinForms UI, and the new Excel/Word/PDF
exports (TC-M11..TC-M14) are covered by manual cases that are **pending execution on a Windows + XrmToolBox host** —
not claimed as passed.
