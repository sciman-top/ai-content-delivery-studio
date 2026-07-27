# Scientific Delivery Workspace Task 25 Evidence

Date: 2026-07-27

## Scope

This evidence records Task 25 and Checkpoint 4 of the trustworthy
scientific-figure workflow. It exposes the complete fake-first five-workspace
path, adds cross-format delivery inspection and explicit Gate 2 decisions, and
connects approved package bytes to a user-invoked system save dialog.

It does not run the 12-item corpus, call a live provider, or claim final
scientific-workflow acceptance.

## Authority And Behavior

- `ScientificFigureDeliveryService.ValidateGateTwoReadiness` remains the
  application authority for current Gate 1, deterministic and machine review,
  repair closure, provider metadata, exactly one PNG and one PDF, SVG/export
  hashes, semantic hashes, and decision ordering.
- The Delivery view model projects SVG, PNG, and PDF artifacts, full
  claim/evidence/item mappings, provider identities, repair count, and all
  unresolved blockers. It cannot enable a human decision when domain readiness
  fails.
- Approval and rejection require non-empty reviewer and notes. Approval builds
  the immutable package containing both gates; rejection routes a
  human-required Figure Spec repair and creates no package.
- Approval has no filesystem side effect. Export becomes enabled only after an
  approved package exists, and only the explicit export command opens the save
  dialog.
- The fake workspace freezes Gate 1, reaches `ReviewPassed`, and keeps Gate 2
  pending for a real user action. Module visibility changed only after the
  complete WPF path and focused Checkpoint 4 tests existed.

## WPF And Accessibility

- The scientific tab is the ninth workbench tab and exposes Source,
  Understanding, Figure Spec, Render & Review, and Delivery stages.
- UI Automation found all five stable workspace IDs plus
  `ScientificDeliveryFormatList`, `ApproveScientificGateTwo`,
  `RejectScientificGateTwo`, and `ExportScientificDeliveryPackage`.
- Initial Gate 2 and export controls were disabled. After UIA supplied reviewer
  and notes, approval enabled; after approval, decision controls disabled and
  export enabled.
- Selecting PNG produced a nonblank deterministic preview. Local ignored
  screenshots are recorded at
  `outputs/verification/task25-visible-scientific-delivery.png` and
  `outputs/verification/task25-visible-scientific-delivery-png.png` at
  `1170 x 740` with no external overlay.
- The first native probe exposed a WPF crash because display-only `Run.Text`
  bindings defaulted to TwoWay. All scientific display runs now declare
  `Mode=OneWay`, with a regression contract test. Stage AutomationIds were
  moved from peerless `Border` elements to accessible title text peers, and
  narrow source/understanding headers no longer overlap.

## Focused Verification

- Delivery, Checkpoint 4, and layout tests: `11 / 11` passed.
- Delivery, Gate 2, layout, module, main-window, and localization regression
  set: `49 / 49` passed.
- Native WPF probe: process remained responsive after scientific and delivery
  selection; `12` total top-level and nested tabs were observed; all `9`
  required AutomationIds were present.

## Compatibility And N/A

- Paid/live provider: `gate_na`; this checkpoint is deterministic fake-first
  acceptance and makes no network call.
- Persistence/schema: `gate_na`; package persistence already belongs to the
  Task 20 delivery contract, and this slice adds no schema.
- Dependency/supply chain: `gate_na`; WPF, UI Automation, save dialog, and image
  preview use existing framework/runtime dependencies.
- Migration: additive WPF module visibility and DI registration only; no stored
  payload changes.

## Rollback

Revert the Task 25 commit to hide the scientific module again and remove the
Delivery view model, WPF view, save service, visibility wiring, localized
labels, tests, architecture note, and this evidence. Tasks 6-24 and the hidden
machine-trust workflow remain intact.

## Gate History

The first fixed-order attempt passed build with zero warnings and errors, then
stopped at test with `638 / 641` passing because three verification-script tests
correctly detected missing reference evidence for the new DI, workbench, and
scientific-module paths. No later gate was run out of order. This architecture
and task evidence is the required root-cause correction; the fresh complete
fixed-order result is recorded below after rerun.

## Repository Gates

The fresh fixed-order rerun passed on 2026-07-27:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `641 / 641` passed
3. contract/invariant: reference evidence and format checks passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed

Task 25 and Checkpoint 4 are closed. Task 26 corpus acceptance, Tasks 27-29
provider acceptance, Task 30 documentation closeout, and Checkpoint 5 remain
open.
