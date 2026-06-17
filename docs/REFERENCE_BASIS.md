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

The machine-sync entrypoint is:

```powershell
.\scripts\sync-reference-governance.ps1
```

That script owns the generated summary block in this document and the repo-side snapshot at `scripts/external-reference-shelf.snapshot.json`.

## Reuse Levels

| Level | Meaning |
| --- | --- |
| `direct-pattern` | Safe to borrow structure or API usage patterns with light adaptation. |
| `adapt-with-review` | Useful reference, but local constraints and contracts must be rechecked before reuse. |
| `inspiration-only` | Study architecture or UX ideas only. Do not treat as implementation default. |

<!-- BEGIN GENERATED REFERENCE BASIS SUMMARY -->

## Machine-Checked Summary

This section is generated from `scripts/reference-basis.json` by `scripts/sync-reference-governance.ps1`.
Do not edit this block by hand. Update the JSON manifest and rerun the sync script instead.

- Manifest version: `1`
- Manifest updatedAt: `2026-06-09T23:10:00+08:00`

### `openai-provider`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Infrastructure/OpenAI/`, `src/ContentDeliveryStudio.Core/Providers/`
- Evidence rules: `docs/research/REFERENCE_RESEARCH.md`, `docs/PROVIDER_CONFIGURATION.md`, `docs/PROVIDER_ROUTING_POLICY.md`, `docs/V1_LAUNCH_EVIDENCE.md`, `docs/REFERENCE_BASIS.md`, `docs/superpowers/specs/`, `docs/superpowers/plans/`
- Required triggers: `request-response-shape`, `images-vs-responses-routing`, `store-or-previous-response-id`, `structured-output`, `vision-review`, `real-provider-enablement`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/01-openai/openai-dotnet` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/01-openai/openai-cookbook-selected` (kind: `official-examples`; reuse: `adapt-with-review`)
  - `D:/CODE/ai-content-delivery-studio/docs/research/REFERENCE_RESEARCH.md` (kind: `repo-evidence`; reuse: `direct-pattern`)

### `host-and-observability`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.App/App.xaml.cs`, `src/ContentDeliveryStudio.App/Telemetry/`, `src/ContentDeliveryStudio.App/Services/ProviderCenterServices.cs`, `src/ContentDeliveryStudio.App/Properties/launchSettings.json`, `src/ContentDeliveryStudio.Infrastructure/Diagnostics/`
- Evidence rules: `docs/research/REFERENCE_RESEARCH.md`, `docs/ARCHITECTURE.md`, `docs/TARGET_ENGINEERING_STATE.md`, `docs/V1_LAUNCH_EVIDENCE.md`, `docs/REFERENCE_BASIS.md`, `docs/superpowers/specs/`, `docs/superpowers/plans/`
- Required triggers: `host-startup`, `dependency-injection`, `telemetry-registration`, `otlp-export`, `aspire-dashboard`, `http-resilience`, `health-diagnostics`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/docs-desktop` (kind: `official-doc-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/WPF-Samples` (kind: `official-samples`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/CommunityToolkit-dotnet` (kind: `official-toolkit-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/08-platform-and-observability/dotnet-extensions` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/08-platform-and-observability/opentelemetry-dotnet` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/08-platform-and-observability/aspire` (kind: `official-source`; reuse: `direct-pattern`)

### `persistence-and-schema`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Infrastructure/Persistence/`, `src/ContentDeliveryStudio.Core/Projects/`, `src/ContentDeliveryStudio.Core/Artifacts/`, `src/ContentDeliveryStudio.Core/Sources/`, `src/ContentDeliveryStudio.Core/Documents/`, `src/ContentDeliveryStudio.Core/Packs/`
- Evidence rules: `docs/research/REFERENCE_RESEARCH.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/TARGET_ENGINEERING_STATE.md`, `docs/REFERENCE_BASIS.md`, `docs/superpowers/specs/`, `docs/superpowers/plans/`
- Required triggers: `entity-configuration`, `aggregate-shape`, `migration-behavior`, `sqlite-limitation`, `project-load-save-contract`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/03-data-persistence/EntityFramework.Docs` (kind: `official-doc-source`; reuse: `direct-pattern`)
  - `D:/CODE/ai-content-delivery-studio/docs/research/REFERENCE_RESEARCH.md` (kind: `repo-evidence`; reuse: `direct-pattern`)

### `tooling-and-operator`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Application/ToolAdapters/`, `src/ContentDeliveryStudio.Infrastructure/ToolAdapters/`, `src/ContentDeliveryStudio.Core/Operators/`, `src/ContentDeliveryStudio.Infrastructure/Composition/`, `src/ContentDeliveryStudio.Infrastructure/Delivery/`, `src/ContentDeliveryStudio.Infrastructure/Import/`, `src/ContentDeliveryStudio.Infrastructure/Sources/`
- Evidence rules: `docs/research/REFERENCE_RESEARCH.md`, `docs/ARCHITECTURE.md`, `docs/OPERATOR_RISK_POLICY.md`, `docs/V1_LAUNCH_EVIDENCE.md`, `docs/REFERENCE_BASIS.md`, `docs/superpowers/specs/`, `docs/superpowers/plans/`
- Required triggers: `deterministic-composition`, `document-conversion`, `artifact-validation`, `delivery-packaging`, `diagnostics-export`, `browser-automation`, `desktop-automation`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/markitdown` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/docling` (kind: `community-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/PdfPig` (kind: `community-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/QuestPDF` (kind: `community-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/SkiaSharp` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/06-automation-testing/playwright-dotnet` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/06-automation-testing/FlaUI` (kind: `community-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/07-image-workflow-references/ComfyUI` (kind: `community-source`; reuse: `inspiration-only`)
  - `D:/CODE/external/ai-content-delivery-studio-references/07-image-workflow-references/InvokeAI` (kind: `community-source`; reuse: `inspiration-only`)
  - `D:/CODE/external/ai-content-delivery-studio-references/07-image-workflow-references/diffusers` (kind: `community-source`; reuse: `inspiration-only`)

### `workflow-and-ux-architecture`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.App/MainWindow.xaml`, `src/ContentDeliveryStudio.App/MainWindow.xaml.cs`, `src/ContentDeliveryStudio.App/ViewModels/`, `src/ContentDeliveryStudio.App/Views/`, `src/ContentDeliveryStudio.Application/Modules/`, `src/ContentDeliveryStudio.Application/Workflows/`
- Evidence rules: `docs/ARCHITECTURE.md`, `docs/TARGET_ENGINEERING_STATE.md`, `docs/REFERENCE_BASIS.md`, `docs/superpowers/specs/`, `docs/superpowers/plans/`
- Required triggers: `large-viewmodel-split`, `large-view-split`, `shell-view-structure`, `workflow-graph`, `queue-gallery-stage-composition`, `module-boundary-change`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/docs-desktop` (kind: `official-doc-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/CommunityToolkit-dotnet` (kind: `official-toolkit-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/WPF-Samples` (kind: `official-samples`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/07-image-workflow-references/ComfyUI` (kind: `community-source`; reuse: `inspiration-only`)
  - `D:/CODE/external/ai-content-delivery-studio-references/07-image-workflow-references/InvokeAI` (kind: `community-source`; reuse: `inspiration-only`)

### `pack-and-policy-modeling`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Core/Packs/`, `src/ContentDeliveryStudio.Application/Packs/`, `src/ContentDeliveryStudio.Core/Projects/ReviewRubricTemplates.cs`, `src/ContentDeliveryStudio.Application/Artifacts/`, `src/ContentDeliveryStudio.Application/Workflows/`
- Evidence rules: `docs/ARCHITECTURE.md`, `docs/TARGET_ENGINEERING_STATE.md`, `docs/ROADMAP.md`, `docs/TASKS.md`, `docs/REFERENCE_BASIS.md`, `docs/superpowers/specs/`, `docs/superpowers/plans/`
- Required triggers: `pack-schema-contract`, `workflow-pack-boundary`, `industry-policy-shape`, `renderer-policy-shape`, `review-rubric-policy-shape`, `scenario-selection-contract`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/CommunityToolkit-dotnet` (kind: `official-toolkit-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/SkiaSharp` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/ai-content-delivery-studio/docs/research/REFERENCE_RESEARCH.md` (kind: `repo-evidence`; reuse: `direct-pattern`)

### `document-extraction-and-ocr`

- `required`: `true`
- Source rules: `src/ContentDeliveryStudio.Core/Documents/`, `src/ContentDeliveryStudio.Infrastructure/Sources/`, `src/ContentDeliveryStudio.Infrastructure/Import/`, `src/ContentDeliveryStudio.Application/Artifacts/`, `src/ContentDeliveryStudio.Application/ToolAdapters/`
- Evidence rules: `docs/SOURCE_ARTIFACT_SUPPORT_MATRIX.md`, `docs/REFERENCE_BASIS.md`, `docs/REFERENCE_EVIDENCE_POLICY.md`, `docs/ROADMAP.md`, `docs/TASKS.md`, `docs/superpowers/specs/`, `docs/superpowers/plans/`
- Required triggers: `pdf-structure-extraction`, `docx-structure-extraction`, `ocr-introduction`, `citation-span-evidence`, `scholarly-figure-source-extraction`
- Local references:
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/markitdown` (kind: `official-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/docling` (kind: `community-source`; reuse: `direct-pattern`)
  - `D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/PdfPig` (kind: `community-source`; reuse: `direct-pattern`)

<!-- END GENERATED REFERENCE BASIS SUMMARY -->

## Reference Areas

### `openai-provider`

Source paths:

- `src/ContentDeliveryStudio.Infrastructure/OpenAI/`
- `src/ContentDeliveryStudio.Core/Providers/`

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

- `src/ContentDeliveryStudio.App/App.xaml.cs`
- `src/ContentDeliveryStudio.App/Telemetry/`
- `src/ContentDeliveryStudio.App/Services/ProviderCenterServices.cs`
- `src/ContentDeliveryStudio.App/Properties/launchSettings.json`
- `src/ContentDeliveryStudio.Infrastructure/Diagnostics/`

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

- changing `App.xaml.cs` host startup, dependency injection registration, or application lifetime wiring
- changing telemetry registration, OTLP export, Aspire dashboard support, logging, or metrics/tracing sources
- changing `HttpClient` resilience handler behavior or named-client conventions
- changing provider-center health or diagnostics plumbing

### `persistence-and-schema`

Source paths:

- `src/ContentDeliveryStudio.Infrastructure/Persistence/`
- `src/ContentDeliveryStudio.Core/Projects/`
- `src/ContentDeliveryStudio.Core/Artifacts/`
- `src/ContentDeliveryStudio.Core/Sources/`
- `src/ContentDeliveryStudio.Core/Documents/`
- `src/ContentDeliveryStudio.Core/Packs/`

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

- `src/ContentDeliveryStudio.Application/ToolAdapters/`
- `src/ContentDeliveryStudio.Infrastructure/ToolAdapters/`
- `src/ContentDeliveryStudio.Core/Operators/`
- `src/ContentDeliveryStudio.Infrastructure/Composition/`
- `src/ContentDeliveryStudio.Infrastructure/Delivery/`
- `src/ContentDeliveryStudio.Infrastructure/Import/`
- `src/ContentDeliveryStudio.Infrastructure/Sources/`

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

- `src/ContentDeliveryStudio.App/MainWindow.xaml`
- `src/ContentDeliveryStudio.App/MainWindow.xaml.cs`
- `src/ContentDeliveryStudio.App/ViewModels/`
- `src/ContentDeliveryStudio.App/Views/`
- `src/ContentDeliveryStudio.Application/Modules/`
- `src/ContentDeliveryStudio.Application/Workflows/`

Required local references:

- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\docs-desktop`
- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\CommunityToolkit-dotnet`
- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\WPF-Samples`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references\ComfyUI`
- `D:\CODE\external\ai-content-delivery-studio-references\07-image-workflow-references\InvokeAI`
- `docs/ARCHITECTURE.md`
- `docs/TARGET_ENGINEERING_STATE.md`

Preferred official docs:

- CommunityToolkit.Mvvm docs
- WPF data binding and commanding docs
- WPF user-control and shell composition docs

Reuse guidance:

- `docs-desktop`, `CommunityToolkit-dotnet`, `WPF-Samples`: `direct-pattern`
- `ComfyUI`, `InvokeAI`: `inspiration-only`

Must check references when:

- splitting large view models, shell views, or feature-owned WPF views
- extracting deterministic projection, localization payload, or selection-summary display builders out of `MainWindowViewModel`
- changing workflow graph, queue, gallery, stage composition, or module boundaries
- changing `MainWindow.xaml` or `MainWindow.xaml.cs` shell data-context or navigation composition
- promoting image-workflow UX ideas into reusable product architecture

### `pack-and-policy-modeling`

Source paths:

- `src/ContentDeliveryStudio.Core/Packs/`
- `src/ContentDeliveryStudio.Application/Packs/`
- `src/ContentDeliveryStudio.Core/Projects/ReviewRubricTemplates.cs`
- `src/ContentDeliveryStudio.Application/Artifacts/`
- `src/ContentDeliveryStudio.Application/Workflows/`

Required local references:

- `D:\CODE\external\ai-content-delivery-studio-references\02-dotnet-wpf\CommunityToolkit-dotnet`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\SkiaSharp`
- `docs/research/REFERENCE_RESEARCH.md`

Preferred official docs:

- CommunityToolkit.Mvvm docs
- WPF and .NET host documentation already used by this repository
- OpenAI images and vision guide when pack policy affects review or deterministic-output expectations

Reuse guidance:

- `CommunityToolkit-dotnet`, `SkiaSharp`: `direct-pattern`
- repository research and architecture docs: `direct-pattern`

Must check references when:

- changing pack schema contracts
- changing workflow-pack or industry-pack metadata shape
- introducing renderer-policy or review-rubric policy records
- introducing scenario-selection or high-requirement policy propagation behavior

### `document-extraction-and-ocr`

Source paths:

- `src/ContentDeliveryStudio.Core/Documents/`
- `src/ContentDeliveryStudio.Infrastructure/Sources/`
- `src/ContentDeliveryStudio.Infrastructure/Import/`
- `src/ContentDeliveryStudio.Application/Artifacts/`
- `src/ContentDeliveryStudio.Application/ToolAdapters/`

Required local references:

- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\markitdown`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\docling`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\PdfPig`
- `docs/research/REFERENCE_RESEARCH.md`

Preferred official docs:

- source and artifact support matrix
- PDF/document extraction references already tracked by the repository

Reuse guidance:

- `markitdown`, `docling`, `PdfPig`: `direct-pattern`

Must check references when:

- introducing PDF or DOCX structure extraction
- introducing OCR into the main product path
- introducing citation-span or paper-figure source extraction
- changing support-matrix promises for binary document inputs

## Current Code Review Findings That Should Drive Reference Use

These repository areas especially benefit from stronger reference discipline:

- `src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.cs`
  - large orchestration surface; prefer WPF/MVVM and modular-composition references before further expansion, and extract pure projection or display-summary builders before moving command ownership
- `src/ContentDeliveryStudio.App/MainWindow.xaml`
  - large shell view; prefer WPF sample and modular-view references before adding more UI surface
- `src/ContentDeliveryStudio.Infrastructure/OpenAI/`
  - high semantic drift; prefer official OpenAI docs and SDK source first
- `src/ContentDeliveryStudio.Infrastructure/Persistence/` plus core aggregate records
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
- `GROBID`
  - only when scholarly PDF extraction or paper-figure evidence extraction becomes an active near-term slice
- `WindowsAppSDK-Samples`
  - only when package identity, lifecycle, or WinUI migration becomes active

### Reduce Or Keep Optional

Keep as optional inspiration, not required implementation sources:

- `04-ai-orchestration/semantic-kernel`
- `07-image-workflow-references/ComfyUI`
- `07-image-workflow-references/InvokeAI`
- `07-image-workflow-references/diffusers`

Do not let these optional references silently redefine the default V1 architecture.
