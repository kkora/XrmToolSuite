# Duplicate Metadata Finder — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 4 (Dataverse Administration), item 3. Also in pack file (`prompt/2...`) idea #14 'Duplicate Metadata Finder' — same tool.
> **Suggested tag:** `ADMIN3` · **Suggested project:** `XrmToolSuite.DuplicateMetadataFinder`
> **Overlaps:** Feeds candidate **ADMIN2 (Metadata Cleanup Advisor)** — that tool should consume this tool's duplicate-group output rather than reimplement similarity matching. Partial overlap with the shipped **Attribute Auditor** (column inventory/usage) — reuse its metadata retrieval and usage evidence.
> **Value/priority (my read):** Medium — real technical-debt value in large multi-team orgs, but similarity scoring produces false positives; the group-and-recommend-primary workflow is the differentiator.

## Notes
- Data sources: `RetrieveMetadataChanges` (attribute display/schema names, data types, option sets), `systemform`/`savedquery`/`savedqueryvisualization`, `webresource` (name + content hash), `workflow` (business rules), `GlobalOptionSetMetadata`/local option sets, `sdkmessageprocessingstep` (steps), `RelationshipMetadataBase`.
- Similarity algorithm is UI-free and unit-testable: normalized display name + schema name (edit distance / token overlap), data type match, description similarity, option-value set overlap, and usage signals; produces a 0–100 similarity score per pair.
- Group duplicates into clusters; recommend a "primary to keep" using usage/dependency weight (most-referenced wins). Read-only — recommends only, never merges or deletes.
- JavaScript "duplicate function" detection is best-effort (text/hash heuristics on `webresource` content) — disclaim confidence.
- Pairwise comparison within a type is O(n²) — block by data type / entity and report progress; support cancellation.
- Names are logical/lowercase; prefer targeted `ColumnSet`s.

---

## EPIC-ADMIN3 — Surface duplicate and near-duplicate metadata so teams can consolidate
> **As** an **ARCH**, **I want** duplicate or near-duplicate components grouped with a similarity score and a recommended primary to keep, **so that** I can consolidate redundant metadata and cut confusion and reporting inconsistency.

**Outcome:** similarity-scored duplicate groups across component types, side-by-side comparison with usage/dependency impact, a recommended keep-candidate per group, and an exportable report.

---

## FEAT-ADMIN3-1 — Scan configuration and metadata retrieval `[Planned]`
- **US-ADMIN3.1.1** `[Planned]` **As** an ARCH, **I want** to configure which component types and scope to scan, **so that** I control run cost and noise.
  - **AC:** Type/scope selection loads via `RetrieveAll`/`RetrieveMetadataChanges` off the UI thread with progress and cancellation.
- **US-ADMIN3.1.2** `[Planned]` **As** an ARCH, **I want** a configurable similarity threshold, **so that** I can tune false positives vs. recall.
  - **AC:** Threshold round-trips via Load/SaveSettings; only pairs at/above threshold are grouped.

## FEAT-ADMIN3-2 — Detect duplicate columns and option sets `[Planned]`
- **US-ADMIN3.2.1** `[Planned]` **As** a MAKER, **I want** duplicate column display names and similar schema names detected, **so that** I find fields created twice by different teams.
  - **AC:** Findings key on normalized display/schema name similarity plus data-type match; each shows the contributing similarity factors.
- **US-ADMIN3.2.2** `[Planned]` **As** a MAKER, **I want** fields with the same data type and similar purpose, and duplicate/overlapping option sets, detected, **so that** semantic duplicates surface too.
  - **AC:** Option-set duplicates compare option-value sets; overlap ratio contributes to the score.

## FEAT-ADMIN3-3 — Detect duplicate UI and logic components `[Planned]`
- **US-ADMIN3.3.1** `[Planned]` **As** an ARCH, **I want** duplicate tables, forms, views, charts, and dashboards detected, **so that** redundant UI assets are grouped.
  - **AC:** Each type is a distinct duplicate category with its own similarity basis (name + structure signals).
- **US-ADMIN3.3.2** `[Planned]` **As** a TOOLDEV, **I want** duplicate business rules, web resources, JavaScript functions (best-effort), plugin steps, and relationships detected, **so that** duplicated logic is found.
  - **AC:** Web-resource/JS duplicates use content hash/text similarity with a confidence disclaimer; plugin-step duplicates key on message+entity+stage.

## FEAT-ADMIN3-4 — Group, compare, and recommend primary `[Planned]`
- **US-ADMIN3.4.1** `[Planned]` **As** an ARCH, **I want** duplicates grouped with a side-by-side comparison and usage/dependency impact, **so that** I can judge which to keep.
  - **AC:** Each group shows members, similarity scores, and per-member usage/dependency counts (via `RetrieveDependenciesForDelete`/usage evidence).
- **US-ADMIN3.4.2** `[Planned]` **As** an ARCH, **I want** a recommended primary component to keep per group, **so that** consolidation has a clear default.
  - **AC:** Recommendation is the most-referenced/most-dependent member; the reasoning is shown and read-only (no merge/delete performed).

## FEAT-ADMIN3-5 — Export `[Planned]`
- **US-ADMIN3.5.1** `[Planned]` **As** an MGR, **I want** the duplicate report exported, **so that** consolidation can be planned as a change.
  - **AC:** Exports to Excel, PDF, JSON, and self-contained HTML run off the UI thread; JSON carries groups, scores, and recommended keeps.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only (recommends only; no merge/delete); similarity engine UI-free and unit-testable; degrades query failures to Info.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/DuplicateMetadataFinder/ when implementation starts.
