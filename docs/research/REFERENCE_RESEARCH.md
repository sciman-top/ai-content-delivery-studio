# Reference Research

Last reviewed: 2026-06-03.

This document records the external evidence used to shape the first engineering target. Official documentation defines API and platform semantics. Community projects provide product and workflow inspiration only.

## Official OpenAI References

### Image Generation

Source: https://developers.openai.com/api/docs/guides/image-generation

Key findings:

- OpenAI supports image generation and editing through both the Image API and Responses API.
- Image API is best for direct single-prompt generation and edits.
- Responses API is better for conversational, editable, multi-step image experiences.
- Responses image generation supports multi-turn iteration through previous response context or image generation call IDs.
- Image output can be customized by size, quality, format, compression, and background where supported.
- `gpt-image-2` supports flexible sizes within constraints: max edge length, multiples of 16, aspect ratio limit, and pixel-count limits.
- `gpt-image-2` currently does not support transparent background.
- Complex prompts can be slow. Text rendering, consistency, and exact composition control remain known limitations.
- `quality: "low"` is suitable for fast drafts before medium or high quality final generation.
- `gpt-image-2` accepts many valid resolutions when max edge length, edge multiples, aspect ratio, and total-pixel constraints are satisfied.
- Cost and latency should be estimated from input text tokens, input image tokens for edits/reference images, and image output tokens.
- Reference-image and edit workflows need cost warnings because high-fidelity image inputs can increase token usage.

### Images And Vision

Source: https://developers.openai.com/api/docs/guides/images-vision

Key findings:

- Vision models can analyze images from URLs, base64 data URLs, or file IDs.
- Multiple image inputs are supported, but image inputs count toward token usage.
- Review accuracy has limits: small text, rotated text, graphs, spatial reasoning, counting, panoramic shapes, metadata, and non-English text can be unreliable.
- Vision review should therefore be structured and auditable, but not treated as the sole final authority.

### GPT Image Prompting Guide

Source: https://developers.openai.com/cookbook/examples/multimodal/image-gen-models-prompting-guide

Key findings:

- Production image workflows benefit from a stable prompt structure instead of clever one-off phrasing.
- Low-quality drafts are often the right first pass before medium or high quality candidate promotion.
- Style transfer works best when prompts explicitly separate what must stay consistent from what must change.
- Editing-heavy workflows, brand-sensitive work, text-in-image use cases, and customer-facing assets are strong candidates for higher quality models and stricter first-pass review.

### Responses API State And Tooling

Sources:

- https://developers.openai.com/api/docs/guides/tools
- https://developers.openai.com/api/reference/responses/overview

Key findings:

- Responses API is the most capable OpenAI surface for stateful model interactions involving tools, image inputs, and multi-step workflows.
- The image generation tool supports multi-turn editing through `previous_response_id` or prior image-generation call IDs.
- Image generation in Responses can expose `revised_prompt`, which is useful provenance for review and delivery metadata.
- Partial images can be streamed for more interactive generation UX.
- Function tools are a good fit for local operations such as saving project files, creating queue items, validating manifests, or exporting delivery packages.

## Official Google Vertex AI References

### Image Generation

Sources:

- https://docs.cloud.google.com/vertex-ai/generative-ai/docs/image/generate-images
- https://docs.cloud.google.com/vertex-ai/generative-ai/docs/image/overview

Key findings:

- Google documents image generation, editing, customization, deterministic generation, negative prompting, prompt rewriting, aspect ratio, and resolution as product-level workflows rather than scattered parameters.
- Prompt rewriting and deterministic generation are first-class considerations for production systems.
- Subject customization and style customization should be modeled separately.
- Responsible AI configuration is part of normal image-generation setup, not a late afterthought.

### Prompting And Iteration

Source: https://docs.cloud.google.com/vertex-ai/generative-ai/docs/learn/prompts/introduction-prompt-design

Key findings:

- Clear instructions, prompt structure, parameter experiments, and iterative comparison are explicit best practices.
- Prompt comparison is a documented workflow, reinforcing the product need for multiple prompt directions and blueprint options before full generation.

## Official Stability AI References

### Stable Image Platform

Sources:

- https://platform.stability.ai/docs/getting-started/stable-image
- https://platform.stability.ai/docs/features/api-parameters
- https://platform.stability.ai/docs/features/multi-prompting

Key findings:

- Stable Image workflows treat prompt, negative prompt, seed, and mode selection as core execution parameters.
- Multi-prompting and explicit parameter control are useful patterns for bounded experiments and variation studies.
- A generalized image workbench should preserve these settings as explicit metadata instead of hiding them inside prose prompts.

### Image Editing

Sources:

- https://platform.stability.ai/docs/legacy/grpc-api/features/image-to-image
- https://platform.stability.ai/docs/legacy/grpc-api/features/inpainting

Key findings:

- Image-to-image and inpainting are not niche features; they are standard repair workflows.
- Edit-first repair is often cheaper and more stable than full regeneration when the route and composition are already correct.

## Official Microsoft References

### Windows Developer Platform

Source: https://learn.microsoft.com/windows/apps/get-started/

Key findings:

- Microsoft recommends WinUI for new modern Windows apps.
- WPF is still actively maintained and remains a mature XAML-based Windows desktop framework with controls, binding, layout, graphics, and styles.
- Windows App SDK APIs can be integrated into WPF where useful.

### WPF Generic Host

Source: https://learn.microsoft.com/dotnet/desktop/wpf/app-development/how-to-use-host-builder

Key findings:

- WPF can use .NET Generic Host for dependency injection, configuration, logging, and hosted background services.
- This fits the product need for background generation queues, provider services, and persistent local configuration.
- The official sample targets `net10.0-windows`, supporting the choice of .NET 10 for the first stack.

### EF Core SQLite

Source: https://learn.microsoft.com/ef/core/providers/sqlite/

Key findings:

- EF Core has an official SQLite provider maintained as part of EF Core.
- SQLite is appropriate for local project metadata, task state, prompt versions, review records, and manifests.

### HTTP Resilience

Sources:

- https://learn.microsoft.com/dotnet/core/resilience/
- https://learn.microsoft.com/dotnet/core/resilience/http-resilience

Key findings:

- `Microsoft.Extensions.Http.Resilience` is the current official package for resilient `HttpClient` behavior in .NET.
- The standard handler provides rate limiting, total timeout, retry, circuit breaker, and attempt timeout.
- Retries should be disabled for unsafe HTTP methods when request semantics are not idempotent.
- Provider adapters should use named clients with explicit resilience policies instead of ad hoc retry code.

### OpenTelemetry And Networking Telemetry

Sources:

- https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel
- https://learn.microsoft.com/dotnet/fundamentals/networking/telemetry/overview

Key findings:

- .NET already exposes logs, metrics, and traces through `ILogger`, `Meter`, and `ActivitySource`.
- `HttpClient` is instrumented, making provider-call telemetry feasible without custom transport hacks.
- OpenTelemetry is the right vendor-neutral path for provider latency, queue duration, failure-rate, and throughput instrumentation.
- Local OTLP dashboards are a good development-time aid before introducing any hosted observability backend.

### Credential Locker

Source: https://learn.microsoft.com/windows/apps/develop/security/credential-locker

Key findings:

- Credential Locker APIs are available to desktop apps such as WPF and WinForms.
- Credentials should be stored as passwords or small secret values, not arbitrary blobs.
- Product secret storage should prefer Credential Locker or DPAPI-backed local secrets over plain environment-variable-only production flows.

## Community Projects And Best Practices

### ComfyUI

Sources:

- https://github.com/Comfy-Org/ComfyUI
- https://docs.comfy.org/
- https://docs.comfy.org/development/core-concepts/workflow

Patterns to borrow:

- Workflow graph as first-class artifact.
- Reproducible workflow files.
- Queue-based generation.
- Generated images can carry workflow metadata, and workflows can also be stored as compact JSON for versioning and sharing.
- Built-in workflow templates support guided reuse without forcing users into blank-canvas graph editing first.
- Advanced user mode can expose graph/workflow internals without forcing that complexity on beginners.

### AUTOMATIC1111 Stable Diffusion WebUI

Source: https://github.com/AUTOMATIC1111/stable-diffusion-webui

Patterns to borrow:

- Parameter experiments such as prompt matrix and X/Y/Z plot.
- Metadata stored with generated images or sidecar records.
- Prompt validation warnings for model limits.
- Customizable filename patterns for traceability.
- Extension/plugin ecosystem.
- API access for automation.

### InvokeAI

Sources:

- https://github.com/invoke-ai/InvokeAI
- https://invoke-ai.github.io/InvokeAI/
- https://invoke-ai.github.io/InvokeAI/nodes/NODES/

Patterns to borrow:

- Creator-oriented workspace instead of a thin prompt form.
- Canvas and gallery concepts.
- Model and asset management.
- Boards that separate generated images from uploaded assets.
- Image actions that can load workflow settings, reuse prompts, reuse seeds, or remix a prior output.
- Workflow and queue visibility.
- The workspace should feel like a creative production surface, not only a request form.

### Hugging Face Diffusers

Sources:

- https://github.com/huggingface/diffusers
- https://huggingface.co/docs/diffusers/api/pipelines/overview

Patterns to borrow:

- Provider-neutral pipeline abstraction.
- Small, swappable pipeline components.
- Reproducible configuration and testable inference paths.
- Explicit scheduler/model configuration and seed control as part of reproducible experiments.
- Image-to-image and inpaint are standard pipeline families, reinforcing that the product should model them as first-class modes.

### Skills.sh

Source: https://skills.sh/

Patterns to borrow:

- Reusable skill bundles are an effective packaging format for workflow templates, prompt helpers, and operational conventions.
- Template import/export and versioning ideas are useful for future blueprint or workflow-template features.
- External skill text should remain inspiration only and must not override repository rules, product contracts, or provider semantics.

### GitHub Monthly Trend Signals

Source: https://github.com/trending?since=monthly

Patterns to borrow:

- Monthly trending is useful for discovering multimodal workflow directions, tool chaining habits, and active creator ergonomics.
- Trending repositories are discovery input, not architecture authority.
- Stable project docs and official API/platform references should decide durable dependencies.

## Style And Parameter Governance Findings

The product should treat image type, style, generation settings, reference assets, experiments, and review rubrics as linked but separate objects.

Preset governance evidence is tracked in `docs/research/PRESET_GOVERNANCE_EVIDENCE.md`. That table records which official, community, trend, skill, and local-production sources can influence executable presets, flexible recommendations, and provider capability warnings.

Recommended local contracts:

- `ImageTypePreset`: category, aspect ratio, output format, text policy, rubric, and delivery naming policy.
- `StyleGuide`: repeatable series-level visual language, palette, lighting, composition, negative constraints, and reference links.
- `GenerationRecipe`: provider-neutral model/settings intent plus provider-specific validation warnings.
- `ReferenceImageSet`: reusable style, subject, composition, palette, mask, and negative references.
- `ParameterExperiment`: bounded variation axes, stable slugs, prompt variants, settings, and candidate metadata.
- `ReviewRubric`: structured checks for requirement match, series consistency, subject accuracy, text readability, safety, and delivery readiness.

AI 推荐: implement presets, style guides, and recipes before graph UI. Community graph tools prove that workflow graphs are powerful, but this app needs a stable provider-neutral domain contract first.

### Label Studio, CVAT, FiftyOne

Sources:

- https://github.com/HumanSignal/label-studio
- https://github.com/cvat-ai/cvat
- https://github.com/voxel51/fiftyone

Patterns to borrow:

- Review and annotation are first-class workflows.
- Quality assurance needs structured labels, status, comments, and audit history.
- A gallery is not enough; review needs filters, assignments, decisions, and exportable records.
- Visual asset tools gain real production value when review states and QA routes are explicit.

## Generalized Product Direction Findings

The strongest official and community sources converge on a few product truths:

- requirement capture should come before expensive generation
- multiple prompt or blueprint directions should be compared before final execution
- references, style rules, recipes, and review rubrics should be distinct durable objects
- editing and localized repair are standard workflows, not advanced edge cases
- workflow metadata and output provenance matter as much as the image file itself
- review is a structured loop, not an afterthought

AI 推荐: keep the product generalized as a `series image workbench` with reusable blueprint routes instead of hard-coding one topic mode such as comics, posters, or science explainers.

## Cloud-First Tooling Findings

The current evidence also supports a specific operational recommendation for this repository:

- the default product path should not require local GPU model runtimes on the user's machine
- cloud APIs are the better default for low-hardware Windows desktops
- ComfyUI, InvokeAI, AUTOMATIC1111, and Diffusers should inform architecture and remain optional remote or advanced integrations, not required local dependencies
- lightweight local tools for storage, deterministic composition, reporting, diagnostics, and review state provide more value than shipping local inference complexity by default
- official SDKs, resilient HTTP clients, secure local secret storage, and telemetry should be prioritized before any local-model installation story

## Project-Specific Evidence From The Physics Poster Tool

Source repository: `D:\CODE\physicist_chinese_poster_batch_tool`

Reusable lessons:

- Queue-driven generation and dry-run mode are essential.
- Prompt snapshots must travel with final images.
- JSON metadata and CSV manifests make review and delivery auditable.
- Final delivery should be content-oriented, not temporary batch-oriented.
- AI-assisted visual review is valuable, but human approval remains necessary.
- A clean separation between raw candidates, review evidence, and final delivery prevents cleanup confusion.
