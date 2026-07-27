# OpenAI Scientific Understanding Task 27 Evidence

Date: 2026-07-27

## Scope

This evidence records Task 27 opt-in OpenAI scientific understanding. It adds a
bounded SDK Responses adapter, strict structured-output schema, local mapper,
telemetry, dependency injection, and live-mode runtime selection while keeping
the default desktop and all repository tests on fake providers.

No paid or network provider call was made in this task. Task 28 live semantic
and visual review, Task 29 live acceptance, Task 30 closeout, and Checkpoint 5
remain open.

## Official Basis

- OpenAI Structured Outputs guidance documents strict `text.format` JSON Schema
  for typed Responses output and requires applications to handle refusals and
  unsuitable content explicitly:
  https://developers.openai.com/api/docs/guides/structured-outputs
- The Responses create reference defines `store` as remote response retention:
  https://developers.openai.com/api/reference/resources/responses/methods/create
- The implementation uses the repository's existing official OpenAI .NET SDK
  Responses client and adds no package or dependency.

## Request And Authority

- Each request contains `1-8` authoritative blocks, at most `12,000` source
  characters, objective, chunk index/count, source hash, block kind, exact text,
  and page/section/character-range/bounding-region location fields.
- `StoredOutputEnabled` is `false`; max output is `4,000` tokens; the strict
  schema covers terms, claims, limitations, explicit conflicts, elements,
  relations, relation classes, and source-block authority.
- Local mapping rechecks JSON, required collections, source block membership,
  verbatim quotations, enums, confidence, accepted/validated evidence,
  limitations, explicit and implicit conflicts, proposal source authority, and
  proposal relation endpoints. Any mismatch adds a blocking code.
- Application orchestration projects any provider blocking code into a required,
  incomplete `coverage-provider-output-validation` record. Valid sibling claims
  cannot hide an invalid term, conflict, limitation, or figure proposal; both
  Understanding and Figure Spec remain blocked.

## Runtime And Tests

- `AddContentDeliveryStudioProviderRuntime` is the single selection authority:
  fake mode resolves `FakeScientificUnderstandingProvider`; explicit live mode
  resolves `OpenAiScientificUnderstandingProvider` using the text role profile.
- Captured SDK tests make no HTTP request and verify model, strict schema,
  `store: false`, token bound, input authority, anchored mapping, malformed JSON,
  unknown blocks, empty output, and conflicts.
- Focused result before full gates: scientific contract, provider-to-domain
  propagation, existing scientific understanding, runtime selection, and
  provider configuration tests passed `43 / 43`.

## Compatibility And N/A

- Paid/live provider: `gate_na`; this task proves the adapter contract and
  opt-in composition only. Recovery condition: Task 29 explicit live run.
- WPF/screenshot/UI Automation: `gate_na`; no view, layout, or user interaction
  changed. Runtime selection is covered through service-provider resolution.
- Persistence/schema: `gate_na`; result extensions are additive in-memory
  application contracts and no stored payload or SQLite schema changed.
- Supply chain: existing OpenAI SDK and resilience stack reused; no dependency
  graph or lock file changed.

## Rollback

Revert the Task 27 commit to remove the OpenAI scientific understanding adapter,
extended result contract, runtime selection, tests, and documentation. Fake
scientific understanding, Task 26 corpus acceptance, and Checkpoint 4 remain
available.

## Repository Gates

The first fresh fixed-order run passed on 2026-07-27:

1. build: exit `0`, `0` warnings, `0` errors
2. test: exit `0`, `649 / 649` passed
3. contract/invariant: OpenAI provider, host/observability, and scientific
   reference evidence plus format checks passed
4. hotspot: release preflight, nested repository verification, publish WhatIf,
   placeholder/conflict scans, and diff hygiene passed

After this final evidence update, the second fresh fixed-order run also passed
with the same `0` build warnings/errors and `649 / 649` tests, followed by
passing reference evidence, format, and release preflight gates.

Task 27 is closed. Tasks 28-30 and Checkpoint 5 remain open.
