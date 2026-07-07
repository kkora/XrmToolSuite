# Plugin Registration Auditor — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 8 (Plugins & Custom APIs), item 2. Not in pack file.
> **Suggested tag:** `PLUGIN02` · **Suggested project:** `XrmToolSuite.PluginRegistrationAuditor`
> **Overlaps:** STRONG overlap with Deployment Risk Analyzer, which already flags duplicate SDK steps, missing types/assemblies, disabled steps, and execution-rank conflicts as *deploy-gating* risks. This tool's differentiator is **governance/quality auditing** — a standing registration quality score, naming standards, stage/mode correctness, image hygiene, and outdated assemblies — run continuously, not only at deploy time. Reuse Deployment Risk Analyzer's step-inventory queries and duplicate-step/disabled-step analyzers; present findings as a quality audit, not a release gate. Technical Debt Analyzer and Solution Complexity Score consume the same signals — feed, don't duplicate, the score.
> **Value/priority (my read):** Medium — high value for governance-mature teams, but meaningful overlap with Deployment Risk Analyzer means it must earn its place through the standing-audit/scoring angle.

## Notes
- Data sources: `pluginassembly` (version/culture/publickeytoken/isolationmode/sourcetype), `plugintype`, `sdkmessageprocessingstep` (stage/mode/rank/filteringattributes/impersonatinguserid/statecode/supporteddeployment), `sdkmessageprocessingstepimage`, `sdkmessage`, `solutioncomponent`/`solution` for expected-solution and managed/unmanaged checks.
- Rules are configuration-driven (expected stage/mode per message, required images, naming regex, expected owning solution) so audits reflect each org's standards; ship sensible defaults.
- Secure/unsecure configuration issues are audited by **presence/shape only** — secure-config values are never read or displayed; unsecure config is secret-redacted before any finding text.
- Feasibility caveat: "sync should be async" / "long-running operation" is heuristic without runtime telemetry — base it on message/stage patterns and mark it advisory (Plugin Performance Profiler owns duration data).
- Read-only tool — audits registration, never modifies steps. No destructive ops; a "recommended fix" panel describes changes but does not apply them.
- Shared-core reuse: `Service.RetrieveAll`, `BatchExecutor`, progress/cancellation, settings round-trip, shared reporting/export module; analyzers stay UI-free (operate on `IOrganizationService`) for console/CI lift.

---

## EPIC-PLUGIN02 — Continuously audit plugin registration quality and governance compliance
> **As** an ALM lead, **I want** a governance-focused auditor that scores plugin registrations against configurable standards, **so that** registrations stay correct, consistent, and maintainable between deployments.

**Outcome:** a plugin registration quality score, rule-based findings with severities (Critical/High/Medium/Low/Info), per-finding recommended fixes, and Excel/PDF/Word/JSON/CSV/HTML exports — from a live connection with no hand-written queries.

---

## FEAT-PLUGIN02-1 — Registration scan `[Planned]`
- **US-PLUGIN02.1.1** `[Planned]` **As** an ALM lead, **I want** to scan all plugin assemblies, types, and SDK steps, **so that** I have the full registration set to audit.
  - **AC:** Assemblies, types, and steps load via `Service.RetrieveAll` off the UI thread with progress and cancellation.
- **US-PLUGIN02.1.2** `[Planned]` **As** an ADM, **I want** assembly and solution filters, **so that** I can audit one assembly or solution at a time.
  - **AC:** Selecting an assembly/solution scopes the step grid and findings; selection persists via settings.
- **US-PLUGIN02.1.3** `[Planned]` **As** an ALM lead, **I want** a plugin step grid with a rule findings grid alongside, **so that** I can move between a step and the rules it violates.
  - **AC:** Selecting a step filters the findings grid to that step and shows a registration details panel.

## FEAT-PLUGIN02-2 — Correctness & consistency rules `[Planned]`
- **US-PLUGIN02.2.1** `[Planned]` **As** a DEVOPS, **I want** `Update` steps with no filtering attributes flagged High, **so that** plugins don't fire on every field change.
  - **AC:** Update message + empty filteringattributes → High finding naming the entity.
- **US-PLUGIN02.2.2** `[Planned]` **As** an ALM lead, **I want** duplicate active steps and steps registered on all attributes detected, **so that** redundant/over-broad registrations surface.
  - **AC:** Steps matching message+entity+stage+mode → duplicate High; steps with no attribute filter where the message supports one → Medium.
- **US-PLUGIN02.2.3** `[Planned]` **As** an ARCH, **I want** incorrect pipeline stage/mode and rank flagged against configured rules, **so that** steps run where and when intended.
  - **AC:** Stage/mode/rank deviating from the configured expectation → Medium/High with the expected value in the finding.

## FEAT-PLUGIN02-3 — Image & configuration hygiene `[Planned]`
- **US-PLUGIN02.3.1** `[Planned]` **As** a TOOLDEV, **I want** missing required images and excessive image attributes flagged, **so that** image usage stays correct and lean.
  - **AC:** Missing rule-required image → Medium/High; image with all/too-many attributes → Medium.
- **US-PLUGIN02.3.2** `[Planned]` **As** a SEC reviewer, **I want** secure/unsecure configuration issues flagged without exposing secrets, **so that** config usage is governed safely.
  - **AC:** Config presence/shape issues are flagged; secure values never read/displayed, unsecure values secret-redacted.
- **US-PLUGIN02.3.3** `[Planned]` **As** an ADM, **I want** plugins running as an unexpected impersonating user flagged, **so that** privilege escalation via registration is caught.
  - **AC:** impersonatinguserid outside an allowed set → High with the user named.

## FEAT-PLUGIN02-4 — Deployment & lifecycle rules `[Planned]`
- **US-PLUGIN02.4.1** `[Planned]` **As** an ALM lead, **I want** disabled steps and unmanaged plugin steps in production flagged, **so that** dead or out-of-process registrations are visible.
  - **AC:** statecode=Disabled → Medium/High; unmanaged step in a managed/production context → High.
- **US-PLUGIN02.4.2** `[Planned]` **As** a DEVOPS, **I want** outdated assembly versions and steps outside their expected solution flagged, **so that** drift is caught.
  - **AC:** Assembly version behind the configured baseline → Medium/High; step whose owning solution ≠ expected → Medium.
- **US-PLUGIN02.4.3** `[Planned]` **As** an ALM lead, **I want** naming standards validated for types, steps, and image aliases, **so that** registrations are consistent and discoverable.
  - **AC:** Names failing the configured regex → Low/Medium with the offending name and expected pattern.

## FEAT-PLUGIN02-5 — Score, recommendations & export `[Planned]`
- **US-PLUGIN02.5.1** `[Planned]` **As** a MGR, **I want** a registration quality score on an audit dashboard, **so that** I can track governance health over time.
  - **AC:** Weighted severities produce a 0–100 score with a Low/Medium/High band on score cards.
- **US-PLUGIN02.5.2** `[Planned]` **As** a TOOLDEV, **I want** a recommended-fix panel per finding, **so that** I know the concrete correction (add filters, fix stage, register image, bump version).
  - **AC:** Each finding carries a plain-language recommended fix; nothing is applied automatically.
- **US-PLUGIN02.5.3** `[Planned]` **As** a MGR, **I want** to export the audit report to Excel, PDF, Word, JSON, CSV, and HTML, **so that** I can share governance results.
  - **AC:** All listed formats export from the shared reporting module and open on demand.

## Definition of Done
- Follows suite conventions; read-only default (no registration changes; fixes are described, not applied); export formats as listed (Excel/PDF/Word/JSON/CSV/HTML).
- All Dataverse access off the UI thread via `RunAsync`/`RetrieveAll`; analyzers UI-free; secure-config secrets never exposed; heuristic rules marked advisory.
- Clearly differentiated from Deployment Risk Analyzer (standing governance audit + score, not a deploy gate); reuses its step-inventory/duplicate-step analyzers.
- Testing skeleton under `testing/PluginRegistrationAuditor/` when implementation starts; SDK-free rule/scoring logic covered by `testing/UnitTests`.
