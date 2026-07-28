# Authorized Operator Acceptance

## Goal

Allow a user-authorized agent that interactively operates the native WPF trial
to make an acceptance or rejection decision with the same evidentiary authority
as a human operator, while preserving the actor's truthful identity and every
existing scientific/package gate.

## Product Decision And Truth Boundary

On 2026-07-28 the user explicitly decided that an agent-operated trial is a
replacement for, and equivalent in acceptance authority to, a human-operated
trial. The repository must encode that decision directly instead of describing
the agent as human or leaving a valid authorized decision permanently pending.

`operator/manual evidence` means an authorized operator manually inspected the
native WPF workspaces and made an explicit decision. The operator may be:

- `human`; or
- `authorized_agent`, with a non-empty traceable authorization reference.

Actor identity and decision authority remain separate. An agent is never stored
as `human`, but both kinds receive `equivalent_operator_acceptance` authority
after satisfying the same finalization checks.

## Considered Approaches

1. Relabel the agent as human. Rejected because it falsifies actor identity.
2. Add an authorized-operator actor kind with equal authority. Selected because
   it preserves provenance and implements the user's product decision.
3. Treat any automated pass as acceptance. Rejected because tests and package
   validation do not prove an authorized visible inspection or decision.

## Contract

The trial script adds:

- `-OperatorKind human|authorized_agent`, defaulting to `human` for compatibility;
- `-AuthorizationReference`, required and non-empty for `authorized_agent`;
- schema v2 record fields for the acceptance policy and operator authority.

Successful finalization still requires all five workspace confirmation,
reviewer and notes, accepted/rejected outcome, Gate 2 reviewer identity match,
and, for acceptance, the existing safe ZIP structure and SVG/PNG/PDF hash
checks. Agent operation receives no reduced quality gate.

Schema v1 pending sessions remain readable. Finalization migrates their record
to schema v2, normalizes the current session paths, and records the actor kind,
authorization reference, and equivalent decision authority. Already-finalized
records remain immutable.

## Acceptance Criteria

- Human finalization remains backward-compatible and records `operatorKind=human`.
- An authorized agent can finalize accepted or rejected evidence only with a
  non-empty authorization reference.
- Both actor kinds produce `operator/manual evidence` and
  `equivalent_operator_acceptance` after the same five-workspace/package gates.
- Missing agent authorization fails closed and leaves the session pending.
- A schema v1 pending session can be finalized and upgraded to schema v2.
- English and Chinese runbooks explain identity versus authority and provide
  exact commands for both actor kinds.
- The complete native WPF session `20260728-204423-261` is finalized under the
  new policy; incomplete session `20260728-214821-253` remains pending.
- Existing live accepted evidence is not refreshed and no paid provider runs.
- Fixed-order verification passes: build, test, contract/invariant, hotspot.

## Non-Goals

- accepting unattended tests, UI Automation, or package validation by itself;
- changing Gate 1, Gate 2, provider, scientific review, or delivery contracts;
- rewriting an agent identity as human;
- refreshing live-provider evidence or changing Future Trigger Lane exclusions;
- committing SQLite, workspace, outputs, screenshots, or ZIP files.

## Write Set

- `scripts/run-scientific-figure-operator-trial.ps1`
- `tests/ContentDeliveryStudio.Tests/ScientificFigureOperatorTrialScriptTests.cs`
- English and Chinese operator-trial runbooks and user-guide entries
- `docs/TASKS.md`
- English and Chinese `V1_LAUNCH_EVIDENCE.md`
- this spec and its implementation plan
- `docs/change-evidence/20260728-scientific-figure-authorized-agent-acceptance.md`

The ignored session `outputs/scientific-figure-operator-trials/20260728-204423-261/trial.json`
is runtime evidence, not a Git write-set item.

## Rollback

Revert this repository slice to restore human-default-only finalization. The
already-finalized ignored session remains historical runtime evidence and must
not be silently rewritten or deleted by Git rollback. Existing live acceptance,
accepted corpus, scientific tasks, and the incomplete pending session remain
unchanged.
