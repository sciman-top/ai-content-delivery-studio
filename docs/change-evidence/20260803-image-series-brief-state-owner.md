# FOCUS-005 Image-Series Brief State Owner

**Date:** 2026-08-03
**Status:** repo-side brief sub-slice implemented; fresh Full/Release closeout is recorded in the FOCUS-005 closeout evidence.
**Authority:** repo-only / fake-first. No paid provider, external publication, desktop-app launch, Narrator, or hardware UIA run was performed.

## Scope And Ownership

`ImageSeriesWorkspace.Brief` now owns planning inputs, brief/blueprint/
prompt-direction rows and selections, localization, and the fake planning,
create, generate, and promote commands. MainWindow retains project identity,
exclusive persisted mutation/reload, and cross-stage projection. Brief-related
XAML binds through the owner and keeps its existing AutomationIds.

## Structured Reference Decision

```reference-decision
{
  "schemaVersion": 1,
  "area": "workflow-and-ux-architecture",
  "trigger": "large-viewmodel-split",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/CommunityToolkit-dotnet",
      "revision": "b135626"
    },
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/WPF-Samples/Accessibility/SelectionPattern",
      "revision": "0238f8a"
    }
  ],
  "observedBehavior": "Brief inputs, blueprint and prompt-direction selections, localized projection, and create/generate/promote enablement form one stateful stage. Persistence, reload, and planning-stage synchronization remain shell-global effects.",
  "decision": "adapt",
  "affectedContract": "ImageSeriesWorkspace.Brief owns brief-stage state and commands while MainWindow owns exclusive project mutation/reload. Stable IDs, localization, selection, fake-first behavior, and AutomationIds remain compatible.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/ViewModels/ImageSeriesBriefWorkspaceViewModel.cs,src/ContentDeliveryStudio.App/Views/FakePlanningPanelView.xaml,docs/change-evidence/20260803-image-series-brief-state-owner.md"
  ]
}
```

Rollback only the brief owner, bridge/facade, XAML, tests, architecture, and this
evidence slice. Do not alter persisted projects or prior acceptance evidence.
