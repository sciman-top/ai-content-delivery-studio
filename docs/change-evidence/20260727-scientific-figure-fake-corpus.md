# Scientific Figure Fake Corpus Task 26 Evidence

Date: 2026-07-27

## Scope

This evidence records Task 26 offline acceptance for the 12-item,
human-approved scientific-figure corpus. It replays accepted baseline authority
through real domain and application contracts and writes a deterministic local
report. It makes no network request and no paid provider call.

This does not prove live-provider quality, generalize beyond the fixed corpus,
or complete human Gate 2 acceptance. Tasks 27-30 and Checkpoint 5 remain
separate gates.

## Authority And Replay

- The loader accepts only corpus `admissionState: human-approved`, item
  `admissionStatus: accepted`, baseline `humanReview.status: accepted`, matching
  item IDs, and matching source hashes. Baseline paths must remain inside the
  repository.
- Each item is rebuilt as immutable source blocks, validated claim evidence,
  complete understanding coverage, evidence-bound Figure Spec elements and
  relations, Gate 1 approval, deterministic render plan, SVG, one PNG, one PDF,
  contract review, and provider-neutral fake semantic/visual review.
- A valid item reaches `ReviewPassed` only when deterministic contract review
  and both fake machine-review layers pass. Gate 2 remains outside this runner.
- Scientific mutations remove required render-plan authority and are blocked by
  `ScientificContractReviewer`. Visual mutations emit item-addressable fake
  findings and are blocked through `ScientificReviewExecutionService`; expected
  outcomes are never copied into actual outcomes as evidence.

## Deterministic Report

- Command: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-corpus.ps1`
- Ignored local artifact:
  `artifacts/scientific-figure-corpus-acceptance/report.json`
- Result: `12 / 12` accepted baselines reached `ReviewPassed`; all `40 / 40`
  declared mutations were blocked with a finding code and responsible item ID.
- Two in-process runs serialize byte-for-byte identical JSON. The report omits
  timestamps, absolute paths, random IDs, provider secrets, and source binaries.

## Compatibility And N/A

- Paid/live provider: `gate_na`; reason: Task 26 is explicitly offline and
  fake-first. Recovery condition: Tasks 27-29 opt-in live-provider acceptance.
- WPF/screenshot/UI Automation: `gate_na`; reason: no WPF or user-visible layout
  changed. Alternative verification: application-level corpus replay.
- Persistence/schema: `gate_na`; no persisted payload or SQLite schema changed.
- Dependency/supply chain: no dependency or package change; existing renderer,
  exporter, Skia cropper, JSON, and test infrastructure are reused.
- Migration: additive runner, report contract, script, tests, and documentation
  only; existing scientific workflow and provider contracts remain compatible.

## Rollback

Revert the Task 26 commit to remove the corpus loader, runner, acceptance test,
script, architecture note, task state, context term, and this evidence. Tasks
6-25 and Checkpoint 4 remain intact; ignored report artifacts can be deleted
independently.

## Repository Gates

The first fresh fixed-order run passed on 2026-07-27:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `643 / 643` passed
3. contract/invariant: reference evidence and format checks passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed

After this final evidence update, the second fresh fixed-order run also passed
with the same `0` build warnings/errors and `643 / 643` tests, followed by
passing reference evidence, format, and release preflight gates.

Task 26 is closed. Tasks 27-30 and Checkpoint 5 remain open.
