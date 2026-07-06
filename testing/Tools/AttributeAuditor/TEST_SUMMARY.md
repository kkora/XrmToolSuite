# Attribute Auditor - Test Summary

## Automated run

- **SDK-free engine** — `dotnet test testing/UnitTests/UnitTests.csproj`
  - **Result:** 17 Attribute Auditor cases passed (TC-ADMIN10-SCAN-01..04, TC-ADMIN10-CLASS-05..09, TC-ADMIN10-RPT-10..11,
    TC-ADMIN10-CSV-12..13). Covers the usage scanners (form / fetch / layout / whole-token), retirement-candidate
    classification, the shared-`ReportModel` projection, and the CSV export. Traces to US-ADMIN10-2/3/4.
- **Collector against a fake connection** — `dotnet test testing/CollectorTests/CollectorTests.csproj`
  - **Result:** 8 cases passed (TC-ADMIN10-COL-01..08). Drives `AttributeUsageCollector` over a fake
    `IOrganizationService` (metadata seeded via `MetaBuilder`): forms, views (fetch + layout), processes,
    field security, the managed/system guards, and the custom-only scope filter.
- **Total automated: 25 passed, 0 failed, 0 skipped.** The tool also builds `Release` (net48) with zero warnings.

## Manual run

Not executed in this environment (headless — no XrmToolBox host or Dataverse connection). The WinForms UI
(run, custom-only / candidates-only toggles, results grid, CSV + HTML export, settings persistence) must be
exercised in a Windows + XrmToolBox session; capture a screenshot per case into `screenshots/`.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated (scanners/classification/report/CSV) | 13 | 13 | 13 | 0 | 0 |
| Automated (collector, headless) | 8 | 8 | 8 | 0 | 0 |
| Manual (UI/run/filter/export/settings) | 7 | 0 | 0 | 0 | 7 |

## Verdict

The v1 audit engine — usage detection (forms, views, processes, field security), retirement-candidate
classification, report projection, and CSV export — is **verified and green** (25 automated cases) and the
tool builds `Release` with zero warnings. Manual GUI/Dataverse cases (TC-ADMIN10-M-01..07) are **pending a live
org** and must be run before release — no manual case is claimed as passed here. Deferred features
(chart/dashboard & data-population signals, table/solution scoping, JSON/Excel export, guarded cleanup)
remain `[Planned]` and are out of this pass.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Attribute Auditor** loads and appears in the Tools list.
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Attribute Auditor** v1.2026.7.1 (Kanchan Kora).
