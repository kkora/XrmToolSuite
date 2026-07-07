# 🤖 AI Solution Reviewer

An **XrmToolBox** plugin that performs an AI-assisted architecture review of a Dataverse solution. Four
UI-free collectors gather structured facts, the suite `ScoreCalculator` projects them onto a shared
report model with a **0–100 concern score** and band, and an AI layer turns the anonymized facts into a
professional review. AI is **opt-in** with a deterministic offline fallback; the tool is **read-only**.

## Features

| Area | What it does |
|---|---|
| **Fact collection** | Four solution-scoped, UI-free collectors — **plugins** (synchronous unfiltered steps, heavy step footprint, no-step assemblies), **JavaScript** (deprecated client APIs `Xrm.Page`/`crmForm`/`/2011/Organization.svc`/`getServerUrl`, heavy scripting), **automation** (classic workflows and sprawl), and **ALM/governance** (unmanaged solutions, default `new_` prefix, version). Each degrades query failures to informational findings. |
| **Concern score** | Findings score into a 0–100 concern score and band, with an Observations total plus a per-category metric row; grid and headline labels colour by band. |
| **AI review** | A principal-architect review covering six sections — **executive summary, architecture recommendations, modernization guidance, refactoring suggestions, a prioritized backlog, and a sprint plan** — landing in the report's AI summary field. |
| **Offline fallback** | With no key/consent, a deterministic templated generator produces the review and labels it offline. |

## Exports

- **Word** (.docx, via OpenXML)
- **PDF** (native, MigraDoc/PdfSharp-GDI)
- **HTML** dashboard
- **Markdown**
- **JSON**

Each export embeds the AI (or offline) review narrative.

## Help & Support

A right-aligned **Help** button on the toolbar opens a Help & Support dialog (Documentation, Report an
issue, and a support link, each opened in the browser). The tool implements `IHelpPlugin` and
`IGitHubPlugin` pointing at repository [`kkora/XrmToolSuite`](https://github.com/kkora/XrmToolSuite).

## Build & install

This tool is **not** single-DLL — it ships the Excel/PDF export dependency chain (ClosedXML +
PdfSharp/MigraDoc-GDI; the Word exporter reuses the OpenXML assembly from that chain) into the Plugins
root next to the tool DLL. The one-step build copies the whole chain automatically:

```powershell
dotnet build src\Tools\XrmToolSuite.AiSolutionReviewer\XrmToolSuite.AiSolutionReviewer.csproj -c Release -p:DeployToXTB=true
```

Restart XrmToolBox and open **AI Solution Reviewer**. For a manual copy to another machine, copy **every**
DLL from the tool's `bin\Release\net48\` folder — flat in the Plugins root, never a subfolder, or
XrmToolBox silently drops the tool. See [`./DEPLOYMENT.md`](./DEPLOYMENT.md) and the suite guide
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Usage

1. Connect to your Dataverse environment.
2. Open **AI Solution Reviewer** and pick a solution (solutions load off the UI thread).
3. Run the review — collectors run on a background worker with progress and cancellation.
4. Review the concern score, per-area metrics, and the review narrative (AI-authored or offline).
5. Export to Word / PDF / HTML / Markdown / JSON.

## Notes & limitations

- **Read-only** — no destructive operations; collectors and the report projection stay UI-free and
  degrade missing metadata to informational findings.
- **AI is opt-in.** It only calls a provider (Anthropic / OpenAI / Google, via raw HTTPS) when a
  session-only API key is supplied (the AI-settings dialog or `ANTHROPIC_API_KEY`) **and** the user
  approves a payload-preview consent dialog naming the provider/host/model. Only the anonymized summary
  payload (finding metadata + headline metrics — no record data, credentials or environment names) is
  sent; the key is held in memory for the session only and never persisted; any error falls back to the
  offline template.
