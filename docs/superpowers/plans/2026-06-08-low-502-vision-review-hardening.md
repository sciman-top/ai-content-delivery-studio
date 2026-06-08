# Low-502 Vision Review Hardening Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Encode low-502 review defaults into AI Content Delivery Studio so cloud vision review runs as bounded local-direct requests instead of long fragile review chains.

**Architecture:** Strengthen the existing provider-routing and review architecture rather than adding a parallel subsystem. The hardening slice should standardize local review-prep artifacts, batch thresholds, stateless default review requests, and task-level reminders before any broader multi-turn review state is enabled.

**Tech Stack:** Markdown policy docs, existing provider-routing and architecture docs, future .NET application options and operator descriptors.

---

### Task 1: Record the low-502 review design

**Files:**
- Create: `docs/superpowers/specs/2026-06-08-low-502-vision-review-design.md`
- Modify: `docs/PROVIDER_ROUTING_POLICY.md`
- Modify: `docs/ARCHITECTURE.md`

- [x] Write the design spec that distinguishes bounded local-direct cloud review from long conversational review chains.
- [x] Add routing-policy language that structured visual review should prefer direct local stateless requests by default.
- [x] Add architecture language that local review-prep artifacts and batch thresholds are first-class parts of the review loop.

Run:

```powershell
rg -n "low-502|store: false|previous_response_id|contact sheet|bounded review" docs
```

Expected: the new review boundary is visible in the design and policy docs.

### Task 2: Reflect the boundary in implementation-facing task tracking

**Files:**
- Modify: `docs/TASKS.md`

- [x] Add an unchecked task to introduce bounded local review-prep artifacts before expanding multi-turn image state.
- [x] Add an unchecked task to encode review batch thresholds and stateless defaults in implementation-facing options or operator descriptors.
- [x] Keep the new tasks aligned with the existing V1 launch boundary instead of broadening scope.

Run:

```powershell
rg -n "review batch|stateless|local-direct|previous_response_id" docs/TASKS.md
```

Expected: the task list now makes the anti-drift review boundary explicit.
