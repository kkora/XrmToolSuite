# AI Solution Documentation Writer — User Stories (Candidate / Backlog)

> **Status:** Candidate backlog — not started (no code).
> **Source:** `all` — `prompt/3.XrmToolBox_ALL_PROMPTS.txt`, Section 9 (AI Assistants), item 6. Not in pack file.
> **Suggested tag:** `AI6` · **Suggested project:** `XrmToolSuite.AiSolutionDocumentationWriter`
> **Overlaps:** **AI Solution Reviewer (shipped)** — reuse AI client, session-only key, consent-preview, redaction, offline-fallback. Overlaps any Solution/Documentation generators in the suite (component-inventory extraction) and Solution Knowledge Graph (structure/relationships) — share the metadata extraction layer; do not rebuild it.
> **Value/priority (my read):** High — accurate, low-effort documentation is a perennial ask; deterministic metadata extraction alone yields a real component inventory and technical doc, with AI filling narrative and missing descriptions.

## Notes
- Deterministic first: extract tables, columns, relationships, forms, views, dashboards, charts, plugins, custom APIs, flows, business rules, web resources, PCFs, environment variables, connection references and security roles into a component inventory and a template-driven technical document — fully available with AI off.
- AI is opt-in and clearly labelled advisory: explain components in plain language, generate descriptions for undocumented components, produce glossary/release-notes/support-handoff/onboarding/testing narrative — it never writes back descriptions to metadata.
- Reuse AI Solution Reviewer's AI client: session-only API key (never persisted), consent preview of the exact redacted payload before any send, GUIDs/URLs/user names/emails masked, audit trail, offline fallback (document renders with deterministic sections only) on failure. Prompts/system-instructions configurable; documentation templates and sections user-selectable.
- Read-only tool. Metadata reads via `Service.RetrieveAll` / metadata requests off the UI thread through `RunAsync`; progress + cancellation; settings round-trip. Keep the extraction/template engine UI-free and unit-testable.

---

## EPIC-AI6 — Generate accurate, multi-audience solution documentation from metadata plus optional AI narrative
> **As** an ARCH / MGR, **I want** deterministic metadata-driven documentation with an optional AI layer that writes plain-language explanations and fills gaps, **so that** teams get maintainable docs without hand-authoring.

**Outcome:** a component inventory and template-driven technical/business/support/onboarding/testing documents, previewable and exportable — deterministic sections stand alone when AI is off.

---

## FEAT-AI6-1 — Deterministic rules-first extraction `[Planned]`
- **US-AI6.1.1** `[Planned]` **As** an ARCH, **I want** the full component inventory and technical document generated without any AI, **so that** the tool is useful and safe with AI disabled.
  - **AC:** Complete deterministic inventory and technical doc available offline; AI toggled off by default.
- **US-AI6.1.2** `[Planned]` **As** an ARCH, **I want** to select a solution and extract tables, columns, relationships, forms, views, dashboards, charts, plugins, custom APIs, flows, business rules, web resources, PCFs, environment variables, connection references and security roles, **so that** documentation reflects the real solution.
  - **AC:** Extraction via `RetrieveAll`/metadata requests off the UI thread with progress and cancellation.

## FEAT-AI6-2 — Templates, sections & preview `[Planned]`
- **US-AI6.2.1** `[Planned]` **As** an MGR, **I want** a documentation template selector and section checklist with a live preview, **so that** I control what the document contains.
  - **AC:** Template/section selections and branding settings round-trip via Load/ClosingPlugin as POCOs.
- **US-AI6.2.2** `[Planned]` **As** an ARCH, **I want** deterministic architecture-overview and component-inventory sections, **so that** the doc is accurate even with AI off.

## FEAT-AI6-3 — AI narrative & missing-description generation (opt-in) `[Planned]`
- **US-AI6.3.1** `[Planned]` **As** a CUST, **I want** AI to explain components in plain language and generate descriptions for undocumented components, **so that** business users can understand the solution.
  - **AC:** Consent preview of the exact redacted payload before send; session-only key; GUIDs/URLs/emails/names masked; labelled advisory; audit trail; offline fallback (deterministic sections render, AI sections omitted).
- **US-AI6.3.2** `[Planned]` **As** an ARCH, **I want** AI to generate a glossary, release notes, support-handoff notes, onboarding and testing guides, **so that** multiple audiences are served from one run.
  - **AC:** AI-generated descriptions are advisory and never written back to Dataverse metadata.

## FEAT-AI6-4 — Configurable prompt templates `[Planned]`
- **US-AI6.4.1** `[Planned]` **As** a TOOLDEV, **I want** editable AI prompt/system-instruction templates, **so that** the writing style matches our documentation standard.
  - **AC:** Templates round-trip via settings as POCOs; no key/connection details persisted.

## FEAT-AI6-5 — Export `[Planned]`
- **US-AI6.5.1** `[Planned]` **As** an ARCH, **I want** to export the documentation to Word/PDF/Markdown/HTML/Excel/JSON, **so that** I can publish it in our chosen format.
  - **AC:** Export runs off the UI thread with progress and cancellation; sensitive values masked; AI-authored sections labelled advisory.

## Definition of Done
- Follows suite conventions; deterministic extraction/documentation works with AI off; AI opt-in, redacted, consented, audited; export formats: Word, PDF, Markdown, HTML, Excel, JSON.
- Testing skeleton under testing/AiSolutionDocumentationWriter/ when implementation starts.
