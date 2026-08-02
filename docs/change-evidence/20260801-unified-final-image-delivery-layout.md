# Unified final-image delivery layout evidence

## Scope

This slice extends the earlier article-figure workspace relocation into a
repository-wide final-image path contract. It does not migrate, delete, or
overwrite existing user assets. The external `D:\CODE\classroom-answer-toolkit`
checkout remains read-only.

## Implemented contract

- `LocalStudioDataPaths` now exposes a finite `FinalImageDeliveryCategory` enum
  and stable slugs for image series, image edits, article figure sets, scientific
  figures, document illustrations, courseware visuals, and poster/report visuals.
- `ResolveFinalDeliveryCategoryRoot`,
  `ResolveFinalDeliveryPackageDirectory`, and `ResolveFinalImageDirectory` are
  the centralized categorized path API.
- Resolution precedence is explicit custom root, then
  `CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT`, then the data-root `deliveries`
  fallback. The legacy uncategorized resolver remains only for compatibility.
- `DeliveryWorkflowCoordinator` defaults approved packages to `image-series` and
  accepts an explicit category and custom root for specialized scenarios.
- The localized WPF review panel exposes all seven finite categories, an
  editable environment/fallback-root default, a native folder picker, and a
  live resolved-category preview with stable AutomationIds. Roots inside the
  temporary workspace produce no preview and keep export disabled.
- `DeliveryExportRequest.CreateForFinalDelivery` now centralizes categorized path
  construction for every application-level delivery caller; the coordinator no
  longer rebuilds the package path independently.
- Scientific Gate 2 package export now opens its Save dialog at the
  `scientific-figures` category root while retaining the user's ability to choose
  another location.
- Generation, editing, review-prep, crop, blind-probe, and system-visual-review
  files remain under `workspace/` or an explicit evidence directory; they are not
  final images.
- Local `.env` model routing was updated without exposing credentials: text and
  Responses reasoning routes use `gpt-5.6-sol` with `medium`; image-only
  generation remains `gpt-image-2` because GPT-5.6 Sol is not an image endpoint.

## Default layout

```text
<delivery-root>/<category>/<project-id>/<timestamp>/
  images/
  prompts/
  metadata/
  composition/
  manifest.json
  manifest.csv
  review-report.md
```

Recommended local deployment configuration:

```powershell
$env:CONTENT_DELIVERY_STUDIO_DATA_ROOT = 'D:\CODE\ai-content-delivery-studio'
$env:CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT = 'D:\CODE\classroom-answer-toolkit\正式交付\科学配图'
```

The user-level `CONTENT_DELIVERY_STUDIO_DELIVERY_ROOT` was configured to that D:
path during this task. The target delivery directory did not exist at the time
of configuration, so no directory was created and the target checkout was not
modified. A newly launched app/process will inherit the setting; an already
running process needs an explicit process-level override or restart of only the
app process by the user.

## Verification record

Fresh gate evidence (2026-08-01):

- Build: `dotnet build ContentDeliveryStudio.sln` — exit 0, 0 warnings, 0 errors.
- Tests: `dotnet test ContentDeliveryStudio.sln --no-build` — exit 0, 779/779
  passed, 0 failed, 0 skipped.
- Contract/invariant: `pwsh -NoProfile -ExecutionPolicy Bypass -File
  scripts/verify-reference-evidence.ps1` — exit 0; reference governance and
  workflow/UX evidence gate passed.
- Format: `dotnet format ContentDeliveryStudio.sln --verify-no-changes
  --no-restore` — exit 0.
- Hotspot: `pwsh -NoProfile -ExecutionPolicy Bypass -File
  scripts/preflight-release.ps1 -NoRestore` — exit 0; publish preflight passed,
  84 payload files, package SHA-256
  `393d9cda2f8c72578a94e25245f4512ef81f4bf78124634296e4a54a06d644a9`.
- Focused path tests: `LocalStudioDataPathsTests` and
  `DeliveryWorkflowCoordinatorTests` — 13/13 passed before the request-factory
  follow-up; the fresh categorized path/factory slice is covered by 22/22 focused
  tests (`LocalStudioDataPathsTests`, `DeliveryExportRequestTests`, and
  `DeliveryWorkflowCoordinatorTests`).
- Focused WPF/path regression after the selector follow-through: 72/72 passed
  across `MainWindowViewModelTests`, `MainWindowLayoutTests`,
  `DeliveryWorkflowCoordinatorTests`, `LocalStudioDataPathsTests`, and
  `DeliveryExportRequestTests`. This includes custom-root/category export,
  picker projection, stable UIA IDs, and workspace-root blocking.
- Native WPF fake-only UIA probe: the isolated process remained responsive;
  `FinalDeliveryCategorySelector`, `FinalDeliveryRootInput`,
  `BrowseFinalDeliveryRootButton`, `FinalDeliveryDestinationPreview`, and
  `ExportDeliveryButton` were visible with localized accessible names. The
  first three interactive settings controls were keyboard-focusable, the live
  preview was non-focusable, and export remained disabled without an approved
  item. The isolated process closed normally and its temporary data was removed.
- Packaged Release UIA probe: the verified 84-file ZIP with SHA-256
  `892677e8cb7e6df5877a6983e284a9dc7c820f66793dba43b7d8880b9acefe57`
  launched at 1180 x 760 and 96 DPI in fake mode. All existing Phase 7 shell and
  backup controls plus the five final-delivery controls were present with
  localized names. Ignored machine evidence is stored at
  `outputs/delivery-selector-packaged-accessibility-probe.json`; this proves the
  current-DPI packaged UIA surface, not Narrator, high-contrast, other-DPI,
  touch/pen, human approval, or live-provider acceptance.
- Article sample rerun: `scripts/run-article-scientific-figure-set.ps1` with the
  eye/lens PDF and D: workspace output
  `D:\CODE\ai-content-delivery-studio\workspace\article-figure-runs\20260801-delivery-factory-rerun-complete-set` — exit 0; six candidates,
  `complete=true`, deterministic package `article-optics-v1`, typed crops and
  expected checks present for every item, and `PendingHumanApproval` retained
  for every Gate 1 status.
- Canonical closeout: `pwsh -NoProfile -ExecutionPolicy Bypass -File
  scripts/verify-repo.ps1 -NoRestore` — exit 0 after the request-factory and
  evidence update.
- Diff hygiene: `git diff --check` and `git diff --cached --check` — exit 0.

Fresh selector follow-through closeout (2026-08-02):

- Build: exit 0, 0 warnings, 0 errors.
- Full tests: 782/782 passed, 0 failed, 0 skipped.
- Contract/invariant: reference governance and workflow/UX evidence passed;
  `dotnet format --verify-no-changes --no-restore` passed.
- Hotspot: release preflight passed with 84 payload files; package SHA-256
  `892677e8cb7e6df5877a6983e284a9dc7c820f66793dba43b7d8880b9acefe57`.
- Diff hygiene: tracked and staged diff checks passed. No live or paid provider
  call was made.

## Truth boundary

This proves a repository-side storage contract only. It does not prove article
scientific Gate 1, Gate 2, live-provider acceptance, or human visual acceptance.
The six article candidates remain `PendingHumanApproval` and continue to be
written to workspace evidence paths until those gates are explicitly completed.

## Rollback

Rollback is limited to this path-policy code, tests, documentation, and local
model-setting lines. Do not use Git rollback to delete generated workspace or
delivery assets.
