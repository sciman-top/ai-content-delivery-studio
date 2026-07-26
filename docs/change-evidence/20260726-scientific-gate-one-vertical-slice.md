# Scientific Gate One Vertical Slice Evidence

Date: 2026-07-26

## Scope

This evidence records Task 11 and the automated portion of Checkpoint 1 for the
trustworthy scientific-figure workflow. The slice adds:

- provider-neutral scientific understanding chunk contracts
- a bounded block/character chunk policy
- a deterministic fake provider with exact source-block quotations
- application-owned evidence reconstruction and claim validation
- deterministic cross-chunk merge-conflict records
- minimum evidence-bound Figure Spec compilation
- explicit affirmative human Gate 1 decisions
- draft and approved aggregate persistence through the Task 9 repository
- fake-first desktop dependency registration
- an end-to-end redistribution-safe PDF to reviewable Gate 1 draft test

It does not add a real understanding provider, automatic conflict resolution,
render plans, SVG, Gate 2, UI, or live-provider acceptance.

## Authority And Merge Rules

- Providers return drafts referencing existing block IDs and verbatim quotes.
- The application reconstructs `ClaimEvidenceLink`; an unknown block or
  non-verbatim quote fails before persistence.
- Only accepted claims with validated supporting evidence become Figure Spec
  elements.
- Block identity remains stable across chunks. The default policy bounds calls
  to 8 blocks and 12,000 characters, with an indivisible source block retained
  as one evidence unit.
- Equal merge keys with incompatible statements create an unresolved
  `ScientificClaimConflict`; the understanding and Figure Spec become blocked.
- Draft creation persists `FigureSpecDraft` and never calls Gate 1 approval.
- `Approved=false` is not approval. Only a separate affirmative human decision
  can freeze the current understanding and spec versions.

## Checkpoint 1 Evidence

- A local redistribution-safe PDF fixture flows through PdfPig extraction,
  deterministic fake understanding, evidence reconstruction, and Figure Spec
  compilation to `ReadyForGate1`.
- Task 10 tests prove unsupported OCR, missing content, corrupted order, and
  empty PDF regions fail closed.
- Task 11 conflict tests prove unresolved science blocks Gate 1.
- SQLite workflow tests save and reload both the unapproved draft and approved
  reviewer/version snapshot.

## Test-First Evidence

The first focused Task 11 run compiled only after the provider-neutral
contracts, fake provider, repository interface, and application service were
implemented. Code review then restricted spec compilation to accepted claims
with validated support and added the PDF-to-draft checkpoint integration test.

Focused command:

`dotnet test ContentDeliveryStudio.sln --filter "ScientificUnderstandingProviderTests|ScientificGateOneWorkflowTests|ScientificDocumentExtractionTests" --no-restore`

Focused result before final repository closeout: exit `0`, `12 / 12` passed.

## Compatibility And N/A

- Existing provider contracts: unchanged; scientific understanding uses a
  separate provider-neutral interface.
- Existing Task 9 repository: compatible; it now implements the application
  interface without changing storage.
- Runtime dependency/supply-chain change: `gate_na`; no package was added.
- Paid/live provider: `gate_na`; fake-first Task 11 makes no network call.
- Human scientific acceptance: `gate_na`; tests exercise an explicit human
  decision record, not a real reviewer judgment. Recovery condition: later
  corpus/live acceptance checkpoints must capture named human review.

## Rollback

Revert the Task 11 commit to remove the understanding contracts, fake provider,
application service, DI registration, tests, glossary/evidence, and status
updates. Task 9-10 persisted records remain readable; no schema rollback is
required.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `541 / 541` passed, 0 failed, 0 skipped
- reference evidence: exit `0`; host/observability,
  `persistence-and-schema`, and `scientific-figure-workflow` detected plan
  evidence
- format verification: exit `0`
- release preflight: exit `0`; canonical repository verification, publish
  WhatIf, placeholder/conflict scans, and diff hygiene passed
