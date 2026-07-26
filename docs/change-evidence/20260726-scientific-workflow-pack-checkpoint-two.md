# Scientific Workflow Pack And Checkpoint 2 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 15 and Checkpoint 2 of the trustworthy
scientific-figure workflow. The slice adds:

- the internal `scientific-figure` workflow pack
- concept-comparison, mechanism/process, and graphical-abstract blueprints
- physics, natural-science, and research domain tags
- SVG, PNG, and PDF renderer outputs
- deterministic-contract, scientific-semantic, and visual-quality rubric IDs
- scientific source, understanding, specification, rendering, review, and
  delivery application-module use cases
- an explicit `ScientificFigureModule.IsUserVisible = false` availability gate
- an approved-fake-specification-to-export Checkpoint 2 integration test

It does not add a WPF view, navigation entry, user command, Gate 2 approval,
live provider execution, or delivery package.

## Pack And Module Contracts

- All five scientific packs use version `1.0.0` and app compatibility
  `1.0.0` through `2.0.0`.
- The workflow uses the existing stable workbench stages: Source, Brief, Plan,
  Produce, Review, Repair, and Deliver.
- Workflow references resolve to exactly one scientific blueprint, domain,
  renderer, and rubric pack.
- Existing 25 starter packs remain unchanged; the registry grows additively to
  30 packs.
- The application module owns repository-relative Core, Application, and
  Infrastructure scientific-figure folders.
- The starter registry is not consumed by the current WPF runtime, and the
  module availability constant remains false.

## Test-First Evidence

The initial focused run failed to compile because the scientific pack IDs and
visibility contract did not exist. After implementation:

- pack/module focused tests: exit `0`; `9 / 9` passed
- Checkpoint 2 combined focused tests: exit `0`; `10 / 10` passed

The Checkpoint 2 test creates a human-approved fake specification containing a
critical scientific element and non-evidentiary decoration, compiles it, renders
editable SVG, and exports PNG/PDF from the exact SVG hash. It verifies stable
scientific IDs, `data-authoritative="false"` decoration, identical source hashes
on both exports, and hidden UI availability.

## Compatibility And N/A

- Existing workflow pack definitions and UI defaults are not modified.
- Pack/schema migration: `gate_na`; the addition uses the existing pack schema.
- Runtime dependency/supply chain: `gate_na`; no package or lockfile changed.
- Paid/live provider: `gate_na`; pack registration and Checkpoint 2 are local.
- WPF acceptance: deliberately open; recovery condition is the later WPF
  acceptance checkpoint and explicit availability change.

## Rollback

Revert the Task 15 commit to remove the five pack definitions, expanded module
catalog, availability constant, Checkpoint 2 test, terminology, status, and
evidence. Tasks 6-14 remain independently valid and the old 25-pack starter
registry is restored.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`; `0` warnings, `0` errors
- test: exit `0`; `562 / 562` passed
- contract/invariant: reference evidence exit `0`; format verification exit `0`
- hotspot: preflight exit `0`; repository verification, publish WhatIf,
  placeholder/conflict scans, and diff hygiene passed
