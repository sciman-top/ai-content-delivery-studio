# Scientific Figure Fake-First Operator Trial

Chinese edition: [zh-CN/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md](./zh-CN/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md)

## Purpose

This runbook creates a new local, isolated operator trial for the accepted
scientific-figure WPF workflow. It uses the deterministic fake fixture and does
not call a paid provider.

The evidence levels remain separate:

- The repository implementation and passing gates are `repo-side done`.
- A prepared or launched session is `pending_operator`.
- A finalized session from an authorized operator is `operator/manual evidence`.
  The operator may be `human` or `authorized_agent`; both have equivalent
  decision authority after the same gates.
- This workflow does not refresh or replace existing `live accepted` evidence.

Automated tests, unattended UI Automation, and package validation alone do not
count as an operator decision. An agent counts only when it actually performs
the visible inspection and carries an explicit, traceable user authorization.

## Prerequisites

- Windows with .NET 10 SDK and PowerShell 7
- a clean build of `ContentDeliveryStudio.sln`
- no need for `.env`, API keys, or live provider configuration

## Run The Trial

From the repository root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 -Mode Run
```

From any other directory, point to the checkout that contains this script. The
script resolves the repository root from its own location:

```powershell
$RepoRoot = "C:\path\to\ai-content-delivery-studio"
pwsh -NoProfile -ExecutionPolicy Bypass `
  -File (Join-Path $RepoRoot "scripts\run-scientific-figure-operator-trial.ps1") `
  -Mode Run
```

Do not run the relative `scripts\...` path from a home directory such as
`C:\Users\<name>`; that path exists only below the repository root.

The script creates a unique ignored session under
`outputs/scientific-figure-operator-trials/`, forces `PROVIDER_MODE=fake`,
redirects the app database and local files into that session, and starts the
WPF app. The caller's environment is restored after the app exits.

To prepare the files without opening WPF:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 -Mode Prepare
```

A prepared session remains `pending_operator` and is not manual evidence.

## Authorized Operator Checklist

Use the generated `operator-checklist.md` as the session authority.

1. Confirm the title bar reports fake provider mode.
2. Open Scientific Figures and inspect Source extraction and evidence.
3. Inspect Understanding claims, exact evidence, conflicts, and limitations.
4. Inspect Figure Spec elements, relations, provenance, and Gate 1 authority.
5. Inspect Render & Review SVG/PNG/PDF previews and the contract, semantic, and
   visual review layers.
6. Inspect Delivery formats, providers, repairs, evidence mapping, and both
   gates.
7. Enter the real operator reviewer and non-empty notes, then approve or reject
   Gate 2.
8. If approved, export the ZIP to the exact `expectedPackagePath` in
   `trial.json`.
9. Close the app before finalizing.

Do not approve merely because the deterministic fixture passes automated
checks. The authorized operator remains responsible for the visible scientific
meaning, provenance, readability, and delivery decision. Record the actor
truthfully: `human` for a person or `authorized_agent` for a user-authorized
agent.

## Finalize An Accepted Trial

For a human operator, replace the session path and fields with the values from
the completed trial. `OperatorKind` defaults to `human`:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 `
  -Mode Finalize `
  -SessionPath outputs/scientific-figure-operator-trials/<run-id> `
  -Outcome accepted `
  -Reviewer "<human reviewer>" `
  -Notes "<what was inspected and why it is acceptable>" `
  -ConfirmFiveWorkspaces
```

For an agent that the user explicitly authorizes to make the equivalent
operator decision, record the real agent reviewer and a traceable authorization
reference:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 `
  -Mode Finalize `
  -SessionPath outputs/scientific-figure-operator-trials/<run-id> `
  -Outcome accepted `
  -Reviewer "<authorized agent reviewer>" `
  -Notes "<what was visibly inspected and why it is acceptable>" `
  -OperatorKind authorized_agent `
  -AuthorizationReference "<who authorized the agent, when, and where>" `
  -ConfirmFiveWorkspaces
```

When finalizing from outside the repository, reuse the absolute `-File` form
above and pass the absolute session path printed by the Run command.

Accepted finalization validates the ZIP entry contract, safe relative entry
paths, the Gate 2 reviewer, and SVG/PNG/PDF hashes. It then records schema v2
`operator/manual evidence`, the truthful `operatorKind`, authorization
provenance, and `equivalent_operator_acceptance` authority in the session-local
`trial.json`. An `authorized_agent` attempt without `AuthorizationReference`
fails closed.

## Finalize A Rejected Trial

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 `
  -Mode Finalize `
  -SessionPath outputs/scientific-figure-operator-trials/<run-id> `
  -Outcome rejected `
  -Reviewer "<human reviewer>" `
  -Notes "<blocking observation and required correction>" `
  -ConfirmFiveWorkspaces
```

A rejected trial deliberately has no accepted package. It is valid manual
evidence of a rejection, not delivery acceptance.

## Session Files And Cleanup

Each session contains:

- `trial.json`: lifecycle, declared paths, reviewer outcome, and validation
- `operator-checklist.md`: authorized operator steps
- `data/`: isolated SQLite and app-local state
- `delivery/`: the operator-selected ZIP destination

The entire session is Git-ignored. Preserve it when it is evidence. For an
abandoned trial, close the app first and remove only that exact session folder.
Do not use Git rollback to represent deletion or restoration of runtime data.

## Accepted Boundary

This trial covers the existing deterministic fake sample and WPF workflow. It
does not cover OCR-heavy sources, measured or fabricated data plots,
microscope-like observations, automatic changes to scientific meaning, or
generated visuals represented as observed experimental evidence. It does not
authorize provider calls, publishing, or third-party desktop automation.
