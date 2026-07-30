# Structured Diagnostics Log Ingestion Evidence

Date: `2026-07-29`

## Status And Boundary

`repo-side implementation complete / fake-first automated acceptance passed / live-provider evidence unchanged`

This slice adds local diagnostics evidence only. It does not authorize or perform a paid provider call, external upload, background worker, automatic queue replay, OTLP enablement, database migration, or live/onsite acceptance refresh.

## Reference Basis

| Area | Revision | Decision |
| --- | --- | --- |
| `dotnet-extensions` | `c6bf2e02eeda9bc67003d509db59ad8f887f1c47` | Use as host/DI and bounded local diagnostics context; no source copied. |
| `opentelemetry-dotnet` | `2eeede10c1bb7b87f3f6f70264a804d8ee9de948` | Preserve the existing Activity/Meter and opt-in OTLP boundary; no exporter/processor source copied. |

Routing truth is `scripts/reference-basis.json`, required area `host-and-observability`. Both external worktrees were read-only and no dependency was added.

## Implementation Evidence

- `IDiagnosticsEventJournal` accepts only strongly typed generation-queue and provider-call inputs. There is no arbitrary log message, exception, template, or property-bag API.
- Each JSONL line has schema version, timestamp, level, category, event name, correlation ID, and a fixed safe-properties record.
- `JsonlDiagnosticsEventJournal` writes below `LocalStudioDataPaths.ResolveStudioRoot()`, rotates at 1 MiB, and retains three files.
- Reads validate every line, reject unknown or duplicate fields and unsafe strings, keep at most the newest 500 events, and report dropped/invalid counts.
- Queue prepare, pause, resume, move, retry, start, and terminal events are emitted only after repository persistence checkpoints.
- Provider summaries contain provider ID, operation, model, status, success, latency, token total, and cost estimate. Endpoint, request/response IDs, prompt/body content, and credentials are absent.
- Existing provider Activity/Metrics remain intact. Journal exceptions are contained and cannot change queue persistence, provider authorization, or call results.
- Diagnostics JSON/Markdown contain recent validated events and counts. No uploader, hosted sink, or background service exists.

## Privacy Contract

The event model and serializer provide no path for prompts, response bodies, source/material content, generated assets, output paths, API keys, Authorization values, `.env` content, full exception text, endpoint/query data, request/response IDs, or user file paths. Bounded string fields receive a second redaction check before persistence, and read-time validation fails closed for hand-edited data.

## Focused Verification

| Command | Result |
| --- | --- |
| `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-build --filter "FullyQualifiedName~JsonlDiagnosticsEventJournalTests\|FullyQualifiedName~DiagnosticsPackageTests\|FullyQualifiedName~DiagnosticsPackageApplicationServiceTests\|FullyQualifiedName~DiagnosticsPanelViewModelTests\|FullyQualifiedName~GenerationWorkflowApplicationServiceTests\|FullyQualifiedName~ProviderCallTelemetryInstrumentationTests\|FullyQualifiedName~MainWindowLayoutTests"` | Exit `0`; `43 / 43` passed. |

Coverage includes round-trip, redaction, rotation, three-file retention, concurrent writes, newest-500 bounding, malformed/oversized/unknown/duplicate-line rejection, queue event ordering, provider summary shape, existing Activity/Metrics, diagnostics rendering, and journal-failure isolation.

## Final Fixed-Order Gate

| Stage | Command | Result |
| --- | --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln` | Exit `0`; `0` warnings, `0` errors. |
| Test | focused command above, then `dotnet test ContentDeliveryStudio.sln --no-build` | Exit `0`; `43 / 43` focused and `716 / 716` full passed. |
| Contract/invariant | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`, then `dotnet format --verify-no-changes` | Both exit `0`; all touched enforced areas and formatting passed. |
| Hotspot | `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore` | Exit `0`; repository verification, `716 / 716` tests, publish WhatIf, and diff hygiene passed. |

## Five-Axis Review And Hygiene

- Correctness: persisted checkpoint ordering, rotation boundaries, newest-500 selection, malformed/unknown/duplicate data, oversized retained files, and diagnostics rendering were reviewed. The review found and fixed the oversized retained-file read boundary before final gates.
- Readability and architecture: Application owns typed ports, Infrastructure owns local JSONL and OpenTelemetry integration, WPF owns singleton composition, and no arbitrary logging abstraction or new dependency was added.
- Security: the fixed event model and read/write validators exclude forbidden content; secret-marker/path scans found no credential or runtime artifact in the slice. Hand-edited JSON is treated as untrusted input.
- Performance: storage is bounded to three 1 MiB files, explicit export reads at most that bounded retained set, and no polling, background service, network sink, or unbounded query was added.
- Git hygiene: `git diff --check` and cached diff checks pass; no staged file, diagnostics runtime output, build output, workspace, or generated asset was added. The independent `AGENTS.md` modification remains untouched.

## Compatibility, Risk, And Rollback

- Existing direct constructors fall back to `NullDiagnosticsEventJournal`; public queue/provider contracts and OTLP configuration remain compatible.
- The journal is bounded to approximately 3 MiB plus filesystem metadata and reads only on explicit diagnostics export.
- Dropped counts are process-local best-effort evidence; invalid counts are recalculated from retained files. Neither count represents failed generation work.
- Older binaries ignore the app-local `diagnostics/events` directory.
- Rollback reverts this source/test/docs slice. Git rollback does not delete app-local journal files, and no schema downgrade or external cleanup is required.
