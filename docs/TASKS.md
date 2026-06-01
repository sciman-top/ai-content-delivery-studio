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

- [ ] Add installer or packaged publish.
- [x] Add diagnostics export.
- [x] Add backup and restore.
- [ ] Add accessibility review.
- [ ] Add large-gallery performance review.
- [ ] Add user guide.
