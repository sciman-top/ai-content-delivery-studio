# Scientific Figure Fake-First Operator Trial Implementation Plan

**Goal:** Deliver a reusable fake-first WPF operator-trial kit that creates
isolated sessions, validates exported scientific delivery packages, and records
truthful manual outcomes without changing accepted scientific or live evidence.

**Architecture:** Add one opt-in local-data-root environment boundary and one
PowerShell lifecycle script around the existing deterministic five-workspace
fixture. Keep all generated data under ignored `outputs/`; keep human decisions
explicit and separate from repository automation.

**Tech Stack:** .NET 10, WPF, xUnit, PowerShell 7, ZIP/JSON standard libraries

## Task 1: Add The Isolated Runtime Root

**Acceptance criteria:**

- [ ] `CONTENT_DELIVERY_STUDIO_DATA_ROOT` selects an absolute app data root only
  when non-empty.
- [ ] Existing current/legacy LocalAppData selection is unchanged without the
  variable.
- [ ] Focused path tests cover override normalization and default compatibility.

**Dependencies:** None

**Write set:**

- `src/ContentDeliveryStudio.Application/Projects/LocalStudioDataPaths.cs`
- `tests/ContentDeliveryStudio.Tests/LocalStudioDataPathsTests.cs`

**Verification:**

- `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter LocalStudioDataPathsTests`

## Task 2: Add The Trial Lifecycle And Package Validator

**Acceptance criteria:**

- [ ] `Prepare` creates a unique `pending_operator` session and checklist.
- [ ] `Run` starts the current WPF app with fake mode and the isolated data root,
  restoring the caller environment after exit.
- [ ] `Finalize` supports accepted and rejected human outcomes, requires all five
  stage attestations, and never upgrades an automated result by itself.
- [ ] Accepted finalization validates required ZIP entries, safe unique paths,
  matching reviewer, Gate 2 approval, and SVG/PNG/PDF SHA-256 values.
- [ ] Existing sessions and finalized records are protected from overwrite.

**Dependencies:** Task 1

**Write set:**

- `scripts/run-scientific-figure-operator-trial.ps1`
- `tests/ContentDeliveryStudio.Tests/ScientificFigureOperatorTrialScriptTests.cs`

**Verification:**

- `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter ScientificFigureOperatorTrialScriptTests`
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 -Mode Prepare -NoLaunch`

## Checkpoint: Executable Trial Boundary

- [ ] The isolated-root and script test sets pass.
- [ ] A fresh no-launch session remains `pending_operator`.
- [ ] No provider call, accepted artifact mutation, or non-ignored generated file
  occurs.

## Task 3: Add Operator Guidance And Repository Evidence

**Acceptance criteria:**

- [ ] English and Chinese runbooks describe the exact five-workspace path,
  accepted/rejected finalization, evidence levels, exclusions, and cleanup.
- [ ] Change evidence records repository implementation separately from any
  future manual trial result.
- [ ] Documentation does not reopen Tasks 1-30, refresh live evidence, or claim
  that the harness itself is manual acceptance.

**Dependencies:** Task 2

**Write set:**

- `docs/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md`
- `docs/zh-CN/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md`
- `docs/change-evidence/20260728-scientific-figure-operator-trial.md`
- this implementation plan

**Verification:**

- scan new documentation for `pending_operator`, `operator/manual evidence`,
  `live accepted`, fake-provider, and exclusion boundary language
- `git diff --check`

## Checkpoint: Fixed-Order Repository Closeout

- [ ] Build: `dotnet build ContentDeliveryStudio.sln`
- [ ] Test: `dotnet test ContentDeliveryStudio.sln --no-build`
- [ ] Contract/invariant:
  `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
- [ ] Contract/invariant: `dotnet format --verify-no-changes`
- [ ] Hotspot:
  `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`
- [ ] Confirm `git status` contains no `.env`, accepted artifacts, SQLite,
  workspace, outputs, or generated ZIP files.

## Risks And Mitigations

| Risk | Mitigation |
| --- | --- |
| Trial accidentally enters live mode | `Run` sets `PROVIDER_MODE=fake` in the child environment; no live command is called. |
| Trial mutates normal app state | Dedicated data-root override points the child to the new ignored session. |
| Script claims human acceptance | Default state is `pending_operator`; finalization requires explicit reviewer, notes, stage attestation, and outcome. |
| Malformed or unrelated ZIP is accepted | Required-entry, path, reviewer, Gate 2, and SHA-256 checks fail closed. |
| Existing accepted evidence changes | Generated files stay under `outputs/`; `artifacts/` and existing evidence files are outside the write set. |

## Approval And Execution Note

The formal handoff on 2026-07-28 authorizes autonomous progression when the
slice boundary is clear. This plan implements the recommended fake-first trial
kit; it does not authorize a paid provider run, remote push, or a fabricated
manual acceptance claim.
