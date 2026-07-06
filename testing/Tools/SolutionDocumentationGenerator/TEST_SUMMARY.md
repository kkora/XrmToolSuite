# Solution Documentation Generator - Test Summary

## Verdict

**Automated: PASS.** Manual/live and Word/PDF/Excel/GUI cases: **PENDING** (require a Windows + XrmToolBox +
Dataverse session; not run in this environment).

## Automated results

- Suite: `testing/UnitTests/SolutionDocumentationGeneratorTests.cs` (12 cases: TC-SOLN05-COUNT-01,
  TC-SOLN05-MODE-02/03/04, TC-SOLN05-SECT-05, TC-SOLN05-SCHEMA-06, TC-SOLN05-NA-07, TC-SOLN05-MD-08,
  TC-SOLN05-HTML-09, TC-SOLN05-JSON-10, TC-SOLN05-PORTAL-11, TC-SOLN05-PORTAL-12).
- Result: **12 passed, 0 failed** (executed against the SDK-free `Doc/DocModels.cs`, `Doc/DocBuilder.cs`,
  `Doc/DocRenderers.cs` sources; full suite `dotnet test testing/UnitTests` = 412 passed).
- Coverage: the `SolutionScanData.ComponentCount` rollup, the `DocBuilder` documentation-mode gating
  (Executive Summary / Standard Reference / Full Solution Reference), the sections checklist, per-table
  column detail at Full Reference, "not available" degradation, and the `DocRenderers` Markdown / HTML /
  JSON output (well-formed, theme-aware, branding, structured inventory) plus the **searchable HTML portal**
  (self-contained/offline, sidebar TOC per section, search box + theme toggle, HTML escaping — folds in DOC05).

> Note: `testing/UnitTests/UnitTests.csproj` must include the three SDK-free `Doc/*.cs` sources for the
> suite runner to compile these tests — the exact `<Compile Include=.../>` lines are listed in the
> implementation report / PR. Until then the tests were executed via an equivalent standalone runner
> (net8.0 xUnit project compiling the same three sources + the test file), which reported
> **10 passed, 0 failed**.

## Run tally

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 12 | 12 | 12 | 0 | 0 |
| Manual | 16 | 0 | 0 | 0 | 16 |

## Build

- `dotnet build src/Tools/XrmToolSuite.SolutionDocumentationGenerator/XrmToolSuite.SolutionDocumentationGenerator.csproj -c Release`:
  **succeeded, 0 warnings / 0 errors.**
- The full export chain is present in `bin/Release/net48/`: the Excel chain (`ClosedXML.dll`,
  `DocumentFormat.OpenXml.dll` — the latter also drives the Word `.docx` export — `ExcelNumberFormat.dll`,
  `XLParser.dll`, `Irony.dll`, `SixLabors.Fonts.dll`, `System.IO.Packaging.dll` + facades) and the
  native-PDF `-gdi` chain (`PdfSharp-gdi`, `PdfSharp.Charting-gdi`, `MigraDoc.DocumentObjectModel-gdi`,
  `MigraDoc.Rendering-gdi`, `MigraDoc.RtfRendering-gdi`). All 17 dependency DLLs are wired in the nuspec
  Plugins ROOT.

## Pending (manual, live)

- TC-SOLN05-M01..M15 — solution loading, the preview pane, per-section progress + cancellation, settings
  round-trip, degradation on permission gaps, no-secret-leakage, and the Word (OpenXML) / PDF (MigraDoc-GDI)
  / Excel (ClosedXML) exporters. These cannot run headlessly and are **not** claimed as passed.
- Evidence to be captured under `testing/SolutionDocumentationGenerator/screenshots/` when executed.

## Notes / deferrals

- **Word export uses the OpenXML SDK** (`DocumentFormat.OpenXml`, already shipped by the ClosedXML chain) to
  produce a true `.docx` — chosen over MigraDoc RTF so the output is a native Word document. OpenXML/ClosedXML
  and MigraDoc/PdfSharp types are confined to method-body locals in `DocWordExporter` / `DocExcelExporter` /
  `DocPdfExporter`; those three files are excluded from the SDK-free unit-test set.
- The SDK `DocCollector` uses `msdyn_solutioncomponentsummary` as the primary component source (with a raw
  `solutioncomponent` fallback) and enriches high-value detail (automation category/state, plug-in step
  stage/mode, form/view entity, env-var type, connection-reference connector, custom-API kind, role BU) via
  targeted queries scoped to the solution's component ids. Correctness against a live org is a manual case.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Solution Documentation Generator** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Solution Documentation Generator** v1.2026.7.2 (Kanchan Kora).
