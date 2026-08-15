# Post-V1 Product Focus PRD

**Status:** Durable product direction; the repository execution queue is retired
**Date:** 2026-08-02
**Supersedes:** No historical release document. [PRD_V1.md](./PRD_V1.md) remains the locked V1 promise.

## 1. Document Role

This PRD narrows the post-V1 product to the capabilities that create direct user value. It exists because the repository has accumulated mature planning, governance, pack, module, operator, and remote-workflow contracts while several ordinary image-production capabilities remain fake-first or contract-only.

This PRD records the durable two-lane product boundary and the criteria for excluding platform work. It is not a task queue or completion ledger. Current external blockers live in [TASKS.md](./TASKS.md). It does not relabel the historical V1 live snapshot, reopen accepted scientific-figure evidence, authorize paid-provider calls, or claim that every long-term roadmap phase is production-ready.

## 2. Product Thesis

AI Content Delivery Studio has two formal production lanes:

1. **Controlled image-series production:** requirement to coherent image series, with explicit cost authority, reference-guided generation or editing where supported, structured review, human approval, and immutable delivery.
2. **Trustworthy scientific figures:** source evidence to approved scientific specification, deterministic SVG authority, independent review, two human gates, and evidence-backed delivery.

Document illustration is an input adapter into one of these lanes. Courseware, poster, report, article, and social visuals are scenario profiles or presets, not separate platforms. A delivery category is an output-routing label, not proof that the named scenario has a production-ready workflow.

## 3. Decision Drivers

- User-visible completion matters more than adding another extension point.
- Real-provider safety must include explicit authority, bounded cost, durable receipts, and no automatic replay.
- Scientific correctness requires stronger gates than ordinary visual production.
- WPF workflow speed, recovery, Chinese file handling, accessibility, and export reliability matter more than decorative or graph-style UI novelty.
- A contract or fake implementation must not be marketed as a live capability.
- New abstractions require two real consumers or one unavoidable external integration; otherwise use a concrete implementation.
- Repository governance must improve decision quality without requiring ceremonial document edits for low-risk local changes.

## 4. Target Users And Jobs

### 4.1 Primary users

- Solo creator producing a coherent multi-image campaign or article package.
- Teacher or courseware author producing readable educational visuals.
- Scientific author or reviewer producing evidence-grounded concept, mechanism, comparison, or graphical-abstract figures.
- Power user who needs local files, explicit provider control, reproducible approvals, and defensible delivery evidence.

### 4.2 Primary jobs

- Compare visual directions before spending on generation.
- Run a controlled real-provider batch without accidental replay or hidden cost.
- Maintain subject, style, and brand continuity through reference-guided operations.
- Route defects to brief, blueprint, prompt, settings, composition, source evidence, or scientific specification.
- Recover work after interruption and export only approved assets.
- Explain where every scientific claim, label, relation, formula, and unit came from.

## 5. Capability Maturity Vocabulary

Every product, roadmap, task, and evidence statement must use one of these maturity states:

| State | Meaning |
| --- | --- |
| `production-proven` | A user-visible path has bounded real-provider or deterministic execution, manual acceptance where required, delivery evidence, and fresh repository gates. |
| `repo-verified` | The implementation and automated contracts pass locally, but a manual, hardware, paid-provider, or field condition remains open. |
| `experimental-contract` | Domain contracts, fake implementations, fixtures, or internal tools exist, but ordinary users cannot rely on a complete real path. |
| `frozen` | Existing compatibility is preserved, but no new feature work is allowed without a new evidence-backed decision. |
| `excluded` | The capability is outside the current product boundary and must fail or be described clearly rather than produce a plausible substitute. |

Terms such as `supported`, `complete`, and `ready` must be qualified by one of these states or by an equally precise evidence boundary.

## 6. Product Scope

### 6.1 Lane A: controlled image-series production

The product must support:

- requirement, audience, constraints, references, and quality criteria;
- brief and blueprint comparison before paid generation;
- editable series plan and prompt history;
- durable queue preparation, pause, resume, reorder, retry, interruption recovery, and provenance;
- explicit real-provider execution receipt containing provider profile, operation count, cost estimate, approval actor, approval timestamp, and immutable request identity;
- no background dispatch, no automatic replay, and no provider call from preparation or local queue mutations;
- provider-capability-aware reference image and image editing operations;
- candidate comparison, structured review, human final approval, and immutable categorized delivery;
- deterministic text composition for exact labels, formulas, legends, and callouts.

### 6.2 Lane B: trustworthy scientific figures

The product must preserve:

- text-bearing source extraction with explicit recovery quality and blocked states;
- claim-to-evidence and element-to-evidence provenance;
- human Gate 1 before scientific specification authority;
- deterministic SVG as the editable scientific source of truth;
- PNG and PDF derived from the exact approved SVG authority;
- deterministic contract review plus independent semantic and full-resolution visual review;
- presentation-only bounded repair without silent scientific changes;
- human Gate 2 and immutable evidence-backed delivery;
- explicit rejection of unsupported OCR-heavy, observed-image, fabricated-data, or ambiguous scientific requests.

### 6.3 Shared product requirements

- Local-first Windows WPF operation remains usable with fake providers and without cloud access.
- Paid-provider use is opt-in and authority-scoped.
- Secrets, prompts, source content, generated assets, local SQLite, workspace data, diagnostics, and delivery output stay outside Git.
- Existing workspaces and persisted records remain readable through additive migration or explicit compatibility adapters.
- Chinese paths, spaces, long paths within supported Windows limits, locked files, removable media failures, and separate delivery roots produce bounded errors.
- Core workflows expose stable AutomationIds, accessible names, keyboard navigation, and status announcements.
- Crash or process interruption never silently replays paid work.

## 7. Frozen Capabilities

The following capabilities are frozen until a new PRD or ADR records a real consumer, acceptance route, and maintenance owner:

- remote workflow-engine integration;
- public pack marketplace or end-user pack ecosystem;
- general browser, desktop, or computer-use operator platform;
- additional provider abstraction layers without a second real provider;
- graph editor or node-authoring product surface;
- partial-image streaming;
- broad multimodal office publishing beyond a bounded scenario;
- further coordinator extraction that does not transfer state ownership out of `MainWindowViewModel`.

Frozen means: preserve persisted compatibility when it exists, do not expand, and allow removal of unused runtime registration or repository-structure metadata after reference and migration review.

## 8. Explicit Exclusions

- Fabricated experimental data, curves, axes, observations, microscope images, medical images, or measurements.
- Generated visuals represented as real observed evidence.
- Silent automatic changes to scientific claims, relations, formulas, values, units, conditions, or conclusions.
- Unattended paid generation, automatic retry after process restart, or hidden provider failover with new cost.
- Cloud collaboration, assignment, multi-user editing, and cloud synchronization.
- Claims of signed installation, Store distribution, or automatic updates until those artifacts are implemented and verified.

## 9. Functional Requirements

### 9.1 Real queue authority

- `FR-QUEUE-001`: Prepare persists ordered tasks and performs zero provider calls.
- `FR-QUEUE-002`: Local pause, resume, reorder, cancel, and retry mutations perform zero provider calls.
- `FR-QUEUE-003`: Execute requires an unexpired approval receipt whose request identity matches the exact queued batch.
- `FR-QUEUE-004`: The receipt records estimated call count and configured cost estimate before execution.
- `FR-QUEUE-005`: A changed prompt, provider, model, size, quality, item set, or retry plan invalidates the receipt.
- `FR-QUEUE-006`: Interrupted `Running` work fails closed and is never replayed automatically.
- `FR-QUEUE-007`: Provider results and failures retain task, attempt, request, provider, latency, usage, and cost provenance without storing secrets or response bodies in diagnostics.

### 9.2 Reference-guided generation and editing

- `FR-EDIT-001`: The UI exposes reference or editing operations only when the active provider advertises the required capability.
- `FR-EDIT-002`: Reference assets have explicit roles such as subject, style, layout, palette, or mask.
- `FR-EDIT-003`: Unsupported provider operations fail before dispatch with a user-visible capability explanation.
- `FR-EDIT-004`: Edited outputs are new candidates and never overwrite the source candidate.
- `FR-EDIT-005`: Delivery records the source candidate, references, mask, edit instruction, provider operation, and human approval.

### 9.3 WPF workflow ownership

- `FR-WPF-001`: The main window owns only navigation, global project selection, localization, and shared status.
- `FR-WPF-002`: Image-series and scientific-figure workspaces own their state, commands, selection, validation, and recovery independently.
- `FR-WPF-003`: Adding a field to one workflow must not require adding unrelated pass-through state to the main window.
- `FR-WPF-004`: Critical operator flows have native UI Automation probes; exact XAML token counts are not acceptance evidence.

### 9.4 Scientific source and data products

- `FR-SCI-001`: Multi-column order, caption, formula, table, and citation recovery status is visible and evidence-linked when available.
- `FR-SCI-002`: A required but unrecovered source structure blocks Gate 1.
- `FR-SCI-003`: Real data charts accept an explicit structured data source and deterministic chart specification; they never infer or invent measurements.
- `FR-SCI-004`: Chart axes, units, transforms, filters, aggregation, legend, and source hash are reviewable before export.

## 10. Non-Functional Requirements

- `NFR-SAFETY-001`: Fake-first remains the default and paid calls require current explicit authority.
- `NFR-COMPAT-001`: Schema changes are additive or carry migration, rollback, and old-workspace reload tests.
- `NFR-PERF-001`: The 1,000-row repository budgets remain regression guards; subjective WPF responsiveness requires a native operator probe.
- `NFR-ACCESS-001`: Narrator, high contrast, non-default DPI, keyboard, touch/pen where relevant, and focus recovery have explicit repo or manual evidence boundaries.
- `NFR-RECOVERY-001`: Crash recovery preserves local project state without replaying provider work.
- `NFR-OBS-001`: Diagnostics remain local, bounded, redaction-first, and behavior-neutral.
- `NFR-GOV-001`: A routine low-risk change should not require a new spec, plan, and evidence file unless it changes a high-risk contract or release claim.
- `NFR-GOV-002`: Current execution state has one machine-readable truth source; narrative documents link to it instead of restating mutable counts.
- `NFR-TEST-001`: Tests prefer behavior, persistence, provider contracts, UI Automation, and delivery evidence over exact source-text structure.

## 11. Success Metrics

### 11.1 Image-series lane

- A representative 10-item batch can be prepared, cost-reviewed, explicitly approved, executed, paused between dispatches, reviewed, and delivered without automatic replay.
- At least one real reference-guided or editing operation completes with provenance and human approval.
- No secret, prompt body, provider response body, or generated binary enters Git or redacted diagnostics.
- A native packaged WPF probe completes the primary operator flow using stable AutomationIds.

### 11.2 Scientific lane

- The accepted scientific corpus and blocking mutations remain green.
- The active six-item article sample receives physics-expert Gate 1 before any new live acceptance claim.
- Any paid live refresh is separately authorized and records exact request checkpoint identity.
- A structured-source extraction benchmark covers reading order, captions, formulas, tables, and fail-closed recovery states.

### 11.3 Architecture and governance

- No unused remote-workflow runtime registration remains unless a real consumer is identified.
- Pack and module abstractions are either consumed by a real user-visible route or reduced to the minimum compatibility surface.
- Main-window state ownership decreases measurably when a workflow slice is touched; file-count growth alone is not accepted as modularity.
- Repository quick, full, and release gates have distinct purposes and documented expected duration.
- Source-text structural tests are reduced where equivalent behavior or UIA coverage exists.

## 12. Release And Completion Boundaries

- `repo-side complete` means implementation, automated contracts, migration, rollback, docs, and repository gates pass.
- `manual accepted` additionally requires the named human or authorized operator to complete the visible workflow and preserve evidence.
- `live accepted` additionally requires current paid-provider authority and accepted real-provider artifacts.
- `hardware accepted` additionally requires the named device, DPI, accessibility, memory, touch, or pen condition.
- One boundary never implies another.

## 13. AI Execution Contract

An AI selecting work must:

1. Inspect the affected code, tests, and current external blockers before editing.
2. Respect authority; repo work cannot consume paid, human, or hardware authority.
3. Freeze the smallest causal write set and expand it only for an observed current failure.
4. Run one focused affected check and one proportional closeout lane.
5. Preserve user-owned dirty files and never treat a passing repository gate as live, manual, or hardware acceptance.
6. Avoid reviving a retired platform or frozen capability without a new approved PRD or ADR and two real consumers.

## 14. Rollback

Implementation changes must keep slice-local source and migration rollback clear. Git rollback never substitutes for restoring local SQLite, workspace, delivery, or provider state.
