# V1 Documentation Governance Design

## Purpose

Tighten the repository's core product and launch documents so they stop mixing three different kinds of truth:

- product promise
- current implementation status
- current launch verification status

The goal is not to rewrite product direction. The goal is to make the current documentation set harder to misread during autonomous implementation and release-readiness reviews.

## Problem

The repository already has strong design and policy coverage, but the top-level docs still drift in a few ways:

- `PRD_V1.md` defines the launch promise, while `ROADMAP.md` and `TASKS.md` sometimes read like proof of current readiness.
- `V1_LAUNCH_EVIDENCE.md` is the most honest current-status file, but other docs do not consistently defer to it for release claims.
- `TARGET_ENGINEERING_STATE.md` is intentionally long-term, but readers can still mistake it for near-term backlog.
- Some roadmap wording is stale after recent automated evidence landed for deterministic composition and the first low-risk operator slice.

## Design Goals

1. Make document authority explicit.
2. Make status vocabulary consistent.
3. Separate release blockers from non-blocking hardening.
4. Preserve the current V1 scope instead of broadening it.

## Non-Goals

- Reopening V1 scope decisions.
- Replacing the current phase-based roadmap structure.
- Turning the target-state document into a backlog.
- Adding new engineering features in this slice.

## Authority Model

The durable answer to each question should be:

- What does V1 promise? `PRD_V1.md`
- What inputs/outputs are launch-capable? `SOURCE_ARTIFACT_SUPPORT_MATRIX.md`
- What are the provider and operator execution boundaries? `PROVIDER_ROUTING_POLICY.md` and `OPERATOR_RISK_POLICY.md`
- What is currently proven enough to claim? `V1_LAUNCH_EVIDENCE.md`
- What should we do next? `ROADMAP.md` and `TASKS.md`
- What is the best longer-term end state? `TARGET_ENGINEERING_STATE.md`

## Status Vocabulary

- `Locked`: a V1 decision is fixed unless a later PRD or ADR reopens it.
- `Verified`: the claim is backed by explicit evidence in `V1_LAUNCH_EVIDENCE.md`.
- `Partial`: some implementation or contract evidence exists, but the full launch claim is still open.
- `Started`: implementation or hardening work exists, but this is not a release claim.
- `Deferred`: intentionally post-V1 or later.

## Planned Changes

1. Add a new `docs/DOCUMENTATION_GOVERNANCE.md` entrypoint describing document roles and allowed status semantics.
2. Update `README.md` to surface the governance entrypoint.
3. Update `PRD_V1.md` to say it defines the launch promise, not current proof.
4. Update `ROADMAP.md` to:
   - point release-readiness truth to `V1_LAUNCH_EVIDENCE.md`
   - remove stale wording around deterministic composition and operator-proof status
   - clarify that phase status is not a launch claim
5. Update `TARGET_ENGINEERING_STATE.md` to state that it is not a current-status or release-readiness document.
6. Update `V1_LAUNCH_EVIDENCE.md` to make its authority role explicit.
7. Update `TASKS.md` to hard-split:
   - current V1 release gap
   - near-term hardening that is useful but not a release blocker

## Acceptance Criteria

- A reviewer can tell, without inference, which document governs promise, proof, backlog, and long-term target state.
- No core document implies that completed implementation slices automatically equal release readiness.
- `TASKS.md` no longer makes non-blocking hardening look like the last required V1 ship items.
- `ROADMAP.md` no longer contradicts the current evidence ledger on deterministic composition or low-risk operator proof.

## Rollback

Revert this documentation slice if the governance model proves more confusing than the current arrangement. The change is docs-only and should be recoverable through git without touching product code.
