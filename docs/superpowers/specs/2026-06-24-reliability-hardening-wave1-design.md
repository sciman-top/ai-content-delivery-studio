# Reliability Hardening Wave 1

## Goal

Record the first repository-owned reliability hardening slice for AI Content Delivery Studio.

This slice focuses on high-risk runtime boundaries that already exist in the shipped V1 baseline:

- persistence save behavior for detached aggregates
- file and path safety for local outputs and secrets
- OpenAI provider failure mapping and invalid-response handling
- UI async state flow for refresh/load/warmup behavior

## Context

The repository currently verifies cleanly through:

- `.\scripts\verify-repo.ps1 -NoRestore`
- `433 / 433` tests

That baseline proves current intended paths work, but it does not guarantee that failure paths are explicit, deterministic, and regression-tested.

This hardening wave is intentionally behavior-preserving at the public-contract level.

## Scope

In scope:

- Add repo-owned spec and plan for this wave
- Strengthen infrastructure fail-closed behavior
- Remove silent overwrite or partial-write risks in local file outputs where practical
- Tighten detached aggregate persistence behavior without schema changes
- Strengthen provider parsing and telemetry failure branches
- Reduce fire-and-forget async risk in the main workbench view model
- Add focused regression tests for all touched boundaries

Out of scope:

- schema migrations
- new product capabilities
- pack-system expansion
- remote workflow changes
- live OpenAI evidence refresh
- broad public API redesign

## Inputs And Outputs

Inputs:

- current repository verification baseline
- existing V1 contracts and WPF bindings
- existing OpenAI provider policy and fake-first posture

Outputs:

- repo-owned implementation plan
- additive hardening helpers and tests
- stronger failure semantics for local persistence, file writes, provider parsing, and UI async flow
- fresh verification evidence from focused tests and full gates

## Failure Modes To Prevent

- duplicate delivery item keys silently overwriting exported artifacts
- partially written local output files surviving failures
- invalid provider JSON surfacing as ambiguous parse failures
- detached aggregate saves missing nested additions or drifting under repeated updates
- background UI tasks swallowing exceptions without state protection
- unsafe local path inputs escaping intended app-local storage roots

## Acceptance Criteria

- Delivery/export, thumbnail, and secret-store writes are atomic or fail closed with cleanup.
- Detached aggregate persistence has explicit regression coverage for nested new children and update paths.
- OpenAI provider tests cover invalid JSON, missing request IDs, and failure telemetry mapping.
- Main workbench async flows no longer rely on unobserved fire-and-forget tasks for important state transitions.
- Public contracts remain compatible: no schema changes, no manifest shape break, no WPF binding break.
- `.\scripts\verify-repo.ps1 -NoRestore` passes after the slice.
- `.\scripts\preflight-release.ps1 -NoRestore` passes after the slice.

## Implementation Outcome

This wave completed as an additive hardening slice.

- Atomic file-write semantics were consolidated into one shared helper and adopted by delivery/export, diagnostics, DPAPI secret persistence, thumbnail cache, and OpenAI artifact/report writing.
- Detached aggregate persistence moved from repeated child-by-child existence probing to one snapshot-based add/update normalization path for nested aggregate saves with assigned Guid identities.
- OpenAI provider parsing now turns invalid top-level JSON and invalid structured payload JSON into explicit `InvalidOperationException` branches with stable messages while preserving existing provider contracts.
- Main workbench startup, selection-driven plan loading, refresh revision checks, and gallery warmup now run through tracked background observation so stale results are dropped and best-effort warmup failures are observed in one place.

## Evidence Summary

- Focused regression coverage now includes duplicate delivery keys, staged package rollback, unsafe local path rejection, detached aggregate repeated-save behavior, invalid provider JSON, invalid SDK image payload parsing, startup refresh resilience, and gallery warmup cancellation behavior.
- Final repository verification for this slice passed through `dotnet test`, `.\scripts\verify-repo.ps1 -NoRestore`, and `.\scripts\preflight-release.ps1 -NoRestore`.

## Non-Goals

- redesigning the entire repository architecture
- splitting every large file in one wave
- replacing fake-first defaults with real-provider-first behavior
- changing V1 launch claims or delivery package format
