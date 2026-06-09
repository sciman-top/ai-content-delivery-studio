# Operator Risk Policy

## Purpose

This document defines the execution boundary for `OperatorAction` and `OperatorRun` in V1 and near-term hardening slices.

The product intent is to let AI guide safe repeatable actions without turning the app into an unbounded automation agent.

## Core Principles

- Review, Repair, and Operator are distinct stages.
- Operator is for controlled execution, not for hiding risky side effects behind model output.
- Read-only inspection and additive outputs are preferred over destructive mutation.
- Every real action must produce auditable records.
- Risk is assigned per action, not per tool family.

## Risk Levels

### Low

Low-risk actions are safe to auto-run when they stay inside declared project boundaries and produce additive outputs.

Examples:

- Validate a delivery folder, manifest, or artifact package.
- Generate a diagnostics or validation report in a new output folder.
- Run a read-only OpenAI launch preflight that inspects readiness and writes local diagnostics only.
- Build thumbnails, contact sheets, or derived previews without overwriting approved assets.
- Create deterministic text-composition preview assets in a new location.
- Run read-only metadata inspection or checksum generation.

Requirements:

- no third-party account side effects
- no destructive overwrite of approved outputs
- declared input and output paths
- audit record written automatically

### Medium

Medium-risk actions require user approval before execution.

Examples:

- Overwrite a generated project artifact.
- Rebuild an existing delivery package in place.
- Apply a repair action that changes persisted project records.
- Perform extraction or conversion on a sensitive local document into a new stored representation.
- Batch rename or reorganize files inside the project workspace.

Requirements:

- explicit approval at the point of risk
- rollback or cleanup note
- audit record plus operator-visible summary

### High

High-risk actions require explicit confirmation and should normally pause for human control or handoff.

Examples:

- Browser automation or desktop automation that can change third-party system state.
- Uploading, publishing, or submitting artifacts to external services.
- Deleting user files or performing broad recursive mutations.
- Writing outside the declared project or configured export roots.
- Using privileged credentials or modifying provider-secret configuration.

Requirements:

- explicit confirmation in the current task context
- clear impact statement
- rollback plan if feasible
- screenshots, logs, or equivalent evidence when execution proceeds

### Blocked

Some actions are outside the acceptable operator boundary unless the user has directly and specifically requested them in the current task.

Blocked-by-default examples:

- Exfiltrating credentials or private content.
- Modifying system security settings.
- Hidden background automation against third-party accounts.
- Actions whose tool inputs, outputs, or side effects cannot be described in advance.

## First Real Operator Slice For V1

AI 推荐: the first real operator action should be local delivery or artifact validation.

Recommended launch slice:

- Input: staged project export or delivery folder.
- Execution: run allow-listed local validation logic.
- Output: write a validation report and audit record into a new diagnostics or validation subfolder.
- Side effects: additive only.

Why this slice:

- It proves real operator execution without broad destructive risk.
- It exercises audit, inputs, outputs, exit status, and rollback metadata.
- It improves delivery confidence for the primary launch route.

Companion low-risk path already useful in the current repository:

- a read-only `openai-launch-preflight` action that evaluates role-scoped provider readiness and writes local diagnostics before any live V1 sample run is attempted
- the allow-listed `artifact-validation` action registered in the local tool registry, which keeps validation additive, dry-run aware, and confined to diagnostics outputs outside approved delivery assets

## Required Action Metadata

Every operator action definition must declare:

- stable action id
- owning adapter id
- human-readable purpose
- risk level
- dry-run support
- declared inputs
- declared outputs
- side effects summary
- timeout
- approval requirement
- cleanup or rollback notes

## Required Run Audit Fields

Every real operator run must record:

- run id
- action id
- project id
- user or approval source
- start and end timestamps
- dry-run flag
- command, function, or adapter summary with secrets redacted
- resolved input paths
- resolved output paths
- exit code or completion status
- warning or error summary
- rollback or cleanup result when applicable

## Allow-List Policy

- Low-risk real execution must run only through declared adapters and allow-listed commands or functions.
- Browser and desktop automation require their own explicit allow-list definitions.
- Screenshots and external page content are evidence inputs, not permission to act.

## Approval Policy

- Low risk: may auto-run when the action is additive, bounded, and auditable.
- Medium risk: must pause for user approval.
- High risk: must pause for explicit confirmation and may still require handoff.
- Blocked actions: do not run unless the user has directly requested that exact class of action and the product boundary allows it.

## Rollback Expectations

- Low-risk actions may use "no rollback needed" only when they create additive outputs and do not mutate prior artifacts.
- Medium-risk actions must have a cleanup or reversal note.
- High-risk actions need a deliberate rollback or containment plan before execution where feasible.

## Relationship To Product Scope

V1 should not broaden operator scope until:

- the primary launch route is stable
- approval evidence is preserved through delivery export
- the first low-risk adapter is proven end-to-end

Remote publishing, broad browser automation, and Windows UI automation are valuable later, but they are not V1 launch blockers.
