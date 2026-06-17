# Phase 14 Rename Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish the internal `ImageSeriesStudio.*` to `ContentDeliveryStudio.*` rename and close the remaining Phase 14 repository-side backlog with fresh verification evidence.

**Architecture:** Land the rename as one mechanical repository slice: rename solution and project paths, rename code and XAML identifiers, preserve a narrow legacy local-data-root fallback, then synchronize machine-readable governance and current docs. Historical evidence stays historical instead of being mass-rewritten.

**Tech Stack:** .NET 10, WPF, PowerShell repo scripts, repository-owned docs, reference-governance manifests, xUnit.

---

## Task 1: Record the rename boundary and compatibility posture

**Files:**
- Create: `docs/superpowers/specs/2026-06-17-phase14-rename-migration-design.md`
- Create: `docs/superpowers/plans/2026-06-17-phase14-rename-migration.md`
- Create: `docs/RENAME_COMPATIBILITY_NOTES.md`

- [ ] Define the slice as a mechanical Phase 14 rename closeout, not a feature slice.
- [ ] Record the compatibility rule for legacy local app-data roots and historical old-name text.
- [ ] Keep the note explicit about what old-name search hits remain acceptable.

## Task 2: Rename the active solution tree

**Files:**
- Rename: `ImageSeriesStudio.sln` -> `ContentDeliveryStudio.sln`
- Rename: `src/ImageSeriesStudio.App/` -> `src/ContentDeliveryStudio.App/`
- Rename: `src/ImageSeriesStudio.Application/` -> `src/ContentDeliveryStudio.Application/`
- Rename: `src/ImageSeriesStudio.Core/` -> `src/ContentDeliveryStudio.Core/`
- Rename: `src/ImageSeriesStudio.Infrastructure/` -> `src/ContentDeliveryStudio.Infrastructure/`
- Rename: `tests/ImageSeriesStudio.Tests/` -> `tests/ContentDeliveryStudio.Tests/`
- Rename matching `*.csproj` files to the new prefix

- [ ] Rename directories and project files in one mechanical pass.
- [ ] Update solution project entries and project-reference paths to the new filenames.
- [ ] Keep the repository buildable from the renamed tree only.

## Task 3: Rename code, XAML, scripts, and tests

**Files:**
- Modify: renamed `src/**`
- Modify: renamed `tests/**`
- Modify: `scripts/publish-app.ps1`
- Modify: `scripts/reference-basis.json`

- [ ] Replace active namespaces, type names, XAML class names, launch profile names, and telemetry identifiers with `ContentDeliveryStudio`.
- [ ] Update test assertions, temp-folder names, repository-root detection, and shimmed build-lock paths to the new tree.
- [ ] Preserve only the bounded legacy alias needed for local app-data-root fallback.

## Task 4: Preserve compatibility and sync current docs

**Files:**
- Modify: `src/ContentDeliveryStudio.Application/Projects/LocalStudioDataPaths.cs`
- Modify: current docs such as `README.md`, `docs/ARCHITECTURE.md`, `docs/AI_CODING_WORKFLOW.md`, `docs/ROADMAP.md`, `docs/TASKS.md`
- Regenerate: `docs/REFERENCE_BASIS.md` through `scripts/sync-reference-governance.ps1`

- [ ] Add the legacy-root fallback in local app-data resolution.
- [ ] Update current repo truth surfaces to describe the renamed internal solution honestly.
- [ ] Mark the completed Phase 14 task items and remove stale rename backlog wording from current task/roadmap readouts.

## Task 5: Verify and close out

**Files:**
- Verify entire repository

- [x] Run `.\scripts\verify-repo.ps1`.
- [x] Run `.\scripts\preflight-release.ps1`.
- [x] Run a targeted repository search for remaining `ImageSeriesStudio` hits and confirm each remaining hit is intentional historical or compatibility text.
- [x] Summarize the repository-side truth boundary and any intentionally preserved legacy alias.
