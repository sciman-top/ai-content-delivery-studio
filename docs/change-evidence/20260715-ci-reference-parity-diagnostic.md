# CI Reference Parity Cross-Time-Zone Repair

## Basis and boundary

- fresh `main` and governance PR both fail `verify-repo` at `sync-reference-governance.ps1 -Check`
- local PowerShell 7.6.3 in UTC+8 reports the same files in sync, while the hosted Windows runner in UTC reports `first_difference=1988`, expected code point `0`, actual code point `1`, with equal normalized lengths
- root cause: `ConvertFrom-Json` materializes ISO `updatedAt` as local `DateTime`; formatting therefore changed `2026-07-06T12:48:35+08:00` to the runner-local `04:48:35`
- repair: restore top-level `updatedAt` from the raw JSON string at both reference-basis and external-shelf manifest read points
- boundary: reference content and gate semantics remain unchanged; no product/runtime, dependency, auth, provider, secret, MCP, process, or hosted UI change

## Verification and rollback

- local `sync-reference-governance.ps1 -Check` and full fixed gates must pass
- a UTC simulation must preserve the exact ISO string instead of converting it to local time
- GitHub Windows runner must pass the reference parity and repository verification checks
- build passed with 0 warnings and 0 errors; 461 tests passed
- reference evidence, `dotnet format`, release preflight, publish `WhatIf`, diff hygiene, and canonical repository verification passed
- five-axis review passed with no Critical or Required issue
- rollback: revert only this independent repair PR; the richer mismatch diagnostic can remain as high-signal error reporting

## Completion boundary

- `diagnostic_published=true`
- `root_cause_repaired=true`
- `locally_verified=true`
- `default_branch_effective=false`
