# Reliability Hardening Wave 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Harden high-risk persistence, file I/O, provider failure handling, and workbench async state boundaries without breaking current public contracts.

**Architecture:** Keep the current V1 surface stable, add internal helpers for atomic writes and async safety, tighten repository save semantics for detached aggregates, and expand regression tests before and after each implementation slice.

**Tech Stack:** .NET 10, WPF, EF Core SQLite, xUnit, OpenAI .NET SDK, local PowerShell verification scripts

---

### Task 1: Lock Spec And First Regression Tests

**Files:**
- Create: `docs/superpowers/specs/2026-06-24-reliability-hardening-wave1-design.md`
- Create: `docs/superpowers/plans/2026-06-24-reliability-hardening-wave1.md`
- Modify: `tests/ContentDeliveryStudio.Tests/DeliveryPackageTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/LocalStudioDataPathsTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/OpenAiProviderContractTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/OpenAiSdkImageTransportTests.cs`

- [x] Add repo-owned spec and plan documents that fix scope, acceptance, and non-goals for this wave.
- [x] Add regression tests for duplicate delivery keys, unsafe local path segments, invalid provider JSON, and invalid SDK image payload parsing.
- [x] Run the touched test set and full repository gates after implementation to confirm the hardening paths pass under repository verification.

### Task 2: Harden Local File And Path Boundaries

**Files:**
- Create: `src/ContentDeliveryStudio.Infrastructure/IO/AtomicFileWriter.cs`
- Modify: `src/ContentDeliveryStudio.Infrastructure/Delivery/DeliveryPackageWriter.cs`
- Modify: `src/ContentDeliveryStudio.Infrastructure/OpenAI/DpapiOpenAiSecretStore.cs`
- Modify: `src/ContentDeliveryStudio.Application/Projects/LocalStudioDataPaths.cs`
- Modify: `src/ContentDeliveryStudio.App/Services/GalleryThumbnailCache.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/DeliveryPackageTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/GalleryThumbnailCacheTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/OpenAiProviderConfigurationTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/LocalStudioDataPathsTests.cs`

- [x] Introduce one shared internal helper for temp-file write + atomic replace + cleanup.
- [x] Reject duplicate delivery item keys and preserve existing manifest contract.
- [x] Route delivery, thumbnail, and DPAPI file writes through the shared helper.
- [x] Reject unsafe area-name traversal in `LocalStudioDataPaths`.
- [x] Add regression tests for cleanup, duplicate keys, and path rejection.

### Task 3: Tighten Provider Failure Handling And Persistence Save Semantics

**Files:**
- Modify: `src/ContentDeliveryStudio.Infrastructure/Persistence/EfProjectRepository.cs`
- Modify: `src/ContentDeliveryStudio.Infrastructure/OpenAI/OpenAiTextPlanningResponseMapper.cs`
- Modify: `src/ContentDeliveryStudio.Infrastructure/OpenAI/OpenAiSdkImageTransport.cs`
- Modify: `src/ContentDeliveryStudio.Infrastructure/OpenAI/OpenAiResponsesClient.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/PersistenceTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/OpenAiProviderContractTests.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/OpenAiSdkImageTransportTests.cs`

- [x] Replace repeated nested `AnyAsync` child-existence probing with deterministic attach/add/update handling for detached aggregates.
- [x] Preserve existing load behavior and schema while adding coverage for nested new children and repeated save/update scenarios.
- [x] Convert invalid provider response shapes into explicit `InvalidOperationException` branches with stable messages.
- [x] Add tests for invalid JSON, missing content/data, and failure telemetry branches.

### Task 4: Reduce Main Workbench Async Risk And Close Out

**Files:**
- Modify: `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ContentDeliveryStudio.App/Services/GalleryThumbnailWarmupService.cs`
- Modify: `tests/ContentDeliveryStudio.Tests/MainWindowViewModelTests.cs`

- [x] Replace important fire-and-forget startup and selection-triggered tasks with tracked safe-dispatch helpers.
- [x] Ensure stale refresh/load results are discarded without losing current selection behavior.
- [x] Keep gallery warmup best-effort, but observe and suppress its failures explicitly in one place.
- [x] Run focused tests for UI state flow, then `.\scripts\verify-repo.ps1 -NoRestore`, then `.\scripts\preflight-release.ps1 -NoRestore`.

## Closeout Notes

- `tests/ContentDeliveryStudio.Tests/GalleryThumbnailCacheTests.cs` and `tests/ContentDeliveryStudio.Tests/OpenAiProviderConfigurationTests.cs` were added to the final regression set during closeout because the atomic write helper was also adopted by thumbnail cache and DPAPI secret persistence.
- `src/ContentDeliveryStudio.App/Services/GalleryThumbnailWarmupService.cs` and `src/ContentDeliveryStudio.Infrastructure/OpenAI/OpenAiResponsesClient.cs` did not require direct code changes in this wave; the behavior-preserving hardening landed in `MainWindowViewModel` and the provider parsing layers around them instead.
- The original “prove failing first” intent was executed as a development discipline for the slice, but the durable repository evidence retained for closeout is the passing regression set plus the full gate runs required by repository policy.

## Verification Evidence

- `dotnet test .\tests\ContentDeliveryStudio.Tests\ContentDeliveryStudio.Tests.csproj --no-build --logger "console;verbosity=minimal"` -> `442 / 442` passing
- `.\scripts\verify-repo.ps1 -NoRestore` -> passing
- `.\scripts\preflight-release.ps1 -NoRestore` -> passing
