# Crash-Safe Generation Queue Evidence

Date: 2026-07-28

> Supersession note (2026-07-29):
> [Generation Queue Operator Controls Evidence](./20260729-generation-queue-operator-controls.md)
> replaces only the reload treatment of incomplete queued work. Explicitly
> prepared `Queued` and `Paused` tasks now survive reload, while orphaned
> `Running` tasks still fail closed without replay. The checkpoint, persistence,
> native-probe, and gate results below remain historical evidence for the
> 2026-07-28 slice.

## Status

`repo-side reliability hardening complete / agent-operated WPF evidence recorded`

This is a bounded post-acceptance reliability slice. It does not reopen or
rewrite accepted scientific-figure Tasks 1-30, Checkpoints 0-5, operator
acceptance, or live-provider evidence.

## Product Boundary

Generation work now persists its real execution boundary:

- all work is saved as `Queued` before dispatch;
- one task is saved as `Running` before its provider call;
- each result is saved as `Succeeded`, `Failed`, or `Cancelled` after the call;
- project load reconciles orphaned `Running` to `Failed` and orphaned `Queued`
  to `Cancelled`, with a persisted reason;
- interrupted provider calls are never replayed automatically.

The existing explicit fake-generation command remains the only restart path.
Per-item retry, resume, pause, reorder, and live-provider policy remain future
trigger lanes.

## Reference Basis And Decision

- Route: `image-workflow` and `persistence-and-schema`
- ComfyUI revision: `806e092ed42772e4ce7abf44c97c50021cc4bd10`
- InvokeAI revision: `68b90174aafebbbba45d14b049fb6852271c76a8`
- EF Core documentation revision:
  `c5931286c90444b8220b14d0c2420f1811b7d2df`
- Microsoft WPF Samples revision:
  `ecd9529fb6941272eff1ee1e7e2554e3ecb2f1e4`
- Decision: adapt explicit queued/running/terminal lifecycle separation and
  additive SQLite upgrade handling, and retain the existing direct DataGrid
  `ItemsSource` projection boundary; reject automatic provider replay, broad
  queue-manager infrastructure, and a new WPF view/control abstraction.

No external source was copied and no new runtime dependency was added.

## Implementation Evidence

- `GenerationTask` owns guarded transitions, monotonic timestamps, attempt
  counting, terminal immutability, and persisted failure/cancellation reasons.
- `GenerationWorkflowApplicationService` checkpoints each fake-first request,
  preserves request order through unique durable timestamps, and rejects
  inconsistent success/image result pairs.
- `ProjectWorkspaceApplicationService` performs fail-closed reconciliation and
  saves only when recovery changed the aggregate.
- `AppDatabaseInitializer` inspects `PRAGMA table_info('GenerationTasks')` and
  adds only nullable `ErrorMessage` when an existing database lacks it.
- WPF Queue projection rebuilds rows from persisted tasks and associates
  successful tasks with their candidate asset paths.

Focused compiled verification:

`dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-restore --filter "FullyQualifiedName~GenerationTaskTests|FullyQualifiedName~GenerationWorkflowApplicationServiceTests|FullyQualifiedName~ProjectWorkspaceApplicationServiceTests|FullyQualifiedName~ProjectWorkbenchStateCoordinatorTests|FullyQualifiedName~GenerationQueuePersistenceRecoveryTests"`

Result: exit `0`; `17 / 17` passed.

## Agent-Operated Native WPF Probe

The probe used fake providers and the isolated data root:

`C:\Users\sciman\AppData\Local\Temp\ContentDeliveryStudio.CodexQueueProbe-019fa8f6`

UI Automation performed the following user-visible flow:

1. Created an isolated project and generated a three-item fake plan.
2. Ran fake generation and opened Queue.
3. Confirmed three `Succeeded` rows with attempt `1` and three distinct output
   pairs: `001-Image 01`, `002-Image 02`, and `003-Image 03`.
4. Closed the application, restarted it with the same data root, and allowed the
   project to load from SQLite.
5. Opened Queue again and confirmed the same three terminal rows, attempts, and
   output paths were restored.

The first reload exposed unstable row ordering because same-batch tasks shared
one timestamp and EF collection order is not stable. The implementation now
persists monotonic per-request timestamps and globally orders Queue rows by that
durable value; focused regression coverage was added. Multi-display coordinate
drift caused one discarded click outside the target window; it did not mutate
the WPF project and is not counted as evidence.

This is truthful `agent-operated WPF evidence`. It is not a paid-provider call,
does not refresh `live accepted`, and does not modify an accepted artifact.

## Compatibility And Risk

- Existing terminal rows load with a null error reason.
- Existing databases receive one additive nullable column; no table rebuild or
  data rewrite occurs.
- Older binaries ignore the added column, so rollback does not require a
  destructive schema downgrade.
- Two application processes racing the first legacy-database upgrade could both
  observe the missing column before one succeeds. The current Windows desktop
  runtime is operated as one instance; cross-process migration coordination is
  outside this slice and remains a documented residual risk.
- Provider selection remains fake-first. No `.env`, SQLite database, workspace,
  output, screenshot, generated image, or ZIP enters Git.

## Fixed-Order Verification

The final tree is verified in the repository-required order:

1. `dotnet build ContentDeliveryStudio.sln`
   - exit `0`; `0` warnings; `0` errors
2. `dotnet test ContentDeliveryStudio.sln --no-build`
   - exit `0`; `690 / 690` passed
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
   - exit `0`; `persistence-and-schema` and
     `workflow-and-ux-architecture` evidence passed
4. `dotnet format --verify-no-changes`
   - exit `0`; no formatting changes required
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`
   - exit `0`; nested build `0 / 0`, tests `690 / 690`, format, reference
     contract, publish WhatIf, placeholder/conflict scans, and cached/uncached
     diff hygiene passed

The same fixed order is rerun after this result block is present on the final
tree and before the implementation commit is created.

## Review And Rollback

Five-axis review covered transition correctness, cancellation, missing/extra
image invariants, deterministic reload ordering, SQLite compatibility,
credential boundaries, and bounded I/O. The unstable reload ordering and
inconsistent terminal-image fallback were fixed before closeout.

Rollback reverts only this slice's source, tests, roadmap/task entries, spec,
plan, and evidence. It must not alter the user's `AGENTS.md`, ignored runtime
data, accepted scientific-figure artifacts, operator evidence, or live evidence.
