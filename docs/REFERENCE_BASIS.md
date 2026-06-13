# Reference Basis

Last reviewed: 2026-06-09.

This document turns the repository's reference strategy into an actionable mapping:

- which code areas and task types map to which local references
- which references are official-first versus inspiration-only
- what may be reused directly, adapted carefully, or only studied conceptually
- when the repository gate should require visible reference evidence

Use this document together with:

- [REFERENCE_EVIDENCE_POLICY.md](./REFERENCE_EVIDENCE_POLICY.md)
- [DOCUMENTATION_GOVERNANCE.md](./DOCUMENTATION_GOVERNANCE.md)
- `D:\CODE\external\ai-content-delivery-studio-references\README.md`

## Hard Rule

When a change touches a high-drift engineering area or a task listed here as `required`, the engineer must:

1. consult the mapped local reference shelf first
2. prefer official documentation or official source repositories before community examples
3. leave an in-repo evidence trail in the same change set

The local enforcement entrypoint is:

```powershell
.\scripts\verify-reference-evidence.ps1
```

## Reuse Levels

| Level | Meaning |
| --- | --- |
| `direct-pattern` | Safe to borrow structure or API usage patterns with light adaptation. |
| `adapt-with-review` | Useful reference, but local constraints and contracts must be rechecked before reuse. |
| `inspiration-only` | Study architecture or UX ideas only. Do not treat as implementation default. |

## Reference Areas

### `openai-provider`

Source paths:

- `src/ImageSeriesStudio.Infrastructure/OpenAI/`
- `src/ImageSeriesStudio.Core/Providers/`

Required local references:

- `D:\CODE\external\ai-content-delivery-studio-references\01-openai\openai-dotnet`
- `D:\CODE\external\ai-content-delivery-studio-references\01-openai\openai-cookbook-selected`
- `docs/research/REFERENCE_RESEARCH.md`

Preferred official docs:

- OpenAI image generation guide
- OpenAI images and vision guide
- OpenAI tools guide
- OpenAI Responses API reference

Reuse guidance:

- `openai-dotnet`: `direct-pattern`
- `openai-cookbook-selected`: `adapt-with-review`

Must check references when:

- adding or changing request or response payload shapes
- changing Images API versus Responses API routing
- changing `store`, `previous_response_id`, structured output, image edit, or vision-review behavior
- changing provider error handling, retries, cost semantics, or request provenance capture
- enabling previously fake-only real-provider routes

### `host-and-observability`

Source paths:

- `src/ImageSeriesStudio.App/`
- `src/ImageSeriesStudio.Infrastructure/Diagnostics/`

Required local references:

- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\docs-desktop`
- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\WPF-Samples`
- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\CommunityToolkit-dotnet`
- `D:\CODE\external\ai-content-delivery-studio-references\08-platform-and-observability\dotnet-extensions`
- `D:\CODE\external\ai-content-delivery-studio-references\08-platform-and-observability\opentelemetry-dotnet`
- `D:\CODE\external\ai-content-delivery-studio-references\08-platform-and-observability\aspire`
- `docs/research/REFERENCE_RESEARCH.md`

Preferred official docs:

- WPF Generic Host guidance
- WPF documentation
- .NET resilience guidance
- .NET observability with OpenTelemetry
- .NET networking telemetry guidance

Reuse guidance:

- `docs-desktop`, `WPF-Samples`, `CommunityToolkit-dotnet`: `direct-pattern`
- `dotnet-extensions`, `opentelemetry-dotnet`, `aspire`: `direct-pattern`
- `Prism`: `adapt-with-review`

Must check references when:

- changing host startup, dependency injection, view-model composition, or lifecycle wiring
- changing telemetry registration, OTLP export, Aspire dashboard support, logging, or metrics/tracing sources
- changing `HttpClient` resilience handler behavior or named-client conventions
- changing provider-center health or diagnostics plumbing

### `persistence-and-schema`

Source paths:

- `src/ImageSeriesStudio.Infrastructure/Persistence/`
- `src/ImageSeriesStudio.Core/Projects/`
- `src/ImageSeriesStudio.Core/Artifacts/`
- `src/ImageSeriesStudio.Core/Sources/`
- `src/ImageSeriesStudio.Core/Documents/`
- `src/ImageSeriesStudio.Core/Packs/`

Required local references:

- `D:\CODE\external\ai-content-delivery-studio-references\03-data-persistence\EntityFramework.Docs`
- `docs/research/REFERENCE_RESEARCH.md`

Preferred official docs:

- EF Core SQLite provider docs
- EF Core migration and testing strategy docs

Reuse guidance:

- `EntityFramework.Docs`: `direct-pattern`

Must check references when:

- adding or changing EF entity configuration
- changing aggregate structure, durable record shape, or migration behavior
- changing SQLite assumptions, locking, rebuild-sensitive schema operations, or persistence tests
- changing delivery manifest, artifact provenance, or project load/save contracts

### `tooling-and-operator`

Source paths:

- `src/ImageSeriesStudio.Application/ToolAdapters/`
- `src/ImageSeriesStudio.Infrastructure/ToolAdapters/`
- `src/ImageSeriesStudio.Core/Operators/`
- `src/ImageSeriesStudio.Infrastructure/Composition/`
- `src/ImageSeriesStudio.Infrastructure/Delivery/`
- `src/ImageSeriesStudio.Infrastructure/Import/`
- `src/ImageSeriesStudio.Infrastructure/Sources/`

Required local references:

- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\markitdown`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\docling`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\PdfPig`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\QuestPDF`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\SkiaSharp`
- `D:\CODE\external\ai-content-delivery-studio-references\06-automation-testing\playwright-dotnet`
- `D:\CODE\external\ai-content-delivery-studio-references\06-automation-testing\FlaUI`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references\ComfyUI`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references\InvokeAI`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references\diffusers`
- `docs/research/REFERENCE_RESEARCH.md`

Reuse guidance:

- `markitdown`, `docling`, `PdfPig`, `QuestPDF`, `SkiaSharp`, `playwright-dotnet`, `FlaUI`: `direct-pattern`
- `ComfyUI`, `InvokeAI`, `diffusers`: `inspiration-only`

Must check references when:

- adding or changing deterministic composition or document conversion behavior
- changing artifact-validation, delivery packaging, diagnostics export, or import logic
- changing browser or desktop automation adapter boundaries
- changing queue, graph, workflow, canvas, gallery, or operator execution concepts by borrowing from local-image projects

### `workflow-and-ux-architecture`

Source paths:

- `src/ImageSeriesStudio.App/ViewModels/`
- `src/ImageSeriesStudio.App/Views/`
- `src/ImageSeriesStudio.Application/Modules/`
- `src/ImageSeriesStudio.Application/Workflows/`

Required local references:

- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\CommunityToolkit-dotnet`
- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\WPF-Samples`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references\ComfyUI`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references\InvokeAI`
- `docs/ARCHITECTURE.md`
- `docs/TARGET_ENGINEERING_STATE.md`

Reuse guidance:

- `CommunityToolkit-dotnet`, `WPF-Samples`: `direct-pattern`
- `ComfyUI`, `InvokeAI`: `inspiration-only`

Must check references when:

- splitting large view models or large WPF views
- changing workflow graph, queue, gallery, stage composition, or module boundaries
- promoting image-workflow UX ideas into reusable product architecture

## Current Code Review Findings That Should Drive Reference Use

These repository areas especially benefit from stronger reference discipline:

- `src/ImageSeriesStudio.App/ViewModels/MainWindowViewModel.cs`
  - large orchestration surface; prefer WPF/MVVM and modular-composition references before further expansion
- `src/ImageSeriesStudio.App/MainWindow.xaml`
  - large shell view; prefer WPF sample and modular-view references before adding more UI surface
- `src/ImageSeriesStudio.Infrastructure/OpenAI/`
  - high semantic drift; prefer official OpenAI docs and SDK source first
- `src/ImageSeriesStudio.Infrastructure/Persistence/` plus core aggregate records
  - schema and SQLite behavior should stay anchored to official EF docs

## Current Reference Shelf Gap Review

### Keep

Keep the current reference groups. They align well with the real codebase and roadmap.

### Add

Add or keep freshly added:

- `08-platform-and-observability/aspire`
  - because the repository already ships an `AspireDashboard` OTLP profile and local telemetry guidance

### Add Later When Activated

- OCR references such as `Tesseract` or `OCRmyPDF`
  - only when scanned-document hardening becomes an active near-term slice
- `WindowsAppSDK-Samples`
  - only when package identity, lifecycle, or WinUI migration becomes active

### Reduce Or Keep Optional

Keep as optional inspiration, not required implementation sources:

- `04-ai-orchestration/semantic-kernel`
- `07-image-workflow-references/ComfyUI`
- `07-image-workflow-references/InvokeAI`
- `07-image-workflow-references/diffusers`

Do not let these optional references silently redefine the default V1 architecture.
