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
- **AI is opt-in.** It only calls a provider — **Anthropic / OpenAI / Google** (cloud, via raw HTTPS) or
  **Ollama** (local) — when the provider is configured **and** the user approves a payload-preview consent
  dialog naming the provider/host/model. Only the anonymized summary payload (finding metadata + headline
  metrics — no record data, credentials or environment names) is sent; for cloud providers the session-only
  API key is held in memory and never persisted; any error falls back to the offline template.

### Local models (Ollama) — no API key, nothing leaves the machine

Run the review against a local model instead of a cloud API. This is the full setup (Windows).

**1. Download & install Ollama**
```powershell
winget install Ollama.Ollama          # or download the installer from https://ollama.com/download
```
After install, Ollama runs a background server automatically at `http://localhost:11434`
(opening that URL shows "Ollama is running"). If it isn't running, start it with:
```powershell
ollama serve
```

**2. Pull the model(s) you want** (downloads once, then cached):
```powershell
ollama pull qwen2.5:7b          # general reasoning — good default for solution review
ollama pull qwen2.5-coder:7b    # code-heavy solutions
ollama pull gemma3:4b           # faster / lighter
ollama pull llama3.2:3b         # smallest / fastest
```

**3. (Optional) Preload / test a model** — the tool loads it on demand, but you can warm it up:
```powershell
ollama run qwen2.5:7b "hello"   # loads the model and prints a reply (proves it works)
```

**4. Check what you have and what's loaded:**
```powershell
ollama list                     # models installed on disk
ollama ps                       # models currently loaded in memory (with expiry)
ollama stop qwen2.5:7b          # unload a model to free RAM/VRAM
```

**5. In the tool:** **AI options ▸ AI settings…** → **Provider = `Ollama (local)`**, put the model id in
**Model** (e.g. `qwen2.5:7b`, or click a **Suggested** button), leave **API key** blank (it's disabled for
local). Then **Generate AI review** and approve the consent preview.

Notes: the first request loads the model and can take a while (the tool waits up to 5 minutes); later
requests are fast. If Ollama isn't running or the model isn't pulled, the tool reports it and falls back
to the offline summary. Bigger models (7B) give better reviews but need more RAM/VRAM; 3–4B are faster on
modest machines. `nomic-embed-text` is an **embedding** model (for RAG) — it won't work here; use a chat
model like `qwen2.5`, `gemma3`, or `llama3.2`.
