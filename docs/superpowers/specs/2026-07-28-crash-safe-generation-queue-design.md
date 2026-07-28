# Crash-Safe Generation Queue Design

## Goal

Make the existing fake-first generation queue durable enough that an application or machine interruption cannot leave queue work invisible or permanently `Running`.

## Current Truth

`GenerationQueue` currently returns only terminal results. `GenerationWorkflowApplicationService` persists every `GenerationTask` after the full provider run completes, so normal repository execution never records `Queued` or `Running`. The Queue workspace is populated from the in-memory result and is cleared after project reload.

Changing stale rows during project load alone would therefore repair synthetic data, not the real crash boundary. This slice must persist execution checkpoints before and after each provider call.

## Decision

Use durable checkpoints with fail-closed recovery:

1. Create and persist all tasks as `Queued` before dispatch.
2. Persist one task as `Running` immediately before its provider call.
3. Persist `Succeeded`, `Failed`, or `Cancelled` immediately after that call returns.
4. When a project is opened, reconcile an orphaned `Running` task to `Failed` and an orphaned `Queued` task to `Cancelled`, with an explicit recovery reason.
5. Never automatically replay an interrupted provider call.

The current application dispatches image generation with concurrency `1`; this slice preserves that behavior and checkpoints one work item at a time. Explicit retry or per-item recovery controls remain a future operator-workflow slice.

## Why Fail Closed

An image provider may have completed an unsafe POST even when the desktop process did not persist its response. Automatically replaying that request can duplicate cost or assets. A visible terminal recovery state is safer than at-least-once execution.

The existing `Run fake generation` action remains the explicit way to start a new run. This slice does not broaden real-provider approval or retry policy.

## Domain Contract

`GenerationTask` owns valid state transitions and a nullable persisted `ErrorMessage`:

- `Queued -> Running`
- `Queued -> Cancelled`
- `Running -> Succeeded | Failed | Cancelled`
- terminal states do not transition

Recovery uses the same guarded methods. Timestamps must advance monotonically, attempt count increments when a task enters `Running`, success clears the error, and failed/cancelled tasks retain an operator-visible reason.

Existing constructor callers and persisted rows remain compatible. Historical terminal rows load with a null error.

## Application And Persistence Flow

`GenerationWorkflowApplicationService` will:

- build the current request set;
- create matching durable tasks and save once;
- run each request through the existing `GenerationQueue` as a one-item batch;
- checkpoint `Running` before dispatch and the terminal result plus candidate image after dispatch;
- return the same `GenerationQueueRun` contract to existing callers.

`ProjectWorkspaceApplicationService.LoadProjectAsync` will reconcile unfinished tasks and save only when reconciliation changed the aggregate. Repository query methods remain side-effect free.

The existing SQLite bootstrap uses `EnsureCreated` plus additive, idempotent DDL. `AppDatabaseInitializer` will inspect `PRAGMA table_info('GenerationTasks')` and add the nullable `ErrorMessage` column only when absent. No table rebuild or destructive migration is permitted.

## WPF Projection

Project reload will build Queue rows from persisted tasks instead of returning an empty queue. The existing Queue table and bindings remain unchanged. Recovered rows show their terminal status and reason; successful rows recover their candidate asset path when available.

No new controls, dialogs, tabs, or provider commands are introduced.
The existing DataGrid/`ItemsSource` boundary remains the WPF presentation
contract; only the project-owned `QueueRows` projection is restored after load.

## External Reference Decision

- ComfyUI revision `806e092ed42772e4ce7abf44c97c50021cc4bd10` distinguishes queued, running, interrupted, failed, and cancelled work. Adopt only the explicit lifecycle separation.
- InvokeAI revision `68b90174aafebbbba45d14b049fb6852271c76a8` persists pending, in-progress, and terminal queue states. Adopt only the durable-state principle.
- EF Core documentation revision `c5931286c90444b8220b14d0c2420f1811b7d2df` confirms `EnsureCreated` does not update an existing schema and SQLite schema changes need explicit handling. Adapt with an additive column-presence check.
- Microsoft WPF Samples revision `ecd9529fb6941272eff1ee1e7e2554e3ecb2f1e4` demonstrates the direct DataGrid `ItemsSource` binding used by the existing shell. Retain that binding boundary and rebuild its queue-row collection from persisted project state; reject a new view, control, or UI framework abstraction for this slice.

No external source is copied. Automatic resumption and broad queue infrastructure are rejected for this bounded slice.

## Acceptance Criteria

- A generation task is persisted as `Queued` before any provider call and as `Running` before the matching call.
- Each completed call persists one terminal task; successful calls persist the matching candidate image.
- Opening a project converts orphaned `Running` to `Failed` and orphaned `Queued` to `Cancelled` without invoking a provider.
- Recovery reasons survive SQLite reload and appear in Queue rows.
- Existing terminal task data and fresh databases remain compatible; an existing database gains only nullable `GenerationTasks.ErrorMessage`.
- Provider mode remains fake-first and no live evidence is refreshed.
- Fixed gates pass in order: build, test, contract/invariant, hotspot.

## Dependencies And Write Set

Expected implementation files:

- `src/ContentDeliveryStudio.Core/Projects/ProjectModel.cs`
- `src/ContentDeliveryStudio.Application/Projects/GenerationWorkflowApplicationService.cs`
- `src/ContentDeliveryStudio.Application/Projects/ProjectWorkspaceApplicationService.cs`
- `src/ContentDeliveryStudio.Infrastructure/Persistence/AppDatabaseInitializer.cs`
- `src/ContentDeliveryStudio.App/ViewModels/ProjectWorkbenchProjectionCoordinator.cs`
- `src/ContentDeliveryStudio.App/ViewModels/ProjectWorkbenchStateCoordinator.cs`
- focused files under `tests/ContentDeliveryStudio.Tests/`
- current roadmap/task/evidence documentation

No `.env`, SQLite database, workspace, outputs, ZIP, live-provider artifact, or accepted scientific-figure artifact belongs in the write set.

## Verification And Rollback

Focused verification covers domain transitions, generation checkpoint ordering, SQLite upgrade/reload, recovery without provider dispatch, and Queue projection. Final verification is:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format --verify-no-changes`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Rollback reverts only this source, test, documentation, and evidence slice. The nullable column may remain on already-opened local databases because older binaries ignore it; no data rewrite is required.
