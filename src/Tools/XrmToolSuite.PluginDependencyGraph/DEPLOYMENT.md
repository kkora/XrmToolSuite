# Plugin Dependency Graph - Build & Install

Deployment guide for the **Plugin Dependency Graph** XrmToolBox plugin. For the suite-wide guide
(all tools, full troubleshooting matrix), see
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Prerequisites

- Windows + Visual Studio 2022 (or the .NET SDK with the **.NET Framework 4.8 Developer Pack**)
- XrmToolBox installed: https://www.xrmtoolbox.com
- A Dataverse connection (System Customizer or higher recommended)

## Build & deploy in one step (recommended)

On the same machine that runs XrmToolBox, build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.PluginDependencyGraph\XrmToolSuite.PluginDependencyGraph.csproj -c Release -p:DeployToXTB=true
```

Then **restart XrmToolBox** and open **Plugin Dependency Graph**. Files written by your own build are not
marked as internet-downloaded, so no unblocking is needed.

## Manual install (distributing to another machine)

1. Build in Release:

   ```powershell
   dotnet build src\Tools\XrmToolSuite.PluginDependencyGraph\XrmToolSuite.PluginDependencyGraph.csproj -c Release
   ```

2. Copy the tool DLL **and its export dependency chain** from

   ```
   src\Tools\XrmToolSuite.PluginDependencyGraph\bin\Release\net48\
   ```

   into `%AppData%\MscrmTools\XrmToolBox\Plugins` -- flat in the Plugins root, **not** a subfolder.
   Ship `XrmToolSuite.PluginDependencyGraph.dll` **plus** the 17 dependency DLLs listed in the tool's
   `.nuspec` `<files>` (the ClosedXML/Excel chain and the PdfSharp/MigraDoc `-gdi` chain). These power the
   Excel and native-PDF exports and MUST sit next to the tool DLL in the Plugins root, or XrmToolBox
   silently drops the tool from the Tools list. The shared core is compiled into the tool DLL.

   > **Do NOT copy** `Newtonsoft.Json.dll` or any `Microsoft.Xrm.*` / `Microsoft.Crm.*` DLLs.
   > XrmToolBox already ships them and duplicates cause load conflicts. `System.Drawing` (used for PNG)
   > is a GAC assembly and is never shipped.

3. **Unblock** the DLL. Windows blocks internet-downloaded files and XrmToolBox's loader (MEF)
   silently skips blocked assemblies:

   ```powershell
   Get-ChildItem "$env:AppData\MscrmTools\XrmToolBox\Plugins" -Filter *.dll | Unblock-File
   ```

4. **Restart XrmToolBox**, search for **Plugin Dependency Graph**, and connect to your environment.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Tool doesn't appear in the list | 1) DLL still blocked: run the `Unblock-File` command above. 2) Version mismatch: align `XrmToolBoxPackage` in `src\Tools\Directory.Build.props` with your installed XrmToolBox, rebuild, re-copy. |
| "Could not load file or assembly" for `Microsoft.Xrm.*` or `Newtonsoft.Json` | You copied a DLL XrmToolBox already ships: delete it. Keep the tool DLL + the 17 export-chain DLLs only. |
| Excel / PDF export fails, or the tool vanishes from the list after adding it | The ClosedXML / PdfSharp / MigraDoc `-gdi` chain is missing or in a subfolder. Copy all 17 dependency DLLs into the Plugins **root** next to the tool DLL. |

## Iterating during development

XrmToolBox locks the DLL while running, so: close XrmToolBox, rebuild with
`-p:DeployToXTB=true`, restart XrmToolBox. To debug with F5, set the project's debug launch
target to your local `XrmToolBox.exe`.
