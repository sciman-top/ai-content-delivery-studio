# Phase 14 Rename Migration Design

## Goal

Complete the post-V1 mechanical rename from `ImageSeriesStudio.*` to `ContentDeliveryStudio.*` across the active solution, project folders, namespaces, scripts, tests, and current engineering documents.

This slice should close the remaining repository-side rename backlog without widening into unrelated product or workflow changes.

## Why Now

- `docs/TASKS.md` still lists the solution, namespace, and compatibility closeout items in Phase 14.
- The local root and product-facing naming are already aligned to `ai-content-delivery-studio` / `AI Content Delivery Studio`.
- The remaining rename work is now a mostly mechanical, verifiable slice with a clear rollback path.

## Scope

This slice covers:

- solution file rename from `ImageSeriesStudio.sln` to `ContentDeliveryStudio.sln`
- `src/` and `tests/` folder rename from `ImageSeriesStudio.*` to `ContentDeliveryStudio.*`
- project file, assembly, namespace, XAML `x:Class`, and internal identifier rename to `ContentDeliveryStudio.*`
- publish script, launch profile, telemetry service name, and test expectations aligned to the new internal name
- reference-governance machine paths aligned to the renamed source tree
- current repo-owned docs updated to describe the renamed internal solution layout
- explicit compatibility notes for existing local workspaces, historical diagnostics, and historical documents

## Non-Goals

This slice does not include:

- another physical root-directory rename
- new product features or workflow behavior changes
- historical ADR or old spec/plan filename rewriting
- retroactive rewrite of already-exported diagnostics bundles or delivery artifacts
- OCR, streaming preview UX, or pack/policy expansion

## Compatibility Boundary

### Existing Local Workspaces

The app should switch its default local app-data root to `ContentDeliveryStudio`, but it should still detect and reuse an existing legacy `ImageSeriesStudio` root when the new root does not yet exist.

This keeps previously generated local project-side folders and review-prep artifacts reachable without forcing an immediate manual migration.

### Historical Diagnostics And Documents

Already exported diagnostics packages, archived delivery outputs, ADRs, and historical spec/plan files may keep `ImageSeriesStudio` text when they describe older states or preserved evidence.

The repository should record this intentionally instead of pretending those historical strings disappeared.

## Design

### Mechanical Rename Strategy

- Rename the active solution and project directories first.
- Apply the same prefix replacement through:
  - project references
  - namespaces
  - WPF `x:Class` and `clr-namespace`
  - scripts and publish output naming
  - tests and helper assertions
- Update machine-readable governance paths after the source tree moves.

### Compatibility Notes

Add a repo-owned note that records:

- legacy local app-data root fallback behavior
- what historical text is intentionally preserved
- what search results are still acceptable after the slice

### Search Rule After Closeout

After the slice lands, `ImageSeriesStudio` should remain only in:

- historical ADR/spec/plan evidence
- compatibility notes
- compatibility tests
- intentionally preserved legacy aliases such as the old local app-data folder segment

## Risks

### Broken Project References

Renaming folders and project files can break solution or project references.

Mitigation:

- rename paths mechanically in one slice
- run full repository verification and release-style preflight after the rename

### Lost Local Artifact Discovery

Blindly switching the default local root can strand previously generated local outputs.

Mitigation:

- preserve a bounded legacy-root fallback in `LocalStudioDataPaths`
- document the behavior explicitly

### Reference-Governance Drift

`scripts/reference-basis.json` and `docs/REFERENCE_BASIS.md` use repository-relative paths that will drift after folder renames.

Mitigation:

- update the machine source
- regenerate the docs surface through `scripts/sync-reference-governance.ps1`

## Acceptance Criteria

- The active solution and project tree use `ContentDeliveryStudio.*`.
- The codebase builds, tests, formats, and passes release-style preflight from the renamed solution tree.
- Current docs describe the new internal solution identity honestly.
- Existing local legacy app-data roots remain discoverable through a bounded compatibility fallback.
- Repository search shows `ImageSeriesStudio` only in historical evidence, compatibility notes/tests, or explicitly preserved aliases.

## Rollback

Rollback by reverting the rename commit.

Because this slice is mechanical and repository-wide, rollback should happen as one unit rather than by partially restoring individual folders or namespaces.
