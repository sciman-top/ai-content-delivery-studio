# OpenAI Scientific Review Task 28 Evidence

Date: 2026-07-27

## Scope

This evidence records Task 28 opt-in OpenAI scientific semantic and visual
review. The default runtime remains fake-first. Explicit live mode selects one
stateless OpenAI provider through two independent application contracts.

Task 29 paid live acceptance, Task 30 closeout, and Checkpoint 5 remain open.

## Review Contracts

- Semantic input contains only approved claims, exact evidence locations and
  quotations, the approved specification, and the deterministic render summary.
- Visual input contains the original full-resolution PNG followed by every
  typed critical-item crop. Images use `detail: high`; the generic compact
  384-pixel/JPEG review path is not called. Provider-side model tokenization may
  still resize inputs, so typed crops, deterministic checks, and human review
  remain required.
- Both requests use fresh Responses calls, `store: false`, a `3,000` output-token
  bound, and strict JSON Schema.
- Local mapping validates verdict/finding enums, non-empty fields, failure
  findings, and every responsible element/relation id. Provider errors,
  malformed output, `Uncertain`, fail-without-findings, and unknown ids all
  block Gate 2.
- Existing full-resolution, pixel, crop-count, and crop-byte dispatch budgets
  are rechecked at the provider boundary.

## Runtime And Tests

- Standard OpenAI service registration resolves the provider through the
  repository's named HTTP client.
- Desktop fake mode resolves independent fake semantic and visual providers.
  Explicit live mode resolves the shared OpenAI scientific review provider for
  both contracts using the text/vision role profile.
- Captured HTTP contract tests do not make network calls. The focused scientific
  review, runtime selection, and OpenAI smoke set passed `15 / 15`.

## Compatibility And N/A

- Paid/live provider: `gate_na`; Task 28 proves the opt-in adapter contract only.
  Recovery condition: Task 29 explicit paid live acceptance.
- WPF/screenshot/UI Automation: `gate_na`; no view or interaction changed.
- Persistence/schema: `gate_na`; no SQLite or stored domain schema changed.
- Supply chain: no dependency, package, or lock-file change.

## Rollback

Revert the Task 28 commit to remove the OpenAI scientific review mapper,
provider, runtime bindings, tests, and this evidence. Fake review and the Task
27 scientific-understanding adapter remain available.

## Repository Gates

The first fresh fixed-order run passed on 2026-07-27:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `656 / 656` passed
3. contract/invariant: OpenAI provider reference evidence and format passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed

During the first hotspot attempt, one existing SkiaSharp-to-PdfPig export text
extraction assertion failed once. The same full suite had passed immediately
before it; the isolated test and a fresh full suite then passed, and the complete
hotspot rerun passed `656 / 656`. No exporter, PDF, font, or test-concurrency code
was changed in this task.

After the evidence and task-status update, the second fresh fixed-order run also
passed with build `0` warnings/errors, test `656 / 656`, reference evidence,
format, and the complete release preflight. A final post-evidence run is recorded
before commit so this file does not rely on a gate that predates its last edit.
