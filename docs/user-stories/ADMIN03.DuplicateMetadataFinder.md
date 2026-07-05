# Duplicate Metadata Finder — User Stories

> **Status:** Implemented. Source spec: [`docs/backlog/04-Dataverse-Administration/ADMIN03.DuplicateMetadataFinder.md`](../backlog/04-Dataverse-Administration/ADMIN03.DuplicateMetadataFinder.md) (same US ids).
> **Project:** `src/Tools/XrmToolSuite.DuplicateMetadataFinder` · **Area tag:** `ADMIN03`
> **Legend:** `[Implemented]` = built + covered (automated where SDK-free, else manual). `[Implemented*]` = built but only verifiable in a live Windows/XrmToolBox session (Dataverse metadata, WinForms grid, GDI/MigraDoc export) — pending manual sign-off.

Finds duplicate and near-duplicate Dataverse metadata — columns, option sets, tables, forms, views,
business rules, web resources, plugin steps and relationships — by scoring each same-kind pair 0–100 from
weighted signals (normalized display/schema name via edit distance + token overlap, data-type agreement,
option-value overlap, and an exact content hash for web resources/JavaScript), then clustering matches
at/above a configurable threshold and recommending a primary to keep (most-referenced wins, deterministic
tie-break). **Read-only — it recommends only; it never merges or deletes.** The similarity engine, scoring,
clustering and the report projection are SDK-free and unit-tested; the Dataverse collector and the WinForms
grid / Excel / PDF exports are manual-tested.

---

## EPIC-ADMIN03 — Surface duplicate and near-duplicate metadata so teams can consolidate `[Implemented]`
> **As** an **ARCH**, **I want** duplicate or near-duplicate components grouped with a similarity score and
> a recommended primary to keep, **so that** I can consolidate redundant metadata and cut confusion and
> reporting inconsistency.

**Outcome:** similarity-scored duplicate groups across component types, a side-by-side comparison with a
recommended keep-candidate per group, and an exportable report — without ever mutating metadata.

---

## FEAT-ADMIN03-1 — Scan configuration and metadata retrieval `[Implemented]`
- **US-ADMIN03.1.1** `[Implemented]` Configure which component types and scope to scan.
  - **AC:** A checkable "Component types" menu toggles each `ComponentKind`; a "Custom only" toggle drops
    managed/system components (`DuplicateScanOptions.CustomOnly`). Retrieval runs off the UI thread via
    `RunAsync`/`RetrieveAll`/`RetrieveAllEntitiesRequest` with progress and per-kind cancellation; each kind
    degrades to a note on failure instead of aborting. *(Collector: manual; options model: automated.)*
- **US-ADMIN03.1.2** `[Implemented]` A configurable similarity threshold tunes false positives vs. recall.
  - **AC:** The threshold round-trips via Load/SaveSettings and is clamped 0–100; only pairs at/above it are
    grouped (`SimilarityEngine.Group`). **Automated** — `Group_ThresholdFiltersWeakPairs`.

## FEAT-ADMIN03-2 — Detect duplicate columns and option sets `[Implemented]`
- **US-ADMIN03.2.1** `[Implemented]` Detect duplicate column display names and similar schema names.
  - **AC:** Score combines normalized display-name and schema-name closeness (Levenshtein ratio + token
    Jaccard) with data-type agreement; each contributing `SimilarityFactor` is shown. **Automated** —
    `Score_IdenticalColumns_Is100`, `Score_DifferentType_LowersScore`, `Normalize_*`, `Levenshtein_*`, `Jaccard_*`.
- **US-ADMIN03.2.2** `[Implemented]` Detect same-type/similar-purpose fields and duplicate/overlapping option sets.
  - **AC:** Option-set duplicates compare option-value sets; the overlap ratio (Jaccard) contributes to the
    score. **Automated** — `Score_OptionSetOverlap_Contributes`.

## FEAT-ADMIN03-3 — Detect duplicate UI and logic components `[Implemented]`
- **US-ADMIN03.3.1** `[Implemented]` Detect duplicate tables, forms and views.
  - **AC:** Each is a distinct duplicate category with its own similarity basis (name + container signals);
    comparisons are blocked by kind so a form is never matched to a column. **Automated** —
    `Score_CrossKind_IsZeroAndNeverGroups`; collector *(manual — live metadata)*.
- **US-ADMIN03.3.2** `[Implemented]` Detect duplicate business rules, web resources/JavaScript, plugin steps and relationships.
  - **AC:** Web-resource/JS duplicates use an exact SHA-256 content hash (flagged exact, no false-positive
    disclaimer); plugin-step duplicates key on the message+entity+stage signature; relationships on schema
    name + type. **Automated** — `Score_IdenticalContentHash_ShortCircuitsTo100_Exact`; collector *(manual)*.

## FEAT-ADMIN03-4 — Group, compare, and recommend primary `[Implemented]`
- **US-ADMIN03.4.1** `[Implemented]` Group duplicates with a side-by-side comparison.
  - **AC:** Union-find clusters transitively-linked pairs into groups (across containers); the detail pane
    lists members, per-pair scores and contributing factors. **Automated** —
    `Group_ClustersTransitiveDuplicates_AcrossContainers`, `Group_RanksWorstFirst`. *(Grid render: manual.)*
- **US-ADMIN03.4.2** `[Implemented]` Recommend a primary component to keep per group.
  - **AC:** The recommendation is the most-referenced member; ties break toward managed then the smaller key,
    and the reasoning is shown. **Read-only — no merge/delete is performed.** **Automated** —
    `Group_RecommendsMostReferencedPrimary`, `Group_TieBreaksTowardManagedDeterministically`.

## FEAT-ADMIN03-5 — Export `[Implemented]`
- **US-ADMIN03.5.1** `[Implemented]` Export the duplicate report to Excel, PDF, JSON and self-contained HTML.
  - **AC:** The scan projects to the shared `ReportModel` (one finding per group, severity from the top pair,
    recommended keep in the recommendation), driving `ExcelReportExporter` / `PdfReportExporter` /
    `JsonReportExporter` / `HtmlDashboardBuilder` off the UI thread. **Automated** for the projection
    (`Report_Projects_MetricsAndOneFindingPerGroup`, `Report_EmptyScan_ZeroScoreNoFindings`,
    `Report_ExactContentMatch_IsHighSeverity`, `Report_ScoreCapsAt100`); **Excel/PDF `[Implemented*]`** —
    ClosedXML / MigraDoc-GDI, manual-tested.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress + cancellation). — **Done.**
- Read-only (recommends only; no merge/delete); the similarity engine, scoring, clustering and report
  projection stay UI-free and SDK-free and degrade query failures to notes. — **Done.**
- Export formats: Excel, PDF, JSON, HTML. — **Done** (Excel/PDF pending manual sign-off).
- Testing under `testing/DuplicateMetadataFinder/`; SDK-free logic covered by
  `testing/UnitTests/DuplicateMetadataFinderTests.cs` (22 cases). — **Done** (collector/grid/Excel/PDF pending manual).
