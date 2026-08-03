# FOCUS-005 Image-Series Gallery State Owner

**Date:** 2026-08-03
**Status:** repo-side gallery/image-edit sub-slice verified; no manual/live/hardware acceptance claimed.

This bounded continuation makes `ImageSeriesWorkspace.Gallery` the owner of
gallery rows, selected candidate, thumbnail-warmup projection, gallery/image-edit
localization, edit inputs, selected-candidate summary, and the fake image-edit
command. `MainWindowViewModel` retains only the exclusive mutation gate, global
activity feed, and review/delivery invalidation. Existing MainWindow properties
remain temporary compatibility facades and return the owner state/command
instances.

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
    },
    {
      "path": "docs/change-evidence/20260730-phase7-product-hardening-closeout.md",
      "revision": "repo-accepted-2026-07-30"
    }
  ],
  "observedBehavior": "Gallery selection, recycled ListBox keyboard behavior, async thumbnail warmup, fake image edit, activity reporting, and downstream review/delivery invalidation form one stateful UI boundary. Only the exclusive operation gate and cross-workspace effects are shell-global.",
  "decision": "adapt",
  "affectedContract": "ImageSeriesWorkspace.Gallery owns gallery and image-edit state and command enablement while MainWindow keeps only exclusive gating, activity, and downstream invalidation. Native list selection, virtualization, warmup replacement, localization, and stable AutomationIds remain compatible.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests|FullyQualifiedName~GalleryThumbnailCacheTests|FullyQualifiedName~LargeGalleryPerformanceBenchmarkTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/ViewModels/ImageSeriesGalleryWorkspaceViewModel.cs,src/ContentDeliveryStudio.App/Views/GalleryRowsListView.xaml,docs/change-evidence/20260803-image-series-gallery-state-owner.md"
  ]
}
```

Verification so far:

| Stage | Result |
| --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore`: exit 0, 0 warnings/errors. |
| Focused | MainWindow, accessibility/XML/UIA, gallery cache, and large-gallery tests: 44/44 passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; 761/761 tests plus reference, product-focus, and format gates passed. |
| Release | `scripts/preflight-release.ps1 -NoRestore`: exit 0; actual 84-file `win-x64` package verified, SHA-256 `d26bb106d3be69bd23428297146c65dfc00c54cc805931684c6fdaf258d90674`. |

The UIA contract preserves `GalleryCandidateList`, `ImageEditPromptInput`,
`ImageEditMaskPathInput`, and `RunFakeImageEditButton`. No app process was
started and no provider was called. Rollback only this owner, facade, XAML, test,
architecture, and evidence slice; do not alter user workspaces or historical
acceptance records.
