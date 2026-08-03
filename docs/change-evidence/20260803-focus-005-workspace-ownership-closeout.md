# FOCUS-005 Workspace Ownership Repo-Side Closeout

**Date:** 2026-08-03
**Status:** repo-side completed with fresh canonical Full and Release results.
**Truth boundary:** this evidence covers repository behavior, XML/UIA contracts,
and packaged binaries only. It does not claim a desktop launch, Narrator run,
hardware observation, paid/live provider call, or external publication.

## Result

- Image-series queue, gallery/edit, review/approval, delivery, planning/prompt,
  brief/blueprint/direction, and generation-settings state are owned by the
  `ImageSeriesWorkspace` tree; the owners also hold their stage commands.
- Scientific workflow state remains in the independent scientific workspace
  created by `ScientificFigureWorkspaceFactory`; no ordinary image-series state
  was moved into it or vice versa.
- MainWindow keeps only global project identity, navigation/status, exclusive
  operation gating, persisted reload, and cross-workspace invalidation.
- Pure workflow label/column facades were removed. Remaining state/command
  aliases are compatibility-only and must be removed after shell-internal and
  test consumers use `ImageSeriesWorkspace` directly and a focused/full
  compatibility cutover passes.

## Measurable Surface Reduction

| Measure | Historical baseline | Current | Reduction |
| --- | ---: | ---: | ---: |
| Five MainWindow partials | 3,657 lines | 2,382 lines | 1,275 (34.9%) |
| Public instance properties declared by MainWindow | 228 | 132 | 96 (42.1%) |
| Non-command public properties | 199 | 103 | 96 (48.2%) |
| Public command properties | 29 | 29 | 0; retained compatibility aliases |

Current reflection count was produced from the freshly built assembly with
`BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly` and
classified by the `Command` suffix. This replaces the earlier source-only
baseline with a directly reproducible runtime count.

## Verification

| Stage | Fresh result |
| --- | --- |
| Build | `dotnet build ContentDeliveryStudio.sln --no-restore`: exit 0, 0 warnings/errors. |
| Focused behavior/XML/UIA | MainWindow and Phase7 accessibility contract tests: 46/46 passed. |
| Reference/product contracts | Structured workflow/UX decisions and final product-focus queue contract passed. |
| Full | `scripts/verify-repo.ps1 -NoRestore`: exit 0; build 0 warnings/errors, 767/767 tests, reference/product-focus contracts, and format passed. |
| Release/package | `scripts/preflight-release.ps1 -NoRestore`: exit 0; 84-file `win-x64` package, SHA-256 `e210915dd2cf8f087542f703f24f327089ba4dd5b9b5b9ad7d584f7107d8bb7a`. |

## Packaged UIA Boundary

The task plan names a packaged native UIA probe for both workspace modes, but
the current authority explicitly forbids expanding native UIA/manual acceptance
or launching the desktop app. This item is `gate_na` for this repo-only slice:

- `reason`: native packaged UIA execution requires desktop-app launch and an
  acceptance authority not granted to this task.
- `alternative_verification`: XML parsing asserts owner bindings and stable
  AutomationIds; focused ViewModel tests cover selection, localization, reload,
  command enablement, and operation gating; Release verifies the real package.
- `evidence_link`: this file plus `Phase7AccessibilityContractTests` and the
  package hash above.
- `expires_at`: when `FOCUS-012` enters execution or current native UIA
  authority is explicitly granted.
- `recovery_condition`: run the named packaged UIA matrix on both workspace
  modes and record Windows/hardware/operator identity without relabeling this
  repo-side result as manual acceptance.

Rollback one workspace owner/XAML/test/evidence slice at a time. Do not reset the
dirty worktree or use Git to imitate restoration of user workspace data.
