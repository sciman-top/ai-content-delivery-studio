# Provider Configuration

This file defines credential naming and role boundaries. Request routing, statefulness defaults, and surface-selection rules live in [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md).

This project treats provider credentials as role-scoped, not just vendor-scoped. A key that is licensed only for image generation must never be used for text planning or vision review.
The default configuration path is now single-key friendly: if no image-specific API key is configured, image generation falls back to `TEXT_PROVIDER_API_KEY`. Explicit image keys still override that fallback.

## Role-Scoped `.env` Format

```env
TEXT_PROVIDER_KIND=openai_compatible
TEXT_PROVIDER_BASE_URL=https://text.example/v1
TEXT_PROVIDER_API_KEY=sk-text-provider-key
TEXT_PROVIDER_ROUTING_MODE=auto
TEXT_PROVIDER_PRESET=sol-xhigh
TEXT_PROVIDER_MODEL=gpt-5.6-sol
TEXT_PROVIDER_REASONING_EFFORT=xhigh

IMAGE_PROVIDER_KIND=openai_compatible_image_only
IMAGE_PROVIDER_BASE_URL=https://image.example/v1
IMAGE_PROVIDER_MODEL=image-model
IMAGE_PROVIDER_IMAGE_SURFACE=images
IMAGE_PROVIDER_API_KEY_1=sk-image-provider-key-1
IMAGE_PROVIDER_API_KEY_2=sk-image-provider-key-2
IMAGE_PROVIDER_API_KEY_3=sk-image-provider-key-3
IMAGE_PROVIDER_API_KEY_4=sk-image-provider-key-4
IMAGE_PROVIDER_APP_ID=image-app-id
IMAGE_PROVIDER_APP_SECRET=as-image-app-secret
IMAGE_PROVIDER_CONCURRENCY_PER_KEY=10
IMAGE_PROVIDER_TOTAL_CONCURRENCY=40
```

`TEXT_PROVIDER_ROUTING_MODE` accepts `auto` or `fixed` and fails closed for any other value. The backward-compatible default is `fixed`. In `fixed` mode, `TEXT_PROVIDER_PRESET` is an optional selector for the active text-planning and vision-review tier; when present, it takes precedence over `TEXT_PROVIDER_MODEL` and `TEXT_PROVIDER_REASONING_EFFORT`:

| Preset | Model | Reasoning effort |
| --- | --- | --- |
| `sol-xhigh` | `gpt-5.6-sol` | `xhigh` |
| `sol-medium` | `gpt-5.6-sol` | `medium` |
| `terra-xhigh` | `gpt-5.6-terra` | `xhigh` |
| `terra-high` | `gpt-5.6-terra` | `high` |

Use only one active preset per text endpoint. Numbered failover endpoints may set their own `TEXT_PROVIDER_FALLBACK_N_PRESET`; failover remains reserved for transient gateway failures and is not a model-tier selector. Existing configurations without a preset continue to use the explicit model and reasoning fields.

In `auto` mode, the runtime chooses only among those four registered pairs from structured request data; it does not inspect prompt keywords and does not make a second model call to classify complexity:

| Workload | Selected preset |
| --- | --- |
| Routine series or document plan | `terra-high` |
| Series with at least 6 items or 2,400 estimated input characters | `terra-xhigh` |
| Series with at least 12 items or 3,600 estimated input characters | `sol-medium` |
| Complex educational document with at least 4,500 input weight or 8 evidence rows | `terra-xhigh` |
| Scholarly draft planning | `sol-medium` |
| Scientific understanding chunk | `terra-xhigh` |
| Scientific semantic review | `sol-medium`; high risk uses `sol-xhigh` |
| Full-resolution scientific visual review | `sol-xhigh` |
| General vision review | `terra-high`; 5 signals uses `terra-xhigh`; 8 signals uses `sol-medium` |

The selected model and effort travel together through HTTP or SDK payloads, telemetry, and scientific-review checkpoint identity. `TEXT_PROVIDER_PRESET=sol-xhigh` remains the operator rollback/default tier when routing is switched back to `fixed`. Fallback profiles remain `fixed` unless their routing mode is explicitly configured and validated for that gateway.

The preset pairs follow [OpenAI's GPT-5.6 model guidance](https://developers.openai.com/api/docs/guides/latest-model) and the [GPT-5.6 Terra model contract](https://developers.openai.com/api/docs/models/gpt-5.6-terra). Gateway-specific availability must still be confirmed through that gateway's model catalog.

## Optional Gateway Failover

Provider failover is profile-scoped. Use the primary provider keys above for the preferred gateway, then append numbered fallback profiles when another gateway is allowed to serve the same role.

For text planning and vision review:

```env
TEXT_PROVIDER_BASE_URL=https://primary-gateway.example/v1
TEXT_PROVIDER_API_KEY=sk-primary
TEXT_PROVIDER_MODEL=gpt-5.6-sol
TEXT_PROVIDER_REASONING_EFFORT=medium

TEXT_PROVIDER_FALLBACK_1_BASE_URL=https://backup-gateway.example/v1
TEXT_PROVIDER_FALLBACK_1_API_KEY=sk-backup
TEXT_PROVIDER_FALLBACK_1_PRESET=sol-medium
TEXT_PROVIDER_FALLBACK_1_MODEL=gpt-5.6-sol
TEXT_PROVIDER_FALLBACK_1_REASONING_EFFORT=medium
```

For image generation:

```env
IMAGE_PROVIDER_BASE_URL=https://primary-gateway.example/v1
IMAGE_PROVIDER_MODEL=gpt-image-2
IMAGE_PROVIDER_IMAGE_SURFACE=responses
IMAGE_PROVIDER_RESPONSES_MODEL=gpt-5.6-sol
IMAGE_PROVIDER_REASONING_EFFORT=medium
IMAGE_PROVIDER_API_KEY_1=sk-primary

IMAGE_PROVIDER_FALLBACK_1_BASE_URL=https://backup-gateway.example/v1
IMAGE_PROVIDER_FALLBACK_1_MODEL=gpt-image-2
IMAGE_PROVIDER_FALLBACK_1_IMAGE_SURFACE=images
IMAGE_PROVIDER_FALLBACK_1_API_KEY_1=sk-backup
```

`IMAGE_PROVIDER_IMAGE_SURFACE=responses` means ordinary image-generation requests default to `POST /responses` with the configured `IMAGE_PROVIDER_RESPONSES_MODEL` and an `image_generation` tool. `IMAGE_PROVIDER_IMAGE_SURFACE=images` means ordinary image-generation requests use `POST /images/generations` with `IMAGE_PROVIDER_MODEL`.

The active local fixed preset defaults to `sol-xhigh` in the recommended `.env` shape above. Without `*_PRESET`, the legacy OpenAI Responses defaults remain `gpt-5.6-sol` with explicit `reasoning.effort=medium`. Each profile may override `*_REASONING_EFFORT` with `none`, `low`, `medium`, `high`, `xhigh`, or `max`; image-only `/images/generations` requests do not receive a reasoning field. Automatic text routing does not alter `gpt-image-2`, `IMAGE_PROVIDER_IMAGE_SURFACE=images`, or `POST /images/generations`. Fake providers remain the desktop default until `PROVIDER_MODE=live` is explicitly selected.

Failover should be used only for transient or reachability failures: network failure, timeout, `408`, `429`, or `5xx`. Do not fail over on `400`, `401`, or `403`; those indicate request, credential, or authorization problems that should fail closed.

## Desktop Runtime Opt-In

The desktop app keeps fake providers as the default runtime. To let the App host use the live profiles above, set:

```env
PROVIDER_MODE=live
```

When live mode is enabled, startup reads the local `.env`, validates the text and image profiles, and registers live providers for text planning, image generation, image editing, vision review, and scientific understanding. Scientific understanding uses the primary text profile through bounded, strict, stateless Responses requests; generic text/image/vision retain their existing failover policies. Missing or invalid `.env` configuration fails closed at registration time instead of silently falling back to fake providers. Image editing uses the primary image profile through `POST /images/edits`; it is not covered by generation failover because the approval receipt binds one provider identity and model.

### Reference-Guided Image Edit Boundary

The current production edit contract is intentionally narrow:

- exactly one persisted source candidate with role `Subject`
- optional PNG mask with the same format and decoded dimensions as the source
- source/reference and mask inputs capped at 50 MB each
- one output candidate created beside, never over, the source or mask
- supported output sizes `1024x1024`, `1024x1536`, and `1536x1024`
- `png`, `jpeg`, or `webp` output and `auto`, `low`, `medium`, or `high` quality
- source, mask, instruction, provider, model, operation, and approval hashes persisted without absolute input paths

Multi-reference composition/style editing remains frozen in this slice. The provider capability matrix exposes only `Subject`, a maximum reference count of one, and optional mask editing. Captured multipart transport tests prove the request shape without network access; they are not evidence that a paid call or visual result was accepted.

The WPF gallery projects this capability but keeps the real-edit command disabled because the desktop host has no trusted paid-authority source. The existing fake edit button remains explicitly fake and is not evidence of live editing.

## Hard Boundaries

- `TEXT_PROVIDER_API_KEY` is reserved for text planning and vision review operations.
- `IMAGE_PROVIDER_API_KEY` and `IMAGE_PROVIDER_API_KEY_1..N` are reserved for image generation operations.
- `IMAGE_PROVIDER_API_KEY*` cannot be used by `OpenAiTextPlanningProvider` or `OpenAiVisionReviewProvider`, even if code manually sets broad provider permissions.
- `TEXT_PROVIDER_API_KEY` can be used by `OpenAiImageGenerationProvider` only through the built-in image-provider fallback path when no `IMAGE_PROVIDER_API_KEY*` value is configured.
- `OpenAiProviderOptions.FromTextProviderEnvironment(...)` creates options for `TextPlanning | VisionReview` only.
- `OpenAiProviderOptions.FromImageProviderEnvironment(...)` creates options for `ImageGeneration` only.

## Vision Review Runtime Defaults

For visual review, the preferred runtime shape is:

- local direct provider call from the desktop app
- dedicated `TEXT_PROVIDER_API_KEY`
- bounded batch request
- `store: false` by default
- no default `previous_response_id` chaining

Recommended implementation-facing defaults:

- review batch items: `3` to `6`
- high-risk review batch items: `2` to `4`
- split the batch before dispatch when a configured threshold is exceeded
- prepare compact local review artifacts first, such as thumbnail grids, candidate manifests, prompt summaries, and selected evidence anchors

Credential placement alone does not justify stateful review. If a workflow later enables retained remote state, that decision must still follow [PROVIDER_ROUTING_POLICY.md](./PROVIDER_ROUTING_POLICY.md).

### Scientific Review Local Checkpoints

Live scientific semantic and visual review requests remain remote-stateless with
`store: false` and no `previous_response_id` chaining. Separately, the desktop
runtime writes a local resume checkpoint after a response has passed the strict
scientific review parser. The default location is:

```text
%LOCALAPPDATA%\AIContentDeliveryStudio\provider-checkpoints\scientific-review
```

A checkpoint is reusable only when its schema, operation, endpoint, model,
reasoning effort, and SHA-256 of the exact transmitted JSON payload all match.
A missing checkpoint permits the normal bounded provider request; a corrupt or
identity-mismatched checkpoint fails closed. Resume revalidates all responsible
item identifiers against the current request and preserves `Pass`, `Fail`, or
`Uncertain` without upgrading the verdict.

Checkpoint files contain the parsed verdict, findings, provider trace id, and
request identity hashes. They do not contain provider secrets, image bytes,
base64 data URLs, or the raw provider response. Fake runtime selection neither
reads nor writes these checkpoints. A resumed result is marked
`PersistedCheckpoint`; it is reuse of an earlier parsed response, not a new live
provider acceptance event.

## Same-Key Providers

Some official or full OpenAI-compatible providers may license the same key for text, vision, and image operations.

Supported configuration shapes are:

- default single-key mode: configure `TEXT_PROVIDER_API_KEY`; image generation reuses it automatically when no `IMAGE_PROVIDER_API_KEY*` is present
- explicit dual-key mode: configure `TEXT_PROVIDER_API_KEY` plus `IMAGE_PROVIDER_API_KEY` or `IMAGE_PROVIDER_API_KEY_1..N`; image generation uses the explicit image key pool

When a provider contract explicitly permits both roles, dual-key mode may still reuse the same underlying secret value under both names. The runtime now no longer requires that duplication for the default single-key path.

Do not put an image-only merchant key under `TEXT_PROVIDER_API_KEY` or generic `OPENAI_API_KEY`. That bypasses the user's license intent and should be treated as misconfiguration.

Implementation guardrail:

- `ProviderEnvironmentConfiguration.Image.UsesSharedTextApiKeyFallback` is true only when no `IMAGE_PROVIDER_API_KEY*` value is present and `TEXT_PROVIDER_API_KEY` is present.
- `OpenAiProviderGuard` allows image generation with `TEXT_PROVIDER_API_KEY` only when `OpenAiProviderOptions.UsesSharedTextApiKeyFallback` is true.
- Adding any explicit `IMAGE_PROVIDER_API_KEY` or `IMAGE_PROVIDER_API_KEY_1..N` disables the fallback and restores the image key pool as the image-generation credential source.

## Statefulness Reminder

Credential placement does not decide whether a workflow should use remote retained state. The V1 default remains `store: false` unless the provider routing policy explicitly allows a stateful workflow.

## OpenAI Launch Preflight

The repository now includes a read-only OpenAI launch preflight path that evaluates whether the current local provider configuration is ready for a live V1 sample run.

Current behavior:

- reads role-scoped provider configuration from the selected `.env` path
- checks text-planning, vision-review, and image-generation readiness separately
- applies the real-provider smoke gate before any live smoke path is considered runnable
- writes local `json` and `md` reports under a diagnostics folder

Default smoke opt-in gate:

```env
IMAGE_SERIES_STUDIO_OPENAI_REAL_API_SMOKE=1
```

If this variable is missing or set to a different value, the preflight remains a dry-run readiness check and records the blocking reason instead of treating the smoke path as enabled.

The preflight is intentionally low-risk:

- no secret values are exported
- no provider dashboard state is required
- normal use stays on the local diagnostics path

This preflight is a readiness and evidence tool, not a replacement for the eventual live `2-item` V1 provider run recorded in [V1_LAUNCH_EVIDENCE.md](./V1_LAUNCH_EVIDENCE.md).

## Health Checks

Provider Center health checks use non-generating `/v1/models` requests. They validate connectivity and authentication without creating text completions or images. If a merchant forbids all non-image endpoints, skip Provider Center health checks for that image provider and rely on opt-in image smoke tests.
