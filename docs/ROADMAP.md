# Roadmap

Target upgrade: the product is moving from a generalized image-series workbench to a multimodal content delivery workbench with image-series production as the core capability. The stable architecture is `Windows local workbench + replaceable cloud AI providers + local deterministic toolchain + versioned Workflow/Blueprint packs`.

AI, providers, workflow packs, and output formats can evolve quickly. Core domain models and application use cases should evolve slowly and should not be reshaped for every model release.

## Phase 0: Product And Architecture Foundation

Status: complete for initial design.

Deliverables:

- Product design.
- Architecture decision records.
- External reference research.
- Roadmap and implementation plan.
- New independent git repository.

## Phase 1: Core Model And Local Project Format

Goal: make the workflow real without calling paid APIs.

Deliverables:

- .NET solution with WPF app, core, infrastructure, and tests.
- Domain model for project, series, items, prompt versions, tasks, candidates, reviews, and delivery.
- SQLite schema and migrations.
- Local workspace folder convention.
- Fake text, image, and vision providers.
- Unit tests for state transitions and manifest generation.

Exit gate:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Phase 2: MVP Workbench UI

Goal: edit a project end-to-end using fake providers.

Deliverables:

- Localization foundation with selectable `zh-CN` / `en-US`.
- Application layer for use-case orchestration outside WPF.
- Workbench shell with Brief, Plan, Prompts, Queue, Gallery, Review, and Delivery tabs.
- Project creation and save/load.
- Series and item table editing.
- Prompt editor with version history.
- Queue view with task states.
- Candidate gallery using generated sample images or placeholders.
- Review panel with structured rubric.

Exit gate:

- UI can complete the full loop with fake providers.
- Language can be switched between Chinese and English for shell labels and user-facing workflow summaries.
- No paid API call is required.

## Phase 3: OpenAI Provider Integration

Goal: integrate real OpenAI APIs safely.

Deliverables:

- OpenAI text planning provider.
- OpenAI image generation provider.
- OpenAI vision review provider.
- Capability discovery and validation.
- Cost estimate display.
- Per-provider concurrency and retry policy.
- Real API smoke tests gated behind explicit local opt-in.

Exit gate:

- A 2-item sample series can be planned, generated, reviewed, revised, and delivered.
- API keys never appear in logs, git status, or delivery manifests.

## Phase 3A: Cloud-First Provider Hardening

Goal: make the real-provider path robust on low-hardware Windows machines without requiring local model installs.

Status: started with DPAPI and `.env` secret resolution, split text/image provider profiles, image key-pool validation, role-scoped provider operation guards, non-generating provider health checks, Provider Center summaries, operation-scoped provider options, resilient HTTP clients, safe provider call telemetry capture for request IDs, token usage, latency, and configured cost estimates, plus .NET `ActivitySource`/`Meter` instrumentation hooks for provider tracing and metrics and a local Aspire Dashboard launch profile for OTLP export.

Deliverables:

- Cloud-first provider strategy recorded in ADR and implementation plan.
- Official OpenAI workflow split between direct Image API and stateful Responses API where appropriate.
- Windows Credential Locker or DPAPI-backed secret storage for production paths.
- Local `.env` fallback and separated text/image provider environment profiles for official, OpenAI-compatible, and third-party image-only services.
- Image provider key-pool concurrency validation for multi-key batch generation.
- Role-scoped provider operation guards so image-only keys cannot be used for text or vision calls.
- Non-generating `/v1/models` health checks for text providers and image key pools.
- Provider Center configuration summary model/view-model that redacts secret values before UI binding.
- Provider Center manual health summary state for text providers and mixed image key-pool results.
- `Microsoft.Extensions.Http.Resilience` integration for named provider clients.
- Provider request ID, latency, token, and cost telemetry capture.
- OpenTelemetry-based traces and metrics for provider calls and queue execution.
- Streaming and multi-turn image workflow support where provider capabilities allow it.
- Remote workflow-engine adapter boundary for optional managed or hosted integrations.

Exit gate:

- A low-hardware Windows machine can run the app with no local model runtime installed.
- A 2-item sample series can complete the real-provider loop through opt-in cloud APIs.
- Retries, timeouts, redaction, and request provenance are verified.

## Phase 4: Review And Regeneration Loop

Goal: make quality iteration strong enough for serious series production.

Deliverables:

- Rubric templates by use case.
- AI-generated repair suggestions.
- Prompt diff and candidate comparison.
- Batch requeue by failure reason.
- Human final approval workflow.
- Review report export.

Exit gate:

- User can select unsatisfactory images, revise prompts, regenerate, and preserve all history.

## Phase 4A: Deterministic Text Composition And Delivery Assurance

Goal: make text-heavy educational, document, and poster outputs reliable even when image-model text rendering is imperfect.

Deliverables:

- Deterministic post-render text composition service for labels, legends, formulas, and callouts.
- Readability and text-placement review checks.
- Persisted human approval state with reviewer and notes.
- Delivery manifests and review reports that include final approval evidence.

Exit gate:

- A text-heavy visual can be produced as image background plus deterministic text overlay.
- Review can distinguish visual-scene success from text-layout failure.
- Delivery export preserves final approval evidence and text-composition provenance.

## Phase 5: Delivery Packaging And Physics Project Migration

Goal: prove the app on the existing physics poster case.

Deliverables:

- Importer for the physics poster prompt and final delivery structure.
- Delivery package builder.
- Manifest validation.
- Prompt snapshot export.
- Candidate and final image provenance view.

Exit gate:

- The current physics poster project can be represented as a read-only imported sample.

## Phase 6: Advanced Workflows

Goal: support broader common image-generation scenarios.

Status: started with provider-neutral parameter grid experiments, reference image sets, image type presets, style guides, generation recipes, provider capability validation, delivery metadata, and a WPF style/recipe inspector.

Deliverables:

- Parameter grid experiments.
- Reference image sets.
- Image type presets.
- Style guide library.
- Provider-neutral generation recipes.
- Provider capability validation for output settings.
- Experiment-to-review comparison flow.
- Mask/edit workflow.
- Workflow export/import.
- Provider plugin boundary.
- Optional graph view inspired by node workflow tools.

## Phase 9: Blueprint-First Generalized Series Workflow

Goal: support common image-series use cases through reusable design blueprints instead of topic-specific modes.

Status: started with persisted design blueprint candidates, blueprint promotion, review routing across brief/blueprint/prompt/settings layers, review-panel route visibility, routed Prompt/Settings repair application that creates a new prompt version instead of overwriting history, persisted non-destructive Brief/Blueprint repair patch proposals that require human approval before record mutation, applied Brief/Blueprint repair records, and diagnostics evidence for those repair patch proposals.

Deliverables:

- Design blueprint domain model and persistence.
- Fake-first blueprint candidate generation.
- Blueprint promotion workflow in the Brief tab.
- Generic support for panel-like series items without creating a second isolated comic subsystem.
- Review routing that can send failures back to brief, blueprint, prompt, or settings layers.
- Blueprint metadata included in delivery packages.

Exit gate:

- A short requirement can become a brief, then several blueprint candidates, then a promoted image-series plan, and finally the existing prompt/generation/review loop.
- The same workflow can support at least three generalized routes such as poster series, article illustration pack, and panel narrative sequence.

## Phase 10: Multimodal Source And Artifact Foundation

Goal: make user files and non-image deliverables first-class without weakening the image-series core workflow.

Status: started with source assets, extracted content, evidence anchors, output artifacts, artifact packages, fake ingestion, fake extraction, fake artifact planning, delivery provenance metadata, and SQLite persistence.

Deliverables:

- `SourceAsset`, `ExtractedContent`, and `EvidenceAnchor` domain model.
- `OutputArtifact`, `ArtifactPackage`, and artifact manifest model.
- Source ingestion service with fake-first file metadata and text fixtures.
- Document extraction boundary for PDF, DOCX, PPTX, markdown, images, and OCR outputs.
- Artifact planning use case that turns a brief and evidence anchors into images, PDF, DOCX, review report, or mixed delivery package targets.
- Delivery manifest extension for source evidence and output artifact provenance.

Exit gate:

- A sample source file fixture can become extracted content, a brief, an artifact plan, and a delivery package record without calling a real provider.
- Existing image-series projects continue to load and export.

## Phase 11: Workflow, Blueprint, And Industry Pack System

Goal: make generalization come from versioned packs instead of hard-coded topic modes.

Status: complete for the local pack foundation: provider-neutral pack metadata, semantic version, compatibility range, lifecycle state, migration notes, local registry validation, built-in starter packs, workflow stage definitions with completion criteria, pack-driven UI defaults using stable view slots, catalog invariant tests, and validated local JSON pack import/export.

Deliverables:

- `WorkflowPack`, `BlueprintPack`, `IndustryPack`, `RendererPack`, and `ReviewRubricPack` metadata schema.
- Local pack registry with semantic version, compatibility range, deprecation state, and migration notes.
- Built-in starter packs for generic image series, article illustration, document review/translation, courseware visual pack, and poster/report delivery.
- Pack import/export with validation and fake execution.
- Pack-declared workflow stages using a small stable vocabulary: `Source`, `Brief`, `Plan`, `Produce`, `Review`, `Repair`, `Deliver`.
- Pack-driven UI defaults that do not leak pack-specific vocabulary into core entities.

Exit gate:

- A new pack can be added without changing core entity types.
- A deprecated pack can still open old projects through compatibility metadata or migration notes.
- A pack can select visible workflow stages without adding permanent global tabs.

## Phase 12: Modular Maintenance And Use Case Split

Goal: prevent the app from growing a central all-knowing orchestrator as capabilities expand.

Status: started with a tested application module catalog for source ingestion, artifact planning, pack registry, repair routing, and tool adapters with repository-folder existence guards, stable workflow view slot names for reusable shell placement, a `FeatureViewModule` contract for future WPF view/view model splits, focused `ProjectApplicationService` extractions for review/repair routing, delivery export, and document illustration planning while preserving facade compatibility, a follow-up provider center configuration/health service split out of the UI view model, and completed `IEntityTypeConfiguration<T>` persistence mapping splits so `AppDbContext` no longer carries inline entity mapping blocks.

Deliverables:

- Split WPF tabs into feature-owned views and view models where the current shell is becoming too large.
- Add reusable `WorkflowViewSlot` and `FeatureViewModule` concepts for source lists, stage workspace, inspector, activity panel, approval panel, and artifact preview.
- Split `ProjectApplicationService` into focused use-case services for sources, briefs, blueprints, queue, review/repair, operator, and delivery.
- Split provider configuration, secret storage, capability validation, and persistence configuration away from UI code.
- Split EF Core mappings into `IEntityTypeConfiguration<T>` as model count grows.
- Establish module folders and tests for source ingestion, artifact planning, pack registry, repair routing, and tool adapters.

Exit gate:

- A new source/artifact feature can be implemented in one module with fake providers and focused tests.
- Existing central view model or application service logic is reduced only when touched by the new slice.

## Phase 13: Review, Repair, And Operator Automation

Goal: let AI replace repetitive human judgment and tool operation while preserving approval and audit boundaries.

Status: started with a structured `RepairPlan` model generated from review routing evidence, `OperatorAction` and `OperatorRun` audit records, a provider-neutral tool adapter contract covering SDK, CLI, local library, browser automation, Windows desktop automation, and computer-use boundaries, built-in local tool descriptors for extraction, conversion, OCR, ImageMagick/FFmpeg processing, deterministic text composition, and artifact validation, an approval gate for medium/high-risk operator actions, a low-risk auto-repair path through the adapter contract, and operator audit export in diagnostics/delivery evidence. Real tool execution remains a future slice.

Deliverables:

- Structured `ReviewResult -> RepairPlan -> OperatorAction` flow.
- Tool adapter contracts for SDK, CLI, local library, browser automation, desktop automation, and computer-use planning.
- Risk model with dry-run support, allow lists, human approval gates, timeout, audit log, and rollback or cleanup metadata.
- Local adapters for document conversion, OCR, ImageMagick/FFmpeg processing, deterministic text composition, and artifact validation.
- Browser automation adapter for web-only workflows when no API/CLI path exists.
- Desktop automation adapter boundary for future Windows UI automation.

Exit gate:

- Low-risk local repair actions can run automatically with audit evidence.
- Medium/high-risk actions pause at the point of risk and require explicit approval or handoff.
- Review no longer stops at comments; it produces a runnable or user-approvable repair path.

## Phase 7: Product Hardening

Goal: make it reliable as a daily Windows tool.

Deliverables:

- Installer or packaged release.
- Crash-safe queue recovery.
- Backup and restore.
- Accessibility pass.
- Performance pass for large galleries.
- Structured logs and diagnostics bundle.
- Documentation and sample projects.

## Long-Term Best Engineering End State

Goal: evolve from MVP workbench to a durable local production studio.

Deliverables:

- Clean application layer with command/query use cases and repository ports.
- Cloud-first provider adapters with official SDKs where practical, resilient HTTP execution, secure local secret storage, and full request provenance.
- Versioned design blueprints and workflow templates, then optional workflow graph import/export.
- Versioned workflow, blueprint, industry, renderer, and review packs.
- First-class source assets, extracted content, evidence anchors, output artifacts, and artifact packages.
- Review, repair, and operator automation with risk-aware approval and audit records.
- Provider plugin boundary with capability discovery and contract tests.
- Localized prompt templates, review reports, delivery manifests, and user guide.
- Deterministic text composition for text-heavy educational and document-oriented visuals.
- Local deterministic extraction, conversion, rendering, validation, and packaging tool adapters.
- Large-gallery virtualization, thumbnail cache, crash-safe queue recovery, backup/restore, and diagnostics bundle.
- Packaged Windows release with accessibility and performance gates.
