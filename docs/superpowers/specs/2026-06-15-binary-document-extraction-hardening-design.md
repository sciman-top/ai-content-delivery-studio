# Binary Document Extraction Hardening Design

## Goal

Add the first support-matrix-approved binary document extraction hardening slice for local `pdf` and `docx` inputs without widening V1 into broad high-fidelity document automation.

This slice should improve the repository's extraction boundary from fake-only binary fixtures to deterministic local extraction for narrow, reviewable text-planning evidence.

## Why Now

- `docs/TASKS.md` still lists targeted binary extraction hardening as the next active follow-through queue item.
- `docs/SOURCE_ARTIFACT_SUPPORT_MATRIX.md` already treats `pdf` and `docx` as post-V1 hardening candidates with an existing contract boundary.
- The repository now has real-provider document illustration planning, so the next useful narrow upgrade is better local source extraction rather than another planning-only contract.

## Scope

This slice covers:

- local `pdf` extraction for document body text and page-level location hints
- local `docx` extraction for document body text and paragraph-level location hints
- deterministic conversion of extracted binary content into the existing `DocumentExtractionResult` contract
- fake-provider fallback retained for unsupported kinds and test fixtures
- explicit refusal of OCR, citation-span extraction, tables, formulas, and image-heavy binary fidelity

## Non-Goals

This slice does not include:

- OCR introduction
- scanned-image PDF support
- citation span extraction
- figure/image extraction from scholarly PDFs
- table recovery or formula layout fidelity
- PPTX/XLSX extraction
- export back into source documents

## Support-Matrix Alignment

The support-matrix truth remains:

- `pdf` and `docx` are not V1 launch blockers
- binary extraction remains narrower than broad office automation
- OCR-heavy paths stay out of scope

This slice only upgrades the local extraction boundary enough to support evidence-backed planning on text-bearing `pdf` and `docx` files.

## Reference Basis

This slice falls under `document-extraction-and-ocr` in `docs/REFERENCE_BASIS.md`.

Required references for implementation:

- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\markitdown`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\docling`
- `D:\CODE\external\ai-content-delivery-studio-references\05-document-rendering\PdfPig`
- `docs/research/REFERENCE_RESEARCH.md`

## Design

### Extraction Strategy

- Keep `FakeDocumentExtractionProvider` unchanged as the deterministic fallback.
- Add one real local extraction provider for text-bearing `pdf` and `docx` inputs.
- Prefer narrow extraction:
  - `pdf`: concatenate text per page, preserve page-number hints
  - `docx`: concatenate text from body paragraphs, preserve paragraph-order hints
- If a file produces too little usable text, fail with a structured extraction error rather than inventing OCR or semantic recovery.

### Provider Contract

Keep the current `IDocumentExtractionProvider` and `DocumentExtractionResult` contract.

The new provider should:

- declare support for `Pdf` and `Docx`
- report `SupportsOcr: false`
- return one or more `ExtractedContentDraft` records plus evidence anchors
- avoid leaking parser-specific objects beyond infrastructure

### Evidence Shape

For `pdf`:

- `ExtractedContentKind.PlainText`
- `LocationHint` values like `file.pdf: page 1`
- one evidence anchor per page or per first extracted segment

For `docx`:

- `ExtractedContentKind.PlainText`
- `LocationHint` values like `file.docx: paragraph 1`
- one evidence anchor per paragraph group or first extracted segment

### Failure Rules

Fail closed when:

- file extension/kind is unsupported
- extracted text is empty after normalization
- a file appears image-only or OCR-dependent
- parser exceptions occur

Error text should tell the operator that OCR/high-fidelity extraction is outside the current slice.

## Implementation Boundaries

- Infrastructure owns parser usage and file-format details.
- Application contracts stay provider-neutral.
- No UI changes are required in this slice.
- No persistence-model changes are required unless existing extracted-content storage needs a focused compatibility update.

## Testing

Add focused tests for:

- `pdf` extraction returns page-hinted plain text
- `docx` extraction returns paragraph-hinted plain text
- empty/unsupported binary input fails with explicit bounded-slice messaging
- fake provider remains available and unchanged for current fixture-heavy tests

Use local fixture files stored in the test project or generated deterministically during the test run.

## Acceptance Criteria

- A text-bearing local `pdf` file can produce deterministic extracted text and evidence anchors without OCR.
- A text-bearing local `docx` file can produce deterministic extracted text and evidence anchors without OCR.
- Unsupported or OCR-dependent binary cases fail explicitly without broadening scope.
- Existing fake-first planning and launch evidence remain intact.
- `.\scripts\verify-repo.ps1` and `.\scripts\preflight-release.ps1` pass after the slice lands.

## Rollback

Rollback by removing the new local binary extraction provider, its registration, and its focused tests.

The existing fake extractor and higher-level planning flow should continue to work because this slice only narrows infrastructure behavior for supported binary kinds.
