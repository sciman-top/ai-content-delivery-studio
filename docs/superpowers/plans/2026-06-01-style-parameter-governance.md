# Implementation Plan: Style And Parameter Governance

## Overview

This slice turns image type, style, references, generation settings, and experiments into provider-neutral, testable contracts. The goal is to prevent prompt/settings sprawl while keeping the app simple for normal users and extensible for advanced workflows.

## Architecture Decisions

- Keep OpenAI-specific request fields inside infrastructure adapters.
- Add domain objects before UI controls.
- Validate provider capabilities before queue execution.
- Preserve all style, recipe, reference, and experiment metadata with generated candidates and delivery packages.
- Use fake providers and dry-run gates before any real image API call.

## Task List

### Phase 6A: Governance Foundation

- [x] Task 1: Document style and parameter governance.
- [x] Task 2: Record ADR 0004.
- [ ] Task 3: Update research references after each major provider API change.

### Phase 6B: Domain Contracts

- [x] Task 4: Add `ImageTypePreset`.
  - Acceptance: presets include name, aspect ratio, output format, text policy, review rubric link, and delivery naming policy.
  - Verification: `dotnet test --filter ImageTypePresetTests`.
  - Dependencies: none.
  - Estimated scope: S.

- [x] Task 5: Add `StyleGuide`.
  - Acceptance: style guide records visual principles, palette, lighting, composition rules, negative constraints, reference set links, and version.
  - Verification: `dotnet test --filter StyleGuideTests`.
  - Dependencies: Task 4 optional.
  - Estimated scope: M.

- [x] Task 6: Add `GenerationRecipe`.
  - Acceptance: recipe records provider profile, model id, image type preset, dimensions, quality, format, background, compression, moderation, seed, and warnings.
  - Verification: `dotnet test --filter GenerationRecipeTests`.
  - Dependencies: Task 4.
  - Estimated scope: M.

### Checkpoint: Domain Foundation

- [x] `dotnet build`
- [x] `dotnet test --filter "ImageTypePresetTests|StyleGuideTests|GenerationRecipeTests"`
- [x] No real API call required.

### Phase 6C: Capability And Cost Validation

- [x] Task 7: Extend provider capabilities with output settings.
  - Acceptance: capabilities declare supported sizes, quality levels, formats, background modes, edit support, reference-image support, and cost hints.
  - Verification: `dotnet test --filter ProviderCapabilityValidatorTests`.
  - Dependencies: Task 6.
  - Estimated scope: M.

- [x] Task 8: Validate recipes before queueing.
  - Acceptance: unsupported size/quality/format/background/reference settings fail before task creation with actionable messages.
  - Verification: `dotnet test --filter GenerationQueueTests`.
  - Dependencies: Task 7.
  - Estimated scope: M.

### Phase 6D: Experiment-To-Review Loop

- [x] Task 9: Link parameter experiments to prompt versions and generation tasks.
  - Acceptance: each variant records base prompt, axes, selected recipe, generated task id, and stable slug.
  - Verification: `dotnet test --filter ParameterGridExperimentTests`.
  - Dependencies: Task 6.
  - Estimated scope: M.

- [x] Task 10: Add experiment comparison metadata to candidate review.
  - Acceptance: review can compare variants by axis values, score, hard failures, and selected final candidate.
  - Verification: `dotnet test --filter CandidateComparisonTests`.
  - Dependencies: Task 9.
  - Estimated scope: M.

### Phase 6E: UI And Export

- [x] Task 11: Add style library and recipe inspector to the WPF shell.
  - Acceptance: user can select a preset, style guide, and recipe for a project or item using fake providers.
  - Verification: `dotnet test`; manual WPF smoke with fake providers.
  - Dependencies: Tasks 4-8.
  - Estimated scope: M.

- [x] Task 12: Include style/recipe/reference/experiment metadata in delivery packages.
  - Acceptance: delivery manifest links final images to prompt version, recipe, style guide, reference sets, experiment axes, and review result.
  - Verification: `dotnet test --filter DeliveryPackageTests`.
  - Dependencies: Tasks 9-10.
  - Estimated scope: M.

### Checkpoint: Complete Slice

- [x] `dotnet build`
- [x] `dotnet test`
- [x] `dotnet format --verify-no-changes`
- [x] Fake-provider end-to-end flow covers preset -> style guide -> recipe -> generation -> review -> delivery.

## Risks And Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Too many settings too early | High | Start with presets, recipes, and bounded experiments before graph UI. |
| Provider drift | Medium | Keep capability mapping and warnings in provider adapters. |
| Hidden cost growth | High | Estimate cost from recipe settings and require opt-in for real calls. |
| Style-guide vagueness | Medium | Review against explicit rubric dimensions and preserve candidate metadata. |
| Metadata bloat | Medium | Store structured metadata and link large assets by workspace-relative path. |

## Open Questions

- Which presets should ship first beyond educational poster and article illustration?
- Should workflow export use one JSON package first, or a folder with manifest plus assets?
- Which provider settings need user-facing controls in MVP, and which remain adapter-only warnings?
