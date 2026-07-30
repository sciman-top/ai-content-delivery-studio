# Generation Queue Operator Controls Implementation Plan

**Goal:** Add explicit prepare, pause, resume, reorder, retry, and execute controls to the durable image-generation queue without automatic or unauthorized provider calls.

**Architecture:** Extend the existing `GenerationTask` aggregate and additive SQLite bootstrap, split queue preparation from execution in the application service, and drive a selectable WPF Queue command bar from authoritative reloads.

**Tech stack:** .NET 10, WPF, EF Core SQLite, CommunityToolkit.Mvvm, xUnit, repository PowerShell gates.

## Task 1: Extend The Durable Queue Contract

**Files:**

- Modify `src/ContentDeliveryStudio.Core/Projects/ProjectModel.cs`
- Modify `tests/ContentDeliveryStudio.Tests/GenerationTaskTests.cs`

**Acceptance criteria:**

- [x] `Paused` and its guarded transitions are additive and existing enum values do not change.
- [x] Queue positions are positive, reorderable, and independent of timestamps.
- [x] Retry provenance is immutable and terminal tasks remain immutable.

**Verification:** Run `GenerationTaskTests`.

## Task 2: Add Queue Preparation And Operator Mutations

**Files:**

- Modify `src/ContentDeliveryStudio.Application/Projects/GenerationWorkflowApplicationService.cs`
- Modify `src/ContentDeliveryStudio.Application/Projects/ProjectApplicationService.cs`
- Modify focused application tests

**Acceptance criteria:**

- [x] Prepare creates ordered queued tasks and makes zero provider calls.
- [x] Pause, resume, reorder, and retry persist exactly once and make zero provider calls.
- [x] Retry creates a new linked task and preserves the original terminal task.

**Verification:** Run `GenerationWorkflowApplicationServiceTests`.

## Task 3: Execute Prepared Work Safely

**Files:**

- Modify `src/ContentDeliveryStudio.Application/Projects/GenerationWorkflowApplicationService.cs`
- Modify project-load recovery and focused tests as required

**Acceptance criteria:**

- [x] Execute rejects non-fake providers before changing task state.
- [x] Queued tasks execute in durable order; paused tasks are skipped.
- [x] Cancellation leaves undispatched work queued and reload recovers only orphaned running work.

**Verification:** Run generation workflow and project workspace tests.

## Checkpoint 1

- [x] Core and application focused tests pass.
- [x] `dotnet build ContentDeliveryStudio.sln` succeeds.

## Task 4: Add Additive SQLite Compatibility

**Files:**

- Modify `src/ContentDeliveryStudio.Infrastructure/Persistence/Configurations/GenerationTaskConfiguration.cs`
- Modify `src/ContentDeliveryStudio.Infrastructure/Persistence/AppDatabaseInitializer.cs`
- Modify focused persistence tests

**Acceptance criteria:**

- [x] New databases contain nullable queue-position and retry-link columns.
- [x] Existing databases gain missing columns idempotently without table rebuild or data rewrite.
- [x] Historical and new tasks reload with correct ordering and provenance.

**Verification:** Run persistence recovery tests.

## Task 5: Build The WPF Operator Surface

**Files:**

- Modify `src/ContentDeliveryStudio.App/ViewModels/GenerationWorkflowCoordinator.cs`
- Modify `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.GenerationReviewDelivery.cs`
- Modify queue projection/localization models
- Modify `src/ContentDeliveryStudio.App/Views/QueueView.xaml`
- Modify `src/ContentDeliveryStudio.App/Views/QueueHeaderView.xaml`
- Modify `src/ContentDeliveryStudio.App/Views/QueueRowsListView.xaml`
- Modify focused WPF/ViewModel/XAML tests

**Acceptance criteria:**

- [x] Queue rows are selectable and expose accurate command eligibility.
- [x] The compatibility Run action and the separate Prepare, Execute, Pause, Resume, Retry, Move Up, and Move Down actions reload authoritative state after success.
- [x] English/Chinese labels, keyboard access, and stable AutomationIds are verified.

**Verification:** Run coordinator, ViewModel, localization, and XAML contract tests.

## Checkpoint 2

- [x] All focused operator queue tests pass.
- [x] The fake provider call-count boundary is proven.
- [x] The WPF Queue workflow works without a paid provider call.

## Task 6: Synchronize Truth And Evidence

**Files:**

- Modify `docs/TASKS.md`
- Modify `docs/ROADMAP.md`
- Modify `docs/ARCHITECTURE.md`
- Modify `docs/USER_GUIDE.md`
- Modify `docs/zh-CN/USER_GUIDE.md`
- Add supersession notes to the 2026-07-28 crash-safe queue spec, plan, and evidence
- Add `docs/change-evidence/20260729-generation-queue-operator-controls.md`

**Acceptance criteria:**

- [x] Current queue status distinguishes repo-side completion from live-provider authority.
- [x] Reference revisions, compatibility, verification, residual risks, and rollback are recorded.
- [x] The prior crash-safe queue and accepted scientific-figure evidence remain intact.
- [x] Architecture and bilingual user guidance describe the operator workflow, and historical queued-recovery wording is explicitly superseded rather than erased.

## Final Gate

Run in fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
2. focused and full `dotnet test`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format --verify-no-changes`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Final evidence checklist:

- [x] Build and focused/full tests pass in the required order.
- [x] Reference evidence and format verification pass.
- [x] Release preflight passes on the implementation state before final evidence closeout.
- [x] Five-axis review, secret/generated-artifact scan, compatibility review, and diff hygiene find no blocking issue.

Then review the final diff for secrets, generated artifacts, contract drift, and accidental inclusion of the user's independent `AGENTS.md` change. Do not push or perform a paid call.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| A control unexpectedly incurs provider cost | Only Execute calls a provider; all other operations have call-count tests. |
| Retry destroys failure history | Create a new task with `RetryOfTaskId`; never reopen the terminal record. |
| Prepared work is lost on restart | Preserve queued/paused tasks; recover only running tasks. |
| Reordering rewrites historical meaning | Store a dedicated nullable queue position and swap only active items. |
| Legacy SQLite cannot load the new model | Add nullable columns through idempotent `PRAGMA table_info` checks. |
| WPF state drifts after a command | Reload the authoritative project aggregate after every successful mutation. |
