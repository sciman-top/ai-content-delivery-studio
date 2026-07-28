# Scientific Figure Fake-First Operator Trial Evidence

Date: 2026-07-28

## Status

`implementation in progress / operator trial pending`

## Scope

This slice adds a reusable operator-trial kit around the already-accepted
scientific-figure WPF path. It creates isolated fake-first sessions, starts the
existing five workspaces, validates accepted delivery ZIPs, and records explicit
human accepted or rejected outcomes.

It does not reopen scientific Tasks 1-30 or Checkpoints 0-5, call a paid
provider, refresh live evidence, modify accepted artifacts, or claim that
automation is a human onsite acceptance.

## Implementation

- `LocalStudioDataPaths` honors an opt-in
  `CONTENT_DELIVERY_STUDIO_DATA_ROOT` while preserving existing default and
  scoped-test behavior.
- `run-scientific-figure-operator-trial.ps1` provides `Prepare`, `Run`, and
  `Finalize` modes with unique ignored sessions, fake-provider enforcement,
  environment restoration, and overwrite protection.
- Accepted finalization validates required ZIP entries, relative unique paths,
  secret-like names, absolute local path leakage, Gate 2 approval/reviewer, and
  SVG/PNG/PDF hashes.
- Rejected finalization records human rejection without manufacturing a package
  or accepted outcome.
- English and Chinese runbooks preserve the evidence and exclusion boundaries.

## Evidence Boundary

- Repo-side implementation: pending fixed-order repository gates.
- Prepared probe: `pending_operator`; it is ignored local output and not human
  evidence.
- Human operator trial: pending. No agent-authored record is represented as a
  human decision.
- Existing live acceptance: unchanged; no provider call occurred.

## Focused Verification

- `LocalStudioDataPathsTests`: `6 / 6` passed.
- `ScientificFigureOperatorTrialScriptTests`: `6 / 6` passed.
- No-launch probe created
  `outputs/scientific-figure-operator-trials/repo-probe-20260728-01/trial.json`
  with `status=pending_operator`, `providerMode=fake`, and
  `liveAccepted=false`.

## Compatibility And N/A

- Provider behavior: unchanged; fake is forced only for the trial child
  process. Live evidence refresh is `gate_na` because no provider contract
  changed.
- Persistence/schema: no migration or schema change. The isolated root changes
  only the file location when explicitly opted in.
- WPF UI: no view, binding, or domain behavior changed; the kit exercises the
  already-accepted workspaces.
- Accepted artifacts: unchanged and outside the write set.
- Human onsite acceptance: pending; recovery condition is a real operator
  completing and finalizing a session.

## Rollback

Revert the operator-trial commits to remove the environment override, script,
tests, and runbooks. Existing scientific code and accepted evidence remain
unchanged. Generated session data under `outputs/` must be retained or removed
separately according to operator evidence needs.

## Repository Gates

Fixed-order closeout evidence will be appended only after the submitted tree
passes build, test, contract/invariant, and hotspot gates.
