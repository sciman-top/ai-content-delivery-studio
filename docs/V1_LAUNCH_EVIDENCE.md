# V1 Launch Evidence

## Purpose

This file tracks current evidence against the explicit V1 launch metrics in [PRD_V1.md](./PRD_V1.md).

Within the core document set, this is the authoritative file for current V1 launch-verification status. `ROADMAP.md` and `TASKS.md` may describe sequencing or implementation progress, but they should defer to this file for current release-claim truth. See [DOCUMENTATION_GOVERNANCE.md](./DOCUMENTATION_GOVERNANCE.md).

It is intentionally strict about evidence type:

- `Automated repo evidence`: local tests, build gates, and deterministic artifact checks inside this repository.
- `Live operator evidence`: a real local tool action or workflow execution that writes artifacts or audit output.
- `Live provider evidence`: an opt-in real-provider run with current credentials and redaction checks.

The goal is to keep launch readiness honest. Completed implementation slices are not the same thing as completed launch metrics.

## Snapshot

- Snapshot date: `2026-06-11`
- Repository root: `D:\CODE\ai-content-delivery-studio`
- Latest live provider sample run: `artifacts/live-openai-v1-sample/20260611-132947`
- Fresh gate for this snapshot:
  - `dotnet build --no-restore`
  - `dotnet test --no-build --no-restore`
  - `dotnet format --verify-no-changes`
- Result:
  - Build passed
  - Tests passed: `362 / 362`
  - Format check passed

## Metric Ledger

| Launch metric | Current status | Evidence now | Remaining gap |
| --- | --- | --- | --- |
| Primary route completes three consecutive fake-first end-to-end runs with no paid APIs and no manual database edits. | Verified by automated repo evidence | `PrimaryLaunchRouteVerificationTests.PrimaryLaunchRoute_CompletesThreeConsecutiveFakeFirstRunsWithoutManualDatabaseEdits` proves three consecutive short-requirement -> brief -> blueprint -> series -> review -> delivery runs with fake providers and persisted local state checks. Supporting slice coverage remains in `FakeWorkflowTests`, `ProjectApplicationServiceTests`, and `BriefWorkflowApplicationServiceTests`. | A future user-facing script can still mirror this suite, but the launch metric now has automated proof. |
| A 2-item sample series completes through the opt-in OpenAI path with request provenance, review evidence, and secret redaction verified. | Verified by live provider evidence | `artifacts/live-openai-v1-sample/20260611-132947/live-v1-sample-summary.json` records a fresh opt-in OpenAI run that passed launch preflight, completed real text planning, real image generation, real image review, final approval, delivery export, and diagnostics export for two items. `artifacts/live-openai-v1-sample/20260611-132947/outputs/delivery/manifest.json` shows both items as human-approved on their first approved attempts with prompt snapshots and metadata preserved, while `artifacts/live-openai-v1-sample/20260611-132947/diagnostics/diagnostics.json` and `openai-launch-preflight.json` keep secret values redacted. This refreshed run also validates the current compact visual-review path and the bounded transient `502 upstream_error` retry on the official SDK Images route under live conditions. Supporting automated guardrails remain in `OpenAiProviderContractTests`, `OpenAiProviderConfigurationTests`, `OpenAiProviderSmokeTests`, `OpenAiLaunchPreflightTests`, `OpenAiLaunchPreflightReportWriterTests`, `OpenAiLaunchPreflightToolAdapterTests`, `ToolAdapterServiceCollectionExtensionsTests`, `DiagnosticsPackageTests`, `OpenAiOfficialSdkImageGenerationProviderTests`, and `OpenAiLiveV1SampleRouteTests`. | Refresh this evidence only when provider behavior materially changes or a newer launch snapshot is needed. |
| Article or plain-text planning can produce and promote approved illustration targets without requiring real providers by default. | Verified by automated repo evidence | `SupportingValidationRouteVerificationTests.SupportingValidationRoute_CompletesFakeFirstDocumentPlanningThroughDelivery` proves article/plain-text planning, approved-target promotion, fake-first generation, review, approval, and delivery export in one route. `DocumentIllustrationWorkflowTests` still cover the narrower planning and oversize-guard boundary. | Still worth adding a user-facing script later, but the launch metric already has automated proof. |
| The educational poster proof path exports deterministic text-composition provenance and human approval evidence. | Verified by automated repo evidence | `EducationalPosterLaunchProofTests.EducationalPosterProofPath_ExportsCompositionProvenanceAndApprovalEvidence` proves deterministic composition, copied composition-report provenance, and final approval evidence in one delivery export. Supporting component coverage remains in `SkiaDeterministicTextComposerTests`, `DeterministicTextCompositionToolAdapterTests`, and `DeliveryPackageTests`. | A future live sample export is still useful, but the launch metric now has automated proof. |
| The first real low-risk operator action runs end-to-end and writes audit output plus rollback or cleanup notes. | Verified by automated repo evidence | `ArtifactValidationToolAdapterTests.LowRiskAutoRepairService_RunsArtifactValidationAdapterAndWritesDiagnosticsReport` proves local validation output into a diagnostics folder and includes cleanup guidance. `LowRiskAutoRepairServiceTests` prove the low-risk-only execution boundary. | A user-visible launch bundle could still be added later, but the launch metric already has automated proof. |

## Current Readout

- `5 / 5` launch metrics now have either automated repo evidence or fresh live provider evidence strong enough to count as currently verified.
- The live OpenAI launch gap is closed for the current `2026-06-11` snapshot.
- The repository now has both the read-only preflight path and one fresh live provider evidence set for the same release claim.
- The current release snapshot includes one fresh live rerun that completed after the official SDK image-generation path gained a bounded transient `502 upstream_error` retry.

## Recommended Next Evidence Slices

1. If provider behavior changes later, rerun the opt-in OpenAI `2-item` sample path and refresh the live evidence artifact set.
2. If needed later, add a user-facing script that mirrors the automated three-run fake-first launch suite.
3. If needed later, add a user-facing sample export bundle that mirrors the automated educational-poster proof.

## Evidence Sources Used For This Snapshot

- `tests/ImageSeriesStudio.Tests/FakeWorkflowTests.cs`
- `tests/ImageSeriesStudio.Tests/PrimaryLaunchRouteVerificationTests.cs`
- `tests/ImageSeriesStudio.Tests/BriefWorkflowApplicationServiceTests.cs`
- `tests/ImageSeriesStudio.Tests/SupportingValidationRouteVerificationTests.cs`
- `tests/ImageSeriesStudio.Tests/DocumentIllustrationWorkflowTests.cs`
- `tests/ImageSeriesStudio.Tests/OpenAiProviderContractTests.cs`
- `tests/ImageSeriesStudio.Tests/OpenAiProviderConfigurationTests.cs`
- `tests/ImageSeriesStudio.Tests/OpenAiLiveV1SampleRouteTests.cs`
- `tests/ImageSeriesStudio.Tests/OpenAiProviderSmokeTests.cs`
- `tests/ImageSeriesStudio.Tests/OpenAiLaunchPreflightTests.cs`
- `tests/ImageSeriesStudio.Tests/OpenAiLaunchPreflightReportWriterTests.cs`
- `tests/ImageSeriesStudio.Tests/OpenAiLaunchPreflightToolAdapterTests.cs`
- `tests/ImageSeriesStudio.Tests/ToolAdapterServiceCollectionExtensionsTests.cs`
- `tests/ImageSeriesStudio.Tests/DiagnosticsPackageTests.cs`
- `tests/ImageSeriesStudio.Tests/SkiaDeterministicTextComposerTests.cs`
- `tests/ImageSeriesStudio.Tests/DeterministicTextCompositionToolAdapterTests.cs`
- `tests/ImageSeriesStudio.Tests/EducationalPosterLaunchProofTests.cs`
- `tests/ImageSeriesStudio.Tests/DeliveryPackageTests.cs`
- `tests/ImageSeriesStudio.Tests/ProjectApplicationServiceTests.cs`
- `tests/ImageSeriesStudio.Tests/ArtifactValidationToolAdapterTests.cs`
- `tests/ImageSeriesStudio.Tests/LowRiskAutoRepairServiceTests.cs`
