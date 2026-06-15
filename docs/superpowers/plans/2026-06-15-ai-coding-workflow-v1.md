# AI Coding Workflow v1 Implementation Plan

## Overview

Land a repository-owned AI coding workflow reference that keeps Superpowers as the main execution skeleton, preserves repo-owned spec/plan/evidence surfaces, and clarifies when to use subagents, worktrees, and layered auto-execution.

## Architecture Decisions

- Do not add `speckit` as a second persistent repository workflow system.
- Keep `docs/superpowers/specs/` and `docs/superpowers/plans/` as the long-lived engineering spec and plan surfaces.
- Publish one stable workflow reference under `docs/` and point all lightweight entry surfaces to it.

## Task List

### Task 1: Record the workflow as a durable repository document

**Why now:** The repository already has a planning/evidence chain, but it lacks one explicit workflow reference for future AI-driven coding work.

**Inputs:** Current planning surfaces, documentation governance, existing spec/plan directories.

**Outputs:** `docs/AI_CODING_WORKFLOW.md` and a design spec under `docs/superpowers/specs/`.

**Acceptance**

- [x] Add one durable workflow reference under `docs/`.
- [x] Add one design spec under `docs/superpowers/specs/`.
- [x] Keep the workflow aligned with fake-first, evidence-heavy, high-trust delivery posture.

**Verification**

- [x] Review the new workflow doc for scope, execution rules, sizing, and acceptance.
- [x] Placeholder scan remains clean for the new docs.

**Evidence**

- [x] New workflow doc exists.
- [x] New design spec exists.

**Not in scope**

- [x] Product scope changes
- [x] New runtime behavior

### Task 2: Align lightweight repository truth surfaces

**Why now:** The workflow must be discoverable from the entry surfaces that humans and agents already read.

**Inputs:** `AGENTS.md`, `README.md`, `DOCUMENTATION_GOVERNANCE.md`, `ROADMAP.md`, `TASKS.md`.

**Outputs:** synchronized workflow references and concise repository rules.

**Acceptance**

- [x] `AGENTS.md` summarizes the hard workflow rules and points to the durable workflow doc.
- [x] `README.md` exposes the workflow doc in the documentation map and engineering posture.
- [x] `DOCUMENTATION_GOVERNANCE.md` gives the workflow doc an authority role.
- [x] `ROADMAP.md` and `TASKS.md` reflect the workflow without reopening product scope.

**Verification**

- [x] Search results show a consistent workflow doc reference across the touched files.
- [x] No second workflow truth surface is introduced.

**Evidence**

- [x] All touched entry docs reference `docs/AI_CODING_WORKFLOW.md`.

**Not in scope**

- [x] Full roadmap/task-list archival refactor
- [x] Reference shelf manifest changes

### Task 3: Verify and close out

**Why now:** The workflow change should itself follow the repository verification posture it defines.

**Inputs:** modified docs and repository verification scripts.

**Outputs:** fresh verification evidence and a resilient canonical repository gate for transient local build-file locks.

**Acceptance**

- [x] `.\scripts\verify-repo.ps1` passes.
- [x] `.\scripts\preflight-release.ps1` passes.
- [x] Final report distinguishes repository-side completion from broader future work.

**Verification**

- [x] Run the canonical repo gate.
- [x] Run the stronger release-style gate.

**Evidence**

- [x] Fresh gate output in this implementation run.
- [x] `tests/ImageSeriesStudio.Tests/VerifyRepoScriptTests.cs` reproduces a transient `dotnet build` file-lock failure and verifies the retry-hardened `.\scripts\verify-repo.ps1` path.

**Completion note**

The closeout run on `2026-06-15` hit a real transient Windows build-file lock on `ImageSeriesStudio.Infrastructure.dll`. The canonical verification gate now retries only that bounded failure shape before failing closed for all other build errors.

**Not in scope**

- [ ] Commit/merge strategy
- [ ] Broader workflow-policy redesign beyond this repository
