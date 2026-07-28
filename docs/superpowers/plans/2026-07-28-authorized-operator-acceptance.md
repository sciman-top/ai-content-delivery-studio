# Authorized Operator Acceptance Implementation Plan

**Goal:** Encode user-authorized agent operation as truthfully identified but
authority-equivalent `operator/manual evidence`, then finalize the complete
native WPF session without changing live evidence.

## Task 1: Lock Actor And Authorization Semantics

**Acceptance criteria:**

- [x] Existing human finalization records `operatorKind=human` and equivalent
  decision authority.
- [x] A schema v1 accepted session can finalize as `authorized_agent`, migrate
  to schema v2, and retain reviewer/package validation.
- [x] An authorized-agent attempt without an authorization reference fails
  closed and remains pending.

**Write set:**

- `tests/ContentDeliveryStudio.Tests/ScientificFigureOperatorTrialScriptTests.cs`

**Verification:**

- run the two new tests before implementation and preserve the red result
- `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter ScientificFigureOperatorTrialScriptTests`

## Task 2: Implement Schema V2 Authorized Operator Finalization

**Acceptance criteria:**

- [x] `OperatorKind` validates `human|authorized_agent`; human remains default.
- [x] `AuthorizationReference` is mandatory for `authorized_agent`.
- [x] Finalization writes schema v2, `authorized_operator_v1`, truthful actor
  identity, authorization provenance, and `equivalent_operator_acceptance`.
- [x] Existing workspace confirmation, reviewer, ZIP, and hash gates are
  unchanged.
- [x] Schema v1 pending sessions and relocated ignored session paths migrate
  safely; finalized records remain immutable.

**Dependencies:** Task 1

**Write set:**

- `scripts/run-scientific-figure-operator-trial.ps1`

**Verification:**

- rerun the focused script suite and require every test to pass

## Task 3: Align Guidance And Finalize The Authorized Session

**Acceptance criteria:**

- [x] English and Chinese guidance separates actor identity, authorization, and
  equivalent decision authority.
- [x] Tasks and launch evidence record the authorized-agent result without
  changing live-provider acceptance.
- [x] Session `20260728-204423-261` validates and finalizes as accepted with the
  exact Gate 2 reviewer and package SHA-256.
- [x] Session `20260728-214821-253` remains `awaiting_finalize / pending_operator`.
- [x] New repository evidence records the policy decision, runtime result,
  exclusions, gates, and rollback.

**Dependencies:** Task 2

**Write set:**

- `docs/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md`
- `docs/zh-CN/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md`
- `docs/USER_GUIDE.md`
- `docs/zh-CN/USER_GUIDE.md`
- `docs/TASKS.md`
- `docs/V1_LAUNCH_EVIDENCE.md`
- `docs/zh-CN/V1_LAUNCH_EVIDENCE.md`
- `docs/change-evidence/20260728-scientific-figure-authorized-agent-acceptance.md`
- this plan

**Runtime evidence outside Git:**

- `outputs/scientific-figure-operator-trials/20260728-204423-261/trial.json`

## Checkpoint: Fixed-Order Repository Closeout

- [x] Build: `dotnet build ContentDeliveryStudio.sln`
- [x] Test: `dotnet test ContentDeliveryStudio.sln --no-build`
- [x] Contract/invariant:
  `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
- [x] Contract/invariant: `dotnet format --verify-no-changes`
- [x] Hotspot:
  `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`
- [x] Review and stage only this slice; preserve `AGENTS.md`; do not push.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Agent is falsely labeled human | Store `operatorKind=authorized_agent`; never rewrite identity. |
| Unattended automation self-accepts | Require explicit authorization reference, five-workspace confirmation, reviewer, notes, and package gates. |
| Existing human usage breaks | Default `OperatorKind` to `human` and retain existing arguments. |
| Legacy pending record cannot finalize | Migrate schema v1 only during successful finalization and test it. |
| Live evidence is conflated | Keep `liveAccepted=false`; do not call or refresh providers. |

## Approval

The user explicitly authorized agent-operated acceptance as a replacement with
the same authority as human operation on 2026-07-28. This approval does not
authorize paid provider calls, push, or acceptance of an incomplete session.
