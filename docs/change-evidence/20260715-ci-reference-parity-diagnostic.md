# CI Reference Parity Cross-Time-Zone Repair

## Basis and boundary

- fresh `main` and governance PR both fail `verify-repo` at `sync-reference-governance.ps1 -Check`
- local PowerShell 7.6.3 in UTC+8 reports the same files in sync, while the hosted Windows runner in UTC reports `first_difference=1988`, expected code point `0`, actual code point `1`, with equal normalized lengths
- root cause: `ConvertFrom-Json` materializes ISO `updatedAt` as local `DateTime`; formatting therefore changed `2026-07-06T12:48:35+08:00` to the runner-local `04:48:35`
- repair: restore top-level `updatedAt` from the raw JSON string at both reference-basis and external-shelf manifest read points
- hosted follow-up: the cross-time-zone repair passed its original parity point, then the Windows runner exposed a separate inherited toolchain gap because `rg` was unavailable
- portable scan repair: retain `rg` when installed and otherwise scan only Git-tracked target files with case-sensitive PowerShell `Select-String`; `-ForcePowerShellTextScan` makes the fallback independently testable
- boundary: reference content and gate semantics remain unchanged; no product/runtime, dependency, auth, provider, secret, MCP, process, or hosted UI change

## Verification and rollback

- local `sync-reference-governance.ps1 -Check` and full fixed gates must pass
- a UTC simulation must preserve the exact ISO string instead of converting it to local time
- GitHub Windows runner must pass the reference parity and repository verification checks
- build passed with 0 warnings and 0 errors; 461 tests passed
- reference evidence, `dotnet format`, release preflight, publish `WhatIf`, diff hygiene, and canonical repository verification passed
- forced fallback preflight initially exposed `Select-String` case-insensitivity and was corrected with `-CaseSensitive`; a normal-path probe then exposed multiple local `rg` installations and was corrected by selecting the first executable
- final `scripts/preflight-release.ps1 -ForcePowerShellTextScan -NoRestore` and normal `scripts/preflight-release.ps1 -NoRestore` runs both exited `0`; each passed reference parity/evidence, placeholder/conflict scans, build, 461 tests, format, publish `WhatIf`, and diff hygiene
- the fallback enumerates only `git ls-files` results, so ignored build artifacts are outside its scan surface
- five-axis review passed with no Critical or Required issue
- rollback: revert only this independent repair PR; the richer mismatch diagnostic can remain as high-signal error reporting

## Completion boundary

- `diagnostic_published=true`
- `root_cause_repaired=true`
- `locally_verified=true`
- `default_branch_effective=false`
