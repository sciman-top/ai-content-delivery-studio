# FOCUS-008 Approval-Receipt And Cost-Bounded Live Queue

**Date:** 2026-08-03
**Status:** repo-side completed with fresh Full/Release evidence.
**Authority:** repo-only until a separately authorized paid live probe. No paid provider call, desktop launch, external publication, native UIA, or hardware acceptance was performed.

## Baseline And Decision

- The durable queue already persisted queued/running/terminal checkpoints and recovered interrupted `Running` work as failed, but every non-fake provider was blocked and no approval identity existed.
- `GenerationTask` now owns an optional immutable `generation-approval-receipt.v1` value. The single nullable JSON column is additive; old databases load it as `null`, which means unapproved.
- The canonical request-set hash binds project, ordered task/series/prompt/provider-profile identities, exact prompt text and SHA-256, direct provider, `images` endpoint class, model, width, height, quality, format, background, seed, retry ceiling, and per-operation estimate.
- The receipt additionally persists series IDs, operation count, total estimate, ceiling, approver, authority reference, issue time, and expiry.
- Fake execution keeps its compatibility path. The new live application path requires a non-fake direct provider, explicit paid authority, a current receipt, and validation immediately before every dispatch. A captured provider proves the path without network or paid calls.
- Pause/resume preserve identity. Reorder invalidates the whole batch. Retry creates an unapproved task. Failover provider identities are rejected until destination-specific approval coverage exists.
- `ImageSeriesWorkspaceViewModel` remains the queue state owner. WPF displays ordered request details and receipt/cost details, while live Execute remains disabled because the desktop host has no current paid-authority source.

## Structured Reference Decisions

```reference-decision
{
  "schemaVersion": 1,
  "area": "openai-provider",
  "trigger": "real-provider-enablement",
  "consultedSources": [
    { "path": "D:/CODE/external/ai-content-delivery-studio-references/01-openai/openai-dotnet", "revision": "383860c" },
    { "path": "docs/PROVIDER_ROUTING_POLICY.md", "revision": "repo-current-2026-08-03" }
  ],
  "observedBehavior": "The configured image provider owns the actual model and Images API dispatch. Queue preparation alone carries no provider authority, and an aggregate failover identity cannot prove which paid destination will receive the next call.",
  "decision": "adapt",
  "affectedContract": "Keep the existing provider request shape, add a direct-provider approval path that binds provider/model/images endpoint before dispatch, and reject live failover until each chargeable destination has explicit receipt coverage. Captured transports are the only repo verification surface.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter FullyQualifiedName~GenerationWorkflowApplicationServiceTests|FullyQualifiedName~GenerationQueueTests|FullyQualifiedName~ProviderFailoverTests",
    "rg -n ApprovePreparedLiveGenerationQueue|ExecuteApprovedLiveGenerationQueue|RequireLiveImageGenerationProvider src tests"
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
  "observedBehavior": "Existing SQLite workspaces are created with EnsureCreated and upgraded by idempotent additive GenerationTasks columns. Missing authorization data must never be synthesized during reload or interrupted-work recovery.",
  "decision": "adapt",
  "affectedContract": "Persist the immutable receipt through one nullable JSON-converted GenerationTasks column, add the column idempotently, and interpret null as unapproved. Preserve queued/paused state and convert interrupted Running work to failed without replay.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter FullyQualifiedName~GenerationQueuePersistenceRecoveryTests|FullyQualifiedName~GenerationTaskTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.Infrastructure/Persistence/AppDatabaseInitializer.cs,src/ContentDeliveryStudio.Core/Projects/ProjectModel.cs,docs/change-evidence/20260803-focus-008-approved-live-queue.md"
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
    { "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/docs-desktop", "revision": "3ed3bb1" }
  ],
  "observedBehavior": "The queue is already owned by ImageSeriesWorkspaceViewModel and reload projects task rows through ProjectWorkbenchProjectionCoordinator. The desktop host currently has no trusted paid-authority source and must not manufacture one from button state.",
  "decision": "adapt",
  "affectedContract": "Project request and approval summaries through the existing queue owner, preserve stable UIA identifiers, and expose a fail-closed live control whose enabled state depends on a future current-authority seam. Do not add another forwarding coordinator or a receipt-creation button.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter FullyQualifiedName~MainWindowViewModelTests.QueueOperatorWorkflow|FullyQualifiedName~Phase7AccessibilityContractTests",
    "rg -n QueueExecuteApprovedLiveButton|RequestSummary|ApprovalSummary src/ContentDeliveryStudio.App tests"
  ]
}
```

## Verification

| Stage | Fresh result |
| --- | --- |
| Build | Canonical Full build passed with 0 warnings and 0 errors. |
| Focused | Receipt/domain/persistence/application/WPF focused set: 46/46 passed; live workflow subset including in-flight pause: 14/14 passed. |
| Zero-call | Prepare, approval rejection, pause/resume/reorder/retry construction and drift rejection use recording/captured providers; no external transport is configured. |
| Reference/product contracts | Reference governance and product-focus plan contracts passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; 754/754 tests and format passed. |
| Release/package | `scripts/preflight-release.ps1 -NoRestore`: exit 0; 84-file `win-x64` package, SHA-256 `f5d861a54f2e708789bb2121880504e5b84678ac22a37370487afd8751fa5a27`. |
| Paid live probe | `gate_na`: prohibited by current `repo-only-until-live-probe` authority. Recovery condition: separate explicit paid-call authorization for one bounded request set. |
| Native UIA/hardware | `gate_na`: desktop launch and expanded UIA/hardware authority were not granted. Alternative: XAML/XML accessibility contracts plus ViewModel command-state tests. Recovery condition: FOCUS-012 or separate explicit authorization. |

Rollback disables the live application entrypoints and WPF live control while retaining the nullable receipt column so previously written workspaces remain readable. Fake queue compatibility and interrupted-work recovery remain unchanged.
