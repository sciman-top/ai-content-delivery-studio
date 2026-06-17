# Document Review Translation Pack Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the stronger scenario/policy pack contract to the built-in `document-review-translation` workflow pack.

**Architecture:** Reuse the existing additive workflow-pack fields and registry validation, then populate document-review-specific policy packs in the built-in starter catalog with focused test updates. Keep the slice bounded to the document-review scenario only.

**Tech Stack:** .NET 10, core pack records, built-in pack catalog, xUnit tests, repo-owned docs.

---

## Task 1: Add document-review policy-pack IDs and starter catalog wiring

**Files:**
- Modify: `src/ContentDeliveryStudio.Core/Packs/BuiltInPackCatalog.cs`
- Test: `tests/ContentDeliveryStudio.Tests/BuiltInPackCatalogTests.cs`

- [ ] Add built-in document-review industry, renderer, and review-rubric pack IDs.
- [ ] Wire the document-review workflow pack to those policy packs.
- [ ] Extend built-in catalog tests to assert the document-review policy links.

## Task 2: Verify the scenario contract remains valid

**Files:**
- Modify: `tests/ContentDeliveryStudio.Tests/PackPackageStoreTests.cs`
- Modify: `docs/TASKS.md`
- Modify: `docs/ROADMAP.md`

- [ ] Update any pack-count or contract assertions affected by the document-review scenario policy packs.
- [ ] Update backlog wording so the document-review scenario slice is no longer outstanding once it lands.
- [ ] Run `.\scripts\verify-repo.ps1`.
