# Plugin Dependency Graph - Test Summary

## Automated run

- **Command:** `dotnet test testing/UnitTests/UnitTests.csproj -c Release`
- **Result:** 11 new Plugin Dependency Graph tests pass (executed in isolation while the shared
  `UnitTests.csproj` Compile entries are added — see below). `Failed: 0, Passed: 11`.
- **Coverage:** builder projection, deterministic ordering, subgraph isolation, filtering (by table /
  no-criteria), risk rules (high-impact, duplicate, unmanaged), emitters (Mermaid/GraphML/JSON/SVG) and the
  secure-config "value never present" guarantee.
- **Note:** the SDK-free sources under test must be registered in `testing/UnitTests/UnitTests.csproj`:

  ```xml
  <!-- Plugin Dependency Graph: SDK-free graph model + builder + risk rules + emitters
       (collector needs Dataverse and PluginPngExporter needs System.Drawing — both manual-tested). -->
  <Compile Include="..\..\src\Tools\XrmToolSuite.PluginDependencyGraph\Graph\PluginGraphModel.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.PluginDependencyGraph\Graph\PluginGraphBuilder.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.PluginDependencyGraph\Graph\PluginRiskRules.cs" />
  <Compile Include="..\..\src\Tools\XrmToolSuite.PluginDependencyGraph\Graph\PluginGraphEmitters.cs" />
  ```

## Build

- `dotnet build src/Tools/XrmToolSuite.PluginDependencyGraph/XrmToolSuite.PluginDependencyGraph.csproj -c Release`
  → **Build succeeded. 0 Warning(s), 0 Error(s).**
- The Excel chain (ClosedXML + OpenXml + facades) and the native-PDF `-gdi` chain land in
  `bin/Release/net48` (17 dependency DLLs); `System.Drawing` is referenced from the GAC and not shipped.

## Manual run

| Group | Cases | Executed | Pass | Fail | Pending |
|---|---|---|---|---|---|
| Automated | 11 | 11 | 11 | 0 | 0 |
| Manual | 6 | 0 | 0 | 0 | 6 |

The 6 manual cases (TC-PDG-LOAD-01 … TC-PDG-SETTINGS-06) require a Windows + XrmToolBox session against a
live Dataverse org and **cannot be run headlessly** in this environment; they remain Pending until executed
with screenshots captured under `screenshots/`.

## Verdict

SDK-free engine is complete and green (11/11 automated). The tool builds cleanly in Release with the full
export dependency chain. Manual Dataverse/UI verification (load, filters, focus, details, exports, settings)
is pending a live XrmToolBox session.

## Live UI smoke test (XrmToolBox)

- **Command:** `dotnet test testing/UiSmokeTests/UiSmokeTests.csproj` with `XTB_EXE` set, on 2026-07-05.
- **Result:** PASS — real XrmToolBox v1.2025.10.74 (FlaUI) confirms **Plugin Dependency Graph** loads and appears in the Tools list (24/24 suite tools verified in one run).
- **Evidence:** `screenshots/xrmtoolbox-tools-list.png` — the Tools tab filtered to **Plugin Dependency Graph** v1.2026.7.2 (Kanchan Kora).
