# Scientific Figure Graphical-Abstract Candidate Evidence

Date: 2026-07-26

## Scope And Boundary

This slice prepares four Task 5 graphical-abstract candidates and draft gold
baselines. It does not admit them: every item is `candidate`, every human
review is `draft`, and the 12-item corpus remains `building`.

The candidates cover projectile components, Doppler relative motion,
photoelectric threshold, and controlled fission-chain feedback. Each baseline
states a central message and abstraction boundary, evidence-links all
scientific elements and relations, permits explicitly named
`non-evidentiary-asset` variation, and blocks:

- omitted scientific limitations
- invented visual claims
- decorative assets that imply evidence

The first-party basis is
`docs/research/SCIENTIFIC_FIGURE_GRAPHICAL_ABSTRACT_CORPUS_CANDIDATES.md`.
All sources use OpenStax *Physics*, `CC-BY-4.0`, the reviewed PDF hash
`a3f75487411ef13d0270c65fc801ceff2b28e6b339afed9b407fe477f7e8453e`,
and extracted-text hash
`331a75a0d1b4238943d24bfb8bef478216979abe29794e20f0ed5ca4f1a62bf4`.
The PDF/TXT remain under the ignored `.cache` boundary and are not tracked.

## Verification

The focused contract began at expected `4`, actual `0`. With all four
candidates present it passed `16 / 16`. The graphical-specific assertions
require the asset boundary and all three mutation classes for every item.

Independent Draft 2020-12 and semantic validation checked all 12 corpus items,
baseline item/hash identity, anchor references, and relation endpoints. Seven
Task 5 `verbatim-text` anchors matched the hash-bound extracted text. This
found and corrected one source-boundary transcription: the Doppler summary
now retains the original `In general, then,` prefix.

Human review remains a real open gate. No reviewer or date is fabricated.
Runtime dependency, persistence migration, and paid-provider gates are
`gate_na` because this slice adds evaluation data only; those gates recover
when their respective implementation work begins.

Rollback reverts only the Task 5 candidate commit. Git rollback does not
remove or restore ignored cache artifacts.

## Repository Gates

Final closeout uses:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `scripts/preflight-release.ps1 -NoRestore`

Fresh outcomes on this slice:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `477 / 477` passed
- reference evidence and format verification: exit `0`
- release preflight: exit `0`, including canonical repository verification,
  publish WhatIf, placeholder/conflict scans, and diff hygiene
