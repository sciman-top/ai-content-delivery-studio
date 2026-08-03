# FOCUS-009 Reference-Guided Image Edit Production Slice

**Date:** 2026-08-03
**Status:** repo-side completed with fresh Full and Release/package evidence.
**Authority:** repo-only until a separately authorized paid live probe. No paid provider call, external publication, desktop launch, native UIA, or hardware acceptance is authorized by this record.

## Product Boundary And Result

- The live runtime now registers a concrete primary-profile `OpenAiImageEditProvider` for `POST /images/edits`; fake mode still registers the fake provider.
- The supported production shape is one persisted `Subject` source candidate plus an optional same-size PNG mask. Multi-reference style/composition editing, live edit failover, background dispatch, and automatic replay remain frozen.
- Approval and execution are separate application operations. Approval makes zero provider calls and produces an immutable `image-edit-approval-receipt.v1`. Execution reloads the project, hashes the source/mask/instruction again, validates the receipt immediately before dispatch, and rejects drift or unsupported combinations.
- A successful edit persists a new `ReviewPending` candidate with nullable generation-task identity and explicit source/edit provenance. The source candidate is unchanged.
- Existing SQLite workspaces receive an idempotent nullable `EditProvenance` column. Historical `GenerationTaskId NOT NULL` candidate tables are transactionally rebuilt to the current nullable contract while retaining rows and the series-item index.
- Gallery state remains owned by `ImageSeriesGalleryWorkspaceViewModel`. It projects provider capability and stable UIA identifiers, but the real-edit command stays disabled because no desktop paid-authority owner exists. The fake button remains explicitly fake.
- Delivery JSON and CSV preserve source candidate, source/mask/instruction hashes, reference roles, provider, endpoint, model, receipt identity, and request-set hash. Absolute source and mask paths are not part of candidate or delivery provenance.

## Structured Reference Decisions

```reference-decision
{
  "schemaVersion": 1,
  "area": "openai-provider",
  "trigger": "real-provider-enablement",
  "consultedSources": [
    { "path": "https://developers.openai.com/api/docs/guides/image-generation#edit-images", "revision": "consulted-2026-08-03" },
    { "path": "D:/CODE/external/ai-content-delivery-studio-references/01-openai/openai-dotnet", "revision": "383860c" },
    { "path": "D:/CODE/external/ai-content-delivery-studio-references/01-openai/openai-dotnet/tests/Images/ImageEditsTests.cs", "revision": "383860c" }
  ],
  "observedBehavior": "The official single-shot edit path uses the Images API with source image, prompt, optional mask, and explicit output settings. The source and mask contract is bounded, and the repository SDK exposes GenerateImageEditAsync without requiring a stateful Responses chain.",
  "decision": "adapt",
  "affectedContract": "Implement one captured multipart POST /images/edits provider for a persisted Subject source and optional same-size PNG mask. Send model, prompt, image, mask, count, size, quality, and output format; validate hashes, roles, limits, dimensions, and non-overwrite before transport. Keep multi-reference and paid live proof out of scope.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-restore --filter FullyQualifiedName~OpenAiImageEditProviderTests|FullyQualifiedName~ProviderRuntimeServiceCollectionTests|FullyQualifiedName~ProviderCapabilityValidatorTests",
    "rg -n OpenAiImageEditProvider|images/edits|ImageEditReferenceInput src tests"
  ]
}
```

```reference-decision
{
  "schemaVersion": 1,
  "area": "persistence-and-schema",
  "trigger": "migration-behavior",
  "consultedSources": [
    { "path": "D:/CODE/external/ai-content-delivery-studio-references/03-data-persistence/EntityFramework.Docs", "revision": "c5931286" },
    { "path": "src/ContentDeliveryStudio.Infrastructure/Persistence/AppDatabaseInitializer.cs", "revision": "repo-current-2026-08-03" }
  ],
  "observedBehavior": "The repository uses EnsureCreated plus explicit idempotent SQLite compatibility upgrades. Historical candidate tables required GenerationTaskId and had no edit lineage column, while an edited candidate is a provider result without a queue-task identity.",
  "decision": "adapt",
  "affectedContract": "Store path-safe edit provenance as nullable JSON, interpret missing data as an unedited historical candidate, and rebuild only the historical CandidateImages table whose GenerationTaskId is still NOT NULL. Preserve candidate rows, cascade ownership, and the series-item index; new edited rows use GenerationTaskId null.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-restore --filter FullyQualifiedName~ImageEditPersistenceTests|FullyQualifiedName~GenerationQueuePersistenceRecoveryTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.Infrastructure/Persistence/AppDatabaseInitializer.cs,src/ContentDeliveryStudio.Infrastructure/Persistence/Configurations/CandidateImageConfiguration.cs,docs/change-evidence/20260803-focus-009-reference-edit-production.md"
  ]
}
```

```reference-decision
{
  "schemaVersion": 1,
  "area": "workflow-and-ux-architecture",
  "trigger": "queue-gallery-stage-composition",
  "consultedSources": [
    { "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/CommunityToolkit-dotnet", "revision": "b135626" },
    { "path": "docs/change-evidence/20260803-image-series-gallery-state-owner.md", "revision": "repo-verified-2026-08-03" }
  ],
  "observedBehavior": "ImageSeriesGalleryWorkspaceViewModel already owns selected candidate, edit inputs, command state, reload projection, and thumbnail effects. MainWindow coordinates the global operation gate, and the desktop host has no trusted paid-authority source.",
  "decision": "adapt",
  "affectedContract": "Keep edit capability state and both fake/real edit command projections in the gallery owner. Expose stable UIA identifiers and a localized missing-authority explanation, keep real execution disabled, and project persisted candidate edit provenance through gallery rows into delivery without adding a forwarding coordinator.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-restore --filter FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests|FullyQualifiedName~DeliveryPackageTests|FullyQualifiedName~DeliveryWorkflowCoordinatorTests",
    "rg -n RunApprovedImageEditButton|ApprovedImageEditCapabilitySummary|EditProvenance src tests"
  ]
}
```

```reference-decision
{
  "schemaVersion": 1,
  "area": "tooling-and-operator",
  "trigger": "delivery-packaging",
  "consultedSources": [
    { "path": "src/ContentDeliveryStudio.Infrastructure/Delivery/DeliveryPackageWriter.cs", "revision": "repo-current-2026-08-03" },
    { "path": "docs/change-evidence/20260730-phase7-product-hardening-closeout.md", "revision": "repo-accepted-2026-07-30" }
  ],
  "observedBehavior": "The existing delivery writer stages approved images, prompts, metadata, manifest, CSV, and review report into a sibling directory and promotes the package atomically. Manifest inputs are explicit application records; source filesystem paths must not be copied into provenance fields.",
  "decision": "adapt",
  "affectedContract": "Carry the path-free CandidateImageEditProvenance value through gallery, delivery application, package, JSON manifest, and compact CSV identity fields. Retain the existing approved-only filter and atomic directory promotion, and test that absolute source directories do not appear in manifest output.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-restore --filter FullyQualifiedName~DeliveryPackageTests|FullyQualifiedName~DeliveryWorkflowCoordinatorTests",
    "rg -n EditProvenance|editApprovalRequestSetHash src/ContentDeliveryStudio.Application/Delivery src/ContentDeliveryStudio.Infrastructure/Delivery tests/ContentDeliveryStudio.Tests/DeliveryPackageTests.cs"
  ]
}
```

## Verification Readout

| Stage | Fresh result |
| --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore`: passed, 0 warnings and 0 errors. |
| Captured provider | `OpenAiImageEditProviderTests`: 8/8 passed; no network transport exists in the test. |
| Approval and drift | Approved edit focused tests: 2/2 passed; approval makes zero calls and source drift fails before provider dispatch. |
| Persistence | `ImageEditPersistenceTests`: 2/2 passed, including legacy schema upgrade and nullable task/provenance round-trip. |
| Delivery/UI | Delivery focused tests: 6/6 passed; MainWindow/UIA focused tests: 46/46 passed; additional live capability/UIA contract: 2/2 passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; build 0 warnings/errors, 770/770 tests, reference/product contracts, and format passed. |
| Release/package | `scripts/preflight-release.ps1 -NoRestore`: exit 0; Full repeated at 770/770, 84-file `win-x64` ZIP, SHA-256 `7949f34ee53aab67af6991e74ccb13b529f48f8f58b2d74aef8a3af42c39665b`. |
| Paid live probe | `gate_na`: current authority explicitly prohibits real or paid provider calls. Recovery condition: separate current approval for one bounded request and cost ceiling. |
| Native UIA/hardware | `gate_na`: app launch and expanded native UIA/hardware authority were not granted. Alternative verification is ViewModel command-state plus XAML/XML/UIA contract tests. |

Rollback disables the live edit registration/application entrypoints and WPF capability control while retaining nullable provenance columns so edited or historical workspaces remain readable. It must not delete source candidates, edited candidates, user workspaces, or delivery artifacts.
