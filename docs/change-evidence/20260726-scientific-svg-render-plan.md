# Scientific SVG Render Plan Evidence

Date: 2026-07-26

## Scope

This evidence records Task 12 of the trustworthy scientific-figure workflow.
The slice adds:

- immutable `SvgRenderPlan` records
- deterministic canvas, layers, elements, connections, layout constraints,
  style tokens, accessibility metadata, and SVG export settings
- derived label, formula, and legend collections
- stable render IDs bound to Figure Spec element/relation IDs
- a Gate-1-aware `ScientificFigureSpecCompiler`
- a pre-render `SvgRenderPlanValidator`
- desktop registration of the deterministic compiler

It does not render SVG, choose final spatial coordinates, rasterize, export
PNG/PDF, call providers, or record render-plan downstream approval.

## Compiler And Validator Rules

- A missing, stale, or different-version Gate 1 approval blocks compilation.
- Forbidden specification content is excluded.
- Every included critical item carries its approved source specification ID.
- Render IDs derive deterministically from stable specification IDs.
- Scientific and decorative content use separate named layers.
- Exact-content element kinds retain their approved label/formula/unit text.
- Unknown render strategies, missing layers, duplicate IDs, missing connection
  endpoints, unapproved source items, missing exact content, and critical
  content outside scientific layers fail validation before rendering.
- Canvas, spacing, padding, style tokens, accessibility text, and SVG metadata
  export behavior are explicit rather than renderer defaults.

## Test-First Evidence

Focused tests were written against the missing compiler/plan contracts. The
first implementation passed the approval, authority, strategy, endpoint,
unapproved-item, and formula-content cases. Design review then added explicit
layout constraints and immutable style tokens and strengthened duplicate
connection/scientific-layer validation.

Focused command:

`dotnet test ContentDeliveryStudio.sln --filter "ScientificFigureSpecCompilerTests|SvgRenderPlanValidatorTests" --no-restore`

Focused result before final repository closeout: exit `0`, `6 / 6` passed.

## Compatibility And N/A

- Task 8 Figure Spec and Gate 1 records: unchanged.
- Task 9 persistence: unchanged; render plans are not persisted in Task 12.
- Runtime dependency/supply-chain change: `gate_na`; no package was added.
- Paid/live provider: `gate_na`; compilation is deterministic and local.
- Visual/manual acceptance: `gate_na`; Task 12 creates no rendered asset.
  Recovery condition: Task 13 renderer must produce inspected SVG evidence.

## Rollback

Revert the Task 12 commit to remove render-plan records, compiler, validator,
tests, DI registration, glossary/evidence, and Task 12 status updates. Tasks
6-11 and Checkpoint 1 remain independent.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `547 / 547` passed, 0 failed, 0 skipped
- reference evidence: exit `0`; host/observability and
  `scientific-figure-workflow` detected plan evidence
- format verification: exit `0`
- release preflight: exit `0`; canonical repository verification, publish
  WhatIf, placeholder/conflict scans, and diff hygiene passed
