# Abstraction Retirement And Governance Scope

**Date:** 2026-08-03
**Tasks:** `FOCUS-006` plus the governance-scope correction identified during its preflight
**Authority:** repo-only; no provider, paid, live, manual, or hardware action was authorized

## Root Cause And Consumer Inventory

The repository treated two development ideas as production runtime contracts:

- `ApplicationModuleCatalog` and `FeatureViewModuleDefinition` described repository folders, type names, command names, and test names, but were consumed only by their own repository-shape tests.
- `IRemoteWorkflowEngineAdapter` had one fake no-network implementation. Its only runtime consumer was unconditional desktop DI registration; there was no command, view, persistence mapping, SQLite/JSON field, delivery manifest field, diagnostics record, or package format that invoked or stored it.

The reference gate also matched the entire `ViewModels/` and `Views/` trees. That path-level rule made an ordinary feature ViewModel edit indistinguishable from a central shell or workflow-boundary change and contradicted `NFR-GOV-001`.

## Decisions

```reference-decision
{
  "schemaVersion": 1,
  "area": "workflow-and-ux-architecture",
  "trigger": "module-boundary-change",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/docs-desktop",
      "revision": "3ed3bb19178883827aa0a81427576797db141862"
    }
  ],
  "observedBehavior": "The WPF shell statically composes its views. Repository-folder module records and FeatureViewModule metadata had no runtime composition consumer, while directory-wide ViewModel reference enforcement treated routine feature edits as architecture changes.",
  "decision": "reject",
  "affectedContract": "Runtime module metadata is removed; reference enforcement is limited to MainWindow, its operation gate, and explicit application workflow/module boundaries.",
  "focusedVerification": [
    "dotnet test ContentDeliveryStudio.sln --filter FullyQualifiedName~ReferenceEvidenceScript_DoesNotRequireEvidenceForOrdinaryViewModelChanges",
    "rg -n ApplicationModuleCatalog|FeatureViewModuleDefinition src tests eval"
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
      "path": "D:/CODE/external/ai-content-delivery-studio-references/08-platform-and-observability/dotnet-extensions",
      "revision": "c6bf2e02eeda9bc67003d509db59ad8f887f1c47"
    }
  ],
  "observedBehavior": "The desktop composition root registered a fake remote-workflow adapter even though no application service, view, persistence record, delivery contract, or diagnostic contract resolved it.",
  "decision": "reject",
  "affectedContract": "The desktop host no longer registers remote workflow services; a future integration requires a concrete user-visible consumer and a separately approved product decision.",
  "focusedVerification": [
    "dotnet build ContentDeliveryStudio.sln --no-restore",
    "rg -n RemoteWorkflow|IRemoteWorkflow|AddBuiltInRemoteWorkflow src tests eval"
  ]
}
```

## Changes

- Removed the application module catalog, feature-view module contract, fake remote-workflow contract/implementation/registration, and tests that only protected those speculative surfaces.
- Rewrote current architecture and roadmap truth so static WPF composition is not described as pack-driven runtime modules.
- Narrowed `workflow-and-ux-architecture` reference matching from all feature ViewModels/views to the central shell, `MainWindowViewModel`, its operation gate, and explicit workflow/module boundaries.
- Added a red-green verifier regression proving an ordinary feature ViewModel path does not require a ceremonial reference record while `MainWindowViewModel` remains fail-closed.

## Compatibility And Rollback

No serialized type, persistence mapping, migration, pack payload, diagnostics schema, delivery manifest, or public command referenced the removed types. No migration adapter is therefore required. Rollback restores only the deleted contracts, fake registration, tests, and prior documentation; it must not invent a live remote-workflow maturity claim.

## Verification

| Check | Result |
| --- | --- |
| Consumer inventory over `src`, `tests`, and `eval` | No remaining module-catalog, feature-module, or remote-workflow runtime/test consumer. |
| Focused reference contract | 3/3 passed, including the red-green ordinary-ViewModel regression. |
| Host/provider registration contracts | 2/2 passed. |
| Full | Build 0 warnings/errors; 731/731 core tests; reference/product-focus/diff contracts passed. |
| Release | 753/753 tests; format, scans, win-x64 publish/package, hashes, and diff hygiene passed. Package contained 84 files and selected `ContentDeliveryStudio.App.exe`. |

The removed implementation accounts for 599 production lines and 392 tests that protected only the unconsumed abstractions. Repo-side verification never upgrades human, live, paid, or hardware acceptance.
