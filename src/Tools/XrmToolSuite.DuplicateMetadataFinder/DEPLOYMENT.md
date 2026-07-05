# Duplicate Metadata Finder - Build & Install

Deployment guide for the **Duplicate Metadata Finder** XrmToolBox plugin. For the suite-wide guide
(all tools, full troubleshooting matrix), see
[`Deployment_Guide_XrmToolBox.md`](../../../Deployment_Guide_XrmToolBox.md).

## Prerequisites

- Windows + Visual Studio 2022 (or the .NET SDK with the **.NET Framework 4.8 Developer Pack**)
- XrmToolBox installed: https://www.xrmtoolbox.com
- A Dataverse connection (System Customizer or higher recommended)

## Build & deploy in one step (recommended)

On the same machine that runs XrmToolBox, build straight into the Plugins folder:

```powershell
dotnet build src\Tools\XrmToolSuite.DuplicateMetadataFinder\XrmToolSuite.DuplicateMetadataFinder.csproj -c Release -p:DeployToXTB=true
```

Then **restart XrmToolBox** and open **Duplicate Metadata Finder**. Files written by your own build are not
marked as internet-downloaded, so no unblocking is needed.

## Manual install (distributing to another machine)

1. Build in Release:

   ```powershell
   dotnet build src\Tools\XrmToolSuite.DuplicateMetadataFinder\XrmToolSuite.DuplicateMetadataFinder.csproj -c Release
   ```

2. Copy the **single** output DLL

   ```
   src\Tools\XrmToolSuite.DuplicateMetadataFinder\bin\Release\net48\XrmToolSuite.DuplicateMetadataFinder.dll
   ```

   into `%AppData%\MscrmTools\XrmToolBox\Plugins` -- flat in the Plugins root, **not** a subfolder.
   The shared core is compiled into this DLL, so it is the only file you ship.

   > **Do NOT copy** `Newtonsoft.Json.dll` or any `Microsoft.Xrm.*` / `Microsoft.Crm.*` DLLs.
   > XrmToolBox already ships them and duplicates cause load conflicts.

3. **Unblock** the DLL. Windows blocks internet-downloaded files and XrmToolBox's loader (MEF)
   silently skips blocked assemblies:

   ```powershell
   Get-ChildItem "$env:AppData\MscrmTools\XrmToolBox\Plugins" -Filter *.dll | Unblock-File
   ```

4. **Restart XrmToolBox**, search for **Duplicate Metadata Finder**, and connect to your environment.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Tool doesn't appear in the list | 1) DLL still blocked: run the `Unblock-File` command above. 2) Version mismatch: align `XrmToolBoxPackage` in `src\Tools\Directory.Build.props` with your installed XrmToolBox, rebuild, re-copy. |
| "Could not load file or assembly" for `Microsoft.Xrm.*` or `Newtonsoft.Json` | You copied a DLL XrmToolBox already ships: delete it and keep only `XrmToolSuite.DuplicateMetadataFinder.dll`. |

## Iterating during development

XrmToolBox locks the DLL while running, so: close XrmToolBox, rebuild with
`-p:DeployToXTB=true`, restart XrmToolBox. To debug with F5, set the project's debug launch
target to your local `XrmToolBox.exe`.
