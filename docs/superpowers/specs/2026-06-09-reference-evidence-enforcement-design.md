# Reference Evidence Enforcement Design

## Purpose

Strengthen the repository's current reference-discipline from documentation-only guidance into a lightweight but executable local gate.

The goal is not to force every tiny UI or wording change to update research notes. The goal is to fail closed when high-drift engineering areas change without any visible reference or evidence trail.

## Problem

The repository already has:

- a local reference shelf under `D:\CODE\external\ai-content-delivery-studio-references`
- `docs/research/REFERENCE_RESEARCH.md`
- `docs/EXTERNAL_REFERENCE_STRATEGY.md`
- ADRs, specs, and plans

But these are still soft constraints. Nothing currently forces an engineer to consult or update reference evidence when changing:

- OpenAI provider behavior
- host, resilience, or observability plumbing
- persistence and schema boundaries
- operator or local tool adapter behavior

## Design Goals

1. Keep the enforcement local and lightweight.
2. Target only high-drift engineering areas.
3. Require visible in-repo evidence updates, not vague claims.
4. Avoid widening scope into a full external-source governance platform.

## Non-Goals

- Enforcing exact external URLs for every commit.
- Blocking docs-only or trivial wording changes.
- Replacing human judgment with a giant compliance matrix.
- Requiring live web access during normal local verification.

## Change Areas That Need Stronger Enforcement

The first enforced areas are:

- `openai-provider`
- `host-and-observability`
- `persistence-and-schema`
- `tooling-and-operator`

These are the areas most likely to drift from official semantics or from the intended architecture if reference discipline becomes optional.

## Enforcement Model

Add a repository policy document plus a local verification script.

### Policy Document

Create a `docs/REFERENCE_EVIDENCE_POLICY.md` entrypoint that defines:

- when reference evidence is required
- which in-repo evidence files count
- which local external references should be consulted first
- how to read the rule without over-applying it

### Local Verification Script

Create `scripts/verify-reference-evidence.ps1`.

The script should:

- inspect changed paths from the worktree by default
- optionally accept explicit paths
- detect whether any changed file falls into an enforced engineering area
- require at least one matching evidence-file change for each touched area
- print a clear failure message with:
  - the touched area
  - the triggering source files
  - the acceptable evidence files
  - the recommended local reference shelf directories

The script should pass immediately when:

- no enforced engineering area is touched
- or the required evidence updates are present

## Evidence File Types That Count

For the first slice, acceptable evidence updates are:

- `docs/research/REFERENCE_RESEARCH.md`
- area-specific policy docs such as provider or operator policy
- architecture docs when the change affects durable boundaries
- a new or updated spec under `docs/superpowers/specs/`
- a new or updated implementation plan under `docs/superpowers/plans/`
- release evidence updates when the change affects launch-proof claims

## Repo Integration

Update:

- `README.md`
- `AGENTS.md`
- `docs/DOCUMENTATION_GOVERNANCE.md`
- `docs/TASKS.md`

to point engineers to the new reference-evidence policy and verification script.

## Acceptance Criteria

- The repository contains a documented `change area -> evidence file -> local reference shelf` mapping.
- A local script can fail when a provider/host/persistence/tooling change lands without evidence updates.
- The gate does not block docs-only edits or unrelated small changes.
- The rule is discoverable from both `README.md` and `AGENTS.md`.

## Rollback

If this gate proves too noisy, revert the script and policy doc together, then reintroduce a narrower area set instead of weakening the whole idea silently.
