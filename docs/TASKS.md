# Task Checklist

## Current V1 Release Readout

Use [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md) as the truth source for current release claims. The latest recorded release-verification readout is:

- Latest recorded release-verification snapshot: `2026-06-11`
- Current readout: no open V1 release-claim gaps remain in that snapshot; all `5 / 5` launch metrics are verified, including the fresh opt-in OpenAI `2-item` sample under `artifacts/live-openai-v1-sample/20260611-132947`
- Reopen this as a release-gap section only when provider behavior materially changes, a newer launch snapshot is needed, or a regression invalidates the existing evidence
- Remaining non-release work is intentionally grouped into three lanes: conditional OCR reference coverage when scanned-document hardening enters the active roadmap, later real-provider plus support-matrix binary document extraction slices, and post-V1 repository/namespace rename compatibility work.

## Near-Term Hardening (Not Current Release Blockers)

These items are still valuable, but they are not the same thing as the current V1 release gap.

- [x] Harden the short requirement -> brief -> blueprint -> series -> review -> delivery path as the primary V1 launch route.
- [x] Harden the article or plain-text -> evidence anchors -> illustration targets -> promoted plan -> delivery path as a supporting validation route without requiring real providers by default.
- [x] Complete Phase 4A deterministic text composition, readability checks, reviewer notes, and approval evidence export.
- [x] Implement the provider routing policy defaults for Images API vs Responses API, structured outputs, and `store: false` by default.
- [x] Evaluate and adopt the official OpenAI .NET SDK where stable; keep raw `HttpClient` only for unsupported or lagging gaps.
- [x] Add a bounded transient `502 upstream_error` retry on the official SDK Images path before failing the live OpenAI route.
- [x] Add Responses API multi-turn image state only where it improves provenance or revision loops.
- [x] Add bounded local review-prep artifacts and review-batch thresholds before expanding multi-turn image-state review.
- [x] Run the first real low-risk operator adapter end-to-end with audit evidence and rollback notes.
- [x] Capture V1 launch evidence against the explicit launch metrics.
- [x] Encode and reuse the text-planning low-502 execution policy on the official SDK Responses text-planning path; future real-provider brief or blueprint planning must use this boundary when it leaves the current fake-first mode.

## V1 Documentation And Policy Alignment

- [x] Record V1 launch PRD.
- [x] Record provider routing policy.
- [x] Record operator risk policy.
- [x] Record source and artifact support matrix.
- [x] Record target engineering state.
- [x] Record external reference strategy.
- [x] Record V1 launch hardening implementation plan.
- [x] Align product design, roadmap, and task checklist to the V1 launch boundary.

## V1 Clarifications

- [x] Reconfirm the primary V1 audience as solo creator or teacher-like power user.
- [x] Reconfirm the short requirement -> image-series route as the only primary launch workflow.
- [x] Lock the first real operator slice to an additive local validation action.
- [x] Choose the deterministic text composition implementation library: `SkiaSharp`.
- [x] Keep packs internal-only for V1 and defer public sharing behavior.
- [x] Reflect the locked V1 defaults in implementation-facing code comments, options, and operator descriptors where relevant.
- [x] Reflect stateless local-direct visual review defaults in implementation-facing options and review operator descriptors.

## Engineering Workflow Governance

- [x] Record the repository-owned AI coding workflow v1.
- [x] Keep `docs/superpowers/specs/` and `docs/superpowers/plans/` as the long-lived engineering spec and plan surfaces for non-trivial slices.
- [x] Default to one agent completing one bounded slice; use subagents and worktrees only when independence or risk clearly justifies them.
- [x] Keep auto-execution layered: low-risk documentation and evidence sync may run automatically, while stronger contract, schema, or cross-surface changes still require explicit spec/plan plus fresh verification.

## Reference Governance

- [x] Record external reference strategy in project docs.
- [x] Establish `_shared` reference governance with manifest, update script, and duplicate audit script.
- [x] Add machine-readable manifest and update flow for `ai-content-delivery-studio-references`.
- [x] Extend `ai-coding-runtime-references` update flow to export a manifest.
- [x] Add a repository-local reference-evidence policy and verification gate for high-drift engineering areas.
- [x] Add a durable `reference-basis` mapping from code areas and task families to local references and reuse levels.
- [x] Move enforced reference-area logic to a machine-readable repository manifest.
- [x] Add a canonical local full-gate script that runs reference evidence checks before build, test, and format verification.
- [x] Add a GitHub Actions verification workflow that reuses the repository gate on normal `push` and `pull_request` events.
- [x] Add a stronger release-style preflight script that layers placeholder, merge-conflict, publish-dry-run, and diff-hygiene checks on top of the canonical gate.
- [x] Add machine-checked parity between `docs/REFERENCE_BASIS.md` and `scripts/reference-basis.json`.
- [x] Add a repo-side snapshot of the external reference shelf manifest and check it in local verification gates.
- [x] Add `dotnet/extensions` as a code-level reference before the next host/options/resilience hardening slice.
- [x] Add `opentelemetry-dotnet` as a code-level reference before the next telemetry or diagnostics slice.
- [x] Add `aspire` as a code-level reference before the next OTLP or local dashboard observability slice.
- [x] Add `SkiaSharp` reference-source coverage before the next deterministic composition expansion beyond the current poster proof path.
- [ ] Add OCR reference coverage such as `Tesseract` or `OCRmyPDF` only when scanned-document hardening enters the active near-term roadmap.
- [ ] Add scholarly PDF extraction references such as `GROBID` only when paper-figure evidence extraction enters the active near-term roadmap.
- [x] Decide whether `Cockpit-Tools-Local-references` should gain a machine-readable manifest next.

## Active Follow-Through Queue

- [x] Add real-provider execution follow-through for document illustration after the fake-first planning path is hardened.
- [x] Add targeted binary extraction hardening only for formats promoted by the support matrix.
- [ ] Add partial-image streaming UX only if a future workbench flow gains clear product value from progressive previews.
- [x] Add the first generic-scenario pack and policy modeling hardening slice with explicit scenario and policy-pack references.
- [x] Extend the stronger pack/policy contract to the built-in `article-illustration` scenario.
- [x] Extend the stronger pack/policy contract to the built-in `document-review-translation` scenario.
- [x] Extend the stronger pack/policy contract to the built-in `courseware-visual` scenario.
- [ ] Continue pack and policy modeling hardening for additional scenarios beyond generic image-series, article illustration, document-review translation, and courseware visual only when each slice has a repo-owned spec and a bounded implementation plan.

## Frozen Until Post-V1

- Physical repository and namespace rename work beyond documentation and planning.
- Broad pack-catalog widening beyond launch routes.
- Remote workflow-engine integrations beyond contract-boundary planning.
- Broad binary document automation beyond the support matrix.
- Browser or desktop operator flows that change third-party system state.

## Foundation

- [x] Create independent repository under `D:\CODE`.
- [x] Record product design.
- [x] Record architecture and stack decision.
- [x] Record official and community references.
- [x] Record roadmap and implementation plan.
- [x] Record product identity and repository rename path.

## Phase 1: Core Model

- [x] Create .NET solution and projects.
- [x] Add WPF app targeting `net10.0-windows`.
- [x] Add `ContentDeliveryStudio.Core` domain library.
- [x] Add `ContentDeliveryStudio.Infrastructure` library.
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
- [x] Add `.env` secret store fallback for local provider credentials.
- [x] Add separated text/image provider environment configuration with image key-pool concurrency validation.
- [x] Enforce role-scoped provider options so image-only keys cannot be used for text or vision calls, while allowing the image provider to fall back to `TEXT_PROVIDER_API_KEY` in the default single-key configuration path.
- [x] Add non-generating `/v1/models` health checks for text providers and image key pools.
- [x] Add Provider Center configuration summary model/view-model without exposing secret values.
- [x] Add Provider Center manual health summary state for mixed text/image key-pool results.
- [x] Evaluate and adopt the official OpenAI .NET SDK where the API surface is stable enough.
- [x] Keep raw `HttpClient` fallback only for unsupported or lagging SDK surfaces.
- [x] Add `Microsoft.Extensions.Http.Resilience` to named provider clients.
- [x] Capture request IDs, token usage, latency, and cost telemetry per provider call.
- [x] Add OpenTelemetry instrumentation and a local OTLP/Aspire dashboard profile.
  - [x] Add .NET `ActivitySource` and `Meter` instrumentation for provider calls.
  - [x] Add local OTLP/Aspire dashboard profile.
- [x] Support opt-in Responses API multi-turn image state where the product benefits and where the provider routing policy calls for it.
- [ ] Add partial-image streaming UX only if a future workbench flow gains clear product value from progressive previews.
- [x] Add a remote workflow-engine adapter boundary without requiring local model installs.
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

Priority note: this phase is now part of the near-term golden-path hardening slice, not a distant quality add-on.

- [x] Add `SkiaSharp`-based deterministic composition foundation for labels, formulas, legends, and callouts.
- [x] Add deterministic post-render text composition service for educational or text-heavy visuals.
- [x] Add readability, label, and callout-specific review checks.
- [x] Persist human approval decisions and reviewer notes.
- [x] Export final approval state in delivery manifests and review reports.
- [x] Run full build, test, and format gates for the implementation slice.

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
- [x] Add real provider integration and support-matrix-approved binary document extraction in later slices.

## Phase 9: Blueprint-First Generalized Series Workflow

- [x] Record blueprint-first generalized series design spec.
- [x] Record blueprint-first implementation plan.
- [x] Add `DesignBlueprint` domain record and persistence.
- [x] Extend fake planning provider with blueprint candidates.
- [x] Add application workflow to create, compare, and promote blueprint routes.
- [x] Add optional `SeriesItemKind` support for panel-like narrative items.
- [x] Expand Brief tab UI with blueprint cards and promotion actions.
- [x] Route review outcomes back to brief, blueprint, prompt, or settings layers.
  - [x] Add provider-neutral review outcome routing model and application service entrypoint.
  - [x] Surface routing decisions in review and repair UI.
  - [x] Apply routed repair actions back to prompt and settings records by creating a new prompt version.
  - [x] Add non-destructive Brief/Blueprint repair patch proposals that require human approval before record mutation.
  - [x] Persist Brief/Blueprint repair patch proposals on the project aggregate and SQLite repository.
  - [x] Include persisted repair patch proposals in diagnostics evidence.
  - [x] Apply routed repair actions back to brief and blueprint records.
- [x] Include blueprint metadata in delivery packages.
- [x] Run full build, test, and format gates for the implementation slice.

## Phase 10: Multimodal Source And Artifact Foundation

- [x] Record ADR for multimodal content delivery, workflow packs, and AI operator boundaries.
- [x] Record multimodal source/artifact implementation plan.
- [x] Add `SourceAsset`, `ExtractedContent`, and `EvidenceAnchor` domain records.
- [x] Add `OutputArtifact`, `ArtifactManifest`, and `ArtifactPackage` domain records.
- [x] Add fake-first source ingestion service with file metadata and text fixtures.
- [x] Add source evidence persistence with backward-compatible project loading.
- [x] Add document extraction provider boundary for PDF, DOCX, PPTX, markdown, image, and OCR results.
- [x] Add artifact planning use case that can plan image, PDF, DOCX, markdown, and review-report outputs from a brief.
- [x] Extend delivery manifest with source evidence and output artifact provenance.
- [x] Keep existing image-series delivery export compatible with the new artifact model.
- [x] Run build, test, and format gates for the implementation slice.

## Phase 11: Workflow, Blueprint, And Industry Pack System

- [x] Add `WorkflowPack`, `BlueprintPack`, `IndustryPack`, `RendererPack`, and `ReviewRubricPack` metadata records.
- [x] Add pack semantic version, compatibility range, deprecation state, and migration notes.
- [x] Add local pack registry and validation service.
- [x] Add built-in generic image-series pack.
- [x] Add built-in article illustration pack.
- [x] Add built-in document review/translation pack.
- [x] Add built-in courseware visual pack.
- [x] Add built-in poster/report delivery pack.
- [x] Add pack import/export with fake execution and validation.
- [x] Add `WorkflowStageDefinition` metadata with stable stage IDs and completion criteria.
- [x] Add pack-driven UI defaults without leaking pack-specific vocabulary into core entities.
- [x] Add validation that packs cannot introduce permanent global tabs without an explicit shell decision.
- [x] Add catalog invariant tests for pack IDs, compatibility, and migrations.
- [x] Run build, test, and format gates for the implementation slice.

## Phase 12: Modular Maintenance And Use Case Split

- [x] Define module folders for source ingestion, artifact planning, pack registry, repair routing, and tool adapters.
  - [x] Guard built-in module folder declarations against stale repository paths.
- [x] Define reusable `WorkflowViewSlot` names for source list, stage workspace, inspector, activity panel, approval panel, and artifact preview.
- [x] Add `FeatureViewModule` contract for WPF view, view model, localization keys, commands, and fake-service tests.
- [x] Split `MainWindowViewModel` by workflow tab or feature module as new slices touch existing UI.
  - [x] Extract project workspace command orchestration into `ProjectWorkspaceCoordinator` while preserving existing bindings and commands.
  - [x] Extract shell-inspector project creation, provider-center, document planning, and image-edit orchestration into `WorkbenchInspectorCoordinator` while preserving existing bindings and inspector behavior.
  - [x] Extract planning and document-planning orchestration into `PlanningWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract brief-tab workflow orchestration into `BriefWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract generation/gallery workflow orchestration into `GenerationWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract review/approval workflow orchestration into `ReviewWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract delivery export orchestration into `DeliveryWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract plan editor command orchestration into `PlanEditorWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract workflow graph row construction into `WorkflowGraphCoordinator` while preserving existing bindings and graph output.
  - [x] Extract workbench projection building into `ProjectWorkbenchProjectionCoordinator` while preserving plan, prompt, gallery, review, and reload output.
  - [x] Extract workbench load/clear state composition into `ProjectWorkbenchStateCoordinator` while preserving plan, prompt, gallery, review, delivery, and active-brief selection behavior.
  - [x] Extract selection-summary display building into `MainWindowSelectionSummaryCoordinator` while preserving item-title, style-recipe, and candidate-summary behavior.
  - [x] Extract current-project summary display building into `MainWindowSelectionSummaryCoordinator` while preserving empty-state and timestamp formatting behavior.
  - [x] Extract document default/strictness localization restoration into `MainWindowLocalizationCoordinator` while preserving user-entered text and educational fallback behavior.
  - [x] Extract shell localization payload building into `MainWindowLocalizationCoordinator` while preserving language-switch behavior and selected-option restoration.
  - [x] Extract localized selection and option restoration into `MainWindowLocalizationCoordinator` while preserving language-switch behavior and current inspector selections.
- [x] Split large WPF views into feature-owned user controls where needed.
  - [x] Extract the brief-tab blueprint list into `BlueprintRoutesView` while preserving existing bindings and selection behavior.
  - [x] Extract the brief-tab prompt-direction list into `PromptDirectionsView` while preserving existing bindings and selection behavior.
  - [x] Extract the review-tab results list into `ReviewResultsListView` while preserving existing bindings and selection behavior.
  - [x] Extract the delivery-tab results list into `DeliveryResultsListView` while preserving existing bindings and output display behavior.
  - [x] Extract the brief-tab actions bar into `BriefWorkflowActionsView` while preserving create, blueprint, and prompt-direction command bindings.
  - [x] Extract the brief-tab blueprint panel into `BlueprintRoutesPanelView` while preserving section header and blueprint-list composition.
  - [x] Extract the brief-tab prompt-directions panel into `PromptDirectionsPanelView` while preserving section header and prompt-direction-list composition.
  - [x] Extract the review-tab header into `ReviewHeaderView` while preserving review column bindings.
  - [x] Extract the delivery-tab header into `DeliveryHeaderView` while preserving delivery column bindings.
  - [x] Extract the plan-tab header into `PlanHeaderView` while preserving plan column bindings.
  - [x] Extract the prompts-tab header into `PromptsHeaderView` while preserving prompt column bindings.
  - [x] Extract the queue-tab header into `QueueHeaderView` while preserving queue column bindings.
  - [x] Extract the gallery-tab header into `GalleryHeaderView` while preserving gallery column bindings.
  - [x] Extract the workflow-graph header into `WorkflowGraphHeaderView` while preserving graph column bindings.
  - [x] Extract the inspector provider-center panel into `ProviderCenterPanelView` while preserving configuration summary, health rows, and refresh/test bindings.
  - [x] Extract the inspector project setup panel into `ProjectSetupPanelView` while preserving project creation, current-project summary, and project-list selection bindings.
  - [x] Extract the inspector style-recipe panel into `StyleRecipeInspectorPanelView` while preserving preset, guide, recipe selection, and summary bindings.
  - [x] Extract the inspector fake-planning panel into `FakePlanningPanelView` while preserving planning input and run-command bindings.
  - [x] Extract the inspector document-illustration panel into `DocumentIllustrationPanelView` while preserving source text, strictness, run-command, and result bindings.
  - [x] Extract the plan-tab rows list into `PlanRowsListView` while preserving plan row visibility and row bindings.
  - [x] Extract the prompts-tab rows list into `PromptRowsListView` while preserving prompt row visibility and row bindings.
  - [x] Extract the queue-tab rows list into `QueueRowsListView` while preserving queue row visibility and row bindings.
  - [x] Extract the gallery-tab rows list into `GalleryRowsListView` while preserving gallery row visibility and selected-row bindings.
  - [x] Extract the workflow-graph rows list into `WorkflowGraphRowsListView` while preserving graph row visibility and row bindings.
  - [x] Extract the workflow graph tab content into `WorkflowGraphView` while preserving existing bindings and graph output.
  - [x] Extract the delivery tab content into `DeliveryView` while preserving existing bindings and delivery output.
  - [x] Extract the review tab content into `ReviewView` while preserving existing bindings and review output.
  - [x] Extract the queue tab content into `QueueView` while preserving existing bindings and queue output.
  - [x] Extract the gallery tab content into `GalleryView` while preserving existing bindings and gallery selection output.
  - [x] Extract the inspector side panel into `WorkbenchInspectorView` while preserving provider center, project setup, planning, document illustration, and review-approval bindings.
  - [x] Extract the workspace navigation column into `WorkspaceNavigationView` while preserving localized shell labels and navigation rows.
  - [x] Extract the bottom activity footer into `ActivityPanelView` while preserving activity summaries and shell layout behavior.
  - [x] Extract the central tab host into `WorkbenchTabHostView` while preserving workflow view placement, empty-state rules, and tab binding behavior.
- [x] Split `ProjectApplicationService` into focused use-case services for sources, briefs, blueprints, queue, review/repair, operator, and delivery.
  - [x] Extract project create/load/list workflow methods into `ProjectWorkspaceApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract series, item, prompt, and fake planning workflow methods into `SeriesWorkflowApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract review/repair routing and Prompt/Settings repair application into `ReviewRepairApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract delivery export into `DeliveryApplicationService` while preserving the existing facade entrypoint.
  - [x] Extract document illustration planning into `DocumentIllustrationApplicationService` while preserving the existing facade entrypoint.
  - [x] Extract brief, prompt-direction, and design-blueprint workflow methods into `BriefWorkflowApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract generation queue and fake image-edit workflow methods into `GenerationWorkflowApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract fake vision review and final approval workflow methods into `ReviewWorkflowApplicationService` while preserving the existing facade entrypoints.
- [x] Move provider configuration and capability validation out of UI-facing view models.
- [x] Move persistence configuration into infrastructure-owned modules.
  - [x] Move `RoutedRepairPatch` persistence mapping into an infrastructure configuration class.
  - [x] Move `CreativeBrief` persistence mapping into an infrastructure configuration class.
  - [x] Move `DocumentBrief` and `IllustrationPlan` persistence mappings into infrastructure configuration classes.
  - [x] Move `SourceAsset`, `OutputArtifact`, and `ArtifactPackage` persistence mappings into infrastructure configuration classes.
  - [x] Move `ReviewRubric` and `ReviewResult` persistence mappings into infrastructure configuration classes.
  - [x] Move project, series, item, prompt, generation, candidate, delivery, and provider mappings into infrastructure configuration classes.
- [x] Split EF Core mappings into `IEntityTypeConfiguration<T>` as model count grows.
  - [x] Extract the first `IEntityTypeConfiguration<T>` slice for `RoutedRepairPatch`.
  - [x] Extract the `CreativeBrief` `IEntityTypeConfiguration<T>` slice while preserving prompt direction and blueprint JSON reload behavior.
  - [x] Extract document illustration, source asset, and artifact packaging `IEntityTypeConfiguration<T>` slices with focused SQLite reload tests.
  - [x] Add a focused SQLite reload test before extracting quality-loop review rubric/result mappings.
  - [x] Remove inline `modelBuilder.Entity<T>` mapping blocks from `AppDbContext`.
- [x] Add focused tests for each extracted use-case service before expanding UI surface.
  - [x] Cover `ProjectWorkspaceApplicationService` directly while keeping facade workflow tests.
  - [x] Cover `SeriesWorkflowApplicationService` directly while keeping facade workflow tests.
  - [x] Add focused delivery application service tests for registered and missing writer paths.
  - [x] Cover `DocumentIllustrationApplicationService` directly while keeping facade workflow tests.
  - [x] Cover `BriefWorkflowApplicationService` directly while keeping facade workflow tests.
  - [x] Cover `GenerationWorkflowApplicationService` directly while keeping facade workflow tests.
  - [x] Cover `ReviewWorkflowApplicationService` directly while keeping facade workflow tests.
- [x] Keep each refactor slice behavior-preserving and tied to a new feature or touched old logic.
- [x] Run build, test, and format gates after each module split.

## Phase 13: Review, Repair, And Operator Automation

- [x] Add structured `RepairPlan` model from `ReviewResult` findings.
- [x] Add `OperatorAction` and `OperatorRun` audit records.
- [x] Add tool adapter contract with risk level, dry-run support, inputs, outputs, side effects, timeout, approval requirement, and cleanup path.
- [x] Add SDK/CLI/local library adapter boundary for deterministic tools.
- [x] Add browser automation adapter boundary for web workflows.
- [x] Add Windows desktop automation adapter boundary for future UI automation.
- [x] Add computer-use provider boundary for model-guided UI action planning.
- [x] Add local tool registry for extraction, conversion, OCR, ImageMagick/FFmpeg processing, deterministic composition, and artifact validation.
- [x] Add approval gate for medium/high-risk operator actions.
- [x] Add low-risk auto-repair path for safe local validation or file-generation tasks.
- [x] Add operator audit export into diagnostics and delivery evidence where appropriate.
- [x] Run the first real low-risk operator adapter end-to-end with audit evidence and rollback notes.
  - [x] Recommended first slice: local delivery or artifact validation report generation into a new diagnostics folder.
- [x] Run build, test, and format gates for the implementation slice.

## Phase 14: Product Identity And Repository Rename

- [x] Record ADR for `AI Content Delivery Studio` product identity and staged rename path.
- [x] Update product-facing README, product design, architecture, roadmap, and user guide naming.
- [x] Update WPF app title localization to `AI Content Delivery Studio` / `AI 内容交付工作台`.
- [x] Post-V1: rename local root directory from `D:\CODE\ai-image-series-studio` to `D:\CODE\ai-content-delivery-studio` after confirming a clean worktree and no active tools depend on the old path.
- [x] Post-V1: reopen the workspace from `D:\CODE\ai-content-delivery-studio` and verify `git status --short`.
- [x] Post-V1: rename solution, project folders, project names, assemblies, namespaces, tests, scripts, and publish output from `ImageSeriesStudio.*` to `ContentDeliveryStudio.*`.
- [x] Post-V1: preserve compatibility notes for existing workspaces, diagnostics packages, and historical documents that still mention `ImageSeriesStudio`.
- [x] Post-V1: run full rename gate: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, and a targeted search for unintended old-name references.
