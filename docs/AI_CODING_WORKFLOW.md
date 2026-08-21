# AI Coding Workflow

## Purpose

This is the repository's lightweight default for AI-assisted implementation. It keeps the product's real safety boundaries while avoiding duplicate plans, status ledgers, evidence receipts, and repeated verification.

## Truth And Scope

Read only the surfaces needed for the current change:

1. the affected code and tests
2. [TASKS.md](./TASKS.md) for remaining work
3. the relevant product or architecture document when a contract is unclear
4. [REFERENCE_BASIS.md](./REFERENCE_BASIS.md) only for a mapped high-risk seam

Before editing, state the goal, non-goals, write set, cheapest sufficient verification, and stop condition in the task conversation. A separate repository spec or plan is not required for ordinary implementation.

## Default Loop

1. Reproduce or inspect the current behavior.
2. Change the smallest causal seam.
3. Add or update behavior coverage only when it protects a real failure mode or contract.
4. Run the focused affected check once.
5. Run one proportional closeout lane after the inputs are frozen.
6. Update an existing durable document only when its contract or current state changed.

Do not create a new abstraction, compatibility layer, evidence file, spec, plan, queue entry, or governance check merely to document the work being done.

## Verification Lanes

Use the lowest lane that proves the claim.

### Focused / Quick

```powershell
.\scripts\verify-repo.ps1 -Mode Quick -TestFilter <focused-filter> -NoRestore
```

Quick performs one solution build and one explicitly filtered test run. It skips reference governance, release-only tests, formatting, scans, and packaging. For a smaller inner loop, invoking the affected test project or verifier directly is preferred when a solution build adds no useful signal.

### Full

```powershell
.\scripts\verify-repo.ps1 -Mode Full
```

Full performs exactly:

```text
build -> tests where Category!=ReleaseOnly, Category!=AcceptanceOnly, and Category!=LiveProvider -> reference contract -> diff hygiene
```

It does not run release-only tests, a whole-solution formatter, packaging, the retired product-focus queue, or a second copy of any prior step.

### Release

```powershell
.\scripts\preflight-release.ps1
```

Release invokes Full once, then adds exactly one `Category=ReleaseOnly` test pass, changed-C# formatting (or full formatting only when formatter configuration changed), placeholder/conflict scans, publish/package verification, and the diff result already established by Full. Core, release-only, and explicit acceptance tests are disjoint; reference governance is not repeated. `AcceptanceOnly` lanes are run only by their named acceptance harnesses.

Pull requests and normal pushes use Full. Release preflight is an explicit, confirmation-gated `workflow_dispatch` action so ordinary commits and accidental manual runs do not publish a Windows package.

## Tests

Prefer tests that observe domain behavior, persisted state, provider requests, package contents, public ViewModel behavior, parsed semantic structure, or native UI Automation.

Delete or avoid tests that merely assert:

- exact source-token counts
- file placement with no external contract
- private call ordering already covered by outcomes
- duplicate wrapper behavior
- historical implementation shape

Do not broaden from a focused test to Full or Release unless the change crosses a shared/high-risk seam or the repository closeout contract requires it.

## Durable Documents

Create or update a durable record only for one of these cases:

- an ADR for an irreversible or long-lived architectural decision
- migration and rollback instructions for persistence/schema changes
- structured reference evidence for a mapped seam only when external source actually adjudicates the decision and `-RequireDecision` is selected
- security, paid/live-provider, manual/hardware, or release acceptance that cannot be inferred from tests
- a product contract or operator instruction that users need after the change

Routine refactors, bug fixes, test updates, and documentation corrections do not require a spec-plan-evidence trio. Completed work belongs in Git history; [TASKS.md](./TASKS.md) should contain only work that remains or an external acceptance blocker.

## Parallel Work

Default to one executor. Use a subagent or worktree only when there are at least two independently verifiable slices with non-overlapping write sets and the isolation has clear net value. Parallelism does not add extra plans, evidence, or repeated gates.

## Safety Boundaries

Keep these strict even when the workflow is lean:

- fake providers before live providers
- explicit authorization for paid calls and external publication
- secret isolation
- persistence migration and rollback compatibility
- human approval before final delivery
- delivery manifest, hash, and path containment
- scientific claim/evidence and deterministic render contracts

Repository verification proves repository behavior only. It does not prove host loading, paid/live-provider behavior, manual accessibility, hardware behavior, publication, or external acceptance.
