# Attribute Auditor - User Stories

Tool Epic under the XrmToolSuite portfolio. See the [index](README.md) for personas, ID scheme, and
status legend. Area tag: `AA`.

> **Status: v1 built.** The audit engine ships: it scans custom columns and detects usage from
> **forms, views, processes (workflows / business rules / cloud flows), and field security**, classifies
> each as used or a retirement candidate, shows them in a filterable grid, and exports **CSV + an HTML
> dashboard**. Deferred to a later pass (still `[Planned]` below): chart/dashboard signals, the data-population
> check, table multi-select / solution scoping, JSON/Excel export, and the guarded cleanup (FEAT-AA-5).
> The reference-detection (`UsageScanners`) and classification are SDK-free and unit-tested; the collector
> is covered headlessly against a fake connection.

---

## EPIC-AA - Find and safely retire unused columns across a Dataverse environment

> **As** a **CUST** / **ADM**, **I want** to identify columns (attributes) that no longer appear to
> be used anywhere, **so that** I can reduce schema clutter, form/API payload size, and maintenance
> cost - and retire them safely with an auditable trail.

**Outcome:** a per-table report classifying each custom column by usage evidence, with filters,
export, and a guarded path to a cleanup solution. "Unused" is evidence-based and always
human-confirmed before any change.

---

## FEAT-AA-0 - Scaffold & shared wiring `[Done]`

- **US-AA-0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background-execution wired via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Tool appears in XTB, connects, runs a background query, persists settings on close.
- **US-AA-0.2** `[Done]` **As** a TOOLDEV, **I want** the placeholder `UserName`/`HelpUrl` and
  sample-data command replaced with real identity and the audit entry points, **so that** no template leftovers ship.
  - **AC:** No `your-github-username` and no "Load sample" remain in the shipped control.

## FEAT-AA-1 - Select audit scope `[Partial]`

- **US-AA-1.1** `[Partial]` **As** a CUST, **I want** to pick one, several, or all tables (with a
  custom-only toggle and a solution filter), **so that** I can target the audit and keep runs fast.
  - **AC:** Table list loads via cached metadata; managed system tables can be excluded.
  - **AC:** Selection (and custom-only/solution filter) persists in settings.
- **US-AA-1.2** `[Planned]` **As** a CUST, **I want** to choose which usage signals count, **so that**
  I can tune strictness (e.g. treat "on a form" as used, or require actual data population).

## FEAT-AA-2 - Detect column usage signals `[Partial]`

Each signal marks a column as "used" with evidence. A column with **no** signals is a retirement candidate.

- **US-AA-2.1** `[Done]` **As** a CUST, **I want** columns present on any **form** detected,
  **so that** UI-referenced columns are not falsely flagged.
- **US-AA-2.2** `[Done]` **As** a CUST, **I want** columns used in any **view / saved query**
  (columns or filter) detected.
- **US-AA-2.3** `[Partial]` **As** a CUST, **I want** columns referenced by **business rules,
  workflows, flows, calculated/rollup fields, and plugin steps** detected.
- **US-AA-2.4** `[Partial]` **As** a CUST, **I want** columns referenced by **charts, dashboards,
  and field security profiles** detected.
- **US-AA-2.5** `[Planned]` **As** an ADM, **I want** an optional **data population** check
  (fraction of non-null values across a sampled/aggregate query), **so that** I can distinguish
  configured-but-empty columns from actively populated ones.
  - **AC:** Population sampling respects service-protection limits (aggregate or capped sample, no full-table scans by default).

## FEAT-AA-3 - Results & classification `[Partial]`

- **US-AA-3.1** `[Done]` **As** a CUST, **I want** a grid of columns with table, display/logical
  name, type, managed/custom, and a **usage summary** (which signals fired), **so that** I can judge each candidate.
  - **AC:** Each row is expandable to the concrete evidence (e.g. "on 2 forms, 1 view; 0% populated").
- **US-AA-3.2** `[Partial]` **As** a CUST, **I want** to sort/filter by "no usage", type, or table,
  **so that** I can focus on the safest wins first.
- **US-AA-3.3** `[Done]` **As** an ADM, **I want** managed columns clearly marked non-deletable,
  **so that** I do not target components I cannot change.

## FEAT-AA-4 - Export `[Partial]`

- **US-AA-4.1** `[Partial]` **As** an ADM, **I want** to export the audit to CSV/Excel, **so that**
  I can review with stakeholders and track decisions.
- **US-AA-4.2** `[Planned]` **As** a DEVOPS engineer, **I want** a JSON export, **so that** the audit
  can feed governance dashboards.

## FEAT-AA-5 - Guarded cleanup `[Planned]`

- **US-AA-5.1** `[Planned]` **As** an ADM, **I want** to select confirmed-unused columns and generate
  a **cleanup solution / delete plan** (preview only), **so that** removal is reviewable and reversible via source control.
  - **AC:** No deletion happens without an explicit confirmation dialog stating the exact columns and count (suite rule 8).
  - **AC:** Dependency check runs first; columns with any dependency are blocked from deletion with the reason shown.
- **US-AA-5.2** `[Planned]` **As** an ADM, **I want** an audit log of what was proposed/removed and
  when, **so that** cleanup is traceable.

---

## Definition of Done (tool-level)

- Audit never deletes without confirmation and a passing dependency check.
- All Dataverse calls run through `RunAsync`/`RetrieveAll`; long runs report progress and cancel cleanly.
- "Used" vs "candidate" classification is explainable from listed evidence.
