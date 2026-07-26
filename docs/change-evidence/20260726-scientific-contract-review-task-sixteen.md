# Scientific Contract Review Task 16 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 16 of the trustworthy scientific-figure workflow.
The slice adds deterministic comparison across:

- the human-approved Scientific Figure Spec
- the compiled SVG Render Plan
- the editable SVG authority and its computed SHA-256 identity
- the PNG/PDF export bundle, semantic fixtures, and artifact hash bindings

It does not add provider-backed scientific review, visual-quality review,
automatic repair, Gate 2, WPF availability, or live-provider acceptance.

## Fail-Closed Contract

- Missing required elements or relations are hard failures.
- Scientific plan/SVG items without specification authority are hard failures.
- Element kind, exact formula/value/unit text, criticality, and provenance must
  remain equal across specification, plan, and SVG.
- Relation endpoints, kind, direction, arrow markers, label, representation,
  and provenance must remain equal.
- SVG bytes must match the artifact SHA-256 and embedded plan/spec identity.
- PNG and PDF must each exist exactly once and retain source-SVG, semantic, and
  artifact-content hash bindings.
- `AdvisoryScore` is bounded metadata only. `Passed` is true only when the hard
  failure collection is empty, so a score of `1` cannot override a mutation.
- Every finding records a stable code, failed invariant, responsible item,
  concrete observed evidence, and owning repair layer.

## Mutation Evidence

Focused command:

`dotnet test ContentDeliveryStudio.sln --no-restore --filter "ScientificContractReviewTests|ScientificMutationTests"`

Fresh focused outcome: exit `0`; `9 / 9` passed. The suite proves the approved
chain passes and blocks missing required content, extra scientific content,
reversed arrow markers, formula drift, numeric-value drift, unit drift, and
export-byte drift. The missing-element case uses `AdvisoryScore = 1` and still
fails.

## Compatibility And N/A

- Provider/live calls: `gate_na`; the reviewer is local and deterministic.
- Runtime dependency/supply chain: `gate_na`; no package or lockfile changes.
- Persistence/schema: `gate_na`; no stored contract changes in this task.
- WPF acceptance: deliberately open; `ScientificFigureModule.IsUserVisible`
  remains false.
- Existing article/image review flows remain unchanged.

## Rollback

Revert the Task 16 commit to remove the report model, deterministic reviewer,
mutation tests, terminology, task status, and this evidence. Tasks 6-15 and
Checkpoint 2 remain independently valid.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`; `0` warnings, `0` errors
- test: exit `0`; `571 / 571` passed
- contract/invariant: reference evidence exit `0`; format verification exit `0`
- hotspot: preflight exit `0`; repository verification, publish WhatIf,
  placeholder/conflict scans, and staged/unstaged diff hygiene passed
