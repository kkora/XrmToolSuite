# Design — SOLN4 & RPT4 as extensions of shipped tools

> **Status:** approach approved (decisions locked below); implementation not started.
> Both are built by **extending a shipped tool**, not as new `src/Tools/` projects (per the NEXT-20
> "deliberately deferred" note). Neither adds a Dataverse write path.

Two facts that shape the design (verified in the code):
- Neither `SolutionComplexityScoreControl` nor `TechnicalDebtAnalyzerControl` uses a `TabControl` — both
  are single-panel dashboards today.
- `ComponentCounts` (the Complexity engine's input) carries only **structural tallies** (tables, columns,
  forms, views, plugin steps, flows/workflows/business rules, JS web resources, PCFs, custom APIs,
  dashboards, apps, widest form). It has **no** naming-prefix / description-coverage / managed-layer signals.

---

## SOLN4 — Solution Quality Score (extend Solution Complexity Score)

A sibling **quality grade** computed alongside the existing complexity score, over the **same** collected
`ComponentCounts` — no new Dataverse queries. Complexity answers "how big/hard"; Quality answers
"how well-built" against best-practice heuristics.

**Engine (SDK-free, unit-tested)** — new `Analysis/QualityScore.cs`:
- `QualityResult Compute(ComponentCounts counts, ComplexityResult complexity)` → 0–100 quality score,
  a letter band (A–F), and a list of **deductions** (`{ signal, points, why }`).
- Signals derived from existing counts only (no collector change):
  - Oversized forms (`WidestForm` past thresholds).
  - Plugin-step density (steps per table).
  - Automation sprawl (flows + workflows + business rules relative to tables).
  - Client-script heaviness (JS web resources).
  - Legacy ratio (classic `Workflows` share vs modern `Flows`).
  - Data-model sprawl (tables, columns-per-table).
  - Positive base from `ComplexityResult.MaintainabilityScore`.
- Bands (tunable): A ≥90 · B ≥80 · C ≥70 · D ≥60 · F <60.

**Report / exports:** extend the existing `ComplexityReport.Build` (or a small `QualityReport`) to add the
quality grade as a metric + each deduction as a `Finding`, so the **same** Excel/PDF/HTML/JSON exporters
already wired carry it. No new exporters, no new deps.

**UI:** stays single-panel — add the quality grade + deduction list into the existing dashboard (a second
headline number beside the complexity gauge). No `TabControl` needed.

**Deferred to phase-2 (needs a collector change):** naming-prefix consistency, description coverage, and
managed/unmanaged layering are better quality signals but require extending `ComplexityCollector` /
`ComponentCounts`. Flagged, not in v1.

**Testing:** xUnit cases in `testing/UnitTests` for `QualityScore.Compute` (banding cutoffs, each deduction,
score caps, empty solution); update the shipped tool's TEST_PLAN/CASES/SUMMARY + user stories. No new project.

**Effort:** small–medium. Fully SDK-free-testable; no persistence; no write path.

---

## RPT4 — Technical Debt Trends ("Trends" tab on Technical Debt Analyzer)

Charts the debt score run-over-run. **Trends need history and Dataverse doesn't retain past scores**, so the
tool persists its own snapshots. **Decided storage: local JSON snapshots** (per-machine; no org writes).

**Storage:** each completed scan appends a snapshot to a per-environment JSON history file in app-data
(e.g. `%AppData%\MscrmTools\XrmToolBox\Settings\XrmToolSuite.TechnicalDebtAnalyzer.trends.json`) — kept out
of the small XTB settings blob. Read-only against Dataverse.

**Engine (SDK-free, unit-tested):**
- `Trends/DebtSnapshot.cs` — POCO `{ TimestampUtc, EnvironmentName, Score, Band, TotalFindings, CategoryCounts }`.
- `Trends/TrendStore.cs` — **pure** list logic: parse/serialize JSON, `Append(snapshot, capN, dedupe)`,
  cap history per environment (e.g. last 100), query by environment. File I/O is a thin wrapper around the
  pure list ops so the logic is unit-testable.
- `Trends/TrendAnalytics.cs` — deltas vs previous run (score + per-category), direction (improving/worsening),
  best/worst run, and the score-over-time series.

**Capture:** build a `DebtSnapshot` from the run's `ReportModel` (score, band, findings-by-category) and
append after each scan completes.

**UI:** introduce a `TabControl` — **tab 1** = the existing dashboard (unchanged), **tab 2 = Trends**: a runs
grid (date, score, band, total, per-category), a **GDI-drawn** score-over-time line chart (no external
charting dependency — the same pure-GDI approach the suite already uses for PNG output), a "since last run"
delta banner, and a confirmation-gated **Clear history** action. Scoped to the connected environment.

**Exports:** a trends CSV/JSON of the snapshot series (BCL/Newtonsoft — no new deps).

**Testing:** xUnit cases for `TrendStore` (append / cap / dedupe-same-run / round-trip / per-env isolation)
and `TrendAnalytics` (deltas, direction, empty & single-run edges); update the shipped tool's
TEST_PLAN/CASES/SUMMARY + user stories. The tab + chart are manual cases.

**Effort:** medium. Store/analytics are SDK-free-testable; the `TabControl` restructure + GDI chart are the
manual UI work.

---

## Cross-cutting
- No new `src/Tools/` project and no NEXT-20 row — both ship inside their parent tool. Update NEXT-20.md's
  "deliberately deferred" note to record they shipped as extensions, and the backlog SOLN4/RPT4 entries.
- Add user stories for the new features under each shipped tool's story file; refresh testing artifacts.
- Version bump in the root `Directory.Build.props` per release (your call on timing).

## Decisions (locked)
1. **SOLN4 grade** — a **0–100 score + Low/Med/High band** (the suite's standard `ScoreCalculator` banding),
   not an A–F letter grade. Consistent with complexity/debt; the existing gauge and exporters speak it.
2. **RPT4 layout** — introduce a **`TabControl`** into Technical Debt Analyzer: tab 1 = existing dashboard
   (unchanged), tab 2 = Trends.
3. **RPT4 chart** — **GDI-drawn** score-over-time line chart (`System.Drawing`), no external/GAC charting
   dependency.
4. **RPT4 history** — **local per-environment JSON snapshots** in app-data (no org writes).
