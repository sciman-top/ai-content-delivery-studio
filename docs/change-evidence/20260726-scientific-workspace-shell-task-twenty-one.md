# Scientific Workspace Shell Task 21 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 21 of the trustworthy scientific-figure workflow.
It adds a localized, hidden-by-default WPF shell with five stable workspace
slots and a read-only coordinator projection of authoritative workflow state.

It does not expose the feature to users, implement the Task 22-25 interactions,
call a provider, or claim WPF/corpus/live acceptance.

## Behavior

- The stages are Source, Understanding, Figure Spec, Render & Review, and
  Delivery in a fixed order.
- Every stage has a stable UI Automation identifier and a localized status.
- Projection identity and progression come from `ScientificFigureWorkflow`;
  no prompt-editing or scientific-authority field is exposed.
- The existing eight workbench tabs remain unchanged because
  `ScientificFigureModule.IsUserVisible` remains `false`.

## Focused Verification

- New coordinator and layout tests: `4 / 4` passed.
- Existing main-window layout, localization, module, and localization-
  coordinator compatibility tests: `39 / 39` passed.
- The WPF XAML compiled as part of the focused test build.

## Native Desktop Probe

A fresh app instance was started only for verification and then stopped.
Microsoft UI Automation reported:

- window title: `AI 内容交付工作台`
- bounds: `1180x760`
- workbench tab count: `8`
- scientific workspace automation elements: `0`

The local screenshot at
`outputs/verification/task21-hidden-scientific-workspace.png` confirms the
scientific tab is hidden and the supported desktop layout has no overlap or
clipped workbench controls. `outputs/` remains intentionally untracked.

## Compatibility And N/A

- Paid/live provider: `gate_na`; Task 21 performs no provider dispatch.
- Persistence/schema: `gate_na`; projection is read-only and unpersisted.
- Dependency/supply chain: `gate_na`; no package or tool dependency changed.
- Visible scientific workspace screenshot: deferred until the approved
  visibility gate in Task 25; exposing it earlier would violate the plan.

## Rollback

Revert the Task 21 commit to remove the coordinator, localized keys, hidden
view registration, layout tests, and evidence. Tasks 6-20 and Checkpoint 3
remain valid and the existing workbench remains user-visible.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

The fixed-order closeout run passed on 2026-07-26:

- build: exit `0`, `0` warnings, `0` errors
- test: exit `0`, `612 / 612` passed
- contract/invariant: reference evidence and format checks passed
- hotspot: release preflight, nested repository verification, publish WhatIf,
  placeholder/conflict scans, and diff hygiene passed

Task 21 is closed. Tasks 22-30 and Checkpoints 4-5 remain open.
