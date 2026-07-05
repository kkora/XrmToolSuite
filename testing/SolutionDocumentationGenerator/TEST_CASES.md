# Solution Documentation Generator - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (xUnit — `testing/UnitTests/SolutionDocumentationGeneratorTests.cs`)

| ID | Case | Traces to | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN5-COUNT-01 | `ComponentCount` rollup | US-SOLN5.3.2 / 5.2.3 | Per-category counts (Schema=2, Forms=1, Config=2) and total=12 correct | Automated | Pass |
| TC-SOLN5-MODE-02 | Executive Summary omits detail | US-SOLN5.1.1 | Keeps Architecture/Inventory/ReleaseNotes; omits Schema/Plugins/Diagrams | Automated | Pass |
| TC-SOLN5-MODE-03 | Full Solution Reference includes all | US-SOLN5.1.1 | Every `SectionKinds` section present; `ModeLabel` = "Full Solution Reference" | Automated | Pass |
| TC-SOLN5-MODE-04 | Standard Reference drops only diagrams | US-SOLN5.1.1 | Schema/Config present; Diagrams absent | Automated | Pass |
| TC-SOLN5-SECT-05 | Unchecked section excluded | US-SOLN5.1.1 | Plugins + WebResources unchecked → absent; Schema still present | Automated | Pass |
| TC-SOLN5-SCHEMA-06 | Per-table column detail (Full) | US-SOLN5.2.1 | Schema section has a per-table column table naming the table + a `telephone1` row | Automated | Pass |
| TC-SOLN5-NA-07 | "Not available" degradation | US-SOLN5.2.3 | Null `Entities` → Schema section carries a "not available" note (no crash) | Automated | Pass |
| TC-SOLN5-MD-08 | Markdown well-formed | US-SOLN5.5.1 / 5.3.1 | Starts `# Contoso Sales`; section headers present; fenced ` ```mermaid `; inventory table | Automated | Pass |
| TC-SOLN5-HTML-09 | HTML self-contained + theme-aware | US-SOLN5.5.1 / 5.1.2 | `<!DOCTYPE html>`, `prefers-color-scheme:dark`, no external fetches, branding header, section headings | Automated | Pass |
| TC-SOLN5-JSON-10 | JSON structured inventory | US-SOLN5.5.1 | `"uniqueName":"contoso_sales"`, `"kind":"Inventory"`/`"Schema"`; one `kind` per section | Automated | Pass |

## Manual (GUI + live Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-SOLN5-M01 | Tool loads + connects | US-SOLN5.1.1 | Open the tool in XrmToolBox, connect | Loads, connects; settings restore | Manual | Pending |
| TC-SOLN5-M02 | Load solutions | US-SOLN5.1.1 | Click Load solutions | Combo populates off-thread with visible solutions | Manual | Pending |
| TC-SOLN5-M03 | Generate (Standard) + preview | US-SOLN5.4.1 | Pick a solution → Generate | Per-section progress; preview shows Markdown; sections match the checklist | Manual | Pending |
| TC-SOLN5-M04 | Preview toggle Markdown/HTML | US-SOLN5.4.1 | Switch Preview combo | Preview re-renders as Markdown vs HTML source | Manual | Pending |
| TC-SOLN5-M05 | Mode switch | US-SOLN5.1.1 | Executive → Full; regenerate | Executive = summary sections only; Full = all incl. diagrams + column detail | Manual | Pending |
| TC-SOLN5-M06 | Sections checklist | US-SOLN5.1.1 | Untick a few sections; regenerate | Unticked sections absent from preview/exports | Manual | Pending |
| TC-SOLN5-M07 | Branding | US-SOLN5.1.2 | Set header/logo/publisher; regenerate → export HTML | Header + logo + publisher in the document header | Manual | Pending |
| TC-SOLN5-M08 | Export Markdown/HTML/JSON | US-SOLN5.5.1 | Export each; open | Files valid; HTML self-contained; JSON parses; MD renders on GitHub | Manual | Pending |
| TC-SOLN5-M09 | Export Word (.docx, OpenXML) | US-SOLN5.5.1 | Export Word; open in Word | Multi-section .docx with headings + native tables | Manual | Pending |
| TC-SOLN5-M10 | Export PDF (MigraDoc-GDI) | US-SOLN5.5.1 | Export PDF; open | Titled multi-section PDF with tables + page numbers | Manual | Pending |
| TC-SOLN5-M11 | Export Excel (ClosedXML) | US-SOLN5.5.1 | Export Excel; open | Overview sheet (metadata + inventory) + one sheet per section | Manual | Pending |
| TC-SOLN5-M12 | Cancellation | US-SOLN5.4.1 | Start Generate on a large solution → cancel | Scan stops promptly; UI stays responsive | Manual | Pending |
| TC-SOLN5-M13 | Degradation on permission gap | US-SOLN5.2.3 | Scan with a source the user cannot read | "Not available" note in the affected section; scan completes | Manual | Pending |
| TC-SOLN5-M14 | No secret leakage | US-SOLN5.2.3 | Scan a solution with a Secret env var | Config section shows the variable + type only — never a value | Manual | Pending |
| TC-SOLN5-M15 | Settings round-trip | US-SOLN5.1.1 | Change mode/sections/branding, close, reopen | Choices restored | Manual | Pending |

> Save a screenshot under `screenshots/` for each executed manual case.
