# Task Checklist

## Foundation

- [x] Create independent repository under `D:\CODE`.
- [x] Record product design.
- [x] Record architecture and stack decision.
- [x] Record official and community references.
- [x] Record roadmap and implementation plan.

## Phase 1: Core Model

- [ ] Create .NET solution and projects.
- [ ] Add WPF app targeting `net10.0-windows`.
- [ ] Add `ImageSeriesStudio.Core` domain library.
- [ ] Add `ImageSeriesStudio.Infrastructure` library.
- [ ] Add test project.
- [ ] Define domain entities and state machines.
- [ ] Define provider contracts.
- [ ] Add fake providers.
- [ ] Add SQLite persistence.
- [ ] Add workspace folder service.
- [ ] Add delivery manifest model.
- [ ] Add unit tests.

## Phase 2: UI MVP

- [ ] Add Generic Host startup to WPF.
- [ ] Add MVVM Toolkit.
- [ ] Add workbench shell.
- [ ] Add project creation and load/save.
- [ ] Add series and item table.
- [ ] Add prompt version editor.
- [ ] Add queue panel.
- [ ] Add candidate gallery.
- [ ] Add review panel.
- [ ] Add delivery export panel.

## Phase 3: OpenAI Integration

- [ ] Add OpenAI provider configuration.
- [ ] Store secrets outside repo.
- [ ] Implement text planning provider.
- [ ] Implement image generation provider.
- [ ] Implement vision review provider.
- [ ] Add provider capability validation.
- [ ] Add dry-run and opt-in real API smoke tests.
- [ ] Add cost estimate and quota guard.

## Phase 4: Quality Loop

- [ ] Add review rubric templates.
- [ ] Add structured AI review output.
- [ ] Add prompt repair suggestions.
- [ ] Add prompt diff.
- [ ] Add candidate comparison.
- [ ] Add batch requeue by reason.
- [ ] Add final approval workflow.

## Phase 5: Sample Migration

- [ ] Import the physics poster project as a sample.
- [ ] Map existing prompt files to generic `SeriesItem` and `PromptVersion`.
- [ ] Map finalized delivery files to `CandidateImage` and `ReviewResult`.
- [ ] Validate manifest compatibility.
- [ ] Document migration limits.

## Release Readiness

- [ ] Add installer or packaged publish.
- [ ] Add diagnostics export.
- [ ] Add backup and restore.
- [ ] Add accessibility review.
- [ ] Add large-gallery performance review.
- [ ] Add user guide.
