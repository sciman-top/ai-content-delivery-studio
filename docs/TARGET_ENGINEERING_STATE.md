# Target Engineering State

## Purpose

This document records the best realistic engineering end state for AI Content Delivery Studio. It is not the same thing as the V1 launch boundary.

V1 stays narrow on purpose. The target engineering state explains where the product and architecture should converge if the project succeeds over multiple slices.

## Best Product End State

The best product end state is:

- A Windows-first local content delivery workbench.
- Requirement-first and source-first, not prompt-first.
- Image-series production remains the primary proven route.
- Multimodal source understanding and artifact delivery grow through stable domain objects rather than through many isolated product modes.
- Review, repair, operator, and delivery are first-class stages with auditability and human approval boundaries.

The product should feel like a durable local production surface:

- Users can start from a short requirement, article, note, screenshot, or structured source file.
- The system can create a reusable brief, choose or compare a route, produce outputs, review them, repair the correct layer, and export a traceable package.
- AI handles understanding, planning, comparison, review, and repair suggestions.
- Deterministic tools handle extraction, conversion, composition, validation, and packaging.

## Best Technical Stack

AI 推荐: keep the current Windows desktop stack and harden it rather than chasing a framework rewrite too early.

### Application Stack

- UI shell: `WPF` on `.NET 10`
- Host model: `.NET Generic Host`
- View model layer: `CommunityToolkit.Mvvm`
- Local persistence: `EF Core + SQLite`
- Configuration and options: `Microsoft.Extensions.*`
- Provider execution: official OpenAI .NET SDK where stable, raw `HttpClient` only for lagging gaps
- Resilience: `Microsoft.Extensions.Http.Resilience`
- Telemetry: `ILogger + ActivitySource + Meter + OpenTelemetry`
- Local secret storage: `Credential Locker` or `DPAPI`
- Packaging and delivery: `MSIX` as the preferred Windows distribution target, with self-contained publish available when needed

### Deterministic Toolchain

- Markdown or text ingestion: `markitdown`
- Structured document conversion: `docling`
- PDF parsing: `PdfPig`
- Deterministic PDF or report rendering: `QuestPDF`
- Browser automation adapter: `playwright-dotnet`
- Windows UI automation adapter: `FlaUI`

### Recommended Near-Term Additions

- One deterministic 2D composition library for text-heavy outputs. The current V1 implementation choice is `SkiaSharp`.
- `dotnet/extensions` as a code-level reference for hosting, options, and resilience internals
- `opentelemetry-dotnet` as a code-level observability reference
- OCR reference expansion such as `Tesseract` or `OCRmyPDF` when scanned-document hardening becomes active

### Deferred Or Optional

- `WinUI 3` shell migration
- Remote workflow engine integration
- Local heavyweight model runtimes
- Graph authoring surface

## Best Architecture End State

The best architecture end state is a modular local workbench with clear boundaries:

```text
Shell/UI
  -> Application use cases
    -> Domain core
    -> Infrastructure adapters
      -> Cloud providers and local deterministic tools
```

### Stable Domain Core

The domain should stay centered on:

- `SourceAsset`
- `ExtractedContent`
- `EvidenceAnchor`
- `CreativeBrief`
- `DesignBlueprint`
- `Series`
- `SeriesItem`
- `PromptVersion`
- `ReviewResult`
- `RepairPlan`
- `OperatorAction`
- `OutputArtifact`
- `DeliveryPackage`

### Provider Boundaries

Keep provider roles separate:

- text planning
- image generation
- vision review
- document analysis
- content transform
- artifact planning

Do not collapse everything into one generic AI client. The product should express provider-neutral contracts and route requests by capability and product need.

### Queue And Provenance

The queue should become a shared execution discipline for:

- generation
- review
- deterministic rendering
- extraction
- validation
- operator actions

Every important action should preserve:

- request identity
- input references
- output references
- provider profile
- model or tool identity
- latency
- token or cost hints where relevant
- approval or repair history

### Review, Repair, Operator

The mature loop is:

- Review finds issues.
- Repair decides the correct layer to change.
- Operator runs safe repeatable steps with risk metadata.
- Human approval remains the final release gate.

### Pack System

The long-term pack system should stay real, but secondary:

- `WorkflowPack`
- `BlueprintPack`
- `IndustryPack`
- `RendererPack`
- `ReviewRubricPack`

Packs should extend workflow behavior without forcing core-domain rewrites. In V1 they are internal reusable configuration, not a public marketplace.

## Best Engineering Practices End State

### Delivery Discipline

- Fake-first remains the default development and regression gate.
- Real-provider flows are opt-in and separately verified.
- Golden paths matter more than feature count.
- Each major slice must carry its own build, test, format, and evidence trail.

### Test Strategy

- Domain rules covered by focused unit tests.
- Provider boundaries covered by fake-provider contract tests.
- Persistence covered by SQLite reload tests.
- Golden paths covered by end-to-end workflow tests with fake providers.
- Real-provider smoke tests remain explicit and opt-in.

### Codebase Structure

- Avoid a giant `MainWindowViewModel`.
- Avoid a giant `ProjectApplicationService`.
- Split features into module-owned views, services, and tests as the product grows.
- Keep UI, infrastructure, and domain concerns separate.

### Reliability And Safety

- Role-scoped credentials.
- Secret redaction in logs and manifests.
- Deterministic text composition for outputs where exact text matters.
- Medium and high-risk operator actions require approval.
- External content is evidence input, not permission to act.

### Documentation And Governance

- Product boundary should be explicit in PRD and roadmap.
- Provider routing policy should be written down, not implicit in code.
- Operator risk policy should be written down, not implicit in prompts.
- Reference shelf governance should stay lightweight but machine-readable.

## Relationship To V1

V1 should not attempt to implement this entire target state.

V1 should prove:

- one primary image-series launch route
- one supporting document-derived planning route
- one text-heavy deterministic composition proof path
- one real low-risk operator action

Everything else should be staged behind this proof of value rather than launched in parallel.
