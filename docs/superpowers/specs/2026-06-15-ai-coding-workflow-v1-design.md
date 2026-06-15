# AI Coding Workflow v1

## Goal

Record a repository-owned default AI coding workflow that fits this project's current engineering posture: local Windows-first workbench, fake-first verification, evidence-heavy closeout, and high-trust delivery.

## Context

The repository already uses:

- `docs/superpowers/specs/`
- `docs/superpowers/plans/`
- `TASKS.md`
- `ROADMAP.md`
- `REFERENCE_BASIS.md`
- `V1_LAUNCH_EVIDENCE.md`

These surfaces already form a repo-owned planning and evidence chain. Adding an external workflow system as a second persistent truth source would increase drift instead of reducing it.

The repository also benefits from some selective external habits:

- stronger acceptance criteria
- clearer public contracts
- bounded use of subagents
- isolated worktrees when risk justifies them
- autonomous execution only inside explicit safety layers

## Decision

Adopt the following repository workflow as the default:

- repo-owned spec and plan surfaces
- Superpowers as the primary implementation skeleton
- contract-first design style for public types and behavior seams
- conditional `subagent` and `worktree` use instead of default parallelism
- layered auto-execution instead of blanket full automation

## Scope

This workflow governs how non-trivial engineering work should be specified, planned, executed, verified, and closed out.

It applies to:

- code changes
- multi-file documentation changes
- reference-governance changes
- provider, host, persistence, tooling, and operator slices

It does not redefine the product scope or release boundary.

## Inputs And Outputs

Inputs:

- repository truth surfaces
- slice intent from a task, issue, or user request
- local reference shelf when required by `REFERENCE_BASIS`

Outputs:

- repo-owned spec for non-trivial slices
- repo-owned implementation plan
- synchronized code and docs changes
- fresh verification evidence

## Failure Modes

This workflow is designed to avoid:

- dual truth surfaces between external spec systems and repo docs
- large implicit implementation bursts without a checked plan
- unnecessary subagent fragmentation for tightly coupled work
- unnecessary worktree sprawl
- claiming completion without fresh verification

## Acceptance Criteria

- The repository has one durable workflow reference doc.
- `AGENTS.md`, `README.md`, `DOCUMENTATION_GOVERNANCE.md`, `ROADMAP.md`, and `TASKS.md` all point to the same workflow posture.
- The workflow keeps `docs/superpowers/specs/` and `docs/superpowers/plans/` as the authoritative long-lived engineering spec/plan surfaces.
- The workflow explicitly rejects `speckit` as a second repository system of record while still allowing concise spec-writing hygiene.
- The workflow explicitly defines when subagents, worktrees, and auto-execution are appropriate.

## Non-Goals

- Replacing `PRD_V1.md`, `ROADMAP.md`, or `TASKS.md`
- Reopening V1 launch claims
- Forcing worktrees or subagents on every slice
- Introducing new product architecture or new provider/runtime behavior
