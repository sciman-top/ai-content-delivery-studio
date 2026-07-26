# Scientific Source And Understanding Task 22 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 22 of the trustworthy scientific-figure workflow.
It adds Source and Understanding WPF views plus a fail-closed view model for
extraction diagnostics, source blocks, claims, evidence, conflicts, and audited
claim correction drafts.

It does not implement Figure Spec editing, Gate 1 controls, provider calls,
feature visibility, or persistence/schema changes.

## Authority And Behavior

- The view model rejects an understanding whose source asset or SHA-256 does
  not match the supplied extraction.
- Selecting a claim resolves validated supporting/definition evidence first,
  then selects the exact source block, page, section, and verbatim quote.
- Extraction and understanding blocking codes are combined without hiding
  missing evidence, unresolved conflicts, or recovery failures.
- Progression is allowed only when extraction is `Ready` and understanding is
  `ReadyForApproval`.
- A correction command requires changed wording, reviewer, and reason, then
  appends an immutable audit draft while preserving the accepted claim.

## WPF Surface

- `ScientificSourceWorkspaceView` exposes block selection and extraction
  blocks with stable AutomationIds.
- `ScientificUnderstandingWorkspaceView` exposes claim selection, exact
  evidence, conflicts, correction inputs, command, and draft history.
- Both views are hosted side by side below the five stable workflow-stage
  headers. The overall scientific tab remains hidden.

## Focused Verification

- Source/understanding behavior and XAML contract tests: `6 / 6` passed.
- Combined Task 22, scientific layout, main-window layout, and localization
  compatibility tests: `38 / 38` passed.
- The WPF XAML compiled as part of the focused test build.

## Compatibility And N/A

- Paid/live provider: `gate_na`; Task 22 performs no provider dispatch.
- Persistence/schema: `gate_na`; correction drafts remain view-session
  proposals and do not alter accepted domain records.
- Dependency/supply chain: `gate_na`; no package dependency changed.
- Visible desktop screenshot: deferred to Task 25 because
  `ScientificFigureModule.IsUserVisible` remains `false` by contract.

## Rollback

Revert the Task 22 commit to remove the Source/Understanding views, projection,
localized labels, tests, terminology, and evidence. Task 21 leaves the hidden
five-stage shell intact and Tasks 6-20 remain unaffected.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

The fixed-order closeout run passed on 2026-07-27:

- build: exit `0`, `0` warnings, `0` errors
- test: exit `0`, `616 / 616` passed
- contract/invariant: reference evidence and format checks passed
- hotspot: release preflight, nested repository verification, publish WhatIf,
  placeholder/conflict scans, and diff hygiene passed

Task 22 is closed. Tasks 23-30 and Checkpoints 4-5 remain open.
