# Scientific Render And Review Task 24 Evidence

Date: 2026-07-27

## Scope

This evidence records Task 24 of the trustworthy scientific-figure workflow.
It adds a zoomable authority SVG preview, render-item provenance navigation,
separated contract/semantic/visual findings, repair actions, and repair history.

It does not perform Gate 2, export delivery, enable the scientific module,
dispatch providers, or change persistence.

## Authority And Behavior

- The view model rejects mismatched understanding, Figure Spec, render plan, or
  SVG identities and verifies the exact SVG SHA-256 before preview.
- Preview XML is parsed with DTD and external resolution disabled. Only the
  deterministic renderer element set is allowed; event, `href`, `src`, and
  inline-style attributes are rejected even when the supplied hash matches.
- Selecting an SVG authority item follows `SourceSpecificationItemId` to the
  Figure Spec provenance, accepted claim, source block, quote, page, and section
  or to the declared scientific convention.
- Contract hard failures, semantic blockers, and visual blockers remain
  separate. The advisory contract score cannot hide a hard failure.
- Automatic repair remains domain-owned: only actions already routed to
  `LayoutStyle` or `NonEvidentiaryAsset` can execute, an actual callback is
  required, and successful execution records continuous attempt history.
- Existing history restores the three-round limit; gaps, duplicates, or a
  fourth automatic attempt fail closed.

## WPF Surface

- The third workspace column contains Figure Spec and Render & Review task tabs
  rather than adding another compressed column.
- `ScientificRenderReviewWorkspaceView` exposes SVG zoom controls, authority
  items, exact provenance, three finding groups, repair actions, and history.
- The browser host receives only validated authority SVG in a CSP-constrained
  local document. Its code-behind handles rendering and zoom lifecycle only.
- Stable AutomationIds identify the preview, authority list, and authorized
  repair command. `ScientificFigureModule.IsUserVisible` remains `false`.

## Focused Verification

- Render/review view-model and XAML contract tests: `9 / 9` passed.
- Combined Task 24, layout, localization, repair routing, and repair-loop tests:
  `35 / 35` passed.

## Compatibility And N/A

- Paid/live provider: `gate_na`; existing typed review results are projected
  without provider dispatch.
- Persistence/schema: `gate_na`; repair history is supplied as an authoritative
  projection and no schema changes are made.
- Dependency/supply chain: `gate_na`; the WPF browser host is framework-provided
  and no package dependency changed.
- Visible desktop screenshot: deferred to Task 25 because the scientific module
  remains hidden by contract.

## Rollback

Revert the Task 24 commit to remove the render/review view model, WPF view,
localized labels, tests, terminology, and evidence. Tasks 6-23 remain intact.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

The fixed-order closeout run passed on 2026-07-27:

- build: exit `0`, `0` warnings, `0` errors
- test: exit `0`, `631 / 631` passed
- contract/invariant: reference evidence and format checks passed
- hotspot: release preflight, nested repository verification, publish WhatIf,
  placeholder/conflict scans, and diff hygiene passed

Task 24 is closed. Tasks 25-30 and Checkpoints 4-5 remain open.
