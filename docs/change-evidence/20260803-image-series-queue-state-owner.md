# FOCUS-005 Image-Series Queue State Owner

**Date:** 2026-08-03
**Status:** repo-side queue-state slice verified; FOCUS-005 remains in progress for later bounded image-series state-owner slices.
**Authority:** repo-only / fake-first. No paid provider, external release, desktop-app launch, human acceptance, or hardware/UIA acceptance was performed.

## Scope And Starting Boundary

This bounded slice moves only the image-series generation-queue state family out
of `MainWindowViewModel`. Brief, plan, prompt, gallery, review, and delivery
state remain outside this slice. The pre-existing scientific workflow already
uses dedicated state ViewModels and was not changed.

At handoff, the five `MainWindowViewModel` partials totalled 3,657 lines:

| File | Baseline lines |
| --- | ---: |
| `MainWindowViewModel.cs` | 2,250 |
| `MainWindowViewModel.GenerationReviewDelivery.cs` | 561 |
| `MainWindowViewModel.AsyncOperations.cs` | 267 |
| `MainWindowViewModel.Planning.cs` | 532 |
| `MainWindowViewModel.ProjectSelection.cs` | 47 |

The source-level public binding baseline was 199 explicit properties plus 29
RelayCommand-generated properties. The queue family accounted for 17 explicit
properties (localized labels, rows, selection, and derived presence) and seven
commands. This record distinguishes that historical public compatibility surface
from ownership: public facades can remain temporarily even after backing state and
commands have moved.

## Change

- Added `ImageSeriesWorkspaceViewModel`, which owns queue rows, selected task,
  queue localization, `HasQueueRows`, prepare/execute/pause/resume/retry/reorder
  commands, and command enablement.
- Kept `MainWindowOperationGate` as the exclusive mutation boundary. The typed
  queue mutation/reload bridge reloads the selected project and lets the shell
  refresh gallery/review/delivery projections after a queue mutation; it does not
  make a provider call on prepare or projection.
- Updated queue and prompt-editor XAML to bind through `ImageSeriesWorkspace`.
  Existing stable queue AutomationIds are unchanged.
- Added direct owner-driven queue workflow coverage and a parser-level XAML/UIA
  contract. The MainWindow queue facade returns the owner instances so old binding
  consumers keep their reference identity during this migration.

`MainWindowViewModel.cs` is now 2,186 lines and its generation/review/delivery
partial is 431 lines. The queue-specific fields and generated command methods no
longer live in the main window. The temporary facade preserves compatibility and
therefore intentionally does not claim a net reflection-public-member reduction
yet. Its removal condition is: migrate all direct non-XAML consumers to
`ImageSeriesWorkspace`, migrate the next image-series state family, and remove
the facade in the same validated compatibility cutover.

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
      "path": "docs/change-evidence/20260729-generation-queue-operator-controls.md",
      "revision": "repo-accepted-2026-07-29"
    }
  ],
  "observedBehavior": "The existing image-series queue requires observable selected-row state, explicit CanExecute refresh, stable native list selection and AutomationIds, fake-first prepare semantics, and one shell-wide exclusive mutation/reload boundary because execution also changes gallery and invalidates downstream projections.",
  "decision": "adapt",
  "affectedContract": "A stateful image-series workspace owns queue projection and commands. MainWindow retains global navigation, MainWindowOperationGate, and cross-workspace reload application; it must not regain queue fields or command implementations. Stable queue AutomationIds and selection semantics remain unchanged.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --no-restore --filter FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/ViewModels/ImageSeriesWorkspaceViewModel.cs,src/ContentDeliveryStudio.App/Views/QueueView.xaml,docs/change-evidence/20260803-image-series-queue-state-owner.md"
  ]
}
```

## Verification

| Stage | Command | Result |
| --- | --- | --- |
| Setup | `dotnet restore ContentDeliveryStudio.sln` | Exit 0; required only because this isolated worktree had no `project.assets.json`. |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore` | Exit 0; 0 warnings, 0 errors. |
| Focused behavior + XML/UIA | `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~MainWindowViewModelTests|FullyQualifiedName~Phase7AccessibilityContractTests"` | Exit 0; 39/39 passed. |
| Full | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1 -NoRestore` | Exit 0; 760/760 tests, reference decision, product-focus contract, and format passed. |
| Release | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore` | Exit 0; 760/760 tests and actual 84-file `win-x64` package verification passed; SHA-256 `798a5f6eb0ad55f8cf91549ffd8a60b80ea80a1f367c953dadbfd843c1de8998`. |

The focused test exercises prepare, pause, reorder, execute, resume, and reload
through `ImageSeriesWorkspace`, verifies the facade returns the same owner command
and rows, and preserves fake-first output behavior. The XML/UIA test verifies
owner bindings and `QueueExecuteButton`, `QueuePauseButton`, `QueueResumeButton`,
`QueueRetryButton`, `QueueMoveUpButton`, `QueueMoveDownButton`, and
`QueueTaskList` IDs.

## Boundary, Rollback, And Remaining Acceptance

This is repository-only proof. It is not an interactive WPF visual review,
Narrator/high-contrast/touch/pen/manual UIA acceptance, a named-hardware test, a
live-provider acceptance, or a release publication.

Rollback only this queue-owner source, queue XAML, focused tests, architecture
note, and this evidence record. Do not use Git rollback to delete generated user
workspace output, local SQLite state, or historical acceptance records.
