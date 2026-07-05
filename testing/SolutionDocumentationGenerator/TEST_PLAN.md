# Solution Documentation Generator - Test Plan

Traces to [`docs/user-stories/SOLN05.SolutionDocumentationGenerator.md`](../../docs/user-stories/SOLN05.SolutionDocumentationGenerator.md).

## Scope

The Solution Documentation Generator scans a Dataverse solution (read-only) and produces a multi-section
document — inventory, schema (tables/columns/relationships/choices), forms, views, apps, automation,
plug-ins, web resources, custom APIs, configuration, security roles, diagrams, release notes and an
architecture summary — in a chosen documentation mode, then exports it to Word, PDF, Markdown, HTML, Excel
and JSON.

These tests verify:

- **Automated (SDK-free):** the `DocBuilder` template engine — documentation-mode gating (Executive Summary
  omits detail sections; Full Solution Reference includes all; Standard Reference drops only diagrams), the
  sections checklist (unchecked sections excluded), per-table column detail at Full Reference, inventory
  component counts, and "not available" degradation for unreadable sources; plus the `DocRenderers`
  (Markdown / self-contained theme-aware HTML / structured JSON) producing well-formed output with the
  expected section headers and branding.
- **Manual (live/GUI):** the SDK `DocCollector` (solution scan, per-section progress, cancellation,
  degradation), the preview pane, settings round-trip, and the Word (OpenXML) / PDF (MigraDoc-GDI) /
  Excel (ClosedXML) exporters, which require the WinForms host + a Dataverse connection and cannot run
  headlessly.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free logic (pure helpers) | xUnit in `testing/UnitTests/`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse queries and UI | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher).

## Entry / exit criteria

- **Entry:** tool builds in Release.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
