# ADR 0006: Cloud-First Provider And Tooling Strategy

Status: accepted

Date: 2026-06-03

## Context

The repository already has a provider-neutral architecture, fake-first development discipline, and an initial OpenAI integration path. The target user environment is a Windows-first desktop machine that may not have enough GPU, RAM, or disk headroom for a local diffusion stack or local multimodal models.

At the same time, the product still needs strong planning, generation, editing, review, approval, and delivery workflows. Community projects such as ComfyUI, InvokeAI, AUTOMATIC1111, and Diffusers are excellent architecture references, but making any of them a required local runtime would significantly raise setup cost, support cost, and failure modes for this product.

The product therefore needs a durable rule for what stays local, what moves to cloud providers, and what third-party tooling is merely optional inspiration.

## Decision

Use a cloud-first provider strategy and keep local execution focused on orchestration, storage, deterministic post-processing, and review evidence.

Specific decisions:

- OpenAI remains the first real provider implementation.
- Prefer the Image API for direct single-shot generate/edit operations.
- Prefer the Responses API for stateful, multi-turn, tool-augmented image workflows.
- Keep the core domain provider-neutral so Google Vertex AI, Stability AI, or remote workflow-engine adapters can be added later.
- Do not make ComfyUI, InvokeAI, AUTOMATIC1111, Diffusers, or any local model runtime a required default dependency.
- Prefer Windows Credential Locker or DPAPI-backed local secret storage for product credentials.
- Prefer `Microsoft.Extensions.Http.Resilience` for provider HTTP clients and OpenTelemetry for provider and queue observability.
- Treat deterministic text composition as a local product responsibility instead of depending on image-model text rendering for high-stakes educational or document visuals.

## Rationale

This preserves the product's strongest advantages:

- low setup friction on ordinary Windows hardware
- fake-first development and testing without paid API calls
- clear provider contracts and future extensibility
- strong provenance and auditability for review and delivery
- the ability to borrow workflow ideas from heavy local-model tools without inheriting their runtime burden

It also matches the current product direction. This is a desktop workbench for requirement capture, planning, review, and delivery traceability, not a bundled local model distribution.

## Alternatives Considered

### Local diffusion stack as the default path

- Pros: no per-call cloud cost after setup, access to rich local workflows.
- Cons: high hardware burden, large install size, driver and runtime instability, and far more support complexity.
- Rejected: the user's hardware and the product's intended accessibility both argue against this as the default.

### Browser-first SaaS with thin local shell

- Pros: simpler local deployment and easier hosted telemetry.
- Cons: weaker offline/local control, harder local asset workflow, and a worse fit for Windows-first project folders and delivery packaging.
- Rejected: the product benefits from being a real local workbench even when AI execution is cloud-backed.

### Single-provider hard-coded architecture

- Pros: fastest short-term implementation.
- Cons: difficult future migration and high risk of provider semantics leaking into the domain model.
- Rejected: the repository already established separate provider contracts and should keep that boundary.

## Consequences

- Real provider slices should prioritize resilient HTTP execution, secret storage, telemetry, request provenance, and capability validation.
- Local heavyweight model installers are optional future integrations, not roadmap prerequisites.
- Community workflow tools remain reference inputs and optional remote adapters rather than mandatory bundled subsystems.
- Deterministic text composition, structured review, and human approval become more important differentiators than raw local generation throughput.

## Follow-Up

- Add a production secret store adapter for Credential Locker or DPAPI.
- Add resilient named `HttpClient` registrations for provider adapters.
- Add OpenTelemetry traces and metrics for provider calls and queue work.
- Add a deterministic text composition service for text-heavy educational and document workflows.
