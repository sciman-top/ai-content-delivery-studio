# Scientific Source Model Evidence

Date: 2026-07-26

## Scope

This evidence records Task 6 of the trustworthy scientific-figure workflow.
The slice adds immutable source authority and extraction-diagnostic records;
it does not implement a PDF adapter, persistence, understanding, rendering,
provider calls, or a user interface.

The model reuses the existing `SourceAsset` identity through
`SourceAssetId`. It preserves:

- a required `sha256:<64 hex>` source hash
- extractor provider identity and version
- page and section
- bounding region and/or character range
- original source text when recovered
- required/non-required source-block status
- explicit formula/table recovery state
- extraction quality and diagnostics
- computed `Ready` or `Blocked` status with stable blocking codes

## Fail-Closed Rules

Construction rejects:

- an empty `SourceAssetId`
- a missing or malformed source hash
- empty extractor identity/version
- non-positive pages
- negative or inverted offsets
- non-finite, negative, or zero-sized bounding regions
- locations with neither coordinates nor offsets
- undefined enum values
- formula/table blocks without an explicit recovery result
- duplicate block IDs or an empty block collection

The result is blocked when:

- a scanned source has no OCR
- reading order is corrupted
- required content is missing
- a required formula/table is missing or uncertain
- an explicit diagnostic has blocking severity

Callers cannot directly set the final status, so blocking evidence cannot be
normalized into a successful extraction.

## Test-First Evidence

Before implementation, the focused command failed to compile because the
`ContentDeliveryStudio.Core.ScientificFigures` namespace and records did not
exist. After the minimal implementation:

- command:
  `dotnet test ContentDeliveryStudio.sln --filter "ScientificSourceModelTests|ApplicationModuleCatalogTests" --no-restore`
- result: exit `0`, `15 / 15` passed

Code review added an immutability regression test. It first demonstrated that
an exposed array could be cast to `IList<T>` and mutated, then passed after
blocks, diagnostics, and blocking codes were wrapped as read-only snapshots.

The application module catalog now registers `scientific-figures` with
repository-owned application and core folders. No infrastructure folder is
claimed before an adapter exists.

## Compatibility And N/A

- Existing source ingestion remains unchanged; no existing provider consumes
  the new records.
- Runtime dependency/supply-chain change: `gate_na`; no package or executable
  was added.
- Persistence migration: `gate_na`; Task 6 adds domain records only. Recovery
  condition: the later persistence task must add migration, compatibility,
  and rollback evidence.
- Paid/live provider: `gate_na`; no provider call was made.
- Performance: bounded immutable snapshots only; no I/O or unbounded external
  enumeration is introduced.

## Rollback

Revert the Task 6 commit to remove the scientific source model, its tests,
module marker/catalog entry, evidence, and Task 6 status updates. The
Checkpoint 0 corpus approval remains independent and must not be reverted as
part of Task 6 rollback.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh outcomes on the Task 6 slice:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `489 / 489` passed, 0 failed, 0 skipped
- reference evidence: exit `0`; both `workflow-and-ux-architecture` and
  `scientific-figure-workflow` detected the repository-owned plan evidence
- format verification: exit `0`
- release preflight: exit `0`; canonical repository verification, publish
  WhatIf, placeholder/conflict scans, and diff hygiene passed
