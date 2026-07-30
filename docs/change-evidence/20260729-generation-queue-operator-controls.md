# Generation Queue Operator Controls Evidence

Date: `2026-07-29`

## Status And Boundary

`repo-side implementation complete / fake-first automated acceptance passed / native WPF shell observed`

This slice does not authorize a paid provider call, background worker, automatic replay, or live acceptance refresh. The compatibility one-click route and the explicit Queue Execute route remain fake-only.

## Reference Basis

| Area | Revision | Decision |
| --- | --- | --- |
| ComfyUI | `806e092` | Adapt pending/running separation; reject remote server queue architecture. |
| InvokeAI | `68b9017` | Adapt durable queue and explicit retry concepts; reject its broader runtime model. |
| EF Core docs | `c593128` | Preserve repo-owned additive schema inspection because `EnsureCreated` does not update existing databases. |
| WPF Samples | `ecd9529` | Reuse command, selection, and UI Automation patterns through the existing MVVM/WPF stack. |

No external source was copied and no dependency was added.

## Implementation Evidence

- `GenerationTaskStatus.Paused` is appended without renumbering existing statuses.
- `GenerationTask` owns guarded pause/resume/reorder transitions, positive nullable `QueuePosition`, immutable nullable `RetryOfTaskId`, and terminal immutability.
- Prepare, pause, resume, move, and retry persist local state without provider calls.
- Retry creates a new queued task linked to the failed/cancelled task; it does not reopen or erase terminal history.
- Execute rejects a non-fake provider before state mutation, runs queued tasks in operator order, skips paused tasks, and preserves per-item Running/terminal checkpoints.
- Undispatched queued work survives cancellation and reload; only orphaned Running work recovers to Failed.
- Durable queue positions also drive generated filename prefixes, so executing
  around paused work in separate runs cannot reuse a local run index and
  overwrite an earlier same-title output.
- Invalid boundary moves validate the target before legacy null positions are
  normalized, so a rejected move cannot leave in-memory queue mutations behind.
- SQLite initialization adds nullable `QueuePosition` and `RetryOfTaskId` idempotently alongside the existing `ErrorMessage` compatibility column.
- The original one-click fake generation command remains compatible. A separate Prepare command enters the operator-controlled two-stage flow.
- Queue rows are selectable and the command bar exposes localized Execute, Pause, Resume, Retry, Move Up, and Move Down controls with stable AutomationIds.

## Automated Verification

Focused command:

`dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter "FullyQualifiedName~GenerationTaskTests|FullyQualifiedName~GenerationWorkflowApplicationServiceTests|FullyQualifiedName~ProjectWorkspaceApplicationServiceTests|FullyQualifiedName~GenerationQueuePersistenceRecoveryTests|FullyQualifiedName~QueueOperatorWorkflow_PreparesPausesReordersResumesAndExecutes|FullyQualifiedName~MainWindowLayoutTests|FullyQualifiedName~LocalizationTests|FullyQualifiedName~MainWindowLocalizationCoordinatorTests"`

Result: exit `0`; `63 / 63` passed.

Final full regression: exit `0`; `710 / 710` passed.

Provider-boundary coverage proves:

- prepare and all local mutations make `0` provider calls;
- a linked retry makes `0` provider calls;
- Execute calls only for queued tasks and skips paused tasks;
- the compatibility route rejects a non-fake provider before creating tasks.

## Native WPF Probe

The probe launched the real Debug `ContentDeliveryStudio.App.exe` with an isolated `CONTENT_DELIVERY_STUDIO_DATA_ROOT` and `PROVIDER_MODE=fake`, selected the localized Queue tab through Microsoft UI Automation, and inspected the rendered command bar by AutomationId.

Observed controls:

| AutomationId | Accessible name | Control type | Empty-queue state |
| --- | --- | --- | --- |
| `QueueExecuteButton` | `执行队列` | Button | Disabled |
| `QueuePauseButton` | `暂停` | Button | Disabled |
| `QueueResumeButton` | `恢复` | Button | Disabled |
| `QueueRetryButton` | `重试` | Button | Disabled |
| `QueueMoveUpButton` | `上移` | Button | Disabled |
| `QueueMoveDownButton` | `下移` | Button | Disabled |

The isolated process closed normally and its temporary data root was deleted. The local ignored screenshot is `outputs/operator-queue-controls/20260729/queue-toolbar.png`; it shows complete labels, stable column widths, and no overlap at the observed desktop DPI.

## Final Fixed-Order Gate

| Stage | Command | Result |
| --- | --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln` | Exit `0`; `0` warnings, `0` errors |
| Test | focused suite and `dotnet test ContentDeliveryStudio.sln --no-build` | Exit `0`; `63 / 63` focused and `710 / 710` full passed |
| Contract/invariant | reference evidence and `dotnet format --verify-no-changes` | Both exit `0`; enforced reference areas and formatting passed |
| Hotspot | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore` | Exit `0`; release preflight passed, including `710 / 710` tests and publish WhatIf |

## Five-Axis Review And Hygiene

- Correctness: transition, ordering, cancellation/reload, retry, persistence,
  command eligibility, and native-shell evidence were reviewed. The review found
  and fixed durable-filename reuse across split executions and pre-validation
  mutation of legacy null positions; both fixes have red-green regression tests.
- Readability and architecture: queue policy remains in the application service,
  state transitions remain domain-owned, WPF reloads authoritative state, and
  no background worker, shared runtime abstraction, or dependency was added.
- Security and authority: changed/untracked task files contain no credential-like
  value; local mutations have zero-call coverage and both Execute entry points
  enforce the fake-provider boundary before task mutation.
- Performance: execution stays bounded, local, single-process, and sequential;
  the slice adds no polling, concurrency, remote queue, or unbounded background
  work.
- Git hygiene: `git diff --check` passes, build/publish/workspace outputs are not
  tracked, the native screenshot remains ignored under `outputs/`, the staging
  area is empty, and the user's independent `AGENTS.md` modification remains
  outside this slice.

## Compatibility, Residual Risk, And Rollback

- Existing enum values, WPF command names, one-click fake flow, provider contracts, and delivery formats remain compatible.
- Legacy databases receive two nullable columns without table rebuild or historical-row rewrite; missing historical values load as null.
- Prepared queues deliberately replace the old behavior that cancelled every orphaned queued task on load. This is the approved operator-workflow semantic change.
- Queue execution remains single-process and sequential. Cross-process coordination and background execution remain outside this slice.
- Rollback reverts this source/test/docs slice. Added nullable columns may remain because older binaries ignore them; no destructive downgrade is required.
