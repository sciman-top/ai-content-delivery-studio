# Task Checklist

## Current V1 Release Readout

Use [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md) as the truth source for current release claims. The latest recorded release-verification readout is:

- Latest recorded release-verification snapshot: `2026-06-23`
- Snapshot readout: no open V1 release-claim gaps remain in that snapshot; all `5 / 5` launch metrics remain verified. The snapshot captured `433 / 433` automated tests and the opt-in OpenAI `2-item` sample under `artifacts/live-openai-v1-sample/20260611-132947`. The newer repo-only baseline is `734 / 734` in [Phase 7 closeout evidence](./change-evidence/20260730-phase7-product-hardening-closeout.md); it does not refresh the historical live-provider sample.
- Reopen this as a release-gap section only when provider behavior materially changes, a newer live-provider snapshot is needed, or a regression invalidates the existing evidence
- Remaining non-release work is intentionally grouped into deferred trigger notes: OCR reference coverage when scanned-document hardening enters the active roadmap, scholarly PDF extraction references when paper-figure evidence extraction becomes active, real additional scenario pack-policy hardening only when a new scenario has a repo-owned spec and bounded plan, and partial-image streaming UX only if a future workbench flow proves it useful.

## Current Queue Status

- The historical V1 execution queue remains closed, but the post-V1 product-focus queue is active. Its mutable truth is [product-focus-execution.json](./product-focus-execution.json); use the [post-V1 PRD](./PRD_POST_V1_PRODUCT_FOCUS.md), [design](./superpowers/specs/2026-08-02-product-focus-and-simplification-design.md), and [implementation plan](./superpowers/plans/2026-08-02-product-focus-and-simplification.md) to interpret each task.
- Two production lanes are in scope: `image-series-production` and `trustworthy-scientific-figures`. Scenario labels are profiles, not independent platforms.
- `FOCUS-004` through `FOCUS-011` are repo-side complete. `FOCUS-002`, `FOCUS-003`, `FOCUS-012`, and `FOCUS-013` require external human/live/hardware authority before further execution.
- The recorded V1 implementation-plan surface remains closed and its launch evidence is unchanged.
- The post-V1 trustworthy-scientific-figure flagship is accepted through Task 30 and Checkpoint 5. Tasks 6-20 and Checkpoint 3 establish the machine-trust loop; Tasks 21-25 and Checkpoint 4 provide the user-visible fake-first five-workspace flow; Task 26 accepts all 12 baselines plus 40 blocking mutations; Tasks 27-28 add opt-in OpenAI understanding and independent review; Task 29 records three live samples and their human Gate 2 approvals; Task 30 synchronizes documentation and final repository evidence. No scientific-figure implementation task remains open.
- The active scope and authority live in [the scientific figure design](./superpowers/specs/2026-07-25-scientific-figure-trustworthy-workflow-design.md) and [implementation plan](./superpowers/plans/2026-07-25-scientific-figure-trustworthy-workflow.md).
- The post-acceptance native WPF shell/Diagnostics accessibility baseline is repo-side complete with XAML contracts and `authorized_agent` equivalent-operator acceptance. This does not reopen Tasks 1-30 or Checkpoints 0-5 and does not close Narrator, high-contrast, DPI, full-form, virtualized gallery/grid, touch/pen, or packaged-app trigger lanes.
- Phase 7 product hardening is repo-side closed through the verified Windows ZIP, safe backup/restore, budgeted 1,000-row gallery benchmark, principal-form accessibility contracts, PerMonitorV2 declaration, and packaged-app UIA probe. Narrator, real system high-contrast switching, a non-default-DPI hardware matrix, touch/pen, subjective scroll quality, and real low-memory devices remain manual/live acceptance rather than repository tasks.
- Reliability Hardening Wave 2 is closed: the `latest-wins`/`exclusive` operation gate, responsibility-focused ViewModel partials, race/failure guardrail tests, and repository-scan pruning landed in `ab3e42d`; its previously stale checklist was reconciled with fresh evidence on `2026-07-29`.
- The unified final-image layout follow-through is repo-side complete: the generic WPF approval panel now selects the finite delivery category, defaults and browses a custom final root, previews the resolved category directory, rejects temporary-workspace roots, and still exports only human-approved `Pass` items. Article Gate 1/Gate 2 and live-provider acceptance remain separate pending authority boundaries.

## Active Post-V1 Product-Focus Queue

The status values below mirror `docs/product-focus-execution.json` as of `2026-08-03`. Update the JSON first whenever state changes, then synchronize this summary in the same slice.

| Task | Priority | State | Authority | Dependency | Execution outcome |
| --- | --- | --- | --- | --- | --- |
| `FOCUS-001` | P0 | `completed` | repo-only | none | Establish the post-V1 PRD, design, detailed plan, machine queue, verifier, navigation, and evidence without changing the locked V1 snapshot. |
| `FOCUS-002` | P0 | `blocked_external` | human-expert | `FOCUS-001` | Record a named physics-expert Gate 1 decision for all six reconstructed article figures. |
| `FOCUS-003` | P0 | `blocked_external` | paid-live-approval | `FOCUS-002` | Run one explicitly authorized live scientific acceptance from the exact Gate-1 identity. |
| `FOCUS-004` | P0 | `completed` | repo-only | `FOCUS-001` | Separate quick/full/release gates and remove low-signal governance and source-shape tests while retaining high-risk guards. |
| `FOCUS-005` | P1 | `completed` | repo-only | `FOCUS-004` | Image-series stages now own state/commands under `ImageSeriesWorkspace`; scientific state remains isolated, MainWindow partials fell 34.9%, and public properties fell 42.1%. |
| `FOCUS-006` | P1 | `completed` | repo-only | `FOCUS-004` | Removed unconsumed repository-folder module metadata and the fake-only remote-workflow contract/DI registration after persistence, package, runtime, and UI consumer inventory. |
| `FOCUS-007` | P1 | `completed` | repo-only | `FOCUS-006` | Retired the unconsumed built-in starter catalog and export surface; retained import-only `pack-package.v1` validation with scenario IDs treated as descriptive profiles. |
| `FOCUS-008` | P1 | `completed` | repo-only until live probe | `FOCUS-004`, `FOCUS-005` | Immutable approval receipts, additive reload, cost-bounded captured-provider dispatch, invalidation, pause, and no-replay behavior are repo-side complete; any paid probe still requires separate authority. |
| `FOCUS-009` | P1 | `completed` | repo-only until live probe | `FOCUS-008` | Added a captured real Images edit provider, immutable approval, non-destructive persisted candidate lineage, capability-aware fail-closed UI, and path-safe delivery provenance; paid live proof remains separately gated. |
| `FOCUS-010` | P1 | `completed` | repo-only | `FOCUS-004` | Recover scholarly reading order, captions, formulas, tables, and citations with explicit fail-closed states. |
| `FOCUS-011` | P2 | `completed` | repo-only | `FOCUS-010` | Add deterministic charts whose values, units, transforms, axes, and provenance come from hashed structured data. |
| `FOCUS-012` | P2 | `blocked_external` | manual-hardware | `FOCUS-005`, `FOCUS-008`, `FOCUS-009` | Replace high-value source assertions with packaged native UIA and named Windows/hardware acceptance. |
| `FOCUS-013` | P2 | `blocked_external` | mixed-explicit | seven production dependencies | Publish a new truthful evidence snapshot without relabeling historical V1 or scientific evidence. |

AI execution rule: run `scripts/verify-product-focus-plan.ps1`, select the lowest-order `ready` task, set only that task to `in_progress`, honor its authority and write-set, run focused verification before the full gate, create one bounded evidence record, then mark it `completed` and promote newly unblocked work. Never treat repo-only completion as human, live, paid, or hardware acceptance.

## Accepted Post-V1 Flagship Slice: Trustworthy Scientific Figures

- [x] Approve the physics and natural-science flagship scope, authority chain, SVG-first rendering, three-layer review, and two human gates.
- [x] Record the repository-owned design specification.
- [x] Record the risk-first, checkpointed implementation plan.
- [x] Establish primary-source dependency decisions for extraction, SVG, formula rendering, and export without adopting new runtime dependencies.
- [x] Establish the machine-readable corpus, gold-baseline, and local-cache contracts without admitting source binaries.
- [x] Prepare four mechanism/process candidates and draft baselines for human corpus review without marking them accepted.
- [x] Prepare four concept/comparison candidates and draft baselines with explicit relation classes for human corpus review without marking them accepted.
- [x] Prepare four graphical-abstract candidates and draft baselines with explicit non-evidentiary asset boundaries for human corpus review without marking them accepted.
- [x] Establish the 12-item human-approved evaluation corpus.
- [x] Establish immutable scientific source, location, recovery, quality, diagnostic, and blocked-extraction records for Task 6.
- [x] Establish source-bound scientific claims, role-preserving evidence, conflicts, coverage, and blocked-understanding records for Task 7.
- [x] Establish evidence-bound Figure Spec elements/relations, Gate 1 approval, versioning, and downstream invalidation records for Task 8.
- [x] Persist the project-owned scientific workflow aggregate with versioned JSON, indexed authority fields, fail-closed reload, and additive SQLite schema initialization for Task 9.
- [x] Adapt text-bearing PDF, Markdown, text, and paste sources into provenance-preserving scientific extraction with fail-closed OCR, reading-order, and required-content diagnostics for Task 10.
- [x] Deliver provider-neutral chunked understanding, deterministic fake claims, cross-chunk conflict blocking, explicit Gate 1 decisions, and persisted authority snapshots for Task 11.
- [x] Compile only current Gate-1-approved Figure Specs into deterministic, validated SVG render plans with stable authority mappings for Task 12.
- [x] Render deterministic, editable scientific SVG with stable IDs, exact text/formulas, accessibility metadata, and non-authoritative decorative boundaries for Task 13.
- [x] Export PNG and PDF only from the exact approved SVG hash with provenance metadata, semantic fixtures, bounded rendering, and visual equivalence checks for Task 14.
- [x] Register the internal `scientific-figure` pack family and complete the approved-spec-to-export Checkpoint 2 while keeping WPF availability disabled for Task 15.
- [x] Compare Figure Spec, render plan, SVG authority, and exports deterministically; hard-fail missing/extra/reversed/exact-content/export mutations without allowing advisory scores to override findings for Task 16.
- [x] Define minimum-evidence scientific semantic review and full-resolution typed-crop visual review contracts, deterministic fakes, and strict non-pass/invalid/missing/provider-failure blocking for Task 17.
- [x] Build path-redacted review manifests, critical SVG structure rows, real full-resolution PNG crops, and pre-dispatch missing/oversized artifact guards for Task 18.
- [x] Route extraction/understanding/specification/renderer/layout/asset/export findings, restrict automation to presentation/non-evidentiary assets, invalidate scientific approvals, and reject a fourth automatic repair for Task 19.
- [x] Enforce explicit Gate 2, route human rejection to repair, and package SVG/PNG/PDF plus specification, provenance map, reviews, repairs, providers, and both human approvals for Task 20 and Checkpoint 3.
- [x] Add the localized five-stage scientific workspace shell, authoritative workflow-state projection, stable AutomationIds, and hidden-by-default tab registration for Task 21.
- [x] Add source extraction diagnostics, exact claim-evidence navigation, visible conflicts/blocks, and non-mutating audited claim correction drafts for Task 22.
- [x] Add evidence-linked element/relation inspection, validated typed proposal diffs, domain-eligible Gate 1 controls, and frozen authority versions for Task 23.
- [x] Add sanitized zoomable SVG review, item-to-source provenance, separated contract/semantic/visual findings, and presentation-only automatic repair controls for Task 24.
- [x] Add cross-format SVG/PNG/PDF delivery comparison, complete evidence and provider projection, explicit Gate 2 approval/rejection, user-initiated package export, and user-visible fake-first WPF acceptance for Task 25 and Checkpoint 4.
- [x] Replay all 12 human-approved corpus baselines through extraction, understanding, Gate 1, deterministic rendering/export, contract review, and fake machine review; block all 40 declared scientific and visual mutations and write a deterministic Task 26 report.
- [x] Add opt-in OpenAI scientific understanding with bounded source-location chunks, strict stateless Responses Structured Outputs, local evidence/proposal/conflict validation, captured contract tests, and fake-by-default runtime selection for Task 27.
- [x] Add opt-in OpenAI scientific semantic and visual review with approved evidence, original PNG bytes, typed critical-item crops, strict stateless output, local responsibility validation, and fail-closed runtime selection for Task 28.
- [x] Deliver trustworthy extraction, claim-evidence understanding, persistence, and Gate 1.
- [x] Deliver deterministic SVG, PNG/PDF export, and the internal `scientific-figure` pack.
- [x] Deliver contract/scientific/visual review, bounded repair, Gate 2, and evidence-backed delivery.
- [x] Deliver the five-workspace WPF flow and pass fake-first workbench acceptance.
- [x] Replay and accept all 12 fixed corpus baselines and blocking mutations.
- [x] After explicit paid-call approval, record live-provider and human acceptance evidence.
- [x] Synchronize user guidance, roadmap status, launch evidence, migration, rollback, and closeout truth.

## Post-Acceptance Operator Evidence Support

- [x] Add a repo-owned fake-first scientific-figure operator-trial kit with an isolated local data root, five-workspace checklist, accepted/rejected finalization, and delivery ZIP validation.
- [x] Keep generated sessions under ignored `outputs/` and preserve existing accepted corpus/live artifacts.
- [x] Run an [agent-operated native WPF fake-first probe](./change-evidence/20260728-scientific-figure-agent-operated-trial.md) across all five workspaces, export and inspect an isolated delivery ZIP, and preserve its truthful agent identity.
- [x] Make the operator-trial script resolve its repository from the script location so the bilingual runbook commands work from any PowerShell directory.
- [x] Align accepted-package validation with the real WPF export contract: Gate 2 approval records do not carry a synthetic `Approved` field, and manifest hashes use the `sha256:` prefix.
- [x] Accept and finalize the complete native WPF trial through the [authorized-agent equivalent operator contract](./change-evidence/20260728-scientific-figure-authorized-agent-acceptance.md): the actor remains `authorized_agent`, explicit user authority is recorded, and the same five-workspace, Gate 2, ZIP, and hash gates produce `operator/manual evidence`. Automated tests and the incomplete no-ZIP probe still do not count.

## Completed Post-Acceptance Reliability Slice

- [x] Persist generation tasks as `Queued` before dispatch, `Running` before the provider call, and terminal after the call.
- [x] Recover orphaned `Running` tasks as `Failed` without replaying provider calls; preserve explicitly prepared `Queued` and `Paused` work across reload.
- [x] Add nullable SQLite `GenerationTasks.ErrorMessage`, `QueuePosition`, and `RetryOfTaskId` compatibility columns through idempotent additive initialization.
- [x] Restore Queue rows, attempts, output paths, and failure reasons from persisted project state after WPF reload.
- [x] Record focused automated coverage and an [agent-operated native WPF fake-first reload probe](./change-evidence/20260728-crash-safe-generation-queue.md) without refreshing live-provider evidence.
- [x] Add an approved two-stage operator workflow with separate Prepare and Execute, per-item pause/resume/reorder, provenance-preserving retry, localized WPF controls, and explicit zero-provider-call mutation boundaries.
- [x] Keep the compatibility one-click fake generation route while requiring a separate future approval receipt and cost summary before any live-provider Execute path can exist.

## Completed Post-Acceptance Local Diagnostics Slice

- [x] Register the existing redacting diagnostics writer and expose it through a read-only application export service.
- [x] Snapshot resolved fake-first provider capabilities and generic secret-presence booleans without reading secret values, making network calls, or emitting configured secret identifiers.
- [x] Add a localized Workbench Inspector panel with a native folder picker, unique local export directory, stable AutomationIds, busy state, and bounded status reporting.
- [x] Run automated diagnostics coverage plus an `authorized_agent` native WPF Browse -> Export probe and verify both output files and redaction boundaries.
- [x] Add repo-owned structured log ingestion through a local, strongly typed JSONL journal for generation-queue lifecycle and provider-call summaries.
- [x] Rotate at 1 MiB with three-file retention, ingest at most 500 validated recent events, and expose dropped/invalid counts in diagnostics JSON and Markdown.
- [x] Keep prompts, response bodies, material content, credentials, `.env`, full exceptions, endpoints, request IDs, and user file paths outside the event contract; preserve existing OTLP opt-in behavior.

## Completed Phase 7 Product Hardening Closeout

- [x] Version safe backup manifests with normalized paths, byte lengths, and SHA-256; create archives through a temporary file and replace only after success.
- [x] Validate complete backup membership, path safety, duplicate paths, supported limits, target conflicts, sizes, and hashes before restore writes its first file.
- [x] Expose localized safe backup and separate-folder restore in the WPF inspector while keeping secrets, databases, workspaces, outputs, and build files excluded.
- [x] Produce a framework-dependent `win-x64` distributable ZIP with portable payload hashes and an archive SHA-256 sidecar; reject tampering through an independent verifier.
- [x] Replace publish WhatIf with an actual isolated publish/package/verify/cleanup release preflight and declare the supported `win-x64` restore graph.
- [x] Enforce broad non-flaky time/memory budgets for the existing 1,000-row gallery benchmark and retain row-limited import and cache-benefit assertions.
- [x] Add virtualized gallery selection, keyboard focus, localized UI Automation, deferred scrolling, and system-color contracts.
- [x] Add PerMonitorV2 declaration, global system-brush focus visuals, and stable names/IDs for all interactive controls in the principal inspector forms.
- [x] Launch the verified ZIP under fake providers and record current-DPI minimum layout plus named shell/Diagnostics/backup UIA evidence.
- [x] Reuse the existing physics-poster and scientific-figure operator samples instead of creating a duplicate sample solely for roadmap wording.

Manual/live acceptance remains outside the checked repository queue: Narrator, actual system high-contrast switching, non-default-DPI hardware matrix, touch/pen ergonomics, subjective WPF scroll responsiveness, and real low-memory Windows behavior.

## Near-Term Hardening (Not Current Release Blockers)

These items are still valuable, but they are not the same thing as the current V1 release gap.

- [x] Harden the short requirement -> brief -> blueprint -> series -> review -> delivery path as the primary V1 launch route.
- [x] Harden the article or plain-text -> evidence anchors -> illustration targets -> promoted plan -> delivery path as a supporting validation route without requiring real providers by default.
- [x] Complete Phase 4A deterministic text composition, readability checks, reviewer notes, and approval evidence export.
- [x] Centralize categorized final-image delivery roots, custom-root precedence, and the application request factory while keeping candidates/compositions in workspace until approval.
- [x] Implement the provider routing policy defaults for Images API vs Responses API, structured outputs, and `store: false` by default.
- [x] Evaluate and adopt the official OpenAI .NET SDK where stable; keep raw `HttpClient` only for unsupported or lagging gaps.
- [x] Add a bounded transient `502 upstream_error` retry on the official SDK Images path before failing the live OpenAI route.
- [x] Add Responses API multi-turn image state only where it improves provenance or revision loops.
- [x] Add bounded local review-prep artifacts and review-batch thresholds before expanding multi-turn image-state review.
- [x] Run the first real low-risk operator adapter end-to-end with audit evidence and rollback notes.
- [x] Capture V1 launch evidence against the explicit launch metrics.
- [x] Encode and reuse the text-planning low-502 execution policy on the official SDK Responses text-planning path; future real-provider brief or blueprint planning must use this boundary when it leaves the current fake-first mode.

## V1 Documentation And Policy Alignment

- [x] Record V1 launch PRD.
- [x] Record provider routing policy.
- [x] Record operator risk policy.
- [x] Record source and artifact support matrix.
- [x] Record target engineering state.
- [x] Record external reference strategy.
- [x] Record V1 launch hardening implementation plan.
- [x] Align product design, roadmap, and task checklist to the V1 launch boundary.

## V1 Clarifications

- [x] Reconfirm the primary V1 audience as solo creator or teacher-like power user.
- [x] Reconfirm the short requirement -> image-series route as the only primary launch workflow.
- [x] Lock the first real operator slice to an additive local validation action.
- [x] Choose the deterministic text composition implementation library: `SkiaSharp`.
- [x] Keep packs internal-only for V1 and defer public sharing behavior.
- [x] Reflect the locked V1 defaults in implementation-facing code comments, options, and operator descriptors where relevant.
- [x] Reflect stateless local-direct visual review defaults in implementation-facing options and review operator descriptors.

## Engineering Workflow Governance

- [x] Record the repository-owned AI coding workflow v1.
- [x] Keep `docs/superpowers/specs/` and `docs/superpowers/plans/` as the long-lived engineering spec and plan surfaces for non-trivial slices.
- [x] Default to one agent completing one bounded slice; use subagents and worktrees only when independence or risk clearly justifies them.
- [x] Keep auto-execution layered: low-risk documentation and evidence sync may run automatically, while stronger contract, schema, or cross-surface changes still require explicit spec/plan plus fresh verification.

## Reference Governance

- [x] Record external reference strategy in project docs.
- [x] Establish `_shared` reference governance with manifest, update script, and duplicate audit script.
- [x] Add machine-readable manifest and update flow for `ai-content-delivery-studio-references`.
- [x] Extend `ai-coding-runtime-references` update flow to export a manifest.
- [x] Add a repository-local reference-evidence policy and verification gate for high-drift engineering areas.
- [x] Add a durable `reference-basis` mapping from code areas and task families to local references and reuse levels.
- [x] Move enforced reference-area logic to a machine-readable repository manifest.
- [x] Add a canonical local full-gate script that runs reference evidence checks before build, test, and format verification.
- [x] Add a GitHub Actions verification workflow that reuses the repository gate on normal `push` and `pull_request` events.
- [x] Add a stronger release-style preflight script that layers placeholder, merge-conflict, publish-dry-run, and diff-hygiene checks on top of the canonical gate.
- [x] Add machine-checked parity between `docs/REFERENCE_BASIS.md` and `scripts/reference-basis.json`.
- [x] Add a repo-side snapshot of the external reference shelf manifest and check it in local verification gates.
- [x] Add `dotnet/extensions` as a code-level reference before the next host/options/resilience hardening slice.
- [x] Add `opentelemetry-dotnet` as a code-level reference before the next telemetry or diagnostics slice.
- [x] Add `aspire` as a code-level reference before the next OTLP or local dashboard observability slice.
- [x] Add `SkiaSharp` reference-source coverage before the next deterministic composition expansion beyond the current poster proof path.
- Deferred: Add OCR reference coverage such as `Tesseract` or `OCRmyPDF` only when scanned-document hardening enters the active near-term roadmap.
- Deferred: Add scholarly PDF extraction references such as `GROBID` only when paper-figure evidence extraction enters the active near-term roadmap.
- [x] Decide whether `Cockpit-Tools-Local-references` should gain a machine-readable manifest next.

## Deferred Triggers

- [x] Add real-provider execution follow-through for document illustration after the fake-first planning path is hardened.
- [x] Add targeted binary extraction hardening only for formats promoted by the support matrix.
- [x] Add the first generic-scenario pack and policy modeling hardening slice with explicit scenario and policy-pack references.
- [x] Extend the stronger pack/policy contract to the built-in `article-illustration` scenario.
- [x] Extend the stronger pack/policy contract to the built-in `document-review-translation` scenario.
- [x] Extend the stronger pack/policy contract to the built-in `courseware-visual` scenario.
- [x] Extend the stronger pack/policy contract to the built-in `poster-report-delivery` scenario.
- Deferred: Continue pack and policy modeling hardening only when a real additional scenario beyond the current built-in starter set has a repo-owned spec and a bounded implementation plan.

## Frozen Until Post-V1

- Additional physical repository or namespace rename work beyond the completed mechanical rename and bounded compatibility posture.
- Broad pack-catalog widening beyond launch routes.
- Remote workflow-engine integrations beyond contract-boundary planning.
- Broad binary document automation beyond the support matrix.
- Browser or desktop operator flows that change third-party system state.

## Foundation

- [x] Create independent repository under `D:\CODE`.
- [x] Record product design.
- [x] Record architecture and stack decision.
- [x] Record official and community references.
- [x] Record roadmap and implementation plan.
- [x] Record product identity and repository rename path.

## Phase 1: Core Model

- [x] Create .NET solution and projects.
- [x] Add WPF app targeting `net10.0-windows`.
- [x] Add `ContentDeliveryStudio.Core` domain library.
- [x] Add `ContentDeliveryStudio.Infrastructure` library.
- [x] Add test project.
- [x] Define domain entities and state machines.
- [x] Define provider contracts.
- [x] Add fake providers.
- [x] Add SQLite persistence.
- [x] Add workspace folder service.
- [x] Add delivery manifest model.
- [x] Add unit tests.

## Phase 2: UI MVP

- [x] Add Generic Host startup to WPF.
- [x] Add MVVM Toolkit.
- [x] Add workbench shell.
- [x] Add application layer project.
- [x] Add Chinese and English localization foundation.
- [x] Add language selection to shell.
- [x] Add project repository and application service foundation.
- [x] Add project creation and load/save.
- [x] Add Phase 2 detailed implementation plan.
- [x] Add project create/list UI foundation.
- [x] Add series and item editor foundation.
- [x] Add series and item table.
- [x] Add prompt version editor.
- [x] Add fake planning action.
- [x] Add queue panel.
- [x] Add candidate gallery.
- [x] Add review panel.
- [x] Add delivery export panel.

## Phase 3: OpenAI Integration

- [x] Add OpenAI provider configuration.
- [x] Store secrets outside repo.
- [x] Implement text planning provider.
- [x] Implement image generation provider.
- [x] Implement vision review provider.
- [x] Add provider capability validation.
- [x] Add dry-run and opt-in real API smoke tests.
- [x] Add cost estimate and quota guard.

## Phase 3A: Cloud-First Provider Hardening

- [x] Record cloud-first provider hardening design spec.
- [x] Record cloud-first provider hardening implementation plan.
- [x] Record ADR for cloud-first provider and tooling strategy.
- [x] Refresh research evidence for OpenAI Responses API, Microsoft resilience, OpenTelemetry, and Credential Locker.
- [x] Add Windows Credential Locker or DPAPI secret store adapter.
- [x] Replace environment-variable-only production secret retrieval.
- [x] Add `.env` secret store fallback for local provider credentials.
- [x] Add separated text/image provider environment configuration with image key-pool concurrency validation.
- [x] Enforce role-scoped provider options so image-only keys cannot be used for text or vision calls, while allowing the image provider to fall back to `TEXT_PROVIDER_API_KEY` in the default single-key configuration path.
- [x] Add non-generating `/v1/models` health checks for text providers and image key pools.
- [x] Add Provider Center configuration summary model/view-model without exposing secret values.
- [x] Add Provider Center manual health summary state for mixed text/image key-pool results.
- [x] Extract Provider Center presentation and health-summary composition into `ProviderCenterPresentationCoordinator` while preserving secret redaction and mixed key-pool status display.
- [x] Evaluate and adopt the official OpenAI .NET SDK where the API surface is stable enough.
- [x] Keep raw `HttpClient` fallback only for unsupported or lagging SDK surfaces.
- [x] Add `Microsoft.Extensions.Http.Resilience` to named provider clients.
- [x] Capture request IDs, token usage, latency, and cost telemetry per provider call.
- [x] Add OpenTelemetry instrumentation and a local OTLP/Aspire dashboard profile.
  - [x] Add .NET `ActivitySource` and `Meter` instrumentation for provider calls.
  - [x] Add local OTLP/Aspire dashboard profile.
- [x] Support opt-in Responses API multi-turn image state where the product benefits and where the provider routing policy calls for it.
- Deferred: Add partial-image streaming UX only if a future workbench flow gains clear product value from progressive previews.
- [x] Add a remote workflow-engine adapter boundary without requiring local model installs.
- [x] Run full build, test, and format gates for the implementation slice.

## Phase 4: Quality Loop

- [x] Add review rubric templates.
- [x] Add structured AI review output.
- [x] Add prompt repair suggestions.
- [x] Add prompt diff.
- [x] Add candidate comparison.
- [x] Add batch requeue by reason.
- [x] Add final approval workflow.

## Phase 4A: Deterministic Text Composition And Delivery Assurance

Priority note: this phase is now part of the near-term golden-path hardening slice, not a distant quality add-on.

- [x] Add `SkiaSharp`-based deterministic composition foundation for labels, formulas, legends, and callouts.
- [x] Add deterministic post-render text composition service for educational or text-heavy visuals.
- [x] Add readability, label, and callout-specific review checks.
- [x] Persist human approval decisions and reviewer notes.
- [x] Export final approval state in delivery manifests and review reports.
- [x] Run full build, test, and format gates for the implementation slice.

## Phase 5: Sample Migration

- [x] Import the physics poster project as a sample.
- [x] Map existing prompt files to generic `SeriesItem` and `PromptVersion`.
- [x] Map finalized delivery files to `CandidateImage` and `ReviewResult`.
- [x] Validate manifest compatibility.
- [x] Document migration limits.

## Release Readiness

- [x] Add installer or packaged publish.
- [x] Add diagnostics export.
- [x] Add backup and restore.
- [x] Add accessibility review.
- [x] Add large-gallery performance review.
- [x] Add user guide.

## Phase 6: Advanced Workflows

- [x] Add provider-neutral parameter grid experiments.
- [x] Add reference image sets.
- [x] Document style and parameter governance.
- [x] Record ADR for style and parameter governance.
- [x] Add image type presets.
- [x] Add style guide domain model.
- [x] Add provider-neutral generation recipes.
- [x] Extend provider capabilities for output settings.
- [x] Validate generation recipes before queue execution.
- [x] Link parameter experiments to queue tasks and candidate review.
- [x] Add style library and recipe inspector UI.
- [x] Include style, recipe, reference, and experiment metadata in delivery packages.
- [x] Add mask/edit workflow foundation.
- [x] Add mask/edit UI controls.
- [x] Add workflow export/import.
- [x] Add optional graph view.

## Phase 7: Brief-First Image Generation

- [x] Record brief-first image generation design spec.
- [x] Record brief-first implementation plan.
- [x] Add `CreativeBrief` and `PromptDirection` domain records.
- [x] Persist creative briefs under image series.
- [x] Add fake-first prompt direction planning.
- [x] Add application service workflow to create briefs, generate directions, and promote directions.
- [x] Add minimal Brief tab UI and localization.
- [x] Run full build, test, and format gates for the implementation slice.

## Phase 7A: Preset Governance

- [x] Record preset governance design spec.
- [x] Record preset governance implementation plan.
- [x] Record preset governance evidence table.
- [x] Record preset governance ADR.
- [x] Add governance metadata to image type presets.
- [x] Add structured prompt direction recommendation model.
- [x] Add fake-first recommendation output.
- [x] Persist recommendations through the application service.
- [x] Use prompt direction recommendations as promotion default settings.
- [x] Show prompt direction recommendations in the Brief tab.
- [x] Add catalog invariant tests.
- [x] Run full build, test, and format gates for the preset governance slice.

## Phase 8: Document Illustration Workflow

- [x] Record document illustration workflow design spec.
- [x] Record document illustration implementation plan.
- [x] Add document illustration domain records for source text, target concepts, and target promotion.
- [x] Add article, concept, graphical abstract, and scholarly schematic presets.
- [x] Add document-specific review rubrics.
- [x] Add fake-first target planning and prompt preparation.
- [x] Add application workflow to create illustration briefs, generate targets, approve targets, and promote approved targets into the existing Plan/Prompts workflow.
- [x] Add persistence for document planning evidence.
- [x] Add document illustration UI entry, localization, and draft-mode guidance.
- [x] Add user documentation for document illustration workflow safety boundaries and first-run fake-provider behavior.
- [x] Run full build, test, and format gates for the implementation slice.
- [x] Add real provider integration and support-matrix-approved binary document extraction in later slices.

## Phase 9: Blueprint-First Generalized Series Workflow

- [x] Record blueprint-first generalized series design spec.
- [x] Record blueprint-first implementation plan.
- [x] Add `DesignBlueprint` domain record and persistence.
- [x] Extend fake planning provider with blueprint candidates.
- [x] Add application workflow to create, compare, and promote blueprint routes.
- [x] Add optional `SeriesItemKind` support for panel-like narrative items.
- [x] Expand Brief tab UI with blueprint cards and promotion actions.
- [x] Route review outcomes back to brief, blueprint, prompt, or settings layers.
  - [x] Add provider-neutral review outcome routing model and application service entrypoint.
  - [x] Surface routing decisions in review and repair UI.
  - [x] Apply routed repair actions back to prompt and settings records by creating a new prompt version.
  - [x] Add non-destructive Brief/Blueprint repair patch proposals that require human approval before record mutation.
  - [x] Persist Brief/Blueprint repair patch proposals on the project aggregate and SQLite repository.
  - [x] Include persisted repair patch proposals in diagnostics evidence.
  - [x] Apply routed repair actions back to brief and blueprint records.
- [x] Include blueprint metadata in delivery packages.
- [x] Run full build, test, and format gates for the implementation slice.

## Phase 10: Multimodal Source And Artifact Foundation

- [x] Record ADR for multimodal content delivery, workflow packs, and AI operator boundaries.
- [x] Record multimodal source/artifact implementation plan.
- [x] Add `SourceAsset`, `ExtractedContent`, and `EvidenceAnchor` domain records.
- [x] Add `OutputArtifact`, `ArtifactManifest`, and `ArtifactPackage` domain records.
- [x] Add fake-first source ingestion service with file metadata and text fixtures.
- [x] Add source evidence persistence with backward-compatible project loading.
- [x] Add document extraction provider boundary for PDF, DOCX, PPTX, markdown, image, and OCR results.
- [x] Add artifact planning use case that can plan image, PDF, DOCX, markdown, and review-report outputs from a brief.
- [x] Extend delivery manifest with source evidence and output artifact provenance.
- [x] Keep existing image-series delivery export compatible with the new artifact model.
- [x] Run build, test, and format gates for the implementation slice.

## Phase 11: Workflow, Blueprint, And Industry Pack System

- [x] Add `WorkflowPack`, `BlueprintPack`, `IndustryPack`, `RendererPack`, and `ReviewRubricPack` metadata records.
- [x] Add pack semantic version, compatibility range, deprecation state, and migration notes.
- [x] Add local pack registry and validation service.
- [x] Add built-in generic image-series pack.
- [x] Add built-in article illustration pack.
- [x] Add built-in document review/translation pack.
- [x] Add built-in courseware visual pack.
- [x] Add built-in poster/report delivery pack.
- [x] Add pack import/export with fake execution and validation.
- [x] Add `WorkflowStageDefinition` metadata with stable stage IDs and completion criteria.
- [x] Add pack-driven UI defaults without leaking pack-specific vocabulary into core entities.
- [x] Add validation that packs cannot introduce permanent global tabs without an explicit shell decision.
- [x] Add catalog invariant tests for pack IDs, compatibility, and migrations.
- [x] Run build, test, and format gates for the implementation slice.

## Phase 12: Modular Maintenance And Use Case Split

- [x] Define module folders for source ingestion, artifact planning, pack registry, repair routing, and tool adapters.
  - [x] Guard built-in module folder declarations against stale repository paths.
- [x] Define reusable `WorkflowViewSlot` names for source list, stage workspace, inspector, activity panel, approval panel, and artifact preview.
- [x] Add `FeatureViewModule` contract for WPF view, view model, localization keys, commands, and fake-service tests.
- [x] Split `MainWindowViewModel` by workflow tab or feature module as new slices touch existing UI.
  - [x] Extract project workspace command orchestration into `ProjectWorkspaceCoordinator` while preserving existing bindings and commands.
  - [x] Extract shell-inspector project creation, provider-center, document planning, and image-edit orchestration into `WorkbenchInspectorCoordinator` while preserving existing bindings and inspector behavior.
  - [x] Extract planning and document-planning orchestration into `PlanningWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract brief-tab workflow orchestration into `BriefWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract generation/gallery workflow orchestration into `GenerationWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract review/approval workflow orchestration into `ReviewWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract delivery export orchestration into `DeliveryWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract plan editor command orchestration into `PlanEditorWorkflowCoordinator` while preserving existing bindings and commands.
  - [x] Extract workflow graph row construction into `WorkflowGraphCoordinator` while preserving existing bindings and graph output.
  - [x] Extract workbench projection building into `ProjectWorkbenchProjectionCoordinator` while preserving plan, prompt, gallery, review, and reload output.
  - [x] Extract workbench load/clear state composition into `ProjectWorkbenchStateCoordinator` while preserving plan, prompt, gallery, review, delivery, and active-brief selection behavior.
  - [x] Extract selection-summary display building into `MainWindowSelectionSummaryCoordinator` while preserving item-title, style-recipe, and candidate-summary behavior.
  - [x] Extract current-project summary display building into `MainWindowSelectionSummaryCoordinator` while preserving empty-state and timestamp formatting behavior.
  - [x] Extract document default/strictness localization restoration into `MainWindowLocalizationCoordinator` while preserving user-entered text and educational fallback behavior.
  - [x] Extract shell localization payload building into `MainWindowLocalizationCoordinator` while preserving language-switch behavior and selected-option restoration.
  - [x] Extract localized selection and option restoration into `MainWindowLocalizationCoordinator` while preserving language-switch behavior and current inspector selections.
- [x] Split large WPF views into feature-owned user controls where needed.
  - [x] Extract the brief-tab blueprint list into `BlueprintRoutesView` while preserving existing bindings and selection behavior.
  - [x] Extract the brief-tab prompt-direction list into `PromptDirectionsView` while preserving existing bindings and selection behavior.
  - [x] Extract the review-tab results list into `ReviewResultsListView` while preserving existing bindings and selection behavior.
  - [x] Extract the delivery-tab results list into `DeliveryResultsListView` while preserving existing bindings and output display behavior.
  - [x] Extract the brief-tab actions bar into `BriefWorkflowActionsView` while preserving create, blueprint, and prompt-direction command bindings.
  - [x] Extract the brief-tab blueprint panel into `BlueprintRoutesPanelView` while preserving section header and blueprint-list composition.
  - [x] Extract the brief-tab prompt-directions panel into `PromptDirectionsPanelView` while preserving section header and prompt-direction-list composition.
  - [x] Extract the review-tab header into `ReviewHeaderView` while preserving review column bindings.
  - [x] Extract the delivery-tab header into `DeliveryHeaderView` while preserving delivery column bindings.
  - [x] Extract the plan-tab header into `PlanHeaderView` while preserving plan column bindings.
  - [x] Extract the prompts-tab header into `PromptsHeaderView` while preserving prompt column bindings.
  - [x] Extract the queue-tab header into `QueueHeaderView` while preserving queue column bindings.
  - [x] Extract the gallery-tab header into `GalleryHeaderView` while preserving gallery column bindings.
  - [x] Extract the workflow-graph header into `WorkflowGraphHeaderView` while preserving graph column bindings.
  - [x] Extract the inspector provider-center panel into `ProviderCenterPanelView` while preserving configuration summary, health rows, and refresh/test bindings.
  - [x] Extract the inspector project setup panel into `ProjectSetupPanelView` while preserving project creation, current-project summary, and project-list selection bindings.
  - [x] Extract the inspector style-recipe panel into `StyleRecipeInspectorPanelView` while preserving preset, guide, recipe selection, and summary bindings.
  - [x] Extract the inspector fake-planning panel into `FakePlanningPanelView` while preserving planning input and run-command bindings.
  - [x] Extract the inspector document-illustration panel into `DocumentIllustrationPanelView` while preserving source text, strictness, run-command, and result bindings.
  - [x] Extract the plan-tab rows list into `PlanRowsListView` while preserving plan row visibility and row bindings.
  - [x] Extract the prompts-tab rows list into `PromptRowsListView` while preserving prompt row visibility and row bindings.
  - [x] Extract the queue-tab rows list into `QueueRowsListView` while preserving queue row visibility and row bindings.
  - [x] Extract the gallery-tab rows list into `GalleryRowsListView` while preserving gallery row visibility and selected-row bindings.
  - [x] Extract the workflow-graph rows list into `WorkflowGraphRowsListView` while preserving graph row visibility and row bindings.
  - [x] Extract the workflow graph tab content into `WorkflowGraphView` while preserving existing bindings and graph output.
  - [x] Extract the delivery tab content into `DeliveryView` while preserving existing bindings and delivery output.
  - [x] Extract the review tab content into `ReviewView` while preserving existing bindings and review output.
  - [x] Extract the queue tab content into `QueueView` while preserving existing bindings and queue output.
  - [x] Extract the gallery tab content into `GalleryView` while preserving existing bindings and gallery selection output.
  - [x] Extract the inspector side panel into `WorkbenchInspectorView` while preserving provider center, project setup, planning, document illustration, and review-approval bindings.
  - [x] Extract the workspace navigation column into `WorkspaceNavigationView` while preserving localized shell labels and navigation rows.
  - [x] Extract the bottom activity footer into `ActivityPanelView` while preserving activity summaries and shell layout behavior.
  - [x] Extract the central tab host into `WorkbenchTabHostView` while preserving workflow view placement, empty-state rules, and tab binding behavior.
- [x] Split `ProjectApplicationService` into focused use-case services for sources, briefs, blueprints, queue, review/repair, operator, and delivery.
  - [x] Extract project create/load/list workflow methods into `ProjectWorkspaceApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract series, item, prompt, and fake planning workflow methods into `SeriesWorkflowApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract review/repair routing and Prompt/Settings repair application into `ReviewRepairApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract delivery export into `DeliveryApplicationService` while preserving the existing facade entrypoint.
  - [x] Extract document illustration planning into `DocumentIllustrationApplicationService` while preserving the existing facade entrypoint.
  - [x] Extract brief, prompt-direction, and design-blueprint workflow methods into `BriefWorkflowApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract generation queue and fake image-edit workflow methods into `GenerationWorkflowApplicationService` while preserving the existing facade entrypoints.
  - [x] Extract fake vision review and final approval workflow methods into `ReviewWorkflowApplicationService` while preserving the existing facade entrypoints.
- [x] Move provider configuration and capability validation out of UI-facing view models.
- [x] Move persistence configuration into infrastructure-owned modules.
  - [x] Move `RoutedRepairPatch` persistence mapping into an infrastructure configuration class.
  - [x] Move `CreativeBrief` persistence mapping into an infrastructure configuration class.
  - [x] Move `DocumentBrief` and `IllustrationPlan` persistence mappings into infrastructure configuration classes.
  - [x] Move `SourceAsset`, `OutputArtifact`, and `ArtifactPackage` persistence mappings into infrastructure configuration classes.
  - [x] Move `ReviewRubric` and `ReviewResult` persistence mappings into infrastructure configuration classes.
  - [x] Move project, series, item, prompt, generation, candidate, delivery, and provider mappings into infrastructure configuration classes.
- [x] Split EF Core mappings into `IEntityTypeConfiguration<T>` as model count grows.
  - [x] Extract the first `IEntityTypeConfiguration<T>` slice for `RoutedRepairPatch`.
  - [x] Extract the `CreativeBrief` `IEntityTypeConfiguration<T>` slice while preserving prompt direction and blueprint JSON reload behavior.
  - [x] Extract document illustration, source asset, and artifact packaging `IEntityTypeConfiguration<T>` slices with focused SQLite reload tests.
  - [x] Add a focused SQLite reload test before extracting quality-loop review rubric/result mappings.
  - [x] Remove inline `modelBuilder.Entity<T>` mapping blocks from `AppDbContext`.
- [x] Add focused tests for each extracted use-case service before expanding UI surface.
  - [x] Cover `ProjectWorkspaceApplicationService` directly while keeping facade workflow tests.
  - [x] Cover `SeriesWorkflowApplicationService` directly while keeping facade workflow tests.
  - [x] Add focused delivery application service tests for registered and missing writer paths.
  - [x] Cover `DocumentIllustrationApplicationService` directly while keeping facade workflow tests.
  - [x] Cover `BriefWorkflowApplicationService` directly while keeping facade workflow tests.
  - [x] Cover `GenerationWorkflowApplicationService` directly while keeping facade workflow tests.
  - [x] Cover `ReviewWorkflowApplicationService` directly while keeping facade workflow tests.
- [x] Keep each refactor slice behavior-preserving and tied to a new feature or touched old logic.
- [x] Run build, test, and format gates after each module split.

## Phase 13: Review, Repair, And Operator Automation

- [x] Add structured `RepairPlan` model from `ReviewResult` findings.
- [x] Add `OperatorAction` and `OperatorRun` audit records.
- [x] Add tool adapter contract with risk level, dry-run support, inputs, outputs, side effects, timeout, approval requirement, and cleanup path.
- [x] Add SDK/CLI/local library adapter boundary for deterministic tools.
- [x] Add browser automation adapter boundary for web workflows.
- [x] Add Windows desktop automation adapter boundary for future UI automation.
- [x] Add computer-use provider boundary for model-guided UI action planning.
- [x] Add local tool registry for extraction, conversion, OCR, ImageMagick/FFmpeg processing, deterministic composition, and artifact validation.
- [x] Add approval gate for medium/high-risk operator actions.
- [x] Add low-risk auto-repair path for safe local validation or file-generation tasks.
- [x] Add operator audit export into diagnostics and delivery evidence where appropriate.
- [x] Run the first real low-risk operator adapter end-to-end with audit evidence and rollback notes.
  - [x] Recommended first slice: local delivery or artifact validation report generation into a new diagnostics folder.
- [x] Run build, test, and format gates for the implementation slice.

## Phase 14: Product Identity And Repository Rename

- [x] Record ADR for `AI Content Delivery Studio` product identity and staged rename path.
- [x] Update product-facing README, product design, architecture, roadmap, and user guide naming.
- [x] Update WPF app title localization to `AI Content Delivery Studio` / `AI 内容交付工作台`.
- [x] Post-V1: rename local root directory from `D:\CODE\ai-image-series-studio` to `D:\CODE\ai-content-delivery-studio` after confirming a clean worktree and no active tools depend on the old path.
- [x] Post-V1: reopen the workspace from `D:\CODE\ai-content-delivery-studio` and verify `git status --short`.
- [x] Post-V1: rename solution, project folders, project names, assemblies, namespaces, tests, scripts, and publish output from `ImageSeriesStudio.*` to `ContentDeliveryStudio.*`.
- [x] Post-V1: preserve compatibility notes for existing workspaces, diagnostics packages, and historical documents that still mention `ImageSeriesStudio`.
- [x] Post-V1: run full rename gate: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, and a targeted search for unintended old-name references.
