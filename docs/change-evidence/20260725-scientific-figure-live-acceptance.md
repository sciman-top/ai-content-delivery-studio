# Scientific Figure Live Acceptance Task 29 Evidence

Date: 2026-07-27

## Status

`accepted`

The opt-in paid-provider machine path completed for all three representative
scientific-figure categories. The designated human reviewer then inspected and
accepted all three final PNGs with no corrections. Task 29 is accepted. Task 30
and Checkpoint 5 were subsequently accepted in
`docs/change-evidence/20260725-scientific-figure-closeout.md`.

## Authorized Live Run

- Command: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-live-acceptance.ps1 -OutputPath artifacts/scientific-figure-live-acceptance/20260727-150622/report.json`
- Run id: `20260727-150622`
- Provider: `OpenAI-compatible Responses`
- Model: `gpt-5.5`
- Paid calls authorized: `true`
- Machine path: `passedMachinePath=true`
- Human-review readiness: `readyForHumanReview=true`
- Final acceptance: `accepted=true`
- Blocking reason: none

The ignored artifact report is the detailed local evidence authority. It
preserves per-call request/provider trace ids, latency, input/output/cached and
reasoning token usage, source and payload hashes, render/export hashes, review
results, gate states, and artifact directories. Secrets, authorization headers,
base64 image bodies, and absolute local paths are omitted from request evidence.

## Representative Samples

| Category | Item | Machine result | Provider calls | Total tokens | Gate 2 |
| --- | --- | --- | ---: | ---: | --- |
| mechanism/process | `electromagnetism-rotating-coil-generator` | understanding unblocked; contract, semantic, and visual review passed | 3 | 7,731 | `accepted` |
| concept comparison | `thermal-three-mode-heat-transfer-comparison` | understanding unblocked; contract, semantic, and visual review passed | 3 | 8,780 | `accepted` |
| graphical abstract | `quantum-photoelectric-threshold-summary` | understanding unblocked; contract, semantic, and visual review passed | 3 | 6,368 | `accepted` |

Each sample reuses its Checkpoint 0 human-approved baseline as Gate 1 authority
without mutation. Each machine review used one scientific-understanding call,
one semantic-review call, and one full-resolution visual-review call. Across the
run, all `9 / 9` provider calls returned HTTP `200` and the three machine review
decisions passed.

## Cost Boundary

The report records all token usage but has
`cost.status=unpriced-no-local-rate-card` and `estimatedAmount=null`. No billing
amount is invented. Configuring a reviewed local rate card is required before a
currency estimate can be asserted; the provider's external bill remains the
authority for actual spend.

## Human Gate Decision

- Decision: all three Gate 2 items accepted
- Reviewer: `sciman`
- Reviewed at: `2026-07-27T23:57:09.9758439+08:00`
- Corrections: none

The ignored report and all three review bundles preserve the same reviewer,
review time, decision, and empty correction record. Task 29 is complete. Task
30 and Checkpoint 5 later closed under the dedicated closeout evidence.

## Harness And Regression Coverage

- The live harness is explicit opt-in and fails fast after a blocked
  understanding result, avoiding unnecessary paid review calls.
- Resume requires matching current renderer, artifact, provider-call, and Gate
  2 state evidence; stale checkpoints cannot be silently reused.
- The understanding mapper keeps provider proposals in domain-valid `Draft`
  state and validates limitations without representing machine anchoring as a
  human Gate 1 decision.
- Deterministic rendering and export retain exact scientific relation authority
  while using separate readable display labels and stable scientific glyphs.
- `VerifyRepoScriptTests` now pass `-SkipReferenceEvidence` because those tests
  exclusively exercise build retry/fail-closed behavior. The real reference
  evidence gate remains enabled in repository closeout. This removes coupling
  to unrelated dirty-worktree evidence without weakening production gates.

Focused verification:

- live harness command: exit `0`; `3 / 3` samples machine-ready
- renderer, exporter, understanding, and live-harness regression set: `26 / 26`
- verify-repo build retry/fail-closed regression set: `3 / 3`

## Compatibility, Supply Chain, And Rollback

- Default runtime remains fake-first; live execution still requires explicit
  opt-in and valid local provider configuration.
- No dependency, package, lock-file, persistence schema, or migration changed.
- Generated live artifacts and `.env` remain Git-ignored and must not be
  committed.
- Rollback is the Task 29 commit only. It removes the live harness, renderer and
  contract adjustments, regression tests, and this evidence while preserving
  the already-accepted Task 28 provider contracts.

## Repository Gates

The first post-evidence fixed-order repository run passed on 2026-07-27:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `663 / 663` passed
3. contract/invariant: reference governance/evidence and format passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed; the nested suite also
   passed `663 / 663`

The initial contract attempt correctly failed because only this change-evidence
file had changed, while the reference policy requires an approved provider and
scientific-workflow basis path. The implementation plan now records the exact
machine-ready/pending-human boundary without checking Task 29 acceptance, and
the reference gate then passed for both enforced areas.

A second fresh post-evidence fixed-order run also passed with build `0`
warnings/errors, test `663 / 663`, reference evidence, format, and the complete
release preflight. The final commit preflight repeats the same fixed order so
the submitted tree is covered by fresh evidence.

After the Gate 2 human decision, the acceptance-closeout fixed-order run passed
again with build `0` warnings/errors, test `664 / 664`, reference evidence,
format, and complete release preflight. The additional regression proves
concurrent native PDF exports remain readable; its root-cause fix is recorded
separately in `docs/change-evidence/20260727-scientific-pdf-concurrency-fix.md`.
