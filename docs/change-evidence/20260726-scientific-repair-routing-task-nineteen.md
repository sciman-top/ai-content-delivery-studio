# Scientific Repair Routing Task 19 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 19 of the trustworthy scientific-figure workflow.
It adds typed repair actions and plans for extraction, scientific understanding,
Figure Spec, SVG renderer, layout/style, non-evidentiary asset, and exporter
responsibility layers, plus a bounded automatic-repair loop.

It does not implement Gate 2, silently mutate scientific content, invoke a live
provider, expose WPF UI, or execute a fourth automatic attempt.

## Authority And Automation Rules

- Only `LayoutStyle` and `NonEvidentiaryAsset` actions receive `Automatic` mode.
- Extraction, understanding, specification, renderer, and exporter actions
  require human action even if an upstream caller attempts to forge automatic
  mode; the repair loop validates the layer again.
- Task 16 contract findings retain their item/evidence and map to the owning
  specification, renderer, or exporter layer.
- Task 17 semantic/visual findings retain typed finding kind. Scientific
  mismatch routes to understanding, missing elements to SVG renderer, visual
  defects to layout/style, and non-evidentiary asset defects to asset repair.
- Provider failures and invalid outputs remain human-required blockers.
- Scientific revisions call the domain workflow revision operation, increment
  the specification version, clear Gate 1, clear all downstream approvals, and
  return the workflow to `FigureSpecDraft`.
- Attempts one through three are allowed for valid automatic plans. Attempt
  four fails with an explicit human-action requirement.

## Focused Verification

- `ScientificRepairRoutingTests`: `9 / 9` passed
- `ScientificRepairLoopTests`: `3 / 3` passed
- Coverage includes all seven responsibility layers, Task 16/17 mapping,
  scientific approval invalidation, forged automatic-mode rejection, missing
  automatic action rejection, and the fourth-attempt boundary.

## Compatibility And N/A

- Paid/live provider: `gate_na`; routing is local and deterministic.
- Dependency/supply chain: `gate_na`; no package or lockfile changed.
- Persistence/schema: `gate_na`; repair persistence belongs to later workflow
  delivery work.
- WPF acceptance: deliberately open; user visibility remains false.

## Rollback

Revert the Task 19 commit to remove typed repair plans, routing, loop limits,
tests, terminology, status, and evidence. Tasks 6-18 remain valid and Gate 2
remains unavailable.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`; `0` warnings, `0` errors
- test: exit `0`; `599 / 599` passed
- contract/invariant: reference evidence exit `0`; format verification exit `0`
- hotspot: preflight exit `0`; repository verification, publish WhatIf,
  placeholder/conflict scans, and staged/unstaged diff hygiene passed
