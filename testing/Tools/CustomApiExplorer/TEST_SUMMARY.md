# Custom API Explorer - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj`
- **Result:** 394 passed, 0 failed, 0 skipped (whole suite; 20 are this tool's
  `CustomApiExplorerTests`). Release build of the tool: `0 Warning(s), 0 Error(s)`.
- **Coverage:** SDK-free scalar value parsing, request-parameter binding / required-validation, the
  illustrative sample-snippet generator, and the HTML/Markdown/CSV catalog exporters (incl. HTML escaping).
  The Dataverse collector and the **live invocation** are **not** covered here (they need a live org) and are
  manual cases.

## Manual run

The **tool-load** case (TC-CAE-M-01) was executed via the FlaUI `testing/UiSmokeTests` harness against the
local XrmToolBox (2026-07-05): the tool appears in the Tools list with its name, version, description and
icon — proving MEF registration resolved at scan time. Evidence: `screenshots/xrmtoolbox-tools-list.png`.
The remaining ten `TC-CAE-M-*` cases (off-thread inventory, filter/plugin, params/responses detail, generated
parameter form, validation, **confirmation gate**, response/fault display, bound-API target, catalog export,
settings round-trip) need a live Dataverse connection and remain **Pending**. Live invocation should be
exercised against a sandbox / read-only function first.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 14 | 14 | 14 | 0 | 0 |
| Manual | 11 | 1 | 1 | 0 | 10 |

## Verdict

**Automated logic: PASS.** The SDK-free parsing/binding/snippet/exporters are fully green and the tool builds
in Release with zero warnings. **Manual GUI/Dataverse/invocation cases: PENDING** a live XrmToolBox session —
not claimed as passed. Inventory/documentation is read-only; the invoke console is the only write path and is
confirmation-gated (naming API, target and environment) and secret-safe.
