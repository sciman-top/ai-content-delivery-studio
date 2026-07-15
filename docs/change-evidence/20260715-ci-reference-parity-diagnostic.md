# CI Reference Parity Diagnostic

## Basis and boundary

- fresh `main` and governance PR both fail `verify-repo` at `sync-reference-governance.ps1 -Check`
- local PowerShell 7.6.3 reports the same files in sync, so a hosted-runner-specific first difference must be captured before choosing a repair
- this independent diagnostic changes only failure detail and evidence; it does not change reference content, gates, product/runtime, dependencies, auth, provider, secret, MCP, process, or hosted UI

## Verification and rollback

- local check must keep passing with exit 0
- GitHub Windows runner must report the exact first difference, code points, and normalized lengths if parity still fails
- rollback: revert only this diagnostic commit after the root cause is repaired or retain it as high-signal error reporting

## Completion boundary

- `diagnostic_published=false`
- `root_cause_repaired=false`
- `default_branch_effective=false`
