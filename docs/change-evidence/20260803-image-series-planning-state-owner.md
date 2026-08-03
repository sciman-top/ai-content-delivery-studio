# FOCUS-005 Image-Series Plan And Prompt State Owner

**Date:** 2026-08-03
**Status:** repo-side plan/prompt sub-slice verified; no manual/live/hardware acceptance claimed.
**Authority:** repo-only / fake-first. No paid provider, external publication, desktop-app launch, Narrator, or hardware UIA run was performed.

## Scope And Ownership

This bounded continuation makes `ImageSeriesWorkspace.Planning` the owner of
series/item/prompt rows and selection, plan and prompt editor inputs, localized
labels and columns, selected-item summary, and create-series/add-item/
create-prompt commands with their enablement. MainWindow retains selected-project
identity, the exclusive mutation gate, persisted project reload snapshots, and
selection notifications to the still-separate brief/blueprint state family.

The historical FOCUS-005 baseline is 3,657 lines across the five MainWindow
partials, with 199 explicit public properties plus 29 RelayCommand-generated
properties. The plan/prompt family accounted for 45 explicit properties and
three commands. Those names remain compatibility facades, so no reflection-
public-member reduction is claimed yet. Remove them only after non-XAML consumers
use `ImageSeriesWorkspace.Planning`, remaining image-series state families have
migrated, and a separate compatibility cutover passes focused and full gates.

After queue, gallery, review, delivery, and plan/prompt ownership migration, the
five partials total 2,875 lines (`1660 + 532 + 380 + 256 + 47`), 782 fewer than
the starting baseline. `ImageSeriesPlanningWorkspaceViewModel` is 356 lines and
owns mutable state, selection projection, localization, and commands.

## Contract Preservation

- Create series, add item, and create prompt remain disabled without their
  selected project/series/item and required title/prompt inputs.
- Mutations still run through `MainWindowOperationGate`, persist through the
  existing coordinator, and apply one project reload snapshot.
- Reload restores series and item selection by stable IDs, then projects prompt
  history, plan rows, and prompt rows.
- Series/item changes continue refreshing brief and prompt-direction command
  state; prompt projection continues refreshing generation command state.
- Plan, prompt, plan-editor, and prompt-editor XAML bind through
  `ImageSeriesWorkspace.Planning`; existing series/item/prompt AutomationIds and
  queue/generation controls remain stable.

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
    },
    {
      "path": "docs/change-evidence/20260730-phase7-product-hardening-closeout.md",
      "revision": "repo-accepted-2026-07-30"
    }
  ],
  "observedBehavior": "Series/item selection, plan and prompt projections, editor inputs, localized selection summaries, and create-series/add-item/create-prompt command state form one stateful planning boundary. Project identity, exclusive persistence, reload, and brief-stage side effects remain shell-global during this compatibility slice.",
  "decision": "adapt",
  "affectedContract": "ImageSeriesWorkspace.Planning owns plan/prompt state and commands while MainWindow owns exclusive project mutation/reload and cross-stage notifications. Stable selection IDs, localization, prompt history, generation enablement, and AutomationIds remain compatible.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests|FullyQualifiedName~PlanEditorWorkflowCoordinatorTests|FullyQualifiedName~PlanningWorkflowCoordinatorTests|FullyQualifiedName~GenerationWorkflowCoordinatorTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/ViewModels/ImageSeriesPlanningWorkspaceViewModel.cs,src/ContentDeliveryStudio.App/Views/PlanEditorPanelView.xaml,docs/change-evidence/20260803-image-series-planning-state-owner.md"
  ]
}
```

## Verification

| Stage | Result |
| --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore`: exit 0, 0 warnings/errors. |
| Focused | MainWindow, XML/UIA, plan editor/planning, and generation coordinator tests: 49/49 passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; build 0 warnings/errors, 764/764 tests, reference/product-focus contracts, and format passed. |
| Release | `scripts/preflight-release.ps1 -NoRestore`: exit 0; actual 84-file `win-x64` package verified, SHA-256 `733baa29d29e889f38f198d7bdd27e47a2a6a81552f6479e0585c8558c3b35a4`. |

Rollback only this owner, bridge/facade, XAML binding, tests, architecture, and
evidence slice. Do not alter user workspace, persisted projects, or prior
acceptance evidence.
