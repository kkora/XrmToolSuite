# testing/UiSmokeTests — Tier-3 UI smoke tests (FlaUI)

Interactive, opt-in end-to-end smoke tests that drive the **real XrmToolBox host** with UI Automation
([FlaUI](https://github.com/FlaUI/FlaUI)) and assert the suite's plugins loaded.

## What it proves (and why it matters)

`AllSuiteTools_AppearInXrmToolBoxToolsList` launches XrmToolBox and checks that every suite tool
(Deployment Risk Analyzer, Technical Debt Analyzer, Solution Complexity Score, AI Solution Reviewer,
Solution Knowledge Graph, Attribute Auditor) appears in the Tools list.

That is exactly the failure mode the headless suites **cannot** see and that CLAUDE.md warns about: a
plugin missing a required `ExportMetadata` key (e.g. `SmallImageBase64`/`BigImageBase64`), or with an
unresolved dependency at scan time, is **silently dropped** from the Tools list — no error, no crash, the
assembly just doesn't appear. This test turns that into a red build.

**No Dataverse connection is required** — XrmToolBox loads and lists plugins at startup, before you connect.

## Why it is separate from `test` / `test-windows`

- It needs a **real, logged-in, unlocked Windows desktop session** — UI Automation cannot drive a headless
  agent or a service session. The GitHub-hosted `windows-latest` runners do not provide one out of the box.
- It needs **XrmToolBox installed** and the **suite DLLs deployed** to its Plugins folder.

So it is **not** wired into CI. Run it deliberately on a dev box, or on a self-hosted Windows runner set up
for interactive UI automation (below). It is also deliberately **not** part of `XrmToolSuite.sln`.

## Running it locally

1. **Build + deploy the tools into XrmToolBox** (copies each tool DLL — and, for the tools that ship them,
   their dependency chain — into `%AppData%\MscrmTools\XrmToolBox\Plugins`):

   ```powershell
   dotnet build XrmToolSuite.sln -c Release -p:DeployToXTB=true
   Get-ChildItem "$env:AppData\MscrmTools\XrmToolBox\Plugins\XrmToolSuite.*.dll" | Unblock-File
   ```

2. **Point the test at your XrmToolBox executable** (or install it to a default location the test probes):

   ```powershell
   $env:XTB_EXE = "C:\path\to\XrmToolBox.exe"
   ```

3. **Run it** (from a normal, interactive PowerShell — not over a locked RDP session):

   ```powershell
   dotnet test testing/UiSmokeTests/UiSmokeTests.csproj
   ```

   Leave the desktop alone while it runs; UI Automation drives the real mouse/keyboard focus.

## Setting it up on a self-hosted CI runner

To gate it in CI you need a Windows machine that keeps an interactive session alive:

- A **self-hosted GitHub Actions runner** installed as a **per-user process (not a service)**, or a service
  configured for an autologon desktop.
- **Autologon** enabled and the screen kept **unlocked** (e.g. disable the lock screen / screen saver; some
  teams use a small "keep-awake" utility). UI Automation fails against a locked session.
- XrmToolBox installed and the tools deployed on that machine (step 1 above), and `XTB_EXE` set as a runner
  environment variable.
- A separate workflow job (`runs-on: [self-hosted, windows]`) that runs `dotnet test testing/UiSmokeTests`.

Keep it **advisory** at first (not a required check) — UI tests are inherently flakier than the headless
tiers, and a single dedicated smoke test ("do the tools load") is the right scope, not full UI coverage.

## Status & caveats

- **The harness compiles and is ready to run, but has not been executed in this environment** — there is no
  interactive desktop here. The first real run must confirm one thing: whether XrmToolBox's tool tiles expose
  their display name to UI Automation. If a tool is present in the app but the test reports it missing, the
  tile is likely owner-drawn without an accessible `Name`; adjust the matching in
  `XrmToolBoxSmokeTest.VisibleNames` (e.g. drive the tool-search textbox and assert on the filtered result, or
  match on AutomationId) — the launch/assert scaffold stays the same.
- UI automation is timing-sensitive. The test polls for up to ~45s after the window appears to allow the
  plugin scan to finish before asserting.
