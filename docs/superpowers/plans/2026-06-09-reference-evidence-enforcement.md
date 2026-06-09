# Reference Evidence Enforcement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Strengthen reference-discipline for high-drift engineering areas with a local policy document and verification script.

**Architecture:** Add one policy document that explains which change areas require reference evidence, then add one PowerShell gate that inspects changed paths and fails when required evidence files were not updated. Surface the gate from repository entrypoints so it becomes part of normal local verification.

**Tech Stack:** Markdown docs, PowerShell, git worktree status, existing docs/spec/plan structure

---

### Task 1: Add policy and design entrypoints

**Files:**
- Create: `docs/REFERENCE_EVIDENCE_POLICY.md`
- Create: `docs/superpowers/specs/2026-06-09-reference-evidence-enforcement-design.md`

- [ ] Record the enforced change areas, acceptable evidence files, and local reference shelf priorities.
- [ ] Keep the policy narrow: OpenAI provider, host/observability, persistence/schema, tooling/operator.

### Task 2: Add the local verification gate

**Files:**
- Create: `scripts/verify-reference-evidence.ps1`

- [ ] Detect changed files from the current worktree when no explicit path list is provided.
- [ ] Map touched files to enforced areas.
- [ ] Fail when an enforced area is touched without any acceptable evidence-file update.
- [ ] Print actionable guidance listing touched files, acceptable evidence files, and recommended local reference directories.

### Task 3: Integrate the gate into repository entrypoints

**Files:**
- Modify: `README.md`
- Modify: `AGENTS.md`
- Modify: `docs/DOCUMENTATION_GOVERNANCE.md`
- Modify: `docs/TASKS.md`

- [ ] Surface the policy document from top-level docs.
- [ ] Add the verification script to the repo's normal local verification story.
- [ ] Record the new enforcement slice in the task checklist.

### Task 4: Verify locally

**Files:**
- Test: `scripts/verify-reference-evidence.ps1`

- [ ] Run the gate against the current docs-only change set and confirm it passes.
- [ ] Run `git diff --check` on the modified docs and script.
- [ ] Summarize remaining risks, especially that this is still a local gate and not yet CI-enforced.
