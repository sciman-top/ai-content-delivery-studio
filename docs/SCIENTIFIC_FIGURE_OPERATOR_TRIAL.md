# Scientific Figure Fake-First Operator Trial

Chinese edition: [zh-CN/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md](./zh-CN/SCIENTIFIC_FIGURE_OPERATOR_TRIAL.md)

## Purpose

This runbook creates a new local, isolated operator trial for the accepted
scientific-figure WPF workflow. It uses the deterministic fake fixture and does
not call a paid provider.

The evidence levels remain separate:

- The repository implementation and passing gates are `repo-side done`.
- A prepared or launched session is `pending_operator`.
- A human-finalized session is `operator/manual evidence`.
- This workflow does not refresh or replace existing `live accepted` evidence.

Automated tests, UI Automation, and package validation do not count as a human
operator trial.

## Prerequisites

- Windows with .NET 10 SDK and PowerShell 7
- a clean build of `ContentDeliveryStudio.sln`
- no need for `.env`, API keys, or live provider configuration

## Run The Trial

From the repository root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 -Mode Run
```

The script creates a unique ignored session under
`outputs/scientific-figure-operator-trials/`, forces `PROVIDER_MODE=fake`,
redirects the app database and local files into that session, and starts the
WPF app. The caller's environment is restored after the app exits.

To prepare the files without opening WPF:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 -Mode Prepare
```

A prepared session remains `pending_operator` and is not manual evidence.

## Human Checklist

Use the generated `operator-checklist.md` as the session authority.

1. Confirm the title bar reports fake provider mode.
2. Open Scientific Figures and inspect Source extraction and evidence.
3. Inspect Understanding claims, exact evidence, conflicts, and limitations.
4. Inspect Figure Spec elements, relations, provenance, and Gate 1 authority.
5. Inspect Render & Review SVG/PNG/PDF previews and the contract, semantic, and
   visual review layers.
6. Inspect Delivery formats, providers, repairs, evidence mapping, and both
   gates.
7. Enter the real reviewer and non-empty notes, then approve or reject Gate 2.
8. If approved, export the ZIP to the exact `expectedPackagePath` in
   `trial.json`.
9. Close the app before finalizing.

Do not approve merely because the deterministic fixture passes automated
checks. The reviewer remains responsible for the visible scientific meaning,
provenance, readability, and delivery decision.

## Finalize An Accepted Trial

Replace the session path and human fields with the values from the completed
trial:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/run-scientific-figure-operator-trial.ps1 `
  -Mode Finalize `
  -SessionPath outputs/scientific-figure-operator-trials/<run-id> `
  -Outcome accepted `
  -Reviewer "<human reviewer>" `
  -Notes "<what was inspected and why it is acceptable>" `
  -ConfirmFiveWorkspaces
```

Accepted finalization validates the ZIP entry contract, safe relative entry
paths, the Gate 2 reviewer, and SVG/PNG/PDF hashes. It then records
`operator/manual evidence` in the session-local `trial.json`.

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
- `operator-checklist.md`: human operation steps
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
