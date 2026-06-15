# AI Coding Workflow

## Purpose

This document defines the default AI-assisted engineering workflow for this repository.

The goal is to keep implementation work:

- repo-owned rather than tool-owned
- spec-first rather than prompt-first
- evidence-backed rather than assumption-backed
- small-slice and verifiable rather than broad and implicit

This workflow is for repository implementation discipline. It is not a product-scope document and it is not a release-readiness ledger.

## Core Decision

AI 推荐: use `repo-owned spec/plan/evidence + Superpowers as the main workflow skeleton + contract-first coding style + conditional subagent/worktree usage + layered auto-execution`.

Do not use an external spec system such as `speckit` as a second long-lived source of truth for this repository.

External workflow tools may still influence writing style, acceptance discipline, or local ergonomics. They must not replace the repository's own documents.

## Truth Sources

Before non-trivial work starts, read the repository truth surfaces in this order:

1. [README.md](../README.md)
2. [PRD_V1.md](./PRD_V1.md)
3. [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md)
4. [ROADMAP.md](./ROADMAP.md)
5. [TASKS.md](./TASKS.md)
6. [REFERENCE_BASIS.md](./REFERENCE_BASIS.md)

If the slice touches provider, host, persistence, tooling, or operator boundaries, consult the mapped local reference shelf first.

## Default Workflow

### Phase 0: Read The Current Truth

- Read the core repository truth surfaces listed above.
- Resolve whether the work is:
  - `XS-S`: one or two files, low-risk, no new long-lived contract
  - `M`: one vertical slice, usually three to five files
  - `L+`: more than one subsystem or more than five files

### Phase 1: Define The Landing Point

For non-trivial changes, create or update a repo-owned spec under `docs/superpowers/specs/`.

The spec must answer:

- goal
- boundary
- inputs and outputs
- failure modes
- acceptance criteria
- explicit non-goals

`speckit`-style hygiene is welcome:

- short spec
- strong acceptance criteria
- low ambiguity

Do not introduce a parallel external spec system as a second truth surface.

### Phase 2: Write The Implementation Plan

Write or update a repo-owned plan under `docs/superpowers/plans/`.

One plan should cover one vertical slice and normally contain `3-5` tasks.

Each task should state:

- `Why now`
- `Inputs`
- `Outputs`
- `Acceptance`
- `Verification`
- `Evidence`
- `Not in scope`

### Phase 3: Execute

Default execution mode is one agent completing one bounded slice end-to-end.

Use `Matt Pocock`-style contract-first thinking where it helps:

- define or clarify public types first
- prefer additive changes
- avoid leaking implementation details
- keep tests centered on contracts and behavior

### Phase 4: Verify

Default verification command:

```powershell
.\scripts\verify-repo.ps1
```

Stronger closeout gate:

```powershell
.\scripts\preflight-release.ps1
```

High-requirement slices must also synchronize:

- docs
- tasks
- evidence
- reference governance

### Phase 5: Close Out

When a slice is complete, synchronize:

- [ROADMAP.md](./ROADMAP.md)
- [TASKS.md](./TASKS.md)
- the spec/plan pair
- any needed evidence or ADR surface

Final reporting should distinguish:

- repository-side completion
- current slice completion
- remaining longer-term work

## Execution Rules

### Superpowers

Superpowers remains the main workflow skeleton for this repository.

Non-trivial changes should leave:

- a repo-owned spec
- a repo-owned plan
- fresh verification evidence

### speckit

Do not use `speckit` as a repository system of record.

You may borrow its writing discipline:

- concise specification
- explicit acceptance criteria
- lower ambiguity

### Matt Pocock

Use this style mainly for public contracts in:

- `src/ImageSeriesStudio.Core`
- `src/ImageSeriesStudio.Application`
- pack metadata
- planning, review, and repair contracts

It is a design style, not the repository's primary workflow system.

### Subagents

Default: do not use subagents.

Use `1-2` subagents only when the work can be cleanly split into independent branches such as:

- document synchronization
- focused test augmentation
- an isolated module seam

Do not split one strongly coupled domain or schema or UI slice across many subagents.

### Worktrees

Default: do not create a worktree for ordinary multi-file work.

Use a worktree when isolation meaningfully lowers risk, especially for:

- large rename slices
- high-risk refactors
- long-running experiments
- clearly independent parallel branches

Do not use worktrees for routine documentation tweaks or one bounded vertical slice.

### Auto-Execution

Use layered automation instead of one global "full auto" mode:

- `L1 automatic`
  - documentation sync
  - conservative local edits
  - test and evidence refresh
- `L2 conditional automatic`
  - pack schema changes
  - review and routing behavior changes
  - provider contract updates
  - only after spec and plan exist
- `L3 not automatic by default`
  - wide rename programs
  - broad schema migrations
  - multi-worktree integration closeout
  - external side effects

## Task Sizing

### `XS-S`

- one or two files
- low-risk
- usually no new spec file required
- update existing plan or task surface when needed

### `M`

- one complete vertical slice
- commonly three to five touched files
- requires a repo-owned plan

### `L+`

- more than five files
- or touches more than one subsystem
- must be split before implementation

Do not treat `schema + planner + review + UI + docs + evidence` as one execution task.

## Acceptance Criteria For A Slice

Every slice must satisfy:

- no competing truth source is introduced
- code, tests, docs, and tasks stay synchronized
- fresh repository verification passes
- reusable evidence remains in-repo

High-requirement slices must also satisfy:

- source and evidence are not replaced by prompt-only behavior
- review and repair routing is explicit
- deterministic-output requirements are visible
- human approval boundaries are preserved

## Non-Goals

This workflow does not:

- replace product documents such as `PRD_V1.md`
- replace release truth in `V1_LAUNCH_EVIDENCE.md`
- require subagents for ordinary slices
- require worktrees for ordinary slices
- require external spec tooling

## Relationship To Other Documents

- [DOCUMENTATION_GOVERNANCE.md](./DOCUMENTATION_GOVERNANCE.md) explains which document answers which question.
- [TASKS.md](./TASKS.md) tracks what still needs to happen.
- [ROADMAP.md](./ROADMAP.md) keeps sequencing and boundary posture visible.
- `docs/superpowers/specs/` and `docs/superpowers/plans/` are the long-lived engineering spec and plan surfaces for this repository.
