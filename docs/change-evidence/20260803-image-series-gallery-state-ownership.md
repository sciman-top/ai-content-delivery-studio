# Image-Series Gallery And Review State Ownership

**Date:** 2026-08-03
**Task:** `FOCUS-005` incremental vertical slice
**Authority:** repo-only; no provider, paid, live, manual, or hardware action was authorized

## Root Cause

`MainWindowViewModel` owned gallery and review rows, both selections, review approval inputs, empty-state derivation, and localized gallery/review labels even though they are image-series workflow state. Existing coordinators transformed data but did not own this state, so file extraction had not reduced the central binding surface.

```reference-decision
{
  "schemaVersion": 1,
  "area": "workflow-and-ux-architecture",
  "trigger": "large-viewmodel-split",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/docs-desktop",
      "revision": "3ed3bb19178883827aa0a81427576797db141862"
    }
  ],
  "observedBehavior": "Gallery and review views inherited MainWindowViewModel through the tab host and bound directly to workflow collections, selections, approval inputs, empty-state flags, and presentation text. MainWindow owned that state while coordinators only returned projections.",
  "decision": "adapt",
  "affectedContract": "Gallery/review state and presentation text move to ImageSeriesWorkspaceViewModel; XAML binds through ImageSeries and MainWindow retains only cross-workflow command, graph, and thumbnail side-effect observation.",
  "focusedVerification": [
    "dotnet test ContentDeliveryStudio.sln --no-restore --filter FullyQualifiedName~ImageSeriesWorkspaceViewModelTests|FullyQualifiedName~MainWindowViewModelTests",
    "rg -n viewModel\\.(GalleryRows|SelectedGalleryRow|HasGalleryRows|ReviewRows|SelectedReviewRow|HasReviewRows|FinalApprovalReviewer|FinalApprovalNotes) tests src"
  ]
}
```

## Changes And Boundary

- Added a real `ImageSeriesWorkspaceViewModel` owner rather than another stateless coordinator.
- Moved gallery/review collections, selection preservation/reset, empty-state derivation, review approval inputs, and fourteen localized presentation strings out of `MainWindowViewModel`.
- Updated gallery/review XAML and MainWindow tests to bind/assert through `ImageSeries`.
- Kept command invalidation, workflow graph refresh, and thumbnail warmup in MainWindow because those effects cross workflow/global-shell boundaries; the nested owner raises ordinary property changes and does not know shell commands.

This slice measurably removes the direct gallery and review binding surface, but it does not mark `FOCUS-005` complete. Queue, generic delivery, brief, blueprint, and prompt state still remain to be transferred. Scientific state is already isolated in dedicated scientific ViewModels; packaged UIA/manual hardware acceptance remains a separate truth boundary.

## Verification And Rollback

Focused gallery/review ownership plus the complete existing MainWindow ViewModel suite passed 31/31. Canonical Full passed 731/731 core tests; Release passed 753/753 tests plus format, scans, actual win-x64 publish/package, hashes, and diff hygiene. The `MainWindowViewModel.cs` diff removes 222 lines and adds 67 (net -155), and no direct MainWindow gallery/review XAML binding remains.

Rollback restores direct MainWindow properties and bindings for these two adjacent image-series families without changing persisted project, candidate, or review records.
