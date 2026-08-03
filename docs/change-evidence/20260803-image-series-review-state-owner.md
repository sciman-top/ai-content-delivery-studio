# FOCUS-005 Image-Series Review State Owner

**Date:** 2026-08-03
**Status:** repo-side review/final-approval sub-slice verified; no manual/live/hardware acceptance claimed.
**Authority:** repo-only / fake-first. No paid provider, external release, desktop-app launch, Narrator, or hardware UIA run was performed.

## Scope And Ownership

This bounded continuation makes `ImageSeriesWorkspace.Review` the state owner for
review rows, selected review, reviewer/notes inputs, review and final-approval
localization, fake review, approve/reject commands, and their `CanExecute` state.
`MainWindowViewModel` retains the exclusive mutation gate, selected-project
identity checks, project reload snapshot application, workflow-graph projection,
and downstream delivery invalidation.

The historical FOCUS-005 baseline remains 3,657 lines across the five
`MainWindowViewModel` partials, with 199 explicit public properties plus 29
RelayCommand-generated properties. The review family accounted for 18 explicit
properties and three commands. Those public names remain as temporary facades,
so this slice does not claim a reflection-public-member reduction. The facade
removal condition is: migrate all direct non-XAML consumers to
`ImageSeriesWorkspace.Review`, migrate the remaining image-series state families,
and remove the legacy aliases in one separately verified compatibility cutover.

After queue, gallery, and review ownership migration, the five partials total
3,264 lines (`2012 + 532 + 414 + 259 + 47`), 393 fewer than the starting
baseline. `ImageSeriesReviewWorkspaceViewModel` is 222 lines and owns behavior,
not forwarding-only coordination.

## Contract Preservation

- `RunFakeReview` remains fake-first and can run only with a selected project,
  gallery candidates, and an idle exclusive operation gate.
- Approve remains available only for a passing review that does not require
  repair and has a reviewer. Reject still requires both reviewer and notes.
- Final approval persists through the existing coordinator and project reload;
  selection is restored by candidate identity.
- Any review mutation invalidates delivery, and export remains blocked until a
  persisted human-approved passing review exists.
- Review XAML now binds through `ImageSeriesWorkspace.Review`; stable
  `RunFakeReviewButton`, reviewer/notes input, approve, and reject AutomationIds
  are unchanged. The mixed approval/delivery inspector continues to bind its
  delivery controls to the MainWindow delivery state.

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
    },
    {
      "path": "docs/change-evidence/20260730-phase7-product-hardening-closeout.md",
      "revision": "repo-accepted-2026-07-30"
    }
  ],
  "observedBehavior": "Review rows, candidate selection, reviewer inputs, fake review, final approve/reject enablement, reload selection, and the human delivery gate form one stateful stage boundary. Exclusive mutation, cross-workspace reload, graph projection, and delivery invalidation remain shell-global effects.",
  "decision": "adapt",
  "affectedContract": "ImageSeriesWorkspace.Review owns review/final-approval state and commands while MainWindow owns the operation gate, project reload, graph projection, and delivery invalidation. Human approval persistence, candidate identity, localization, native ListBox selection, and stable AutomationIds remain compatible.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests|FullyQualifiedName~ReviewWorkflowCoordinatorTests|FullyQualifiedName~ReviewWorkflowApplicationServiceTests|FullyQualifiedName~FinalApprovalWorkflowTests|FullyQualifiedName~DeliveryWorkflowCoordinatorTests|FullyQualifiedName~DeliveryApplicationServiceTests|FullyQualifiedName~DeliveryPackageTests|FullyQualifiedName~DeliveryExportRequestTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/ViewModels/ImageSeriesReviewWorkspaceViewModel.cs,src/ContentDeliveryStudio.App/Views/ReviewView.xaml,docs/change-evidence/20260803-image-series-review-state-owner.md"
  ]
}
```

## Verification

| Stage | Result |
| --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore`: exit 0, 0 warnings/errors. |
| Focused | MainWindow, XML/UIA, review application/coordinator/final approval, and delivery contract tests: 65/65 passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; build 0 warnings/errors, 762/762 tests, reference/product-focus contracts, and format passed. |
| Release | `scripts/preflight-release.ps1 -NoRestore`: exit 0; actual 84-file `win-x64` package verified, SHA-256 `144a6262dace88891b899c8afe9ec9235fe49c6cfc05a06929aad7d5cf0a0f38`. |

Rollback only this owner, bridge/facade, XAML binding, tests, architecture, and
evidence slice. Do not alter user workspaces, persisted delivery data, or prior
acceptance evidence.
