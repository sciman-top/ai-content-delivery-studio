# FOCUS-010 Scholarly Source Extraction Hardening

**Date:** 2026-08-03
**Status:** repo-side completed with fresh Full evidence; no external service, OCR runtime, or paid provider was used.
**Boundary:** this hardens local extraction signals and fail-closed gating. It does not make PdfPig an authority for scientific truth or close manual/live acceptance.

## Result

- PDF extraction now uses PdfPig `ContentOrderTextExtractor` and emits ordered line blocks with monotonic character ranges.
- Caption, citation/reference, formula, and table blocks are recovered through conservative, visible heuristics; unknown or missing required structures remain blocking.
- Reading-order and required-content quality now support `Uncertain`; uncertain scholarly input produces blocking codes instead of guessed acceptance.
- The existing PdfPig `0.1.14` dependency remains unchanged. GROBID, Docling, OCR, and sidecars remain benchmark-only/frozen.
- Fixed local PDF fixtures cover ordered structures, missing required caption/citation, and uncertain reading-order behavior.

## Structured Reference Decisions

```reference-decision
{
  "schemaVersion": 1,
  "area": "scientific-figure-workflow",
  "trigger": "scholarly-source-extraction",
  "consultedSources": [
    { "path": "D:/CODE/external/ai-content-delivery-studio-references/05-document-rendering/PdfPig", "revision": "reference-current-2026-08-03" },
    { "path": "docs/research/SCIENTIFIC_FIGURE_WORKFLOW_RESEARCH.md", "revision": "repo-current-2026-08-03" },
    { "path": "src/ContentDeliveryStudio.Infrastructure/ScientificFigures/PdfPigScientificDocumentExtractor.cs", "revision": "repo-current-2026-08-03" }
  ],
  "observedBehavior": "PdfPig page.Text collapses a page into one paragraph and cannot by itself establish scholarly reading order. ContentOrderTextExtractor provides a deterministic local ordering signal, but remains heuristic; OCR-heavy, damaged, missing, or ambiguous regions must not be promoted to scientific acceptance.",
  "decision": "adapt",
  "affectedContract": "Retain PdfPig 0.1.14, split content-order text into ordered blocks, classify only conservative caption/citation/formula/table patterns, represent uncertainty explicitly, and emit blocking extraction codes for uncertain reading order or required content. Do not add GROBID, Docling, OCR, or remote extraction runtime dependencies.",
  "focusedVerification": [
    "dotnet build ContentDeliveryStudio.sln --no-restore",
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --no-restore --filter FullyQualifiedName~ScientificDocumentExtractionTests|FullyQualifiedName~ScientificSourceModelTests|FullyQualifiedName~ScientificGateOneWorkflowTests|FullyQualifiedName~ScientificUnderstandingTests",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-product-focus-plan.ps1"
  ]
}
```

## Acceptance Boundary

The evidence proves local repo contracts and deterministic fixture behavior only. It does not prove OCR recall, arbitrary publisher layouts, scientific correctness, visual fidelity, or human/live/hardware acceptance.

## Verification

- Focused scientific extraction, source-model, Gate 1, and understanding tests: `44/44` passed.
- Canonical Full: `scripts/verify-repo.ps1 -NoRestore` passed with build `0` warnings / `0` errors and tests `777/777`.
- Reference evidence, product-focus contract, format, and `git diff --check` passed.
- Release preflight passed with `84` files in the `win-x64` framework-dependent ZIP; SHA-256 `482d9c86cec1bd55d1ba07849317882402ab35790ed54d4a01a5e48792304d6f`.
