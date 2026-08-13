# Task-Aware Text Provider Model Routing

**Date:** 2026-08-12  
**Authority:** local configuration and repository contract only  
**Risk:** provider configuration; no paid generation, publication, schema, or secret persistence

## Scope

Add deterministic `auto|fixed` text routing across exactly four registered model/reasoning pairs. Route from structured workload fields, keep fixed mode as the backward-compatible operator lock and rollback path, and keep `gpt-image-2` on the standard Images API path. No paid provider request is in scope.

## Structured Reference Decision

```reference-decision
{
  "schemaVersion": 1,
  "area": "openai-provider",
  "trigger": "real-provider-enablement",
  "consultedSources": [
    {
      "path": "docs/research/REFERENCE_RESEARCH.md",
      "revision": "working-tree-baseline-2026-08-12"
    },
    {
      "path": "https://developers.openai.com/api/docs/models/gpt-5.6-terra",
      "revision": "fetched-2026-08-12"
    },
    {
      "path": "https://ai.input.im/v1/models",
      "revision": "live-read-only-probe-2026-08-12"
    }
  ],
  "observedBehavior": "Model choice and reasoning effort are independent workload controls. The configured gateway's read-only model catalog exposes gpt-5.6-sol, gpt-5.6-terra, and gpt-image-2. Repository request contracts contain structured size, evidence, document-family, and scientific-risk fields sufficient for deterministic routing without prompt keyword inference or an extra classification call.",
  "decision": "adapt",
  "affectedContract": "TEXT_PROVIDER_ROUTING_MODE accepts auto or fixed and defaults to fixed. Auto routes text planning, scientific understanding, semantic review, and visual review among exactly sol-xhigh, sol-medium, terra-xhigh, and terra-high using structured workload fields. The chosen pair remains aligned in HTTP and SDK payloads, telemetry, capabilities, and scientific checkpoint identity. Gateway failover defaults remain fixed and image routing is unchanged.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter OpenAiTaskModelRouterTests plus provider contracts",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1"
  ]
}
```

## Verification Receipt

- `GET https://ai.input.im/v1/models`: HTTP 200; `gpt-5.6-sol`, `gpt-5.6-terra`, and `gpt-image-2` visible; no generation request issued.
- Focused routing and provider contract tests: exit 0; 105 passed, 0 failed, 0 skipped before the checkpoint identity regression was added.
- Scientific checkpoint route identity regression: exit 0; 1 passed, 0 failed, 0 skipped.
- Full repository gate `scripts/verify-repo.ps1`: exit 0; build 0 warnings and 0 errors; 812 passed, 0 failed, 0 skipped; reference evidence, product-focus contract, and format verification passed.

## Compatibility And Rollback

- Existing `.env` files without `TEXT_PROVIDER_ROUTING_MODE` remain `fixed` and retain their preset or explicit model/effort behavior.
- Unknown routing modes and presets are validation errors; live registration remains fail-closed.
- `.env` stays Git-ignored; API key values are neither logged nor committed.
- Rollback: set `TEXT_PROVIDER_ROUTING_MODE=fixed`; the retained `TEXT_PROVIDER_PRESET=sol-xhigh` becomes authoritative without changing image routing. Revert this evidence, provider routing, tests, and documentation slice only if code rollback is required.

## 2026-08-13 Route Observability Follow-Through

```reference-decision
{
  "schemaVersion": 1,
  "area": "host-and-observability",
  "trigger": "telemetry-registration",
  "consultedSources": [
    {
      "path": "D:/CODE/external/ai-content-delivery-studio-references/08-platform-and-observability/opentelemetry-dotnet",
      "revision": "2eeede10c1bb7b87f3f6f70264a804d8ee9de948"
    },
    {
      "path": "docs/superpowers/specs/2026-07-29-structured-diagnostics-log-ingestion-design.md",
      "revision": "repository-baseline-2026-08-13"
    }
  ],
  "observedBehavior": "The existing provider telemetry seam emits bounded Activity and Meter tags and mirrors a typed subset into a fail-closed local JSONL journal. The journal rejects unknown fields, unsafe strings, and fields assigned to the wrong event category.",
  "decision": "adapt",
  "affectedContract": "Add only the selected model preset, reasoning effort, and bounded route reason to the existing provider-call telemetry and diagnostics records. Preserve the existing sanitizer, exact property allowlist, queue/provider category separation, local retention limits, and best-effort non-interference with provider behavior.",
  "focusedVerification": [
    "dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter FullyQualifiedName~OpenAiProviderContractTests|FullyQualifiedName~JsonlDiagnosticsEventJournalTests --no-restore",
    "pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-reference-evidence.ps1"
  ]
}
```

- Scope: preserve all existing thresholds and provider outputs while projecting the already-selected preset, reasoning effort, and bounded route reason into provider telemetry, OpenTelemetry tags, and the redacted local JSONL diagnostics journal.
- Privacy boundary: prompts, source text, credentials, endpoints, and response bodies are not added to diagnostics. New values pass through the existing string sanitizer and exact property allowlist.
- Focused verification: `dotnet test tests/ContentDeliveryStudio.Tests/ContentDeliveryStudio.Tests.csproj --filter "FullyQualifiedName~OpenAiTaskModelRouterTests|FullyQualifiedName~OpenAiProviderContractTests|FullyQualifiedName~OpenAiScientificReviewContractTests|FullyQualifiedName~JsonlDiagnosticsEventJournalTests" --no-restore` exited `0`; `64 / 64` passed after synchronizing the fail-closed journal allowlist and category invariant.
- Full closeout: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify-repo.ps1` exited `0`; build completed with `0` warnings and `0` errors, tests passed `812 / 812`, both enforced reference areas passed, the product-focus contract passed, and `dotnet format --verify-no-changes` passed.
- Correctness boundary: this proves deterministic routing observability and persistence, not that the current thresholds are statistically optimal. Live paired model evaluation remains outside this provider-free slice.
- Rollback: revert only the additive telemetry/diagnostics fields, their tests, and this follow-through section; routing behavior and provider request payloads remain unchanged.
