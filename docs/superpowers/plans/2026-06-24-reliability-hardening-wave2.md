# Reliability Hardening Wave 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or the stronger subagent workflow when available. Keep work on a non-`main` branch and verify each slice before claiming completion.

**Goal:** Harden `MainWindowViewModel` async/state behavior, make command concurrency explicit, split the file into partials, and narrow the repository rename-guard scan boundary without changing public contracts.

**Architecture:** Add one internal `MainWindowOperationGate` for `latest-wins` reads and `exclusive` mutations, route all main-window command paths through that helper, and keep the existing coordinators as the lower workflow boundary.

**Tech Stack:** .NET 10, WPF, xUnit, repository-local PowerShell verification scripts

**Status:** Closed. Implemented in `ab3e42d`; plan state reconciled on `2026-07-29` with fresh evidence in [20260729-reliability-hardening-wave2-closeout.md](../../change-evidence/20260729-reliability-hardening-wave2-closeout.md).

---

### Task 1: Lock Wave 2 Scope In Repo-Owned Docs

**Files:**
- Create: `docs/superpowers/specs/2026-06-24-reliability-hardening-wave2-design.md`
- Create: `docs/superpowers/plans/2026-06-24-reliability-hardening-wave2.md`

- [x] Record the wave2 scope, non-goals, acceptance criteria, and evidence path in repo-owned docs.
- [x] State the concurrency rule explicitly: read flows are `latest-wins`; mutating flows are `exclusive`.
- [x] Record that public contracts, bindings, schema, and delivery formats remain unchanged.

### Task 2: Add Guardrail Tests Before Refactor

**Files:**
- Modify: `tests/ContentDeliveryStudio.Tests/MainWindowViewModelTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/RenameCompatibilityGuardTests.cs`

- [x] Add focused coverage for rapid refresh/load stale-result protection.
- [x] Add coverage that mutating commands are blocked while another mutation is active.
- [x] Add coverage that mutation failure/cancellation does not apply partial UI state.
- [x] Add coverage that document-source import/browse cannot overwrite a newer source target.
- [x] Add or extend warmup and startup refresh resilience coverage if needed.
- [x] Add repository scan tests proving directory pruning ignores `.worktrees`, generated directories, and hidden root dot-directories.

### Task 3: Introduce Main Window Operation Gate And Partial Split

**Files:**
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowOperationGate.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.ProjectSelection.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.Planning.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.GenerationReviewDelivery.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.AsyncOperations.cs`
- Modify: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.cs`

- [x] Add `MainWindowOperationGate` with explicit `latest-wins` and `exclusive` policies.
- [x] Replace direct revision/cancellation bookkeeping in `MainWindowViewModel`.
- [x] Route startup refresh, project refresh, plan load, document-source follow-up refresh, provider-center reads, and gallery warmup through latest-wins lanes.
- [x] Route mutating command bodies through the exclusive gate.
- [x] Add explicit busy-state command blocking without changing command names or binding surface.
- [x] Split `MainWindowViewModel` into responsibility-focused partial files while keeping `MainWindowViewModel` as the XAML-facing type. The implementation retained shell/properties/localization in the main partial and extracted project selection, planning, generation/review/delivery, and async-operation responsibilities; no separate shell partial was needed.

### Task 4: Tighten Repository Guardrail Scan Boundary

**Files:**
- Modify: `tests/ContentDeliveryStudio.Tests/RenameCompatibilityGuardTests.cs`

- [x] Replace post-enumeration file filtering with directory pruning before file-content reads.
- [x] Ignore `.git`, `.worktrees`, `bin`, `obj`, `publish`, and hidden root dot-directories.
- [x] Keep the real-file allowlist semantics unchanged.

### Task 5: Verify And Close Out

**Files:**
- Modify as required by implementation

- [x] Run focused `MainWindowViewModelTests`.
- [x] Run focused `RenameCompatibilityGuardTests`.
- [x] Run full `dotnet test` through the canonical repository gate.
- [x] Run `.\scripts\verify-repo.ps1 -NoRestore`.
- [x] Run `.\scripts\preflight-release.ps1 -NoRestore`.
- [x] Confirm the worktree remains ready for review without public-contract drift.
