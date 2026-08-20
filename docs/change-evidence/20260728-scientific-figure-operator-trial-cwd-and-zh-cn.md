# Scientific Figure Operator Trial CWD And Chinese Guidance Evidence

Date: 2026-07-28

Chinese edition:
[zh-CN/change-evidence/20260728-scientific-figure-operator-trial-cwd-and-zh-cn.md](../zh-CN/change-evidence/20260728-scientific-figure-operator-trial-cwd-and-zh-cn.md)

## Status

`repo-side fix complete / human operator trial pending`

## Problem And Root Cause

The documented relative command works only when PowerShell is already at the
repository root. A user ran it from `C:\Users\sciman`, so PowerShell correctly
reported that `scripts/run-scientific-figure-operator-trial.ps1` did not exist
under the current directory.

Using an absolute script path alone was also insufficient before this fix:
`Resolve-RepositoryRoot` called `git rev-parse --show-toplevel` against the
caller's current directory. A script launched from outside a Git checkout
therefore failed after PowerShell found the file.

## Fix

- Resolve the Git repository with `git -C $PSScriptRoot`, binding repository
  discovery to the checked-in script rather than the caller's prompt.
- Add a regression test that launches the script with an external temporary
  working directory and verifies that the session is still created under the
  real repository root.
- Add copy-ready absolute-path examples to both English and Chinese runbooks.
- Add the operator-trial runbook to the Chinese documentation hub and bilingual
  governance surface.
- Add a Chinese companion for the agent-operated native WPF evidence without
  changing the canonical evidence claims.
- Align the finalization validator and its fixture with the package emitted by
  the real WPF delivery workspace. Gate 2 is represented by an approval record,
  not a synthetic `Approved` field, and manifest hashes use a `sha256:` prefix.

## Focused Evidence

The new regression test failed before the script change with exit code `1` and
passed after the change as `1 / 1`.

An additional probe was run with working directory `C:\Users\sciman` and the
absolute script path. It exited `0` and created:

`outputs/operator-trials/scientific-figures/external-cwd-probe-20260728-01`

The probe remained `pending_operator`; it used `Mode Prepare`, did not launch
WPF, did not call a provider, and did not create human evidence.

A later user-authorized agent-operated WPF probe exported a package with the
real application schema. That read-only inspection exposed the validator/fixture
drift above. The accepted-finalization test failed with exit code `1` after the
fixture was aligned, then passed after the validator fix; all operator-trial
script tests passed `7 / 7`. The probe remains `awaiting_finalize` with
`evidenceLevel=pending_operator` and is not human evidence.

## Truth Boundary

- Repo-side command usability: fixed for repository-root and external working
  directories.
- Chinese operator guidance: available and linked from the Chinese hub.
- Human `operator/manual evidence`: still pending.
- Existing `live accepted` evidence: unchanged; provider behavior did not
  change and no live refresh was run.
- Generated probe sessions stay under ignored `outputs/` and are not committed.

## Rollback

Revert this bounded script, test, documentation, and evidence slice. Existing
scientific domain behavior, accepted artifacts, provider contracts, schema,
and live evidence remain unchanged. Ignored probe sessions must be retained or
removed separately from Git rollback.

## Verification

The fixed-order closeout passed on 2026-07-28:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `673 / 673` passed
3. contract/invariant: reference evidence and format passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed; the nested suite also
   passed `673 / 673`

After recording these results, the same fixed order is rerun against the final
tree. Git is also checked to confirm that no `.env`, accepted artifact, SQLite,
workspace, output session, or ZIP file enters the write set.
