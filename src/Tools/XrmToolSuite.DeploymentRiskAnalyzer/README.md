# 🛡 Deployment Risk Analyzer

An **XrmToolBox** plugin that analyzes a solution *before* deployment and produces a
deployment-readiness risk report: a Low/Medium/High risk score, missing dependencies,
schema conflicts against a target environment, environment-variable and connection-reference
gaps, flow/plugin readiness, security coverage gaps, Power Pages readiness, and rollback guidance —
exportable as a **native PDF**, an **HTML dashboard**, Excel, CI-friendly JSON, or a Markdown fix checklist.

## Features

| Analyzer | What it checks |
|---|---|
| **Solution Dependencies** | Missing required components (`RetrieveMissingDependencies`), prerequisite managed solutions, publisher prefix/option-value-prefix collisions, components duplicated across unmanaged solutions, unmanaged deployment warnings |
| **Environment Variables & Connection References** | Definitions with no default/current value, secret variables (Key Vault) that must be configured per environment, values accidentally packaged into the solution, unbound/missing connection references in the target |
| **Flows & Plugins** | Draft (OFF) cloud flows/processes, flows referencing non-existent connection references (parsed from `clientdata`), plugin steps with missing plugin types/assemblies, disabled steps, steps targeting tables absent from the target, duplicate SDK-step registrations (same type + overlapping filters on one event), and execution-rank conflicts (different types sharing a rank) |
| **Security Impact** | New custom tables with no role coverage in the solution, secured columns without field security profiles, roles assigned to no user/team in the target |
| **Data Model Conflicts** *(target required)* | Attribute type mismatches (import-breaking), string max-length reductions, choice value label conflicts and removed values, 1:N / N:N relationship schema-name collisions, solution version not incremented, managed/unmanaged mismatch |
| **Deleted Components** *(target required)* | Components in the target's installed solution but absent from the source — deleted on a managed upgrade. Removed tables → Critical (data loss), columns → High, other components → Medium; unmanaged target reported as informational drift |
| **Power Pages Readiness** | Web role defaults (anonymous/authenticated), tables surfaced by basic forms/lists without table permissions, forms bypassing table permissions, web files without content, empty content snippets, baseline site settings, cache-refresh checklist. Supports both `adx_` and `mspp_` (enhanced data model) schemas |

**Risk score:** Critical=25, High=12, Medium=5, Low=2 points, capped at 100.
≥40 (or any Critical) → **High**, ≥15 → **Medium**, else **Low**. Tunable in `Scoring/RiskScoreCalculator.cs`.

## Exports

Five report formats, all from the **Export** toolbar button (available once an analysis has run):

| Format | What you get |
|---|---|
| **PDF report (executive)** | A **native** PDF rendered directly with MigraDoc/PdfSharp (no browser needed) — a dashboard-style cover with a radial score gauge, colour-coded risk banner, risk-categories and recommendations tables, next-steps block, executive summary, and findings grouped by category. Fully unattended. |
| **HTML report** | A self-contained, theme-aware (light/dark) **dashboard**: radial gauge, severity KPI cards, risk categories, top issues, recommendations, next steps, and full findings detail. No external CSS/JS/fonts — opens offline; **Print → Save as PDF** in any browser is pixel-identical. |
| **Excel workbook** | Summary / Findings / Fix Checklist worksheets for tracking remediation. |
| **JSON (CI/CD)** | Machine-readable payload with `ci.pass` + `suggestedExitCode` for pipeline gating. |
| **Fix checklist (Markdown)** | An ordered, actionable checklist that always ends with rollback guidance. |

The native PDF and the HTML dashboard share the same derived data (risk-category scores, friendly
category names) so both surfaces stay in sync. If an executive deployment summary has been
generated (offline template or opt-in AI), it is embedded in the PDF, HTML, and JSON exports.

## Help & Support

A **Help** button (right of the toolbar) opens a Help & Support dialog with:

- **Documentation** — links to this README on GitHub
- **Report an issue** — opens a new GitHub issue
- **Buy me a coffee** — support future updates

The tool also implements `IGitHubPlugin` and `IHelpPlugin`, so XrmToolBox's own tool-menu links
(repository, help) resolve to the same GitHub project.

## Build

Prereqs: Visual Studio 2022 (or `dotnet build` with the .NET Framework 4.8 developer pack) on Windows.

```bash
git clone <your-repo>/XrmToolSuite
cd XrmToolSuite
dotnet build XrmToolSuite.sln -c Release
# or with local deploy to XrmToolBox:
dotnet build -p:DeployToXTB=true
```

The project targets **net48** (XrmToolBox requirement) and references:

- `XrmToolBoxPackage` — extensibility base classes + Dataverse SDK assemblies
- `Newtonsoft.Json` — flow `clientdata` parsing and JSON export
- `ClosedXML` — Excel export
- `PDFsharp-MigraDoc-GDI` — native PDF export (GDI build: pure-managed, net48-compatible, no SkiaSharp natives)

A post-build step copies the DLL into `%AppData%\MscrmTools\XrmToolBox\Plugins`
if that folder exists, so F5 debugging works (set XrmToolBox.exe as the debug launch target).

## Install

1. Build in Release, or download the compiled `XrmToolSuite.DeploymentRiskAnalyzer.dll`.
2. Copy `XrmToolSuite.DeploymentRiskAnalyzer.dll` **and its full export dependency chains** into
   `%AppData%\MscrmTools\XrmToolBox\Plugins` — flat in the Plugins root, **not** a subfolder, or
   XrmToolBox silently drops the tool. Two chains ship next to the tool DLL: the Excel chain
   (ClosedXML + the OpenXml/SixLabors/`System.*` facade set) and the native-PDF chain
   (PdfSharp/MigraDoc GDI build, `-gdi` suffix). The complete DLL list and the reason it must sit
   next to the tool DLL are in
   [`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md) (Step 2). Easiest path:
   `dotnet build -p:DeployToXTB=true`, which copies both chains for you.
3. Restart XrmToolBox → open **Deployment Risk Analyzer**.

To publish on the official Tool Library, follow
[MscrmTools' plugin submission process](https://www.xrmtoolbox.com/documentation/for-developers/) —
this project implements `IGitHubPlugin` and `IHelpPlugin` (repository `kkora/XrmToolSuite`, help →
this README), configured in `DeploymentRiskAnalyzerControl.cs`.

## Usage

1. Connect to your **source** (dev) environment as usual.
2. Click **Load solutions**, pick the solution to be deployed.
3. *(Recommended)* Click **Connect target env…** and pick the destination (test/prod) connection —
   this unlocks schema-conflict checks, target env-var/connection-reference verification,
   version/downgrade detection, and role-assignment checks.
4. Tick the analyzers you want, hit **▶ Analyze**.
5. Review the grid (click a row for the full recommendation), then **Export**.

### CI/CD gating example

Export JSON and gate a pipeline stage on it:

```powershell
$r = Get-Content .\DeploymentRiskAnalyzer_MySolution.json | ConvertFrom-Json
if (-not $r.ci.pass) {
  Write-Error "Deployment Risk Analyzer risk = $($r.risk) (score $($r.score)). Blocking deployment."
  exit $r.ci.suggestedExitCode
}
```

(The UI tool produces the report; for fully headless runs, the analyzers only depend on
`IOrganizationService`, so `Analyzers/` + `Scoring/` + `Reporting/` can be lifted into a
console app or pac-cli wrapper unchanged.)

## Architecture

```
DeploymentRiskAnalyzerPlugin.cs        MEF entry point (PluginBase)
DeploymentRiskAnalyzerControl.cs       UI + orchestration (BaseToolControl (XrmToolSuite.Core), RunAsync)
Analyzers/
  AnalyzerContext.cs            IAnalyzer contract + shared caches/query helpers
  DependencyAnalyzer.cs
  EnvironmentVariableAnalyzer.cs
  FlowPluginAnalyzer.cs
  SecurityAnalyzer.cs
  SchemaConflictAnalyzer.cs
  DeletedComponentAnalyzer.cs
  FormAnalyzer.cs
  RibbonAnalyzer.cs
  PowerPagesAnalyzer.cs
Scoring/RiskScoreCalculator.cs       Deployment-risk facade over the shared ScoreCalculator (weighted score → Low/Medium/High)
Reporting/DeploymentReportModel.cs   Adapter: AnalysisResult → the shared ReportModel (branding, next steps, verdicts, release-manager AI prompt)
Models/RiskModels.cs                 Finding, severity, result types

Shared source (compiled in from src/Shared/, reused by every score/report tool in the suite):
  Core/Analysis/*      Finding, Severity/ScoreBand, IAnalyzer<T>, ScoreCalculator, ReportModel
  Reporting/*          JSON (+CI block) / Markdown / HTML dashboard / ClosedXML Excel / MigraDoc PDF / OpenXML Word exporters
  Summarization/*      offline templated + AI (Anthropic/OpenAI/Google/Ollama) executive-summary generators
```

Adding an analyzer = implement `IAnalyzer`, add one line to `_allAnalyzers` in the control.
Every analyzer is defensive: individual query failures degrade to informational findings
rather than aborting the run.

## Notes & limitations

- The AI executive summary is opt-in behind a consent preview; cloud providers (Anthropic/OpenAI/Google)
  use a **session-only** API key that is never persisted. **Local models (Ollama):** run it locally — no
  API key, nothing leaves the machine.
  1. **Install:** `winget install Ollama.Ollama` (or [ollama.com/download](https://ollama.com/download)).
     Ollama serves `http://localhost:11434` automatically; start it manually with `ollama serve` if needed.
  2. **Pull a model:** `ollama pull qwen2.5:7b` (also `gemma3:4b`, `llama3.2:3b`, `qwen2.5-coder:7b`).
     `ollama list` shows installed models, `ollama ps` shows loaded ones, `ollama run <model> "hi"` warms one up.
  3. **AI options ▸ Set API key… ▸ Provider = `Ollama (local)`**, **Model = `qwen2.5:7b`**, leave **API key** blank.
  The first request loads the model (waits up to 5 min); if Ollama isn't running it falls back to the
  offline summary. `nomic-embed-text` is an embedding model — use a chat model (`qwen2.5`/`gemma3`/`llama3.2`).
- **PDF export** is now **native** — rendered directly with the MigraDoc/PdfSharp **GDI** build
  (pure-managed, net48, no SkiaSharp natives), so no browser is required. The signature radial gauge
  is drawn as a PdfSharp `XGraphics` overlay (MigraDoc has no charting) and is decorative-only: any
  drawing failure degrades to a gauge-less PDF rather than failing the export. The HTML dashboard
  remains available and also prints to PDF from any browser. The MigraDoc/PdfSharp chain ships in the
  Plugins root alongside the ClosedXML chain (see `DEPLOYMENT.md`).
- Duplicate-layer detection samples up to 500 components to keep `IN` clauses sane on huge solutions.
- Privilege-name matching for role coverage uses an `EndsWith(schemaName)` heuristic that covers
  standard `prv{Action}{SchemaName}` privileges.
- Target comparisons match by **logical/schema name** (metadata IDs differ across environments by design).
- Requires System Customizer or higher in both environments for full results.
