# AI Image Series Studio

Windows-first AI image series production studio.

This repository is intentionally separate from `D:\CODE\physicist_chinese_poster_batch_tool`. The physics poster project remains a real production sample and migration source. This repository owns the general-purpose product design, architecture, roadmap, and future implementation.

## Product Goal

Help a user turn an idea into a complete image series:

1. Discuss goal, audience, constraints, style, references, and quality standards with AI.
2. Produce a series plan, item list, prompt set, and review rubric.
3. Generate images in controlled batches.
4. Review candidates against user-defined standards with AI assistance and human final approval.
5. Revise prompts and regenerate unsatisfactory items through versioned cycles.
6. Deliver a clean package with images, prompt snapshots, metadata, review records, and manifest.

## Recommended Final Stack

- Desktop UI: WPF on .NET 10 for the MVP, using MVVM and .NET Generic Host.
- Domain and application core: C# class libraries with provider-neutral interfaces.
- Persistence: SQLite via EF Core for project state; filesystem for image assets and delivery packages.
- AI APIs: separate provider contracts for text planning, image generation, and vision review.
- Primary provider: OpenAI Responses API for conversation, planning, vision, and multi-turn image workflows; OpenAI Images API for direct batch image generation and edits.
- Background execution: local queue with bounded concurrency, retry policy, cancellation, cost budget, and audit logs.
- Delivery format: folder package plus manifest CSV/JSON, prompt snapshots, image metadata, and review reports.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and [docs/PRODUCT_DESIGN.md](docs/PRODUCT_DESIGN.md).

## Design Documents

- [Product Design](docs/PRODUCT_DESIGN.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Task Checklist](docs/TASKS.md)
- [User Guide](docs/USER_GUIDE.md)
- [Reference Research](docs/research/REFERENCE_RESEARCH.md)
- [ADR 0001: Independent Repo And Stack](docs/adr/0001-independent-repo-and-stack.md)
- [ADR 0002: API Boundaries And Review Loop](docs/adr/0002-api-boundaries-and-review-loop.md)
- [Superpowers Spec](docs/superpowers/specs/2026-05-31-ai-image-series-studio-design.md)
- [Implementation Plan](docs/superpowers/plans/2026-05-31-ai-image-series-studio-implementation.md)

## First Implementation Slice

Build the domain model, local project file format, fake AI providers, and a minimal WPF shell before calling any paid image API. This keeps the core workflow testable without API cost.

Expected first gate after code exists:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Until source code exists, documentation validation is:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOLDER)" .
git status --short
```
