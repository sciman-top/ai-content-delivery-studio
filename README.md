# AI Content Delivery Studio

Windows-first AI content delivery studio with image-series production as the core capability.

This repository is intentionally separate from `D:\CODE\physicist_chinese_poster_batch_tool`. The physics poster project remains a real production sample and migration source. This repository owns the general-purpose product design, architecture, roadmap, and future implementation.

The original working name was `AI Image Series Studio`. The codebase and active local checkout still use `ImageSeriesStudio` / `ai-image-series-studio` until the planned repository and namespace migration is completed.

## Product Goal

Help a user turn an idea, source file, or draft into a reviewed delivery package. The first production path remains complete image-series creation:

1. Discuss goal, audience, constraints, style, references, and quality standards with AI.
2. Produce a series plan, item list, prompt set, and review rubric.
3. Generate images in controlled batches.
4. Review candidates against user-defined standards with AI assistance and human final approval.
5. Revise prompts and regenerate unsatisfactory items through versioned cycles.
6. Deliver a clean package with images, prompt snapshots, metadata, review records, and manifest.

## Current Repository Status

Current implementation status is intentionally narrower than the long-term product vision.

- The WPF desktop shell, fake-first project workflow, localized workbench, SQLite persistence, and delivery export path are in place.
- The strongest current user-visible workflows are requirement-first image series and fake-first plain-text or article illustration planning.
- Deterministic text composition for text-heavy educational or poster-style output is implemented on the local `SkiaSharp` path and has automated launch-proof coverage.
- Built-in low-risk local tool adapters currently wired into the desktop host are `artifact-validation`, `deterministic-text-composition`, and the read-only `openai-launch-preflight` readiness check.
- Diagnostics export can now carry redacted OpenAI launch-preflight readiness output alongside project, provider, repair, and operator evidence.
- Current V1 release-claim truth lives in [docs/V1_LAUNCH_EVIDENCE.md](docs/V1_LAUNCH_EVIDENCE.md). As of the current snapshot, the remaining explicit V1 launch gap is a fresh opt-in live OpenAI `2-item` sample run with provenance and review evidence.

## Recommended Final Stack

- Desktop UI: WPF on .NET 10 for the MVP, using MVVM and .NET Generic Host.
- Domain and application core: C# class libraries with provider-neutral interfaces.
- Persistence: SQLite via EF Core for project state; filesystem for image assets and delivery packages.
- AI APIs: separate provider contracts for text planning, image generation, vision review, and artifact planning.
- Primary provider split: OpenAI Images API for direct generation and edit flows, OpenAI Responses API for stateful planning, structured review, and only those multi-turn image workflows that provide clear provenance or revision value.
- Background execution: local queue with bounded concurrency, retry policy, cancellation, cost budget, and audit logs.
- Delivery format: folder package plus manifest CSV/JSON, prompt snapshots, image metadata, and review reports.

Current launch-boundary and engineering-state docs:

- [Documentation Governance](docs/DOCUMENTATION_GOVERNANCE.md)
- [Reference Evidence Policy](docs/REFERENCE_EVIDENCE_POLICY.md)
- [Reference Basis](docs/REFERENCE_BASIS.md)
- [V1 PRD](docs/PRD_V1.md)
- [Product Design](docs/PRODUCT_DESIGN.md)
- [Architecture](docs/ARCHITECTURE.md)
- [V1 Launch Evidence](docs/V1_LAUNCH_EVIDENCE.md)
- [Source And Artifact Support Matrix](docs/SOURCE_ARTIFACT_SUPPORT_MATRIX.md)
- [Provider Configuration](docs/PROVIDER_CONFIGURATION.md)
- [Provider Routing Policy](docs/PROVIDER_ROUTING_POLICY.md)
- [Operator Risk Policy](docs/OPERATOR_RISK_POLICY.md)
- [Target Engineering State](docs/TARGET_ENGINEERING_STATE.md)
- [External Reference Strategy](docs/EXTERNAL_REFERENCE_STRATEGY.md)

## Design Documents

- [Documentation Governance](docs/DOCUMENTATION_GOVERNANCE.md)
- [Reference Evidence Policy](docs/REFERENCE_EVIDENCE_POLICY.md)
- [Reference Basis](docs/REFERENCE_BASIS.md)
- [V1 PRD](docs/PRD_V1.md)
- [Product Design](docs/PRODUCT_DESIGN.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Task Checklist](docs/TASKS.md)
- [V1 Launch Evidence](docs/V1_LAUNCH_EVIDENCE.md)
- [Source And Artifact Support Matrix](docs/SOURCE_ARTIFACT_SUPPORT_MATRIX.md)
- [Provider Configuration](docs/PROVIDER_CONFIGURATION.md)
- [Provider Routing Policy](docs/PROVIDER_ROUTING_POLICY.md)
- [Operator Risk Policy](docs/OPERATOR_RISK_POLICY.md)
- [Target Engineering State](docs/TARGET_ENGINEERING_STATE.md)
- [External Reference Strategy](docs/EXTERNAL_REFERENCE_STRATEGY.md)
- [User Guide](docs/USER_GUIDE.md)
- [Reference Research](docs/research/REFERENCE_RESEARCH.md)
- [ADR 0001: Independent Repo And Stack](docs/adr/0001-independent-repo-and-stack.md)
- [ADR 0002: API Boundaries And Review Loop](docs/adr/0002-api-boundaries-and-review-loop.md)
- [ADR 0008: Product Identity And Repository Rename Path](docs/adr/0008-product-identity-and-repository-rename.md)
- [Superpowers Spec](docs/superpowers/specs/2026-05-31-ai-image-series-studio-design.md)
- [Implementation Plan](docs/superpowers/plans/2026-05-31-ai-image-series-studio-implementation.md)

## Local Reference Shelf

A local external reference shelf is available at:

`D:\CODE\external\ai-content-delivery-studio-references`

Its index lives at:

`D:\CODE\external\ai-content-delivery-studio-references\README.md`

Use it for quick local lookup of:

- official OpenAI .NET SDK and selected OpenAI cookbook examples
- WPF, Generic Host, MVVM, and desktop app samples
- EF Core and SQLite documentation sources
- `Microsoft.Extensions` host/options/resilience internals and OpenTelemetry implementation references
- Aspire dashboard, OTLP, and local observability references
- document extraction and deterministic rendering tools such as MarkItDown, Docling, PdfPig, QuestPDF, and SkiaSharp
- browser and Windows desktop automation references such as Playwright .NET and FlaUI
- image workflow architecture references such as ComfyUI, InvokeAI, and Diffusers

These repositories are reference material only. They do not override this repository's `AGENTS.md`, source code, tests, ADRs, or product direction.

## Current Working Baseline

Keep the default day-to-day development path fake-first and local-first:

- use fake providers to exercise planning, generation, review, and delivery without paid API calls
- use deterministic local tools for validation, text composition, diagnostics, and preflight reporting
- enable real OpenAI behavior only through explicit opt-in configuration and release-evidence review

Current core gate:

```powershell
.\scripts\verify-repo.ps1
```

`.\scripts\verify-repo.ps1` runs the repository-local reference-evidence gate first, then `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`.

The same verification path is also wired into GitHub Actions through `.github/workflows/verify-repo.yml`.

The reference-evidence gate matters most when changes touch provider behavior, host or observability plumbing, persistence/schema boundaries, or tooling/operator execution. See [docs/REFERENCE_EVIDENCE_POLICY.md](docs/REFERENCE_EVIDENCE_POLICY.md).

Stronger release-style preflight:

```powershell
.\scripts\preflight-release.ps1
```

`.\scripts\preflight-release.ps1` adds placeholder and merge-conflict scans, reuses the reference-evidence gate, runs the canonical repository verification path, performs a publish dry-run, and finishes with diff-hygiene checks.

## Local Publish

Create a Windows publish folder under the ignored `publish/` directory:

```powershell
.\scripts\publish-app.ps1 -Configuration Release -Runtime win-x64
```

Preview the command without writing output:

```powershell
.\scripts\publish-app.ps1 -WhatIfOnly
```

For docs-only updates or lightweight governance checks, use:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOLDER)" .
git status --short
```
