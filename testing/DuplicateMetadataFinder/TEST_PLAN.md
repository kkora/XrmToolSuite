# Duplicate Metadata Finder - Test Plan

Traces to [`docs/user-stories/ADMIN3.DuplicateMetadataFinder.md`](../../docs/user-stories/ADMIN3.DuplicateMetadataFinder.md).

## Scope

The Duplicate Metadata Finder scores same-kind Dataverse metadata pairs 0–100 from weighted signals,
clusters matches at/above a threshold, and recommends a primary to keep — read-only. These tests verify:

- **Automated (SDK-free):** the similarity primitives (normalize, Levenshtein, token/set Jaccard, content
  hash), per-pair scoring incl. cross-kind isolation and exact content match, threshold-driven
  clustering, the recommended-primary rule, and the `ReportModel` projection (score, banding, findings).
- **Manual (live):** the Dataverse collector (metadata → components), the WinForms grid + detail pane,
  option/threshold/custom-only controls, settings round-trip, the four exports (Excel/PDF/JSON/HTML), and
  that the tool loads in XrmToolBox.

Out of scope: any write path — the tool never merges or deletes (there is nothing destructive to test).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | SDK-free similarity/scoring/grouping/projection | xUnit in `testing/UnitTests/DuplicateMetadataFinderTests.cs`, run with `dotnet test` | .NET 8 SDK |
| Manual | Dataverse collector, WinForms UI, exports, XTB load | Numbered GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + a Dataverse env |

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`).
- **Manual:** Windows + XrmToolBox + a Dataverse connection (System Customizer or higher).

## Entry / exit criteria

- **Entry:** tool builds in Release (`dotnet build XrmToolSuite.sln -c Release`).
- **Exit:** all automated tests pass; all manual cases executed with Pass, or defects logged in the summary.
