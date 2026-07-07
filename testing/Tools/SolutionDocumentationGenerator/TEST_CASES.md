# Solution Documentation Generator - Test Cases

Status: `Pass` / `Fail` / `Pending`. Type: `Automated` (xUnit, executed) / `Manual` (GUI + Dataverse).

## Automated (xUnit — `testing/UnitTests/SolutionDocumentationGeneratorTests.cs`)

| ID | Case | Traces to | Expected | Type | Status |
|---|---|---|---|---|---|
| TC-SOLN05-COUNT-01 | `ComponentCount` rollup | US-SOLN05.3.2 / 5.2.3 | Per-category counts (Schema=2, Forms=1, Config=2) and total=12 correct | Automated | Pass |
| TC-SOLN05-MODE-02 | Executive Summary omits detail | US-SOLN05.1.1 | Keeps Architecture/Inventory/ReleaseNotes; omits Schema/Plugins/Diagrams | Automated | Pass |
| TC-SOLN05-MODE-03 | Full Solution Reference includes all | US-SOLN05.1.1 | Every `SectionKinds` section present; `ModeLabel` = "Full Solution Reference" | Automated | Pass |
| TC-SOLN05-MODE-04 | Standard Reference drops only diagrams | US-SOLN05.1.1 | Schema/Config present; Diagrams absent | Automated | Pass |
| TC-SOLN05-SECT-05 | Unchecked section excluded | US-SOLN05.1.1 | Plugins + WebResources unchecked → absent; Schema still present | Automated | Pass |
| TC-SOLN05-SCHEMA-06 | Per-table column detail (Full) | US-SOLN05.2.1 | Schema section has a per-table column table naming the table + a `telephone1` row | Automated | Pass |
| TC-SOLN05-NA-07 | "Not available" degradation | US-SOLN05.2.3 | Null `Entities` → Schema section carries a "not available" note (no crash) | Automated | Pass |
| TC-SOLN05-MD-08 | Markdown well-formed | US-SOLN05.5.1 / 5.3.1 | Starts `# Contoso Sales`; section headers present; fenced ` ```mermaid `; inventory table | Automated | Pass |
| TC-SOLN05-HTML-09 | HTML self-contained + theme-aware | US-SOLN05.5.1 / 5.1.2 | `<!DOCTYPE html>`, `prefers-color-scheme:dark`, no external fetches, branding header, section headings | Automated | Pass |
| TC-SOLN05-JSON-10 | JSON structured inventory | US-SOLN05.5.1 | `"uniqueName":"contoso_sales"`, `"kind":"Inventory"`/`"Schema"`; one `kind` per section | Automated | Pass |
| TC-SOLN05-PORTAL-11 | HTML portal self-contained + searchable (folds in DOC05) | US-SOLN05.5.2 | `<!DOCTYPE html>`, `id="search"`, `id="themeBtn"`, inline `<script>`, branding; no `http://`/`https://` fetch; a `#sec-…` TOC link + `id="sec-…"` section per doc section | Automated | Pass |
| TC-SOLN05-PORTAL-12 | HTML portal escapes injected markup | US-SOLN05.5.2 | A component named `<script>alert(1)</script>` renders escaped (`&lt;script&gt;…`), never as a live tag | Automated | Pass |

## Manual (GUI + live Dataverse)

| ID | Case | Traces to | Steps | Expected | Type | Status |
|---|---|---|---|---|---|---|
| TC-SOLN05-M01 | Tool loads + connects | US-SOLN05.1.1 | Open the tool in XrmToolBox, connect | Loads, connects; settings restore | Manual | Pending |
| TC-SOLN05-M02 | Load solutions | US-SOLN05.1.1 | Click Load solutions | Combo populates off-thread with visible solutions | Manual | Pending |
| TC-SOLN05-M03 | Generate (Standard) + preview | US-SOLN05.4.1 | Pick a solution → Generate | Per-section progress; preview shows Markdown; sections match the checklist | Manual | Pending |
| TC-SOLN05-M04 | Preview toggle Markdown/HTML/Portal | US-SOLN05.4.1 | Switch Preview combo through all three | Preview re-renders as Markdown, HTML source, and HTML Portal source | Manual | Pending |
| TC-SOLN05-M13 | Export + browse HTML Portal (folds in DOC05) | US-SOLN05.5.2 | Export "HTML Portal (searchable)"; open the .html from disk offline | Single file opens from `file://`; sidebar TOC navigates; type in search → sections/rows filter live; section headers collapse; theme toggle flips light/dark | Manual | Pending |
| TC-SOLN05-M05 | Mode switch | US-SOLN05.1.1 | Executive → Full; regenerate | Executive = summary sections only; Full = all incl. diagrams + column detail | Manual | Pending |
| TC-SOLN05-M06 | Sections checklist | US-SOLN05.1.1 | Untick a few sections; regenerate | Unticked sections absent from preview/exports | Manual | Pending |
| TC-SOLN05-M07 | Branding | US-SOLN05.1.2 | Set header/logo/publisher; regenerate → export HTML | Header + logo + publisher in the document header | Manual | Pending |
| TC-SOLN05-M08 | Export Markdown/HTML/JSON | US-SOLN05.5.1 | Export each; open | Files valid; HTML self-contained; JSON parses; MD renders on GitHub | Manual | Pending |
| TC-SOLN05-M09 | Export Word (.docx, OpenXML) | US-SOLN05.5.1 | Export Word; open in Word | Multi-section .docx with headings + native tables | Manual | Pending |
| TC-SOLN05-M10 | Export PDF (MigraDoc-GDI) | US-SOLN05.5.1 | Export PDF; open | Titled multi-section PDF with tables + page numbers | Manual | Pending |
| TC-SOLN05-M11 | Export Excel (ClosedXML) | US-SOLN05.5.1 | Export Excel; open | Overview sheet (metadata + inventory) + one sheet per section | Manual | Pending |
| TC-SOLN05-M12 | Cancellation | US-SOLN05.4.1 | Start Generate on a large solution → cancel | Scan stops promptly; UI stays responsive | Manual | Pending |
| TC-SOLN05-M13 | Degradation on permission gap | US-SOLN05.2.3 | Scan with a source the user cannot read | "Not available" note in the affected section; scan completes | Manual | Pending |
| TC-SOLN05-M14 | No secret leakage | US-SOLN05.2.3 | Scan a solution with a Secret env var | Config section shows the variable + type only — never a value | Manual | Pending |
| TC-SOLN05-M15 | Settings round-trip | US-SOLN05.1.1 | Change mode/sections/branding, close, reopen | Choices restored | Manual | Pending |

> Save a screenshot under `screenshots/` for each executed manual case.
