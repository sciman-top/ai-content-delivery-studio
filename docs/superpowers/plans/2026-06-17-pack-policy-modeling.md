# Pack Policy Modeling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Make the generic image-series workflow pack declare explicit scenario and policy-pack references, then enforce that contract in the pack registry and built-in starter catalog.

**Architecture:** Extend `WorkflowPack` additively with scenario and policy reference lists, validate those references inside `PackRegistry`, and update the built-in starter catalog plus focused tests to satisfy the stronger contract. Keep the slice bounded to pack contracts and validation only.

**Tech Stack:** .NET 10, core pack records, pack registry validation, xUnit tests, repo-owned docs.

**Historical note:** This plan predates the internal `ImageSeriesStudio.*` to `ContentDeliveryStudio.*` rename. Old paths below are preserved as historical implementation context, not current repository truth.

---

## Task 1: Extend workflow-pack metadata with scenario and policy references

**Files:**
- Modify: `src/ContentDeliveryStudio.Core/Packs/PackMetadata.cs`
- Test: `tests/ContentDeliveryStudio.Tests/PackMetadataTests.cs`

- [x] Add additive `ScenarioIds`, `IndustryPackIds`, `RendererPackIds`, and `ReviewRubricPackIds` fields to `WorkflowPack`.
- [x] Normalize and validate those ID lists in the same style as existing pack IDs.
- [x] Add or update focused tests so invalid or empty scenario IDs fail closed.

## Task 2: Enforce the stronger contract in the registry

**Files:**
- Modify: `src/ContentDeliveryStudio.Core/Packs/PackRegistry.cs`
- Test: `tests/ContentDeliveryStudio.Tests/PackRegistryTests.cs`
- Test: `tests/ContentDeliveryStudio.Tests/PackUiDefaultsTests.cs`

- [x] Add validation that scenario IDs are unique across workflow packs.
- [x] Add validation that workflow-pack industry/renderer/review-rubric references resolve to packs of the expected type when present.
- [x] Cover missing-reference and duplicate-scenario failures with focused tests.

## Task 3: Populate built-in starter packs and prove the catalog stays valid

**Files:**
- Modify: `src/ContentDeliveryStudio.Core/Packs/BuiltInPackCatalog.cs`
- Test: `tests/ContentDeliveryStudio.Tests/BuiltInPackCatalogTests.cs`
- Test: `tests/ContentDeliveryStudio.Tests/PackPackageStoreTests.cs`

- [x] Update the built-in starter registry so the generic workflow pack declares scenario and policy-pack references.
- [x] Add the built-in generic policy packs needed to satisfy that contract.
- [x] Extend catalog tests so the starter registry proves the new generic policy links are present and valid.

## Task 4: Sync docs and verify the slice

**Files:**
- Modify: `docs/ROADMAP.md`
- Modify: `docs/TASKS.md`
- Modify: `docs/REFERENCE_BASIS.md` only through the managed sync path if needed

- [x] Update roadmap and task wording so the active lane reflects the stronger scenario/policy contract once the code lands.
- [x] Run `.\scripts\verify-repo.ps1`.
- [x] Summarize what the slice now proves and what still remains deferred for broader pack/policy work.
