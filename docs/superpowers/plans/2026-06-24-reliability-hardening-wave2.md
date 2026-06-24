# Reliability Hardening Wave 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or the stronger subagent workflow when available. Keep work on a non-`main` branch and verify each slice before claiming completion.

**Goal:** Harden `MainWindowViewModel` async/state behavior, make command concurrency explicit, split the file into partials, and narrow the repository rename-guard scan boundary without changing public contracts.

**Architecture:** Add one internal `MainWindowOperationGate` for `latest-wins` reads and `exclusive` mutations, route all main-window command paths through that helper, and keep the existing coordinators as the lower workflow boundary.

**Tech Stack:** .NET 10, WPF, xUnit, repository-local PowerShell verification scripts

---

### Task 1: Lock Wave 2 Scope In Repo-Owned Docs

**Files:**
- Create: `docs/superpowers/specs/2026-06-24-reliability-hardening-wave2-design.md`
- Create: `docs/superpowers/plans/2026-06-24-reliability-hardening-wave2.md`

- [ ] Record the wave2 scope, non-goals, acceptance criteria, and evidence path in repo-owned docs.
- [ ] State the concurrency rule explicitly: read flows are `latest-wins`; mutating flows are `exclusive`.
- [ ] Record that public contracts, bindings, schema, and delivery formats remain unchanged.

### Task 2: Add Guardrail Tests Before Refactor

**Files:**
- Modify: `tests/ContentDeliveryStudio.Tests/MainWindowViewModelTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/RenameCompatibilityGuardTests.cs`

- [ ] Add focused coverage for rapid refresh/load stale-result protection.
- [ ] Add coverage that mutating commands are blocked while another mutation is active.
- [ ] Add coverage that mutation failure/cancellation does not apply partial UI state.
- [ ] Add coverage that document-source import/browse cannot overwrite a newer source target.
- [ ] Add or extend warmup and startup refresh resilience coverage if needed.
- [ ] Add repository scan tests proving directory pruning ignores `.worktrees`, generated directories, and hidden root dot-directories.

### Task 3: Introduce Main Window Operation Gate And Partial Split

**Files:**
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowOperationGate.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.ProjectSelection.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.Planning.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.GenerationReviewDelivery.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.AsyncOperations.cs`
- Create: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.Shell.cs`
- Modify: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.cs`

- [ ] Add `MainWindowOperationGate` with explicit `latest-wins` and `exclusive` policies.
- [ ] Replace direct revision/cancellation bookkeeping in `MainWindowViewModel`.
- [ ] Route startup refresh, project refresh, plan load, document-source follow-up refresh, provider-center reads, and gallery warmup through latest-wins lanes.
- [ ] Route mutating command bodies through the exclusive gate.
- [ ] Add explicit busy-state command blocking without changing command names or binding surface.
- [ ] Split `MainWindowViewModel` into responsibility-focused partial files while keeping `MainWindowViewModel` as the XAML-facing type.

### Task 4: Tighten Repository Guardrail Scan Boundary

**Files:**
- Modify: `tests/ContentDeliveryStudio.Tests/RenameCompatibilityGuardTests.cs`

- [ ] Replace post-enumeration file filtering with directory pruning before file-content reads.
- [ ] Ignore `.git`, `.worktrees`, `bin`, `obj`, `publish`, and hidden root dot-directories.
- [ ] Keep the real-file allowlist semantics unchanged.

### Task 5: Verify And Close Out

**Files:**
- Modify as required by implementation

- [ ] Run focused `MainWindowViewModelTests`.
- [ ] Run focused `RenameCompatibilityGuardTests`.
- [ ] Run full `dotnet test`.
- [ ] Run `.\scripts\verify-repo.ps1 -NoRestore`.
- [ ] Run `.\scripts\preflight-release.ps1 -NoRestore`.
- [ ] Confirm the branch remains ready for review without public-contract drift.
