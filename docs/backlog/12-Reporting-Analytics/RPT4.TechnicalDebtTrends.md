# Technical Debt Trends — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 12 (Reporting & Analytics), item 4. Not in pack file.
> **Suggested tag:** `RPT4` · **Suggested project:** `XrmToolSuite.TechnicalDebtTrends`
> **Overlaps:** STRONG overlap with the SHIPPED Technical Debt Analyzer — this is NOT a new scanner. It is the snapshot/trend/comparison layer over that tool's existing UI-free analyzer engine. NOTE clearly: reuse the shipped analyzers (unused/duplicate metadata, deprecated JS, plugin risk, form/view complexity, documentation completeness, unmanaged customizations) verbatim; add only local snapshots, trend calc, and the diff engine. Strongly consider folding this into the shipped tool as a "Trends" tab instead of a separate project.
> **Value/priority (my read):** High — "is debt getting better or worse across releases" is exactly what MGRs and ARCHs ask, and the scanning is already done, so the incremental build is small (snapshot store + diff). Best ROI in this track. Decide up front: separate tool vs. tab on the shipped analyzer.

## Notes
- Reuses the shipped Technical Debt Analyzer's UI-free engine to produce a scan; adds nothing to the scanning rules. New code is: local snapshot schema, trend series calc, and a two-snapshot comparison (diff) engine.
- Snapshot schema (local, no credentials): timestamp, environment id, overall debt score, counts by severity (Critical/High/Medium/Low/Info), and per-category counts (unused metadata, duplicate metadata, deprecated JavaScript, plugin risk, form/view complexity, documentation completeness, unmanaged customizations).
- Trend calc: per-category and overall series over saved snapshots; comparison engine classifies each finding as New, Resolved, or Recurring between two snapshots.
- Keep snapshot, trend, and diff logic UI-free and unit-testable (deterministic given two snapshot files) — these are pure functions ideal for testing/UnitTests/.
- Charts (debt-score line, findings-by-severity stacked) as inline SVG/PNG for self-contained exports; snapshot manager UI to name/select/delete snapshots.
- Read-only; the scan itself runs off the UI thread via RunAsync/WorkAsync with progress + cancellation; trend/diff over saved snapshots is local and instant.

---

## EPIC-RPT4 — Show whether technical debt is improving or worsening across releases
> **As** an MGR / ARCH, **I want** technical debt tracked over time with new/resolved/recurring breakdowns, **so that** I can support governance, roadmap planning, and executive reporting with a trend, not a point-in-time number.

**Outcome:** local scan snapshots, a debt-score trend chart, findings-trend charts, a new/resolved/recurring panel, a category breakdown, and an exportable trend report.

---

## FEAT-RPT4-1 — Scan & snapshot capture `[Planned]`
- **US-RPT4.1.1** `[Planned]` **As** an ARCH, **I want** to run a technical debt scan reusing the shipped analyzer engine and save it as a local snapshot, **so that** I build a history without re-implementing scanning.
  - **AC:** Scan runs off the UI thread with progress and cancellation; snapshot persists locally with no credentials or record data, only aggregate counts/scores.
- **US-RPT4.1.2** `[Planned]` **As** a MAKER, **I want** a snapshot manager to name, select, and delete snapshots, **so that** I control my local history.

## FEAT-RPT4-2 — Debt score trend `[Planned]`
- **US-RPT4.2.1** `[Planned]` **As** an MGR, **I want** the overall debt score plotted over saved snapshots, **so that** I can see the direction of travel.
  - **AC:** Trend chart renders as inline SVG/PNG; deterministic from the snapshot series.

## FEAT-RPT4-3 — Findings trend by severity & category `[Planned]`
- **US-RPT4.3.1** `[Planned]` **As** an ARCH, **I want** Critical/High/Medium/Low/Info counts trended over time, **so that** I can see if severe debt is falling.
- **US-RPT4.3.2** `[Planned]` **As** an ARCH, **I want** per-category trends (unused/duplicate metadata, deprecated JS, plugin risk, form/view complexity, documentation, unmanaged customizations), **so that** I know which debt categories move.
  - **AC:** Category series are computed purely from snapshot fields; missing categories in older snapshots render as gaps, not zero.

## FEAT-RPT4-4 — Snapshot comparison (diff) `[Planned]`
- **US-RPT4.4.1** `[Planned]` **As** an ARCH, **I want** to compare two snapshots and classify findings as New, Resolved, or Recurring, **so that** I know what changed between releases.
  - **AC:** The diff engine is UI-free, deterministic given two snapshots, and unit-tested.

## FEAT-RPT4-5 — Export `[Planned]`
- **US-RPT4.5.1** `[Planned]` **As** an MGR, **I want** to export the technical debt trend report to Excel/PDF/CSV/HTML, **so that** I can present debt direction to leadership.
  - **AC:** Export runs off the UI thread with progress; trend charts embedded as SVG/PNG.

## Definition of Done
- Follows suite conventions; read-only default; reuses the shipped Technical Debt Analyzer engine (no duplicate scanning rules); snapshots stored locally (no credentials); snapshot/trend/diff logic UI-free and unit-tested; export formats: Excel, PDF, CSV, HTML, JSON, PNG, SVG.
- Testing skeleton under testing/TechnicalDebtTrends/ when implementation starts.
