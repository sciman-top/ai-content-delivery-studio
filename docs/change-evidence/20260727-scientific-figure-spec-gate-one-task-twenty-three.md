# Scientific Figure Spec And Gate 1 Task 23 Evidence

Date: 2026-07-27

## Scope

This evidence records Task 23 of the trustworthy scientific-figure workflow.
It adds the structured Figure Spec WPF surface, evidence provenance projection,
typed proposal decisions, and explicit Gate 1 controls.

It does not apply accepted proposals to authoritative scientific content,
enable the hidden scientific module, dispatch providers, or change persistence.

## Authority And Behavior

- Every element and relation is projected from the current authoritative
  `ScientificFigureSpec`, including claim, source block, exact quote, page,
  section, or scientific-convention provenance.
- Proposal diffs use typed target and field enums. The view model rejects
  duplicate or empty proposal ids, unknown targets, invalid target/field pairs,
  stale current values, and incomplete proposed values or rationale.
- Pending proposals require a decision. Accepting a proposal blocks Gate 1 for
  the current version until an authoritative spec revision incorporates it;
  rejecting all proposals leaves the unchanged current version eligible.
- Gate 1 eligibility is derived from the domain specification and its blocking
  codes. The approval command uses the domain workflow to freeze the exact
  understanding and specification versions with reviewer, notes, and time.

## WPF Surface

- `ScientificFigureSpecWorkspaceView` uses structured element, relation, and
  proposal lists with stable AutomationIds and no raw JSON editor.
- Gate 1 inputs and controls expose blocking reasons, risk, current versions,
  and frozen versions after approval.
- The view is hosted beside Source and Understanding below the stable five-stage
  header. `ScientificFigureModule.IsUserVisible` remains `false`.

## Focused Verification

- Figure Spec view-model behavior and XAML contract tests: `6 / 6` passed.
- Combined Task 23, Gate 1, layout, and localization tests: `22 / 22` passed.
- Existing Gate 1 persistence tests continue to prove affirmative human
  decisions and exact frozen-version reload behavior.

## Compatibility And N/A

- Paid/live provider: `gate_na`; Task 23 performs no provider dispatch.
- Persistence/schema: `gate_na`; existing Gate 1 persistence contracts are
  reused without schema changes.
- Dependency/supply chain: `gate_na`; no package dependency changed.
- Visible desktop screenshot: deferred to Task 25 because the scientific module
  remains hidden by contract.

## Rollback

Revert the Task 23 commit to remove the Figure Spec view, typed proposal
projection, localized labels, tests, terminology, and evidence. Tasks 6-22 and
the hidden five-stage shell remain intact.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

The fixed-order closeout run passed on 2026-07-27:

- build: exit `0`, `0` warnings, `0` errors
- test: exit `0`, `622 / 622` passed
- contract/invariant: reference evidence and format checks passed
- hotspot: release preflight, nested repository verification, publish WhatIf,
  placeholder/conflict scans, and diff hygiene passed

The first full-suite attempt stopped during `test` because the script-fixture
tests correctly ran the reference-evidence gate before this Task 23 evidence
was present. After synchronizing the plan, task list, terminology, and change
evidence, the unchanged production verification script and its retry tests
passed. Task 23 is closed; Tasks 24-30 and Checkpoints 4-5 remain open.
