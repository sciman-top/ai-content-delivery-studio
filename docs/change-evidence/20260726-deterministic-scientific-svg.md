# Deterministic Scientific SVG Evidence

Date: 2026-07-26

## Scope

This evidence records Task 13 of the trustworthy scientific-figure workflow.
The slice adds:

- an application-owned `IScientificFigureRenderer` contract
- deterministic SVG artifact identity and SHA-256
- a local XML-API-based `DeterministicSvgRenderer`
- stable layer, element, and relation SVG identifiers
- deterministic grid geometry and directed/bidirectional markers
- exact labels, formulas, values, units, legends, and annotations from the
  render plan
- accessibility title/description and embedded plan/spec metadata
- a strict non-authoritative decorative geometry boundary
- fake-first desktop renderer registration
- semantic golden structure tests

It does not export files, rasterize PNG, generate PDF, accept provider assets,
perform visual/scientific review, or record downstream render approval.

## Determinism And Authority

- Repeated rendering of the same immutable plan produces byte-identical SVG
  and the same `sha256:` hash.
- XML is built with `XDocument`; labels and formulas are escaped without
  changing their decoded text.
- Numeric coordinates use invariant culture and fixed precision.
- Every scientific `<g>` and relation `<path>` carries a stable SVG `id`,
  source `data-spec-id`, and provenance kind.
- Visible text comes only from `ExactContent` or an approved relation label.
  Scientific meaning is retained only as element accessibility `<title>`.
- The renderer has no generated-asset input, remote URL, `href`, or prompt
  surface.
- A decorative asset becomes a local `<rect>` inside a
  `data-authoritative="false"` group. It cannot contain text, paths, images,
  arrows, values, or formulas.
- Plan/spec/version identity is embedded in metadata for Task 14 export
  derivation.

## Test-First Evidence

The initial focused run failed because the renderer contract and implementation
did not exist. The implementation then passed byte determinism, SHA-256,
stable element/relation authority, XML escaping, decorative isolation,
accessibility, metadata, and golden semantic-order tests.

Focused command:

`dotnet test ContentDeliveryStudio.sln --filter "DeterministicSvgRendererTests|ScientificSvgGoldenTests" --no-restore`

The pre-commit review then found that a valid plan with multiple scientific
layers duplicated every relation and its SVG ID. A regression test reproduced
the failure (`1` failed, `4` passed); connections are now emitted once in the
first deterministic scientific layer.

Focused result before final repository closeout: exit `0`, `7 / 7` passed.

## Compatibility And N/A

- Task 12 render plans: unchanged and validated before rendering.
- Task 9 persistence: unchanged; SVG artifacts are not persisted in Task 13.
- Runtime dependency/supply-chain change: `gate_na`; .NET XML and cryptography
  APIs are used without a new package.
- Paid/live provider: `gate_na`; renderer is deterministic and local.
- PNG/PDF consistency: `gate_na`; recovery condition is Task 14 derivation
  from the exact approved SVG hash.
- Human visual acceptance: `gate_na`; later review checkpoints must inspect
  rendered artifacts at target resolution.

## Rollback

Revert the Task 13 commit to remove the rendering contract, renderer, tests,
DI registration, glossary/evidence, and Task 13 status updates. Task 12 render
plans remain independently valid.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`; `0` warnings, `0` errors
- test: exit `0`; `554 / 554` passed
- contract/invariant: reference evidence exit `0`; format verification exit `0`
- hotspot: preflight exit `0`; repository verification, publish WhatIf,
  placeholder/conflict scans, and diff hygiene passed
