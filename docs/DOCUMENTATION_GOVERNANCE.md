# Documentation Governance

## Purpose

This document explains how to read the repository's core product and launch documents without mixing:

- V1 promise
- current implementation status
- current launch verification status
- long-term target state

## Authority Map

Use these documents in this order:

| Question | Authoritative document |
| --- | --- |
| What does V1 promise? | [PRD_V1.md](./PRD_V1.md) |
| What source inputs and output artifacts are launch-capable? | [SOURCE_ARTIFACT_SUPPORT_MATRIX.md](./SOURCE_ARTIFACT_SUPPORT_MATRIX.md) |
| What are the provider and operator execution boundaries? | [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md) and [OPERATOR_RISK_POLICY.md](./OPERATOR_RISK_POLICY.md) |
| What is currently proven enough to claim? | [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md) |
| What should be implemented or hardened next? | [ROADMAP.md](./ROADMAP.md) and [TASKS.md](./TASKS.md) |
| What is the best realistic longer-term engineering end state? | [TARGET_ENGINEERING_STATE.md](./TARGET_ENGINEERING_STATE.md) |

## Status Semantics

- `Locked`: a V1 decision is fixed unless a later PRD or ADR explicitly reopens it.
- `Verified`: the claim is backed by explicit evidence in `V1_LAUNCH_EVIDENCE.md`.
- `Partial`: some implementation or contract evidence exists, but the full launch claim is still open.
- `Started`: implementation or hardening work exists, but this is not by itself a release claim.
- `Deferred`: intentionally post-V1 or later.

## Hard Rules

- `PRD_V1.md` defines launch promise and launch gate. It does not claim current proof.
- `V1_LAUNCH_EVIDENCE.md` is the only core document that should summarize current V1 release-verification status.
- `ROADMAP.md` may describe phase status and next sequencing, but phase status does not equal launch readiness.
- `TASKS.md` is an action backlog, not a release-claim document.
- `TARGET_ENGINEERING_STATE.md` is the best-end-state target, not a near-term commitment list.

## Review Shortcut

When reviewing the repository quickly:

1. Read [PRD_V1.md](./PRD_V1.md) for the launch promise.
2. Read [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md) for current proof.
3. Read [ROADMAP.md](./ROADMAP.md) and [TASKS.md](./TASKS.md) for what still needs to happen next.
4. Read [TARGET_ENGINEERING_STATE.md](./TARGET_ENGINEERING_STATE.md) only when deciding how to extend the architecture beyond the current V1 boundary.
