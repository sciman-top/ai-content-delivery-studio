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

## Phase 4: Quality Loop

- [x] Add review rubric templates.
- [x] Add structured AI review output.
- [x] Add prompt repair suggestions.
- [x] Add prompt diff.
- [x] Add candidate comparison.
- [x] Add batch requeue by reason.
- [x] Add final approval workflow.

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
- [ ] Add mask/edit workflow.
- [ ] Add workflow export/import.
- [ ] Add optional graph view.

## Phase 7: Brief-First Image Generation

- [x] Record brief-first image generation design spec.
- [x] Record brief-first implementation plan.
- [x] Add `CreativeBrief` and `PromptDirection` domain records.
- [x] Persist creative briefs under image series.
- [x] Add fake-first prompt direction planning.
- [ ] Add application service workflow to create briefs, generate directions, and promote directions.
- [ ] Add minimal Brief tab UI and localization.
- [ ] Run full build, test, and format gates for the implementation slice.

## Phase 8: Document Illustration Workflow

- [x] Add document illustration design spec.
- [x] Add document illustration implementation plan.
- [x] Add document illustration domain model.
- [ ] Add article, concept, graphical abstract, and scholarly schematic presets.
- [ ] Add document-specific review rubrics.
- [ ] Add fake document illustration planner.
- [ ] Add application workflow from approved targets to series items.
- [ ] Add persistence for document planning evidence.
- [ ] Add minimal WPF entry point.
