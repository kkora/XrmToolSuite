# Deployment Risk Analyzer - Test Summary

## Automated run

Two automated projects, both green:

### 1. Risk scoring & banding (SDK-free)

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Framework:** xUnit on net8.0 (SDK-free logic compiled directly; no Dataverse/WinForms).
- **Result:** **24 passed, 0 failed, 0 skipped** (scoring/banding + deployment-summary logic).
- **Duration:** ~0.1 s.
- **Evidence:** [`test-run.txt`](test-run.txt).

Covers all risk-scoring/banding cases TC-ALM07-SCORE-01..05, TC-ALM07-BAND-06..11, TC-ALM07-EXPLAIN-12,
TC-ALM07-SUMMARY-13 - i.e. the full acceptance criteria of **US-ALM07-8.1** (weights, cap at 100, Critical
forces High, Medium/High thresholds).

Also covers deployment-summary logic TC-ALM07-SUM-01..05 (**US-ALM07-8**): the anonymized payload builder
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
- TC-ALM07-EV-01..09 (US-ALM07-4, environment variables & connection references)
- TC-ALM07-SC-01..05 (US-ALM07-6, solution version / managed-state paths)
- TC-ALM07-SC-06..10 (US-ALM07-6, metadata-level schema conflicts — attribute type mismatch, string-length
  shrink, choice value label/removal conflicts, relationship schema-name collision; seeded via `MetaBuilder`)
- TC-ALM07-FP-01..10 (US-ALM07-5, cloud-flow draft state & missing connection references;
  duplicate SDK-step registrations & execution-rank conflicts)
- TC-ALM07-DC-01..07 (US-ALM07-2.3, managed-upgrade component deletions)
- TC-ALM07-PP-01..07 (US-ALM07-7, Power Pages web roles / table permissions / web files / snippets)
- TC-ALM07-DEP-01..04 (US-ALM07-2/3, missing dependencies & managed state)
- TC-ALM07-FM-01..04 (US-ALM07-11, form scripts/controls referencing missing web resources)
- TC-ALM07-RB-01..03 (US-ALM07-12, ribbon commands referencing missing web resources)

The plugin-step conflict checks (TC-ALM07-FP-05..10) read only the step's own columns, so they run in
the automated suite. Schema attribute/relationship comparisons are now seeded via a reflection
`EntityMetadata` builder (`Fakes/MetaBuilder.cs`) and run headlessly too (TC-ALM07-SC-06..10). Remaining
paths needing aliased LEFT-OUTER joins or richer metadata (plugin-step *health* — missing
type/assembly/target table — security role/field metadata, publisher `Retrieve`, duplicate layers)
remain manual - see TC-ALM07-M-*.

```
Passed!  - Failed: 0, Passed: 54, Skipped: 0, Total: 54
```

### 3. Report exporters

- **Command:** `dotnet test testing/ReportTests/ReportTests.csproj`
- **Framework:** xUnit on net48; compiles `Reporting/ReportExporters.cs` directly with the tool's
  export dependencies (ClosedXML, PdfSharp/MigraDoc-GDI, Newtonsoft) — no WinForms/XrmToolBox host.
- **Result:** **6 passed, 0 failed, 0 skipped.**
- **Covers:** TC-ALM07-RPT-01..06 — the native **PDF** exporter renders a valid `%PDF-` document
  through MigraDoc/PdfSharp (GDI, fonts resolved via GDI+); HTML, JSON (CI gating), Markdown
  (rollback guidance), and Excel (OOXML/ZIP) each produce a well-formed payload; and an executive
  summary, when present, is embedded in the PDF/HTML/JSON outputs.

```
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

The GUI export flow itself (SaveFileDialog → open) stays manual — see TC-ALM07-M-11..15.

## Manual run

- **Status:** **Not executed in this environment.** The analyzer, connection, and export cases
  (TC-ALM07-M-01..16) require a live Dataverse connection and the XrmToolBox host, which are not
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
