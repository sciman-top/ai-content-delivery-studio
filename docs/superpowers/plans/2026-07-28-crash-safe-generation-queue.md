# Crash-Safe Generation Queue Implementation Plan

> Superseded recovery detail (2026-07-29): the completed
> [generation queue operator controls plan](./2026-07-29-generation-queue-operator-controls.md)
> preserves explicitly prepared `Queued` and `Paused` tasks across reload and
> recovers only orphaned `Running` tasks to `Failed`. This plan remains the
> historical implementation record for durable checkpoints; its earlier
> queued-to-cancelled reload rule is no longer current.

**Goal:** Persist generation queue checkpoints, fail closed after interruption, and restore Queue workspace evidence without automatically replaying provider calls.

**Architecture:** Domain-owned transitions feed application-level one-item checkpoints. Project opening reconciles incomplete tasks, SQLite bootstrap adds one nullable compatibility column, and the existing WPF Queue projection reads durable task state.

**Tech stack:** .NET 10, WPF, EF Core SQLite, xUnit, PowerShell repository gates.

## Task 1: Lock Domain State Transitions

**Files:**

- Modify `src/ContentDeliveryStudio.Core/Projects/ProjectModel.cs`
- Add or modify focused domain tests

**Acceptance criteria:**

- `GenerationTask` permits only the documented state transitions.
- Starting increments attempts; terminal transitions record stable timestamps and reasons.
- Terminal tasks reject further mutation.

**Verification:** Run the focused generation-task domain tests.

**Dependencies:** None.

## Task 2: Persist Execution Checkpoints

**Files:**

- Modify `src/ContentDeliveryStudio.Application/Projects/GenerationWorkflowApplicationService.cs`
- Modify `tests/ContentDeliveryStudio.Tests/GenerationWorkflowApplicationServiceTests.cs`

**Acceptance criteria:**

- All work is saved as `Queued` before provider dispatch.
- Each work item is saved as `Running` before dispatch and terminal after dispatch.
- Successful output remains linked to the durable task; failure and cancellation do not create candidates.

**Verification:** Run focused generation workflow tests, including a recording repository/provider ordering test.

**Dependencies:** Task 1.

## Checkpoint 1

- Focused domain and generation workflow tests pass.
- The solution builds.

## Task 3: Reconcile Interrupted Work On Project Open

**Files:**

- Modify `src/ContentDeliveryStudio.Application/Projects/ProjectWorkspaceApplicationService.cs`
- Modify focused project workspace/application tests

**Acceptance criteria:**

- Project open changes orphaned `Running` to `Failed` and `Queued` to `Cancelled`.
- The reconciliation is persisted once and invokes no provider.
- Projects containing only terminal work remain read-only during load.

**Verification:** Run focused project application tests.

**Dependencies:** Task 1.

## Task 4: Add SQLite Compatibility Column

**Files:**

- Modify `src/ContentDeliveryStudio.Infrastructure/Persistence/AppDatabaseInitializer.cs`
- Modify `tests/ContentDeliveryStudio.Tests/PersistenceTests.cs` or a focused initializer test file

**Acceptance criteria:**

- New databases contain nullable `GenerationTasks.ErrorMessage`.
- Existing databases gain the column idempotently without table rebuild or data loss.
- Historical terminal tasks reload with null error; recovered reasons survive reload.

**Verification:** Run focused persistence and initializer tests.

**Dependencies:** Task 1.

## Checkpoint 2

- Domain, application, and persistence focused tests pass.
- The solution builds with no new warning.

## Task 5: Restore Durable Queue Rows In WPF

**Files:**

- Modify `src/ContentDeliveryStudio.App/ViewModels/ProjectWorkbenchProjectionCoordinator.cs`
- Modify `src/ContentDeliveryStudio.App/ViewModels/ProjectWorkbenchStateCoordinator.cs`
- Modify focused coordinator/view-model tests

**Acceptance criteria:**

- Reloaded projects show persisted queue status, attempts, output path, and error reason.
- Existing bindings and Queue layout remain unchanged.
- A native WPF probe confirms the Queue surface remains usable after project reload.

**Verification:** Run focused coordinator tests, then perform a fake-first native WPF/UI Automation probe.

**Dependencies:** Tasks 2-4.

## Task 6: Close Documentation And Evidence

**Files:**

- Modify `docs/ROADMAP.md` and `docs/TASKS.md` only to record this bounded hardening result
- Add `docs/change-evidence/20260728-crash-safe-generation-queue.md`

**Acceptance criteria:**

- Documentation distinguishes this repo-side automated proof from live-provider evidence.
- External revisions, adoption decisions, commands, exit codes, compatibility, and rollback are recorded.
- Existing accepted Tasks 1-30, Checkpoints 0-5, operator evidence, and live evidence remain unchanged.

**Verification:** Review the diff and run the reference evidence contract.

**Dependencies:** Tasks 1-5.

## Final Gate

Run in fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format --verify-no-changes`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Then review staged diff, scan for secrets/generated artifacts, and create one local commit. Do not push.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Provider completed but terminal checkpoint was lost | Mark the orphaned task failed and require an explicit new run; never replay automatically. |
| Cancellation prevents final persistence | Reconciliation on the next project open closes any remaining queued/running task. |
| Existing SQLite database lacks the new column | Inspect table metadata and execute one additive nullable-column statement only when absent. |
| Queue UI drifts from persisted truth | Build Queue rows from the loaded aggregate and cover reload with coordinator tests. |
| Scope expands into a queue manager | Defer per-item retry, resume, pause, reorder, and real-provider dispatch to a separate approved slice. |
