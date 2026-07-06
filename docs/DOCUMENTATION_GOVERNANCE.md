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
| Where is the Chinese reader entrypoint for repository overview and documentation navigation? | [../README.zh-CN.md](../README.zh-CN.md) and [./zh-CN/README.md](./zh-CN/README.md) |
| What is this repository, where is the current root, and how do I start locally? | [README.md](../README.md) |
| What does V1 promise? | [PRD_V1.md](./PRD_V1.md) |
| What source inputs and output artifacts are launch-capable? | [SOURCE_ARTIFACT_SUPPORT_MATRIX.md](./SOURCE_ARTIFACT_SUPPORT_MATRIX.md) |
| What are the provider and operator execution boundaries? | [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md) and [OPERATOR_RISK_POLICY.md](./OPERATOR_RISK_POLICY.md) |
| What is currently proven enough to claim? | [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md) |
| What should be implemented or hardened next? | [ROADMAP.md](./ROADMAP.md) and [TASKS.md](./TASKS.md) |
| How should non-trivial engineering work be executed in this repository? | [AI_CODING_WORKFLOW.md](./AI_CODING_WORKFLOW.md) and [AGENTS.md](../AGENTS.md) |
| What is the best realistic longer-term engineering end state? | [TARGET_ENGINEERING_STATE.md](./TARGET_ENGINEERING_STATE.md) |
| What high-drift engineering changes require explicit reference evidence? | [REFERENCE_EVIDENCE_POLICY.md](./REFERENCE_EVIDENCE_POLICY.md) |
| Which code areas and task families should consult which local references? | [REFERENCE_BASIS.md](./REFERENCE_BASIS.md) |

## Bilingual Companion Surface

The repository now carries a bounded Chinese companion layer for core explanatory docs:

- [../README.zh-CN.md](../README.zh-CN.md)
- [./zh-CN/README.md](./zh-CN/README.md)
- [./zh-CN/USER_GUIDE.md](./zh-CN/USER_GUIDE.md)
- [./zh-CN/PRD_V1.md](./zh-CN/PRD_V1.md)
- [./zh-CN/PRODUCT_DESIGN.md](./zh-CN/PRODUCT_DESIGN.md)
- [./zh-CN/V1_LAUNCH_EVIDENCE.md](./zh-CN/V1_LAUNCH_EVIDENCE.md)

These companion docs are for Chinese-first reading and onboarding. If wording drifts, repository truth still resolves in this order:

1. code, tests, ADRs, scripts, and recorded evidence
2. the canonical English document paths already used by repository rules and automation
3. the Chinese companion documents

Deep governance, reference, workflow, roadmap, and architecture documents may remain English-only until a dedicated translation slice lands. The Chinese documentation hub must still point readers to those English authority surfaces explicitly.

## Status Semantics

- `Locked`: a V1 decision is fixed unless a later PRD or ADR explicitly reopens it.
- `Verified`: the claim is backed by explicit evidence in `V1_LAUNCH_EVIDENCE.md`.
- `Partial`: some implementation or contract evidence exists, but the full launch claim is still open.
- `Started`: implementation or hardening work exists, but this is not by itself a release claim.
- `Deferred`: intentionally post-V1 or later.

## Hard Rules

- `PRD_V1.md` defines launch promise and launch gate. It does not claim current proof.
- `README.md` is the repository overview and local-start entrypoint. It must summarize current posture, but it must defer release-claim truth to `V1_LAUNCH_EVIDENCE.md`.
- `README.zh-CN.md` and `docs/zh-CN/README.md` are the Chinese reader entrypoints. They must link back to the authoritative English files and must not invent status claims that are absent from repo truth.
- `V1_LAUNCH_EVIDENCE.md` is the only core document that should summarize current V1 release-verification status.
- `ROADMAP.md` may describe phase status and next sequencing, but phase status does not equal launch readiness.
- `TASKS.md` is an action backlog, not a release-claim document.
- `AI_CODING_WORKFLOW.md` defines the repository's default implementation discipline for non-trivial engineering work. It does not define product scope or current release status.
- `TARGET_ENGINEERING_STATE.md` is the best-end-state target, not a near-term commitment list.
- `REFERENCE_EVIDENCE_POLICY.md` defines when high-drift engineering changes must leave a visible evidence trail and points to the local verification gate.
- `REFERENCE_BASIS.md` defines the durable `task/code area -> local reference shelf -> reuse level` mapping and should be refreshed when hard-drift engineering areas or the reference shelf change.
- `scripts/sync-reference-governance.ps1` is the machine-sync companion for those two docs. It regenerates the managed summary inside `REFERENCE_BASIS.md` and refreshes the repo-side snapshot of the external shelf manifest.
- When a mirrored English explanatory doc changes user-facing meaning, update its Chinese companion in the same slice or leave an explicit pending-drift note in the affected Chinese file.
- Non-trivial changes should leave a repo-owned spec under `docs/superpowers/specs/` and a repo-owned plan under `docs/superpowers/plans/` unless the slice is small enough to stay within the repository's `XS-S` sizing rules.

## Review Shortcut

When reviewing the repository quickly:

1. Read [README.md](../README.md) for repository scope, local entrypoints, and the current root/rename posture.
2. Read [PRD_V1.md](./PRD_V1.md) for the launch promise.
3. Read [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md) for current proof.
4. Read [ROADMAP.md](./ROADMAP.md) and [TASKS.md](./TASKS.md) for what still needs to happen next.
5. Read [AI_CODING_WORKFLOW.md](./AI_CODING_WORKFLOW.md) before executing a non-trivial slice.
6. Read [REFERENCE_EVIDENCE_POLICY.md](./REFERENCE_EVIDENCE_POLICY.md) and [REFERENCE_BASIS.md](./REFERENCE_BASIS.md) before changing provider, host, persistence, or tooling boundaries.
7. Read [TARGET_ENGINEERING_STATE.md](./TARGET_ENGINEERING_STATE.md) only when deciding how to extend the architecture beyond the current V1 boundary.
