# Trustworthy Scientific Figure Closeout Evidence

Date: 2026-07-28

## Status

`Task 30 accepted / Checkpoint 5 accepted`

This file closes documentation and evidence only after the implementation,
fake-first, live-provider, and human-acceptance layers each have direct
authority. It does not replace the detailed task evidence or the ignored local
artifact reports.

## Acceptance Ledger

| Layer | Result | Authority |
| --- | --- | --- |
| Implemented | Tasks 1-30 accepted | Approved design and 30-task plan, task-specific change evidence, and repository tests cover extraction through delivery and documentation closeout. |
| Fake-first | `12 / 12` baselines passed; `40 / 40` blocking mutations rejected | Fresh `scripts/run-scientific-figure-corpus.ps1` output at `artifacts/scientific-figure-corpus-acceptance/report.json`. |
| WPF and both gates | Five workspaces, Gate 1, Gate 2 approval/rejection, and package export proven | `ScientificCheckpointFourTests`, `ScientificFigureSpecWorkspaceViewModelTests`, `ScientificDeliveryViewModelTests`, and `ScientificFigureDeliveryPackageTests`. |
| Live provider | `3 / 3` representative samples machine-ready | `artifacts/scientific-figure-live-acceptance/20260727-150622/report.json` and the Task 29 committed summary. |
| Human Gate 2 | `3 / 3` accepted, no corrections | Reviewer `sciman`, reviewed at `2026-07-27T23:57:09.9758439+08:00`; report and three review bundles agree. |
| Final closeout | Accepted | Task 30 fixed-order gate, documentation scans, publish WhatIf, and diff hygiene pass; Checkpoint 5 is accepted. |

## Documentation Synchronization

- `USER_GUIDE.md` and its Chinese companion distinguish implemented,
  fake-first verified, live verified, and human accepted states and describe the
  five-workspace operator path.
- `TASKS.md` records Tasks 1-30 and Checkpoint 5 accepted with no remaining
  scientific-figure implementation task.
- `ROADMAP.md` replaces the stale pre-implementation claim with the accepted
  post-V1 boundary and explicit exclusions.
- `V1_LAUNCH_EVIDENCE.md` and its Chinese companion keep the historical V1
  `5 / 5` snapshot separate from post-V1 scientific acceptance.
- The implementation plan remains the task/checkpoint authority; this evidence
  is the closeout summary.

## Migration And Compatibility

- Scientific persistence was introduced additively; existing image-series
  projects require no manual schema migration and old workspaces remain
  readable.
- Provider contracts remain independently selectable for understanding,
  semantic review, and visual review. Fake mode remains the default.
- Accepted live evidence is local and Git-ignored. No secret, `.env`, SQLite
  database, workspace, or generated binary is committed.
- The final PDF concurrency fix serializes only the native Skia writer and does
  not change export formats, semantic hashes, dimensions, or public contracts.

## Operator And Rollback Guidance

- Use Source -> Understanding -> Figure Spec -> Render & Review -> Delivery.
  Gate 1 freezes scientific authority; Gate 2 requires deterministic and both
  machine reviews plus explicit human fields.
- Do not rerun the paid live command to inspect existing accepted evidence.
  Refresh only after explicit approval when provider behavior or the contract
  materially changes.
- Return to fake provider mode to stop live execution. Preserve local
  SQLite/workspace data and accepted artifact directories before code rollback;
  Git cannot restore runtime data.
- Reverting code removes behavior, not already-recorded human decisions. Rebuild
  a new immutable package after any accepted content changes.

## Excluded Capabilities

Acceptance does not cover OCR-heavy sources, measured or fabricated data plots,
microscope-like observations, automatic changes to scientific meaning, or
generated visuals represented as observed experimental evidence. These remain
blocked or require a new repo-owned spec, plan, evidence, and human acceptance.

## Final Verification

The required order remains:

The final post-evidence fixed-order closeout run passed on 2026-07-28:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `664 / 664` passed
3. contract/invariant: reference governance/evidence and format passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed; the nested suite also
   passed `664 / 664`

This submitted-tree verification passed before the closeout commit was
created.
