# XrmToolSuite - User Stories & Backlog

Central home for the suite's agile backlog: a **Portfolio Epic** for the product and a **Platform
Epic** for the shared foundation (both below).

> **Per-tool stories moved.** Each tool's Epic → Features → User Stories now lives **by category**
> under [`backlog/`](backlog/) — both the shipped tools (marked `✅ Done`) and the candidate
> tools from the prompt pack. This file keeps the portfolio/platform epics and the `_TEMPLATE.md`
> that `New-Tool.ps1` stamps for new tools. See the [backlog index](backlog/README.md).

## Contents

| File | Scope | Status |
|---|---|---|
| this file | Portfolio Epic + Platform Epic | Living |
| [_TEMPLATE.md](_TEMPLATE.md) | Starter stamped into every new tool by `New-Tool.ps1` | Template |

### Shipped-tool stories (moved to the category backlog)

| Tool | Now at | Status |
|---|---|---|
| Deployment Risk Analyzer | [backlog/01-ALM-DevOps/DG1.DeploymentRiskAnalyzer.md](backlog/01-ALM-DevOps/DG1.DeploymentRiskAnalyzer.md) | ✅ Done |
| Attribute Auditor | [backlog/04-Dataverse-Administration/AA1.AttributeAuditor.md](backlog/04-Dataverse-Administration/AA1.AttributeAuditor.md) | ✅ Done |
| Solution Complexity Score | [backlog/05-Solution-Management/SC1.SolutionComplexityScore.md](backlog/05-Solution-Management/SC1.SolutionComplexityScore.md) | ✅ Done |
| Solution Knowledge Graph | [backlog/05-Solution-Management/KG1.SolutionKnowledgeGraph.md](backlog/05-Solution-Management/KG1.SolutionKnowledgeGraph.md) | ✅ Done |
| Technical Debt Analyzer | [backlog/05-Solution-Management/TD1.TechnicalDebtAnalyzer.md](backlog/05-Solution-Management/TD1.TechnicalDebtAnalyzer.md) | ✅ Done |
| AI Solution Reviewer | [backlog/09-AI-Assistants/AR1.AiSolutionReviewer.md](backlog/09-AI-Assistants/AR1.AiSolutionReviewer.md) | ✅ Done |

> These six shipped **before** the tagging convention. Since then all 20 of the ranked
> [NEXT-20](backlog/NEXT-20.md) tools have also shipped — the suite now has **26 tools total**. The
> full as-built user-story index (shipped tools + the tagged NEXT-20 tools) lives at
> [`user-stories/README.md`](user-stories/README.md).

## Hierarchy & conventions

```
Portfolio Epic  (the suite as a product)
  Platform Epic (shared core, template, tooling, packaging)   -> this file
  Tool Epic per tool                                          -> one file each, in backlog/<category>/
```

Each **Epic** contains **Features**; each Feature contains **User Stories** written as
`As a <persona>, I want <capability>, so that <benefit>`, with **Acceptance Criteria (AC)**.

**ID scheme:** `EPIC-<area>`, `FEAT-<area>-<n>`, `US-<area>-<n>.<m>`.
Areas: `XTS` (portfolio), `PLAT` (platform), `DG` (Deployment Risk Analyzer), `TD` (Technical Debt
Analyzer), `SC` (Solution Complexity Score), `AR` (AI Solution Reviewer), `KG` (Solution Knowledge
Graph), `AA` (Attribute Auditor), and a short tag per new tool.

**Status legend:** `[Done]` shipped - `[WIP]` in progress - `[Planned]` not started.

## Personas

| Tag | Persona | Cares about |
|---|---|---|
| **ADM** | Dataverse / D365 Administrator | Environment health, governance, cleanup |
| **ALM** | Solution Architect / ALM Engineer | Safe dev -> test -> prod solution deployments |
| **CUST** | System Customizer | Tables, columns, forms, business logic |
| **DEVOPS** | DevOps / CI Engineer | Automated, gated release pipelines |
| **TOOLDEV** | Tool Developer | Building and shipping tools in this suite |

---

## EPIC-XTS - Portfolio Epic: XrmToolSuite

> **As** a Dataverse administration team, **we want** a cohesive suite of XrmToolBox tools that
> share one core and one look-and-feel, **so that** we can standardize admin/ALM tasks and ship
> new tools quickly without per-tool infrastructure work.

**Business outcomes**
- Reduce time-to-first-tool for a new admin need from days to minutes.
- Consistent connection handling, threading, settings, and packaging across every tool.
- Publishable to the official XrmToolBox Tool Library.

**Success measures**
- A new tool can be scaffolded, built, and loaded in XrmToolBox in under 10 minutes.
- Every tool passes Tool Library packaging rules with zero manual fix-ups.
- Each tool ships as a single DLL (plus declared extras only where justified).

Child epics: **EPIC-PLAT** (below) and one Tool Epic per file above.

---

## EPIC-PLAT - Platform & Developer Enablement

> **As** a **TOOLDEV**, **I want** a shared foundation (core library-as-source, a template, a
> generator script, and packaging conventions), **so that** every tool behaves consistently and I
> never re-solve connection/threading/paging/packaging.

### FEAT-PLAT-1 - Shared Core (compiled into every tool) `[Done]`

- **US-PLAT-1.1** `[Done]` **As** a TOOLDEV, **I want** `BaseToolControl` to wrap connection,
  background execution, status bar, settings, and error handling, **so that** tools contain only
  their own logic.
  - **AC:** `RunAsync(msg, work, done)` runs Dataverse calls off the UI thread and marshals results back on it.
  - **AC:** `ExecuteMethod(...)` prompts for a connection when none exists.
  - **AC:** `UpdateConnection` clears `MetadataCache` so metadata never leaks across environments.
- **US-PLAT-1.2** `[Done]` **As** a TOOLDEV, **I want** `RetrieveAll` paging and `BatchExecutor`
  chunking with progress + cancellation, **so that** I never hand-write paging or `ExecuteMultiple` loops.
  - **AC:** `RetrieveAll` follows paging cookies to completion and reports running counts.
  - **AC:** `BatchExecutor` chunks writes (default 200), collects faults, and honors the `BackgroundWorker` cancel.
- **US-PLAT-1.3** `[Done]` **As** a TOOLDEV, **I want** shared source compiled into each assembly
  (not a shared DLL), **so that** two tools can never load conflicting core versions in XTB's single process.
  - **AC:** No `XrmToolSuite.Core.dll` is produced or shipped; the core is included via the `Compile` glob.

### FEAT-PLAT-2 - Tool generator (`New-Tool.ps1`) `[Done]`

- **US-PLAT-2.1** `[Done]` **As** a TOOLDEV, **I want** one command to stamp out a new tool project
  from the template, **so that** naming, wiring, and solution membership are correct by construction.
  - **AC:** `New-Tool.ps1 -Name -DisplayName -Description` clones the template, renames files/namespaces, and adds the project to the solution.
  - **AC:** The caller's description is written into the nuspec `<description>` and `<summary>`.
  - **AC:** Generated docs (README, DEPLOYMENT.md, and `docs/backlog/<Name>.md`) are re-tokenized with the new tool's names.
- **US-PLAT-2.2** `[Planned]` **As** a TOOLDEV, **I want** the generator to stamp the author's
  GitHub identity into the control's `UserName`/`HelpUrl`, **so that** "Report a bug"/help links
  are correct without manual edits.
  - **AC:** No `your-github-username` placeholder remains in any generated file.

### FEAT-PLAT-3 - Packaging & Tool Library compliance `[Done]`

- **US-PLAT-3.1** `[Done]` **As** a TOOLDEV, **I want** a single `Version` and shared build props,
  **so that** assembly version == nupkg version across all tools.
  - **AC:** Bumping `Version` in the root `Directory.Build.props` flows to every tool's assembly and nuspec.
- **US-PLAT-3.2** `[Done]` **As** a TOOLDEV, **I want** documented rules (tags start with
  `XrmToolBox`, depend on `XrmToolBox`, DLL under `Plugins`, own `iconUrl`), **so that** the Tool
  Library never rejects a package.
  - **AC:** Each tool's nuspec satisfies all rules; `nuget pack` produces a compliant package.

### FEAT-PLAT-4 - Consistent branding & required metadata `[Done]`

- **US-PLAT-4.1** `[Done]` **As** a TOOLDEV, **I want** shared `SmallImageBase64`/`BigImageBase64`
  defaults, **so that** MEF never silently drops a tool for missing required metadata.
  - **AC:** Every `*Plugin.cs` exports both image keys via `PluginIcons`.

### FEAT-PLAT-5 - Licensing & open-source hygiene `[Done]`

- **US-PLAT-5.1** `[Done]` **As** a consumer, **I want** an MIT `LICENSE` and per-package license
  metadata, **so that** I know my rights to use and redistribute the tools.
  - **AC:** Root `LICENSE` present; every nuspec declares `<license type="expression">MIT</license>`.

### Platform backlog (not yet scheduled)

- **US-PLAT-6.1** `[Planned]` Headless/console harness so UI-free analyzers can run in CI without XrmToolBox.
- **US-PLAT-6.2** `[Planned]` Automated build/pack pipeline (GitHub Actions) producing versioned nupkgs.
- **US-PLAT-6.3** `[Planned]` Shared unit-test project for core helpers (`RetrieveAll`, `BatchExecutor`).
