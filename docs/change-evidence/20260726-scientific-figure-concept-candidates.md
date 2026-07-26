# Scientific Figure Concept Candidate Evidence

Date: 2026-07-26

## Scope And Truth Boundary

This evidence records machine-verifiable preparation for Task 4 of the
trustworthy-scientific-figure plan. It adds four concept/comparison candidates,
four draft gold baselines, and a required semantic `relationClass` contract.

This is not Task 4 admission or Checkpoint 0 closeout. Every corpus item remains
`admissionStatus: candidate`, every baseline remains
`humanReview.status: draft`, and the corpus remains
`admissionState: building`. No reviewer identity or approval date is invented.

Risk is low to medium. The slice changes evaluation contracts and draft
scientific assertions, but does not change runtime behavior, dependencies,
persistence, provider configuration, user workspaces, or generated assets.

## Candidates

| Item | Domain | Bounded comparison |
| --- | --- | --- |
| `mechanics-static-vs-kinetic-friction` | Mechanics | No-slip static response versus sliding kinetic friction |
| `thermal-three-mode-heat-transfer-comparison` | Thermal physics | Conduction, convection, and radiation by carrier and medium |
| `optics-specular-vs-diffuse-reflection` | Optics | Smooth ordered reflection versus rough distributed reflection |
| `electromagnetism-series-vs-parallel-resistors` | Electromagnetism | Circuit topology, conserved quantities, and equivalent resistance |

The detailed primary-source basis is recorded in
`docs/research/SCIENTIFIC_FIGURE_CONCEPT_CORPUS_CANDIDATES.md`.

## Contract And Incremental Evidence

Gold relations now require one of:

- `causal`
- `directional`
- `comparative`
- `associative-non-causal`

The four committed mechanism baselines were compatibly backfilled. The
category contract requires all four classes across concept/comparison
baselines, checks every relation field, and still verifies anchor and element
references.

The focused test began with expected `4`, actual `0`. After each candidate it
progressed through actual counts `1`, `2`, and `3`, with the other 14 checks
passing. After the fourth candidate:

- `dotnet test ContentDeliveryStudio.sln --filter ScientificFigureCorpusContractTests --no-restore`
- exit `0`
- `15 / 15` passed

Independent Draft 2020-12 validation checked the corpus manifest and all eight
current baselines. A semantic probe confirmed unique item IDs, resolvable
claim/element/relation anchors, valid relation endpoints, and complete
four-class coverage.

## Source, License, And Anchor Evidence

- Source authority: official OpenStax *Physics* HTML sections and PDF.
- License: `CC-BY-4.0`, redistribution allowed with attribution and change
  notice.
- PDF SHA-256:
  `a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e`.
- Extracted text SHA-256:
  `331a75a0d1b4238943d24bfb8bef478216979abe29794e20f0ed5ca4f1a62bf4`.
- Local cache key:
  `openstax/physics/2026-07-26/Physics-WEB-a3f75487411e.pdf`.
- Both PDF and extracted text remain under the Git-ignored
  `eval/scientific-figures/.cache/` boundary.

All nine `verbatim-text` anchors in the four concept baselines independently
matched normalized whitespace in the hash-matched extracted text. This check
found and corrected one transcription omission: the parallel-resistance quote
now retains the source sentence's leading `Therefore,`. Normalized prose and
equations remain explicitly classified as `normalized-text` or
`normalized-equation`, not exact quotations.

No source image, PDF, extracted text, or other source binary is tracked by Git.
The external disposable research probe is not claimed removed.

## Human Review Gate

A human reviewer must still accept, revise, reject, or replace each candidate
after checking its source context, claims, relations and classes, required
elements, conditions, allowed variations, and scientific/visual mutations.
Passing machine tests cannot authorize `accepted`.

## Compatibility, N/A, And Rollback

- Runtime dependency adoption: `gate_na`; no package or executable was added.
  Recovery condition: runtime adapter work must pass its own supply-chain and
  corpus benchmark gates.
- Persistence/schema migration: `gate_na`; these are evaluation schemas, not
  application persistence. Recovery condition: any later persistence change
  requires migration, compatibility, and rollback evidence.
- Paid provider/live call: `gate_na`; no provider call was made. Recovery
  condition: live-provider work requires explicit user approval.
- Human approval: not N/A and remains open.

Rollback reverts only the Task 4 candidate commit: four manifest records, four
draft baselines, the additive relation-class contract and compatibility
backfill, tests, evidence, and task-status wording. Ignored cache files and the
external research probe are outside Git rollback.

## Repository Gates

The final closeout uses the required fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh results on this slice:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `476 / 476` passed, 0 failed, 0 skipped
- reference evidence: exit `0`
- format verification: exit `0`
- release preflight: exit `0`; reference parity, placeholder/conflict scans,
  canonical repository verification, publish WhatIf, and diff hygiene passed
