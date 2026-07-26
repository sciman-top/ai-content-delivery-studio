# Scientific Gate 2 And Delivery Task 20 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 20 and Checkpoint 3 of the trustworthy
scientific-figure workflow. It adds explicit Gate 2 decision handling and an
evidence-backed ZIP delivery package.

It does not expose WPF UI, run corpus/live acceptance, call a paid provider, or
claim the remaining Tasks 21-30 are complete.

## Gate 2 Predicate

Gate 2 approval requires all of the following:

- current human Gate 1 approval for the exact specification version
- zero deterministic contract hard failures
- zero semantic or visual machine-review blockers, including uncertainty
- zero unresolved specification issues or repair records
- exactly one valid semantic and one valid visual provider metadata record
- SVG bytes and identity matching the specification and export source hash
- recomputed PNG/PDF content, source-SVG, and semantic hash bindings
- a final affirmative human decision after Gate 1

Human rejection returns a human-required Figure Spec repair plan and never
creates Gate 2 approval or package bytes.

## Delivery Contents

The stable ZIP writer includes:

- `figure.svg`, `figure.png`, and `figure.pdf`
- `specification.json`
- `claim-evidence-item-map.json`
- `reviews.json`
- `repairs.json`
- `providers.json`
- `approvals.json`
- `manifest.json` with SVG, semantic, PNG, and PDF hashes

ZIP entry timestamps are fixed for reproducible metadata. The package contains
both Gate 1 and Gate 2 reviewers and the provenance row for every included
element and relation.

## Focused Verification

- Gate 2 and package tests: `8 / 8` passed
- Checkpoint 3 approved fake chain: `1 / 1` passed
- Blockers cover contract drift, uncertainty, unresolved repair, invalidated
  specification, tampered export bytes, and invalid provider metadata.
- The checkpoint composes deterministic contract review, full-resolution prep,
  separate fake semantic/visual providers, presentation repair, both human
  decisions, and final ZIP delivery while preserving specification identity.

## Compatibility And N/A

- Paid/live provider: `gate_na`; Checkpoint 3 uses deterministic fakes.
- Dependency/supply chain: `gate_na`; ZIP uses the standard library.
- Persistence/schema: `gate_na`; Task 20 returns package bytes without changing
  the persisted workflow schema.
- WPF acceptance: deliberately open; user visibility remains false.

## Rollback

Revert the Task 20 commit to remove Gate 2 delivery service, package writer,
tests, checkpoint status, terminology, and evidence. Tasks 6-19 remain valid,
and no final scientific delivery can be approved.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

The fixed-order closeout run passed on 2026-07-26:

- build: exit `0`, `0` warnings, `0` errors
- test: exit `0`, `608 / 608` passed
- contract/invariant: reference evidence and format checks passed
- hotspot: release preflight, nested repository verification, publish WhatIf,
  placeholder/conflict scans, and diff hygiene passed

Checkpoint 3 is therefore closed. This does not close WPF, corpus, live
provider, or final human acceptance in Tasks 21-30.
