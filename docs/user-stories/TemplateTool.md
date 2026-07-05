# Template Tool — User Stories (Scaffold)

> **Status:** Scaffold — the canonical starting point cloned by `scripts/New-Tool.ps1`, not a shipped tool.
> **Project:** `src/Tools/XrmToolSuite.TemplateTool` · **Area tag:** — (n/a; template)
> **Legend:** `[Scaffold]` = generic wiring every tool inherits. When you clone this tool, replace this
> file with the new tool's real stories (title, EPIC/FEAT/US ids, and `[Implemented]` / `[Implemented*]`
> markers) modelled on an implemented example such as
> [`DOC2.ErdGenerator.md`](DOC2.ErdGenerator.md).

The Template Tool is the reference scaffold for a suite tool: one MEF-registered `PluginBase` with the
required `SmallImageBase64`/`BigImageBase64` metadata, a `BaseToolControl` host, connection-aware command
wiring (`ExecuteMethod` → `RunAsync`), settings round-trip (`Load`/`ClosingPlugin`), and a Help & Support
button. It performs no domain work of its own; it exists so every new tool starts from the non-negotiable
suite conventions instead of a blank project.

---

## EPIC-TMPL — Provide a convention-correct starting point for new suite tools `[Scaffold]`
> **As** a **TOOLDEV**, **I want** a template tool that already embodies every non-negotiable XrmToolBox
> pattern, **so that** a new tool is scaffolded correct-by-construction and I only add its domain logic.

**Outcome:** `New-Tool.ps1` clones this project (renaming namespace, MEF `Name`/`Description`, nuspec, and
testing/user-story skeletons) into a tool that loads in XrmToolBox, connects, persists settings, and
surfaces Help — before any feature code is written.

---

## FEAT-TMPL-1 — MEF registration and load `[Scaffold]`
- **US-TMPL.1.1** `[Scaffold]` Exactly one class derives from `PluginBase` with
  `[Export(typeof(IXrmToolBoxPlugin))]` and unique `Name`/`Description`, plus the required
  `SmallImageBase64`/`BigImageBase64` metadata.
  - **AC:** The tool appears in the XrmToolBox **Tools** list at scan time. *(Manual — live XrmToolBox host.)*

## FEAT-TMPL-2 — Connection-safe command wiring `[Scaffold]`
- **US-TMPL.2.1** `[Scaffold]` Command handlers that need Dataverse call `ExecuteMethod(...)` and run work
  through `RunAsync`, updating the UI only in the completion callback.
  - **AC:** No `IOrganizationService` call runs on the UI thread; XrmToolBox prompts for a connection when
    none exists. *(Manual — live host.)*

## FEAT-TMPL-3 — Settings round-trip `[Scaffold]`
- **US-TMPL.3.1** `[Scaffold]` Settings load in the `Load` event via `LoadSettings<T>()` and save in
  `ClosingPlugin` via `SaveSettings(...)`, using a plain serializable POCO (no controls/credentials).
  - **AC:** A setting changed in one session is restored in the next. *(Manual — live host.)*

## FEAT-TMPL-4 — Help & Support `[Scaffold]`
- **US-TMPL.4.1** `[Scaffold]` A right-aligned Help button opens the shared Help & Support dialog with
  Documentation / Report-an-issue / support links pointing at `kkora/XrmToolSuite`.
  - **AC:** Uses `BaseToolControl.CreateHelpButton()` / `ShowHelpDialog(...)`; no placeholder
    `your-github-username` links remain. *(Manual — live host.)*

## Definition of Done
- Never carries feature-specific code; improvements here must stay generic to all tools.
- Every clone made by `New-Tool.ps1` replaces this file with the new tool's real user stories and updates
  the testing skeleton under `testing/<Tool>/`.
