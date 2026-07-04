# Attribute Auditor - Test Plan

Traces to [`docs/user-stories/AttributeAuditor.md`](../../docs/user-stories/AttributeAuditor.md).

## Scope

> **Tool status: WIP.** Only the scaffold exists (loads sample accounts); the audit logic is not yet
> implemented. These cases are the acceptance tests for the planned features and are all `Pending`
> until the corresponding user stories are built.

Planned coverage: scope selection, usage-signal detection, classification, export, and guarded cleanup.

## Approach

| Tier | What | How | When |
|---|---|---|---|
| Automated | Pure helpers once they exist (e.g. usage-signal aggregation, "unused" classification) | xUnit in `testing/UnitTests/` on SDK-free logic | When FEAT-AA-2/3 land |
| Manual | Scope UI, running an audit, results grid, export, guarded delete | GUI cases with a live Dataverse env | When features land |

## Environments

- **Automated:** .NET 8 SDK.
- **Manual:** Windows + XrmToolBox + a Dataverse env with System Customizer+ (a test env with disposable custom columns is ideal for the cleanup cases).

## Exit criteria

- Every shipped feature has at least one executed case at Pass.
- No delete path passes without a confirmation dialog and a passing dependency check (US-AA-5.1).
