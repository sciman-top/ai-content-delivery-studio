# External Reference Strategy

## Purpose

This document records how external reference repositories should be selected, kept, deduplicated, and surfaced to the project.

The goal is not to build a giant archive. The goal is to keep a focused local reference shelf that improves engineering decisions without increasing maintenance noise.

## Main Findings

The current reference shelves were useful, but they had two recurring problems:

- the same upstream repository appeared as multiple physical clones across different project shelves
- different projects evolved different README and update patterns, making shared maintenance harder than it needed to be

At the same time, not every project should share the same reference view. Different products need different reading order, emphasis, and update frequency.

## Recommended Model

AI 推荐: use `shared base + project view + original path junction compatibility`.

That means:

- truly duplicated high-value repositories are stored once under `D:\CODE\external\_shared\repos`
- each project keeps its own `*-references` directory, README, grouping, reading order, and update guidance
- project-facing paths remain stable through directory junctions when a repository becomes shared-backed

This avoids large README rewrites while still reducing duplicate clones.

## Keep, Reduce, Add

### Keep

These are high-value references for AI Content Delivery Studio and should remain part of the project shelf:

- `openai-dotnet`
- `openai-cookbook-selected`
- `WPF-Samples`
- `docs-desktop`
- `CommunityToolkit-dotnet`
- `EntityFramework.Docs`
- `dotnet/extensions`
- `opentelemetry-dotnet`
- `markitdown`
- `docling`
- `PdfPig`
- `QuestPDF`
- `SkiaSharp`
- `playwright-dotnet`
- `FlaUI`
- `ComfyUI`
- `InvokeAI`
- `diffusers`

### Keep But Treat As Optional

- `Prism`
- `semantic-kernel`
- broader orchestration or workflow-graph references
- heavyweight image workflow repositories outside immediate product needs

These are useful inspiration, but they should not reshape the default V1 architecture.

### Deduplicate

The first shared-backed repositories should be official or low-semantic-drift references that multiple projects truly reuse.

Already good candidates:

- `semantic-kernel`
- `docling`
- `EntityFramework.Docs`
- `openai-codex`

Future candidates if duplication actually appears:

- `openai-dotnet`
- `WPF-Samples`
- `playwright-dotnet`

### Add

These references are worth adding when the corresponding engineering slices become active:

- `Tesseract` or `OCRmyPDF`
  - when OCR or scanned-document hardening becomes a real near-term slice
- `WindowsAppSDK-Samples`
  - when package identity, app lifecycle, or future WinUI migration details need stronger code references

## Shared Base Rules

Promote a repository into `_shared` only when all of these are true:

- the same upstream appears in more than one project shelf
- the clones are on the same upstream and same intended branch or revision family
- there are no project-specific local modifications in either copy
- the repository is more foundational than product-specific

Do not promote a repository into `_shared` when:

- it uses a project-specific fork
- it relies on special local branches
- it serves mainly as product-specific inspiration
- sharing it would force multiple project READMEs or scripts to rewrite their logic immediately

## Project View Rules

Each project shelf should keep:

- its own README
- its own reading order
- its own update modes if needed
- its own explanation of why a repository matters for that product

Shared base solves physical duplication. It does not replace project-specific meaning.

## Machine-Readable Governance

Recommended minimum structure:

- `_shared`
  - `README.md`
  - `references.manifest.json`
  - update script
  - duplicate-audit script
- project shelves
  - `README.md`
  - `references.manifest.json` when practical
  - project-appropriate update script

Manifest goals:

- record `relativePath`
- record `upstream`
- record `lastVerifiedCommit`
- record whether a path is shared-backed
- preserve project-facing paths even when the physical entity lives under `_shared`

## Relationship To Current Product Work

This strategy supports the current V1 boundary in two ways:

- it keeps the active reference shelf focused on the real V1 engineering path
- it reduces maintenance noise so the team can spend more attention on product hardening rather than reference sprawl

The reference system should serve the product roadmap, not become a second project of its own.

## Cockpit Tools Local Manifest Decision

Decision: `Cockpit-Tools-Local-references` should gain a machine-readable `references.manifest.json` in the next reference-governance maintenance slice.

Reason:

- the shelf already contains several mirrors and at least one shared-backed candidate such as `openai-codex`
- its README already asks maintainers to keep revision records aligned with project docs
- a manifest would reduce drift without changing the project-facing reference paths

Scope boundary:

- add a lightweight manifest and update guidance only
- do not promote additional repositories into `_shared` until duplicate upstreams and clean local state are verified
- do not make Cockpit reference maintenance a V1 launch blocker for AI Content Delivery Studio
