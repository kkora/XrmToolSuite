# Business Rule Performance Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 3 (Performance), item 7. Related pack idea #16 'Business Rule Analyzer'.
> **Suggested tag:** `PERF7` · **Suggested project:** `XrmToolSuite.BusinessRulePerformanceAnalyzer`
> **Overlaps:** Technical Debt Analyzer flags dead/overlapping logic broadly; this tool is business-rule-specific (complexity, conflicts, contradictions, form load impact). Solution Complexity Score is solution-level, not rule-level. No direct overlap.
> **Value/priority (my read):** Medium — business rules quietly accumulate and slow forms/confuse users, but they're less catastrophic than plugins/views; strong conflict detection is the differentiator.

## Notes
- Data sources: business rules are workflows — `workflow` where `category = 2` (Business Rule); parse the rule's XAML/clientdata (`clientdata`/`xaml`) for conditions and actions, plus `scope` and active status (`statecode`).
- Analysis: rule scope, conditions, actions; too many rules on a form/table; duplicate conditions; conflicting actions (e.g., one shows a field another hides); circular/contradictory logic where detectable; rules affecting hidden/removed or deprecated fields; overlapping rules.
- Feasibility caveat: business-rule definitions are stored as serialized logic — parsing conditions/actions reliably is non-trivial and version-dependent; degrade to structural counts + informational findings when a rule can't be fully parsed, never throw.
- Detecting "affects hidden/removed fields" needs cross-referencing rule field references against current attribute metadata.
- Read-only tool — analyzes definitions only.
- Shared-core reuse: `RunAsync`/`RetrieveAll`, progress/cancellation, settings round-trip, shared export module; keep the parser/rule analyzer UI-free.

---

## EPIC-PERF7 — Analyze business rules for complexity, conflicts, and form-performance impact
> **As** a MAKER/ADM, **I want** to see which tables carry heavy, overlapping, or conflicting business rules, **so that** I can simplify logic and speed up forms.

**Outcome:** a rule complexity score, conflict/duplication findings, a business-rule map, recommendations, and exports — from parsed rule definitions.

---

## FEAT-PERF7-1 — Business rule inventory `[Planned]`
- **US-PERF7.1.1** `[Planned]` **As** an ADM, **I want** to list business rules by table with active/inactive status and scope, **so that** I can see what's running where.
  - **AC:** Business-rule `workflow` rows load via `RetrieveAll` off the UI thread; grid shows table, name, status, scope.
- **US-PERF7.1.2** `[Planned]` **As** a MAKER, **I want** a condition/action viewer per rule, **so that** I can inspect what a rule does.
  - **AC:** Selecting a rule shows its parsed conditions and actions; unparseable rules show a best-effort structural summary, not an error.

## FEAT-PERF7-2 — Complexity analysis `[Planned]`
- **US-PERF7.2.1** `[Planned]` **As** a PERF engineer, **I want** rule conditions and actions counted per rule, **so that** I can gauge complexity.
  - **AC:** Per-rule condition/action counts feed a complexity metric shown in the grid.
- **US-PERF7.2.2** `[Planned]` **As** a PERF engineer, **I want** tables/forms with too many rules flagged, **so that** I catch form-load overload.
  - **AC:** Rule count on a form/table over a configurable threshold → Medium/High.

## FEAT-PERF7-3 — Conflict & duplication detection `[Planned]`
- **US-PERF7.3.1** `[Planned]` **As** a MAKER, **I want** duplicate conditions and overlapping rules detected, **so that** I can consolidate.
  - **AC:** Rules sharing identical conditions or overlapping scope/fields are grouped and flagged Medium.
- **US-PERF7.3.2** `[Planned]` **As** a PERF engineer, **I want** conflicting or contradictory actions detected where possible, **so that** I can resolve logic fights.
  - **AC:** Opposing actions on the same field (show vs hide, enable vs disable) → High; detection labeled best-effort.
- **US-PERF7.3.3** `[Planned]` **As** an ADM, **I want** rules referencing hidden, removed, or deprecated fields flagged, **so that** I can clean up broken logic.
  - **AC:** Rule field references cross-checked against current attribute metadata; unresolved/deprecated references → Medium.

## FEAT-PERF7-4 — Map, score, recommendations & export `[Planned]`
- **US-PERF7.4.1** `[Planned]` **As** a MAKER, **I want** a business-rule map and a complexity score, **so that** I can see and communicate the logic footprint.
  - **AC:** A rule map view and a 0–100 complexity score (labeled heuristic) summarize the table/form.
- **US-PERF7.4.2** `[Planned]` **As** a MGR, **I want** recommendations and Excel/PDF/JSON/HTML/CSV exports, **so that** I can act and share.
  - **AC:** Each finding carries a recommendation; all export formats come from the shared reporting module.

## Definition of Done
- Follows suite conventions; read-only default; export formats as listed.
- Rule parsing degrades to structural/informational findings when definitions can't be fully parsed; conflict detection labeled best-effort.
- Analyzer/parser UI-free; all Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; settings round-trip.
- Testing skeleton under `testing/BusinessRulePerformanceAnalyzer/` when implementation starts; complexity/conflict logic covered by `testing/UnitTests`.
