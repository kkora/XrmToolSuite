# Publishing Guide — XrmToolBox Tool Library (online)

How to **package** each XrmToolSuite tool as a NuGet package and **publish** it to the
**XrmToolBox Tool Library** so anyone can install it from inside XrmToolBox (Configuration →
Tool Library). Covers a **manual** release and an **automated CI** release.

> For installing a tool **locally** (build → copy DLL → unblock → launch), see
> [`Deployment_Guide_XrmToolBox.md`](Deployment_Guide_XrmToolBox.md). This guide is only about
> the **public Tool Library**.

---

## How the Tool Library works

The XrmToolBox Tool Library is **an index over [nuget.org](https://www.nuget.org)**. XrmToolBox
periodically scans nuget.org for packages that:

- **depend on the `XrmToolBox` package**, and
- have **`tags` that start with `XrmToolBox`**.

So "deploying online" means **pushing a `.nupkg` to nuget.org**. There is no separate XrmToolBox
upload — once your package is on nuget.org and satisfies the rules below, it appears in every
user's Tool Library (typically within a few hours to a day, after the next index refresh).

```
 bump version ─► build Release ─► nuget pack (.nuspec) ─► nuget push ─► nuget.org
                                                                           │
                                                        XrmToolBox indexes ▼
                                                              Tool Library (all users)
```

---

## One-time prerequisites

1. **A nuget.org account.** How it authenticates to publish depends on the path:
   - **CI (recommended): Trusted Publishing (OIDC) — no API key.** This repo uses it. On nuget.org →
     your username → **Trusted Publishing** → **Create**, with: Package owner = your account
     (`kora.kanchan`), Publisher = **GitHub Actions**, Repository Owner = `kkora`, Repository =
     `XrmToolSuite`, Workflow = `publish.yml`. The [`publish.yml`](.github/workflows/publish.yml)
     workflow exchanges the run's short-lived OIDC token for a temporary push key — nothing to
     store, nothing to rotate. See [CI](#automated-release-github-actions).
   - **Manual/local push: an API key** (Account → **API Keys** → Create). Scope **Push** (new
     packages and versions), glob pattern `XrmToolSuite.*` (covers every tool, including IDs that
     don't exist yet), expiry up to 365 days. The key is shown **once** — copy it. Treat it like a
     password; never commit it. Use it only from your machine via `$env:NUGET_API_KEY` (below).
2. **A hosted icon.** Each nuspec's `<iconUrl>` must resolve publicly. This repo uses
   `https://raw.githubusercontent.com/kkora/XrmToolSuite/main/icon.png` (the committed
   [`icon.png`](icon.png)). If you fork/rename, update every nuspec `<iconUrl>`.
3. **Windows + .NET Framework 4.8 targeting pack** (tools are net48) and **`nuget.exe`**
   (or the `dotnet` SDK) to build and pack.

---

## Packaging rules the Tool Library enforces

A package that breaks any of these is silently ignored or rejected by the Tool Library. All of
this is already correct in the committed `.nuspec` files — verify it stays that way when you edit:

| Rule | Where |
|---|---|
| nupkg `version` **==** the tool's assembly version | both derive from root [`Directory.Build.props`](Directory.Build.props) `Version` |
| `tags` **start with** `XrmToolBox` (plus extra words) | `<tags>XrmToolBox …</tags>` |
| dependency is on **`XrmToolBox`** — never `XrmToolBoxPackage` | `<dependencies><dependency id="XrmToolBox" …/>` |
| the tool DLL sits under **`lib\net48\Plugins`** | `<files><file … target="lib\net48\Plugins" /></files>` |
| package contains **only this suite's files** (no `Microsoft.Xrm.*`, no `Newtonsoft.Json`) | the `<files>` list |
| `iconUrl` set to your **own** hosted icon | `<iconUrl>` |

**Export tools ship their dependency chain.** `DeploymentRiskAnalyzer`,
`FetchXmlPerformanceAnalyzer`, `EnvironmentInventory`, and `PrivilegeGapAnalyzer` export
Excel/PDF/Word and therefore bundle the **ClosedXML + PdfSharp/MigraDoc-GDI** chains — **18 DLLs
total** (the tool DLL + 17 dependencies), all under `lib\net48\Plugins` (the **Plugins root**, not
a subfolder). Their nuspec `<files>` already lists them; you can [verify the count](#verify-the-package-contents)
after packing. See `CLAUDE.md` → *Excel/PDF/Word export pattern* for the full rationale.

> **Do not publish `TemplateTool`.** It is the scaffold, not a shipping tool. Exclude
> `XrmToolSuite.TemplateTool.nuspec` from every pack/push step.

---

## Versioning

There is **one version for the whole suite**, in root [`Directory.Build.props`](Directory.Build.props):

```xml
<Version>1.2026.7.1</Version>
```

- **nuget.org versions are immutable** — you can never re-push the same version. Every release
  must bump this number. The scheme here is `1.<year>.<month>.<patch>`.
- `New-Tool.ps1` stamps this version into each tool; the assembly version and the nuspec
  `<version>` both derive from it, so bumping this one value keeps them equal (a Tool Library
  requirement).
- Bump it, then also update each nuspec's `<releaseNotes>` if you want per-release notes.

---

## Manual release

Run from the repository root on Windows.

### 1. Bump the version

Edit [`Directory.Build.props`](Directory.Build.props) → `<Version>` (e.g. `1.2026.8.1`) and update
each publishing tool's nuspec `<version>` and `<releaseNotes>` to match. (Keeping the nuspec
`<version>` equal to `Directory.Build.props` is mandatory — the Tool Library rejects a mismatch.)

### 2. Build Release

```powershell
dotnet build XrmToolSuite.sln -c Release
```

This must succeed with **0 warnings, 0 errors**. For the export tools, `CopyLocalLockFileAssemblies`
puts the 17 dependency DLLs next to each tool DLL in `bin\Release\net48\` — the nuspec ships them
from there.

### 3. (Recommended) Run the tests

```powershell
dotnet test testing/UnitTests/UnitTests.csproj -c Release --nologo
```

### 4. Pack each tool (except the template)

`nuget pack <path-to-nuspec>` resolves the nuspec's relative `<file src="bin\Release\net48\…">`
paths against the nuspec's own folder, so you can run it from the repo root:

```powershell
$out = "artifacts"
New-Item -ItemType Directory -Force $out | Out-Null
Get-ChildItem src/Tools -Recurse -Filter *.nuspec |
  Where-Object { $_.Name -notlike '*TemplateTool*' } |
  ForEach-Object { nuget pack $_.FullName -OutputDirectory $out }
```

You get one `.nupkg` per tool in `artifacts\`, e.g.
`artifacts\XrmToolSuite.FetchXmlPerformanceAnalyzer.1.2026.8.1.nupkg`.

### 5. Verify the package contents

Sanity-check that the DLLs landed under `lib\net48\Plugins` (and, for export tools, that all 18 are
present):

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
$pkg = Get-ChildItem artifacts\XrmToolSuite.FetchXmlPerformanceAnalyzer.*.nupkg | Select-Object -First 1
[IO.Compression.ZipFile]::OpenRead($pkg.FullName).Entries |
  Where-Object { $_.FullName -like 'lib/net48/Plugins/*.dll' } |
  ForEach-Object { $_.Name }
```

Expected: **1 DLL** for single-DLL tools (Attribute Auditor, Solution Complexity Score, etc.);
**18 DLLs** for the export tools (the tool + `ClosedXML`, `DocumentFormat.OpenXml`,
`ExcelNumberFormat`, `XLParser`, `Irony`, `SixLabors.Fonts`, `System.IO.Packaging`, four `System.*`
facades, `System.Threading.Tasks.Extensions`, and the five `-gdi` PdfSharp/MigraDoc assemblies).

### 6. Push to nuget.org

```powershell
# Use an API key scoped to XrmToolSuite.* (never hard-code it — read from an env var)
$key = $env:NUGET_API_KEY
dotnet nuget push "artifacts\*.nupkg" `
  --api-key $key `
  --source https://api.nuget.org/v3/index.json `
  --skip-duplicate
```

`--skip-duplicate` makes a re-run safe: already-published versions are skipped instead of failing
the whole batch.

### 7. Verify it went live

1. **nuget.org** — the package page appears within minutes (it may show "unlisted/validating"
   for a short while during validation).
2. **XrmToolBox Tool Library** — open XrmToolBox → **Configuration → Tool Library**, click
   **Refresh**, and search for the tool. Indexing can take a few hours to a day after the package
   is listed on nuget.org.
3. **Install + load** — install from the Tool Library and confirm the tool appears in the **Tools**
   list (this is the same thing the `testing/UiSmokeTests` harness asserts locally).

---

## Automated release (GitHub Actions)

A workflow at [`.github/workflows/publish.yml`](.github/workflows/publish.yml) builds, tests, packs,
and pushes all publishable tools. It is **safe by default**: it only pushes to nuget.org when you
push a version tag or explicitly ask it to.

Authentication is **Trusted Publishing (OIDC)** — no API-key secret. The workflow requests an OIDC
token (`permissions: id-token: write`) and the `NuGet/login` action exchanges it for a short-lived
push key that nuget.org honours because the Trusted Publishing policy matches this repo + workflow.

### One-time setup

1. Create the **Trusted Publishing** policy on nuget.org (username → **Trusted Publishing** →
   **Create**): Publisher **GitHub Actions**, Repository Owner `kkora`, Repository `XrmToolSuite`,
   Workflow `publish.yml`. *(Already done — shows **Active** under Manage.)*
2. Confirm the `user:` in the workflow's **Authenticate to NuGet** step matches the policy's
   **Package owner** (`kora.kanchan`). Nothing else — no repository secret is required.

### Release by tag (the normal path)

1. Bump `Directory.Build.props` `<Version>` and each nuspec `<version>` (as in the manual flow),
   commit, and merge to `main` via PR.
2. Tag the release commit with **`v` + the exact version** and push the tag:

   ```powershell
   git tag v1.2026.8.1
   git push origin v1.2026.8.1
   ```

   The workflow verifies the tag matches `Directory.Build.props` (so a mistyped tag fails fast),
   builds Release, runs the SDK-free tests, packs every tool except the template, and pushes to
   nuget.org.

### Dry run / manual trigger

Run it from **Actions → Publish to NuGet → Run workflow**. Leave **publish** unchecked (the default)
to build + pack and upload the `.nupkg` files as a build artifact **without** pushing — a safe way to
inspect exactly what would ship. Check **publish** to push (via Trusted Publishing).

### What the workflow does

- Runs on **windows-latest** (net48 build + `nuget pack` need the Windows targeting pack).
- Guards that a `v*` tag equals the repo version.
- `dotnet build XrmToolSuite.sln -c Release` and `dotnet test testing/UnitTests` — a failed build or
  test blocks the release.
- Packs `src/Tools/*/*.nuspec` **except** `TemplateTool` into `artifacts/`.
- Uploads `artifacts/*.nupkg` (always) and, when publishing, runs
  `dotnet nuget push artifacts/*.nupkg --skip-duplicate`.

> The live **UI smoke test** (does the tool load in a real XrmToolBox?) is **not** part of this
> workflow — it needs an interactive desktop and runs on the self-hosted
> [`ui-smoke.yml`](.github/workflows/ui-smoke.yml). Run it before tagging a release.

---

## Updating a published tool

Same flow, new version. nuget.org versions are immutable, so:

1. Bump `Directory.Build.props` `<Version>` (and the nuspec `<version>`/`<releaseNotes>`).
2. Manual: repeat [Manual release](#manual-release). CI: push a new `v<version>` tag.

XrmToolBox shows users an **update** for tools they already installed once the new version is
indexed.

---

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `nuget push` fails: **409 / version already exists** | nuget.org versions are immutable — bump `Directory.Build.props` `<Version>` and re-pack. Use `--skip-duplicate` to keep a batch push from aborting. |
| `nuget push` fails: **403 / API key** | Key expired, wrong scope, or its glob doesn't cover `XrmToolSuite.*`. Regenerate a **Push**-scoped key. |
| Package publishes but **never appears in the Tool Library** | A packaging rule is broken: dependency must be `XrmToolBox` (not `XrmToolBoxPackage`); `tags` must start with `XrmToolBox`; DLL must be under `lib\net48\Plugins`. Fix the nuspec, bump the version, re-push. |
| Tool appears but **won't load** (missing from the Tools list after install) | For an export tool, a dependency DLL didn't ship — [verify the package contents](#verify-the-package-contents) show **18** DLLs under `lib\net48\Plugins`. |
| **Icon** is blank in the Tool Library | `<iconUrl>` doesn't resolve publicly — confirm `icon.png` is committed on the referenced branch and the raw URL loads in a browser. |
| CI push **did nothing** | You ran the workflow without checking **publish** (that's a dry run) — re-run with **publish** ticked, or push a `v*` tag. |
| CI push fails: **OIDC / trusted publishing** | The nuget.org Trusted Publishing policy doesn't match this run: confirm Repository Owner `kkora`, Repository `XrmToolSuite`, Workflow `publish.yml`, and that the step's `user:` equals the policy's **Package owner**. The job also needs `permissions: id-token: write`. |

---

## Quick reference

```powershell
# One-shot manual publish from repo root (Windows), version already bumped:
dotnet build XrmToolSuite.sln -c Release
dotnet test  testing/UnitTests/UnitTests.csproj -c Release --nologo
New-Item -ItemType Directory -Force artifacts | Out-Null
Get-ChildItem src/Tools -Recurse -Filter *.nuspec |
  Where-Object { $_.Name -notlike '*TemplateTool*' } |
  ForEach-Object { nuget pack $_.FullName -OutputDirectory artifacts }
dotnet nuget push "artifacts\*.nupkg" --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json --skip-duplicate
```
