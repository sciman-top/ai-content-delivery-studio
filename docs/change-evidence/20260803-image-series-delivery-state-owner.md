# FOCUS-005 Image-Series Delivery State Owner

**Date:** 2026-08-03
**Status:** repo-side delivery sub-slice verified; no manual/live/hardware acceptance claimed.
**Authority:** repo-only / fake-first. No paid provider, external publication, desktop-app launch, Narrator, or hardware UIA run was performed.

## Scope And Ownership

This bounded continuation makes `ImageSeriesWorkspace.Delivery` the owner of
delivery rows, final category options and selection, delivery root, resolved
destination preview, delivery localization, root browsing, export command, and
both commands' enablement. `MainWindowViewModel` retains only the exclusive gate,
selected project and cross-workspace export snapshot, existing delivery
coordinator call, and global workflow-graph projection.

The historical FOCUS-005 baseline is 3,657 lines across five MainWindow partials,
with 199 explicit public properties plus 29 RelayCommand-generated properties.
The delivery family accounted for 16 explicit properties and two commands. Those
names remain temporary facades, so no reflection-public-member reduction is
claimed yet. Remove them only after all direct non-XAML consumers use
`ImageSeriesWorkspace.Delivery`, the remaining image-series state families have
migrated, and one compatibility cutover has its own focused and full evidence.

After queue, gallery, review, and delivery ownership migration, the five partials
total 3,137 lines (`1913 + 532 + 387 + 258 + 47`), 520 fewer than the starting
baseline. `ImageSeriesDeliveryWorkspaceViewModel` is 215 lines and owns mutable
state, projection, localization, and commands rather than forwarding calls.

## Contract Preservation

- Export remains enabled only for an idle selected project with gallery rows, a
  valid category/root destination, and a persisted human-approved passing review.
- The root picker remains optional; its command is disabled when the service is
  absent. Invalid or workspace-local roots still produce an empty preview and
  block export.
- Localized category options preserve the selected enum identity across language
  refresh.
- Review mutation still invalidates delivery; owner row changes rebuild the
  global workflow graph.
- Delivery views and the delivery controls in `ReviewApprovalPanelView` bind via
  `ImageSeriesWorkspace.Delivery`. Existing category/root/browse/preview/export
  AutomationIds remain stable.

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
  "observedBehavior": "Delivery rows, category/root selection, localized option identity, destination validation, root browsing, export enablement, and export results are one stateful delivery boundary. Exclusive execution and the project/gallery/review/blueprint snapshot remain shell-global effects.",
  "decision": "adapt",
  "affectedContract": "ImageSeriesWorkspace.Delivery owns delivery state and commands while MainWindow owns exclusive execution, cross-workspace export inputs, and workflow-graph projection. Human-approval gating, destination validation, category identity, localization, and stable AutomationIds remain compatible.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests|FullyQualifiedName~ReviewWorkflowCoordinatorTests|FullyQualifiedName~ReviewWorkflowApplicationServiceTests|FullyQualifiedName~FinalApprovalWorkflowTests|FullyQualifiedName~DeliveryWorkflowCoordinatorTests|FullyQualifiedName~DeliveryApplicationServiceTests|FullyQualifiedName~DeliveryPackageTests|FullyQualifiedName~DeliveryExportRequestTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/ViewModels/ImageSeriesDeliveryWorkspaceViewModel.cs,src/ContentDeliveryStudio.App/Views/DeliveryView.xaml,docs/change-evidence/20260803-image-series-delivery-state-owner.md"
  ]
}
```

## Verification

| Stage | Result |
| --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore`: exit 0, 0 warnings/errors. |
| Focused | MainWindow, XML/UIA, review/final approval, and delivery contract tests: 66/66 passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; build 0 warnings/errors, 763/763 tests, reference/product-focus contracts, and format passed. |
| Release | `scripts/preflight-release.ps1 -NoRestore`: exit 0; actual 84-file `win-x64` package verified, SHA-256 `d4a33ad50e61ed4d99a2015358d84ad06b8fea68251e35d31cd93c639c9f6675`. |

Rollback only this owner, bridge/facade, XAML binding, tests, architecture, and
evidence slice. Do not alter user workspace, persisted delivery output, or prior
acceptance evidence.
