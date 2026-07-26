# Scientific Understanding Model Evidence

Date: 2026-07-26

## Scope

This evidence records Task 7 of the trustworthy scientific-figure workflow.
The slice adds an immutable, source-bound authority model for:

- normalized scientific terminology
- reviewable claims and claim categories
- verbatim evidence quotations with exact source locations
- support, definition, qualification, and contradiction link roles
- claim conflicts and their resolution state
- required objective coverage
- computed understanding approval readiness and stable blocking codes

It does not implement provider planning, runtime extraction, persistence,
Figure Spec or Gate 1 approval, rendering, review, delivery, or user interface
behavior.

## Authority And Fail-Closed Rules

- Every evidence link records the source asset id, source hash, source block id,
  location, verbatim quotation, role, confidence, and validation state.
- Evidence can be created only from a block belonging to the supplied immutable
  extraction.
- An accepted claim requires validated `Support` or `Definition` evidence.
- `Qualification` and `Contradiction` remain distinct roles and never enter the
  supporting-evidence projection.
- Evidence from a different source asset cannot satisfy the selected
  understanding.
- Draft claims, missing evidence, validated contradictions, unresolved
  conflicts, absent required coverage, incomplete required coverage, and
  required coverage without an accepted claim all block approval readiness.
- `ReadyForApproval` is computed readiness only; it is not human approval or
  Gate 1.

The repository glossary in `CONTEXT.md` records the distinction among claim,
evidence link, supporting evidence, coverage, and approval readiness.

## Test-First Evidence

The first focused run failed at compile time because the Task 7 model did not
exist. After the minimal model was added, a fixture quotation was correctly
rejected because it was not verbatim; the fixture was corrected without
weakening source validation.

Code review then added regression cases for source-asset binding, supported but
unaccepted claims, missing required coverage, mixed-status coverage, and null
collection elements. The focused command is:

`dotnet test ContentDeliveryStudio.sln --filter ScientificUnderstandingTests --no-restore`

Focused result before final repository closeout: exit `0`, `17 / 17` passed.

## Compatibility And N/A

- Existing source ingestion and Task 6 extraction records remain compatible.
- Runtime dependency/supply-chain change: `gate_na`; no package or executable
  was added.
- Persistence migration: `gate_na`; Task 7 adds domain records only. Recovery
  condition: Task 9 must add migration, compatibility, and rollback evidence.
- Paid/live provider: `gate_na`; no provider call is part of this slice.
- Performance: all aggregate inputs are copied into bounded read-only snapshots;
  there is no I/O or unbounded external enumeration.

## Rollback

Revert the two Task 7 commits to remove the understanding model, focused tests,
glossary additions, evidence, and Task 7 status updates. Task 6 and Checkpoint 0
remain independent and must not be reverted with this slice.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `506 / 506` passed, 0 failed, 0 skipped
- reference evidence: exit `0`; `scientific-figure-workflow` detected the
  repository-owned plan evidence
- format verification: exit `0`
- release preflight: exit `0`; canonical repository verification, publish
  WhatIf, placeholder/conflict scans, and diff hygiene passed
