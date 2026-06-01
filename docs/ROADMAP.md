# Roadmap

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
- Versioned workflow templates, then optional workflow graph import/export.
- Provider plugin boundary with capability discovery and contract tests.
- Localized prompt templates, review reports, delivery manifests, and user guide.
- Large-gallery virtualization, thumbnail cache, crash-safe queue recovery, backup/restore, and diagnostics bundle.
- Packaged Windows release with accessibility and performance gates.
