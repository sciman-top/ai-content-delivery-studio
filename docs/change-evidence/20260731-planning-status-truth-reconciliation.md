# Planning Status Truth Reconciliation Evidence

## Scope And Result

- Date: `2026-07-31`.
- Scope: PRD/design, roadmap, implementation-plan, task-list, V1 snapshot,
  current repository baseline, and remaining-work truth reconciliation.
- Result: the current repository-owned implementation queue is closed. No open
  checkbox remains in `docs/TASKS.md` or under `docs/superpowers/specs/` and
  `docs/superpowers/plans/`.
- `docs/PRD_V1.md` remains the locked V1 promise and contains no open TODO/TBD
  or unchecked implementation item.

## Truth Reconciliation

- The V1 launch snapshot remains frozen at `2026-06-23`; its `433 / 433` test
  count and `2026-06-11` opt-in OpenAI sample are historical snapshot facts.
- The newer repo-only baseline is `734 / 734`, recorded by Phase 7 closeout.
  This does not refresh the paid-provider sample, create a new V1 launch
  snapshot, or relabel historical live evidence.
- The 116 unchecked boxes in
  `docs/reviews/SCIENTIFIC_FIGURE_CHECKPOINT_0_CORPUS_REVIEW.md` are a retained
  historical review worksheet. The scientific workflow is accepted through
  Task 30 and Checkpoint 5, so those worksheet controls are not active backlog.

## Remaining Boundaries

- Manual/live acceptance remains for Narrator, actual system high-contrast
  switching, non-default-DPI hardware, touch/pen ergonomics, subjective WPF
  scroll responsiveness, and real low-memory Windows devices.
- Deferred trigger lanes remain OCR reference coverage, scholarly PDF/GROBID
  references, an additional real scenario pack, and partial-image streaming.
  They become implementation tasks only when their documented trigger occurs
  and a bounded repo-owned slice is opened.
- MSI/MSIX, signing, Store registration, and a new paid-provider evidence run
  are not claimed by this reconciliation.

## Changes

- `docs/TASKS.md` and `docs/ROADMAP.md` now distinguish the frozen V1 snapshot
  from the newer repo-only baseline.
- English and Chinese `V1_LAUNCH_EVIDENCE.md` retain the historical `433 / 433`
  snapshot count while identifying `734 / 734` as the current repo-only result.

## Verification

| Check | Result |
| --- | --- |
| Active task checkbox scan | `TASK_OPEN_COUNT=0` |
| Active spec/plan checkbox scan | `PLAN_SPEC_OPEN_COUNT=0` |
| Historical review worksheet scan | `116` unchecked controls, retained as non-backlog review history |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1` | exit `0`; build has `0` warnings/errors, tests `734 / 734`, reference evidence and format pass |
| `git diff --check` | exit `0` on the final documentation slice |

## N/A And Rollback

- `gate_na`: paid/live-provider refresh. Reason: no provider behavior or release
  claim changed; alternative verification is the current fake-first repository
  gate plus retained live snapshot evidence; evidence is this file and
  `V1_LAUNCH_EVIDENCE.md`; expires when provider behavior materially changes or
  a new release snapshot is explicitly opened; recovery condition is explicit
  paid-call authorization and a new evidence slice.
- Rollback reverts only the four planning/evidence wording updates and this
  evidence file. It does not alter code, accepted live artifacts, workspaces,
  databases, generated packages, or historical snapshot evidence.
