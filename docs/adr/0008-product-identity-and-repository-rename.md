# ADR 0008: Product Identity And Repository Rename Path

Status: accepted

Date: 2026-06-04

## Context

The repository and solution started as `ai-image-series-studio` / `ImageSeriesStudio` because the first production path was controlled image-series generation. That name still describes the MVP core, but the roadmap has moved beyond prompt-to-image and single-purpose image batches.

The current target is a Windows local multimodal content delivery studio. The product now models source assets, extracted content, evidence anchors, output artifacts, artifact packages, workflow packs, blueprint packs, industry packs, renderer packs, review rubrics, repair plans, and operator actions. Coherent image-series production remains the first-class path, but it is no longer the whole product boundary.

Changing all names at once would be noisy and risky. A full rename touches the local directory, solution file, project folders, namespaces, assembly names, tests, publish output, workspace paths, documentation links, and possibly user-local data. The local root directory rename is also not a git-tracked change and can break active shells, IDE windows, or Codex sessions if it happens mid-run.

## Decision

Use `AI Content Delivery Studio` as the product-facing name.

Use `AI 内容交付工作台` as the Chinese product-facing name.

The active internal solution, namespace, and project-folder identity now use `ContentDeliveryStudio.*`.

Treat `ai-content-delivery-studio` as the active repository and local root directory name. Preserve only bounded compatibility behavior for legacy local app-data discovery and historical evidence that still mentions `ImageSeriesStudio`.

## Migration Plan

1. Product-facing rename:
   - Update app title, README, user guide, product design, architecture, roadmap, and task checklist.
   - Keep image-series wording where it describes the MVP or core capability.
   - Verify with build, tests, format, and documentation search.

2. Repository/root directory rename:
   - Preconditions: clean git worktree, no running app/publish/test process holding files, IDE/Codex sessions aware of the path move.
   - Move `D:\CODE\ai-image-series-studio` to `D:\CODE\ai-content-delivery-studio`.
   - Reopen tools from the new path.
   - Verify `git status --short`, `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`.
   - Rollback: move the directory back to `D:\CODE\ai-image-series-studio` before making further changes.
   - Status: completed in the verified post-V1 rename slice.

3. Solution and namespace rename:
   - Rename `ImageSeriesStudio.sln`, project folders, project names, namespaces, assembly names, publish output, tests, and scripts in one mechanical slice.
   - Preserve compatibility notes for existing workspaces and diagnostics that still contain `ImageSeriesStudio` strings.
   - Verify no unintended references remain except historical ADRs, migration notes, and compatibility text.
   - Status: completed in the verified post-V1 rename slice.

## Alternatives Considered

### Keep `AI Image Series Studio`

- Pros: matches the MVP, current code, and repository path.
- Cons: under-describes the source/artifact/pack/operator direction already accepted in ADR 0007.
- Rejected: acceptable as an internal legacy name for now, not as the final product identity.

### Rename everything immediately

- Pros: removes naming drift in one pass.
- Cons: high churn, high conflict risk, and the local root move is not represented by git.
- Rejected: the rename should be staged so each layer has a clear gate and rollback.

### Use `Multimodal Content Delivery Workbench` as the product name

- Pros: very precise architecture wording.
- Cons: too long for app chrome, package names, and daily use.
- Rejected: keep it as the category/positioning phrase; use `AI Content Delivery Studio` as the product name.

## Consequences

- Product-facing docs and UI should say `AI Content Delivery Studio`.
- Roadmap and tasks must track the later root-directory and namespace migration explicitly.
- `ImageSeriesStudio` now remains only in historical docs, compatibility notes/tests, and explicitly preserved legacy aliases such as the old local app-data root segment.
- New generic capabilities should avoid adding more user-facing `Image Series Studio` branding.
- Historical ADRs, old implementation plans, and compatibility notes may keep old names when they describe past decisions or migration evidence.
