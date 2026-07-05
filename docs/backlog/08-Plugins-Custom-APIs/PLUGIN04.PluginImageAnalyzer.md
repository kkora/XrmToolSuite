# Plugin Image Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 8 (Plugins & Custom APIs), item 4. Not in pack file.
> **Suggested tag:** `PLUGIN04` · **Suggested project:** `XrmToolSuite.PluginImageAnalyzer`
> **Overlaps:** Plugin Registration Auditor covers image hygiene at a summary level (missing/excessive images) as one of many rules; Deployment Risk Analyzer does not deep-dive images. This tool is the **image specialist** — per-attribute validation against table metadata, sensitive/large-field detection, and an image health score. Reuse the shared step/image queries and Attribute Auditor's metadata (sensitivity/type) helpers; do not duplicate the auditor's summary rule.
> **Value/priority (my read):** Medium — image misconfiguration is a real but narrow performance/data-exposure risk; strongest value is the sensitive/large-field-in-image detection that other tools don't do.

## Notes
- Data sources: `sdkmessageprocessingstepimage` (imagetype pre/post, entityalias, attributes, messagepropertyname), joined to `sdkmessageprocessingstep` (message/stage/mode) and `plugintype`; attribute metadata via `RetrieveEntityRequest`/`RetrieveAttributeRequest` for type, size, and sensitivity.
- Detecting "sensitive fields" leans on attribute metadata (`IsSecured`/field-level security, plus a configurable sensitive-name list) — surface the attribute name as a warning; **never read or display the field values themselves**.
- "Large" = file/image/multiline-text/large-decimal attributes identified from metadata; flag their inclusion in images as a performance risk.
- Feasibility caveat: "expected" pre-vs-post image and "missing required image" are rule-driven per message/step — ship defaults (e.g. Update → pre-image expected) and let config refine; mark as advisory.
- Read-only tool — analyzes image registration + metadata, never modifies images. No destructive ops.
- Shared-core reuse: `Service.RetrieveAll`, `BatchExecutor`, progress/cancellation, settings round-trip, shared reporting/export module, and the shared metadata-retrieval/cache used by Attribute Auditor; analyzers stay UI-free for console/CI lift.

---

## EPIC-PLUGIN04 — Analyze plugin step images for correctness, performance, and data-exposure risk
> **As** a TOOLDEV, **I want** a clear view of every pre/post image and the attributes it carries, **so that** I can fix missing, bloated, mistyped, or sensitive images before they cost performance or leak data.

**Outcome:** an image inventory with per-attribute validation, image health score, findings with severities (Critical/High/Medium/Low/Info), recommendations, and Excel/PDF/JSON/CSV/HTML exports — from a live connection with no hand-written queries.

---

## FEAT-PLUGIN04-1 — Image inventory `[Planned]`
- **US-PLUGIN04.1.1** `[Planned]` **As** a TOOLDEV, **I want** all plugin step images listed with name, alias, type, attributes, message, table, stage, and owning step, **so that** I have a complete image inventory.
  - **AC:** Images load via `Service.RetrieveAll` off the UI thread with progress and cancellation; grid shows name/alias/imagetype/message/entity/stage/attribute-count.
- **US-PLUGIN04.1.2** `[Planned]` **As** a TOOLDEV, **I want** a plugin step selector that scopes the image grid, **so that** I can focus on one step's images.
  - **AC:** Selecting a step filters images and shows an attribute detail panel; selection persists via settings.
- **US-PLUGIN04.1.3** `[Planned]` **As** an ADM, **I want** an image dashboard summarizing counts by type and risk, **so that** I get the shape of image usage at a glance.
  - **AC:** Dashboard shows pre/post counts, attribute-count distribution, and finding counts by severity.

## FEAT-PLUGIN04-2 — Attribute & metadata validation `[Planned]`
- **US-PLUGIN04.2.1** `[Planned]` **As** a TOOLDEV, **I want** image attributes not present on the target table flagged, **so that** stale image definitions are caught.
  - **AC:** Each image attribute is checked against table metadata; unknown attributes → Medium finding naming them.
- **US-PLUGIN04.2.2** `[Planned]` **As** a PERF engineer, **I want** images that include all attributes or too many attributes flagged, **so that** I can trim payloads.
  - **AC:** Image with all attributes → High; attribute count over a configurable threshold → Medium/High.
- **US-PLUGIN04.2.3** `[Planned]` **As** a TOOLDEV, **I want** duplicate images on a step detected, **so that** redundant images are removed.
  - **AC:** Images on the same step matching on type+attribute-set are grouped and flagged Medium.

## FEAT-PLUGIN04-3 — Sensitive & large-field detection `[Planned]`
- **US-PLUGIN04.3.1** `[Planned]` **As** a SEC reviewer, **I want** sensitive fields included in images flagged without exposing their values, **so that** I can prevent data exposure through images.
  - **AC:** Field-secured or configured-sensitive attributes in an image → High; only the attribute name is shown, never a value.
- **US-PLUGIN04.3.2** `[Planned]` **As** a PERF engineer, **I want** file/image/large-text fields in images flagged, **so that** heavy attributes don't bloat the pipeline.
  - **AC:** File/image/multiline-text/large attributes in an image → Medium/High naming the attribute and its type.

## FEAT-PLUGIN04-4 — Image-type & naming rules `[Planned]`
- **US-PLUGIN04.4.1** `[Planned]` **As** a TOOLDEV, **I want** missing expected images and wrong image types flagged, **so that** steps have the images their logic needs.
  - **AC:** Missing rule-required image → High; pre-image where a post-image is expected (or vice versa) → Medium/High with the expected type.
- **US-PLUGIN04.4.2** `[Planned]` **As** an ALM lead, **I want** image aliases that violate naming standards flagged, **so that** aliases stay consistent.
  - **AC:** Alias failing the configured regex → Low/Medium with the offending alias and expected pattern.

## FEAT-PLUGIN04-5 — Score, recommendations & export `[Planned]`
- **US-PLUGIN04.5.1** `[Planned]` **As** a MGR, **I want** an image health score, **so that** I can track image hygiene over time.
  - **AC:** Weighted severities produce a 0–100 score with a Low/Medium/High band on score cards.
- **US-PLUGIN04.5.2** `[Planned]` **As** a TOOLDEV, **I want** a recommendation panel per finding, **so that** I know the concrete fix (trim attributes, change image type, remove sensitive field, rename alias).
  - **AC:** Each finding carries a plain-language recommendation; nothing is applied automatically.
- **US-PLUGIN04.5.3** `[Planned]` **As** a MGR, **I want** to export the image analysis report to Excel, PDF, JSON, CSV, and HTML, **so that** I can share it.
  - **AC:** All listed formats export from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; read-only default (no image changes); export formats as listed (Excel/PDF/JSON/CSV/HTML).
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; analyzers UI-free; sensitive field values never read or displayed (names only).
- Testing skeleton under `testing/PluginImageAnalyzer/` when implementation starts; SDK-free attribute-validation/scoring logic covered by `testing/UnitTests`.
