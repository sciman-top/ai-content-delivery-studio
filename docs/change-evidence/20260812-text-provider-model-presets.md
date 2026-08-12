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
