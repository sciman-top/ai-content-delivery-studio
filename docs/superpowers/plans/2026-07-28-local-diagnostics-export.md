# Local Diagnostics Export Implementation Plan

**Goal:** Expose the existing redacting diagnostics writer through a local-only native WPF export workflow.

**Architecture:** An application service reads project truth and calls the existing writer. Desktop services supply already-redacted runtime snapshots and a native folder picker. A focused inspector panel exposes the workflow without network or provider calls.

**Tech stack:** .NET 10, WPF, CommunityToolkit.Mvvm, EF Core SQLite repository abstraction, xUnit, PowerShell repository gates.

## Task 1: Add Application Export Orchestration

**Files:**

- Add `src/ContentDeliveryStudio.Application/Diagnostics/DiagnosticsPackageApplicationService.cs`
- Add focused application tests

**Acceptance criteria:**

- The service lists and loads every project from `IProjectRepository` without mutation.
- Missing projects between list and load are skipped safely.
- The service creates a unique child directory and delegates a complete redacted request to `IDiagnosticsPackageWriter`.

**Verification:** Run focused diagnostics application-service tests.

## Task 2: Build Desktop Runtime Snapshots

**Files:**

- Add `src/ContentDeliveryStudio.App/Services/DesktopDiagnosticsSnapshotFactory.cs`
- Add focused snapshot-factory tests

**Acceptance criteria:**

- Resolved provider capabilities are represented deterministically and duplicate provider identities are merged.
- Generic secret-presence booleans derive only from configuration counts/app-credential flags.
- No secret value, `.env` content, health check, or network call is used.

**Verification:** Run focused snapshot-factory tests in fake and configured-snapshot modes.

## Checkpoint 1

- Application and snapshot-factory tests pass.
- The solution builds without a new warning.

## Task 3: Add Native Folder Picker And Panel ViewModel

**Files:**

- Add `src/ContentDeliveryStudio.App/Services/DiagnosticsDirectoryPickerService.cs`
- Add `src/ContentDeliveryStudio.App/ViewModels/DiagnosticsPanelViewModel.cs`
- Modify localization resources
- Add focused ViewModel/localization tests

**Acceptance criteria:**

- Browse updates the output parent path when the operator confirms a directory.
- Export is disabled without a path and while busy.
- Success, cancellation, and failure leave a localized, bounded status and restore command availability.

**Verification:** Run focused ViewModel and localization tests.

## Task 4: Wire The Inspector And Desktop Host

**Files:**

- Add `src/ContentDeliveryStudio.App/Views/DiagnosticsPanelView.xaml` and code-behind
- Modify `WorkbenchInspectorView.xaml`
- Modify `MainWindowViewModel.cs`
- Modify `App.xaml.cs`
- Add layout and DI contract tests

**Acceptance criteria:**

- The panel appears after Provider Center and binds through `MainWindowViewModel.Diagnostics`.
- Stable AutomationIds exist for output path, Browse, Export, and status.
- Desktop DI resolves the complete main-window graph in fake-first mode.

**Verification:** Build, run focused layout/DI tests, then run a native WPF fake-first probe.

## Checkpoint 2

- Focused tests pass.
- Native Browse -> Export creates `diagnostics.json` and `diagnostics.md`.
- Visual inspection confirms no clipping, overlap, or incoherent state in the inspector.
- The exported files contain no seeded sentinel secret or configured secret identifier.

## Task 5: Close Documentation And Evidence

**Files:**

- Modify `docs/ROADMAP.md` and `docs/TASKS.md` only for this bounded Phase 7 result
- Add `docs/change-evidence/20260728-local-diagnostics-export.md`

**Acceptance criteria:**

- Documentation distinguishes repo-side completion, automated proof, `authorized_agent` native acceptance, live accepted evidence, and still-open Future Trigger Lanes.
- Reference revisions, adoption decisions, commands, exit codes, compatibility, and rollback are recorded.
- Existing accepted Tasks 1-30, Checkpoints 0-5, live evidence, and accepted artifacts remain unchanged.

**Verification:** Review the diff and run the reference evidence contract.

## Final Gate

Run in fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format --verify-no-changes`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Then review the staged diff, scan for secrets and generated artifacts, and create one local commit. Do not stage `AGENTS.md` and do not push.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Diagnostics leaks configured secret identifiers or values | Export only generic presence booleans and cover sentinel values/names with negative assertions. |
| Provider snapshot drifts from the running host | Build it from providers resolved by DI, not static documentation. |
| Export mutates project state | Use repository list/load only and assert no save call in tests. |
| Multiple exports overwrite evidence | Create a unique timestamped child directory and reject collisions by suffixing a unique token if needed. |
| UI becomes unusable after an exception | Restore busy state in `finally` and expose a bounded localized failure status. |
| Scope expands into telemetry or support upload | Keep structured-log ingestion, archive creation, network transfer, and health checks deferred. |
