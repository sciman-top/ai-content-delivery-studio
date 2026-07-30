# Phase 7 Product Hardening Closeout Implementation Plan

**Goal:** Finish every repository-owned Phase 7 hardening task while preserving manual/live acceptance boundaries.

**Architecture:** Harden existing backup and publish seams, add bounded WPF composition, convert recorded benchmarks into enforceable contracts, and synchronize evidence-backed documentation truth.

**Tech stack:** .NET 10, WPF, `System.IO.Compression`, SHA-256, PowerShell, xUnit.

## Task 1: Harden Backup Archive Integrity

**Files:** backup Application contracts, Infrastructure service, focused tests.

- [x] Add schema version, normalized path, size, and SHA-256 manifest fields.
- [x] Create through a temporary archive and atomically replace only after success.
- [x] Preflight the complete restore before target mutation.
- [x] Reject duplicate, missing, extra, tampered, oversized, and unsafe entries.
- [x] Preserve safe-default exclusions and cancellation behavior.

## Task 2: Expose Safe Backup And Restore In WPF

**Files:** app composition, feature-owned picker/ViewModel/view, localization, UI tests.

- [x] Register `IBackupRestoreService` and conservative picker services.
- [x] Add localized backup/restore controls to the Workbench inspector.
- [x] Keep restore destination user-selected and never target live data automatically.
- [x] Add busy/error status, stable AutomationIds/names, and keyboard focus visuals.

## Task 3: Build And Verify A Distributable Windows ZIP

**Files:** `publish-app.ps1`, new verification script, script tests/fixtures as appropriate.

- [x] Emit portable file hashes and package metadata.
- [x] Create the distributable ZIP outside the publish directory.
- [x] Verify path safety, uniqueness, membership, sizes, hashes, and required files.
- [x] Make release preflight perform and clean an actual isolated publish/package verification.

## Task 4: Gate Large-Gallery Regressions

**Files:** benchmark test, gallery XAML/tests, performance review.

- [x] Add explicit, generous 1,000-row time and memory budgets.
- [x] Require cached revisit to improve materially over initial thumbnail generation.
- [x] Add AutomationId/name, selection, focus visual, and recycling-container contracts.
- [x] Keep real frame timing and low-memory device behavior in manual/live acceptance.

## Task 5: Close Automatable Accessibility Work

**Files:** WPF XAML, static/native probes, focused tests, evidence.

- [x] Prove system-brush/high-contrast-compatible focus and selection styling.
- [x] Prove minimum-size and PerMonitorV2/current-DPI layout invariants that are deterministic in-repo.
- [x] Audit principal workflow form controls for names, IDs, labels, and keyboard access.
- [x] Verify the packaged executable can launch and expose the expected UIA shell contract where host automation is available.
- [x] Keep Narrator, touch/pen, subjective scroll quality, and hardware low-memory explicitly open.

## Task 6: Synchronize Samples, Docs, And Evidence

**Files:** `TASKS`, `ROADMAP`, architecture, user guides, performance review, change evidence.

- [x] Confirm existing sample/trial assets remain the canonical examples.
- [x] Reconcile the no-open-checkbox task list with the Phase 7 roadmap status.
- [x] Record package/backup operator steps, limits, verification, rollback, and N/A fields.
- [x] Record repo-side completion separately from manual/live acceptance.

## Final Gate

Run in fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
2. focused tests, then `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format --verify-no-changes`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Then run five-axis review, secret/generated-output scan, compatibility/truth-boundary review, and diff hygiene. Do not call a paid provider, commit, push, or stage the user's independent `AGENTS.md` change.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Crafted ZIP mutates the target before failure | Validate all paths, manifest rows, sizes, hashes, conflicts, and limits before creating the target. |
| Release claim exceeds the artifact | Call it a verified distributable ZIP; do not claim MSI/MSIX install, signing, or Store readiness. |
| Performance budgets become host-flaky | Use broad regression ceilings and relative cache assertions; preserve measured report output. |
| Automated accessibility is called Narrator acceptance | Label UIA/static/native facts precisely and retain assistive-technology/hardware lanes as manual/live. |
| Existing dirty changes are overwritten | Inspect overlapping diffs before each edit and never reset, restore, stage, commit, or push unrelated work. |
