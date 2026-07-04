# Connection Reference Validator — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 1 (ALM & DevOps), item 6. Not in pack file.
> **Suggested tag:** `ALM6` · **Suggested project:** `XrmToolSuite.ConnectionReferenceValidator`
> **Overlaps:** Deployment Risk Analyzer already detects unbound/missing connection references and flows referencing non-existent connection references — this tool is a *dedicated validator* (owner/status/connector-mismatch, usage mapping, non-prod-endpoint detection, cross-env compare). Note the overlap and reuse DRA's connection-reference and flow `clientdata` analyzers.
> **Value/priority (my read):** Medium — real deployment-failure pain, but a solid slice ships in Deployment Risk Analyzer; the differentiator is owner/status/endpoint validation and per-flow usage mapping.

## Notes
- Core tables: `connectionreference` (connectorid, logicalname, connectionid), `connection`, `workflow`/flow `clientdata`, `canvasapp`.
- Some status/owner/expiry facts live in the Power Platform / connector APIs, not core Dataverse — retrieve where available and degrade to Info when not (the source says "where available").
- Read-only; output is validation findings, a remediation panel, and a deployment checklist — no writes/rebinding.
- Reuse shared-core `RetrieveAll`; lift DRA's flow `clientdata` parsing to resolve which flows reference each connection reference; keep the rules engine UI-free.
- Cross-environment compare implies an optional second connection (mirror Deployment Risk Analyzer's `TargetOrganization` dual-connection pattern); single-env validation must work without it.
- "Non-production endpoint used in production" needs a configurable endpoint/allowlist convention — make it a setting, not a hard-coded list.

---

## EPIC-ALM6 — Validate connection references before deployment
> **As** an **ALM** engineer, **I want** to confirm every connection reference is present, mapped, owned correctly, and pointing at the right connector/endpoint before deploy, **so that** flows and apps do not break in the target environment.

**Outcome:** a per-reference validation status with usage mapping, a remediation panel, a cross-environment comparison, and a deployment checklist, exportable for humans and pipelines.

---

## FEAT-ALM6-1 — Inventory connection references and usage `[Planned]`
- **US-ALM6.1.1** `[Planned]` **As** an ALM engineer, **I want** all connection references listed with connector name and logical name, **so that** I have the full set to validate.
  - **AC:** References load off the UI thread via `RetrieveAll` with progress; connector and logical name are shown per row.
- **US-ALM6.1.2** `[Planned]` **As** an ALM engineer, **I want** the flows/apps/components using each reference mapped, **so that** I know the blast radius of a broken reference.
  - **AC:** Usage is resolved by scanning flow `clientdata` and canvas apps; a usage panel lists consumers per reference.

## FEAT-ALM6-2 — Presence and mapping validation `[Planned]`
- **US-ALM6.2.1** `[Planned]` **As** a release manager, **I want** missing and unmapped connection references detected, **so that** an import does not leave flows unbound.
  - **AC:** Missing references and references not mapped in the target are Critical/High findings (reusing DRA's unbound-reference logic).
- **US-ALM6.2.2** `[Planned]` **As** an ALM engineer, **I want** references not included in the solution and flows using direct connections instead of references detected, **so that** the package is portable across environments.
  - **AC:** Solution-missing references and direct-connection usage are distinct findings with remediation.

## FEAT-ALM6-3 — Owner, status, and connector validation `[Planned]`
- **US-ALM6.3.1** `[Planned]` **As** a Security/Governance owner, **I want** owner mismatch and disabled connections detected, **so that** references are not owned by the wrong user or pointing at a dead connection.
  - **AC:** Owner mismatch and disabled connection are findings; degrade to Info where owner/status is unavailable via API.
- **US-ALM6.3.2** `[Planned]` **As** an ALM engineer, **I want** connector mismatch and expired/invalid connection status detected where available, **so that** a reference actually resolves at runtime.
  - **AC:** Connector-vs-reference mismatch and expired/invalid status (where the API exposes it) are findings; the source's "where available" is honored.

## FEAT-ALM6-4 — Environment-appropriateness and unused detection `[Planned]`
- **US-ALM6.4.1** `[Planned]` **As** an Enterprise Architect, **I want** production references pointing at non-production services detected, **so that** PROD does not call a DEV/TEST endpoint.
  - **AC:** A configurable non-prod endpoint/allowlist setting drives detection; violations are High findings.
- **US-ALM6.4.2** `[Planned]` **As** an ALM engineer, **I want** unused references and references used only by inactive/disabled flows flagged, **so that** I can clean up and spot flows disabled due to a connection.
  - **AC:** Unused → Low/Info; reference-used-by-inactive-flow and flow-disabled-due-to-connection are distinct findings.

## FEAT-ALM6-5 — Cross-environment compare, remediation, and export `[Planned]`
- **US-ALM6.5.1** `[Planned]` **As** an ALM engineer, **I want** connection references compared across environments with generated remediation steps, **so that** I know exactly what to fix in the target.
  - **AC:** With an optional second connection (`TargetOrganization` pattern), a comparison panel shows mapping differences; each finding carries a remediation step.
- **US-ALM6.5.2** `[Planned]` **As** a DEVOPS engineer, **I want** a deployment checklist and Excel/PDF/JSON/HTML export with a pass flag, **so that** I can gate a pipeline on unmapped references.
  - **AC:** JSON carries pass/fail and suggested exit code; export runs off the UI thread.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default; validation rules engine is UI-free and unit-tested; overlap with Deployment Risk Analyzer documented and its connection-reference analyzer reused. No credentials/connection secrets displayed or persisted.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/ConnectionReferenceValidator/ when implementation starts.
