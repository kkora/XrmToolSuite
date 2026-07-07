# Solution Size Optimizer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `pack` — `prompt/2.XrmToolBox_Plugin_Prompt_Pack.txt`, idea #15. No direct equivalent in the ALL_PROMPTS (Doc 3) set. Merged into the Solution Management category (05-Solution-Management) as item 7; kept pack-sourced.
> **Suggested tag:** `SOLN07` · **Suggested project:** `XrmToolSuite.SolutionSizeOptimizer`
> **Overlaps:** The Admin track's Storage/Capacity and "Metadata Cleanup" candidates (ALL_PROMPTS) overlap on unused-resource detection and storage reporting — this tool is **solution-scoped and export/deployment-oriented** (size of the shippable artifact + deployment impact) rather than org-wide capacity. Some size/complexity plumbing could be shared with Solution Complexity Score (shipped), which scores structure, not byte size.
> **Value/priority (my read):** Medium — real pain for large ISV/enterprise solutions (slow imports, bloated web resources), but the "unused resource" verdict is heuristic and needs care to avoid false deletes.

## Notes
- Primary data sources: `solutioncomponent` for membership; `webresource` (`content` base64 length = byte size, `webresourcetype` for JS/CSS/HTML/images); other sized assets (plugin assemblies `pluginassembly.content`, images, reports). Prefer targeted `ColumnSet`s and pull `content` lazily per asset to avoid huge retrieves.
- "Deployment impact" is an estimate: sum of component bytes, count of components, and known slow-import categories (large assemblies, many web resources). Present as an estimate with the assumptions stated — do not imply a guaranteed import time.
- "Unused" is heuristic: a web resource referenced by no form/ribbon/sitemap/other web resource is a *candidate*, not a certainty (it may be called dynamically or from external code). Always label as candidate and require human review; never auto-delete.
- All reads off the UI thread via `RunAsync`/`WorkAsync`; page with `Service.RetrieveAll`; progress per asset category and cancellation on the scan (content retrieval can be heavy).
- Size math + banding (bytes -> human-readable, oversized thresholds, unused-candidate rules) is UI-free and unit-testable in `testing/UnitTests/`.
- Read-only by default. If an optional cleanup/delete action is offered, it is a destructive operation and MUST show a confirmation dialog stating exact scope and record count, and default to a report-only (no delete) mode.
- Thresholds (oversized-asset limits) configurable and persisted via settings round-trip POCO.

---

## EPIC-SOLN07 — Measure and shrink solution footprint
> **As** an ALM, **I want** to see what makes a solution big and what is dead weight, **so that** I can slim imports and speed up deployments without breaking references.

**Outcome:** A per-solution size breakdown by component type, a ranked list of oversized and unused-candidate assets, an estimated deployment-impact summary, and an exportable optimization report — with any cleanup gated behind explicit confirmation.

---

## FEAT-SOLN07-1 — Solution size measurement `[Planned]`
- **US-SOLN07.1.1** `[Planned]` **As** an ALM, **I want** total solution size broken down by component type, **so that** I know where the weight is.
  - **AC:** Sizes aggregated by category (web resources, assemblies, images, other) via `Service.RetrieveAll` off the UI thread; totals shown in human-readable units; progress + cancellation.
- **US-SOLN07.1.2** `[Planned]` **As** an ALM, **I want** to pick which solution(s) to measure, **so that** I can target the artifact I ship.
  - **AC:** Solution picker lists unmanaged (and optionally managed) solutions; selection scopes all subsequent analysis.
- **US-SOLN07.1.3** `[Planned]` **As** a TOOLDEV, **I want** size math done in a pure, testable function, **so that** results are deterministic.
  - **AC:** Byte-size + unit conversion + banding implemented UI-free; covered by `testing/UnitTests/` fixtures.

## FEAT-SOLN07-2 — Oversized asset detection `[Planned]`
- **US-SOLN07.2.1** `[Planned]` **As** an ALM, **I want** oversized assets (large web resources, images, assemblies) ranked, **so that** I can target the biggest wins.
  - **AC:** Assets over a configurable threshold flagged and sorted by size descending; each shows type, name, and byte size.
- **US-SOLN07.2.2** `[Planned]` **As** an ALM, **I want** oversized-image and un-minified-script hints, **so that** I know a shrink is possible.
  - **AC:** Heuristics flag large raster images and likely-unminified JS/CSS (size vs. content shape) as candidates, clearly labeled as suggestions.
- **US-SOLN07.2.3** `[Planned]` **As** a TOOLDEV, **I want** oversized thresholds configurable and persisted, **so that** teams set their own limits.
  - **AC:** Thresholds editable and round-tripped via a plain settings POCO (load in `*_Load`, save in `ClosingPlugin`).

## FEAT-SOLN07-3 — Unused-resource candidates `[Planned]`
- **US-SOLN07.3.1** `[Planned]` **As** an ALM, **I want** web resources that appear referenced by nothing surfaced, **so that** I can consider removing dead weight.
  - **AC:** A web resource with no discoverable reference (form, ribbon, sitemap, other web resource) is listed as an **unused candidate**, explicitly labeled as heuristic; failed reference queries degrade to informational findings.
- **US-SOLN07.3.2** `[Planned]` **As** an ALM, **I want** each candidate to show why it was flagged, **so that** I can judge safety before acting.
  - **AC:** Each candidate lists the reference checks performed and their results; a caveat warns dynamic/external references cannot be detected.

## FEAT-SOLN07-4 — Deployment impact estimate `[Planned]`
- **US-SOLN07.4.1** `[Planned]` **As** an ALM, **I want** an estimated deployment-impact summary, **so that** I can anticipate import cost.
  - **AC:** Summary shows total bytes, component count, and slow-import risk categories; explicitly framed as an estimate with stated assumptions (not a guaranteed time).
- **US-SOLN07.4.2** `[Planned]` **As** an MGR, **I want** a before/after projection when candidates are removed, **so that** I can justify the cleanup.
  - **AC:** Projected size after removing selected oversized/unused candidates shown alongside current size; projection is read-only and performs no changes.

## FEAT-SOLN07-5 — Reporting & export `[Planned]`
- **US-SOLN07.5.1** `[Planned]` **As** an ALM, **I want** to export the optimization report, **so that** I can review and share it.
  - **AC:** CSV/HTML export of size breakdown, oversized assets, unused candidates, and deployment estimate; export off the UI thread.
- **US-SOLN07.5.2** `[Planned]` **As** an MGR, **I want** a one-page summary, **so that** I can report footprint at a glance.
  - **AC:** Summary shows total size, top offenders, and count of unused candidates; regenerates on filter change.

## FEAT-SOLN07-6 — Optional gated cleanup `[Planned]`
- **US-SOLN07.6.1** `[Planned]` **As** an ALM, **I want** cleanup to be report-only by default, **so that** I never delete something unexpectedly.
  - **AC:** Default mode performs no writes; any delete action is explicitly opt-in.
- **US-SOLN07.6.2** `[Planned]` **As** an ALM, **I want** any delete guarded by a confirmation stating scope and count, **so that** destructive actions are deliberate.
  - **AC:** Deletion shows a confirmation dialog naming the exact assets and record count; deletes run via `RunAsync` with progress + cancellation; caches cleared after.

## Definition of Done
- Follows suite conventions; read-only by default; optional cleanup is confirmation-gated (scope + count).
- Size/banding/unused-candidate logic is UI-free and unit-tested; unused verdicts labeled heuristic.
- Testing skeleton under testing/SolutionSizeOptimizer/ when implementation starts.
