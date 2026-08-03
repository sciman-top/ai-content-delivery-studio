# FOCUS-005 Image-Series Generation Settings Owner

**Date:** 2026-08-03
**Status:** repo-side generation/settings sub-slice implemented; fresh Full/Release closeout is recorded in the FOCUS-005 closeout evidence.
**Authority:** repo-only / fake-first. No paid provider, external publication, desktop-app launch, Narrator, or hardware UIA run was performed.

## Scope And Ownership

`ImageSeriesWorkspace.GenerationSettings` owns image-type preset, style-guide,
and recipe options/selections, their localization, and the derived summary.
`ImageSeriesWorkspace` owns the one-click fake generation command and queue
preparation state. MainWindow keeps the exclusive gate and cross-workspace
reload/invalidation boundary. The recipe inspector and generation button bind
through these owners with stable AutomationIds.

## Structured Reference Decision

```reference-decision
{
  "schemaVersion": 1,
  "area": "workflow-and-ux-architecture",
  "trigger": "queue-gallery-stage-composition",
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
  "observedBehavior": "Generation option selection and summary are one cohesive state family, while one-click fake generation and queue preparation share planning and queue command state. Exclusive mutation, reload, and downstream invalidation remain shell-global.",
  "decision": "adapt",
  "affectedContract": "GenerationSettings owns option state/localization/summary and ImageSeriesWorkspace owns fake generation/queue commands. Operation gating, reload, selection, localization, fake-first behavior, and AutomationIds remain compatible.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/ViewModels/ImageSeriesGenerationSettingsWorkspaceViewModel.cs,src/ContentDeliveryStudio.App/Views/StyleRecipeInspectorPanelView.xaml,docs/change-evidence/20260803-image-series-generation-settings-owner.md"
  ]
}
```

Rollback only the generation/settings owner, root command bridge, XAML, tests,
architecture, and this evidence slice. Do not alter persisted projects or prior
acceptance evidence.
