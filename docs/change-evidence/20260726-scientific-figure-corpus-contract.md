# Scientific Figure Corpus Contract Evidence

Date: 2026-07-26

## Scope

This evidence closes Task 2 of the trustworthy-scientific-figure plan at the
corpus-contract and local-cache boundary. It does not admit a paper, create a
human-approved gold baseline, complete the 12-item corpus, start runtime
implementation, or approve a live provider.

Risk: low to medium. The change adds repository-owned JSON contracts and test
invariants that later corpus work must satisfy. Product runtime behavior,
package references, persistence schemas, provider configuration, user
workspaces, and generated assets are unchanged.

## Contract Decisions

- `corpus.json` is the machine-readable corpus authority. It declares the
  12-item boundary and the required 4+4+4 distribution across mechanism/process,
  concept/comparison, and graphical-abstract sources.
- `building` permits an incomplete 0-12 item manifest. `human-approved` requires
  exactly 12 items, exactly four from each category, and every item to have
  `admissionStatus: accepted`.
- Every admitted item requires source identity, public location, license
  review, a stable SHA-256 content hash, evidence locations, a local-cache key,
  a figure objective, and a gold-baseline path.
- Every gold baseline requires claims, source anchors, semantic elements and
  relations, allowed variation, at least one scientific blocking mutation, at
  least one visual blocking mutation, and explicit human-review state.
- `eval/scientific-figures/.cache/` is the only new ignored path. Paper binaries
  remain local and outside Git; the default contract tests use an inline,
  redistribution-safe fixture rather than an external paper binary.
- The checked-in manifest remains `building` with zero admitted items. This is
  intentional: Tasks 3-5 own source selection and human approval.

## Test-First Evidence

Initial focused command:

```powershell
dotnet test ContentDeliveryStudio.sln --filter ScientificFigureCorpusContractTests
```

Before the schemas and manifest existed, the run discovered eight tests and
failed two file-authority tests because `corpus.schema.json` and `corpus.json`
were absent. After the contracts were added and the approval-state invariant
was tightened, the focused run returned exit `0` with `9 / 9` tests passed.

The tests reject missing source license, source content hash, evidence
locations, gold-baseline anchor location, scientific mutation coverage, visual
mutation coverage, and an incomplete corpus mislabeled as human-approved.

## Schema And Cache Verification

The three JSON files parse through PowerShell `ConvertFrom-Json`.

Python `jsonschema 4.26.0` supplied an independent Draft 2020-12 check:

- both schemas passed `Draft202012Validator.check_schema`;
- the checked-in building manifest validated;
- changing only `admissionState` to `human-approved` produced four validation
  errors for the missing count and category requirements.

An earlier optional `ajv-cli 5.0.0` probe was not accepted as schema evidence:
that CLI exposes Draft 7 and Draft 2019 modes, not Draft 2020-12, and its
unconfigured strict mode does not load URI/date formats. No schema formats were
weakened to accommodate that tool limitation.

```powershell
git check-ignore -v eval/scientific-figures/.cache/probe.pdf
```

returned exit `0` and matched
`.gitignore:8:/eval/scientific-figures/.cache/`. No source binary or generated
cache file is part of this change.

## Compatibility And N/A

- Backward compatibility: preserved; the contracts are new and no existing
  runtime or external contract changed.
- Runtime dependency adoption: `gate_na`. No runtime or test package was added.
  Recovery condition: a later adapter task must pass its own supply-chain and
  corpus benchmark gate before adoption.
- Persistence/schema migration: `gate_na`. These evaluation JSON schemas are
  repository contracts, not application persistence schemas. Recovery
  condition: a later persistence task must provide migration, compatibility,
  and rollback evidence.
- Paid provider/live call: `gate_na`. Contract verification is fully offline.
  Recovery condition: the opt-in live acceptance tasks require explicit user
  approval.
- Human corpus approval: not N/A and not complete. Tasks 3-5 must supply 12
  reviewed baselines before the manifest may become `human-approved`.

## Rollback

Rollback reverts only the Task 2 commit: remove the two schemas, the building
manifest, the contract test, the exact `.cache` ignore rule, this evidence, and
the Task 2 truth updates. Local paper binaries under the ignored cache are
outside Git and must not be deleted or represented as restored by Git rollback.

## Repository Gate Evidence

The final closeout uses the repository fixed order:

1. `dotnet build ContentDeliveryStudio.sln`
   - exit `0`
   - 0 warnings, 0 errors
2. `dotnet test ContentDeliveryStudio.sln --no-build`
   - exit `0`
   - `470 / 470` passed, 0 failed, 0 skipped
3. Contract and invariant:
   - `scripts/verify-reference-evidence.ps1`: exit `0`
   - `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`:
     exit `0`
4. `scripts/preflight-release.ps1 -NoRestore`
   - exit `0`
   - reference parity, placeholder/conflict scans, canonical repository
     verification, publish WhatIf, staged/unstaged diff hygiene passed

Task-specific focused tests, Draft 2020-12 validation,
`git check-ignore -v`, JSON parsing, and `git diff --check` also returned exit
`0`.
