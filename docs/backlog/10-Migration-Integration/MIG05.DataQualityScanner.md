# Data Quality Scanner — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 10 (Migration & Integration), item 5. Not in pack file.
> **Suggested tag:** `MIG05` · **Suggested project:** `XrmToolSuite.DataQualityScanner`
> **Overlaps:** Touches SEC07 'Sensitive Data Scanner' (both scan record data with masking) and MIG06 'Duplicate Record Analyzer' (duplicate values) — different focus: this is broad column-level quality (formats, blanks, orphans, stale, placeholders), MIG06 is dedicated duplicate grouping. Share the safe row-sampling + masking helpers.
> **Value/priority (my read):** Medium-High — poor data is a top cause of migration failure and reporting distrust; a *safe* (read-only, sampled, masked) scanner is broadly useful, but the data-touching nature demands careful sampling/masking to be shippable.

## Notes
- Data sources: record data in selected tables/columns via **sampled** `Service.RetrieveAll`/paged queries with a default row cap; Attribute metadata for required levels, formats (email/phone/URL), and option-set definitions; `owningbusinessunit`/`ownerid` and `statecode`/`modifiedon` for owner/stale checks.
- **Safety is the headline:** read-only by default; row sampling limited by a configurable default cap; sensitive values masked in the sample panel and all exports; long/large scans require an explicit confirmation dialog stating table(s) and estimated row volume before running.
- Rules engine is UI-free and unit-testable: each rule (missing-required, bad-format, invalid-date-range, duplicate-candidate, orphan-lookup, stale, missing-owner/BU, inconsistent-casing, leading/trailing-space, placeholder value, invalid choice) takes a value/record and emits a typed finding; no Dataverse/WinForms dependency so rules lift into console/CI.
- Query strategy respects service protection limits: targeted `ColumnSet`s (never `ColumnSet(true)`), paged retrieval, no parallel `ExecuteMultiple`; runs off the UI thread via `RunAsync`/`WorkAsync` with progress + cancellation.
- Scan configuration (tables, columns, rule toggles, sample cap, masking on) round-trips via Load/SaveSettings; never persist scanned data or credentials.

---

## EPIC-MIG05 — Safely scan Dataverse data quality across selected tables and columns
> **As** an **ADM**, **I want** a configurable, read-only data-quality scan of chosen tables that samples safely and masks sensitive values, **so that** I can find and prioritize cleanup before it breaks migration, reporting, or business processes.

**Outcome:** a data-quality dashboard, a findings grid by rule/severity, a masked sample-records panel, a data-quality score, cleanup recommendations, and a masked exportable report — all read-only with limited sampling.

---

## FEAT-MIG05-1 — Scope and configure a safe scan `[Planned]`
- **US-MIG05.1.1** `[Planned]` **As** an ADM, **I want** to select tables, columns, and which rules to run, **so that** I scan only what matters.
  - **AC:** Selection drives targeted `ColumnSet`s; disabled rules are not evaluated; configuration round-trips via settings.
- **US-MIG05.1.2** `[Planned]` **As** an ADM, **I want** a configurable row-sampling cap and a confirmation before large scans, **so that** I never accidentally run a long/heavy scan.
  - **AC:** Default sample cap applies unless raised; a confirmation dialog stating table(s) and estimated row volume is required before a scan exceeding the cap; scan runs off the UI thread with progress/cancellation.

## FEAT-MIG05-2 — Detect value-level quality issues `[Planned]`
- **US-MIG05.2.1** `[Planned]` **As** an ADM, **I want** missing required values, invalid email/phone/URL formats, and invalid date ranges detected, **so that** malformed data is surfaced.
  - **AC:** Format rules validate against attribute type/format metadata; each violation is a typed finding with severity; the rules engine is UI-free and unit-testable.
- **US-MIG05.2.2** `[Planned]` **As** an ADM, **I want** inconsistent casing, leading/trailing spaces, and placeholder values (test, n/a, unknown, dummy) detected, **so that** dirty text data is surfaced.
  - **AC:** Placeholder list is configurable; casing/whitespace/placeholder violations are separate finding types.
- **US-MIG05.2.3** `[Planned]` **As** an ADM, **I want** invalid choice values and duplicate candidate values detected where possible, **so that** reference-data problems are surfaced.
  - **AC:** Choice values not in the current option set and repeated candidate values are findings; masked in output.

## FEAT-MIG05-3 — Detect record-level quality issues `[Planned]`
- **US-MIG05.3.1** `[Planned]` **As** an ADM, **I want** orphan lookup references and records missing owner/business unit detected, **so that** relational and ownership gaps are surfaced.
  - **AC:** Orphan detection resolves lookups where feasible and degrades to informational findings otherwise; missing owner/BU is a finding.
- **US-MIG05.3.2** `[Planned]` **As** an ADM, **I want** inactive/stale records detected, **so that** obsolete data is flagged before migration.
  - **AC:** Stale threshold (by `modifiedon`/`statecode`) is configurable; stale records are counted per table.

## FEAT-MIG05-4 — Review masked samples, score, and export `[Planned]`
- **US-MIG05.4.1** `[Planned]` **As** an ADM, **I want** a findings grid and a masked sample-records panel, **so that** I can review issues without exposing sensitive data.
  - **AC:** Grid filters by table/rule/severity; sample panel masks sensitive values by default; masking cannot be silently disabled in exports.
- **US-MIG05.4.2** `[Planned]` **As** an **MGR**, **I want** a data-quality score and cleanup recommendations, **so that** I can prioritize remediation.
  - **AC:** Score is a UI-free weighted roll-up with severities Critical/High/Medium/Low/Info; recommendations are ordered by severity.
- **US-MIG05.4.3** `[Planned]` **As** an ADM, **I want** a masked exported data-quality report, **so that** I can share findings safely.
  - **AC:** Exports to Excel, PDF, CSV, JSON, and self-contained HTML run off the UI thread with sensitive values masked; read-only.

## Definition of Done
- Follows suite conventions; **read-only default; row sampling limited by a default cap; sensitive values masked in samples and all exports; large scans gated by a confirmation dialog.**
- Rules engine UI-free/unit-testable and liftable to console/CI; export formats Excel, PDF, CSV, JSON, HTML; settings round-trip without persisting scanned data.
- Testing skeleton under testing/DataQualityScanner/ when implementation starts.
