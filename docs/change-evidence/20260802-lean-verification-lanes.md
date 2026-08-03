# Lean Verification Lanes Evidence

**Task:** `FOCUS-004`
**Date:** 2026-08-02
**Authority:** repo-only
**Final repo-side state:** `completed`

## Scope And Starting Measurements

The slice changes repository verification composition and removes low-signal tests. It does not change product runtime, persisted schemas, providers, delivery formats, secrets, user workspaces, or paid-call authority.

Starting observations:

- `preflight-release.ps1` invoked reference-governance parity directly, invoked `verify-reference-evidence.ps1` which performed parity again, then invoked `verify-repo.ps1` which performed parity a third time.
- `verify-repo.ps1` had only a full-repository path; AI inner-loop work had no canonical focused entrypoint.
- `verify-repo.ps1` ran `dotnet build` and then invoked `dotnet test` without `--no-build` unless the caller also supplied `-NoRestore`; the default Full path could therefore build and restore twice, while format could restore again.
- `MainWindowLayoutTests.cs` contained 797 lines, 22 tests, and 244 exact source-token assertions that primarily preserved historical file decomposition.
- `WpfShellAccessibilityTests.cs` contained 94 lines, 4 tests, and 38 exact source-token assertions already overlapped by XML contract checks, ViewModel behavior, packaged UI Automation, and accepted native evidence.
- `Phase7AccessibilityContractTests.PackagedProbe_VerifiesPackageAndKeepsManualBoundariesExplicit` asserted script literals rather than running the package/probe behavior.

## Structured Reference Decision

```reference-decision
{
  "schemaVersion": 1,
  "area": "workflow-and-ux-architecture",
  "trigger": "source-structure-test-reduction",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/02-dotnet-wpf/docs-desktop",
      "revision": "3ed3bb19178883827aa0a81427576797db141862"
    },
    {
      "path": "docs/change-evidence/20260729-wpf-shell-accessibility-baseline.md",
      "revision": "accepted-2026-07-29"
    },
    {
      "path": "scripts/run-packaged-accessibility-probe.ps1",
      "revision": "working-tree-baseline-2026-08-02"
    }
  ],
  "observedBehavior": "The packaged native UI Automation probe already verifies named shell, backup, final-delivery, accessible-name, keyboard-focusable, window-size, DPI, fake-provider, and package-integrity outcomes. ViewModel and domain tests cover commands and state. The deleted tests only compared source text and historical view-file placement.",
  "decision": "reject",
  "affectedContract": "Do not retain exact XAML or PowerShell token assertions when the risk is already covered by public behavior, XML structure, package verification, native UI Automation, or recorded manual evidence.",
  "focusedVerification": [
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1 -Mode Quick -TestFilter VerifyRepoScriptTests -NoRestore",
    "dotnet test ContentDeliveryStudio.sln --no-build --filter Phase7AccessibilityContractTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1 -Paths src/ContentDeliveryStudio.App/ViewModels/MainWindowViewModel.cs,docs/change-evidence/20260802-lean-verification-lanes.md"
  ]
}
```

Decision effect:

- Keep the XML-based principal-form accessibility contract because it checks a semantic property across a bounded view set.
- Keep DPI manifest and global focus-style declarations because those are static Windows contracts that cannot be inferred from ViewModel tests.
- Remove exact file-placement and script-literal assertions whose deletion does not remove user-visible risk coverage.
- Treat native Narrator, high contrast, non-default DPI, touch/pen, and low-memory results as manual/hardware evidence; static tests still cannot close them.

## Target Gate Composition

| Lane | Interface | Required behavior |
| --- | --- | --- |
| Quick | `verify-repo.ps1 -Mode Quick -TestFilter <filter>` | Build the solution, run only the named focused tests, require an explicit filter, and omit format, reference, package, and release scans. |
| Full | `verify-repo.ps1` | Build, run the core suite excluding explicit `Category=ReleaseOnly` long-tail tests, verify reference evidence/parity once, verify the product-focus queue, and run range-aware diff hygiene without Roslyn formatter startup. |
| Release | `preflight-release.ps1` | Run release-inclusive repository verification once with all tests and full-solution format, then add placeholder/conflict scans, actual publish/package verification, and staged/unstaged diff hygiene. |

## Preserved Hard Guards

- scientific source, authority, mutation, deterministic rendering, and package tests;
- secret redaction and provider request/response contract tests;
- additive SQLite migration, reload, and backup/restore integrity tests;
- approval, paid-call, queue interruption, and no-automatic-replay tests;
- actual publish/package hash and membership verification;
- reference decisions for provider, host, persistence, tooling, workflow, pack, extraction, and scientific high-drift changes.

## Compatibility And Rollback

- Existing no-argument `verify-repo.ps1` remains the canonical Full interface.
- Existing `-NoRestore`, diff-range, and reference-skip parameters remain supported.
- Rollback restores the prior three scripts and deleted source-structure tests, then regenerates reference governance from `scripts/reference-basis.json`.
- No Git rollback is used as a substitute for user workspace or generated-asset restoration; this slice does not mutate either.

## Verification Results

| Lane or contract | Command | Result |
| --- | --- | --- |
| Quick, script behavior | `verify-repo.ps1 -Mode Quick -TestFilter VerifyRepoScriptTests -NoRestore` | Exit 0; build 0 warnings/errors; 8/8 focused tests; 18.8 seconds. |
| Quick, retained WPF semantic structure | `verify-repo.ps1 -Mode Quick -TestFilter Phase7AccessibilityContractTests -NoRestore` | Exit 0; build 0 warnings/errors; 10/10 focused cases; 5.6 seconds. |
| Quick fail-closed | `verify-repo.ps1 -Mode Quick -NoRestore` | Expected exit 1; rejected missing `-TestFilter` before build. |
| Reference negative | Explicit WPF source path plus narrative-only `docs/ARCHITECTURE.md` | Expected exit 1; rejected missing structured decision. |
| Reference positive | Explicit WPF source path plus this evidence file | Exit 0; verified `reject / source-structure-test-reduction`. |
| Full | `verify-repo.ps1 -NoRestore` | Exit 0; build 0 warnings/errors; 739/739 core tests; one reference parity/evidence pass; product-focus contract; range-aware diff check; 26.0 seconds after the deep audit on 2026-08-03. |
| Release | `preflight-release.ps1 -NoRestore` | Exit 0; release-inclusive verification ran once; 760/760 tests; complete changed-C# format rules; placeholder/conflict scans; actual package verification; diff hygiene; 71.8 seconds after the deep audit on 2026-08-03. |

The release package contained 84 payload files, selected `ContentDeliveryStudio.App.exe`, and verified SHA-256 `2874644bd3ca7735aee5d3e290ed62b8c2f9a1fd2329ce0cb7fa61c94eaa1a4b` before cleanup.

Test-governance result:

- removed 27 source-token tests: 22 main-window file-placement tests, 4 duplicate shell accessibility token tests, and 1 packaged-probe script-literal test;
- added 5 behavior tests for Quick filtering, single-build/single-restore Full composition, single Full composition inside Release, and structured reference evidence rejection/acceptance;
- retained 10 semantic WPF accessibility cases based on XML structure and static Windows declarations;
- total suite changed from 782 to 760 tests while preserving scientific, provider, persistence, queue/no-replay, secret, package, backup/restore, and delivery coverage.

During focused validation, the PowerShell fallback text scanner failed when `git ls-files` returned a deleted tracked test. The regression test exposed the exact missing path; the scanner now skips non-existent tracked paths before `Select-String`. The focused regression and Full/Release gates pass after the root fix.

During the 2026-08-03 audit, a new red-green script contract exposed the remaining default-path duplication: before the fix the recorded invocation was `test ContentDeliveryStudio.sln`, without `--no-build`. Full now makes the build step the only restore/build point; test always uses `--no-build --no-restore`, and format always uses `--no-restore` against the explicit solution.

No paid provider, external mutation, manual operator, or hardware action occurred. These results close only the repo-side `FOCUS-004` contract.
