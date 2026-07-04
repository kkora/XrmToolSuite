# Connection Usage Analyzer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 7 (Power Automate), item 5. Not in pack file.
> **Suggested tag:** `PA5` · **Suggested project:** `XrmToolSuite.ConnectionUsageAnalyzer`
> **Overlaps:** **Strong** — this substantially overlaps the ALM6 Connection Reference Validator *candidate* (owner/status/connector-mismatch, non-prod-endpoint, cross-env compare, deployment checklist) and the shipped Deployment Risk Analyzer (unbound/missing connection references, direct-connection detection). Before building, decide whether PA5 and ALM6 are **one tool** — they are near-duplicates. If both proceed, PA5's distinct angle is *connection/usage-centric* mapping (which flows and apps use each connection reference, unused references, connector inventory); ALM6's is *validation-centric*. Reuse DRA's connection-reference + `clientdata` analyzers regardless.
> **Value/priority (my read):** Medium — real deployment-break pain, but heavily overlapped by ALM6 and DRA; recommend consolidating with ALM6 rather than shipping both.

## Notes
- Core data: `connectionreference` (connectorid, connectionreferencelogicalname, connectionid), `connection`, `workflow`/`clientdata` (which flows use each reference), `canvasapp` (apps using references), `solutioncomponent` (solution membership), `systemuser` (owner).
- Some status/owner/expiry facts (disabled/invalid/expired connections, non-prod endpoint of a connection) live in the connector/Power Platform APIs, **not** core Dataverse. The source says "where available" repeatedly — retrieve where possible, degrade to Info when not. Do not claim connection health you cannot read from the org connection.
- Usage mapping is the differentiator: parse `clientdata` (reuse DRA/PA1 parser) to resolve, per connection reference, the exact list of flows (and apps) that consume it — and, inversely, references consumed by nothing (unused).
- Rules engine (UI-free, unit-tested) encodes the source's severities: Missing reference = Critical; Unmapped reference = Critical; Non-prod endpoint in prod = Critical; Disabled connection = Critical; Owner mismatch = High; Direct connection in solution flow = High; Unused reference = Low/Medium.
- Non-prod-endpoint detection needs a configurable endpoint allowlist setting, not a hard-coded list.
- Read-only; output is usage grid, per-reference flow-usage panel, risk findings, remediation checklist, and export. No writes/rebinding. Never display connection secrets or endpoint credentials — redact.

---

## EPIC-PA5 — Map connection usage and surface connection risk
> **As** an **ADM**, **I want** to see which flows and apps use each connection reference and where references are missing, unused, mis-owned, or unhealthy, **so that** deployments do not break on connection issues.

**Outcome:** a connection-reference inventory with per-reference usage mapping, prioritized risk findings, a remediation checklist, and an optional cross-environment comparison, exportable.

---

## FEAT-PA5-1 — Connection-reference and connector inventory `[Planned]`
- **US-PA5.1.1** `[Planned]` **As** an ADM, **I want** connection references listed with connector name/type, logical name, and owner, **so that** I have the full set to analyze.
  - **AC:** References load via `RetrieveAll` off the UI thread with progress/cancel; connector, logical name, and owner show per row.
- **US-PA5.1.2** `[Planned]` **As** an ADM, **I want** the connectors used across flows listed where available, **so that** I see which connectors the environment depends on.
  - **AC:** Connector inventory is aggregated from parsed flow definitions; connectors visible only via API degrade to "where available".

## FEAT-PA5-2 — Usage mapping `[Planned]`
- **US-PA5.2.1** `[Planned]` **As** an ADM, **I want** the flows using each connection reference shown, **so that** I know the blast radius if a reference breaks.
  - **AC:** Usage is resolved by scanning `clientdata` (reusing DRA/PA1 parser); a usage panel lists consuming flows per reference.
- **US-PA5.2.2** `[Planned]` **As** an ADM, **I want** apps/custom pages using each reference shown where available, **so that** I see non-flow consumers too.
  - **AC:** Canvas-app usage is resolved from `canvasapp` where possible; unavailable consumers are marked "where available".

## FEAT-PA5-3 — Presence, mapping, and ownership risk `[Planned]`
- **US-PA5.3.1** `[Planned]` **As** a release manager, **I want** missing, unmapped, and solution-excluded references detected, **so that** an import does not leave flows unbound.
  - **AC:** Missing/unmapped = Critical, solution-excluded = distinct finding; reuses DRA's unbound-reference logic.
- **US-PA5.3.2** `[Planned]` **As** an ADM, **I want** owner mismatch and flows using direct connections instead of references detected, **so that** references are portable and correctly owned.
  - **AC:** Owner mismatch = High, direct-connection-in-solution-flow = High; each carries remediation.

## FEAT-PA5-4 — Health, environment-appropriateness, and unused detection `[Planned]`
- **US-PA5.4.1** `[Planned]` **As** an ADM, **I want** disabled/invalid connections and production references pointing at non-production endpoints detected where available, **so that** PROD does not run on a dead or wrong-environment connection.
  - **AC:** Disabled/invalid connection and non-prod-in-prod = Critical; non-prod detection uses a configurable endpoint allowlist setting; unavailable health degrades to Info.
- **US-PA5.4.2** `[Planned]` **As** an ADM, **I want** unused connection references flagged, **so that** I can clean up the environment.
  - **AC:** References consumed by no flow/app = Low/Medium finding.

## FEAT-PA5-5 — Cross-environment compare, remediation, and export `[Planned]`
- **US-PA5.5.1** `[Planned]` **As** an ALM engineer, **I want** connection references compared across environments, **so that** I can see mapping differences before deploy.
  - **AC:** With an optional second connection (mirror DRA's `TargetOrganization` dual-connection pattern), a comparison panel shows per-reference differences; single-env analysis works without it.
- **US-PA5.5.2** `[Planned]` **As** a DEVOPS engineer, **I want** a deployment checklist and Excel/PDF/JSON/HTML export with a pass flag, **so that** I can gate a pipeline on connection issues.
  - **AC:** Export runs off the UI thread; JSON carries the usage map and a pass/fail flag; secrets/endpoints are redacted.

## Definition of Done
- Follows suite conventions (BaseToolControl, RunAsync/RetrieveAll, Load/SaveSettings, progress+cancel).
- Read-only default; rules engine is UI-free and unit-tested; overlap with ALM6 Connection Reference Validator and Deployment Risk Analyzer documented (consolidation with ALM6 recommended) and DRA's connection-reference/`clientdata` analyzers reused; connection secrets/endpoints never displayed or persisted.
- Export formats: Excel, PDF, JSON, HTML.
- Testing skeleton under testing/ConnectionUsageAnalyzer/ when implementation starts.
