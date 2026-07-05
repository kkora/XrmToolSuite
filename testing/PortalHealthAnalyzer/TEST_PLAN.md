# Portal Health Analyzer - Test Plan

Traces to [`docs/user-stories/PP1.PortalHealthAnalyzer.md`](../../docs/user-stories/PP1.PortalHealthAnalyzer.md).

## Scope

The Portal Health Analyzer is a read-only tool that inventories one Power Pages website's configuration
across the dual `adx_`/`mspp_` schemas and scores its health. The SDK collector
(`Analysis/PortalCollector.cs`) detects the provisioned schema, lists websites, and retrieves the site's
pages, templates, page templates, snippets, site settings, web roles, table permissions, forms, lists, web
files and redirects into a schema-normalized `PortalInventory`. The SDK-free rule engine
(`Analysis/PortalHealthRules.cs`) then runs structural-integrity, site-settings, and security-surface rules
to produce a deterministic 0–100 health score, a Low/Medium/High band, and a categorized issue grid, each
finding carrying a plain-language recommendation. Excel/PDF/Word/JSON/HTML/CSV exports come from the shared
reporting module.

These tests verify:

- **Automated (SDK-free):** the `PortalHealthRules` engine over hand-built `PortalInventory` fixtures —
  missing/dangling page templates and parents (High), inactive pages/templates (Medium), web files
  referenced but absent (High), forms bound to missing tables (High), missing required settings (High),
  duplicate settings (Medium), anonymous permissions (Critical + High), Global-scope permissions (Medium),
  a clean baseline (Low, score 0), determinism, both-schema normalization, and unavailable-table degradation.
- **Manual (live):** website discovery with schema badges, collection with progress against a real
  `adx_`/`mspp_` site, the dashboard (score + band + config summary + issue grid), settings round-trip, and
  every export format — none of which can run headlessly (they need a Dataverse connection and the WinForms
  host).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (`PortalModels`, `PortalHealthRules`) | xUnit in `testing/UnitTests/PortalHealthAnalyzerTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse queries (`PortalCollector`), UI, exports | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env with a Power Pages site |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`). Requires the two source
  files under test to be referenced by `UnitTests.csproj` (see TEST_SUMMARY note).
- **Manual:** Windows + XrmToolBox + a Dataverse connection with a provisioned Power Pages website
  (`adx_website` or `mspp_website`), System Customizer or higher to read portal tables and metadata.

## Entry / exit criteria

- **Entry:** the tool builds in Release with 0 warnings; the export dependency DLLs land in
  `bin/Release/net48/` (ClosedXML + the `-gdi` PdfSharp/MigraDoc chain).
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
