# AGENTS.md - ai-image-series-studio

This repository is the general-purpose successor product planning and implementation home for the image-series generation workflow. Communicate in Chinese by default. Keep code identifiers, commands, API names, and error strings in English.

## 1. Current Landing And Target

- Current landing: `D:\CODE\ai-image-series-studio`
- Target: Windows-first desktop app for AI-assisted image series planning, batch generation, review, iteration, and delivery.
- Source sample: `D:\CODE\physicist_chinese_poster_batch_tool` is a production case study, not the implementation root.

## A. Module Boundaries

- `docs/`: product design, architecture, references, roadmap, task lists, and ADRs.
- `docs/research/`: official and community references used for engineering decisions.
- `docs/adr/`: durable architecture decisions.
- Future `src/`: WPF app, domain core, infrastructure providers, and tests.
- Future `workspace/`: local runtime project data; ignored by git.
- Future `outputs/`: generated images and delivery packages; ignored by git.

## B. Gate Order

Before code exists:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOLDER)" .
git status --short
```

After code exists:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

Provider integration gates must use fake providers first. Real paid API calls require explicit user approval.

## C. Safety And Resource Guards

- Do not commit API keys, generated image assets, local SQLite databases, or user workspaces.
- Store secrets via Windows Credential Manager or DPAPI-backed local configuration, not plain text project files.
- Keep text planning, image generation, and vision review as separate provider contracts.
- AI review is advisory. Human approval remains the final delivery gate.
- Text-heavy educational or poster output should support deterministic post-render text composition rather than relying only on image model text rendering.

## D. Evidence And Rollback

- Design evidence lives in `docs/research/REFERENCE_RESEARCH.md`.
- Architecture decisions live in `docs/adr/`.
- Implementation tasks live in `docs/TASKS.md` and `docs/superpowers/plans/`.
- Revert documentation changes with git. Generated outputs and local workspaces are ignored and must be backed up outside git when needed.
