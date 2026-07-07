# AI Solution Reviewer - Test Plan

Traces to [`docs/user-stories/AI10.AiSolutionReviewer.md`](../../../docs/user-stories/AI10.AiSolutionReviewer.md).

## Scope

Validate the AI Solution Reviewer end to end: the SDK-free report projection and concern scoring
(automated), and the Dataverse fact collectors, the AI/offline review generation, the Word export, and
the WinForms UI (manual, against a live org — the AI path also needs an API key).

## Approach

| Tier | What | How | Environment |
|---|---|---|---|
| Automated | Concern score, per-area metrics, AI-prompt sections (US-AI10-2, US-AI10-3) | xUnit over `ReviewReport` | .NET 8 SDK (`testing/UnitTests`) |
| Manual | Collectors, AI/offline review, Word/PDF/HTML export, UI (US-AI10-1, US-AI10-3, US-AI10-4) | GUI cases in `TEST_CASES.md`, evidence in `screenshots/` | Windows + XrmToolBox + org (+ AI key for the AI path) |

## What is NOT automatable here

The collectors need a live solution; the AI review makes an HTTPS call to a provider (key required); the
Word/PDF exporters and UI need the net48/WinForms host. These are documented manual cases with a screenshot each.

## Environments

- **Automated:** .NET 8 SDK (`dotnet test testing/UnitTests/UnitTests.csproj`) — no connection, no AI.
- **Manual:** Windows + XrmToolBox + a Dataverse connection; an Anthropic/OpenAI/Google API key for the AI path.

## Entry / exit criteria

- **Entry:** tool builds `Release` with zero warnings; automated tests green.
- **Exit:** all automated tests pass; all manual cases executed with a screenshot, or defects logged.

## Risks

- The AI must receive ONLY the anonymized payload — verify the consent preview (TC-AI10-M-05).
- The offline fallback must produce a usable review when no key is present (TC-AI10-M-04).
