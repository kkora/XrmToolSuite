# Deployment Risk Analyzer - Test Summary

## Automated run

Two automated projects, both green:

### 1. Risk scoring & banding (SDK-free)

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit on net8.0 (SDK-free logic compiled directly; no Dataverse/WinForms).
- **Result:** **24 passed, 0 failed, 0 skipped** (scoring/banding + deployment-summary logic).
- **Duration:** ~0.1 s.
- **Evidence:** [`test-run.txt`](test-run.txt).

Covers all risk-scoring/banding cases TC-DG-SCORE-01..05, TC-DG-BAND-06..11, TC-DG-EXPLAIN-12,
TC-DG-SUMMARY-13 - i.e. the full acceptance criteria of **US-DG-8.1** (weights, cap at 100, Critical
forces High, Medium/High thresholds).

Also covers deployment-summary logic TC-DG-SUM-01..05 (**US-DG-8**): the anonymized payload builder
(field mapping, component redaction / Mode C, top-N truncation & ordering) and the offline templated
generator (go/no-go verdict per band, top-risks list). The live Anthropic HTTP path is manual.

```
Passed!  - Failed: 0, Passed: 24, Skipped: 0, Total: 24
```

### 2. Analyzer logic via fake connection

- **Command:** `dotnet test testing/AnalyzerTests/AnalyzerTests.csproj`
- **Framework:** xUnit on net48; the real analyzers driven against an in-memory
  `FakeOrganizationService` (no live org).
- **Result:** **54 passed, 0 failed, 0 skipped.**
- **Duration:** ~0.23 s.

Covers the query-driven logic of eight analyzers:
- TC-DG-EV-01..09 (US-DG-4, environment variables & connection references)
- TC-DG-SC-01..05 (US-DG-6, solution version / managed-state paths)
- TC-DG-SC-06..10 (US-DG-6, metadata-level schema conflicts — attribute type mismatch, string-length
  shrink, choice value label/removal conflicts, relationship schema-name collision; seeded via `MetaBuilder`)
- TC-DG-FP-01..10 (US-DG-5, cloud-flow draft state & missing connection references;
  duplicate SDK-step registrations & execution-rank conflicts)
- TC-DG-DC-01..07 (US-DG-2.3, managed-upgrade component deletions)
- TC-DG-PP-01..07 (US-DG-7, Power Pages web roles / table permissions / web files / snippets)
- TC-DG-DEP-01..04 (US-DG-2/3, missing dependencies & managed state)
- TC-DG-FM-01..04 (US-DG-11, form scripts/controls referencing missing web resources)
- TC-DG-RB-01..03 (US-DG-12, ribbon commands referencing missing web resources)

The plugin-step conflict checks (TC-DG-FP-05..10) read only the step's own columns, so they run in
the automated suite. Schema attribute/relationship comparisons are now seeded via a reflection
`EntityMetadata` builder (`Fakes/MetaBuilder.cs`) and run headlessly too (TC-DG-SC-06..10). Remaining
paths needing aliased LEFT-OUTER joins or richer metadata (plugin-step *health* — missing
type/assembly/target table — security role/field metadata, publisher `Retrieve`, duplicate layers)
remain manual - see TC-DG-M-*.

```
Passed!  - Failed: 0, Passed: 54, Skipped: 0, Total: 54
```

### 3. Report exporters

- **Command:** `dotnet test testing/ReportTests/ReportTests.csproj`
- **Framework:** xUnit on net48; compiles `Reporting/ReportExporters.cs` directly with the tool's
  export dependencies (ClosedXML, PdfSharp/MigraDoc-GDI, Newtonsoft) — no WinForms/XrmToolBox host.
- **Result:** **6 passed, 0 failed, 0 skipped.**
- **Covers:** TC-DG-RPT-01..06 — the native **PDF** exporter renders a valid `%PDF-` document
  through MigraDoc/PdfSharp (GDI, fonts resolved via GDI+); HTML, JSON (CI gating), Markdown
  (rollback guidance), and Excel (OOXML/ZIP) each produce a well-formed payload; and an executive
  summary, when present, is embedded in the PDF/HTML/JSON outputs.

```
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

The GUI export flow itself (SaveFileDialog → open) stays manual — see TC-DG-M-11..15.

## Manual run

- **Status:** **Not executed in this environment.** The analyzer, connection, and export cases
  (TC-DG-M-01..16) require a live Dataverse connection and the XrmToolBox host, which are not
  available here.
- **Action for a Windows/XTB session:** work through `TEST_CASES.md`, mark Pass/Fail, and save one
  screenshot per case into `screenshots/`. Update the table below when done.

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated - scoring/banding | 13 | 13 | 13 | 0 | 0 |
| Automated - deployment summary | 7 | 7 | 7 | 0 | 0 |
| Automated - analyzer logic (fake conn) | 54 | 54 | 54 | 0 | 0 |
| Automated - report exporters | 6 | 6 | 6 | 0 | 0 |
| Manual - connections | 3 | 0 | 0 | 0 | 3 |
| Manual - analyzers | 7 | 0 | 0 | 0 | 7 |
| Manual - exports | 6 | 0 | 0 | 0 | 6 |

## Verdict

Automated scoring logic, the deployment-summary payload/offline generator, the query-driven paths of
eight analyzers (environment variables, schema version/managed-state, flows & plugin conflicts, deleted
components, forms, ribbons, Power Pages, dependencies), **and** all report exporters
(PDF/HTML/JSON/Markdown/Excel + summary embedding) are **verified and green** (80 automated cases). Full sign-off is pending the manual GUI/Dataverse cases (connections, metadata-level
comparisons, the GUI export flow), which must be run in a Windows + XrmToolBox environment against a real org.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-04.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Deployment Risk Analyzer** loads and appears in the Tools list.
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Deployment Risk Analyzer** v1.2026.7.1 (Kanchan Kora).
