# Component Ownership Tracker — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 5 (Solution Management), item 3. Not in pack file.
> **Suggested tag:** `SOLN03` · **Suggested project:** `XrmToolSuite.ComponentOwnershipTracker`
> **Overlaps:** Shipped **Attribute Auditor** covers column-level created/modified-by and hygiene for attributes — reuse its audit-field retrieval pattern; this tool is *ownership across all component types* plus a **logical-ownership tagging layer** (assign owner/team/category without touching Dataverse metadata). Mild overlap with Technical Debt Analyzer on "unmanaged production customization" findings.
> **Value/priority (my read):** Medium — real governance value in large multi-vendor environments, but ownership data quality depends heavily on audit history and the tagging layer is the differentiator; niche compared to the scoring tools.

## Notes
- Core data: `solution` + `solutioncomponent` for the component inventory; `msdyn_solutioncomponentsummary` for enriched membership/managed state; `publisher`; audit fields `createdby`/`createdon`/`modifiedby`/`modifiedon`/`ownerid` on the underlying component records; `systemuser` (+ `isdisabled`) to resolve modifier/owner and detect inactive users; `ownershiptype`/business-unit/team where the record type supports it.
- **Custom ownership tagging:** default is a **local, serialized settings store** (POCO round-tripped via LoadSettings/SaveSettings) keyed by component id — assigns logical Business/Technical/Support/Vendor/Legacy/Platform/Integration owner without changing any Dataverse metadata. Optional advanced mode can persist to a customer-provided Dataverse table; that mode is opt-in and the only path that writes.
- Read-only against Dataverse metadata by default; the only write is the *opt-in* Dataverse-table tagging mode, which must gate behind an explicit confirmation dialog stating scope.
- Retrieval off the UI thread via `RunAsync`; page with `Service.RetrieveAll`; report progress and honor cancellation; cache per connection and clear on `UpdateConnection`.
- Keep ownership-risk rules and scoring UI-free (analyzer style) so they are testable; degrade missing audit/owner data (some component types lack owner or reliable audit) to "Unknown Owner" Info findings, never a crash.
- Local tags are environment-specific — key by component GUID + environment id and never persist credentials.

---

## EPIC-SOLN03 — Know who owns, introduced, and last changed every solution component
> **As** an **MGR**, **I want** an ownership matrix across all solution components with a way to assign logical owners, **so that** I can find unowned, vendor, and orphaned customizations and plan support handoff.

**Outcome:** a component ownership inventory (owner/creator/modifier/publisher/solution/managed-state), an ownership score, risk findings (unknown/vendor/inactive-modifier/unmanaged-prod), and an exportable ownership matrix + handoff report.

---

## FEAT-SOLN03-1 — Inventory components with ownership metadata `[Planned]`
- **US-SOLN03.1.1** `[Planned]` **As** an MGR, **I want** all solution components inventoried with created-by/modified-by, dates, publisher, solution, and managed state, **so that** I have one place to see responsibility signals.
  - **AC:** Inventory loads off the UI thread with progress/cancel; grid is filterable by owner/team, publisher, solution, and modified-by.
- **US-SOLN03.1.2** `[Planned]` **As** an MGR, **I want** owner and owning team/business-unit shown where the component type supports it, **so that** I capture platform ownership where it exists.
  - **AC:** Owner/team/BU columns populate for record types that have them; unsupported types show "n/a" (Info), not blank ambiguity.

## FEAT-SOLN03-2 — Logical ownership tagging `[Planned]`
- **US-SOLN03.2.1** `[Planned]` **As** an MGR, **I want** to assign a logical owner/team/category to a component without changing Dataverse metadata, **so that** I can record responsibility the platform doesn't track.
  - **AC:** Tags persist to the local settings store keyed by component id + environment and round-trip via Load/SaveSettings; no Dataverse write occurs in this default mode.
- **US-SOLN03.2.2** `[Planned]` **As** an MGR, **I want** an optional mode to persist tags to a customer Dataverse table, **so that** ownership is shared across the team.
  - **AC:** The Dataverse-table mode is opt-in, gated behind an explicit confirmation dialog stating what will be written, and is the only path that writes; batched writes via BatchExecutor with progress.

## FEAT-SOLN03-3 — Ownership risk findings `[Planned]`
- **US-SOLN03.3.1** `[Planned]` **As** a SEC reviewer, **I want** components with unknown ownership, vendor/legacy owners, and inactive modifiers flagged, **so that** I can close governance gaps.
  - **AC:** Findings classify Unknown / Vendor / Legacy owner and "modified by inactive user" (resolved via `systemuser.isdisabled`) with severity.
- **US-SOLN03.3.2** `[Planned]` **As** a SEC reviewer, **I want** unmanaged production customizations and components with no support owner surfaced, **so that** I know what is unsupported.
  - **AC:** Unmanaged-in-production and untagged-support-owner components are distinct finding categories with counts.

## FEAT-SOLN03-4 — Ownership scoring `[Planned]`
- **US-SOLN03.4.1** `[Planned]` **As** an MGR, **I want** an ownership score, **so that** I can track how well ownership is documented over time.
  - **AC:** Score reflects the share of components with a known/assigned owner and support owner; weighting lives in settings and round-trips.

## FEAT-SOLN03-5 — Ownership reports and export `[Planned]`
- **US-SOLN03.5.1** `[Planned]` **As** an MGR, **I want** the ownership matrix and named reports (Unknown Ownership, Vendor Component, Inactive Modifier, Support Handoff, Production Unmanaged) exported, **so that** I can drive a handoff or audit.
  - **AC:** Export to Excel, PDF, JSON, HTML runs off the UI thread; HTML is self-contained and theme-aware; each named report is a selectable output.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel); local tags round-trip in settings.
- Read-only by default; the only write path (optional Dataverse tagging table) requires an explicit confirmation dialog and uses BatchExecutor.
- Ownership-risk rules and scoring stay UI-free and degrade missing audit/owner data to Info ("Unknown Owner").
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/ComponentOwnershipTracker/ when implementation starts.
