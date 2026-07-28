# EF JSON Collection Value Comparer Implementation Plan

**Goal:** Give every JSON-converted collection property a consistent deep EF
Core comparer and prove in-place mutation tracking without schema drift.

**Dependencies:** Existing EF Core 10 SQLite persistence mappings and the
repository-mapped `EntityFramework.Docs` value-comparer guidance at revision
`c5931286c90444b8220b14d0c2420f1811b7d2df`.

## Task 1: Lock The Model Contract With A Failing Test

**Acceptance criteria:**

- [x] Model discovery enumerates every collection or dictionary CLR property
  that has a value converter.
- [x] The test reports the entity and property names for every missing comparer.
- [x] The current model fails with the reproduced 20-property warning set.

**Write set:**

- `tests/ContentDeliveryStudio.Tests/PersistenceModelContractTests.cs`

**Verification:**

- `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter PersistenceModelContractTests.JsonConvertedCollections_HaveValueComparers`

## Task 2: Add And Apply The Shared Deep Comparer

**Acceptance criteria:**

- [x] One internal generic comparer provides JSON equality, consistent hashing,
  and deserialize-based deep snapshots.
- [x] All 20 reproduced collection mappings attach the comparer.
- [x] Existing conversion functions and JSON storage representations stay
  unchanged.

**Dependencies:** Task 1

**Write set:**

- `src/ContentDeliveryStudio.Infrastructure/Persistence/Configurations/JsonValueComparer.cs`
- `CreativeBriefConfiguration.cs`
- `DocumentBriefConfiguration.cs`
- `IllustrationPlanConfiguration.cs`
- `OutputArtifactConfiguration.cs`
- `ReviewResultConfiguration.cs`
- `ReviewRubricConfiguration.cs`
- `RoutedRepairPatchConfiguration.cs`
- `SourceAssetConfiguration.cs`

**Verification:**

- rerun the Task 1 focused test and require it to pass
- inspect model creation output for absence of `Model.Validation[10620]`

## Task 3: Prove In-Place Mutation Tracking And Record Evidence

**Acceptance criteria:**

- [x] Mutating `SourceAsset.ExtractedContents` through its existing domain method
  marks that converted property modified after `DetectChanges()`.
- [x] Change evidence records the WPF reproduction, official reference revision,
  compatibility boundary, commands, and rollback.
- [x] No `.env`, SQLite, `workspace/`, `outputs/`, artifacts, or ZIP enters Git.

**Dependencies:** Task 2

**Write set:**

- `tests/ContentDeliveryStudio.Tests/PersistenceModelContractTests.cs`
- `docs/change-evidence/20260728-ef-json-value-comparer.md`
- this implementation plan

**Verification:**

- `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter PersistenceModelContractTests`
- `git diff --check`

## Checkpoint: Fixed-Order Repository Closeout

- [x] Build: `dotnet build ContentDeliveryStudio.sln`
- [x] Test: `dotnet test ContentDeliveryStudio.sln --no-build`
- [x] Contract/invariant:
  `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
- [x] Contract/invariant: `dotnet format --verify-no-changes`
- [x] Hotspot:
  `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`
- [x] Review the exact diff and stage only this slice; do not push.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Comparer semantics diverge from storage conversion | Use the same JSON options and validate equality/hash/snapshot together. |
| A mapping is missed now or later | Model-level discovery fails closed for all converted collection CLR types. |
| Snapshot changes serialized data | Keep existing converters untouched; no migration or schema edit. |
| Mutable collection remains undetected | Focused `SourceAsset` mutation test asserts the property-level tracking result. |

## Approval And Truth Boundary

The user authorized autonomous execution of the recommended bounded slice on
2026-07-28. Completion is repository-side persistence hardening only and makes
no operator acceptance decision. The separately authorized equivalence between
agent-operated and human-operated acceptance is handled as its own contract
slice. This plan does not refresh live accepted evidence, reopen accepted
scientific tasks, authorize paid providers, or authorize push.
