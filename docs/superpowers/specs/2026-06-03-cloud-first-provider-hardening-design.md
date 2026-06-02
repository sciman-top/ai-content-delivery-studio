# Cloud-First Provider Hardening Design

## Current Landing And Target

- Current landing: `D:\CODE\ai-image-series-studio`
- Target: strengthen the real-provider execution path for low-hardware Windows desktops without requiring local model installs.
- Scope: provider API strategy, secret storage, resilient HTTP, observability, multi-turn image workflows, and remote-integration boundaries.

## Problem

The repository already has fake-first planning, generation, review, approval, and delivery loops. It also has a first OpenAI adapter path. What it lacks is a hardened real-provider operating model.

Current risks:

- real provider calls still rely too much on environment-variable-only secret lookup
- provider adapters do not yet use a formal resilience stack
- request provenance, latency, token, and cost telemetry are still too thin
- the product needs a firm stance against drifting into a required local-model installation story on underpowered hardware

## Goals

- Keep the product provider-neutral while making cloud APIs the default real execution path.
- Avoid any requirement to install local diffusion stacks or local multimodal models.
- Use official API surfaces where they improve safety and maintainability.
- Add resilient provider networking and observable execution.
- Preserve fake-first development and opt-in real API calls.

## Non-Goals

- Do not bundle ComfyUI, InvokeAI, AUTOMATIC1111, Diffusers, or other local model runtimes into the default product path.
- Do not rewrite the current WPF shell into a browser-only product.
- Do not collapse text planning, image generation, and vision review into one generic provider interface.

## Recommended Approach

AI 推荐: keep the current local desktop architecture, but make real execution explicitly cloud-first.

That means:

- local app: workbench UI, queue orchestration, metadata, delivery, review evidence, deterministic text composition
- cloud APIs: text planning, image generation, image editing, visual review
- optional remote engines: later provider adapters, never required setup for MVP or standard use

## Provider Priorities

### OpenAI First

- Use the Image API for direct single-shot generate/edit workflows.
- Use the Responses API for stateful, multi-turn image workflows, tool use, and richer provenance.
- Capture `revised_prompt`, request IDs, and partial-image streaming where it helps UX and traceability.

### Future Optional Providers

- Google Vertex AI for broader image-generation and prompt-rewrite capabilities.
- Stability AI for parameter-rich bounded experiments.
- Remote workflow-engine adapters for hosted or managed graph execution.

## Local Infrastructure Requirements

- Windows Credential Locker or DPAPI-backed secret storage
- `Microsoft.Extensions.Http.Resilience` for provider `HttpClient` pipelines
- OpenTelemetry traces and metrics for provider calls and queue operations
- deterministic text composition for labels, legends, formulas, and callouts

## Lightweight Tools Worth Borrowing

- `SkiaSharp` for deterministic text and layout composition
- DB Browser for SQLite as a developer support tool
- local OTLP/Aspire-style dashboards for development observability

## Acceptance Direction

The first hardening slice should prove:

- a low-hardware Windows machine can run the app with no local model runtime installed
- secrets are not limited to raw environment variables
- provider networking is resilient and observable
- delivery metadata captures enough provenance for serious review and audit
