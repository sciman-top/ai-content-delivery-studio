# Product Focus And Simplification Planning Evidence

**Date:** 2026-08-02
**Task:** `FOCUS-001`
**Authority:** repo-only
**Risk:** low planning change plus medium repository-verifier integration

## 1. Starting Truth

- Start branch state: `main...origin/main [ahead 12]`.
- The initial worktree contained only three untracked files from this slice: the post-V1 PRD, machine queue, and design spec.
- `docs/PRD_V1.md`, `docs/V1_LAUNCH_EVIDENCE.md`, accepted scientific evidence, and historical implementation plans were treated as immutable history.
- The prior V1 repository queue and the original trustworthy-scientific-figure Tasks 1-30 remain closed. This slice opens a new product-focus queue; it does not relabel either historical closure.

## 2. Review Basis And Decision

The review found a mature image-series spine and a rigorous scientific-figure chain, but also several future-facing abstractions whose user-visible consumption was weak: repository module metadata in production runtime, fake-only remote workflow contracts, broad pack/view-slot metadata, a general operator model, and many source-shape tests. At the same time, the ordinary production path still lacked paid-dispatch approval receipts, a real reference/edit operation, stronger scholarly structure extraction, data-grounded charts, and named Windows hardware acceptance.

The program therefore keeps two production lanes:

- `image-series-production`
- `trustworthy-scientific-figures`

Document illustration is an input adapter. Courseware, poster, article, report, and social outputs are profiles. The following remain frozen: remote workflows, a public pack ecosystem, a general operator platform, additional provider abstractions, a graph editor, and partial-image streaming.

## 3. Planning And Verification Surfaces

- `docs/PRD_POST_V1_PRODUCT_FOCUS.md`
- `docs/product-focus-execution.json`
- `docs/superpowers/specs/2026-08-02-product-focus-and-simplification-design.md`
- `docs/superpowers/plans/2026-08-02-product-focus-and-simplification.md`
- `docs/ROADMAP.md`
- `docs/TASKS.md`
- `docs/DOCUMENTATION_GOVERNANCE.md`
- `scripts/verify-product-focus-plan.ps1`
- `scripts/verify-repo.ps1`
- this evidence record

The JSON queue defines 13 tasks with priority, state, lane, dependency, authority, risk, write-set, verification, acceptance, evidence, and rollback. The narrative plan adds preconditions, implementation sequence, focused tests, stop conditions, and task-local rollback for AI execution.

## 4. Verifier Contract

The product-focus verifier checks:

- schema and plan identity;
- the exact maturity, production-lane, and frozen-capability sets;
- task ID and order uniqueness;
- allowed task state, priority, risk, lane, and authority values;
- required goal, write-set, verification, acceptance, evidence, and rollback fields;
- repository-relative write sets without parent-directory escape;
- dependency existence, self-dependency, duplicate dependency, and cycle rejection;
- completed dependencies for `ready` tasks and at most one `in_progress` task;
- evidence-file existence for completed tasks that name a repository path;
- PRD, spec, implementation-plan, and task-checklist synchronization;
- the lowest-order next `ready` task.

The verifier is called after solution tests and before format verification in `scripts/verify-repo.ps1`. It extends the existing contract stage and does not introduce another gate runner.

## 5. Authority And Side Effects

- No paid provider was called.
- No external system was mutated.
- No human-expert, live-provider, manual-operator, or hardware acceptance was claimed.
- `FOCUS-002` and `FOCUS-003` remain blocked on external authority.
- The next autonomous repository task is `FOCUS-004`.

## 6. Compatibility And Rollback

- Product runtime, provider behavior, persisted schema, delivery formats, and user workspace data are unchanged by this planning slice.
- The locked V1 PRD and historical evidence are unchanged.
- Rollback consists of reverting only the ten files listed in section 3. It does not delete user workspace/output data or alter historical acceptance records.

## 7. Fresh Verification Results

All commands ran from `D:\CODE\ai-content-delivery-studio` on `2026-08-02` after the complete planning/verifier write-set was present.

| Command | Exit | Evidence |
| --- | ---: | --- |
| JSON `ConvertFrom-Json` parse | 0 | `schemaVersion=1`, `planId=post-v1-product-focus-2026-08`, `tasks=13`. |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-product-focus-plan.ps1` | 0 | 13 tasks; `blocked_external=2`, `completed=1`, `proposed=9`, `ready=1`; next task `FOCUS-004`. |
| `dotnet build ContentDeliveryStudio.sln` | 0 | 0 warnings, 0 errors, 17.13 seconds. |
| `dotnet test ContentDeliveryStudio.sln --no-build` | 0 | 782 passed, 0 failed, 0 skipped, 20 seconds. |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1` | 0 | Reference governance in sync; no enforced high-drift engineering area touched. |
| `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore` | 0 | No formatting changes required. |
| `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore` | 0 | 57.6 seconds; placeholder/conflict scans, reference parity/evidence, build, 782/782 tests, product-focus verifier, format, actual win-x64 publish/package verification, and staged/unstaged diff hygiene passed. |

The transient release package contained 84 payload files, selected `ContentDeliveryStudio.App.exe` as the entrypoint, and verified SHA-256 `2874644bd3ca7735aee5d3e290ed62b8c2f9a1fd2329ce0cb7fa61c94eaa1a4b` before preflight cleanup.

These results establish repo-side planning and verifier completion for `FOCUS-001`. They do not establish physics-expert review, paid/live provider acceptance, native manual acceptance, or named-hardware acceptance.
