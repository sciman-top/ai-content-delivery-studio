# FOCUS-007 Pack Compatibility Surface Reduction

**Date:** 2026-08-03
**Status:** repo-side completed with fresh Full/Release evidence.
**Authority:** repo-only. No provider call, desktop launch, remote distribution, or external publication was performed.

## Runtime And Compatibility Inventory

- No WPF view, ViewModel, desktop DI registration, application use case,
  persistence entity, or delivery writer consumes `BuiltInPackCatalog`,
  `PackPackage.FromRegistry`, or `JsonPackPackageStore.ExportAsync`.
- The catalog and export surface were consumed only by pack-specific tests.
- `pack-package.v1` is a serialized compatibility contract. Its workflow,
  blueprint, industry, renderer, rubric, UI-default, version, and migration
  fields remain parseable through `JsonPackPackageStore.ImportAsync`.
- Scenario IDs loaded from v1 are retained as descriptive profile labels; the
  WPF shell does not build tabs, commands, or maturity claims from them.

## Reduction

- Removed the 476-line built-in starter catalog and its 236-line catalog test.
- Removed registry-to-package export, export validation/version selection, and
  JSON export (57 production lines).
- Replaced export-oriented scenario-family tests with two import compatibility
  tests: a representative legacy v1 scenario profile opens, while a missing
  blueprint reference still fails closed.
- Kept `PackMetadata`, `PackRegistry`, `PackPackage.CreateRegistry`, and the
  import adapter as the minimum legacy compatibility layer. No replacement
  marketplace/profile runtime abstraction was introduced.

## Structured Reference Decision

```reference-decision
{
  "schemaVersion": 1,
  "area": "pack-and-policy-modeling",
  "trigger": "workflow-pack-boundary",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/CommunityToolkit-dotnet",
      "revision": "b135626"
    },
    {
      "path": "docs/change-evidence/20260802-product-focus-and-simplification-plan.md",
      "revision": "repo-accepted-2026-08-02"
    }
  ],
  "observedBehavior": "The built-in catalog described multiple scenario-specific pack families and export mechanics, but the desktop shell and production use cases did not consume them. Only the v1 JSON shape has a plausible compatibility obligation.",
  "decision": "reject",
  "affectedContract": "Retire built-in catalog/export and production-maturity claims; retain import-only pack-package.v1 parsing, reference validation, scenario labels, semantic compatibility, and migration fields. Public distribution and pack-driven UI remain frozen.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter FullyQualifiedName~PackMetadataTests|FullyQualifiedName~PackRegistryTests|FullyQualifiedName~PackUiDefaultsTests|FullyQualifiedName~PackPackageStoreTests|FullyQualifiedName~WorkflowPackageTests|FullyQualifiedName~ArtifactPlanningWorkflowTests|FullyQualifiedName~DeliveryApplicationServiceTests",
    "rg -n BuiltInPackCatalog|PackPackage.FromRegistry|ValidateForExport src tests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.Core/Packs/PackRegistry.cs,src/ContentDeliveryStudio.Application/Packs/PackPackage.cs,docs/change-evidence/20260803-focus-007-pack-compatibility-surface.md"
  ]
}
```

```reference-decision
{
  "schemaVersion": 1,
  "area": "persistence-and-schema",
  "trigger": "migration-behavior",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/03-data-persistence/EntityFramework.Docs",
      "revision": "c5931286"
    },
    {
      "path": "src/ContentDeliveryStudio.Infrastructure/Persistence/AppDbContext.cs",
      "revision": "repo-current-2026-08-03"
    }
  ],
  "observedBehavior": "Pack definitions are not EF entities, project aggregate fields, or SQLite columns. The only serialized compatibility boundary is standalone pack-package.v1 JSON imported by JsonPackPackageStore.",
  "decision": "adapt",
  "affectedContract": "No database migration is required. Preserve standalone v1 JSON deserialization and validation while removing only generation/export/catalog APIs that never persisted into project or workspace state.",
  "focusedVerification": [
    "rg -n PackPackage|PackRegistry src/ContentDeliveryStudio.Infrastructure/Persistence src/ContentDeliveryStudio.Core/Projects",
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter FullyQualifiedName~PackPackageStoreTests|FullyQualifiedName~PersistenceTests|FullyQualifiedName~ProjectApplicationServiceTests"
  ]
}
```

Rollback only the catalog/export removals, compatibility tests, and associated
docs if a proven local legacy consumer requires export. Do not reopen public
marketplace or pack-driven WPF claims as part of rollback.

## Verification

| Stage | Fresh result |
| --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore`: exit 0, 0 warnings/errors. |
| Focused | Pack metadata/registry/UI defaults/import, workflow package, artifact planning, and delivery: 16/16 passed. |
| Compatibility | Representative `pack-package.v1` scenario profile imports; missing blueprint reference fails closed. |
| Reference/product contracts | Pack boundary and no-database-migration decisions passed; product-focus queue contract passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; 746/746 tests and format passed. |
| Release/package | `scripts/preflight-release.ps1 -NoRestore`: exit 0; 84-file `win-x64` package, SHA-256 `ed8e2f27141b9a47b483299f88131e8a4a65ebeaf29f79ae962b76ef93b52915`. |
