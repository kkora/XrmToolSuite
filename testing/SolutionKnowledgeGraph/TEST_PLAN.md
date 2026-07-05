# Solution Knowledge Graph - Test Plan

Traces to [`docs/user-stories/KG1.SolutionKnowledgeGraph.md`](../../docs/user-stories/KG1.SolutionKnowledgeGraph.md).

## Scope

Validate the Solution Knowledge Graph end to end: the SDK-free graph model, algorithms (trace/impact/
cycles), and GraphML/SVG/HTML exporters (automated), and the Dataverse graph builder, PNG export, the
interactive browser view, and the WinForms UI (manual, against a live org).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | Model, dependency trace, impact, cycle detection, GraphML/SVG/HTML output (US-KG-2..5) | xUnit over `GraphModel` + exporters | .NET 8 SDK (`testing/UnitTests`) |
| Manual | Graph builder, PNG export, interactive view, UI (US-KG-1, US-KG-4..5) | GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## What is NOT automatable here

The builder needs a live solution and the `dependency` table; PNG export uses GDI+ (System.Drawing); the
interactive view and UI need the WinForms host + a browser. These are documented manual cases with a screenshot each.

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`) — no connection.
- **Manual:** Windows + XrmToolBox + a Dataverse connection with a real solution; a browser for the interactive view.

## Entry / exit criteria

- **Entry:** tool builds `Release` with zero warnings; automated tests green.
- **Exit:** all automated tests pass; all manual cases executed with a screenshot, or defects logged.

## Risks

- The `dependency` table only exposes tracked dependencies — edges may be sparser than a full reference graph; note this in the summary (TC-KG-M-02).
- Very large solutions: verify the interactive view and layout remain usable (TC-KG-M-04).
