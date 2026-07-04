# Deployment Risk Analyzer — Build & Install

Deployment guide for the **Deployment Risk Analyzer** XrmToolBox plugin. For feature/usage
docs see [`README.md`](README.md); for the suite-wide guide see
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

> **This tool is the one exception in the suite:** unlike the other single-DLL tools, Deployment
> Risk Analyzer ships two **export dependency chains** (Excel + native PDF) that must sit next to the
> tool DLL. If any of those DLLs are missing (or placed in a subfolder), XrmToolBox **silently drops
> the tool** — it won't appear in the Tools list and there is no error. Follow Step 2 exactly.

## Prerequisites

- Windows + Visual Studio 2022 (or the .NET SDK with the **.NET Framework 4.8 Developer Pack**)
- XrmToolBox installed (**~1.2025.10 or newer** — earlier builds lack the facade binding redirects
  Deployment Risk Analyzer relies on) — https://www.xrmtoolbox.com
- A Dataverse connection with **System Customizer** or higher in the source (and target, if used) environment

## Build & deploy in one step (recommended)

On the same machine that runs XrmToolBox, build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.DeploymentRiskAnalyzer\XrmToolSuite.DeploymentRiskAnalyzer.csproj -c Release -p:DeployToXTB=true
```

For Deployment Risk Analyzer, `DeployToXTB=true` copies the tool DLL **and** both of its export
dependency chains — Excel and native PDF (the csproj carries a dedicated deploy target). Then
**restart XrmToolBox** and open
**Deployment Risk Analyzer**. No unblocking needed — files written by your own build are not marked
as internet-downloaded.

## Manual install (distributing to another machine)

### Step 1 — Build in Release

```powershell
dotnet build src\Tools\XrmToolSuite.DeploymentRiskAnalyzer\XrmToolSuite.DeploymentRiskAnalyzer.csproj -c Release
```

### Step 2 — Copy the tool DLL **and its full dependency chain**

From `src\Tools\XrmToolSuite.DeploymentRiskAnalyzer\bin\Release\net48\`, copy **all** of these into
`%AppData%\MscrmTools\XrmToolBox\Plugins` — flat in the Plugins root, **not** a subfolder:

| DLL | Purpose |
|---|---|
| `XrmToolSuite.DeploymentRiskAnalyzer.dll` | The tool itself |
| `ClosedXML.dll` | Excel export |
| `DocumentFormat.OpenXml.dll` | ClosedXML dependency |
| `ExcelNumberFormat.dll` | ClosedXML dependency |
| `XLParser.dll` | ClosedXML dependency |
| `Irony.dll` | XLParser dependency (formula parser) |
| `SixLabors.Fonts.dll` | ClosedXML dependency (font metrics) |
| `System.IO.Packaging.dll` | writing the .xlsx package |
| `System.Numerics.Vectors.dll` | SixLabors.Fonts facade |
| `System.Runtime.CompilerServices.Unsafe.dll` | SixLabors.Fonts facade |
| `System.Buffers.dll` | SixLabors.Fonts facade |
| `System.Memory.dll` | SixLabors.Fonts facade |
| `System.Threading.Tasks.Extensions.dll` | SixLabors.Fonts facade |
| `PdfSharp-gdi.dll` | Native PDF export (GDI build) |
| `PdfSharp.Charting-gdi.dll` | PdfSharp dependency |
| `MigraDoc.DocumentObjectModel-gdi.dll` | Native PDF document model |
| `MigraDoc.Rendering-gdi.dll` | Native PDF rendering |
| `MigraDoc.RtfRendering-gdi.dll` | MigraDoc dependency |

> **Why flat, and why all of them:** XrmToolBox's plugin analysis must resolve the tool's direct
> `ClosedXML` **and** `PdfSharp`/`MigraDoc` references when it scans the folder (before the tool's own
> `AssemblyResolve` fallback is registered). If either chain isn't found next to the tool DLL, the tool
> is silently dropped. The PDF assemblies use the **`-gdi`** suffix (the GDI build — pure-managed on
> net48, no SkiaSharp natives). Current XrmToolBox ships the `System.*` facades with binding redirects,
> so `SixLabors.Fonts`' version mismatch resolves at runtime.

> **Do NOT copy** `Newtonsoft.Json.dll` or any `Microsoft.Xrm.*` / `Microsoft.Crm.*` DLLs —
> XrmToolBox already ships them and duplicates cause load conflicts.

### Step 3 — Unblock the DLLs

Windows blocks internet-downloaded files and XrmToolBox's loader (MEF) silently skips blocked
assemblies:

```powershell
Get-ChildItem "$env:AppData\MscrmTools\XrmToolBox\Plugins" -Filter *.dll | Unblock-File
```

### Step 4 — Restart XrmToolBox

Open **Deployment Risk Analyzer**. See [`README.md`](README.md) for the analysis workflow and
CI/CD JSON gating.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Tool doesn't appear in the list | 1) DLLs still blocked — redo Step 3. 2) A dependency is missing — copy the **entire** Step 2 list into the Plugins root (flat, no subfolder). XrmToolBox silently drops a tool whose `ClosedXML` reference it can't resolve during its scan. 3) Version mismatch — align `XrmToolBoxPackage` in `src\Tools\Directory.Build.props` with your installed XrmToolBox, rebuild, re-copy. |
| "Tools not loaded" listing `System.Numerics.Vectors` / `SixLabors.Fonts` | Your XrmToolBox is older than ~1.2025.10 and lacks the facade binding redirects — update XrmToolBox. |
| Crash / no output when exporting to Excel | A ClosedXML dependency from Step 2 is missing — copy the **entire** list (including `System.IO.Packaging.dll` and `Irony.dll`). |
| Crash / no output when exporting to PDF | A PDF dependency from Step 2 is missing — copy all five `-gdi` DLLs (`PdfSharp-gdi`, `PdfSharp.Charting-gdi`, `MigraDoc.DocumentObjectModel-gdi`, `MigraDoc.Rendering-gdi`, `MigraDoc.RtfRendering-gdi`). |
| "Could not load file or assembly" for `Microsoft.Xrm.*` or `Newtonsoft.Json` | You copied a DLL XrmToolBox already ships — delete it (keep only the Step 2 list). |
| Analyzers return "skipped" findings | Usually insufficient privileges — verify System Customizer or higher in both environments. |

## Iterating during development

XrmToolBox locks the tool DLL while running, so: close XrmToolBox → rebuild with
`-p:DeployToXTB=true` (re-copies the DLL and the dependency chain) → restart XrmToolBox. To debug
with F5, set the project's debug launch target to your local `XrmToolBox.exe`.
