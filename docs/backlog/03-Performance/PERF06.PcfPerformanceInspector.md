# PCF Performance Inspector — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 6. Related pack idea #8 'PCF Dependency Scanner'.
> **Suggested tag:** `PERF06` · **Suggested project:** `XrmToolSuite.PcfPerformanceInspector`
> **Overlaps:** Solution Knowledge Graph maps component relationships broadly; this tool is PCF-specific (versions, bindings, form placement, risk score). Reuse any shared form-parsing helpers but keep PCF risk logic here.
> **Value/priority (my read):** Medium — PCF overuse is a genuine form-load risk, but PCF adoption is uneven across orgs, so audience is narrower than views/plugins.

## Notes
- Data sources: `customcontrol` (PCF manifest metadata: name, namespace, version, publisher), plus `systemform` FormXML to find where controls are bound (forms/views/apps), and `appmodule`/`appmodulecomponent` for app usage where available.
- Detection: outdated versions, duplicate PCF packages, PCFs on many high-traffic forms, PCFs co-located with many heavy controls, PCFs bound to large text/file/image/multiselect columns, and unmanaged PCF controls in production.
- Feasibility caveat: "high-traffic" can't be measured from metadata alone — approximate via form count / primary-form heuristics and label it; version currency needs a known-latest reference the user may have to supply.
- Bindings require parsing FormXML control nodes and correlating `customcontrol` IDs to attributes and forms.
- Read-only tool — inventory and risk only, no changes.
- Shared-core reuse: `RunAsync`/`RetrieveAll`, progress/cancellation, settings round-trip, shared export module, shared form-XML parsing where it exists.

---

## EPIC-PERF06 — Inventory PCF controls and score their form-performance risk
> **As** an ARCH, **I want** to see every PCF control, where it's used, and its performance risk, **so that** I can rein in overuse before it slows forms and grids.

**Outcome:** a PCF inventory dashboard, per-control usage locations, form-impact analysis, a risk score, recommendations, and exports — from live metadata.

---

## FEAT-PERF06-1 — PCF inventory `[Planned]`
- **US-PERF06.1.1** `[Planned]` **As** an ARCH, **I want** all installed PCF controls listed with version, publisher, namespace, and manifest metadata, **so that** I know what's deployed.
  - **AC:** `customcontrol` rows load via `RetrieveAll` off the UI thread into an inventory grid.
- **US-PERF06.1.2** `[Planned]` **As** an ARCH, **I want** a PCF inventory dashboard with counts, **so that** I get a quick overview.
  - **AC:** Dashboard cards summarize total controls, managed vs unmanaged, and duplicate packages.

## FEAT-PERF06-2 — Usage location mapping `[Planned]`
- **US-PERF06.2.1** `[Planned]` **As** an ARCH, **I want** the forms, views, and apps where each PCF is used, **so that** I can gauge exposure.
  - **AC:** FormXML is parsed to map `customcontrol` bindings to forms/views/apps; a usage location panel lists them per control.
- **US-PERF06.2.2** `[Planned]` **As** a PERF engineer, **I want** a form-impact panel showing other heavy controls on the same form, **so that** I can spot compounded load.
  - **AC:** The panel counts co-located controls and highlights forms where a PCF sits among many heavy controls.

## FEAT-PERF06-3 — Risk rules `[Planned]`
- **US-PERF06.3.1** `[Planned]` **As** a PERF engineer, **I want** outdated versions and duplicate packages flagged, **so that** I can consolidate and update.
  - **AC:** Version below a known/reference latest → Medium; duplicate PCF packages → Medium.
- **US-PERF06.3.2** `[Planned]` **As** a PERF engineer, **I want** PCFs on many/high-traffic forms and those bound to large text/file/image/multiselect columns flagged, **so that** I catch the heaviest placements.
  - **AC:** Usage count over threshold → Medium/High (labeled heuristic); binding to a large-payload column type → High.
- **US-PERF06.3.3** `[Planned]` **As** a SEC/ADM, **I want** unmanaged PCF controls in production flagged, **so that** governance gaps surface.
  - **AC:** Unmanaged `customcontrol` in the connected (production-flagged) org → High.

## FEAT-PERF06-4 — Score, recommendations & export `[Planned]`
- **US-PERF06.4.1** `[Planned]` **As** a MGR, **I want** a PCF usage and risk score, **so that** I can track PCF health.
  - **AC:** A 0–100 risk score combines the rule findings, shown on the dashboard.
- **US-PERF06.4.2** `[Planned]` **As** an ARCH, **I want** recommendations and Excel/PDF/JSON/HTML/CSV exports, **so that** I can act and share.
  - **AC:** Each finding carries a recommendation; all export formats come from the shared reporting module.

## Definition of Done
- Follows suite conventions; read-only default; export formats as listed.
- FormXML parsing degrades gracefully; "high-traffic" and version-currency heuristics labeled as such.
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; settings round-trip.
- Testing skeleton under `testing/PcfPerformanceInspector/` when implementation starts; parsing/scoring covered by `testing/UnitTests`.
