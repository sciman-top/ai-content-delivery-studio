# GitHub Actions Node.js 24 Maintenance Evidence

## Scope And Boundary

- Date: `2026-07-30`.
- Scope: remove the Node.js 20 deprecation warning from the product
  `verify-repo` workflow by upgrading its checkout and .NET setup actions.
- Product/runtime behavior: unchanged. No provider call, external publication,
  package signing, installer registration, schema change, or dependency restore
  policy change was introduced.

## Supply-Chain Decision

| Action | Official v5 commit | GitHub verification | Runtime | Decision |
| --- | --- | --- | --- | --- |
| `actions/checkout` | `fbc6f3992d24b796d5a048ff273f7fcc4a7b6c09` | verified | `node24` | Adopt as an immutable SHA pin. |
| `actions/setup-dotnet` | `26b0ec14cb23fa6904739307f278c14f94c95bf1` | verified | `node24` | Adopt as an immutable SHA pin. |

The official `v5` Git refs resolved directly to those commits on the evidence
date. The action metadata at each exact commit declares `runs.using: node24`.
Existing inputs remain supported: checkout `fetch-depth: 0` and setup-dotnet
`dotnet-version: "10.0.x"`.

## Verification

| Check | Result |
| --- | --- |
| YAML parse and exact-pin assertions | exit `0`; both expected 40-character commit pins present |
| Floating action-tag scan | exit `0`; two references, zero floating v4/v5 tags |
| `git diff --check` | exit `0` |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore` | exit `0`; build has 0 warnings/errors, tests `734 / 734`, reference and format gates pass, and actual 84-file publish/package verification passes |

## Risk, Recovery, And Rollback

- Risk is bounded to GitHub-hosted workflow bootstrap. The product code and
  release script are unchanged, and action permissions remain `contents: read`.
- Rollback reverts only `.github/workflows/verify-repo.yml` and this evidence
  file. Git-external publish artifacts and workspaces are unaffected.
