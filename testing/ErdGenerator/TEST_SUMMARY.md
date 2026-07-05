# ERD Generator - Test Summary

## Verdict

**Automated: PASS.** Manual/live and PNG/PDF/GUI cases: **PENDING** (require a Windows + XrmToolBox +
Dataverse session; not run in this environment).

## Automated results

- Suite: `testing/UnitTests/ErdGeneratorTests.cs` (9 cases: TC-ERD-MERMAID-01/02, TC-ERD-PLANTUML-03,
  TC-ERD-JSON-04, TC-ERD-FILTER-05/06/07, TC-ERD-SVG-08, TC-ERD-COLSELECT-09).
- Result: **9 passed, 0 failed** (executed against the SDK-free `Erd/*.cs` sources).
- Coverage: the ERD model, column selection by display level, the `ErdModel.Apply` filter (custom-only /
  managed-only / relationship-type), and the Mermaid / PlantUML / JSON / SVG emitters. HTML and Markdown
  are built directly on the covered SVG/Mermaid emitters.

> Note: `testing/UnitTests/UnitTests.csproj` must include the SDK-free `Erd/*.cs` sources for the suite
> runner to compile these tests — the exact `<Compile Include=.../>` lines are listed in the
> implementation report / PR. Until then the tests were executed via an equivalent standalone runner.

## Run tally

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 9 | 9 | 9 | 0 | 0 |
| Manual | 11 | 0 | 0 | 0 | 11 |

## Build

- `dotnet build src/Tools/XrmToolSuite.ErdGenerator/XrmToolSuite.ErdGenerator.csproj -c Release`:
  **succeeded, 0 warnings / 0 errors.**
- The five native-PDF `-gdi` DLLs (`PdfSharp-gdi`, `PdfSharp.Charting-gdi`,
  `MigraDoc.DocumentObjectModel-gdi`, `MigraDoc.Rendering-gdi`, `MigraDoc.RtfRendering-gdi`) are present in
  `bin/Release/net48/`. The ClosedXML/Excel chain and the System.* facades are correctly **not** shipped.

## Pending (manual, live)

- TC-ERD-M01..M11 — scope loading (all/solution/publisher), the table picker, live metadata collection via
  `ErdCollector`, the preview pane, settings round-trip, missing-metadata degradation, and the PNG (GDI+)
  and PDF (MigraDoc-GDI) exporters. These cannot run headlessly and are **not** claimed as passed.
- Evidence to be captured under `testing/ErdGenerator/screenshots/` when executed.

## Notes / deferrals

- PNG and PDF exporters are implemented to suite convention (System.Drawing / MigraDoc types confined to
  method bodies) and compile into the tool, but their rendered output is only verifiable in a live Windows
  session; marked `[Implemented*]` in the user stories pending manual sign-off.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **ERD Generator** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **ERD Generator** v1.2026.7.2 (Kanchan Kora).
