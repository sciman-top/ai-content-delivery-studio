# AGENTS.md - ai-image-series-studio

This repository is the active implementation home for AI Content Delivery Studio, with image-series generation as the core production path. Communicate in Chinese by default. Keep code identifiers, commands, API names, and error strings in English.

## 1. Current Landing And Target

- Current landing: `D:\CODE\ai-image-series-studio`
- Target: AI Content Delivery Studio, a Windows-first desktop app for AI-assisted source understanding, image-series planning, artifact generation, review, repair, operator automation, and delivery packaging.
- Intended medium-term local root name: `D:\CODE\ai-content-delivery-studio`; keep the current root until the rename gate in `docs/adr/0008-product-identity-and-repository-rename.md` is executed.
- Source sample: `D:\CODE\physicist_chinese_poster_batch_tool` is a production case study, not the implementation root.

## A. Module Boundaries

- `docs/`: product design, architecture, references, roadmap, task lists, and ADRs.
- `docs/research/`: official and community references used for engineering decisions.
- `docs/adr/`: durable architecture decisions.
- `src/`: WPF app, application services, domain core, infrastructure providers, deterministic composition, diagnostics, persistence, and tool adapters.
- `tests/`: unit and integration tests, including fake-first launch verification, provider preflight, and operator/tool-adapter coverage.
- `workspace/`: local runtime project data; ignored by git.
- `outputs/`: generated images and delivery packages; ignored by git.

## B. Gate Order

Before code exists:

```powershell
rg -n "(TB[D]|TO[D]O|PLACE''HOLDER)" .
git status --short
```

After code exists:

```powershell
.\scripts\verify-repo.ps1
.\scripts\preflight-release.ps1
```

Provider integration gates must use fake providers first. Real paid API calls require explicit user approval.
`.\scripts\verify-repo.ps1` is the canonical local full gate. It runs `.\scripts\verify-reference-evidence.ps1` first, then `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`.
`.\scripts\preflight-release.ps1` is the stronger release-style preflight. It adds placeholder and merge-conflict scans, reuses the reference-evidence gate, runs the canonical repository verification path, performs a publish `-WhatIfOnly` dry run, and finishes with diff-hygiene checks.
When provider, host/observability, persistence/schema, or tooling/operator boundaries change, the reference-evidence portion of that gate must pass before treating the slice as ready.
The repository also carries `.github/workflows/verify-repo.yml` so the same verification path can run remotely on normal `push` and `pull_request` events.

## C. Safety And Resource Guards

- Do not commit API keys, generated image assets, local SQLite databases, or user workspaces.
- Store secrets via Windows Credential Manager or DPAPI-backed local configuration, not plain text project files.
- Keep text planning, image generation, and vision review as separate provider contracts.
- AI review is advisory. Human approval remains the final delivery gate.
- Text-heavy educational or poster output should support deterministic post-render text composition rather than relying only on image model text rendering.

## D. Evidence And Rollback

- Design evidence lives in `docs/research/REFERENCE_RESEARCH.md`.
- Reference-discipline rules live in `docs/REFERENCE_EVIDENCE_POLICY.md`.
- Durable `code area/task -> reference shelf` mapping lives in `docs/REFERENCE_BASIS.md` and `scripts/reference-basis.json`.
- Architecture decisions live in `docs/adr/`.
- Implementation tasks live in `docs/TASKS.md` and `docs/superpowers/plans/`.
- Revert documentation changes with git. Generated outputs and local workspaces are ignored and must be backed up outside git when needed.
