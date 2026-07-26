# Scientific Review Contracts Task 17 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 17 of the trustworthy scientific-figure workflow.
The slice adds:

- an independent provider-neutral scientific semantic review contract
- an independent provider-neutral full-resolution visual review contract
- accepted-claim and minimum-evidence request projection
- bounded render element/relation summaries without provider prompts or history
- typed, item-addressable visual region crop contracts
- deterministic semantic and visual fake providers
- strict result execution and mapping into machine-review blockers

It does not build review manifests or crops, call a live provider, approve Gate
2, expose WPF UI, run automatic repair, or persist final delivery evidence.

## Authority And Data-Minimization Contract

- Semantic requests are built only when understanding, specification, and
  render-plan identities match.
- Only `Accepted` claims referenced by non-forbidden specification provenance
  are included.
- Evidence is deduplicated to the exact validated links used by the Figure
  Spec; unrelated source blocks, claims, and provider/planner history are not
  sent.
- Render input is projected to item IDs, meaning, exact content, relation
  endpoints/direction, and criticality rather than exposing mutable workflow
  state.
- Visual requests require original pixel dimensions and reject downscaled,
  empty, untyped, out-of-bounds, or non-addressable crops.

## Fail-Closed Mapping

- Semantic and visual providers execute as separate contracts.
- `Fail` and `Uncertain` verdicts create blockers.
- A provider `Pass` carrying any finding still creates blockers, including a
  `MissingElement` finding.
- Null/incomplete/undefined provider output creates `invalid-provider-output`.
- Provider exceptions create a redacted `provider-failure` blocker containing
  only the exception type; cancellation requested by the caller is preserved.
- `CanProceedToGate2` is true only when both independent blocker collections
  are empty. It is readiness metadata, not Gate 2 approval.

## Focused Verification

- combined contract/fake command: exit `0`; `9 / 9` passed
- `ScientificReviewProviderContractTests`: request minimization,
  full-resolution typed crops, and downscale rejection
- `FakeScientificReviewProviderTests`: independent pass, fail, uncertain,
  missing-element, invalid-output, and provider-failure routing

## Compatibility And N/A

- Paid/live provider: `gate_na`; Task 17 uses deterministic fakes only.
- Runtime dependency/supply chain: `gate_na`; no package or lockfile changed.
- Persistence/schema: `gate_na`; no stored schema changes.
- WPF acceptance: deliberately open; `ScientificFigureModule.IsUserVisible`
  remains false.
- Existing generic article/image review contracts remain unchanged.

## Rollback

Revert the Task 17 commit to remove the contracts, fakes, tests, terminology,
status, and evidence. Tasks 6-16 remain valid and Gate 2 remains unavailable.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`; `0` warnings, `0` errors
- test: exit `0`; `580 / 580` passed
- contract/invariant: reference evidence exit `0`; format verification exit `0`
- hotspot: preflight exit `0`; repository verification, publish WhatIf,
  placeholder/conflict scans, and staged/unstaged diff hygiene passed
