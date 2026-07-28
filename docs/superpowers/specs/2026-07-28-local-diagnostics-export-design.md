# Local Diagnostics Export Design

**Status:** Approved for implementation
**Date:** 2026-07-28
**Scope:** User-visible, local-only diagnostics export from the native WPF inspector

## Problem

The repository already contains a redacting `DiagnosticsPackageWriter`, but the desktop application does not register it or expose an operator path to use it. Support evidence therefore depends on test-only or developer-authored calls. Phase 7 lists structured logs and diagnostics as future work, yet the smallest useful product slice is not a logging subsystem: it is a local export command that captures the current application, machine, project, provider-capability, and secret-presence state without calling a provider or revealing credentials.

## Decision

Add a Diagnostics panel to the existing Workbench Inspector. The operator selects a parent directory and explicitly exports a uniquely named diagnostics directory containing `diagnostics.json` and `diagnostics.md`.

The export is local-only and append-only:

1. The application creates `content-delivery-studio-diagnostics-YYYYMMDD-HHmmss-fff` beneath the selected parent directory.
2. An application service lists and loads projects through `IProjectRepository`, maps them to `DiagnosticsProjectSnapshot`, and delegates serialization to `IDiagnosticsPackageWriter`.
3. A desktop snapshot factory maps the providers actually resolved by dependency injection to capability records. It also maps provider configuration to generic secret-presence records such as text-key pool configured and image-key pool configured. It never reads or emits secret values or secret names.
4. The panel reports the completed directory or a bounded error message. It does not open, upload, email, or otherwise transmit the result.

## User Experience

The panel appears after Provider Center in the right-side inspector and contains:

- a concise title and privacy summary;
- a read-only output-parent path;
- a Browse button using the native WPF `OpenFolderDialog`;
- an Export button disabled while an export is running or while no directory is selected;
- a wrapping status line with a stable AutomationId.

The panel follows the existing compact inspector styling. Stable AutomationIds cover the path field, Browse button, Export button, and status text so native WPF automation can verify the workflow.

## Truth And Safety Boundaries

- The export proves a repo-side capability and can receive an `authorized_agent` native operator acceptance under the user's approved acceptance policy. Evidence must retain the actual actor type and must not relabel it as human execution.
- Automated tests remain automated evidence; they do not independently prove native interaction.
- No paid or live provider call occurs. Provider health checks are out of scope.
- Secret values, secret identifiers, `.env` contents, SQLite files, workspace files, generated assets, outputs, and delivery ZIPs are not copied into the package.
- Existing Tasks 1-30, Checkpoints 0-5, live accepted evidence, and accepted scientific-figure artifacts remain unchanged.
- This slice does not claim that Phase 7 structured logging, backup/restore, installer, accessibility, or large-gallery work is complete.

## Architecture

### Application

`DiagnosticsPackageApplicationService` owns project discovery and export orchestration. Its request accepts already-redacted application, machine, provider, and secret-presence snapshots. This keeps WPF and provider configuration types out of the application layer while ensuring project data comes from the repository truth.

### Desktop

`DesktopDiagnosticsSnapshotFactory` maps the resolved text planning, image generation, image edit, and vision review providers. Duplicate provider identities are merged and capability names are deterministic. Fake providers are marked `realApiEnabled=false` and `dryRunOnly=true`; non-fake registered providers are represented as live-capable without making a call.

The factory uses `IProviderCenterConfigurationService` only for counts and app-credential booleans. Its secret snapshots use generic names and booleans, never configured environment variable names.

`DiagnosticsPanelViewModel` coordinates folder selection, snapshot creation, export, localization, busy state, and visible status. `IDiagnosticsDirectoryPickerService` wraps `Microsoft.Win32.OpenFolderDialog` so the ViewModel remains testable.

### Infrastructure

The existing `DiagnosticsPackageWriter` remains the serialization authority and is registered as `IDiagnosticsPackageWriter`. No format change is required.

## Reference Basis

- WPF common-dialog guidance: `02-dotnet-wpf/docs-desktop/dotnet-desktop-guide/wpf/windows/how-to-open-common-system-dialog-box.md` at `3ed3bb19178883827aa0a81427576797db141862`; adopted for an explicit desktop dialog boundary.
- WPF common-dialog sample: `02-dotnet-wpf/WPF-Samples/Windows/CommonDialog/MainWindowViewModel.cs` at `ecd9529fb6941272eff1ee1e7e2554e3ecb2f1e4`; adapted behind a picker service for testability.
- `dotnet-extensions` at `c6bf2e02eeda9bc67003d509db59ad8f887f1c47` and `opentelemetry-dotnet` at `2eeede10c1bb7b87f3f6f70264a804d8ee9de948` remain reference context for host/observability boundaries; neither is copied because this slice exports existing snapshots rather than introducing a telemetry pipeline.

## Acceptance Criteria

- The desktop host resolves the diagnostics writer, application service, snapshot factory, picker, panel ViewModel, and main window without DI failure.
- Selecting a directory and exporting creates one unique child directory containing both diagnostics files.
- Project counts reflect repository state; provider capabilities reflect resolved providers.
- Exported JSON and Markdown contain no configured secret value or configured secret identifier.
- Cancellation and failures leave the UI usable and expose a bounded status without crashing the app.
- English and Simplified Chinese labels refresh with the application language.
- A fake-first native WPF probe completes Browse -> Export and verifies both files plus the redaction boundary.
- Fixed gates pass in order: build, test, contract/invariant, hotspot.

## Dependencies And Write Set

Expected files are limited to diagnostics application/desktop services, one inspector panel and ViewModel, DI and localization wiring, focused tests, this spec/plan, roadmap/task status, and one new change-evidence record.

No `.env`, credential material, SQLite database, `workspace/`, `outputs/`, generated asset, diagnostics output directory, or ZIP belongs in Git.

## Rollback

Revert only this source, test, documentation, and evidence slice. Exported diagnostics directories are user-selected local files outside repository truth and are not removed by Git rollback.
