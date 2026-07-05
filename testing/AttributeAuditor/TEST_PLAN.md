# Attribute Auditor - Test Plan

Traces to [`docs/user-stories/AA1.AttributeAuditor.md`](../../docs/user-stories/AA1.AttributeAuditor.md).

## Scope

> **Tool status: v1 built.** The audit engine ships (usage detection across forms, views, processes,
> and field security; retirement-candidate classification; CSV + HTML export). Deferred features
> (chart/dashboard & data-population signals, table/solution scoping, JSON/Excel export, guarded cleanup)
> remain `[Planned]` and their cases stay `Pending`.

In scope now: usage-signal detection, classification, report projection, CSV export, and the run/filter/export UI.

## Approach

| Tier | What | How | Status |
|---|---|---|---|
| Automated (SDK-free) | Usage scanners, retirement-candidate classification, report projection, CSV | xUnit in `testing/UnitTests/` (`AttributeAuditTests.cs`) | 13 passing |
| Automated (fake conn) | The `AttributeUsageCollector` over a fake `IOrganizationService` | xUnit in `testing/CollectorTests/` (`AttributeAuditCollectorTests.cs`) | 8 passing |
| Manual | Run/scope/filter UI, results grid, CSV + HTML export, settings persistence | GUI cases with a live Dataverse env | Pending |

## Environments

- **Automated:** .NET 8 SDK.
- **Manual:** Windows + XrmToolBox + a Dataverse env with System Customizer+ (a test env with disposable custom columns is ideal for the cleanup cases).

## Exit criteria

- Every shipped feature has at least one executed case at Pass.
- No delete path passes without a confirmation dialog and a passing dependency check (US-AA-5.1).
