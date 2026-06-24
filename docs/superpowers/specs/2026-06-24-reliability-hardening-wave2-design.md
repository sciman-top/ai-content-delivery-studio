# Reliability Hardening Wave 2

## Goal

Record the second repository-owned reliability hardening slice for AI Content Delivery Studio.

This wave focuses on `MainWindowViewModel` async/state consistency and one narrow repository-guardrail fix.

## Context

Wave 1 already hardened infrastructure, persistence, provider parsing, and the first layer of workbench async safety.

The remaining hot spot is the main window coordination surface:

- async command bodies still own too much cancellation and stale-result policy
- read and write flows are not expressed through one explicit concurrency model
- the class remains oversized and difficult to review
- repository rename guard scans still treat local worktrees and generated folders as source-of-truth input

## Stable Contracts

This wave keeps the following surfaces stable:

- WPF bindings and command names exposed by `MainWindowViewModel`
- provider contracts and current fake-first workflow boundaries
- persistence schema and SQLite semantics
- delivery manifest/package formats
- project load and workbench user-visible flow shape

No public API, schema, manifest, or provider contract changes are part of this slice.

## Concurrency Policy

The main window now uses two explicit internal policies:

- read-style flows are `latest-wins`
- mutating flows are `exclusive`

`latest-wins` applies to:

- startup project refresh
- later project refreshes
- project plan loads driven by selection changes or explicit reloads
- document-source follow-up refresh after import/browse
- gallery thumbnail warmup
- provider-center read-only refresh/health probes

`exclusive` applies to:

- create project
- fake planning and fake document planning
- document-source import
- create brief / prompt directions / design blueprints
- blueprint and prompt-direction promotion
- series / item / prompt creation
- generation / image edit / review
- final approval and delivery export

## Design

### Main Window Operation Gate

Add one internal helper in `src/ContentDeliveryStudio.App/ViewModels`:

- `MainWindowOperationGate`

Responsibilities:

- own the `latest-wins` lane registrations and cancellation sources
- own the single `exclusive` mutation lock
- expose the currently tracked background task for startup/load/warmup tests
- centralize suppression of expected background cancellation/failure behavior

Non-goals:

- replacing the existing workflow coordinators
- adding a new app/service layer
- introducing user-visible cancellation UI

### Main Window File Split

Keep `MainWindowViewModel` as the XAML-facing type, but split it into partial files by responsibility:

- shell/properties/localization
- project selection and refresh/load
- planning and document-source flows
- generation/review/delivery flows
- async operation tracking

### State Application Rules

- Selection-driven loads go through the gate and drop stale completions through cancellation, not local revision integers.
- Mutating commands receive gate-issued tokens instead of `CancellationToken.None`.
- Persistence-backed mutations reload authoritative project state once after the write succeeds.
- Runtime-only flows that are not persisted yet keep direct state application, but still run under the exclusive gate.
- Gallery warmup remains best-effort and background-only, but stale warmups are canceled by the gate and never mutate current UI state.

### Repository Guardrail Fix

`RenameCompatibilityGuardTests` must prune auxiliary/generated directories before reading file contents.

Ignored scan roots:

- `.git`
- `.worktrees`
- `bin`
- `obj`
- `publish`
- hidden dot-directories directly under the repository root

This is intentionally narrow:

- allowlist semantics for true repository files remain unchanged
- this is not a rename-policy broadening

## Acceptance Criteria

- `MainWindowViewModel` command entrypoints no longer use ad hoc revision counters or `CancellationToken.None`.
- latest-wins reads never let stale completions overwrite a newer selection or newer document-source target.
- overlapping mutating commands are blocked through explicit busy-state `CanExecute` changes.
- startup refresh remains non-throwing.
- gallery warmup cancellation remains best-effort and centrally suppressed.
- `RenameCompatibilityGuardTests` no longer fail because of local `.worktrees` or generated directories.
- no public contracts change.

## Verification

- focused `MainWindowViewModelTests`
- focused `RenameCompatibilityGuardTests`
- full `dotnet test`
- `.\scripts\verify-repo.ps1 -NoRestore`
- `.\scripts\preflight-release.ps1 -NoRestore`
