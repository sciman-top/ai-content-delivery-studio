# Poster Report Delivery Pack Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Apply the stronger scenario/policy pack contract to the built-in `poster-report-delivery` workflow pack.

**Architecture:** Reuse the existing additive workflow-pack fields and registry validation, then populate poster-specific policy packs in the built-in starter catalog with focused test updates. Keep the slice bounded to the poster scenario only.

**Tech Stack:** .NET 10, core pack records, built-in pack catalog, xUnit tests, repo-owned docs.

**Historical note:** This plan predates the internal `ImageSeriesStudio.*` to `ContentDeliveryStudio.*` rename. Old paths below are preserved as historical implementation context, not current repository truth.

---

## Task 1: Add poster policy-pack IDs and starter catalog wiring

**Files:**
- Modify: `src/ContentDeliveryStudio.Core/Packs/BuiltInPackCatalog.cs`
- Test: `tests/ContentDeliveryStudio.Tests/BuiltInPackCatalogTests.cs`

- [x] Add built-in poster industry, renderer, and review-rubric pack IDs.
- [x] Wire the poster workflow pack to those policy packs.
- [x] Extend built-in catalog tests to assert the poster policy links.

## Task 2: Verify the scenario contract remains valid

**Files:**
- Modify: `tests/ContentDeliveryStudio.Tests/PackPackageStoreTests.cs`
- Modify: `docs/TASKS.md`
- Modify: `docs/ROADMAP.md`

- [x] Update any pack-count or contract assertions affected by the poster scenario policy packs.
- [x] Update backlog wording so the poster scenario slice is no longer outstanding once it lands.
- [x] Run `.\scripts\verify-repo.ps1`.
