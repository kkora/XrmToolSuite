# Template Tool - User Stories

Starter user-story backlog for a XrmToolSuite tool. `New-Tool.ps1` copies this file to
`docs/backlog/<YourTool>.md` and renames it - **replace the TODO placeholders with real work
items, then move it under the right `docs/backlog/<category>/` folder.** See the
[index](../README.md) for personas, ID scheme, and status legend.

Pick a short area tag for this tool (e.g. `TT`) and use it in the IDs below.

> **This is a starter backlog** - a checklist for turning a fresh scaffold into a real tool.
> Replace the `<TODO>` placeholders below with this tool's real Epic, Features, and User Stories.

---

## EPIC-TT - <TODO: one-sentence goal of this tool>

> **As** a <persona>, **I want** <the core capability this tool provides>, **so that** <the outcome
> or problem it solves>.

**Outcome:** <TODO: the measurable result a user gets from this tool.>

---

## FEAT-TT-0 - Scaffold & shared wiring `[Done]`

- **US-TT-0.1** `[Done]` **As** a TOOLDEV, **I want** the tool to load in XrmToolBox with connection,
  settings, and background execution wired via `BaseToolControl`, **so that** feature work starts from a working shell.
  - **AC:** Tool appears in XTB, connects, runs a background query, persists settings on close.
- **US-TT-0.2** `[Planned]` **As** a TOOLDEV, **I want** to replace the placeholder
  `UserName`/`HelpUrl` and the "Load sample" command with this tool's real identity and entry points,
  **so that** no template leftovers ship.
  - **AC:** No `your-github-username` and no sample-data placeholder remain in the shipped control.

## FEAT-TT-1 - <TODO: first real capability>

- **US-TT-1.1** `[Planned]` **As** a <persona>, **I want** <capability>, **so that** <benefit>.
  - **AC:** <observable, testable condition>
  - **AC:** All Dataverse access runs through `RunAsync` / `RetrieveAll` with progress and cancellation.
- **US-TT-1.2** `[Planned]` **As** a <persona>, **I want** <capability>, **so that** <benefit>.
  - **AC:** <observable, testable condition>

## FEAT-TT-2 - Results & export `[Planned]`

- **US-TT-2.1** `[Planned]` **As** a <persona>, **I want** to review results in the tool, **so that**
  I can act on them.
- **US-TT-2.2** `[Planned]` **As** a <persona>, **I want** to export results (CSV/Excel/JSON as
  appropriate), **so that** I can share or automate.

---

## Definition of Done (tool-level)

- Every Dataverse call runs off the UI thread via `RunAsync` / `WorkAsync`.
- Any destructive operation shows a confirmation dialog stating scope and record count first.
- Settings round-trip (load on `Load`, save in `ClosingPlugin`).
- nuspec id/version/description/tags are correct; the tool packs cleanly for the Tool Library.
- README.md and DEPLOYMENT.md describe this tool (not "Template Tool").
