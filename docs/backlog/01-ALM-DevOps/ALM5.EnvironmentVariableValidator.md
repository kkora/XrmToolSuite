# Environment Variable Validator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 1 (ALM & DevOps), item 5. Related pack idea #12 'Environment Variable Auditor'.
> **Suggested tag:** `ALM5` · **Suggested project:** `XrmToolSuite.EnvironmentVariableValidator`
> **Overlaps:** Deployment Risk Analyzer already flags env-var definitions with no default/current value, secret (Key Vault) variables, and values accidentally packaged into the solution — this tool is a *dedicated validator* (format/type validation, usage tracing, cross-environment compare). Note the overlap and reuse DRA's env-var analyzer for the missing-value/secret/packaged-value classes.
> **Value/priority (my read):** Medium — genuinely useful and self-contained, but the highest-value checks partially ship in Deployment Risk Analyzer; the differentiator is format validation + cross-env compare + usage tracing.

## Notes
- Core tables: `environmentvariabledefinition` (type, schemaname, defaultvalue, secretstore) and `environmentvariablevalue` (current value, bound solution).
- Type-specific validation: URL/JSON/boolean/number/secret parsing per `type` optionset; secret variables reference Key Vault and carry no plaintext value.
- Usage tracing means scanning flow `clientdata`, plugin/custom-API config, and web resources for the variable schema name — reuse DRA's flow `clientdata` parsing.
- Read-only; produces a validation report and a deployment checklist, no writes.
- Cross-environment compare implies an optional second connection (mirror Deployment Risk Analyzer's `TargetOrganization` dual-connection pattern); single-env validation must work without it.
- Keep the rules engine UI-free (pure validators over definition/value POCOs) so it is unit-testable without a connection.

---

## EPIC-ALM5 — Validate environment variables across solutions and environments
> **As** an **ALM** engineer, **I want** to validate every environment variable's presence, format, type, and usage before deployment, **so that** missing or malformed values do not break flows, plugins, custom APIs, Power Pages, or integrations after go-live.

**Outcome:** a per-variable validation status, a missing-values panel, a cross-environment comparison, and a deployment checklist, exportable for humans and pipelines.

---

## FEAT-ALM5-1 — Inventory variables and values `[Planned]`
- **US-ALM5.1.1** `[Planned]` **As** an ALM engineer, **I want** all environment-variable definitions and their current values listed, **so that** I have the full inventory to validate.
  - **AC:** Definitions and values load off the UI thread via `RetrieveAll` with progress; each row shows type, schema name, default, and current value.
- **US-ALM5.1.2** `[Planned]` **As** an ALM engineer, **I want** variables grouped/filterable by solution and type, **so that** I can focus on the ones I am shipping.
  - **AC:** Solution filter and type filter drive the grid; grouping persists via Load/SaveSettings.

## FEAT-ALM5-2 — Presence and duplication validation `[Planned]`
- **US-ALM5.2.1** `[Planned]` **As** an ALM engineer, **I want** missing current values and default-used-in-production cases detected, **so that** the target is properly configured before go-live.
  - **AC:** Missing current value and "default value used where a current value is expected" are distinct findings (reusing DRA's missing-value logic).
- **US-ALM5.2.2** `[Planned]` **As** an ALM engineer, **I want** duplicate schema names and secret-not-configured cases flagged, **so that** I avoid ambiguous references and broken Key Vault bindings.
  - **AC:** Duplicate schema names and secret variables lacking a configured secret store are findings.

## FEAT-ALM5-3 — Format and type validation `[Planned]`
- **US-ALM5.3.1** `[Planned]` **As** a System Customizer, **I want** URL, JSON, boolean, and number values validated against their declared type, **so that** malformed values do not fail at runtime.
  - **AC:** Invalid URL/JSON format and boolean/number parse failures are findings tied to the variable and type.
- **US-ALM5.3.2** `[Planned]` **As** an ALM engineer, **I want** invalid data types and environment-specific values accidentally moved detected, **so that** DEV values do not leak into PROD.
  - **AC:** Type mismatches and values matching a known non-target pattern (reusing DRA's packaged-value check) are flagged.

## FEAT-ALM5-4 — Usage tracing and unused detection `[Planned]`
- **US-ALM5.4.1** `[Planned]` **As** an ALM engineer, **I want** variables referenced by flows/plugins/web resources/custom APIs traced, **so that** I know which are actually in use.
  - **AC:** References are resolved by scanning flow `clientdata`, plugin/custom-API config, and web resources; each variable shows its consumers.
- **US-ALM5.4.2** `[Planned]` **As** an ALM engineer, **I want** unused variables and referenced-but-not-in-solution cases flagged, **so that** I can clean up and avoid broken references.
  - **AC:** Unused variables → Low/Info; referenced-but-not-included → higher severity per the source rules.

## FEAT-ALM5-5 — Cross-environment compare and export `[Planned]`
- **US-ALM5.5.1** `[Planned]` **As** an Enterprise Architect, **I want** variable values compared across DEV/TEST/UAT/PROD, **so that** I can spot values that differ or are missing in the target.
  - **AC:** With an optional second connection (`TargetOrganization` pattern), a comparison panel shows value differences and missing-in-target variables.
- **US-ALM5.5.2** `[Planned]` **As** a DEVOPS engineer, **I want** a deployment checklist and Excel/PDF/JSON/HTML export with a pass flag, **so that** I can gate a pipeline on unconfigured variables.
  - **AC:** JSON carries pass/fail and suggested exit code; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default; validation rules engine is UI-free and unit-tested; overlap with Deployment Risk Analyzer documented and its env-var analyzer reused. Secret values never displayed or persisted.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/EnvironmentVariableValidator/ when implementation starts.
