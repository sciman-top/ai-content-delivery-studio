# Scientific Figure Fake-First Operator Trial

## Goal

Add a reusable, repository-owned operator-trial kit for the accepted scientific
figure workflow. The kit must let a human run the existing five-workspace WPF
path against the deterministic fake fixture, export a delivery package, and
record a new manual outcome without calling a paid provider or changing any
previously accepted evidence.

## Context And Truth Boundary

Tasks 1-30 and Checkpoints 0-5 of the trustworthy scientific-figure plan are
accepted. Existing tests and native probes prove the WPF controls and state
transitions, but automated UI evidence is not a substitute for a human operator
trial.

This slice adds the missing handoff layer. Completing its code and repository
gates counts as `repo-side done`. Preparing or launching a trial counts as
`pending_operator`. Only an explicit human outcome produced after the five
workspaces have been inspected counts as new `operator/manual evidence`. It does
not change the existing `live accepted` ledger.

## Considered Approaches

1. Add only a runbook. This is low risk, but it does not isolate runtime data or
   validate the exported package.
2. Add a fake-first trial harness plus runbook and package validation. This is
   the selected approach because it is repeatable, additive, and leaves the
   scientific judgments with the human operator.
3. Add broader UI automation that clicks and approves Gate 2. This would repeat
   existing UIA coverage and could falsely imply manual acceptance, so it is out
   of scope.

## Architecture

The slice stays outside the accepted scientific domain and provider contracts.
It adds a PowerShell orchestration boundary around the current desktop app and
one opt-in local-data-root override used only by the child process.

The trial lifecycle is:

1. `Prepare` creates a new ignored session under
   `outputs/scientific-figure-operator-trials/<run-id>`.
2. `Run` sets `PROVIDER_MODE=fake`, redirects local application data into that
   session, writes a `pending_operator` record, and starts the existing WPF app.
3. The human inspects Source, Understanding, Figure Spec, Render & Review, and
   Delivery; enters the Gate 2 reviewer and notes; then approves or rejects.
4. For approval, the human saves the ZIP into the declared session export path.
5. `Finalize` requires explicit human attestation. Accepted outcomes must also
   pass ZIP structure, approval, and manifest hash checks. Rejected outcomes
   require notes and intentionally have no delivery ZIP.
6. Finalization writes a new session-local record without modifying previous
   corpus or live-acceptance artifacts.

## Components

### Isolated Data Root

`LocalStudioDataPaths` will honor
`CONTENT_DELIVERY_STUDIO_DATA_ROOT` when it is non-empty. The value is normalized
to an absolute path. The default LocalAppData and legacy-folder behavior remains
unchanged when the variable is absent.

### Operator Trial Script

`scripts/run-scientific-figure-operator-trial.ps1` will provide explicit
`Prepare`, `Run`, and `Finalize` modes. It will resolve all paths against the
repository or selected session, fail closed on reuse or malformed state, restore
the caller's environment variables, and never read provider credentials.

The session contains:

- `trial.json`: machine-readable lifecycle, declared paths, fake-provider mode,
  operator outcome, and validation summary
- `operator-checklist.md`: the five-stage instructions, expected state, evidence
  type, exclusions, and cleanup guidance
- `data/`: isolated SQLite and application-local files
- `delivery/`: the operator-selected ZIP destination

All session files remain under the ignored `outputs/` tree.

### Delivery Package Validation

Accepted finalization requires exactly one supplied ZIP and validates the
existing package contract:

- required entries exist: SVG, PNG, PDF, specification, provenance map,
  reviews, repairs, providers, approvals, and manifest
- ZIP entry paths are relative and unique
- `approvals.json` contains an approved Gate 2 decision by the declared reviewer
- `manifest.json` hashes match `figure.svg`, `figure.png`, and `figure.pdf`
- no secret-like entry names or absolute local paths are admitted

The validator does not reinterpret scientific correctness or replace either
human gate.

## Error Handling

- A trial never silently overwrites an existing run id or finalized record.
- `Run` refuses non-fake provider mode and scopes environment changes to the
  child process lifetime.
- `Finalize` refuses missing human fields, incomplete five-workspace
  attestation, invalid outcome values, missing approval packages, or packages
  whose reviewer/hash/entry contract does not match.
- Rejected outcomes remain valid manual records but are not delivery acceptance.
- Failure leaves the session record non-accepted and includes a retry-safe error
  summary; it does not mutate prior evidence.

## Acceptance Criteria

- A no-launch preparation path deterministically creates a new
  `pending_operator` session and checklist under `outputs/`.
- The run path forces fake provider mode and an isolated data root before
  starting the existing WPF app.
- Accepted finalization fails closed unless all five workspaces are attested and
  the delivery ZIP passes structure, approval, and hash validation.
- Rejected finalization records a human rejection without manufacturing a
  package or accepted result.
- Script regression tests cover preparation, accepted/rejected finalization,
  malformed packages, mismatched reviewers, and overwrite protection.
- Existing scientific contracts, persistence schema, WPF views, accepted
  artifacts, and live-provider behavior are unchanged.
- Fixed-order verification passes: build, test, contract/invariant, hotspot.

## Non-Goals

- rerunning or refreshing paid-provider evidence
- changing Gate 1, Gate 2, scientific repair, or package domain contracts
- committing generated trial sessions, SQLite files, screenshots, or ZIP files
- claiming an automated run is a human operator acceptance
- covering OCR-heavy sources, measured or fabricated data plots,
  microscope-like observations, automatic scientific-meaning changes, or
  generated visuals represented as observed evidence
- adding general-purpose desktop automation or remote publishing

## Write Set

- `src/ContentDeliveryStudio.Application/Projects/LocalStudioDataPaths.cs`
- `tests/ContentDeliveryStudio.Tests/LocalStudioDataPathsTests.cs`
- `scripts/run-scientific-figure-operator-trial.ps1`
- `tests/ContentDeliveryStudio.Tests/ScientificFigureOperatorTrialScriptTests.cs`
- `docs/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md`
- `docs/zh-CN/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md`
- `docs/USER_GUIDE.md`
- `docs/zh-CN/USER_GUIDE.md`
- `docs/TASKS.md`
- `docs/change-evidence/20260728-scientific-figure-operator-trial.md`
- this spec and its implementation plan

## Rollback

Revert this bounded slice. Existing scientific implementation, accepted corpus,
live evidence, human approvals, WPF workspaces, and delivery packages remain
unchanged. Session-local `outputs/` data can be retained for operator review or
removed separately; Git rollback does not touch it.
