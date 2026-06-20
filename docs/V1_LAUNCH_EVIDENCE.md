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

- Snapshot date: `2026-06-21`
- Repository root: `D:\CODE\ai-content-delivery-studio`
- Latest automated repo verification: `2026-06-21` via `.\scripts\verify-repo.ps1 -NoRestore`
- Latest live provider sample run: `artifacts/live-openai-v1-sample/20260611-132947`
- Fresh gate for this snapshot:
  - `.\scripts\verify-repo.ps1 -NoRestore`
- Result:
  - Reference governance parity passed
  - Reference evidence gate passed
  - Build passed
  - Tests passed: `433 / 433`
  - Format check passed

## Metric Ledger

| Launch metric | Current status | Evidence now | Remaining gap |
| --- | --- | --- | --- |
| Primary route completes three consecutive fake-first end-to-end runs with no paid APIs and no manual database edits. | Verified by automated repo evidence | `PrimaryLaunchRouteVerificationTests.PrimaryLaunchRoute_CompletesThreeConsecutiveFakeFirstRunsWithoutManualDatabaseEdits` proves three consecutive short-requirement -> brief -> blueprint -> series -> review -> delivery runs with fake providers and persisted local state checks. Supporting slice coverage remains in `FakeWorkflowTests`, `ProjectApplicationServiceTests`, and `BriefWorkflowApplicationServiceTests`. | A future user-facing script can still mirror this suite, but the launch metric now has automated proof. |
| A 2-item sample series completes through the opt-in OpenAI path with request provenance, review evidence, and secret redaction verified. | Verified by live provider evidence | `artifacts/live-openai-v1-sample/20260611-132947/live-v1-sample-summary.json` records the latest available opt-in OpenAI run for this release claim. That recorded sample passed launch preflight, completed real text planning, real image generation, real image review, final approval, delivery export, and diagnostics export for two items. `artifacts/live-openai-v1-sample/20260611-132947/outputs/delivery/manifest.json` shows both items as human-approved on their first approved attempts with prompt snapshots and metadata preserved, while `artifacts/live-openai-v1-sample/20260611-132947/diagnostics/diagnostics.json` and `openai-launch-preflight.json` keep secret values redacted. The recorded sample also validates the current compact visual-review path and the bounded transient `502 upstream_error` retry on the official SDK Images route under live conditions. Supporting automated guardrails remain in `OpenAiProviderContractTests`, `OpenAiProviderConfigurationTests`, `OpenAiProviderSmokeTests`, `OpenAiLaunchPreflightTests`, `OpenAiLaunchPreflightReportWriterTests`, `OpenAiLaunchPreflightToolAdapterTests`, `ToolAdapterServiceCollectionExtensionsTests`, `DiagnosticsPackageTests`, `OpenAiOfficialSdkImageGenerationProviderTests`, and `OpenAiLiveV1SampleRouteTests`. | Refresh this evidence only when provider behavior materially changes or a newer live-provider snapshot is needed. |
| Article or plain-text planning can produce and promote approved illustration targets without requiring real providers by default. | Verified by automated repo evidence | `SupportingValidationRouteVerificationTests.SupportingValidationRoute_CompletesFakeFirstDocumentPlanningThroughDelivery` proves article/plain-text planning, approved-target promotion, fake-first generation, review, approval, and delivery export in one route. `DocumentIllustrationWorkflowTests` still cover the narrower planning and oversize-guard boundary. | Still worth adding a user-facing script later, but the launch metric already has automated proof. |
| The educational poster proof path exports deterministic text-composition provenance and human approval evidence. | Verified by automated repo evidence | `EducationalPosterLaunchProofTests.EducationalPosterProofPath_ExportsCompositionProvenanceAndApprovalEvidence` proves deterministic composition, copied composition-report provenance, and final approval evidence in one delivery export. Supporting component coverage remains in `SkiaDeterministicTextComposerTests`, `DeterministicTextCompositionToolAdapterTests`, and `DeliveryPackageTests`. | A future live sample export is still useful, but the launch metric now has automated proof. |
| The first real low-risk operator action runs end-to-end and writes audit output plus rollback or cleanup notes. | Verified by automated repo evidence | `ArtifactValidationToolAdapterTests.LowRiskAutoRepairService_RunsArtifactValidationAdapterAndWritesDiagnosticsReport` proves local validation output into a diagnostics folder and includes cleanup guidance. `LowRiskAutoRepairServiceTests` prove the low-risk-only execution boundary. | A user-visible launch bundle could still be added later, but the launch metric already has automated proof. |

## Current Readout

- `5 / 5` launch metrics now have either fresh automated repo evidence or recorded live provider evidence strong enough to count as currently verified.
- The live OpenAI launch gap remains closed for the current `2026-06-21` snapshot.
- The fresh automated repo gate for this snapshot passed through the canonical repository verification path with `433 / 433` tests.
- The latest live OpenAI evidence in this snapshot still comes from `artifacts/live-openai-v1-sample/20260611-132947`; no newer paid-provider rerun was required for this readout refresh.
- The repository now has both the read-only preflight path and one recorded live provider evidence set for the same release claim.

## Recommended Next Evidence Slices

1. If provider behavior changes later, rerun the opt-in OpenAI `2-item` sample path and refresh the live evidence artifact set.
2. If needed later, add a user-facing script that mirrors the automated three-run fake-first launch suite.
3. If needed later, add a user-facing sample export bundle that mirrors the automated educational-poster proof.

## Evidence Sources Used For This Snapshot

- `tests/ContentDeliveryStudio.Tests/FakeWorkflowTests.cs`
- `tests/ContentDeliveryStudio.Tests/PrimaryLaunchRouteVerificationTests.cs`
- `tests/ContentDeliveryStudio.Tests/BriefWorkflowApplicationServiceTests.cs`
- `tests/ContentDeliveryStudio.Tests/SupportingValidationRouteVerificationTests.cs`
- `tests/ContentDeliveryStudio.Tests/DocumentIllustrationWorkflowTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiProviderContractTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiProviderConfigurationTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiLiveV1SampleRouteTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiProviderSmokeTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiLaunchPreflightTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiLaunchPreflightReportWriterTests.cs`
- `tests/ContentDeliveryStudio.Tests/OpenAiLaunchPreflightToolAdapterTests.cs`
- `tests/ContentDeliveryStudio.Tests/ToolAdapterServiceCollectionExtensionsTests.cs`
- `tests/ContentDeliveryStudio.Tests/DiagnosticsPackageTests.cs`
- `tests/ContentDeliveryStudio.Tests/SkiaDeterministicTextComposerTests.cs`
- `tests/ContentDeliveryStudio.Tests/DeterministicTextCompositionToolAdapterTests.cs`
- `tests/ContentDeliveryStudio.Tests/EducationalPosterLaunchProofTests.cs`
- `tests/ContentDeliveryStudio.Tests/DeliveryPackageTests.cs`
- `tests/ContentDeliveryStudio.Tests/ProjectApplicationServiceTests.cs`
- `tests/ContentDeliveryStudio.Tests/ArtifactValidationToolAdapterTests.cs`
- `tests/ContentDeliveryStudio.Tests/LowRiskAutoRepairServiceTests.cs`
- `scripts/verify-repo.ps1`
- `scripts/sync-reference-governance.ps1`
- `scripts/verify-reference-evidence.ps1`
