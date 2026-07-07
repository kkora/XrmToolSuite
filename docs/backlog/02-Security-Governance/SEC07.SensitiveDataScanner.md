# Sensitive Data Scanner — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 2 (Security & Governance), item 7. Not in pack file.
> **Suggested tag:** `SEC07` · **Suggested project:** `XrmToolSuite.SensitiveDataScanner`
> **Overlaps:** Feeds "sensitive fields without field security" to Field Security Profiler (SEC03) and "sensitive without audit" to Audit Compliance Checker (SEC05); data-protection sub-score feeds Environment Governance Score (SEC09).
> **Value/priority (my read):** High — organizations rarely know where PII lives; a metadata-first inventory with safe optional sampling is high-value and low-risk if defaults are right.

## Notes
- **Safety-first:** default is **metadata-only** — scan table/column names, descriptions, and data types; NO record reads unless the user explicitly opts in to sampling. Sample mode uses safe caps (small `TopCount`, capped tables) and **masks all sampled values** in UI and exports. Never export raw sensitive values. Secure-logging rules: never log sampled values.
- Metadata: `RetrieveAllEntitiesRequest`/`RetrieveEntityMetadata` for names, descriptions, `AttributeTypeCode`, `IsSecured`, `IsAuditEnabled`, and form/view presence.
- Detection: sensitive-name patterns (SSN, DOB, passport, tax ID, IBAN/bank, health, national ID), sensitive descriptions, sensitive data types; PII-like value patterns only in opt-in sample mode.
- Risk rules: unprotected sensitive columns, sensitive fields without field security, without audit, exposed on forms/views. Severity per finding.
- Read-only. Metadata cached; any sampling paged via `RetrieveAll` off the UI thread with progress + cancellation. Keep the pattern-matching engine UI-free and unit-testable.

---

## EPIC-SEC07 — Inventory where sensitive data lives and whether it is protected
> **As** a SEC lead / DPO (SEC), **I want** to scan metadata (and optionally safe samples) to find sensitive columns and their protection status, **so that** I can build a PII inventory and close protection gaps.

**Outcome:** a sensitive-data inventory with per-column risk severity and protection status (secured? audited? exposed?), remediation recommendations, and a masked export.

---

## FEAT-SEC07-1 — Scan configuration `[Planned]`
- **US-SEC07.1.1** `[Planned]` **As** a SEC lead, **I want** a metadata-only scan as the default with sampling strictly opt-in, **so that** I never accidentally read sensitive records.
  - **AC:** Sampling is off unless explicitly enabled; enabling it shows a warning about masking and caps.
- **US-SEC07.1.2** `[Planned]` **As** an ADM, **I want** to scope the scan to tables/solutions and tune the sensitivity ruleset, **so that** results fit our data model.
  - **AC:** Scope and ruleset choices persist in settings.

## FEAT-SEC07-2 — Metadata detection `[Planned]`
- **US-SEC07.2.1** `[Planned]` **As** a SEC lead, **I want** columns flagged by sensitive name, description, and data type, **so that** I find candidate PII from metadata alone.
  - **AC:** Detection runs off cached metadata; each hit records which rule fired.
- **US-SEC07.2.2** `[Planned]` **As** an ADM, **I want** a findings grid by table/column with the matched pattern, **so that** I can review and dismiss false positives.

## FEAT-SEC07-3 — Optional safe sampling `[Planned]`
- **US-SEC07.3.1** `[Planned]` **As** a SEC lead, **I want** opt-in sampling to detect PII-like value patterns under safe caps, **so that** I can confirm suspected columns without a full scan.
  - **AC:** Sample size is capped; all sampled values are masked in UI and never exported raw; values are never logged.

## FEAT-SEC07-4 — Protection status `[Planned]`
- **US-SEC07.4.1** `[Planned]` **As** a SEC lead, **I want** each sensitive column's protection status (field security? audit? form/view exposure?), **so that** I see gaps at a glance.
  - **AC:** Unprotected sensitive columns are flagged with a severity (Critical/High/Medium/Low/Info).

## FEAT-SEC07-5 — Inventory & recommendations `[Planned]`
- **US-SEC07.5.1** `[Planned]` **As** a DPO, **I want** a sensitive-data inventory with remediation suggestions (secure this, enable audit, remove from view), **so that** each finding has a next step.
  - **AC:** Recommendations are read-only; the tool changes nothing.

## FEAT-SEC07-6 — Export `[Planned]`
- **US-SEC07.6.1** `[Planned]` **As** a SEC lead, **I want** to export the sensitive-data report to Excel/PDF/CSV/HTML with all sample values masked, **so that** I can share it safely.
  - **AC:** No raw sensitive value ever appears in an export; export runs off the UI thread.

## Definition of Done
- Follows suite conventions; read-only default; metadata-only default with masked opt-in sampling; sensitive values masked in exports; export formats: Excel, PDF, CSV, HTML.
- Testing skeleton under testing/SensitiveDataScanner/ when implementation starts.
