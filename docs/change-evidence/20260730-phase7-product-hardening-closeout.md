# Phase 7 Product Hardening Closeout Evidence

## Scope And Truth Boundary

- Date: `2026-07-30`.
- Scope: safe backup/restore, verified Windows ZIP, 1,000-row gallery regression budgets, repo-owned accessibility/DPI contracts, packaged-app UIA probe, and documentation/sample truth reconciliation.
- Repo-side result: closed. The design, implementation plan, code, scripts, focused/full tests, native probe, and repository gates are present.
- Live/provider result: unchanged. All runtime probes used `PROVIDER_MODE=fake`; no paid call, upload, code signing, installation, external publication, or live-provider evidence refresh occurred.
- Manual/live acceptance still open: Narrator, actual system high-contrast switching, a non-default-DPI hardware matrix, touch/pen ergonomics, subjective WPF frame/scroll quality, and real low-memory Windows devices.
- Package claim: verified framework-dependent `win-x64` ZIP. MSI/MSIX installation, signing, auto-update, and Store registration are not claimed.

## Implemented Contracts

### Backup And Restore

- Schema-v1 manifest records normalized path, byte length, and SHA-256 for every payload.
- Backup creation uses a same-directory temporary ZIP and replaces the destination only after success.
- Safe defaults exclude `.env`, local appsettings overrides, SQLite databases, `workspace`, `outputs`, `bin`, and `obj`.
- Restore validates the complete archive before target mutation: supported manifest, case-insensitive uniqueness, one-to-one membership, lexical containment, reparse/link rejection, entry/total limits, target conflicts, lengths, and streamed hashes.
- The localized WPF inspector exposes safe backup and non-overwriting separate-folder restore with stable UIA IDs/names and polite status.

### Windows Package

- `publish-app.ps1` performs Release `win-x64` publish, writes portable payload hashes, creates a normalized ZIP, writes the archive `.sha256`, and invokes an independent verifier.
- `verify-publish-package.ps1` rejects unsafe/duplicate/link-like/oversized/nested-ZIP entries, manifest drift, missing required files, length/hash changes, and sidecar mismatch without extracting.
- `preflight-release.ps1 -NoRestore` now performs a real isolated publish/package/verify cycle and removes only `publish/preflight`.
- The WPF project declares `RuntimeIdentifiers=win-x64` so ordinary restore/build produces the RID graph required by truthful `--no-restore` publish.

### Gallery And Accessibility

- The 1,000-row benchmark enforces generous local regression ceilings and requires cached thumbnail revisit to be at least 25% faster than initial warmup.
- The gallery keeps recycling virtualization and asynchronous thumbnails, adds deferred scrolling, native single selection, container focus, localized UIA, and system-color boundaries.
- The app embeds `PerMonitorV2`, applies a system-highlight focus visual to principal interactive control types, and statically audits every interactive control in nine principal inspector forms for stable ID and localized name.
- Existing physics-poster import/trial assets and the scientific-figure operator kit remain the canonical samples; no duplicate sample was manufactured for closeout.

## Focused Evidence

| Check | Result |
| --- | --- |
| Phase 7 focused tests | `45 / 45` passed after the final build |
| Backup service tests | safe exclusions, manifest SHA, tamper rejection, path escape, missing/duplicate manifest entries, and zero-write target conflict passed |
| Package positive verification | `84` payload files, framework-dependent `win-x64`, required executable/runtime files and sidecar passed |
| Package negative verification | replacing `ContentDeliveryStudio.App.runtimeconfig.json` caused the verifier to fail on manifest size mismatch |
| Principal-form accessibility audit | every Button/TextBox/ComboBox/ListBox in nine owned inspector views has an AutomationId and accessible Name |
| Gallery benchmark | rows `1000`; projection `1 ms`; warmup `2,454 ms`; cached revisit `132 ms`; export `4,256 ms`; 250-row import `50 ms`; peak managed memory `16.65 MiB` |

The performance figures are host-local regression evidence. They do not certify frame timing or low-memory hardware.

## Canonical Package And Native Probe

- Artifact: `publish/ContentDeliveryStudio.App-win-x64-Release.zip` (ignored runtime output).
- ZIP SHA-256: `044496e4f5256de6ad5954717b145c71aa201ea44696e87b631f4eef111addd0`.
- Executable SHA-256: `85db9bbcd045e330130df3923138ac30f7e604b3c8c06a6f16052ef0ab78327e`.
- Actor: `authorized_agent` under the user's explicit autonomous execution authority; not relabeled as human.
- Launch: verified ZIP extracted to an isolated temporary directory, isolated data root, `PROVIDER_MODE=fake`.
- Window: `AI 内容交付工作台`, `1180 x 760`, observed `96 DPI / 100%`.
- UIA: all 13 required shell, Diagnostics, backup/restore, and activity elements were present with non-empty localized names and expected enabled/focusable state.
- Probe report: ignored local output `outputs/phase7-packaged-accessibility-probe.json`.

This probe proves verified-ZIP launch, current-DPI minimum layout, and the named UIA contract. It is not evidence for Narrator, real high contrast, non-default DPI, touch/pen, or low-memory acceptance.

## Five-Axis Review

- Correctness: reviewed manifest membership, preflight-before-write, command enablement, RID restore, package membership, and benchmark assertions; focused and full suites passed.
- Readability/architecture: backup remains Application port + Infrastructure implementation + feature-owned WPF panel; publish verification stays in scripts; no new dependency or general framework was introduced.
- Security: review found and fixed missing source/target reparse rejection plus package entry count/size/link/nested-ZIP limits. Secrets, SQLite, workspace, output, prompts, and provider bodies remain outside backup/package evidence.
- Performance: backup/package hashing is explicit user/release work, not a UI hot loop; gallery hot-path budgets and cache benefit are enforced.
- Compatibility: provider, scientific workflow, queue, diagnostics, SQLite schema, existing delivery package, and accepted evidence contracts are unchanged. Old unversioned backup manifests fail closed because their integrity cannot be proven.

## Fixed-Order Verification

| Stage | Command | Result |
| --- | --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln` | exit `0`; `0` warnings, `0` errors |
| Focused test | `dotnet test ... --no-build --filter "LocalBackupRestoreServiceTests|BackupRestorePanelViewModelTests|LargeGalleryPerformanceBenchmarkTests|Phase7AccessibilityContractTests|WpfShellAccessibilityTests|MainWindowLayoutTests"` | exit `0`; `45 / 45` passed |
| Full test | `dotnet test ContentDeliveryStudio.sln --no-build` | exit `0`; `734 / 734` passed, `0` skipped |
| Contract | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1` | exit `0`; all touched enforced areas passed |
| Format | `dotnet format --verify-no-changes` | exit `0`; no formatting drift |
| Hotspot | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore` | exit `0`; scans, nested `734 / 734`, format, actual 84-file publish/package/verify, and diff hygiene passed |

## N/A, Rollback, And Recovery

- `gate_na`: paid/live-provider verification. Reason: this slice is local backup, packaging, performance, and WPF hardening; alternative verification is fake-first tests and native packaged launch; evidence is this file; expires when provider behavior changes; recovery condition is explicit user approval for a new paid sample.
- `gate_na`: Narrator/high-contrast/non-default-DPI/touch/pen/low-memory acceptance. Reason: the current machine/run did not provide those assistive-technology and hardware conditions; alternative verification is static UIA/system-brush/PerMonitorV2 contracts plus current-DPI native probe; evidence is this file; no automatic expiry; recovery condition is a declared manual matrix on representative Windows hardware.
- Rollback only this Phase 7 source, script, test, doc, and evidence slice. Generated `publish/`, `outputs/`, backup ZIPs, restore targets, databases, and workspaces are Git-external runtime data and are not restored by Git rollback.
