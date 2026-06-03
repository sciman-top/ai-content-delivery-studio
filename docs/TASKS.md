# Task Checklist

## Foundation

- [x] Create independent repository under `D:\CODE`.
- [x] Record product design.
- [x] Record architecture and stack decision.
- [x] Record official and community references.
- [x] Record roadmap and implementation plan.

## Phase 1: Core Model

- [x] Create .NET solution and projects.
- [x] Add WPF app targeting `net10.0-windows`.
- [x] Add `ImageSeriesStudio.Core` domain library.
- [x] Add `ImageSeriesStudio.Infrastructure` library.
- [x] Add test project.
- [x] Define domain entities and state machines.
- [x] Define provider contracts.
- [x] Add fake providers.
- [x] Add SQLite persistence.
- [x] Add workspace folder service.
- [x] Add delivery manifest model.
- [x] Add unit tests.

## Phase 2: UI MVP

- [x] Add Generic Host startup to WPF.
- [x] Add MVVM Toolkit.
- [x] Add workbench shell.
- [x] Add application layer project.
- [x] Add Chinese and English localization foundation.
- [x] Add language selection to shell.
- [x] Add project repository and application service foundation.
- [x] Add project creation and load/save.
- [x] Add Phase 2 detailed implementation plan.
- [x] Add project create/list UI foundation.
- [x] Add series and item editor foundation.
- [x] Add series and item table.
- [x] Add prompt version editor.
- [x] Add fake planning action.
- [x] Add queue panel.
- [x] Add candidate gallery.
- [x] Add review panel.
- [x] Add delivery export panel.

## Phase 3: OpenAI Integration

- [x] Add OpenAI provider configuration.
- [x] Store secrets outside repo.
- [x] Implement text planning provider.
- [x] Implement image generation provider.
- [x] Implement vision review provider.
- [x] Add provider capability validation.
- [x] Add dry-run and opt-in real API smoke tests.
- [x] Add cost estimate and quota guard.

## Phase 3A: Cloud-First Provider Hardening

- [x] Record cloud-first provider hardening design spec.
- [x] Record cloud-first provider hardening implementation plan.
- [x] Record ADR for cloud-first provider and tooling strategy.
- [x] Refresh research evidence for OpenAI Responses API, Microsoft resilience, OpenTelemetry, and Credential Locker.
- [x] Add Windows Credential Locker or DPAPI secret store adapter.
- [x] Replace environment-variable-only production secret retrieval.
- [ ] Evaluate and adopt the official OpenAI .NET SDK where the API surface is stable enough.
- [ ] Keep raw `HttpClient` fallback only for unsupported or lagging SDK surfaces.
- [x] Add `Microsoft.Extensions.Http.Resilience` to named provider clients.
- [ ] Capture request IDs, token usage, latency, and cost telemetry per provider call.
- [ ] Add OpenTelemetry instrumentation and a local OTLP/Aspire dashboard profile.
- [ ] Support Responses API multi-turn image state and partial-image streaming where the product benefits.
- [ ] Add a remote workflow-engine adapter boundary without requiring local model installs.
- [x] Run full build, test, and format gates for the implementation slice.

## Phase 4: Quality Loop

- [x] Add review rubric templates.
- [x] Add structured AI review output.
- [x] Add prompt repair suggestions.
- [x] Add prompt diff.
- [x] Add candidate comparison.
- [x] Add batch requeue by reason.
- [x] Add final approval workflow.

## Phase 4A: Deterministic Text Composition And Delivery Assurance

- [ ] Add deterministic post-render text composition service for educational or text-heavy visuals.
- [ ] Add readability, label, and callout-specific review checks.
- [ ] Persist human approval decisions and reviewer notes.
- [ ] Export final approval state in delivery manifests and review reports.
- [ ] Run full build, test, and format gates for the implementation slice.

## Phase 5: Sample Migration

- [x] Import the physics poster project as a sample.
- [x] Map existing prompt files to generic `SeriesItem` and `PromptVersion`.
- [x] Map finalized delivery files to `CandidateImage` and `ReviewResult`.
- [x] Validate manifest compatibility.
- [x] Document migration limits.

## Release Readiness

- [x] Add installer or packaged publish.
- [x] Add diagnostics export.
- [x] Add backup and restore.
- [x] Add accessibility review.
- [x] Add large-gallery performance review.
- [x] Add user guide.

## Phase 6: Advanced Workflows

- [x] Add provider-neutral parameter grid experiments.
- [x] Add reference image sets.
- [x] Document style and parameter governance.
- [x] Record ADR for style and parameter governance.
- [x] Add image type presets.
- [x] Add style guide domain model.
- [x] Add provider-neutral generation recipes.
- [x] Extend provider capabilities for output settings.
- [x] Validate generation recipes before queue execution.
- [x] Link parameter experiments to queue tasks and candidate review.
- [x] Add style library and recipe inspector UI.
- [x] Include style, recipe, reference, and experiment metadata in delivery packages.
- [x] Add mask/edit workflow foundation.
- [x] Add mask/edit UI controls.
- [x] Add workflow export/import.
- [x] Add optional graph view.

## Phase 7: Brief-First Image Generation

- [x] Record brief-first image generation design spec.
- [x] Record brief-first implementation plan.
- [x] Add `CreativeBrief` and `PromptDirection` domain records.
- [x] Persist creative briefs under image series.
- [x] Add fake-first prompt direction planning.
- [x] Add application service workflow to create briefs, generate directions, and promote directions.
- [x] Add minimal Brief tab UI and localization.
- [x] Run full build, test, and format gates for the implementation slice.

## Phase 7A: Preset Governance

- [x] Record preset governance design spec.
- [x] Record preset governance implementation plan.
- [x] Record preset governance evidence table.
- [x] Record preset governance ADR.
- [x] Add governance metadata to image type presets.
- [x] Add structured prompt direction recommendation model.
- [x] Add fake-first recommendation output.
- [x] Persist recommendations through the application service.
- [x] Use prompt direction recommendations as promotion default settings.
- [x] Show prompt direction recommendations in the Brief tab.
- [x] Add catalog invariant tests.
- [x] Run full build, test, and format gates for the preset governance slice.

## Phase 8: Document Illustration Workflow

- [x] Record document illustration workflow design spec.
- [x] Record document illustration implementation plan.
- [x] Add document illustration domain records for source text, target concepts, and target promotion.
- [x] Add article, concept, graphical abstract, and scholarly schematic presets.
- [x] Add document-specific review rubrics.
- [x] Add fake-first target planning and prompt preparation.
- [x] Add application workflow to create illustration briefs, generate targets, approve targets, and promote approved targets into the existing Plan/Prompts workflow.
- [x] Add persistence for document planning evidence.
- [x] Add document illustration UI entry, localization, and draft-mode guidance.
- [x] Add user documentation for document illustration workflow safety boundaries and first-run fake-provider behavior.
- [x] Run full build, test, and format gates for the implementation slice.
- [ ] Add real provider integration and binary document extraction in later slices.

## Phase 9: Blueprint-First Generalized Series Workflow

- [x] Record blueprint-first generalized series design spec.
- [x] Record blueprint-first implementation plan.
- [x] Add `DesignBlueprint` domain record and persistence.
- [x] Extend fake planning provider with blueprint candidates.
- [x] Add application workflow to create, compare, and promote blueprint routes.
- [ ] Add optional `SeriesItemKind` support for panel-like narrative items.
- [x] Expand Brief tab UI with blueprint cards and promotion actions.
- [ ] Route review outcomes back to brief, blueprint, prompt, or settings layers.
- [ ] Include blueprint metadata in delivery packages.
- [ ] Run full build, test, and format gates for the implementation slice.
