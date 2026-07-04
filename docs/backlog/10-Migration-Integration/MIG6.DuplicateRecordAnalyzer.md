# Duplicate Record Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 10 (Migration & Integration), item 6. Not in pack file.
> **Suggested tag:** `MIG6` · **Suggested project:** `XrmToolSuite.DuplicateRecordAnalyzer`
> **Overlaps:** Touches MIG5 'Data Quality Scanner' (duplicate candidate values) — this tool is the dedicated duplicate-grouping engine (fuzzy/normalized matching, groups, confidence, side-by-side). Share MIG5's safe row-sampling + masking helpers. Complements Dataverse's built-in duplicate detection by handling enterprise matching scenarios it doesn't.
> **Value/priority (my read):** Medium — duplicate cleanup is a real pain and fuzzy matching adds value over native detection, but the recommendation-only (no-merge) safety stance limits it to analysis; still a solid, self-contained, testable slice (the matching engine).

## Notes
- Data sources: record data in the selected table via **sampled/paged** `Service.RetrieveAll` with a configurable cap over the chosen matching columns; Attribute metadata to interpret column types; alternate-key metadata for key-candidate matching; `statecode` to compare across active/inactive.
- **Safety is the headline:** read-only by default; **no automatic merge/delete** — the tool produces merge *recommendations* only; any optional safe-action mode is off by default, gated behind an explicit confirmation dialog stating scope/record count, and logs every action.
- Matching engine is UI-free and unit-testable: exact, fuzzy (edit-distance/similarity), and normalized matchers (phone, email, name, address) take normalized values and emit candidate pairs/groups with a confidence score; no Dataverse/WinForms dependency so it lifts into console/CI.
- Query strategy respects service protection limits: targeted `ColumnSet`s (never `ColumnSet(true)`), paged retrieval, blocking/keying to avoid O(n²) full comparison on large tables; runs off the UI thread via `RunAsync`/`WorkAsync` with progress + cancellation.
- Matching-rule configuration (table, columns, exact/fuzzy/normalized rules, thresholds, sample cap) round-trips via Load/SaveSettings; sensitive values masked in the comparison panel and exports; never persist scanned data or credentials.

---

## EPIC-MIG6 — Identify potential duplicate records with configurable matching (recommendations only)
> **As** an **ADM**, **I want** a read-only duplicate analysis using configurable exact/fuzzy/normalized matching that groups likely duplicates with a confidence score, **so that** I can review and plan de-duplication without any automatic merge or delete.

**Outcome:** duplicate groups with confidence scores, a side-by-side comparison panel, optional merge recommendations (never executed by default), and a masked exportable duplicate report.

---

## FEAT-MIG6-1 — Select table and build matching rules `[Planned]`
- **US-MIG6.1.1** `[Planned]` **As** an ADM, **I want** to select a table and matching columns, **so that** I control what defines a duplicate.
  - **AC:** Selection drives targeted `ColumnSet`s; a configurable sample cap applies; configuration round-trips via settings.
- **US-MIG6.1.2** `[Planned]` **As** an ADM, **I want** to configure exact, fuzzy, and normalized (phone/email/name/address) match rules with thresholds, **so that** I can tune matching to my data.
  - **AC:** Rule builder composes per-column matchers with weights/thresholds; the matching engine is UI-free and unit-testable.

## FEAT-MIG6-2 — Detect duplicate groups `[Planned]`
- **US-MIG6.2.1** `[Planned]` **As** an ADM, **I want** duplicate accounts, contacts, leads, or custom records detected, **so that** I find dupes in any table.
  - **AC:** Grouping works on any selected table via metadata-driven column handling; runs off the UI thread with progress/cancellation.
- **US-MIG6.2.2** `[Planned]` **As** an ADM, **I want** duplicates detected across active/inactive status and by alternate-key candidates, **so that** I catch dupes native detection misses.
  - **AC:** Cross-`statecode` grouping is optional; alternate-key-candidate matching uses key metadata; large tables use blocking/keying to avoid full O(n²) comparison.

## FEAT-MIG6-3 — Score confidence and compare `[Planned]`
- **US-MIG6.3.1** `[Planned]` **As** an ADM, **I want** a confidence score per duplicate group, **so that** I can triage high-confidence dupes first.
  - **AC:** Confidence is a UI-free weighted roll-up of per-column match strength with bands (e.g., High/Medium/Low); score is deterministic and unit-tested.
- **US-MIG6.3.2** `[Planned]` **As** an ADM, **I want** a side-by-side record comparison, **so that** I can verify a group is truly duplicate.
  - **AC:** Comparison panel shows matched records field-by-field; sensitive values are masked by default.

## FEAT-MIG6-4 — Recommend, safely act (opt-in), and export `[Planned]`
- **US-MIG6.4.1** `[Planned]` **As** an ADM, **I want** merge *recommendations* without any merge performed, **so that** I stay in control.
  - **AC:** By default the tool only recommends a survivor/merge strategy per group and performs no write; recommendation logic is read-only.
- **US-MIG6.4.2** `[Planned]` **As** an ADM, **I want** any optional safe-action mode to be explicitly opt-in, confirmed, and logged, **so that** destructive actions never happen silently.
  - **AC:** Safe-action mode is off by default; enabling it requires a confirmation dialog stating scope and record count before any write; every optional action is logged.
- **US-MIG6.4.3** `[Planned]` **As** an **MGR**, **I want** a masked exported duplicate report, **so that** I can share findings safely.
  - **AC:** Exports to Excel, PDF, CSV, JSON, and self-contained HTML run off the UI thread with sensitive values masked; read-only export.

## Definition of Done
- Follows suite conventions; **read-only default; no automatic merge/delete; recommendations only unless an explicit, confirmed, logged safe-action mode is enabled.**
- Matching engine UI-free/unit-testable and liftable to console/CI; row sampling limited; sensitive values masked; export formats Excel, PDF, CSV, JSON, HTML.
- Testing skeleton under testing/DuplicateRecordAnalyzer/ when implementation starts.
