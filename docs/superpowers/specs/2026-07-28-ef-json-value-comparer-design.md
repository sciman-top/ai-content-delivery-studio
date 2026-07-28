# EF JSON Collection Value Comparer

## Goal

Eliminate the EF Core model warnings and change-tracking gap for collection and
dictionary properties persisted through JSON value converters. The fix must
detect in-place mutations by using matching deep equality, hash, and snapshot
semantics without changing the SQLite schema or serialized JSON contract.

## Context And Root Cause

The fake-first native WPF probe on 2026-07-28 consistently emitted EF Core
`Model.Validation[10620]` warnings for 20 collection-valued properties. Each
property has a JSON `ValueConverter`, but none has a `ValueComparer`.

EF Core snapshots reference types by retaining the same instance unless a
comparer supplies a deep snapshot. An in-place collection mutation can therefore
change both the current value and its snapshot, leaving the converted property
undetected by `ChangeTracker.DetectChanges()`.

The repository-mapped EF Core documentation at revision
`c5931286c90444b8220b14d0c2420f1811b7d2df` confirms that mutable collections
using value conversion require corresponding equality, hash, and deep snapshot
logic.

## Selected Design

Add one internal generic JSON value comparer in the persistence configuration
layer. It will:

- compare values by their deterministic serialized JSON representation;
- derive the hash from the same serialized representation; and
- deep-snapshot values by serializing and deserializing them.

Each collection or dictionary property that already uses JSON conversion will
attach the comparer to its property metadata. Existing converter functions and
JSON options remain the serialization authority, so database values and schema
stay compatible.

The model contract test discovers collection-valued converted properties and
fails when any lacks a comparer. A focused tracking test mutates a tracked
`SourceAsset.ExtractedContents` collection through the existing domain method
and asserts that the specific property becomes modified after change detection.

## Acceptance Criteria

- Every collection or dictionary property with a value converter has an
  explicit value comparer in the EF model.
- Equality, hashing, and snapshots use one consistent JSON representation.
- A tracked in-place collection mutation marks the converted property modified.
- EF Core `Model.Validation[10620]` is no longer emitted for these mappings.
- No migration, column, serialized JSON, provider, or domain contract changes.
- Fixed-order verification passes: build, test, contract/invariant, hotspot.

## Non-Goals

- redesigning domain collections or making unrelated entities immutable;
- changing the `ArtifactPackage.Manifest` mapping, which is not a collection and
  is outside the reproduced warning set;
- refreshing live-provider evidence or human operator/manual evidence;
- changing accepted scientific Tasks 1-30 or Checkpoints 0-5.

## Write Set

- `src/ContentDeliveryStudio.Infrastructure/Persistence/Configurations/JsonValueComparer.cs`
- the eight collection-mapping configuration files named in the implementation
  plan
- `tests/ContentDeliveryStudio.Tests/PersistenceModelContractTests.cs`
- this spec and its implementation plan
- `docs/change-evidence/20260728-ef-json-value-comparer.md`

## Rollback

Revert this bounded slice. No migration or data rollback is required because
the storage schema and serialized values do not change. Existing runtime output,
accepted evidence, and ignored operator-trial sessions remain untouched.
