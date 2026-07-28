# EF JSON Collection Value Comparer Evidence

Date: 2026-07-28

## Status

`repo-side persistence hardening complete`

This bounded slice does not change or decide operator acceptance. The user's
separate authorization for agent-operated acceptance equivalence is handled by
the operator-trial contract slice that follows this commit.

## Reproduction And Root Cause

The isolated fake-first native WPF session `20260728-214821-253` consistently
emitted EF Core `Model.Validation[10620]` for 20 JSON-converted collection or
dictionary properties. The warning states that a value converter is present but
no value comparer is configured.

The initial model contract test failed with the same 20 properties:

- `CreativeBrief`: `DesignBlueprints`, `MustAvoid`, `MustInclude`,
  `PromptDirections`
- `DocumentBrief`: `KeyClaims`, `KnownConstraints`, `Sections`,
  `VisualOpportunities`
- `IllustrationPlan`: `CoverageNotes`, `RiskNotes`, `Targets`
- `OutputArtifact`: `EvidenceAnchorIds`, `Metadata`, `SourceAssetIds`
- `ReviewResult`: `HardFailures`, `Scores`
- `ReviewRubric.Dimensions`
- `RoutedRepairPatch.Items`
- `SourceAsset`: `EvidenceAnchors`, `ExtractedContents`

All affected configurations supplied only a JSON `ValueConverter`. EF's default
reference-type snapshot therefore retained the same collection instance and
could miss in-place mutations.

## Reference Basis And Decision

- Route: `persistence-and-schema`
- Local reference:
  `D:\CODE\external\ai-content-delivery-studio-references\03-data-persistence\EntityFramework.Docs\entity-framework\core\modeling\value-comparers.md`
- Runnable reference sample:
  `D:\CODE\external\ai-content-delivery-studio-references\03-data-persistence\EntityFramework.Docs\samples\core\Modeling\ValueConversions\MappingListProperty.cs`
- Reference revision: `c5931286c90444b8220b14d0c2420f1811b7d2df`
- Decision: adapt the official equality/hash/deep-snapshot pattern into one
  persistence-owned generic comparer and keep every existing converter as the
  storage serialization authority.

The comparer canonicalizes JSON objects by ordinal property-name order while
preserving array order. Equality and hashing therefore share the same structural
representation, including dictionary insertion-order equivalence. Deep
snapshots reuse each field's existing serialize/deserialize functions.

## Regression Evidence

The focused test sequence demonstrated the red-green boundary:

1. The model discovery test failed with all 20 missing mappings.
2. After applying the shared comparer, the same test passed.
3. A tracked `SourceAsset.ExtractedContents` in-place mutation marked that exact
   property modified after `ChangeTracker.DetectChanges()`.
4. Equality, hashing, and deep snapshot behavior passed for a GUID list.
5. A dictionary insertion-order test failed against raw string comparison, then
   passed after canonical object-key ordering was added.

Final focused command:

`dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter "FullyQualifiedName~PersistenceModelContractTests" --nologo`

Result: exit `0`, `4 / 4` passed.

## Compatibility And Data Boundary

- SQLite tables, columns, migrations, and model schema are unchanged.
- Existing JSON converter functions and serialized database values are
  unchanged.
- Provider contracts and provider selection are unchanged; no paid provider was
  called and live evidence was not refreshed.
- Scientific Tasks 1-30 and Checkpoints 0-5 remain accepted and closed.
- No `.env`, artifact, SQLite, workspace, output, or ZIP belongs to this write
  set.

## Fixed-Order Verification

The final implementation tree was verified in the required order before this
evidence document was added:

1. Build: `dotnet build ContentDeliveryStudio.sln --nologo`
   - exit `0`; `0` warnings; `0` errors
2. Test: `dotnet test ContentDeliveryStudio.sln --no-build --nologo`
   - exit `0`; `677 / 677` passed
3. Contract/invariant:
   - `scripts/verify-reference-evidence.ps1`: passed for
     `persistence-and-schema`
   - `dotnet format ... --verify-no-changes`: exit `0`
4. Hotspot: `scripts/preflight-release.ps1 -NoRestore`
   - exit `0`; nested build `0 / 0`, tests `677 / 677`, format, publish WhatIf,
     placeholder/conflict scans, reference gate, and diff hygiene passed

The same fixed order is rerun after this evidence and the completed plan are on
the final tree.

## Review

Five-axis review covered correctness, readability, architecture, security, and
performance. One required issue was found and fixed before closeout: raw JSON
string equality treated dictionary insertion order as a change. Canonical object
key ordering and a red-green regression test resolved it. No dependency,
credential, query, unbounded I/O, or UI risk was introduced.

## Rollback

Revert this slice's comparer, configuration attachments, tests, spec, plan, and
evidence. No migration or data rollback is required. Git rollback must not alter
the user's `AGENTS.md` change or ignored runtime/operator-trial data.
