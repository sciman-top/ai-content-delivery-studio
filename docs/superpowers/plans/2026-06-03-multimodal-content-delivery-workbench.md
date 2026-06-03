# Multimodal Content Delivery Workbench Plan

## Goal

Upgrade the implementation path from an image-series-only workbench to a multimodal content delivery workbench while preserving the image-series workflow as the first production path.

The stable target is:

```text
Windows local workbench
  + replaceable cloud AI providers
  + local deterministic toolchain
  + versioned Workflow/Blueprint packs
```

## Architecture Decisions

- Source files and generated outputs become first-class domain objects through `SourceAsset`, `ExtractedContent`, `EvidenceAnchor`, `OutputArtifact`, and `ArtifactPackage`.
- Workflow generalization comes from versioned packs, not hard-coded topic modes.
- Review becomes `Review -> Repair -> Operator`.
- AI providers stay replaceable through capability-specific contracts.
- Local deterministic tools handle extraction, conversion, rendering, composition, validation, and packaging.
- New features enter through modules; touched old centralized logic is moved incrementally.

## Phase 1: Source And Artifact Foundation

### Task 1: Add source domain records

Description: Add provider-neutral records for `SourceAsset`, `ExtractedContent`, and `EvidenceAnchor`.

Acceptance criteria:

- Source assets can represent local files, generated intermediates, screenshots, or text notes.
- Extracted content can store text, page/range, media, table, formula, OCR, and metadata hints.
- Evidence anchors can connect briefs, plans, reviews, and artifacts back to source content.

Verification:

- `dotnet build`
- `dotnet test`
- Focused persistence tests for new records.

### Task 2: Add artifact domain records

Description: Add `OutputArtifact`, `ArtifactManifest`, and `ArtifactPackage` without breaking existing delivery exports.

Acceptance criteria:

- Existing image delivery packages still export.
- New artifact records can describe image, PDF, DOCX, markdown, review report, manifest, and archive outputs.
- Delivery manifest can include source evidence and artifact provenance.

Verification:

- `dotnet build`
- `dotnet test`
- Delivery manifest snapshot tests.

### Checkpoint: Source and artifact baseline

- Existing image-series workflow still works with fake providers.
- New source/artifact records persist and reload.
- No real provider calls are required.

## Phase 2: Extraction And Artifact Planning

### Task 3: Add fake-first extraction boundary

Description: Add a document extraction provider boundary with fake fixtures before integrating binary PDF/DOCX/OCR tools.

Acceptance criteria:

- Fake extractor can return deterministic text, page anchors, image references, and table/formula placeholders.
- Extraction errors are structured and can route to review or manual correction.
- Binary parser differences do not leak into application use cases.

Verification:

- `dotnet build`
- `dotnet test`
- Fake extractor contract tests.

### Task 4: Add artifact planning use case

Description: Turn brief plus evidence anchors into a plan for image, PDF, DOCX, markdown, or mixed delivery artifacts.

Acceptance criteria:

- Planning is provider-neutral and testable without WPF or network access.
- Plan records cite source evidence anchors.
- Image-series planning remains a supported specialization.

Verification:

- `dotnet build`
- `dotnet test`
- Application service tests with fake planning provider.

## Phase 3: Pack Registry

### Task 5: Add pack metadata model

Description: Add `WorkflowPack`, `BlueprintPack`, `IndustryPack`, `RendererPack`, and `ReviewRubricPack` metadata.

Acceptance criteria:

- Packs have stable ID, version, compatibility range, deprecation state, and migration notes.
- Pack-specific vocabulary stays in pack metadata, not core entity names.
- Invalid pack metadata fails validation.

Verification:

- `dotnet build`
- `dotnet test`
- Pack catalog invariant tests.

### Task 6: Add built-in starter packs

Description: Add starter packs for generic image series, article illustration, document review/translation, courseware visuals, and poster/report delivery.

Acceptance criteria:

- Each pack declares required inputs, outputs, review gates, repair routes, and tool requirements.
- A new pack can be added without changing core entity types.
- Deprecated pack metadata can still explain how old projects should load.

Verification:

- `dotnet build`
- `dotnet test`
- Pack import/export validation tests.

### Task 7: Add pack-driven UI stage metadata

Description: Let packs declare visible workflow stages without creating permanent global tabs.

Acceptance criteria:

- Packs can declare stages through stable IDs: `Source`, `Brief`, `Plan`, `Produce`, `Review`, `Repair`, `Deliver`.
- Each stage can declare required domain objects, visible panels, command groups, and completion criteria.
- Pack-specific vocabulary stays in localization and pack metadata, not shell-level tab names.
- Pack validation rejects stage declarations that require unknown shell slots or unregistered feature modules.

Verification:

- `dotnet build`
- `dotnet test`
- Pack stage validation tests.

## Phase 4: Modular Maintenance

### Task 8: Split feature modules as new work lands

Description: Split source ingestion, artifact planning, pack registry, repair routing, and tool adapters into module-owned folders and tests.

Acceptance criteria:

- New features do not expand one central orchestrator.
- `MainWindowViewModel` and `ProjectApplicationService` shrink only through touched-feature slices.
- Each module has a small use-case API and fake adapter where applicable.
- UI modules register through reusable shell slots such as source list, stage workspace, inspector, activity panel, approval panel, and artifact preview.

Verification:

- `dotnet build`
- `dotnet test`
- `dotnet format --verify-no-changes`

### Task 9: Split persistence and provider configuration

Description: Move growing EF Core mappings and provider configuration into infrastructure-owned modules.

Acceptance criteria:

- EF mappings can be added through `IEntityTypeConfiguration<T>`.
- Provider configuration and capability validation are not WPF concerns.
- Secret storage remains outside repo files and manifests.

Verification:

- `dotnet build`
- `dotnet test`
- Configuration and persistence tests.

## Phase 5: Review, Repair, Operator

### Task 10: Add repair planning

Description: Convert review findings into structured repair plans.

Acceptance criteria:

- Repair plans route issues to source extraction, brief, blueprint, prompt, settings, references, renderer, or operator.
- Repair history is persisted.
- Human final approval remains explicit.

Verification:

- `dotnet build`
- `dotnet test`
- Review routing and repair plan tests.

### Task 11: Add operator action contracts

Description: Add controlled operator action records and adapter contracts for SDK/CLI/local library/browser/desktop/computer-use execution.

Acceptance criteria:

- Each action declares risk, dry-run support, input files, output files, side effects, approvals, timeout, and cleanup path.
- Low-risk actions can run automatically with audit records.
- Medium/high-risk actions pause for approval or handoff.

Verification:

- `dotnet build`
- `dotnet test`
- Tool adapter contract tests.

### Task 12: Add first deterministic tool adapters

Description: Add local adapters for safe deterministic operations before broad UI automation.

Acceptance criteria:

- Initial adapters cover file validation, image conversion/compression, manifest validation, and deterministic text composition fixtures.
- Adapter runs record command/tool provenance, output paths, exit code, and errors.
- No adapter can access secrets or external accounts without explicit configuration and approval.

Verification:

- `dotnet build`
- `dotnet test`
- `dotnet format --verify-no-changes`

## Risks And Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Core model becomes too generic | High | Keep image-series workflow as the first concrete path and verify every new concept with fake-first tests. |
| Central orchestrator grows again | High | Add modules through feature slices and move only touched old logic. |
| Tool adapters become unsafe | High | Require risk metadata, dry-run support where possible, approval gates, timeout, and audit logs. |
| Binary document extraction is noisy | Medium | Start with fake fixtures and structured extraction contracts before real parsers. |
| Provider APIs change quickly | Medium | Keep provider-specific behavior in adapters and capability records. |

## Verification Order

For implementation slices:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

For documentation-only slices:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOLDER)" .
git diff --check
git status --short
```
