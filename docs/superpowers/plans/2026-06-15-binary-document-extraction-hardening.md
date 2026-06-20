# Binary Document Extraction Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Add the first narrow real local extraction path for text-bearing `pdf` and `docx` files without widening the repository into OCR or high-fidelity document automation.

**Architecture:** Keep the existing provider-neutral extraction contract and fake extractor, then add one infrastructure-owned local binary extraction provider for `pdf` and `docx`. The slice stays bounded to deterministic text/evidence extraction and explicit failure for OCR-dependent or unsupported binary cases.

**Tech Stack:** .NET 10, existing source-ingestion/extraction contracts, repository-owned tests, local binary parser integration chosen from the reference shelf.

**Historical note:** This plan predates the internal `ImageSeriesStudio.*` to `ContentDeliveryStudio.*` rename. Old paths below are preserved as historical implementation context, not current repository truth.

---

## Task 1: Record the bounded binary extraction slice

**Files:**
- Create: `docs/superpowers/specs/2026-06-15-binary-document-extraction-hardening-design.md`
- Create: `docs/superpowers/plans/2026-06-15-binary-document-extraction-hardening.md`
- Modify: `docs/TASKS.md`
- Modify: `docs/ROADMAP.md`

- [x] **Step 1: Review the support matrix and reference basis**

Read:

```text
docs/SOURCE_ARTIFACT_SUPPORT_MATRIX.md
docs/REFERENCE_BASIS.md
docs/research/REFERENCE_RESEARCH.md
```

Expected: the slice stays limited to support-matrix-approved `pdf/docx` text extraction without OCR.

- [x] **Step 2: Keep task and roadmap wording aligned**

Update:

- `docs/TASKS.md`
- `docs/ROADMAP.md`

Acceptance:

- binary extraction hardening is described as the current next bounded slice
- OCR remains explicitly deferred

- [x] **Step 3: Verify documentation-only consistency**

Run:

```powershell
rg -n "binary extraction|OCR|pdf|docx" docs
git diff --check
```

Expected: wording stays internally consistent and diff hygiene is clean.

## Task 2: Add a narrow local binary extraction provider

**Files:**
- Create: `src/ContentDeliveryStudio.Infrastructure/Sources/LocalBinaryDocumentExtractionProvider.cs`
- Modify: `src/ContentDeliveryStudio.Infrastructure/Sources/FakeDocumentExtractionProvider.cs` only if shared helper extraction is warranted
- Modify: `src/ContentDeliveryStudio.Infrastructure/OpenAI/` no changes expected
- Modify: provider registration file only if the extraction provider is registered through DI today

- [x] **Step 1: Add a failing extraction test for PDF**

Test file:

- `tests/ContentDeliveryStudio.Tests/DocumentExtractionProviderTests.cs`

Acceptance:

- a real local provider extracts deterministic plain text with `page` location hints from a text-bearing PDF fixture

- [x] **Step 2: Run the PDF extraction test to verify it fails**

Run:

```powershell
dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter "DocumentExtractionProviderTests"
```

Expected: the red step was satisfied before the provider landed; the current repository truth is that the focused extraction tests now pass.

- [x] **Step 3: Add a failing extraction test for DOCX**

Acceptance:

- a real local provider extracts deterministic plain text with `paragraph` location hints from a text-bearing DOCX fixture

- [x] **Step 4: Add the minimal provider implementation**

Implement:

- `src/ContentDeliveryStudio.Infrastructure/Sources/LocalBinaryDocumentExtractionProvider.cs`

Requirements:

- supports `SourceAssetKind.Pdf` and `SourceAssetKind.Docx`
- returns `DocumentExtractionResult`
- preserves provider-neutral output
- fails closed for empty or OCR-dependent content

- [x] **Step 5: Run the focused extraction tests**

Run:

```powershell
dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter "DocumentExtractionProviderTests"
```

Expected: the new PDF/DOCX extraction tests pass, existing fake extractor tests still pass.

## Task 3: Keep failure boundaries explicit

**Files:**
- Modify: `tests/ContentDeliveryStudio.Tests/DocumentExtractionProviderTests.cs`
- Modify: `src/ContentDeliveryStudio.Infrastructure/Sources/LocalBinaryDocumentExtractionProvider.cs`

- [x] **Step 1: Add a failing bounded-scope failure test**

Acceptance:

- OCR-dependent or empty binary extraction fails with a message that points back to the bounded slice instead of silently succeeding

- [x] **Step 2: Run the bounded failure test to verify it fails**

Run:

```powershell
dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter "DocumentExtractionProviderTests"
```

Expected: the red step was satisfied before the bounded failure handling landed; the current repository truth is that the focused bounded-scope tests now pass.

- [x] **Step 3: Add the minimal failure handling**

Requirements:

- no OCR fallback
- no fabricated extracted text
- explicit message that OCR/high-fidelity binary extraction is outside the current supported boundary

- [x] **Step 4: Re-run the focused extraction tests**

Run:

```powershell
dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter "DocumentExtractionProviderTests"
```

Expected: all focused extraction tests pass.

## Task 4: Verify and close out

**Files:**
- Modify: `docs/TASKS.md`
- Modify: `docs/ROADMAP.md`
- Modify: `docs/USER_GUIDE.md` only if user-facing wording changes materially

- [x] **Step 1: Run the canonical repository gate**

Run:

```powershell
.\scripts\verify-repo.ps1
```

Expected: passes.

- [x] **Step 2: Run the stronger release-style gate**

Run:

```powershell
.\scripts\preflight-release.ps1
```

Expected: passes.

- [x] **Step 3: Update closeout wording in task and roadmap surfaces**

Acceptance:

- task truth reflects the binary extraction slice state honestly
- roadmap no longer describes the slice as untouched if implementation landed
