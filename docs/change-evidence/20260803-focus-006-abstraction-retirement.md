# FOCUS-006 Unconsumed Module And Remote-Workflow Retirement

**Date:** 2026-08-03
**Status:** repo-side completed with fresh Full/Release evidence.
**Authority:** repo-only. No provider call, external publication, desktop-app launch, or live/manual/hardware acceptance was performed.

## Consumer Inventory

The removal inventory searched source, tests, persistence, package/workflow
formats, docs, DI registrations, and user-visible XAML/commands:

- `ApplicationModuleCatalog.cs` and its `FeatureViewModuleDefinition` were only
  consumed by their dedicated tests and described repository folders/use-case
  names; no application service, persistence record, package codec, DI lookup,
  or WPF binding consumed them.
- `RemoteWorkflowEngineContract.cs`, `FakeRemoteWorkflowEngineAdapter.cs`, and
  `RemoteWorkflowServiceCollectionExtensions.cs` were only consumed by their
  dedicated tests and the host registration. No persisted schema, package
  format, ViewModel command, or XAML binding referenced them.
- `WorkflowViewSlotIds` and pack metadata were deliberately not removed: they
  are consumed by the pack surface and are scoped to FOCUS-007.

## Changes

- Removed the unused application module catalog and feature-view metadata
  contract plus their dedicated folder-existence/shape tests.
- Removed the fake-only remote workflow contract, adapter, DI extension, and
  dedicated adapter/registration tests.
- Removed the desktop host registration and stale using directive.
- Updated architecture/roadmap language so remote workflow execution is frozen,
  not presented as an active runtime capability.

## Structured Reference Decision

```reference-decision
{
  "schemaVersion": 1,
  "area": "workflow-and-ux-architecture",
  "trigger": "module-boundary-change",
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
  "observedBehavior": "The module catalog and fake remote workflow seam had no production, persistence, package, or user-visible consumer; only tests and the desktop registration referenced them.",
  "decision": "reject",
  "affectedContract": "Keep behavior-bearing application services, pack metadata, and local fake-first provider runtime. Remove repository-folder metadata and fake remote execution from the desktop composition root; no persisted migration is required because no record or codec references the removed types.",
  "focusedVerification": [
    "rg -n -i IRemoteWorkflow|FakeRemoteWorkflow|ApplicationModuleCatalog|FeatureViewModuleDefinition src tests",
    "dotnet build ContentDeliveryStudio.sln --no-restore",
    "dotnet test ContentDeliveryStudio.sln --no-build",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/App.xaml.cs,docs/change-evidence/20260803-focus-006-abstraction-retirement.md"
  ]
}
```

```reference-decision
{
  "schemaVersion": 1,
  "area": "host-and-observability",
  "trigger": "dependency-injection",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/docs-desktop",
      "revision": "b135626"
    },
    {
      "path": "docs/ARCHITECTURE.md",
      "revision": "repo-current-2026-08-03"
    }
  ],
  "observedBehavior": "The desktop host registered a fake-only remote workflow adapter even though no ViewModel, application service, persistence record, package codec, or user-visible command resolved it.",
  "decision": "reject",
  "affectedContract": "Remove only the remote-workflow using and registration from App.xaml.cs. Provider runtime, diagnostics journal, local tool adapters, persistence, and fake-first image/text/vision composition remain unchanged.",
  "focusedVerification": [
    "dotnet build ContentDeliveryStudio.sln --no-restore",
    "dotnet test ContentDeliveryStudio.sln --no-build",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore"
  ]
}
```

## Acceptance Boundary

The task asks for a fake-first WPF launch after removal. Current authority
forbids starting or stopping a possibly-used desktop app, so that launch is
`gate_na` here:

- `reason`: no desktop-app lifecycle authority was granted.
- `alternative_verification`: solution build/test, source consumer scan, XML/UIA
  contracts, and Release package preflight prove the removal at repo/package
  level without launching the app.
- `evidence_link`: this file and the final preflight output below.
- `expires_at`: when explicit desktop-launch authority is granted or FOCUS-012
  enters execution.
- `recovery_condition`: run the fake-first launch smoke check and record its
  exit/close behavior separately; do not relabel repo-side evidence as live
  acceptance.

Rollback only the removed registrations/contracts/tests and this documentation
slice if a real consumer is later discovered. Do not restore a speculative
remote runtime merely to satisfy a historical test.

## Verification

| Stage | Fresh result |
| --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore`: exit 0, 0 warnings/errors. |
| Focused/consumer | Removed-symbol scan returned no src/tests consumer; all 756 remaining tests passed. |
| Reference/product contracts | Host DI and workflow module-boundary decisions passed; product-focus queue contract passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; 756/756 tests and format passed. |
| Release/package | `scripts/preflight-release.ps1 -NoRestore`: exit 0; 84-file `win-x64` package, SHA-256 `0babd33b222259f8851563c82f14a960364ce66fe94880bc5ecd68fb36618199`. |
