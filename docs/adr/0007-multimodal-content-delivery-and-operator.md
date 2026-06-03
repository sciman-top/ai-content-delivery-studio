# ADR 0007: Multimodal Content Delivery And AI Operator Boundary

Status: accepted

Date: 2026-06-03

## Context

The product started as a generalized image-series workbench. That direction is still valuable, but user needs are broader than prompt-to-image generation. Real requests often include PDFs, DOCX files, screenshots, reference images, notes, slides, spreadsheets, or draft text. Real outputs may include images, PDF reports, DOCX review files, slide-ready assets, manifests, markdown, or mixed delivery packages.

AI capabilities are also changing quickly. New models, tool surfaces, provider APIs, and agent patterns can appear faster than the application should be refactored. If the core domain model follows every provider release, the product will become brittle.

The project therefore needs a stable architecture for:

- source files and evidence
- image-series and non-image output artifacts
- versioned workflow and blueprint packs
- deterministic local tool execution
- AI-assisted review, repair, and operator actions
- modular maintenance as the codebase grows

## Decision

Upgrade the product target to a multimodal content delivery workbench with image-series production as the core capability.

Use the following stable architecture rule:

```text
Windows local workbench
  + replaceable cloud AI providers
  + local deterministic toolchain
  + versioned Workflow/Blueprint packs
```

Core domain models and application use cases must remain provider-neutral and pack-neutral. AI providers, model versions, workflow packs, industry packs, renderer packs, and review rubrics may be added, removed, upgraded, or deprecated without forcing a rewrite of the core model.

The WPF shell must also remain stable. Workflow packs may select visible stages and feature modules, but new scenarios should not automatically create permanent top-level tabs or a larger central view model.

Introduce first-class concepts for:

- `SourceAsset`
- `ExtractedContent`
- `EvidenceAnchor`
- `OutputArtifact`
- `ArtifactPackage`
- `WorkflowPack`
- `BlueprintPack`
- `IndustryPack`
- `RendererPack`
- `ReviewRubricPack`
- `RepairPlan`
- `OperatorAction`
- `OperatorRun`

Separate the quality loop into three stages:

- `Review`: structured AI, programmatic, and human checks.
- `Repair`: a structured plan that identifies the right layer to change.
- `Operator`: controlled execution through SDK, CLI, local library, browser automation, desktop automation, or computer-use planning.

Prefer local deterministic tools for repeatable extraction, conversion, rendering, composition, validation, and packaging. AI should focus on understanding, planning, selection, review, orchestration, and repair.

## Rationale

This keeps the product flexible without becoming shapeless:

- User files become durable project evidence instead of transient prompt context.
- Output formats become artifact types instead of ad hoc export branches.
- Industry-specific behavior lives in packs instead of core entity names.
- The UI can stay task-first because packs select stages and modules while the shell keeps a stable layout.
- Provider-specific capabilities live in adapters and capability records.
- Local open-source/free tools do the deterministic work they are good at.
- AI remains valuable where ambiguity, judgment, and adaptation matter.
- Operator automation can replace repetitive human actions while preserving approval and audit boundaries.

The rule also matches official and community evidence:

- OpenAI Responses and tool guidance supports multimodal, tool-using workflows.
- OpenAI computer-use guidance emphasizes isolated harnesses, untrusted content boundaries, and human confirmation for risky actions.
- OpenAI guardrails guidance separates automatic checks from human approvals.
- Community workflow tools show the value of reusable workflows, metadata, queue visibility, and review states.
- Local document and media tools are strong candidates for deterministic adapters.

## Alternatives Considered

### Keep the product as image-series only

- Pros: narrower implementation scope.
- Cons: misses the common user pattern where files drive the task and outputs are not only images.
- Rejected: the architecture can stay image-series-first while still modeling sources and artifacts generically.

### Add one central orchestrator for every workflow

- Pros: faster early wiring.
- Cons: grows into a hard-to-test coordination object that knows every UI tab, provider, tool, artifact, and repair path.
- Rejected: new functionality should enter through modules and use-case services.

### Let AI handle extraction, rendering, and validation directly

- Pros: flexible and fast to prototype.
- Cons: weak reproducibility, higher cost, weaker auditability, and poorer precision for text, formulas, layout, file formats, and manifests.
- Rejected: deterministic tools should handle repeatable operations; AI should plan, choose, review, and repair.

### Make local model/workflow runtimes mandatory

- Pros: rich image workflows and less dependence on cloud calls.
- Cons: high hardware burden, larger install size, more setup failures, and weaker fit for low-hardware Windows users.
- Rejected: local model stacks can remain optional adapters or references, not required dependencies.

## Consequences

- Roadmap and tasks now include source/artifact modeling, pack systems, modular maintenance, and operator automation.
- Provider contracts must stay split by capability and include provenance.
- Tool adapters must declare risk, dry-run support, side effects, approval requirements, timeout, and audit output.
- UI and application services should be split incrementally as new modules land.
- Real provider calls remain opt-in; fake providers and deterministic tool fixtures remain the default development path.
- Human approval remains the final delivery gate, especially for external side effects and sensitive user files.

## Follow-Up

- Implement Phase 10 source/artifact foundation before expanding real binary extraction.
- Implement pack registry and compatibility metadata before adding many industry packs.
- Split `MainWindowViewModel` and `ProjectApplicationService` only through feature-touched slices.
- Build `ReviewResult -> RepairPlan -> OperatorAction` using fake/local adapters first.
- Add deterministic document/media tool adapters with audit records before exposing broad UI automation.
