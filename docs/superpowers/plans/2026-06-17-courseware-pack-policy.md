# Courseware Visual Pack Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the stronger scenario/policy pack contract to the built-in `courseware-visual` workflow pack.

**Architecture:** Reuse the existing additive workflow-pack fields and registry validation, then populate courseware-specific policy packs in the built-in starter catalog with focused test updates. Keep the slice bounded to the courseware scenario only.

**Tech Stack:** .NET 10, core pack records, built-in pack catalog, xUnit tests, repo-owned docs.

---

## Task 1: Add courseware policy-pack IDs and starter catalog wiring

**Files:**
- Modify: `src/ContentDeliveryStudio.Core/Packs/BuiltInPackCatalog.cs`
- Test: `tests/ContentDeliveryStudio.Tests/BuiltInPackCatalogTests.cs`

- [ ] Add built-in courseware industry, renderer, and review-rubric pack IDs.
- [ ] Wire the courseware workflow pack to those policy packs.
- [ ] Extend built-in catalog tests to assert the courseware policy links.

## Task 2: Verify the scenario contract remains valid

**Files:**
- Modify: `tests/ContentDeliveryStudio.Tests/PackPackageStoreTests.cs`
- Modify: `docs/TASKS.md`
- Modify: `docs/ROADMAP.md`

- [ ] Update any pack-count or contract assertions affected by the courseware scenario policy packs.
- [ ] Update backlog wording so the courseware scenario slice is no longer outstanding once it lands.
- [ ] Run `.\scripts\verify-repo.ps1`.
