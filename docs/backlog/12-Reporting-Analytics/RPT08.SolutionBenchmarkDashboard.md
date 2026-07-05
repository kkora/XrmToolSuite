# Solution Benchmark Dashboard — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 12 (Reporting & Analytics), item 8. Not in pack file.
> **Suggested tag:** `RPT08` · **Suggested project:** `XrmToolSuite.SolutionBenchmarkDashboard`
> **Overlaps:** STRONG overlap with the SHIPPED Solution Complexity Score — that scores a single solution; this benchmarks MANY solutions side-by-side against configurable standards and flags outliers. NOTE clearly: reuse Solution Complexity Score's UI-free scoring engine as the per-solution metric source; add only the multi-solution comparison, standards/thresholds, and outlier detection. Also pulls tech-debt/dependency signals from the shipped Technical Debt Analyzer and Deployment Risk Analyzer.
> **Value/priority (my read):** Medium-High — "does this solution meet our bar, and which is the worst" is a real ARCH/MGR need at migration/handoff gates, and most per-solution scoring already exists, so the build is mostly comparison + thresholds. Value hinges on de-duping cleanly with Solution Complexity Score.

## Notes
- Benchmarking layer over existing per-solution scores: reuses the shipped Solution Complexity Score engine (complexity/quality), Technical Debt Analyzer (debt), and Deployment Risk Analyzer (dependency/deployment risk) per selected solution; adds multi-solution comparison and standards.
- Configurable enterprise standards/thresholds per metric (component counts, complexity, quality, dependency risk, technical debt, documentation completeness, plugin/flow usage, security role complexity, env variable/connection reference readiness); pass/warn/fail bands per standard.
- Benchmark score is a deterministic weighted roll-up per solution vs the standard; outlier detection flags best/worst and out-of-band solutions across the selected set — keep the rules/scoring engine UI-free and unit-testable.
- Signals: `solution`, `solutioncomponent` (component counts), plus reused analyzer engines; env variables/connection references via `environmentvariabledefinition`/`connectionreference` — all via Service.RetrieveAll off the UI thread with targeted ColumnSets.
- Local snapshot storage (no credentials) so benchmarks can be re-run and compared; side-by-side grid + category charts as inline SVG/PNG for self-contained exports.
- Read-only; scoring multiple solutions is heavy — run sequentially via RunAsync/WorkAsync with progress + cancellation; a failed metric degrades to Info, never aborts the benchmark.

---

## EPIC-RPT08 — Benchmark solutions against enterprise standards and surface outliers
> **As** an ARCH / MGR, **I want** to compare solutions against configurable quality/complexity/ALM/security standards and identify outliers, **so that** I can gate deployment, migration, and handoff on measurable criteria.

**Outcome:** a solution selector, a benchmark dashboard with score cards and category charts, a side-by-side comparison grid, an outlier panel, recommendations, local snapshots, and an exportable benchmark report.

---

## FEAT-RPT08-1 — Solution selection & metric collection `[Planned]`
- **US-RPT08.1.1** `[Planned]` **As** an ARCH, **I want** to select one or more solutions and collect their metrics, **so that** I can benchmark them together.
  - **AC:** Collection runs off the UI thread with progress and cancellation; per-solution metrics reuse the shipped Solution Complexity / Technical Debt / Deployment Risk engines.
- **US-RPT08.1.2** `[Planned]` **As** a TOOLDEV, **I want** the benchmark rules/scoring engine kept UI-free, **so that** it stays deterministic and unit-testable.

## FEAT-RPT08-2 — Configurable standards `[Planned]`
- **US-RPT08.2.1** `[Planned]` **As** an ARCH, **I want** enterprise thresholds per metric (component counts, complexity, quality, dependency risk, tech debt, documentation, plugin/flow usage, security role complexity, env var/connection reference readiness), **so that** benchmarks reflect our bar.
  - **AC:** Standards/thresholds round-trip via settings load/save; each metric yields a pass/warn/fail band.

## FEAT-RPT08-3 — Benchmark score & cards `[Planned]`
- **US-RPT08.3.1** `[Planned]` **As** an MGR, **I want** a benchmark score per solution with category score cards and charts, **so that** I can judge each solution against the standard.
  - **AC:** Score is deterministic and explainable from the metrics; bands use Critical/High/Medium/Low/Info.

## FEAT-RPT08-4 — Side-by-side comparison & outliers `[Planned]`
- **US-RPT08.4.1** `[Planned]` **As** an ARCH, **I want** a side-by-side comparison grid across selected solutions, **so that** I can compare them directly.
- **US-RPT08.4.2** `[Planned]` **As** an MGR, **I want** best/worst and out-of-band solutions flagged in an outlier panel, **so that** I know which solutions need attention.
  - **AC:** Outlier detection is deterministic across the selected set.

## FEAT-RPT08-5 — Recommendations & snapshots `[Planned]`
- **US-RPT08.5.1** `[Planned]` **As** an ARCH, **I want** recommendations for out-of-band metrics plus locally saved benchmarks, **so that** I know what to fix and can re-compare later.
  - **AC:** Recommendations link to the failing metric; snapshots persist locally with no credentials.

## FEAT-RPT08-6 — Export `[Planned]`
- **US-RPT08.6.1** `[Planned]` **As** an MGR, **I want** to export the benchmark report to Excel/PDF/CSV/HTML, **so that** I can present solution comparisons at gates.
  - **AC:** Export runs off the UI thread with progress; comparison grid and charts embedded.

## Definition of Done
- Follows suite conventions; read-only default; reuses shipped Solution Complexity / Technical Debt / Deployment Risk engines (no duplicate scoring); snapshots stored locally (no credentials); rules/scoring engine UI-free and unit-tested; export formats: Excel, PDF, CSV, HTML, JSON, PNG, SVG.
- Testing skeleton under testing/SolutionBenchmarkDashboard/ when implementation starts.
