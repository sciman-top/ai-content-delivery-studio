# Structured Diagnostics Log Ingestion Implementation Plan

**Goal:** Add a bounded, local-only structured event journal and include its recent safe events in diagnostics exports.

**Architecture:** Define typed journal ports in Application, implement fixed-schema rotating JSONL in Infrastructure, mirror explicit queue/provider summaries, and ingest at most 500 validated entries into the existing diagnostics package.

**Tech stack:** .NET 10, `System.Text.Json`, WPF dependency injection, xUnit, repository PowerShell gates.

## Task 1: Define The Typed Contract

**Files:**

- Add `src/ContentDeliveryStudio.Application/Diagnostics/DiagnosticsEventJournal.cs`
- Modify `src/ContentDeliveryStudio.Application/Diagnostics/DiagnosticsExport.cs`
- Add focused tests

**Acceptance criteria:**

- [x] Only typed queue and provider event inputs exist.
- [x] Export entries use a fixed top-level schema and fixed safe-properties record.
- [x] A null implementation preserves existing construction and behavior.

## Task 2: Implement Rotating JSONL Persistence

**Files:**

- Add `src/ContentDeliveryStudio.Infrastructure/Diagnostics/JsonlDiagnosticsEventJournal.cs`
- Add `tests/ContentDeliveryStudio.Tests/JsonlDiagnosticsEventJournalTests.cs`

**Acceptance criteria:**

- [x] Valid events round-trip in order under the app-local data root.
- [x] Rotation retains at most three files and concurrent writes remain line-valid.
- [x] Reads return at most the requested bounded count.
- [x] Malformed, oversized, unknown, and unsafe content fails closed with counts.

**Verification:** Run `JsonlDiagnosticsEventJournalTests`.

## Task 3: Ingest Recent Events Into Diagnostics

**Files:**

- Modify `src/ContentDeliveryStudio.Application/Diagnostics/DiagnosticsPackageApplicationService.cs`
- Modify `src/ContentDeliveryStudio.Infrastructure/Diagnostics/DiagnosticsPackageWriter.cs`
- Modify diagnostics application/writer tests

**Acceptance criteria:**

- [x] Export requests contain at most 500 recent valid entries.
- [x] JSON and Markdown include retained entries and dropped/invalid counts.
- [x] Existing constructor call sites and exports remain compatible through the null journal.

## Task 4: Instrument Queue And Provider Boundaries

**Files:**

- Modify `src/ContentDeliveryStudio.Application/Projects/GenerationWorkflowApplicationService.cs`
- Modify `src/ContentDeliveryStudio.Application/Projects/ProjectApplicationService.cs`
- Modify `src/ContentDeliveryStudio.Infrastructure/OpenAI/OpenAiProviderTelemetry.cs`
- Modify focused queue and telemetry tests

**Acceptance criteria:**

- [x] Queue events are emitted only at persisted lifecycle checkpoints.
- [x] Provider summaries exclude endpoint, request/response IDs, bodies, and secrets.
- [x] Existing Activity/Meter tests remain green.
- [x] Journal failure never changes workflow or provider behavior.

## Task 5: Compose The Runtime

**Files:**

- Modify `src/ContentDeliveryStudio.App/App.xaml.cs`
- Modify composition tests as required

**Acceptance criteria:**

- [x] One singleton journal is shared by queue, provider telemetry, and diagnostics export.
- [x] Fake-first host composition resolves without a network call.
- [x] No background service, upload, or OTLP behavior change is introduced.

## Task 6: Synchronize Truth And Evidence

**Files:**

- Modify `docs/TASKS.md`
- Modify `docs/ROADMAP.md`
- Modify `docs/ARCHITECTURE.md`
- Modify `docs/USER_GUIDE.md`
- Modify `docs/zh-CN/USER_GUIDE.md`
- Add `docs/change-evidence/20260729-structured-diagnostics-log-ingestion.md`

**Acceptance criteria:**

- [x] Repo-side completion and live/onsite boundaries remain explicit.
- [x] Privacy exclusions, retention, reference revisions, rollback, and verification are recorded.
- [x] Prior diagnostics and queue evidence remain intact.

## Final Gate

Run in fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
2. focused tests, then `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format --verify-no-changes`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Then run five-axis review, secret/generated-artifact scan, compatibility review, and diff hygiene. Do not call a paid provider, commit, or push.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Sensitive content reaches support bundles | No arbitrary message/property API; fixed typed fields plus write/read validation. |
| Logging failure breaks production work | Journal operations are fail-closed and non-throwing to callers. |
| Runtime files grow without bound | 1 MiB rotation with three-file retention and a 500-entry export cap. |
| Existing telemetry changes behavior | Preserve Activity/Meter code and keep the local journal as a separate optional sink. |
| Old or hand-edited lines poison export | Validate each line independently, skip invalid data, and report invalid count. |
