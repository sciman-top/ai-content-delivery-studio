# Scientific Document Extraction Evidence

Date: 2026-07-26

## Scope

This evidence records Task 10 of the trustworthy scientific-figure workflow.
The slice adds:

- an application-owned `IScientificDocumentExtractor` contract
- a local `PdfPigScientificDocumentExtractor`
- text-bearing PDF, Markdown, plain-text, and paste support
- page and character-range provenance
- explicit Markdown heading, paragraph, formula, and table blocks
- structured blocking for unsupported OCR, corrupted reading order, missing
  required formulas/tables, and documents with no usable text
- an `UnrecoverableRegion` block kind for located missing source regions

It does not add OCR, DOCX scientific extraction, provider understanding,
automatic Gate 1 approval, rendering, review, UI, or live-provider calls.

## Trust Boundary

- PDF text comes only from the existing local PdfPig dependency.
- Markdown formulas are recovered only from explicit `$$ ... $$` or
  `FORMULA:` blocks.
- Markdown tables require pipe-delimited rows and an explicit separator row.
- Plain-language statements such as "force equals mass times acceleration"
  are not promoted to recovered formulas.
- A required formula/table with no explicit recovered block is persisted as
  `Missing`, with no `OriginalText`, and blocks readiness.
- OCR requests produce an `ocr-not-supported` blocking diagnostic. Any
  available text layer remains inspectable but cannot make the result ready.
- Corrupted reading order produces both a quality-state block and a structured
  diagnostic.
- An image-only/empty PDF produces a located `UnrecoverableRegion`; generated
  placeholder text is never inserted into the source record.

## Test-First Evidence

The initial focused run failed because the Task 10 application contract and
infrastructure namespace did not exist. The first implementation then exposed
that a missing formula block still requires a source location. Missing
required content now reuses the exact searched source-block location, while a
fully unrecoverable page uses a normalized whole-page region.

Focused command:

`dotnet test ContentDeliveryStudio.sln --filter "ScientificDocumentExtractionTests|DocumentExtractionProviderTests" --no-restore`

Focused result before final repository closeout: exit `0`, `17 / 17` passed.

## Compatibility And N/A

- Existing generic `IDocumentExtractionProvider`: unchanged; its PDF/DOCX
  workflows and tests remain compatible.
- Runtime dependency/supply-chain change: `gate_na`; the adapter reuses the
  existing pinned PdfPig package.
- Persistence/schema change: `gate_na`; the Task 9 payload already persists
  the Task 6 extraction model, including the additive enum value.
- Paid/live provider: `gate_na`; local deterministic extraction needs no
  provider call.
- OCR acceptance: `gate_na`; OCR is deliberately unsupported and fail-closed.
  Recovery condition: a future OCR task must add primary-source dependency
  evidence, confidence policy, fixtures, and human acceptance.

## Rollback

Revert the Task 10 commit to remove the application contract, local adapter,
`UnrecoverableRegion`, focused tests, glossary additions, evidence, and Task 10
status updates. Task 9 persistence remains independently valid; payloads that
already contain `UnrecoverableRegion` must be exported or retained before
running an older binary that does not know that enum value.

## Repository Gates

Final closeout uses the fixed repository order:

1. `dotnet build ContentDeliveryStudio.sln`
2. `dotnet test ContentDeliveryStudio.sln --no-build`
3. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1`
4. `dotnet format ContentDeliveryStudio.sln --verify-no-changes --no-restore`
5. `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/preflight-release.ps1 -NoRestore`

Fresh final outcomes:

- build: exit `0`, 0 warnings, 0 errors
- full test: exit `0`, `535 / 535` passed, 0 failed, 0 skipped
- reference evidence: exit `0`; `scientific-figure-workflow` detected the
  repository-owned plan evidence
- format verification: exit `0`
- release preflight: exit `0`; canonical repository verification, publish
  WhatIf, placeholder/conflict scans, and diff hygiene passed
