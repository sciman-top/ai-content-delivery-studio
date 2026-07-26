# Scientific Figure Spec And Gate 1 Evidence

Date: 2026-07-26

## Scope

This evidence records Task 8 of the trustworthy scientific-figure workflow.
The slice adds:

- versioned `ScientificFigureSpec` records bound to one understanding version
- element and relation contracts with stable identifiers
- exact labels/formulas, render strategies, requirement states, and direction
- validated claim-evidence or explicit `scientific_convention` provenance
- unresolved specification issues and fail-closed blocking codes
- human Gate 1 approval snapshots
- ordered downstream approval snapshots for render plan and scientific review
- scientific-content revision that increments the specification version and
  invalidates Gate 1 plus every downstream approval

It does not create render plans, SVG, review reports, Gate 2 or delivery
approval, persistence, provider calls, or user interface behavior.

## Authority And Gate Rules

- Scientific elements require provenance; only explicitly non-evidentiary
  decorative assets may omit it.
- Every relation requires provenance.
- Evidence provenance must refer to validated, non-contradictory evidence from
  an accepted claim in the selected understanding.
- Scientific conventions use an explicit `scientific_convention:` identifier
  and remain distinct from paper evidence.
- Unknown relation endpoints fail fast.
- A blocked understanding, foreign evidence authority, missing required
  elements, or unresolved conflict/uncertainty/coverage/unsupported-content
  issue blocks the specification.
- Gate 1 can approve only `ReadyForGate1` and freezes both understanding and
  specification versions.
- Scientific review cannot be recorded before render-plan approval.
- Revision returns the workflow to `FigureSpecDraft`, increments the spec
  version, and clears Gate 1 and all downstream approvals.

The repository glossary in `CONTEXT.md` records Figure Spec, provenance,
scientific convention, and Gate 1 approval as separate domain terms.

## Test-First Evidence

The first focused run failed at compile time because the Task 8 spec and
workflow records did not exist. One compiler correction made the downstream
stage prerequisite explicitly nullable; a test call then used the canonical
`labelOrFormula` parameter name.

Code review removed a premature `FinalDelivery` approval path because Gate 2 is
outside Task 8, and added regression coverage for out-of-order review, resolved
issues without a resolution, formulas without exact content, and included
relations connected to forbidden elements.

Focused command:

`dotnet test ContentDeliveryStudio.sln --filter "ScientificFigureSpecTests|ScientificFigureWorkflowStateTests" --no-restore`

Focused result before final repository closeout: exit `0`, `19 / 19` passed.

## Compatibility And N/A

- Task 6 extraction and Task 7 understanding records remain unchanged.
- Runtime dependency/supply-chain change: `gate_na`; no package or executable
  was added.
- Persistence migration: `gate_na`; Task 8 adds domain records only. Recovery
  condition: Task 9 must add migration, compatibility, and rollback evidence.
- Paid/live provider: `gate_na`; no provider call is part of this slice.
- UI/manual acceptance: `gate_na`; Task 8 has no user interface. Recovery
  condition: the later WPF Gate 1 workspace must record human acceptance.

## Rollback

Revert the Task 8 commits to remove Figure Spec/workflow records, focused tests,
glossary additions, evidence, and Task 8 status updates. Tasks 6-7 and
Checkpoint 0 remain independent.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `525 / 525` passed, 0 failed, 0 skipped
- reference evidence: exit `0`; `scientific-figure-workflow` detected the
  repository-owned plan evidence
- format verification: exit `0`
- release preflight: exit `0`; canonical repository verification, publish
  WhatIf, placeholder/conflict scans, and diff hygiene passed
