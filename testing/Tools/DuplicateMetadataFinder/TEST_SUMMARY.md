# Duplicate Metadata Finder - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Result:** 374 passed, 0 failed, 0 skipped (whole suite; 22 are this tool's
  `DuplicateMetadataFinderTests`). Release build of the tool: `0 Warning(s), 0 Error(s)`.
- **Coverage:** the SDK-free similarity primitives, per-pair scoring (incl. cross-kind isolation and exact
  content-hash match), threshold-driven clustering, the recommended-primary rule, and the `ReportModel`
  projection (score, banding, findings). The Dataverse collector, WinForms grid, and Excel/PDF exports are
  **not** covered here (they need a live org / GDI runtime) and are manual cases.

## Manual run

The **tool-load** case (TC-DMF-M-01) was executed via the FlaUI `testing/UiSmokeTests` harness against the
local XrmToolBox (2026-07-05): the tool appears in the Tools list with its name, version, description and
icon — proving MEF registration and the ClosedXML/PdfSharp dependency chain resolved at scan time. Evidence:
`screenshots/xrmtoolbox-tools-list.png`. The remaining eight `TC-DMF-M-*` cases (off-thread scan, filters,
threshold, group detail, the four exports, settings round-trip, degraded-scan note) need a live Dataverse
connection and remain **Pending**.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 22 | 22 | 22 | 0 | 0 |
| Manual | 9 | 1 | 1 | 0 | 8 |

## Verdict

**Automated logic: PASS.** The SDK-free engine and report projection are fully green and the tool builds in
Release with zero warnings. **Manual GUI/Dataverse/export cases: PENDING** a live XrmToolBox session — not
claimed as passed. The tool is read-only (recommends a primary to keep; never merges or deletes).
