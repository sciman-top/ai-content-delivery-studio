# Generation Queue Operator Controls

**Status:** Approved by the user on `2026-07-29` for autonomous implementation.

## Goal

Turn the crash-safe generation checkpoint list into a usable, explicit operator queue without automatically replaying provider calls or weakening fake-first/live-approval boundaries.

The bounded workflow is:

`Prepare -> Pause/Resume/Reorder/Retry -> Execute -> durable terminal evidence`

## Product Boundary

This slice adds per-item controls for image-generation tasks only:

- prepare the latest prompt for each eligible series item as durable `Queued` work;
- pause and resume work before provider dispatch;
- move queued or paused work up and down;
- retry a failed or cancelled task by creating a new linked task;
- explicitly execute currently queued work in durable order.

It does not add background processing, concurrent operator mutation during a provider call, automatic retry, automatic resume, real-provider approval, scheduling, remote queue workers, or cross-process coordination.

## Cost And Authority Contract

- `Prepare`, `Pause`, `Resume`, `Move Up`, `Move Down`, and `Retry` make local persistence changes only and must not call a provider.
- `Execute` is the only queue-control command that can call an image provider.
- The current application service remains fake-only. A non-fake provider fails before any task enters `Running`.
- A future live-provider execution path must add an explicit approval receipt and cost summary; this slice does not infer that authority from a queued task.
- Retry never executes automatically. It creates a new `Queued` task linked by `RetryOfTaskId`, preserving the immutable failed/cancelled record.

## Domain Contract

Append `Paused` to `GenerationTaskStatus` without renumbering existing values.

Valid transitions:

- `Queued -> Running`
- `Queued -> Paused`
- `Queued -> Cancelled`
- `Paused -> Queued`
- `Paused -> Cancelled`
- `Running -> Succeeded | Failed | Cancelled`
- terminal tasks remain immutable

Each task gains:

- nullable `QueuePosition` for durable operator ordering;
- nullable `RetryOfTaskId` for retry provenance.

New tasks always receive a unique positive position. Historical tasks with no position remain readable and sort by `CreatedAt`, then `Id`. Reordering swaps two active tasks' positions; it never rewrites timestamps or terminal history.

## Application Flow

### Prepare

Load the project, reject preparation while any `Running` task exists, create one task for each item with a current prompt, assign increasing positions after the current maximum, persist once, and return without provider dispatch.

To prevent accidental duplicate cost, preparation is rejected while any `Queued` or `Paused` task already exists. Terminal history does not block a new prepared batch.

### Operator Mutations

Pause, resume, and reorder load the project, validate the selected task and transition, mutate only the aggregate, and persist once. Reordering considers active `Queued` and `Paused` tasks together and cannot move beyond the first or last active item.

Retry accepts only `Failed` or `Cancelled`, resolves the original item/prompt/provider references, creates a new linked `Queued` task at the end, and persists once. A succeeded task cannot be retried through this control.

### Execute

Load the project and fail before mutation unless the provider is registered and fake. Select `Queued` tasks by durable position, reconstruct each request from its stored item and prompt references, checkpoint `Running`, call the provider, then checkpoint the terminal result and candidate image.

Paused tasks are skipped. Cancellation leaves undispatched tasks `Queued` so the operator can resume explicitly; it never silently turns prepared work into terminal history. An orphaned `Running` task still recovers to `Failed` on project load. Prepared `Queued` and `Paused` tasks survive reload.

## Persistence

Extend `GenerationTasks` additively with nullable columns:

- `QueuePosition INTEGER NULL`
- `RetryOfTaskId TEXT NULL`

`AppDatabaseInitializer` must inspect `PRAGMA table_info('GenerationTasks')` and add each missing column idempotently. No table rebuild, destructive migration, or historical-row rewrite is permitted.

## WPF Interaction

- The existing `Run fake generation` action remains a compatibility shortcut for prepare plus execute.
- A separate prompt-editor `Prepare fake queue` action enters the operator-controlled two-stage workflow.
- The Queue workspace gains a compact command bar for `Execute`, `Pause`, `Resume`, `Retry`, `Move Up`, and `Move Down`.
- Queue rows become selectable and carry stable task IDs plus boolean eligibility used by command `CanExecute` logic.
- All controls receive stable AutomationIds and localized English/Chinese labels.
- After each mutation or execution, the ViewModel reloads the authoritative project aggregate; it does not patch row state optimistically.

## Reference Decision

- ComfyUI `806e092`: retain explicit pending/running queue separation; reject its remote/server queue architecture.
- InvokeAI `68b9017`: retain durable queue-state and explicit retry concepts; reject its broader invocation/runtime model.
- EF Core docs `c593128`: `EnsureCreated` does not update an existing schema; keep repo-owned additive column inspection.
- WPF Samples `ecd9529`: retain command-bound controls, selection, and stable UI Automation identity using existing WPF/MVVM patterns.

No external source is copied and no dependency is added.

## Acceptance Criteria

- Prepare persists ordered work and performs zero provider calls.
- Pause/resume/reorder persist and survive reload without provider calls.
- Retry preserves terminal history and creates a linked queued task without provider calls.
- Execute processes only queued work in operator order and skips paused work.
- Cancellation leaves undispatched prepared work queued; reload fails closed only for orphaned running work.
- Legacy SQLite databases gain only the two nullable columns and preserve historical tasks.
- Queue controls are localized, keyboard reachable, AutomationId-addressable, and backed by command eligibility tests.
- No paid call or live-acceptance refresh occurs.

## Verification And Rollback

Focused verification covers domain transitions, preparation/cost boundaries, ordering, retry provenance, cancellation/reload, SQLite upgrade, projection, localization, XAML contracts, and ViewModel commands.

Final verification follows `build -> test -> contract/invariant -> hotspot`.

Rollback reverts only this slice. Added nullable columns may remain because older binaries ignore them; no destructive downgrade is required.
