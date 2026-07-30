# Structured Diagnostics Log Ingestion Design

**Status:** Approved for implementation
**Date:** 2026-07-29
**Scope:** Local-only, redaction-first structured event journal and diagnostics-bundle ingestion

## Problem

The native diagnostics export can capture current application, project, provider, and secret-presence snapshots, but it cannot explain the recent sequence that led to a queue or provider failure. `docs/TASKS.md` and `docs/ROADMAP.md` deliberately leave structured log ingestion open for a repo-owned observability slice. A general `ILogger` collector would be too broad because prompts, response bodies, paths, credentials, and third-party messages could enter the support bundle.

## Decision

Add a bounded JSONL event journal under the existing app-local data root. The journal accepts only two strongly typed repository-owned event families:

1. generation queue lifecycle events for prepare, pause, resume, move, retry, execution start, and terminal completion;
2. provider call summaries already represented by `ProviderCallTelemetry`.

Each persisted line has a fixed schema version, timestamp, level, category, event name, correlation ID, and a fixed safe-properties object. There is no arbitrary message, exception, template, or property-bag input. The journal writes locally, rotates at 1 MiB, retains the active file plus two older files, and never uploads or starts a background worker.

The diagnostics exporter reads at most the most recent 500 valid events. Malformed, oversized, unknown-schema, unknown-category, unknown-event, or unsafe-string lines are skipped and counted as invalid. Write failures and rejected write records are counted as dropped. Diagnostics JSON and Markdown expose retained events plus dropped/invalid counts.

## Data Contract

The persisted top-level fields are:

- `schemaVersion`
- `timestamp`
- `level`
- `category`
- `eventName`
- `correlationId`
- `properties`

The fixed properties shape may contain only project/task IDs, queue state/position/count, provider ID, operation, model, HTTP status, success, latency, token total, and estimated cost. Fields that do not apply remain absent or null. IDs and numeric/boolean values are scalar. String values are bounded and rejected or redacted when they resemble credentials, authorization material, `.env` references, line-oriented content, or rooted user paths.

Queue records correlate by project ID. Provider records correlate with the current Activity trace when available and otherwise with a bounded repository-generated identifier. Request/response IDs, endpoint/query data, prompts, response bodies, generated assets, output paths, full exception text, and user-facing names are excluded.

## Architecture

### Application

`IDiagnosticsEventJournal` owns the two typed record operations and a bounded recent-read operation. `NullDiagnosticsEventJournal` preserves existing direct construction and tests. `DiagnosticsPackageApplicationService` reads recent journal entries immediately before delegating to the existing writer.

`GenerationWorkflowApplicationService` receives an optional journal and records only after authoritative persistence checkpoints. Observability failure must never change queue state or provider authorization behavior.

### Infrastructure

`JsonlDiagnosticsEventJournal` serializes the fixed schema with `System.Text.Json`, synchronizes append/rotation/read access, and resolves its directory through `LocalStudioDataPaths`. The reader processes retained files oldest to newest, keeps only the newest requested entries, and validates every line before returning it.

`DiagnosticProviderCallTelemetrySink` keeps its existing Activity/Meter behavior and optionally mirrors a safe summary into the journal. OTLP remains opt-in through the existing environment configuration and is not changed by this slice.

### Desktop Composition

The WPF host registers one singleton journal as `IDiagnosticsEventJournal`. Existing diagnostics export, project workflow, and OpenAI telemetry composition receive it through dependency injection. No new panel or background service is introduced.

## Safety And Truth Boundaries

- The journal is local-only and contains no prompt, response body, material content, API key, Authorization value, `.env` content, complete exception text, or user file path.
- Only repository-defined typed events are accepted; this is not general application logging.
- Existing OpenTelemetry Activity/Metrics and OTLP opt-in behavior remain intact.
- No paid provider call, live-provider enablement, upload, external publication, database migration, or automatic queue replay is part of this slice.
- Dropped/invalid counts describe journal handling, not product-task failures.
- Completion is repo-side structured diagnostics capability, not live-provider or onsite support acceptance.

## Reference Basis

- `dotnet-extensions` at `c6bf2e02eeda9bc67003d509db59ad8f887f1c47`: used as reference context for bounded host-local diagnostics and dependency-injection composition; no source copied.
- `opentelemetry-dotnet` at `2eeede10c1bb7b87f3f6f70264a804d8ee9de948`: used to preserve the existing Activity/Meter boundary while adding a separate local journal; no exporter or processor implementation copied.
- Repository routing truth: `scripts/reference-basis.json`, area `host-and-observability`.

## Acceptance Criteria

- Valid typed events round-trip through JSONL with deterministic schema and ordering.
- A file rotates before exceeding 1 MiB and no more than three retained files remain.
- Concurrent record calls do not corrupt lines.
- Recent reads return at most 500 valid newest records and report malformed/oversized/unknown records.
- Unsafe strings are redacted or rejected before persistence; forbidden fields have no model or serializer path.
- Queue prepare/mutation/execute tests verify expected event names without adding provider calls.
- Provider telemetry still emits existing Activity/Metrics and adds a safe journal summary.
- Diagnostics JSON/Markdown include recent events and dropped/invalid counts.
- DI composition resolves in fake-first mode and fixed-order repository gates pass.

## Rollback

Revert only this source, test, documentation, and evidence slice. The app-local `diagnostics/events` directory is runtime data outside Git; rollback does not delete user-local files. Older application versions ignore the directory.
