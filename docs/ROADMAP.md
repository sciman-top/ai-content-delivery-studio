# Roadmap

Target upgrade: the product is now positioned as AI Content Delivery Studio, a multimodal content delivery workbench with image-series production as the core capability. The stable architecture is `Windows local workbench + replaceable cloud AI providers + local deterministic toolchain + versioned Workflow/Blueprint packs`.

AI, providers, workflow packs, and output formats can evolve quickly. Core domain models and application use cases should evolve slowly and should not be reshaped for every model release.

## Planning Readout

- Historical phase numbers capture how major slices were introduced. They are not the recommended remaining execution order.
- `Status: complete for initial design` or `complete for the local pack foundation` means the design baseline or fake-first/local foundation exists.
- A workflow is user-visible complete only when it can run end-to-end in the workbench with review and delivery.
- A workflow is production-ready only when approval evidence, deterministic rendering or composition where required, and real-provider behavior are verified.
- Current V1 release-claim truth lives in [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md); roadmap phase status and completed backlog items do not count as launch proof by themselves.
- As of the latest recorded launch snapshot on `2026-06-11`, that evidence file closes all `5 / 5` V1 launch metrics. This does not mean every roadmap phase is complete or that future release snapshots never need refresh.

## V1 Release Frame

The launch boundary is narrower than the long-term multimodal product boundary.

- Primary launch route: short requirement -> brief -> blueprint -> series -> review -> delivery.
- Supporting validation route: article or plain text -> evidence anchors -> illustration targets -> promoted plan -> same downstream delivery flow.
- Proof path: text-heavy educational poster -> deterministic text composition -> approval evidence export.

V1 should ship these routes well before it widens the public pack surface, binary document ambitions, remote workflow integrations, or rename program.

Authoritative launch-boundary docs:

- [DOCUMENTATION_GOVERNANCE.md](./DOCUMENTATION_GOVERNANCE.md)
- [PRD_V1.md](./PRD_V1.md)
- [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md)
- [OPERATOR_RISK_POLICY.md](./OPERATOR_RISK_POLICY.md)
- [SOURCE_ARTIFACT_SUPPORT_MATRIX.md](./SOURCE_ARTIFACT_SUPPORT_MATRIX.md)
- [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md)
- [TARGET_ENGINEERING_STATE.md](./TARGET_ENGINEERING_STATE.md)
- [EXTERNAL_REFERENCE_STRATEGY.md](./EXTERNAL_REFERENCE_STRATEGY.md)
- [docs/superpowers/plans/2026-06-07-v1-launch-hardening.md](./superpowers/plans/2026-06-07-v1-launch-hardening.md)

## Main Issues To Control

- Product scope can widen faster than launch evidence.
- Provider routing can drift unless the policy is implemented, not only documented.
- Deterministic text composition is a required proof path; automated repo evidence now exists, but stronger user-visible collateral is still optional future work.
- Operator execution now has a bounded automated proof for the first low-risk slice; broader automation still needs to stay behind explicit scope and evidence gates.
- Reference shelf growth needs lightweight governance so research does not become maintenance drag.

## Locked V1 Decisions

- Primary V1 audience: solo creator or teacher-like power user.
- Primary launch workflow: short requirement -> image-series.
- First real operator slice: additive local validation or diagnostics generation.
- Deterministic composition implementation path: `SkiaSharp`.
- Pack posture in V1: internal reusable configuration only.

## Launch Gate

V1 is ready only when all of these are true:

- The primary launch route passes three consecutive fake-first end-to-end runs.
- A 2-item sample series completes through the opt-in OpenAI path with provenance, redaction, and approval evidence verified.
- Article or plain-text planning can produce and promote approved illustration targets without requiring real providers by default.
- The educational poster proof path exports deterministic text-composition provenance and human approval evidence.
- The first real low-risk operator action runs end-to-end and writes audit output plus rollback or cleanup notes.

The current recorded answer is "yes" for the `2026-06-11` snapshot in [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md). Keep this gate as the standard for future refreshes instead of treating the existing proof as a permanent exemption.

## Frozen Until Post-V1

- Physical repository and namespace rename.
- Broad pack-catalog growth beyond launch routes.
- Remote workflow-engine integration beyond a contract boundary.
- Browser or desktop operator flows that change third-party system state.
- Broad high-fidelity binary document automation across office and PDF formats.

## Now

- Keep [AI_CODING_WORKFLOW.md](./AI_CODING_WORKFLOW.md) as the default engineering discipline for non-trivial slices: repo-owned spec/plan/evidence first, conditional subagent/worktree use, and layered auto-execution instead of a second external workflow truth surface.
- Keep the verified short requirement -> brief -> blueprint -> series -> review -> delivery route stable as the primary launch spine.
- Keep the article or plain-text route focused on evidence-backed planning that promotes into the same downstream workflow, and only widen it when real-provider or extraction slices are explicitly activated.
- Keep Phase 4A deterministic composition, readability checks, reviewer notes, and approval evidence export stable, adding stronger user-visible collateral only when release review or onboarding actually needs it.
- Keep the stable Images API path on the official OpenAI .NET SDK, while limiting raw `HttpClient` usage to lagging Responses surfaces; the current opt-in Responses image path is for stateful revision metadata, and partial-preview streaming remains a future UX slice.
- Keep the first low-risk operator execution slice bounded and auditable, and refresh live evidence only when provider behavior or release-claim snapshots change.
- Continue modular splits in Phase 12 only when new slices touch large WPF views or orchestration-heavy services.
- Keep the external reference system focused, deduplicated, and machine-readable instead of widening it ad hoc.
- Keep `REFERENCE_BASIS.md`, `scripts/reference-basis.json`, and the repo-side external-shelf snapshot in machine-checked parity rather than relying on manual drift review alone.

## Next

- Reuse the text-planning low-502 execution policy if future real-provider brief or blueprint planning moves beyond the current fake-first boundary.
- Extend Responses image workflows only when a route gains meaningful provenance, revision, or preview value beyond the current opt-in stateful path.
- Add targeted binary extraction hardening for the support-matrix-approved `pdf` / `docx` text extraction slice only; keep OCR, scanned documents, and other office/PDF formats post-V1.
- Add pack and policy modeling hardening only when a scenario-specific slice has a repo-owned spec and a bounded implementation plan.
- Continue Phase 12 modular splits only where new feature slices touch large WPF or application services.
- Expand mixed artifact delivery and pack coverage only after the launch routes are reliable.

## Later

- Broaden advanced workflow coverage, optional graph-style workflow views, and optional remote workflow-engine integrations.
- Run the medium-term physical repository and namespace rename only through the dedicated gate in ADR 0008 and only after V1 launch hardening is complete.

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

Status: started with DPAPI and `.env` secret resolution, split text/image provider profiles, image key-pool validation, role-scoped provider operation guards, non-generating provider health checks, Provider Center summaries, operation-scoped provider options, resilient HTTP clients, safe provider call telemetry capture for request IDs, token usage, latency, and configured cost estimates, .NET `ActivitySource`/`Meter` instrumentation hooks for provider tracing and metrics, a local Aspire Dashboard launch profile for OTLP export, and an opt-in raw-HTTP Responses image path that records stateful revision metadata. User-visible real-provider hardening is not complete yet.

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
- Responses API multi-turn image workflow support where provider capabilities allow it and where the extra state improves review or revision loops.
- Partial-image streaming support only when a future workbench preview flow justifies progressive image events.
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

Status: core proof path verified by automated repo evidence for the current V1 scope; further work is now about keeping the slice stable and optionally adding stronger user-visible collateral.

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

Status: complete for the local pack foundation: provider-neutral pack metadata, semantic version, compatibility range, lifecycle state, migration notes, local registry validation, built-in starter packs, workflow stage definitions with completion criteria, pack-driven UI defaults using stable view slots, catalog invariant tests, and validated local JSON pack import/export. This does not by itself mean every pack-driven workflow is production-ready.

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

Status: started with a tested application module catalog for source ingestion, artifact planning, pack registry, repair routing, and tool adapters with repository-folder existence guards, stable workflow view slot names for reusable shell placement, a `FeatureViewModule` contract for future WPF view/view model splits, focused `ProjectApplicationService` extractions for project workspace, series workflow, brief workflow, generation workflow, review workflow, review/repair routing, delivery export, and document illustration planning while preserving facade compatibility, incremental `MainWindowViewModel` orchestration splits into `ProjectWorkspaceCoordinator`, `PlanningWorkflowCoordinator`, `BriefWorkflowCoordinator`, `GenerationWorkflowCoordinator`, `ReviewWorkflowCoordinator`, `DeliveryWorkflowCoordinator`, `PlanEditorWorkflowCoordinator`, `WorkflowGraphCoordinator`, and `ProjectWorkbenchProjectionCoordinator`, a follow-up provider center configuration/health service split out of the UI view model, and completed `IEntityTypeConfiguration<T>` persistence mapping splits so `AppDbContext` no longer carries inline entity mapping blocks.

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

Status: started with a structured `RepairPlan` model generated from review routing evidence, `OperatorAction` and `OperatorRun` audit records, a provider-neutral tool adapter contract covering SDK, CLI, local library, browser automation, Windows desktop automation, and computer-use boundaries, built-in local tool descriptors for extraction, conversion, OCR, ImageMagick/FFmpeg processing, deterministic text composition, and artifact validation, an approval gate for medium/high-risk operator actions, a low-risk auto-repair path through the adapter contract, and operator audit export in diagnostics/delivery evidence. The first bounded low-risk execution slice now has automated repo evidence; broader real tool execution remains a future slice.

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

## Phase 14: Product Identity And Repository Rename

Goal: align product-facing naming, local repository identity, and code namespace with the multimodal content delivery end state without breaking existing workspaces or active tools.

Status: started with ADR 0008 and product-facing title updates. The active checkout now lives at `D:\CODE\ai-content-delivery-studio`; the remaining rename work is the later solution/namespace/mechanical migration.

Deliverables:

- Product-facing name: `AI Content Delivery Studio` / `AI 内容交付工作台`.
- Medium-term local root and repository name: `ai-content-delivery-studio`.
- Medium-term solution, project, assembly, namespace, tests, scripts, and publish output rename from `ImageSeriesStudio.*` to `ContentDeliveryStudio.*`.
- Compatibility notes for historical workspaces, diagnostics packages, and docs that still contain `ImageSeriesStudio`.
- Clean rename gate with build, test, format, search, and rollback evidence.

Exit gate:

- The physical root rename is run from a clean worktree with no active process depending on the old path.
- After reopening from the new root, `git status --short`, `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` pass.
- A repository search shows `ImageSeriesStudio` only in historical ADRs, migration notes, compatibility tests, or intentionally preserved aliases.

## Phase 7: Product Hardening (cross-cutting)

Goal: make it reliable as a daily Windows tool.

Status: partially complete through release-readiness work. Remaining hardening continues alongside active feature slices instead of after every other phase is finished.

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
- Product/repository identity aligned to `AI Content Delivery Studio` / `ai-content-delivery-studio`, with `ImageSeriesStudio` kept only as documented historical or compatibility text.
- Provider plugin boundary with capability discovery and contract tests.
- Localized prompt templates, review reports, delivery manifests, and user guide.
- Deterministic text composition for text-heavy educational and document-oriented visuals.
- Local deterministic extraction, conversion, rendering, validation, and packaging tool adapters.
- Large-gallery virtualization, thumbnail cache, crash-safe queue recovery, backup/restore, and diagnostics bundle.
- Packaged Windows release with accessibility and performance gates.
