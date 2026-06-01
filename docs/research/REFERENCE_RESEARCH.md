# Reference Research

Last reviewed: 2026-06-01.

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

### Responses API And Tools

Sources:

- https://developers.openai.com/api/docs/guides/tools
- https://developers.openai.com/api/reference/responses/overview

Key findings:

- Responses API supports tool use, function calling, hosted tools, MCP tools, streaming, background responses, and conversation state.
- `previous_response_id` can be used for multi-turn workflows.
- Function tools are a good fit for local operations such as saving project files, creating generation tasks, validating manifests, or exporting delivery packages.

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

## Community Projects And Best Practices

### ComfyUI

Sources:

- https://github.com/comfyanonymous/ComfyUI
- https://docs.comfy.org/

Patterns to borrow:

- Workflow graph as first-class artifact.
- Reproducible workflow files.
- Queue-based generation.
- Generated images can carry workflow metadata, and workflows can also be stored as compact JSON for versioning and sharing.
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

Patterns to borrow:

- Creator-oriented workspace instead of a thin prompt form.
- Canvas and gallery concepts.
- Model and asset management.
- Boards that separate generated images from uploaded assets.
- Image actions that can load workflow settings, reuse prompts, reuse seeds, or remix a prior output.
- Workflow and queue visibility.

### Hugging Face Diffusers

Source: https://github.com/huggingface/diffusers

Patterns to borrow:

- Provider-neutral pipeline abstraction.
- Small, swappable pipeline components.
- Reproducible configuration and testable inference paths.
- Explicit scheduler/model configuration and seed control as part of reproducible experiments.

## Style And Parameter Governance Findings

The product should treat image type, style, generation settings, reference assets, experiments, and review rubrics as linked but separate objects.

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

## Project-Specific Evidence From The Physics Poster Tool

Source repository: `D:\CODE\physicist_chinese_poster_batch_tool`

Reusable lessons:

- Queue-driven generation and dry-run mode are essential.
- Prompt snapshots must travel with final images.
- JSON metadata and CSV manifests make review and delivery auditable.
- Final delivery should be content-oriented, not temporary batch-oriented.
- AI-assisted visual review is valuable, but human approval remains necessary.
- A clean separation between raw candidates, review evidence, and final delivery prevents cleanup confusion.
