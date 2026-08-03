# Deep Verification Performance Audit

**Date:** 2026-08-03
**Scope:** test, gate, and repository-governance execution cost
**Authority:** repo-only

## Starting Evidence

The first lean-gate pass removed repeated reference work but did not inspect the test-duration distribution or the cost of full-solution formatting. A fresh TRX run established:

- 760 tests: 30.2 seconds wall time and 208.04 seconds summed test duration;
- 53 tests at or above one second and 24 at or above two seconds;
- `LargeGalleryPerformanceBenchmarkTests`: 20.32 seconds for one 1,000-row benchmark;
- `ScientificFigureCorpusAcceptanceTests`: 20.45 seconds summed across two full-corpus tests;
- `ScientificFigureOperatorTrialScriptTests`: 23.25 seconds summed across nine PowerShell lifecycle tests;
- `VerifyRepoScriptTests`: 21.62 seconds summed across eight self-hosting gate tests;
- full-solution `dotnet format --verify-no-changes`: 23.8 seconds by itself;
- reference evidence: 1.06 seconds; product-focus contract: 0.73 seconds; no evidence that either governance contract was an execution bottleneck.

The root cause was lane misclassification, not simply excessive test count: performance, whole-corpus, end-to-end launch, operator-script, and gate-self-test workloads ran in every Full invocation, while full-solution formatting cost more than the complete core test wall time.

## Repair

- Mark exactly 21 intrinsically long-running or self-hosting tests as `Category=ReleaseOnly`:
  - one 1,000-row gallery benchmark;
  - two full scientific corpus acceptance tests;
  - one three-run fake-first launch proof;
  - nine operator PowerShell lifecycle tests;
  - eight verification-script self-tests.
- Full runs 739 core tests and excludes only that explicit category.
- Release and mainline CI still run all 760 tests once; pull requests run Full rather than packaging the same feature change again.
- Explicit filters still run a release-only class directly for focused development.
- Full uses range-aware `git diff --check` over supplied CI refs or local staged/unstaged state; build continues to run compilation and analyzers without paying Roslyn formatter startup on every invocation.
- Release applies complete `dotnet format` rules to changed C#; formatting-configuration changes or an unknown clean range trigger a full-solution scan.
- GitHub Actions no longer runs Release for both feature pushes and pull requests; PRs run Full, while `main`/`master` pushes run Release.
- Build remains the only restore/build step; tests and format reuse its outputs.

No test was deleted in this follow-up. Domain, provider, persistence, schema, security, paid-call, compatibility, packaging, and scientific mutation guards remain in the core or release-inclusive lane according to their execution cost and claim boundary.

## Verification Results

| Surface | Before | Fresh result | Decision |
| --- | ---: | ---: | --- |
| Raw test wall | 760 tests in 30.2 s | 739 core tests in 16.5 s in the isolated comparison | Core feedback reduced about 45%; all tests retained for Release. |
| Canonical Full | 51.1 s | 26.0 s; build clean; 739/739; reference/product contracts and diff check passed | About 49% faster without claiming ReleaseOnly coverage. |
| Release | 79.7 s | 71.8 s; 760/760; complete changed-C# format rules; scans, package, hashes, and diff hygiene passed | Remaining cost is the intentional long-tail suite plus actual publish/package work. |
| Reference contract | not isolated | 1.06 s | Retained; not a meaningful bottleneck. |
| Product-focus contract | not isolated | 0.73 s | Retained; not a meaningful bottleneck. |
| Format policy | full solution cost 23.8 s | absent from Full; Release uses trustworthy changed-C# scope, with fail-safe full fallback | Removes Roslyn startup from the high-frequency lane without dropping release formatting. |

The release package contained 84 payload files, selected `ContentDeliveryStudio.App.exe`, and verified SHA-256 `2874644bd3ca7735aee5d3e290ed62b8c2f9a1fd2329ce0cb7fa61c94eaa1a4b` before cleanup.

The GitHub Actions change is repo-side configuration only. It proves that the workflow now routes pull requests to Full and mainline pushes to Release; it does not claim a hosted run until the change is pushed and GitHub Actions completes.

## Boundary And Rollback

This is a repository execution-cost repair. It does not establish live-provider, paid-call, expert, manual operator, Narrator, hardware-DPI, touch/pen, or low-memory acceptance.

Rollback removes the five class traits, restores Full to all tests and full-solution format, restores the prior CI triggers, and removes the Release mode handoff. It must not delete any of the retained long-tail tests.
