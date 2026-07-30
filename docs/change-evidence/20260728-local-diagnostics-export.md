# Local Diagnostics Export Evidence

> Supersession note (2026-07-29): the structured-log future trigger named below is now closed by `20260729-structured-diagnostics-log-ingestion.md`. Automatic upload, ZIP creation, backup/restore, installer, broader accessibility, and performance lanes remain open.

## Scope And Truth Boundary

- Slice: user-visible, local-only diagnostics export from the native WPF Workbench Inspector.
- Repo-side result: implemented. The existing redacting writer is registered and reachable through a read-only application service, resolved-provider snapshot factory, native folder picker, localized panel, and stable AutomationIds.
- Automated evidence: focused unit/layout/localization coverage plus the fixed repository gate.
- Operator evidence: an `authorized_agent` used the native WPF UI under the user's explicit equivalent-operator acceptance authority. The actor is not relabeled as human.
- Live accepted evidence: unchanged. No paid provider was called and no live-provider evidence was refreshed.
- Future Trigger Lanes: structured log ingestion, automatic support upload, ZIP creation, backup/restore, installer, accessibility pass, and broader performance work remain open.
- Existing accepted Tasks 1-30, Checkpoints 0-5, scientific-figure artifacts, and V1 launch evidence were not modified.

## Reference Basis

| Area | Revision | Decision |
| --- | --- | --- |
| `02-dotnet-wpf/docs-desktop` common-dialog guidance | `3ed3bb19178883827aa0a81427576797db141862` | Adopt the native common-dialog interaction boundary. |
| `02-dotnet-wpf/WPF-Samples` common-dialog ViewModel sample | `ecd9529fb6941272eff1ee1e7e2554e3ecb2f1e4` | Adapt behind an injectable folder-picker service for testability. |
| `03-dotnet-extensions` | `c6bf2e02eeda9bc67003d509db59ad8f887f1c47` | Retain as host/DI reference context; no source copied. |
| `04-opentelemetry-dotnet` | `2eeede10c1bb7b87f3f6f70264a804d8ee9de948` | Retain as observability reference context; structured telemetry ingestion is deferred. |

No external source was modified or executed.

## Native WPF Authorized-Agent Probe

- Date: `2026-07-28`.
- Mode: fake-first; header showed `假 Provider` and activity stated that no real API call was enabled.
- Actor type: `authorized_agent`.
- Acceptance policy: explicit user authority treats this native operation as equivalent operator acceptance while preserving the truthful actor identity.
- Window: `AI 内容交付工作台`, observed at `1166 x 753`.
- Interaction: `BrowseDiagnosticsDirectory` -> native `OpenFolderDialog` -> select `%TEMP%` -> `ExportDiagnosticsPackage`.
- UI result: the export button changed from disabled to enabled after folder selection; the success status exposed a unique child directory; no clipping, overlap, or incoherent layout was observed in the diagnostics panel.
- Package directory: `C:\Users\sciman\AppData\Local\Temp\content-delivery-studio-diagnostics-20260728-161623-236-40831d68338a4cac82695930a008c085`.
- Files: `diagnostics.json` and `diagnostics.md` both existed.
- Provider result: `fake-image`, `fake-text`, and `fake-vision`; every `realApiEnabled` value was `false`.
- Secret-presence result: only `text-provider-key-pool`, `text-provider-app-credentials`, `image-provider-key-pool`, and `image-provider-app-credentials`, all `false` in this probe.
- Negative scan: `OPENAI_API_KEY`, `TEXT_PROVIDER_API_KEY`, `IMAGE_PROVIDER_API_KEY`, `APP_SECRET`, and `.env` were absent from the combined JSON/Markdown output.
- Project count was `0` for this isolated app launch. Automated application-service coverage separately proves repository project loading and snapshot mapping without `SaveAsync`.

The probe directory is local temporary output, not Git evidence, and is not part of any accepted scientific-figure artifact.

## Verification

Focused verification before final gate:

| Command | Result |
| --- | --- |
| `dotnet build ContentDeliveryStudio.sln` | Exit `0`; `0` warnings, `0` errors. |
| `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter "FullyQualifiedName~Diagnostics\|FullyQualifiedName~WorkbenchInspector_ExposesLocalDiagnosticsPanel"` | Exit `0`; `13 / 13` passed after the environment-independent localization correction. |

Final fixed-order verification is recorded after implementation closeout:

| Stage | Command | Result |
| --- | --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln` | Exit `0`; `0` warnings, `0` errors. |
| Test | `dotnet test ContentDeliveryStudio.sln --no-build` | Exit `0`; `696 / 696` passed, `0` skipped. |
| Contract | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1` | Exit `0`; reference governance and both touched enforced areas passed. |
| Format | `dotnet format --verify-no-changes` | Exit `0`; no formatting drift. |
| Hotspot | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore` | Exit `0`; placeholder/conflict scans, repository verification, publish WhatIf, and diff hygiene passed. |

## Compatibility, Risk, And Rollback

- Compatibility: no diagnostics file-format change, schema migration, provider contract change, or accepted-artifact rewrite. The new workflow calls repository list/load only.
- Security: no secret value or configured secret identifier is requested from the configuration service; exported secret records are generic booleans.
- Performance: projects are loaded sequentially through the existing repository contract to avoid concurrent EF contexts. This is an explicit local export action, not a hot UI path.
- Failure behavior: the panel restores command availability in `finally` and shows a bounded localized error without crashing the host.
- Rollback: revert the diagnostics application/desktop source, tests, spec/plan, roadmap/task line, and this evidence file. User-selected local diagnostics directories are outside Git truth and are not deleted by rollback.
