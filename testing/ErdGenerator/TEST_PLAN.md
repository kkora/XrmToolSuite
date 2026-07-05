# ERD Generator - Test Plan

Traces to [`docs/user-stories/ErdGenerator.md`](../../docs/user-stories/ErdGenerator.md).

## Scope

The ERD Generator reads Dataverse entity metadata and produces an entity-relationship diagram, then
exports it to Mermaid, PlantUML, SVG, PNG, PDF, HTML, Markdown and JSON. These tests verify:

- **SDK-free logic** (automated): the ERD model, the Mermaid/PlantUML/JSON/SVG emitters, column selection
  by display level, and the re-queryless `ErdModel.Apply` filter (custom-only / managed-only / relationship-type).
- **Live/UI behavior** (manual): scope loading (all/solution/publisher), the table picker, metadata
  collection via `ErdCollector`, the preview pane, and the PNG (GDI+) and PDF (MigraDoc-GDI) exporters.

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free model + emitters + filter | xUnit in `testing/UnitTests/ErdGeneratorTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Metadata collection, scope/UI, PNG + PDF render | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher).

## Entry / exit criteria

- **Entry:** `dotnet build src/Tools/XrmToolSuite.ErdGenerator/XrmToolSuite.ErdGenerator.csproj -c Release`
  succeeds with 0 warnings and the five `-gdi` PDF DLLs are present in `bin/Release/net48/`.
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.

## Notes on rendering coverage

- SVG, Mermaid, PlantUML, JSON, HTML and Markdown are pure-managed string output and are covered by (or
  built directly from) the automated emitters.
- PNG requires `System.Drawing`/GDI+ and PDF requires the MigraDoc-GDI runtime; both are Windows/WinForms
  concerns and are only exercised in the manual tier (they cannot be asserted headlessly).
