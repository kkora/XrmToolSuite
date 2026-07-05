# XrmToolSuite — Build & Install Guide for XrmToolBox

Step-by-step instructions for building **any tool in this suite** and installing it into
XrmToolBox **locally** (build → copy DLL → unblock → launch).

> To **publish a tool to the public XrmToolBox Tool Library** (package → push to nuget.org, manual
> or via CI), see [`Publishing_Guide_XrmToolBox.md`](Publishing_Guide_XrmToolBox.md) instead.

Throughout this guide, replace `<ToolName>` with the tool you are building. Every tool follows the
**same** build/copy/unblock/launch flow; the only difference is **how many DLLs you copy**, which
depends on whether the tool exports to Office/PDF. The suite currently ships **12 tools** in three
dependency categories:

| Category | What you copy | Tools (`<ToolName>`) |
|---|---|---|
| **Single-DLL** | just `XrmToolSuite.<ToolName>.dll` | `AttributeAuditor`, `SolutionKnowledgeGraph` |
| **Full export chain** (Excel + native PDF/Word) | the tool DLL **+ the 17-DLL ClosedXML/PdfSharp-MigraDoc-GDI chain** below | `DeploymentRiskAnalyzer`, `TechnicalDebtAnalyzer`, `SolutionComplexityScore`, `AiSolutionReviewer`, `FetchXmlPerformanceAnalyzer`, `EnvironmentInventory`, `PrivilegeGapAnalyzer`, `ViewPerformanceAnalyzer`, `TeamPermissionExplorer` |
| **PDF-only chain** | the tool DLL **+ the five `-gdi` PdfSharp/MigraDoc DLLs** (no ClosedXML) | `ErdGenerator` |

`TemplateTool` is the scaffold for new tools (not published); build it the same way (single-DLL).

> Steps that only apply to a tool that ships a dependency chain are marked **“export tools only.”**
> If you build with `-p:DeployToXTB=true` (below) the right DLLs are copied for you automatically,
> whichever category the tool is in.

---

## Prerequisites

- Windows PC
- Visual Studio 2022 (Community edition is fine) **or** the .NET SDK with the
  **.NET Framework 4.8 Developer Pack** installed
- XrmToolBox installed (latest version recommended — https://www.xrmtoolbox.com)
- A Dataverse connection with **System Customizer** or higher in the source
  (and target, if used) environment

---

## Step 1 — Build the tool

From the repository root, build one tool:

```powershell
dotnet build src\Tools\XrmToolSuite.<ToolName>\XrmToolSuite.<ToolName>.csproj -c Release
```

…or build the whole suite at once:

```powershell
dotnet build XrmToolSuite.sln -c Release
```

(or open `XrmToolSuite.sln` in Visual Studio, set the configuration to **Release**,
and build with `Ctrl+Shift+B`).

> **Tip:** If the build complains about XrmToolBox / `McTools.Xrm.Connection`
> assembly versions, make sure the `XrmToolBoxPackage` version in
> `src\Tools\Directory.Build.props` matches your installed XrmToolBox, then rebuild.

---

## Easiest path — build straight into XrmToolBox

If you are building on the same machine that runs XrmToolBox, skip the manual copy
entirely. The `DeployToXTB` flag copies the tool DLL into your XrmToolBox Plugins folder
automatically:

```powershell
dotnet build src\Tools\XrmToolSuite.<ToolName>\XrmToolSuite.<ToolName>.csproj -c Release -p:DeployToXTB=true
```

Then **restart XrmToolBox**. No manual copy, and no unblocking needed — files written
by your own build are not marked as internet-downloaded. Steps 2–4 below are only for
distributing a build to *another* machine.

> **Export tools:** the same flag additionally copies whatever dependency chain the tool ships
> (the full Excel + native-PDF chain, or ERD Generator's PDF-only chain) via each tool's dedicated
> deploy target, so nothing extra is needed there either.

---

## Step 2 — Locate the build output

Navigate to:

```
src\Tools\XrmToolSuite.<ToolName>\bin\Release\net48\
```

**For a single-DLL tool** (`AttributeAuditor`, `SolutionKnowledgeGraph`), **you copy exactly one
file:** `XrmToolSuite.<ToolName>.dll`. That is the whole tool — the shared core is compiled into it,
and XrmToolBox already ships the SDK and common libraries.

**Do NOT copy** `Newtonsoft.Json.dll` or any `Microsoft.Xrm.*` / `Microsoft.Crm.*`
DLLs — XrmToolBox already ships its own copies and duplicates can cause load conflicts.

### Export tools only — additional dependencies

The **full-export-chain** tools (`DeploymentRiskAnalyzer`, `TechnicalDebtAnalyzer`,
`SolutionComplexityScore`, `AiSolutionReviewer`, `FetchXmlPerformanceAnalyzer`,
`EnvironmentInventory`, `PrivilegeGapAnalyzer`, `ViewPerformanceAnalyzer`, `TeamPermissionExplorer`)
ship extra libraries for **Excel** and **native PDF/Word** export. In addition to
`XrmToolSuite.<ToolName>.dll`, copy **all** of these DLLs from the same `bin\Release\net48\` folder:

| DLL | Purpose |
|---|---|
| `XrmToolSuite.<ToolName>.dll` | The tool itself |
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

> **All in the Plugins root — do not put the dependencies in a subfolder.** XrmToolBox's
> plugin analysis must be able to resolve the tool's `ClosedXML` and `PdfSharp`/`MigraDoc` references
> when it scans the folder; if the dependencies aren't found next to the tool DLL, XrmToolBox
> **silently drops the tool** (it won't appear in the Tools list, with no error). The PDF assemblies
> carry the **`-gdi`** suffix (the GDI build — pure-managed on net48, no SkiaSharp natives). Current
> XrmToolBox ships the `System.*` facades with binding redirects, so `SixLabors.Fonts`' version
> mismatch resolves without a "Tools not loaded" warning.

> **ERD Generator (PDF-only chain):** it exports PDF but not Excel, so it ships **only** the five
> `-gdi` PdfSharp/MigraDoc DLLs (the last five rows above) alongside `XrmToolSuite.ErdGenerator.dll` —
> **not** ClosedXML or the `System.*` facades. (Its PNG export uses the net48 `System.Drawing` GAC
> assembly, which is not shipped.)

---

## Step 3 — Copy into the XrmToolBox Plugins folder

1. **Close XrmToolBox** completely.
2. Open File Explorer and paste this into the address bar:

   ```
   %AppData%\MscrmTools\XrmToolBox\Plugins
   ```

3. Copy the DLL(s) from Step 2 into that folder (flat — no subfolder):

   ```
   Plugins\
     XrmToolSuite.<ToolName>.dll
   ```

   *Export tools* — the folder also contains that tool's dependency chain, all flat (all 17 DLLs for
   a full-export-chain tool, or just the five `-gdi` DLLs for ERD Generator):

   ```
   Plugins\
     XrmToolSuite.<ToolName>.dll
     ClosedXML.dll            (full-export-chain tools only)
     SixLabors.Fonts.dll      (full-export-chain tools only)
     PdfSharp-gdi.dll
     ... (the rest of the Step 2 list)
   ```

---

## Step 4 — Unblock the DLLs (important!)

Windows marks files that came from the internet (e.g. copied out of a downloaded zip)
as blocked, and XrmToolBox's plugin loader (MEF) will **silently skip** blocked
assemblies.

For each DLL you copied:

1. Right-click → **Properties**.
2. If an **Unblock** checkbox appears at the bottom of the General tab, tick it.
3. Click **OK**.

Alternatively, unblock everything at once in PowerShell:

```powershell
Get-ChildItem "$env:AppData\MscrmTools\XrmToolBox\Plugins" -Filter *.dll | Unblock-File
```

---

## Step 5 — Launch the tool

1. Start **XrmToolBox**.
2. In the tool list, search for the tool by name (e.g. **"Attribute Auditor"** or
   **"Deployment Risk Analyzer"**) and open it.
3. Connect to your Dataverse environment when prompted.

Each tool's own in-app usage is documented in its project `README.md` under
`src\Tools\XrmToolSuite.<ToolName>\`. The DeploymentRiskAnalyzer walkthrough below is included as
a worked example.

---

## Example — running a Deployment Risk Analyzer scan

*(DeploymentRiskAnalyzer-specific; other tools have their own workflow.)*

1. Open **Deployment Risk Analyzer** and connect to your **source (dev)** environment.
2. Click **Load solutions** and pick the solution you plan to deploy.
3. *(Recommended)* Click **Connect target env…** and select your test/prod
   connection — this unlocks:
   - Schema conflict detection (type mismatches, choice value conflicts, etc.)
   - Target-side environment variable / connection reference verification
   - Solution version downgrade detection
   - Role assignment coverage checks
4. Tick the analyzers you want in the left panel.
5. Click **▶ Analyze**.
6. Review the findings grid (click any row for the full recommendation).
7. Click **Export** to save the report:
   - **PDF** — native executive PDF (radial score gauge, risk categories, recommendations, next steps) — no browser needed
   - **HTML** — self-contained, theme-aware dashboard (also prints to PDF from any browser)
   - **Excel** — Summary / Findings / Fix Checklist worksheets
   - **JSON** — CI/CD-friendly payload with pass/fail and suggested exit code
   - **Markdown** — admin fix checklist with rollback guidance

   The **Help** button (right of the toolbar) opens documentation, a "report an issue" link, and a support link.

### CI/CD gating (optional)

Use the JSON export to block risky deployments in a pipeline:

```powershell
$r = Get-Content .\DeploymentRiskAnalyzer_MySolution.json | ConvertFrom-Json
if (-not $r.ci.pass) {
  Write-Error "Deployment Risk Analyzer risk = $($r.risk) (score $($r.score)). Blocking deployment."
  exit $r.ci.suggestedExitCode
}
```

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| Tool doesn't appear in XrmToolBox | 1) DLL still blocked — redo Step 4. 2) Version mismatch — align `XrmToolBoxPackage` in `Directory.Build.props` with your installed XrmToolBox, rebuild, re-copy. |
| An **export tool** doesn't appear | As above, plus: a dependency is missing — copy the **entire** Step 2 list into the Plugins root (all in one folder, no subfolder). XrmToolBox silently drops a tool whose `ClosedXML`/`PdfSharp`/`MigraDoc` reference it can't resolve during its scan. (ERD Generator needs only its five `-gdi` DLLs.) |
| "Tools not loaded" listing `System.Numerics.Vectors` / `SixLabors.Fonts` (a full-export-chain tool) | Your XrmToolBox is older than ~1.2025.10 and lacks the facade binding redirects. Update XrmToolBox to the current version. |
| Crash / no output when exporting to **Excel** | A ClosedXML dependency from Step 2 is missing from the Plugins root — copy the **entire** list (including `System.IO.Packaging.dll` and `Irony.dll`). |
| Crash / no output when exporting to **PDF** | A PDF dependency from Step 2 is missing — copy all five `-gdi` DLLs (`PdfSharp-gdi`, `PdfSharp.Charting-gdi`, `MigraDoc.DocumentObjectModel-gdi`, `MigraDoc.Rendering-gdi`, `MigraDoc.RtfRendering-gdi`). |
| "Could not load file or assembly" for `Microsoft.Xrm.*` or `Newtonsoft.Json` | You copied a DLL XrmToolBox already ships — delete it (keep only your tool DLL, plus the Step 2 dependency list if it's an export tool). |
| Analyzers/queries return "skipped" findings | Usually insufficient privileges — verify System Customizer or higher in both environments. |

---

## Iterating during development

Build with `-p:DeployToXTB=true` (see *Easiest path* above). For a single-DLL tool that copies the
tool DLL into the Plugins root; **for an export tool** it also copies that tool's dependency chain
(the full Excel + native-PDF chain, or ERD Generator's PDF-only chain). After the first deploy your
loop is just: **rebuild with `-p:DeployToXTB=true` → restart XrmToolBox**.

(XrmToolBox locks the tool DLL while running, so close XrmToolBox before rebuilding with the
deploy flag.)

To debug with F5, set the project's debug launch target to your local `XrmToolBox.exe`.
